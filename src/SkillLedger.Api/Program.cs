using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Api.Configuration;
using SkillLedger.Api.Middleware;
using SkillLedger.Infrastructure.Authorization;
using SkillLedger.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Microsoft.AspNetCore.DataProtection;
using System.Reflection; // For GetCustomAttribute
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using StackExchange.Redis;
using Microsoft.AspNetCore.HttpOverrides; // For ForwardedHeaders
using Resend;
using Microsoft.AspNetCore.Mvc;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Key Vault for production
if (builder.Environment.IsProduction())
{
    var keyVaultEndpoint = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT");
    if (!string.IsNullOrEmpty(keyVaultEndpoint))
    {
        builder.Configuration.AddAzureKeyVaultWithFallback(
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AzureKeyVaultConfiguration>(),
            builder.Environment
        );
    }
}

// P1 PERFORMANCE FIX: Configure request timeout to prevent hanging requests
builder.WebHost.ConfigureKestrel(options =>
{
    // Set global request timeout to 30 seconds
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(130); // Slightly longer than request timeout
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Sentry()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Sentry
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? "";
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(sentryDsn))
    Log.Warning("Sentry DSN is not configured — error tracking is disabled in {Env}", builder.Environment.EnvironmentName);

builder.WebHost.UseSentry(o =>
{
    o.Dsn = sentryDsn;
    o.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.2;
    o.SendDefaultPii = false;
    o.Environment = builder.Environment.EnvironmentName;
    o.MinimumEventLevel = LogLevel.Error;
    o.MinimumBreadcrumbLevel = LogLevel.Information;
});

// Configure Application Insights for production/self-hosted environments
if (builder.Environment.IsProduction() || builder.Environment.IsEnvironment("SelfHosted"))
{
    var appInsightsConnectionString = builder.Configuration.GetConnectionString("ApplicationInsights");
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        // BUG-39 FIX: Never log the connection string — it contains the instrumentation key.
        // Use Serilog so the message goes through the structured logging pipeline.
        Log.Information("Application Insights connection string detected (length: {Length})", appInsightsConnectionString.Length);

        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
            options.EnableAdaptiveSampling = true;
            options.EnableQuickPulseMetricStream = true;
            options.EnablePerformanceCounterCollectionModule = true;
            options.EnableDependencyTrackingTelemetryModule = true;
        });

        Log.Information("Application Insights configured successfully");
    }
    else
    {
        Log.Warning("Application Insights connection string not configured — telemetry disabled");
        builder.Services.AddSingleton<Microsoft.ApplicationInsights.TelemetryClient>(provider =>
            new Microsoft.ApplicationInsights.TelemetryClient());
    }
}
else
{
    // BUG-39 FIX: Use Serilog instead of Console.WriteLine
    Log.Debug("Development environment — Application Insights disabled");
    builder.Services.AddSingleton<Microsoft.ApplicationInsights.TelemetryClient>(provider =>
        new Microsoft.ApplicationInsights.TelemetryClient());
}

// Add services to the container
builder.Services.AddMvc(options =>
{
    // This ensures all MVC features including ViewFeatures are available
    // Required for ValidateAntiForgeryToken attribute to work
});

// Ensure controllers are explicitly registered
builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // BUG-BE-023 FIX: Allow enum serialization as strings (e.g., "Monthly" instead of 0)
        // This is required for frontend compatibility where enums are sent as strings
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add SignalR for real-time messaging
builder.Services.AddSignalR();

// Configure Entity Framework
if (builder.Environment.IsEnvironment("Testing"))
{
    // Use unique database name for each test execution to prevent cross-test interference
    var databaseName = $"TestDb_{Guid.NewGuid()}";
    builder.Services.AddDbContext<SkillLedgerDbContext>(options =>
        options.UseInMemoryDatabase(databaseName));
}
else
{
    var useSqlite = builder.Configuration.GetValue<bool>("Database:UseSqlite");

    if (useSqlite)
    {
        Log.Information("Using SQLite database for development");
        builder.Services.AddDbContext<SkillLedgerDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"),
                b => b.MigrationsAssembly("SkillLedger.Infrastructure")));
    }
    else
    {
        Log.Information("Using PostgreSQL database");
        // Support both PostgreSQL URI format (postgresql://...) and ADO.NET format (Host=...;...)
        var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured");
        Npgsql.NpgsqlConnectionStringBuilder csBuilder;
        if (rawConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || rawConnectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(rawConnectionString);
            csBuilder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]),
                Password = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]),
                SslMode = Npgsql.SslMode.Require,
            };
        }
        else
        {
            csBuilder = new Npgsql.NpgsqlConnectionStringBuilder(rawConnectionString);
        }
        // Optimize connection pool for Neon serverless (allow connections to fully close when idle)
        csBuilder.MinPoolSize = 0;
        csBuilder.ConnectionIdleLifetime = 30;
        var optimizedConnectionString = csBuilder.ConnectionString;

        builder.Services.AddDbContext<SkillLedgerDbContext>(options =>
        {
            options.UseNpgsql(optimizedConnectionString,
                b =>
                {
                    b.MigrationsAssembly("SkillLedger.Infrastructure");
                    b.CommandTimeout(30);
                    b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                });
            options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
            options.EnableDetailedErrors(builder.Environment.IsDevelopment());
        });
    }
}

// BUG-042 ISOLATION TEST RESULT: Removing Identity did NOT fix the hang!
// The issue is NOT with ASP.NET Core Identity's .AddEntityFrameworkStores()
// Restoring Identity configuration...

// Configure Identity
builder.Services.AddIdentity<User, SkillLedger.Core.Entities.Role>(options =>
{
    // Password settings - matching our validation requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Email confirmation not required
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<SkillLedgerDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// Configure Identity's Application Cookie (this overrides the default .AspNetCore.Identity.Application cookie)
builder.Services.ConfigureApplicationCookie(options =>
{
    // CRITICAL FIX: Configure Identity's cookie to match our custom authentication cookie settings
    options.Cookie.Name = ".SkillLedger.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";

    // API FIX: Return 401 JSON response instead of redirecting to /Account/Login
    options.LoginPath = null;
    options.AccessDeniedPath = null;
    options.LogoutPath = null;

    // Development vs Production settings
    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
    {
        var allowInsecureDevCookies = builder.Configuration["AllowInsecureDevCookies"] == "true";

        if (allowInsecureDevCookies)
        {
            // DEVELOPMENT ONLY: Allow HTTP cookies
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        }
        else
        {
            // DEFAULT: Use secure settings even in development
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        }
    }
    else
    {
        // PRODUCTION: Use SameSite=None for cross-subdomain auth (skillledger.app <-> api.skillledger.app)
        // This requires Secure=true (HTTPS only) which is enforced below
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Set domain to allow cookie sharing across subdomains
        options.Cookie.Domain = ".skillledger.app";
    }

    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true;

    // Event handlers for authentication
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        },
        OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
    };
});

// Configure Anti-forgery tokens
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;

    // Configure cookie for development and testing
    if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
    {
        options.Cookie.Name = ".SkillLedger.Antiforgery";
        options.Cookie.SameSite = SameSiteMode.Lax; // Allows cookies with same-site requests (via Next.js proxy)
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP for local development
        options.Cookie.HttpOnly = true;
    }
    else
    {
        // Production settings - SameSite=None for cross-subdomain requests
        options.Cookie.Name = ".SkillLedger.Antiforgery";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
        options.Cookie.HttpOnly = true;
        options.Cookie.Domain = ".skillledger.app";
    }
});

// Configure Data Protection (simplified for compatibility)
builder.Services.AddDataProtection();

// Add Rate Limiting (skip in testing environment)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddRateLimitingServices(builder.Configuration);
}

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Configure<SequencerOptions>(options =>
{
    var section = builder.Configuration.GetSection(SequencerOptions.SectionName);
    options.BaseUrl = builder.Configuration["SEQUENCER_BASE_URL"] ?? section["BaseUrl"];
    options.CloudflareAccessClientId = builder.Configuration["SEQUENCER_CF_ACCESS_CLIENT_ID"] ?? section["CloudflareAccessClientId"];
    options.CloudflareAccessClientSecret = builder.Configuration["SEQUENCER_CF_ACCESS_CLIENT_SECRET"] ?? section["CloudflareAccessClientSecret"];
});
builder.Services.AddHttpClient<ISequencerClient, SequencerClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SequencerOptions>>().Value;
    if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseAddress))
    {
        client.BaseAddress = baseAddress;
    }

    client.Timeout = TimeSpan.FromSeconds(10);
});

// Register Resend Email Service
var resendApiKey = builder.Configuration["Resend:ApiKey"];
if (string.IsNullOrEmpty(resendApiKey))
{
    builder.Services.AddScoped<IEmailService, MockEmailService>();
    Log.Warning("Using MockEmailService - Resend API key not configured");
}
else
{
    // Configure Resend client
    builder.Services.AddOptions();
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.Configure<ResendClientOptions>(o => o.ApiToken = resendApiKey);
    builder.Services.AddTransient<IResend, ResendClient>();
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
    Log.Information("Using ResendEmailService for email delivery");
}

builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Phase 1: Foundation Services for Bug Fix Implementation
builder.Services.AddScoped<ControllerHelperService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IExperienceService, ExperienceService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectSearchService, ProjectSearchService>();
builder.Services.AddScoped<IProjectApplicationService, ProjectApplicationService>();
builder.Services.AddScoped<IProviderSelectionService, ProviderSelectionService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAzureKeyVaultService, AzureKeyVaultService>();
builder.Services.AddScoped<ICreditWalletService, CreditWalletService>();
builder.Services.AddScoped<ICreditTransferService, CreditTransferService>();
builder.Services.AddScoped<IProjectEscrowService, ProjectEscrowService>();
builder.Services.AddScoped<IFinancialReportingService, FinancialReportingService>();
builder.Services.AddScoped<IFinancialExportService, FinancialExportService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IFileShareService, FileShareService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
// Register Content Moderation Service - use mock if Azure Content Safety not configured
var contentModerationApiKey = builder.Configuration["ContentModeration:ApiKey"];
if (string.IsNullOrEmpty(contentModerationApiKey))
{
    builder.Services.AddScoped<IContentModerationService, MockContentModerationService>();
    Log.Warning("Using MockContentModerationService - Azure Content Safety not configured");
}
else
{
    builder.Services.Configure<ContentModerationConfiguration>(
        builder.Configuration.GetSection("ContentModeration"));
    builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
}
builder.Services.AddScoped<IReputationCalculationService, ReputationCalculationService>();
// Register Document Management Services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IVirusScanService, VirusScanService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<ICdnService, CdnService>();
builder.Services.AddScoped<IDocumentSearchService, DocumentSearchService>();
builder.Services.AddScoped<IFilePreviewService, FilePreviewService>();
builder.Services.AddScoped<IDocumentSharingService, DocumentSharingService>();

// Register subscription services
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISubscriptionBillingService, SubscriptionBillingService>();
builder.Services.AddScoped<SubscriptionDataSeeder>();
builder.Services.AddScoped<ProjectDataSeeder>();

// Configure Stripe
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Configure Payment Retry settings
builder.Services.Configure<PaymentRetryConfiguration>(builder.Configuration.GetSection("PaymentRetry"));

// BUG-031 FIX: Removed commented-out Stripe services
// BUG-CRIT-004 FIX: Re-enabled StripeWebhookService with proper signature validation
builder.Services.AddScoped<StripeCheckoutService>();
builder.Services.AddScoped<StripeWebhookService>();
builder.Services.AddScoped<PaymentErrorHandlingService>();
builder.Services.AddScoped<IStripePromotionService, StripePromotionService>();

// Configure Backup and CDN settings
builder.Services.Configure<BackupConfiguration>(options =>
{
    options.CompressBackups = true;
    options.VerifyBackups = true;
    options.DefaultRetentionDays = 90;
    options.MaxBackupsPerDocument = 10;
});

// BE-LOW-001 FIX: Move CDN configuration to appsettings with sensible defaults
builder.Services.Configure<CdnConfiguration>(options =>
{
    var cdnSection = builder.Configuration.GetSection("Cdn");
    options.CdnEndpoint = cdnSection["Endpoint"] ?? "https://cdn.skillledger.app";
    options.EnableCompression = cdnSection.GetValue("EnableCompression", true);
    options.DefaultCacheDurationMinutes = cdnSection.GetValue("DefaultCacheDurationMinutes", 1440);
});
builder.Services.AddScoped<IMilestoneTrackingService, MilestoneTrackingService>();

// Register anti-gaming fraud detection services
builder.Services.AddScoped<IAntiGamingService, AntiGamingService>();
builder.Services.AddScoped<IGamingDetectionML, GamingDetectionML>();
builder.Services.AddScoped<IGraphDatabaseService, PostgresGraphDatabaseService>();

// Badge system services
builder.Services.Configure<BadgeSecurityConfiguration>(builder.Configuration.GetSection(BadgeSecurityConfiguration.SectionName));
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<IBadgeSecurityService, BadgeSecurityService>();
builder.Services.AddHttpClient<IExternalIntegrationService, ExternalIntegrationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "SkillLedger/1.0");
});
builder.Services.Configure<GamingDetectionConfig>(options =>
{
    options.HighRiskThreshold = 0.8m;
    options.MediumRiskThreshold = 0.6m;
    options.AutoSanctionThreshold = 0.95m;
    options.MaxReviewsPerDay = 10;
    options.MaxReviewsPerHour = 3;
    options.ContentSimilarityThreshold = 0.8m;
    options.NetworkConnectionMinSize = 3;
    options.CoordinatedTimingWindow = TimeSpan.FromMinutes(30);
});

// Register filters
builder.Services.AddScoped<SkillLedger.Api.Filters.ConditionalAntiforgeryFilter>();

// Configure settings
builder.Services.Configure<AzureKeyVaultSettings>(builder.Configuration.GetSection(AzureKeyVaultSettings.SectionName));
builder.Services.Configure<EncryptionConfiguration>(builder.Configuration.GetSection(EncryptionConfiguration.SectionName));

// Register authentication and authorization services
builder.Services.AddScoped<AzureKeyVaultService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<SkillLedger.Core.Interfaces.IAuthorizationService, SkillLedger.Infrastructure.Services.AuthorizationService>();

// Configure authorization policies
// SECURITY FIX: Removed debug Console.WriteLine statements that should not be in production code
try
{
    builder.Services.AddAuthorization(options =>
    {
        // Active subscription policies
        options.AddPolicy(SubscriptionPolicies.ActiveSubscription, policy =>
            policy.Requirements.Add(new ActiveSubscriptionRequirement()));

        options.AddPolicy("ActiveOrTrialSubscription", policy =>
            policy.Requirements.Add(new ActiveOrTrialSubscriptionRequirement()));

        // Tier-based policies
        options.AddPolicy(SubscriptionPolicies.BusinessOrHigher, policy =>
            policy.Requirements.Add(new BusinessOrHigherRequirement()));

        options.AddPolicy(SubscriptionPolicies.EnterpriseTier, policy =>
            policy.Requirements.Add(new EnterpriseTierRequirement()));

        // Feature-based policies
        options.AddPolicy(SubscriptionPolicies.PrioritySupport, policy =>
            policy.Requirements.Add(new PrioritySupportRequirement()));

        options.AddPolicy(SubscriptionPolicies.ApiAccess, policy =>
            policy.Requirements.Add(new ApiAccessRequirement()));

        options.AddPolicy(SubscriptionPolicies.AdvancedAnalytics, policy =>
            policy.Requirements.Add(new AdvancedAnalyticsRequirement()));

        options.AddPolicy(SubscriptionPolicies.AdvancedFraudDetection, policy =>
            policy.Requirements.Add(new AdvancedFraudDetectionRequirement()));

        options.AddPolicy(SubscriptionPolicies.MultiSignature, policy =>
            policy.Requirements.Add(new MultiSignatureRequirement()));

        options.AddPolicy(SubscriptionPolicies.CustomIntegrations, policy =>
            policy.Requirements.Add(new CustomIntegrationsRequirement()));

        // Access policies
        options.AddPolicy(SubscriptionPolicies.TeamMemberAccess, policy =>
            policy.Requirements.Add(new TeamMemberAccessRequirement()));

        options.AddPolicy(SubscriptionPolicies.UnlimitedProjects, policy =>
            policy.Requirements.Add(new UnlimitedProjectsRequirement()));

        // User type policies
        options.AddPolicy(SubscriptionPolicies.TrialUsers, policy =>
            policy.Requirements.Add(new SubscriptionRequirement
            {
                AllowTrial = true,
                RequiredTierNames = new List<string> { "Professional", "Business", "Enterprise" }
            }));

        options.AddPolicy(SubscriptionPolicies.PaidUsers, policy =>
            policy.Requirements.Add(new SubscriptionRequirement
            {
                AllowTrial = false,
                RequiredTierNames = new List<string> { "Professional", "Business", "Enterprise" }
            }));

        // Admin role policy
        options.AddPolicy("RequireAdminPermission", policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy("RequireAdminRole", policy =>
            policy.RequireRole("Admin"));
    });
}
catch (Exception ex)
{
    // SECURITY FIX: Use Serilog instead of Console.WriteLine for exception logging
    Log.Fatal(ex, "FATAL: Failed to configure authorization policies. Application cannot start safely.");
    throw;
}

// Register custom authorization policy provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Register subscription authorization handler
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionAuthorizationService>();

// Add memory cache for JWT token blacklisting
builder.Services.AddMemoryCache();

// Configure Redis caching with fallback
// PERFORMANCE: Skip Redis in Testing environment to speed up test startup
if (!builder.Environment.IsEnvironment("Testing"))
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    var cacheEnabled = builder.Configuration.GetValue<bool>("Cache:Enabled", true);
    var useRedis = builder.Configuration.GetValue<bool>("Cache:UseRedis", true);

    if (cacheEnabled && useRedis && !string.IsNullOrEmpty(redisConnectionString))
    {
        try
        {
            // Add Redis distributed cache
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "SkillLedger:";
            });

            // Register Redis connection multiplexer for advanced operations
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                try
                {
                    return ConnectionMultiplexer.Connect(redisConnectionString);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to connect to Redis. Cache service will fall back to in-memory cache.");
                    // Return null, CacheService will handle fallback
                    return null!;
                }
            });

            // BUG-014 FIX: Don't log full connection string (may contain password)
            // Extract host/port for logging without exposing credentials
            try
            {
                // Safe Split pattern - extract host from connection string
                var serverParts = redisConnectionString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (serverParts.Length > 0)
                {
                    var hostParts = serverParts[0].Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var sanitizedInfo = hostParts.Length > 0 ? hostParts[0] : "unknown";
                    Log.Information("Redis distributed cache configured: {Host}", sanitizedInfo);
                }
                else
                {
                    Log.Information("Redis distributed cache configured");
                }
            }
            catch
            {
                // If sanitization fails, just log that Redis is configured without details
                Log.Information("Redis distributed cache configured");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Redis. Using in-memory cache only.");

            // Register a null IConnectionMultiplexer to signal Redis is unavailable
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp => null!);

            // Fallback to in-memory distributed cache for idempotency service
            builder.Services.AddDistributedMemoryCache();
        }
    }
    else
    {
        Log.Information("Redis caching disabled. Using in-memory cache only.");

        // Register a null IConnectionMultiplexer to signal Redis is unavailable
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp => null!);

        // Fallback to in-memory distributed cache for idempotency service
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    // Testing mode - skip Redis entirely and use null IConnectionMultiplexer
    Log.Debug("Testing mode: Redis disabled, using in-memory cache only");
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => null!);

    // Fallback to in-memory distributed cache for idempotency service
    builder.Services.AddDistributedMemoryCache();
}

// DIAGNOSTIC: Track service registration progress
Log.Information("DIAGNOSTIC: Starting service registrations after Redis configuration");

// Register cache service
Log.Information("DIAGNOSTIC: Registering CacheService");
builder.Services.AddScoped<ICacheService, CacheService>();
Log.Information("DIAGNOSTIC: CacheService registered successfully");

// Register distributed lock service (uses Redis if available, falls back to in-memory)
Log.Information("DIAGNOSTIC: Registering DistributedLockService");
builder.Services.AddSingleton<IDistributedLockService, DistributedLockService>();
Log.Information("DIAGNOSTIC: DistributedLockService registered successfully");

// Configure Cookie Authentication (Pure cookie-based auth - no Bearer tokens)
Log.Information("DIAGNOSTIC: Configuring Cookie Authentication");

// NOTE: AddAuthentication() is automatically called by AddIdentity()
// Identity configures cookie authentication with the IdentityConstants.ApplicationScheme
// We don't need to manually add authentication here - it's already handled by ConfigureApplicationCookie() above

Log.Information("DIAGNOSTIC: Cookie authentication configured successfully");

// Add Swagger/OpenAPI
Log.Information("DIAGNOSTIC: Configuring Swagger/OpenAPI");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkillLedger API",
        Version = "v1",
        Description = "Secure user registration, authentication, and cookie-based API for SkillLedger platform"
    });

    // Add security definition for anti-forgery tokens
    c.AddSecurityDefinition("CSRF", new OpenApiSecurityScheme
    {
        Description = "Anti-forgery token",
        Name = "X-CSRF-TOKEN",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    // Add security definition for cookie authentication
    c.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
    {
        Description = "Cookie-based authentication",
        Name = ".SkillLedger.Auth",
        In = ParameterLocation.Cookie,
        Type = SecuritySchemeType.ApiKey
    });
});
Log.Information("DIAGNOSTIC: Swagger/OpenAPI configured successfully");

// Configure CORS
Log.Information("DIAGNOSTIC: Configuring CORS");

// BUG-MEDIUM-004 FIX: Validate CORS configuration at startup (skip for Testing environment)
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if ((corsOrigins == null || corsOrigins.Length == 0) && builder.Environment.EnvironmentName != "Testing")
{
    var errorMsg = "CORS AllowedOrigins not configured. API will be inaccessible. " +
                   "Configure in appsettings.json or appsettings.Development.json";
    Log.Fatal(errorMsg);
    throw new InvalidOperationException(errorMsg);
}
// Use default CORS config for Testing environment if not configured
corsOrigins ??= new[] { "http://localhost", "https://localhost" };
Log.Information("CORS configured with {OriginCount} allowed origins: {Origins}",
    corsOrigins.Length, string.Join(", ", corsOrigins));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins)
               .AllowAnyMethod()
               // BUG-MED-001 FIX: Restrict headers instead of AllowAnyHeader for better security
               // BUG-002 FIX: Use correct case for CSRF header to match antiforgery configuration
               .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept", "X-CSRF-TOKEN")
               .AllowCredentials();
    });
});
Log.Information("DIAGNOSTIC: CORS configured successfully");

// Add health checks
Log.Information("DIAGNOSTIC: Configuring health checks");
// BUG-028 FIX: Actually test database connectivity instead of always returning healthy
// CRITICAL FIX: Removed .AddDbContextCheck() which was causing startup hangs by trying to validate
// database connection during builder.Build(). The database might not exist yet (needs migrations).
// Health checks will only validate that the service is responsive, not database connectivity.
builder.Services.AddHealthChecks();
Log.Information("DIAGNOSTIC: Health checks configured successfully");

// BUG-BE-021 FIX: Configure ForwardedHeaders for reverse proxy SSL termination (Traefik)
// This allows ASP.NET Core to recognize that requests are HTTPS even when Traefik terminates SSL
// and forwards HTTP to the backend. Without this, antiforgery cookies fail with SecurePolicy=Always
Log.Information("DIAGNOSTIC: Configuring ForwardedHeaders for reverse proxy support");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
    var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? Array.Empty<string>();

    if (knownProxies.Length > 0 || knownNetworks.Length > 0)
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var proxyAddress))
            {
                options.KnownProxies.Add(proxyAddress);
            }
            else
            {
                Log.Warning("Ignoring invalid ForwardedHeaders:KnownProxies entry {Proxy}", proxy);
            }
        }

        foreach (var network in knownNetworks)
        {
            var parts = network.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var prefix) &&
                int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
            }
            else
            {
                Log.Warning("Ignoring invalid ForwardedHeaders:KnownNetworks entry {Network}", network);
            }
        }
    }

    options.ForwardLimit = builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;
});
Log.Information("DIAGNOSTIC: ForwardedHeaders configured successfully");

Log.Information("DIAGNOSTIC: All services registered. Building application...");
var app = builder.Build();
Log.Information("DIAGNOSTIC: Application built successfully!");

// Add Serilog HTTP request logging (must be early in pipeline)
// BUG-041 FIX: Add correlation ID middleware first for request tracing
app.UseCorrelationId();

app.UseSentryTracing();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
        // BUG-041 FIX: Add correlation ID to logs
        var correlationId = httpContext.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// BUG-BE-021 FIX: Use ForwardedHeaders middleware EARLY in pipeline
// This MUST run before anything that depends on Request.Scheme (authentication, antiforgery)
// Allows ASP.NET Core to recognize HTTPS requests when behind Traefik reverse proxy
app.UseForwardedHeaders();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Only use HTTPS redirection in non-testing environments
if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' https://crm.example.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self' https://crm.example.com";

    await next();
});

app.UseCors("AllowedOrigins");

// P1 PERFORMANCE FIX: Request timeout middleware (before rate limiting)
app.UseRequestTimeout();

// P1 SECURITY FIX: Rate limiting with monitoring
// Only use rate limiting in non-testing environments
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimitMonitoring(); // Add monitoring BEFORE rate limiter
    app.UseRateLimiter();
}

app.UseAuthentication();
app.UseMiddleware<SubscriptionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SkillLedger.Api.Hubs.MessagingHub>("/hubs/messaging");
app.MapHub<SkillLedger.Api.Hubs.FinancialAnalyticsHub>("/hubs/financial-analytics");
app.MapHub<SkillLedger.Api.Hubs.MilestoneTrackingHub>("/hubs/milestone-tracking");

// BUG-028 FIX: Enhanced health check with version information
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var version = System.Reflection.Assembly.GetEntryAssembly()?
            .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            version = version,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });

        await context.Response.WriteAsync(result);
    }
});

// BUG-029 FIX: Add metrics endpoint for monitoring
app.MapGet("/metrics", () =>
{
    var process = System.Diagnostics.Process.GetCurrentProcess();
    var gcMemoryInfo = GC.GetGCMemoryInfo();

    return Results.Json(new
    {
        timestamp = DateTime.UtcNow,
        process = new
        {
            workingSet = process.WorkingSet64,
            privateMemory = process.PrivateMemorySize64,
            cpuTime = process.TotalProcessorTime.TotalSeconds,
            threadCount = process.Threads.Count
        },
        gc = new
        {
            totalMemory = GC.GetTotalMemory(false),
            heapSize = gcMemoryInfo.HeapSizeBytes,
            fragmentedBytes = gcMemoryInfo.FragmentedBytes,
            gen0Collections = GC.CollectionCount(0),
            gen1Collections = GC.CollectionCount(1),
            gen2Collections = GC.CollectionCount(2)
        }
    });
}).RequireAuthorization(policy => policy.RequireRole("Admin")); // BUG-029 + BE-HIGH-001 FIX: Require Admin role for metrics (sensitive server info)

// Initialize system data on startup (needed for E2E tests)
// CRITICAL FIX: Skip this initialization during Testing environment to prevent host factory timeout
// BUG-036 FIX: Run synchronously (await) before app.Run() so migrations complete before first request
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        // BUG-BE-003 FIX: Add error handling to prevent silent failures
        using (var scope = app.Services.CreateScope())
        {
            var skillService = scope.ServiceProvider.GetRequiredService<ISkillService>();
            var subscriptionSeeder = scope.ServiceProvider.GetRequiredService<SubscriptionDataSeeder>();
            var projectSeeder = scope.ServiceProvider.GetRequiredService<ProjectDataSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

                // Apply pending migrations (skip when SKIP_DB_MIGRATIONS=true for DB-only deploy workflow)
                var skipMigrations = Environment.GetEnvironmentVariable("SKIP_DB_MIGRATIONS");
                if (skipMigrations != "true")
                {
                    logger.LogInformation("Applying database migrations...");
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");
                }
                else
                {
                    logger.LogInformation("Database migrations skipped (SKIP_DB_MIGRATIONS=true)");
                }

                // Initialize subscription tiers first
                await subscriptionSeeder.SeedSubscriptionTiersAsync();
                logger.LogInformation("Subscription tiers initialized");

                // Initialize system skills
                await skillService.InitializeSystemSkillsAsync();
                logger.LogInformation("System skills initialized");

                // Seed sample projects for testing
                await projectSeeder.SeedSampleProjectsAsync();
                logger.LogInformation("Sample projects seeded");

                // Validate subscription tiers
                var isValid = await subscriptionSeeder.ValidateSubscriptionTiersAsync();
                if (isValid)
                {
                    logger.LogInformation("Subscription tier validation passed");
                }
                else
                {
                    logger.LogWarning("Subscription tier validation failed");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing system data");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to initialize system data on startup");
        throw;
    }
}

try
{
    Log.Information("Starting SkillLedger API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to integration tests
public partial class Program { }
