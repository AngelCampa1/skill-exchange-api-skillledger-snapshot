using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Api;
using SkillLedger.Api.Configuration;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Mocks;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.RateLimiting;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Marker class to identify test environment and disable rate limiting
/// </summary>
public class TestEnvironmentMarker
{
}

/// <summary>
/// Shared web application factory configured for testing to minimize resource usage
/// Simplified to prevent deadlocks and hanging issues
/// </summary>
public class SharedWebApplicationFactory : WebApplicationFactory<Program>
{
    // Base database name - each test will append its unique identifier
    private static readonly string BaseDatabaseName = "SkillLedgerTest";
    // Shared database root to ensure all contexts use the same in-memory database instance
    internal static readonly InMemoryDatabaseRoot SharedInMemoryDatabaseRoot = new InMemoryDatabaseRoot();

    // Track active databases to ensure proper cleanup
    internal static readonly HashSet<string> ActiveDatabases = new HashSet<string>();
    internal static readonly object DatabaseLock = new object();

    // Instance-specific database name override for factory instances
    internal string? _instanceDatabaseName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // NOTE: ConfigureWebHost may be called multiple times by WebApplicationFactory infrastructure
        // This is normal and doesn't rebuild the host - it configures the builder before build
        System.Diagnostics.Debug.WriteLine("SharedWebApplicationFactory: ConfigureWebHost called");

        builder.UseEnvironment("Testing");

        // CRITICAL: Disable file system watchers to prevent inotify exhaustion
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // CRITICAL FIX: Allow any hostname in tests to prevent "Invalid Hostname" errors
                // WebApplicationFactory's test client uses a generated hostname, not localhost
                ["AllowedHosts"] = "*",
                ["Jwt:Issuer"] = "SkillLedger-Test",
                ["Jwt:Audience"] = "SkillLedger-Test-Users",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                // CRITICAL FIX: Don't set JWT keys in config for tests - let Program.cs generate temporary RSA keys
                // Setting invalid Base64 strings here causes the HostFactoryResolver to timeout waiting for IHost build
                // ["Jwt:PrivateKey"] = "test-private-key-for-testing-only",  // REMOVED - invalid Base64
                // ["Jwt:PublicKey"] = "test-public-key-for-testing-only",    // REMOVED - invalid Base64
                ["AzureKeyVaultConfiguration:Enabled"] = "false",
                ["Encryption:MasterKeyName"] = "test-master-key",
                ["Encryption:SsnEncryptionKeyName"] = "test-ssn-key",
                ["Encryption:KeyCacheDuration"] = "01:00:00",
                ["Encryption:KeySizeInBits"] = "256",
                ["Encryption:UseHsm"] = "false",
                ["Encryption:EnableKeyRotation"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=TestDatabase;Mode=Memory;Cache=Shared",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
                // Override rate limiting for testing - set very high limits to prevent test interference
                ["RateLimiting:RegistrationPerHour"] = "1000",
                ["RateLimiting:VerificationPerHour"] = "1000",
                ["RateLimiting:LoginAttemptsPerMinute"] = "1000",
                ["RateLimiting:GeneralApiPerMinute"] = "10000",
                // Badge security configuration for testing
                ["BadgeSecurity:SecretKey"] = "test-badge-secret-key-for-testing-only-min-32-characters-required",
                ["Resend:WebhookSecret"] = "whsec_test_secret_for_unit_tests",
                // CORS configuration for testing - allow test client origins (BUG-MEDIUM-004 fix requires this)
                ["Cors:AllowedOrigins:0"] = "http://localhost",
                ["Cors:AllowedOrigins:1"] = "https://localhost"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            // CRITICAL: Add startup filter to inject test database middleware into the pipeline
            services.AddSingleton<IStartupFilter, TestDatabaseMiddlewareStartupFilter>();

            // CRITICAL: Add cache services first before any other services that depend on them
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            // CRITICAL: Configure permissive rate limiting for tests to prevent 400 errors
            // [EnableRateLimiting] attributes require rate limiter services to be registered,
            // even when UseRateLimiter() middleware is not used. Add no-op rate limiting with very high limits.
            services.AddRateLimiter(options =>
            {
                // Use NoLimiter for all policies in tests - allows unlimited requests
                var noOpLimiter = RateLimitPartition.GetNoLimiter<string>("");

                // Register all policies that are used in controllers with [EnableRateLimiting]
                var allPolicies = new[]
                {
                    "RegistrationPolicy", "PhoneVerificationPolicy", "LoginPolicy",
                    "TokenRefreshPolicy", "PasswordResetPolicy", "PasswordResetVerifyPolicy",
                    "EmailVerificationPolicy", "GeneralApiPolicy", "ProjectSearchPolicy",
                    "ProfileCreationPolicy", "ReviewSubmissionPolicy", "ApplicationPolicy",
                    "ProjectCreationPolicy", "FileUploadPolicy", "MessageSendPolicy",
                    "BadgeVerificationPolicy", "CreditOperationPolicy", "NotificationPolicy",
                    "WebhookPolicy", "ExportPolicy", "BulkOperationPolicy", "AntiGamingPolicy",
                    "FraudDetectionPolicy", "AdminOperationPolicy", "MessagingPolicy",
                    "EscrowPolicy", "ProviderSelectionPolicy", "TransactionPolicy",
                    "FinancialReportingPolicy", "DocumentWorkspacePolicy", "ExperiencePolicy",
                    "WorkspacePolicy", "PaymentPolicy", "AdminPolicy", "SecurityPolicy",
                    "RoleManagementPolicy", "DocumentPreviewPolicy", "FileDownloadPolicy",
                    "WalletPolicy", "ProjectApplicationPolicy", "ProjectApplicationStatusUpdatePolicy",
                    "ProjectApplicationWithdrawPolicy", "ProjectUpdatePolicy", "ProjectPublishPolicy",
                    "ProjectDeletionPolicy", "ModerationPolicy", "ProviderSelectionUpdatePolicy",
                    "MilestonePaymentPolicy", "MilestoneStateChangePolicy", "SkillManagementPolicy",
                    "MessageSearchPolicy", "PublicProfileSearchPolicy", "ProfileUpdatePolicy",
                    "ProfileDeletionPolicy", "SubscriptionPolicy", "CheckoutPolicy",
                    "ReviewActionPolicy", "SkillEndorsementPolicy", "DefaultPolicy"
                };

                foreach (var policy in allPolicies)
                {
                    options.AddPolicy(policy, _ => noOpLimiter);
                }

                // Global limiter - also use no-op
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ => noOpLimiter);
                options.OnRejected = async (context, _) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    await context.HttpContext.Response.WriteAsync("Rate limit exceeded (test environment)");
                };
            });

            // Remove existing DbContext registrations
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<SkillLedgerDbContext>) ||
                d.ServiceType == typeof(SkillLedgerDbContext) ||
                d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                d.ServiceType.GetGenericArguments()[0] == typeof(SkillLedgerDbContext)).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add test-specific DbContext with configurable database for test isolation
            // CRITICAL FIX: Use DbContextOptionsFactory to ensure consistent database name resolution
            services.AddTransient<IDbContextOptionsFactory<SkillLedgerDbContext>, TestDbContextOptionsFactory>();

            // Add HttpContextAccessor for middleware (needed for database propagation)
            services.AddHttpContextAccessor();

            services.AddDbContext<SkillLedgerDbContext>((serviceProvider, options) =>
            {
                // CRITICAL FIX: Use the factory to get consistent database name across all contexts
                var factory = serviceProvider.GetRequiredService<IDbContextOptionsFactory<SkillLedgerDbContext>>();

                // Get database name using the same logic as the factory
                string databaseName;
                string databaseSource;

                // Tier 1: Try instance-specific database name first (for separate factory instances)
                if (_instanceDatabaseName != null)
                {
                    databaseName = _instanceDatabaseName;
                    databaseSource = "InstanceSpecific";
                }
                // Tier 2: Try HttpContext.Items (for HTTP requests)
                else if (serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext?.Items["TestDatabaseName"] is string httpDbName)
                {
                    databaseName = httpDbName;
                    databaseSource = "HttpContext.Items";
                }
                // Tier 3: Try AsyncLocal (for test setup)
                else
                {
                    databaseName = GetDatabaseNameForCurrentContext();
                    databaseSource = "AsyncLocal";
                }

                // Debug logging for database creation (disabled to reduce log noise)
                // var logger = serviceProvider.GetService<ILogger<SharedWebApplicationFactory>>();
                // logger?.LogDebug("Creating DbContext with database '{DatabaseName}' from {DatabaseSource}", databaseName, databaseSource);

                // Track the database for cleanup
                lock (DatabaseLock)
                {
                    ActiveDatabases.Add(databaseName);
                }

                // Use the shared database root to ensure all contexts use the same in-memory database instance
                options.UseInMemoryDatabase(databaseName, SharedInMemoryDatabaseRoot);
                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching(false);
                // CRITICAL: Suppress transaction warnings for in-memory database
                // CreditTransferService and other services use transactions with isolation levels
                // which are not supported by the in-memory provider. This allows tests to run.
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Add HttpContextAccessor for middleware (needed for database propagation)
            services.AddHttpContextAccessor();

            // JWT configuration removed - using cookie-based authentication

            // Add HttpContextAccessor for test authentication handler
            services.AddHttpContextAccessor();

            // For integration tests, add a test authentication handler that bypasses JWT validation
            // Use the consistent scheme name from TestAuthenticationHandler
            services.AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme, options => { });

            // Override the default authentication scheme for tests
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthenticationHandler.AuthenticationScheme;
            });

            // Disable model validation for testing to get better error details
            services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            // Disable file system watchers in development mode
            services.Configure<HostOptions>(options =>
            {
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            // Rate limiting services are now conditionally excluded from Program.cs for Testing environment
            // No additional rate limiting configuration needed in tests

            // Replace the real Azure Communication Services with a mock for testing
            // Remove the real email service registration
            var emailServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SkillLedger.Core.Interfaces.IEmailService));
            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }

            // Add the mock email service instead
            services.AddSingleton<SkillLedger.Core.Interfaces.IEmailService, SkillLedger.Tests.Mocks.MockEmailService>();
            services.AddSingleton<IFileStorageService, SkillLedger.Tests.Mocks.MockFileStorageService>();
            services.AddSingleton<ICacheService, SkillLedger.Tests.Mocks.MockCacheService>();
            services.AddSingleton<IAntiGamingService, SkillLedger.Tests.Mocks.MockAntiGamingService>();

            // Replace AuditLogService with a mock to prevent IMemoryCache dependency issues
            var auditServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SkillLedger.Core.Interfaces.IAuditLogService));
            if (auditServiceDescriptor != null)
            {
                services.Remove(auditServiceDescriptor);
            }
            services.AddScoped<SkillLedger.Core.Interfaces.IAuditLogService, SkillLedger.Tests.Mocks.MockAuditLogService>();

            // Add mock gaming detection services for testing
            services.AddSingleton<IGamingDetectionML, SkillLedger.Tests.Mocks.MockGamingDetectionML>();
            services.AddSingleton<IGraphDatabaseService, SkillLedger.Tests.Mocks.MockGraphDatabaseService>();

            // PERFORMANCE OPTIMIZATION: Replace real encryption with fast mock (Base64 encoding)
            // This dramatically speeds up tests - real encryption adds ~1-2s per test
            var encryptionServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(SkillLedger.Core.Interfaces.IEncryptionService));
            if (encryptionServiceDescriptor != null)
            {
                services.Remove(encryptionServiceDescriptor);
            }
            services.AddSingleton<SkillLedger.Core.Interfaces.IEncryptionService, SkillLedger.Tests.Mocks.MockEncryptionService>();

            // PERFORMANCE OPTIMIZATION: Use fast password hashing for tests (work factor 4 instead of 10)
            // This reduces user creation time from ~500ms to ~30ms per user
            services.Configure<Microsoft.AspNetCore.Identity.PasswordHasherOptions>(options =>
            {
                options.IterationCount = 4; // Minimum allowed, vs production's 10+ iterations
            });
        });
    }

    /// <summary>
    /// Gets the database name for the current context using AsyncLocal storage
    /// </summary>
    private static readonly System.Threading.AsyncLocal<string> CurrentDatabaseName = new();

    /// <summary>
    /// Thread-safe storage for database names keyed by managed thread ID
    /// Used as fallback when AsyncLocal value is not available
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, string> ThreadDatabaseNames = new();

    /// <summary>
    /// Sets the database name for the current test context
    /// </summary>
    public static void SetDatabaseNameForCurrentContext(string databaseName)
    {
        CurrentDatabaseName.Value = databaseName;
        // Also store by thread ID as fallback for contexts where AsyncLocal doesn't flow
        ThreadDatabaseNames[Environment.CurrentManagedThreadId] = databaseName;
    }

    /// <summary>
    /// Gets the database name for the current test context
    /// </summary>
    public static string GetDatabaseNameForCurrentContext()
    {
        // First try AsyncLocal
        if (CurrentDatabaseName.Value != null)
            return CurrentDatabaseName.Value;

        // Fall back to thread-specific storage
        if (ThreadDatabaseNames.TryGetValue(Environment.CurrentManagedThreadId, out var threadDbName))
            return threadDbName;

        // Last resort - generate unique name to prevent cross-contamination
        return $"{BaseDatabaseName}_Thread{Environment.CurrentManagedThreadId}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Clears the database name for the current thread (call during test cleanup)
    /// </summary>
    public static void ClearDatabaseNameForCurrentContext()
    {
        CurrentDatabaseName.Value = null!;
        ThreadDatabaseNames.TryRemove(Environment.CurrentManagedThreadId, out _);
    }

    /// <summary>
    /// Internal access to the base database name for the factory
    /// </summary>
    internal static string GetBaseDatabaseName() => BaseDatabaseName;

    /// <summary>
    /// Clean database state between tests while preserving schema
    /// Simple synchronous cleanup to prevent deadlocks
    /// </summary>
    public void CleanDatabase()
    {
        try
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

            // Remove all data but keep schema
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database cleanup warning: {ex.Message}");
            // Try alternative cleanup approach
            try
            {
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();
                context.Database.EnsureCreated();
            }
            catch
            {
                // Final fallback - ignore errors
            }
        }
    }

    /// <summary>
    /// Sets an instance-specific database name for this factory instance
    /// This provides isolation for separate factory instances in the same test
    /// </summary>
    public void SetInstanceDatabaseName(string databaseName)
    {
        _instanceDatabaseName = databaseName;
    }

    /// <summary>
    /// Clean up a specific database
    /// </summary>
    public static void CleanupDatabase(string databaseName)
    {
        lock (DatabaseLock)
        {
            if (ActiveDatabases.Contains(databaseName))
            {
                ActiveDatabases.Remove(databaseName);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            // Clean up database when factory is disposed
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();
            context.Database.EnsureDeleted();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database disposal warning: {ex.Message}");
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Enhanced middleware to propagate test database name through HTTP requests
/// CRITICAL FIX: This solves the AsyncLocal context isolation issue between test setup and HTTP API requests
/// </summary>
public class TestDatabasePropagationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TestDatabasePropagationMiddleware> _logger;

    public TestDatabasePropagationMiddleware(RequestDelegate next, ILogger<TestDatabasePropagationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // PRIORITY 1: Get database name from HTTP header (most reliable for cross-thread scenarios)
        string? databaseName = null;

        if (context.Request.Headers.TryGetValue("X-Test-Database", out var headerValue) &&
            !string.IsNullOrEmpty(headerValue.FirstOrDefault()))
        {
            databaseName = headerValue.FirstOrDefault();
        }
        // PRIORITY 2: Fall back to AsyncLocal/thread storage
        else
        {
            databaseName = SharedWebApplicationFactory.GetDatabaseNameForCurrentContext();
#if DEBUG
            if (!string.IsNullOrEmpty(databaseName))
            {
                _logger.LogDebug("Using database name '{DatabaseName}' from AsyncLocal for request {RequestId}",
                    databaseName, context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning("No database name found for request {RequestId}", context.TraceIdentifier);
            }
#endif
        }

        if (!string.IsNullOrEmpty(databaseName))
        {
            context.Items["TestDatabaseName"] = databaseName;
            // Also update AsyncLocal so services created during this request use the same database
            SharedWebApplicationFactory.SetDatabaseNameForCurrentContext(databaseName);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for middleware registration
/// </summary>
public static class TestDatabasePropagationMiddlewareExtensions
{
    public static IApplicationBuilder UseTestDatabasePropagation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TestDatabasePropagationMiddleware>();
    }

    public static IServiceCollection AddMiddleware<T>(this IServiceCollection services) where T : class
    {
        // Simple middleware registration for test infrastructure
        return services.AddTransient<T>();
    }
}

// NOTE: Collection definitions moved to TestCollections.cs for parallel test execution
// Each collection gets its own SharedTestHostFixture instance

/// <summary>
/// Interface for creating DbContext options with consistent database name resolution
/// </summary>
public interface IDbContextOptionsFactory<TDbContext> where TDbContext : DbContext
{
    void ConfigureDbContextOptions(IServiceProvider serviceProvider, DbContextOptionsBuilder<TDbContext> options);
}

/// <summary>
/// Factory for creating SkillLedgerDbContext options with consistent database name resolution
/// CRITICAL FIX: Ensures the same database name is used across all DbContext instances
/// </summary>
public class TestDbContextOptionsFactory : IDbContextOptionsFactory<SkillLedgerDbContext>
{
    public void ConfigureDbContextOptions(IServiceProvider serviceProvider, DbContextOptionsBuilder<SkillLedgerDbContext> options)
    {
        // CRITICAL FIX: Use a multi-tier approach to get the database name
        string databaseName;
        string databaseSource;

        // Tier 1: Try to get instance-specific database name from the factory that created this DbContext
        // This requires getting the factory instance from the service provider
        SharedWebApplicationFactory? factory = null;
        try
        {
            // Try to get the factory through service provider
            factory = serviceProvider.GetService<SharedWebApplicationFactory>();
        }
        catch
        {
            // If we can't get the factory, fall back to other methods
        }

        if (factory?._instanceDatabaseName != null)
        {
            databaseName = factory._instanceDatabaseName;
            databaseSource = "InstanceSpecific";
        }
        // Tier 2: Try HttpContext.Items (for HTTP requests)
        else if (serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext?.Items["TestDatabaseName"] is string httpDbName)
        {
            databaseName = httpDbName;
            databaseSource = "HttpContext.Items";
        }
        // Tier 3: Try AsyncLocal (for test setup)
        else
        {
            databaseName = SharedWebApplicationFactory.GetDatabaseNameForCurrentContext();
            databaseSource = "AsyncLocal";
        }

        // Debug logging for database creation
#if DEBUG
        var logger = serviceProvider.GetService<ILogger<TestDbContextOptionsFactory>>();
        logger?.LogDebug("Creating DbContext with database '{DatabaseName}' from {DatabaseSource}",
            databaseName, databaseSource);
#endif

        // Track the database for cleanup
        lock (SharedWebApplicationFactory.DatabaseLock)
        {
            SharedWebApplicationFactory.ActiveDatabases.Add(databaseName);
        }

        // Use the shared database root to ensure all contexts use the same in-memory database instance
        options.UseInMemoryDatabase(databaseName, SharedWebApplicationFactory.SharedInMemoryDatabaseRoot);
        options.EnableSensitiveDataLogging(false);
        options.EnableServiceProviderCaching(false);
        // CRITICAL: Suppress transaction warnings for in-memory database
        // CreditTransferService and other services use transactions with isolation levels
        // which are not supported by the in-memory provider. This allows tests to run.
        options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }
}

/// <summary>
/// Utility class for validating database context isolation in tests
/// </summary>
public static class DatabaseContextValidationHelper
{
    /// <summary>
    /// Validates that the current test context uses the expected database name
    /// </summary>
    public static bool ValidateDatabaseContext(IServiceProvider serviceProvider, string expectedDatabaseName)
    {
        // Get the current database name from the context
        var factory = serviceProvider.GetService<IDbContextOptionsFactory<SkillLedgerDbContext>>();
        if (factory == null)
        {
            return false;
        }

        // Check if the database name matches
        return SharedWebApplicationFactory.GetDatabaseNameForCurrentContext() == expectedDatabaseName;
    }

    /// <summary>
    /// Gets the actual database name being used by DbContext instances
    /// </summary>
    public static string GetActualDatabaseName(IServiceProvider serviceProvider)
    {
        // Try HttpContext first
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        if (httpContextAccessor?.HttpContext?.Items["TestDatabaseName"] is string httpDbName)
        {
            return httpDbName;
        }

        // Fall back to AsyncLocal
        return SharedWebApplicationFactory.GetDatabaseNameForCurrentContext();
    }
}

/// <summary>
/// Startup filter that adds the test database propagation middleware to the pipeline
/// This ensures the middleware runs for all HTTP requests during tests
/// </summary>
public class TestDatabaseMiddlewareStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // Add our middleware FIRST so it runs before any other middleware
            app.UseTestDatabasePropagation();
            next(app);
        };
    }
}

