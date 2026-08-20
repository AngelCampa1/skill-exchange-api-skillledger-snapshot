using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Infrastructure.Services;
using System.Threading.RateLimiting;

namespace SkillLedger.Api.Configuration;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");
        var isEnabled = rateLimitConfig.GetValue<bool>("Enabled", true);
        var registrationPerHour = rateLimitConfig.GetValue<int>("RegistrationPerHour", 5);
        var loginAttemptsPerMinute = rateLimitConfig.GetValue<int>("LoginAttemptsPerMinute", 5);
        var generalApiPerMinute = rateLimitConfig.GetValue<int>("GeneralApiPerMinute", 100);

        // Development multiplier: when limits are high (>100), apply them across all policies
        var isDevelopmentMode = registrationPerHour >= 100;
        var devMultiplier = isDevelopmentMode ? 100 : 1;

        services.AddRateLimiter(options =>
        {
            // Registration rate limiting: configurable attempts per hour per IP
            options.AddPolicy("RegistrationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = registrationPerHour,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for security endpoints
                    }));

            // Phone verification: 3 attempts per hour per IP (scaled in dev)
            options.AddPolicy("PhoneVerificationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Login attempts: 10 attempts per 15 minutes per IP (scaled in dev)
            options.AddPolicy("LoginPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10 * devMultiplier,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Token refresh: 20 attempts per 5 minutes per IP (more frequent than login)
            options.AddPolicy("TokenRefreshPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2 // Allow small queue for legitimate token refreshes
                    }));

            // Password reset request: 3 attempts per hour per IP (scaled in dev)
            options.AddPolicy("PasswordResetPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for security-sensitive endpoints
                    }));

            // Profile creation: 2 attempts per hour per IP (scaled in dev)
            options.AddPolicy("ProfileCreationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Profile updates: 10 attempts per hour per IP
            options.AddPolicy("ProfileUpdatePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Profile deletion: 1 attempt per hour per IP (scaled in dev)
            options.AddPolicy("ProfileDeletionPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, 1 * devMultiplier),
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Public profile search: 50 attempts per minute per IP
            options.AddPolicy("PublicProfileSearchPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Tax compliance setup: 2 attempts per hour per IP (scaled in dev)
            options.AddPolicy("TaxComplianceSetupPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for sensitive operations
                    }));

            // Tax information updates: 5 attempts per hour per IP
            options.AddPolicy("TaxComplianceUpdatePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Tax document generation: 10 attempts per hour per IP
            options.AddPolicy("TaxDocumentGenerationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Tax document downloads: 20 attempts per hour per IP
            options.AddPolicy("TaxDocumentDownloadPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Workspace management: 30 attempts per hour per IP (for workspace operations)
            options.AddPolicy("WorkspacePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3
                    }));

            // Project creation: 10 attempts per hour per IP (reasonable for project creation)
            options.AddPolicy("ProjectCreationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Project updates: 20 attempts per hour per IP (more frequent than creation)
            options.AddPolicy("ProjectUpdatePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Project publishing: 5 attempts per hour per IP (scaled in dev)
            options.AddPolicy("ProjectPublishPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Project deletion: 3 attempts per hour per IP (scaled in dev)
            options.AddPolicy("ProjectDeletionPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Project application submission: 15 attempts per hour per IP (reasonable for applications)
            options.AddPolicy("ProjectApplicationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3
                    }));

            // Application status updates: 30 attempts per hour per IP (for clients reviewing applications)
            options.AddPolicy("ProjectApplicationStatusUpdatePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Application withdrawal: 10 attempts per hour per IP
            options.AddPolicy("ProjectApplicationWithdrawPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Provider selection creation: 5 attempts per hour per IP (scaled in dev)
            options.AddPolicy("ProviderSelectionPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for critical decisions
                    }));

            // Provider selection updates: 10 attempts per hour per IP (status, escrow, contract updates)
            options.AddPolicy("ProviderSelectionUpdatePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Administrative operations: 20 attempts per hour per IP (for admin actions)
            options.AddPolicy("AdminOperationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for admin operations
                    }));

            // Content moderation: 30 attempts per hour per IP (for moderators)
            options.AddPolicy("ModerationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Credit wallet general operations: 100 attempts per hour per IP (balance checks, history)
            options.AddPolicy("WalletPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Credit transfers: 10 attempts per hour per IP (scaled in dev)
            options.AddPolicy("TransferPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for financial operations
                    }));

            // Batch credit transfers: 3 attempts per hour per IP (scaled in dev)
            options.AddPolicy("BatchTransferPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for bulk financial operations
                    }));

            // Credit transfer reversals: 5 attempts per hour per IP (scaled in dev)
            options.AddPolicy("ReversalPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for reversal operations
                    }));

            // Escrow operations: 5 attempts per hour per IP (scaled in dev)
            options.AddPolicy("EscrowPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for escrow operations
                    }));

            // CRITICAL: Milestone payment release: 3 attempts per 5 minutes per IP
            // Extremely restrictive to prevent double payment vulnerabilities
            options.AddPolicy("MilestonePaymentPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // CRITICAL: No queueing for payment releases
                    }));

            // Milestone state changes: 10 state changes per minute per IP
            // Protects against abuse of milestone approval/rejection workflows
            options.AddPolicy("MilestoneStateChangePolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2 // Allow minimal queueing for legitimate workflows
                    }));

            // Skill management operations: 20 operations per minute per IP
            // Balances normal usage with protection against bulk operations
            options.AddPolicy("SkillManagementPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5 // Allow some queueing for batch skill operations
                    }));

            // Real-time messaging rate limiting: 60 messages per minute per user
            options.AddPolicy("MessagingPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10 // Allow some queueing for messaging
                    }));

            // File upload rate limiting: 10 uploads per hour per IP (to prevent abuse)
            options.AddPolicy("FileUploadPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for file uploads to prevent storage abuse
                    }));

            // File download rate limiting: 50 downloads per hour per IP (more generous for downloads)
            options.AddPolicy("FileDownloadPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5 // Allow small queue for legitimate downloads
                    }));

            // General API rate limiting: configurable requests per minute per IP
            options.AddPolicy("GeneralApiPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = generalApiPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Default policy for general endpoints: 200 requests per minute per IP
            // Used by ProfileController, FinancialReportingController, and other general endpoints
            options.AddPolicy("DefaultPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Global rate limiting fallback
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1000,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Review submission: 5 reviews per hour per IP (scaled in dev)
            options.AddPolicy("ReviewSubmissionPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for review operations
                    }));

            // Review actions (retract, respond, flag): 20 attempts per hour per IP
            options.AddPolicy("ReviewActionPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    }));

            // Skill endorsement: 10 endorsements per hour per IP (prevent endorsement spam)
            options.AddPolicy("SkillEndorsementPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for endorsement operations
                    }));

            // Subscription tiers viewing: 100 requests per hour per IP (public information)
            options.AddPolicy("SubscriptionPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Subscription creation/upgrades: 10 attempts per hour per IP (financial operation)
            options.AddPolicy("SubscriptionModificationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for subscription modifications
                    }));

            // Subscription cancellation: 5 attempts per hour per IP (scaled in dev)
            options.AddPolicy("SubscriptionCancellationPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for cancellations
                    }));

            // Checkout operations: 10 attempts per hour per IP (financial operation)
            options.AddPolicy("CheckoutPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for checkout operations
                    }));

            // Payment operations: 20 attempts per hour per IP (financial operation)
            options.AddPolicy("PaymentPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Webhooks: narrow queue for legitimate third-party retries
            options.AddPolicy("WebhookPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Admin controllers: strict per-IP throttling
            options.AddPolicy("AdminPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Security admin endpoints: strict per-IP throttling
            options.AddPolicy("SecurityPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Role management endpoints: strict per-IP throttling
            options.AddPolicy("RoleManagementPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Export endpoints: restrict expensive data extraction
            options.AddPolicy("ExportPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Document preview rendering: prevent expensive preview abuse
            options.AddPolicy("DocumentPreviewPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3
                    }));

            // SECURITY FIX: Project search rate limiting: 30 searches per minute per IP
            // Prevents data scraping, enumeration attacks, and DoS via expensive queries
            options.AddPolicy("ProjectSearchPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5 // Allow small queue for legitimate searches
                    }));

            // SECURITY FIX: Document search rate limiting: 40 searches per minute per IP
            // Prevents workspace document enumeration and information disclosure
            options.AddPolicy("DocumentSearchPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 40,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 8 // Allow slightly higher queue for authenticated users
                    }));

            // BUG-BE-005 FIX: Message search rate limiting: 30 searches per minute per IP
            // Prevents message content scraping, privacy violations, and DoS attacks
            options.AddPolicy("MessageSearchPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5 // Allow small queue for legitimate searches
                    }));

            // Feedback submission: 5 attempts per hour per IP (prevent spam while allowing legitimate feedback)
            options.AddPolicy("FeedbackLimit", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientIpAddress(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5 * devMultiplier,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing for feedback to prevent spam
                    }));

            // Customize rejection response
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter.TotalSeconds
                        : (double?)null
                };

                await context.HttpContext.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(response), token);
            };
        });

        return services;
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        return TrustedClientIpResolver.GetClientIpAddress(context, "unknown");
    }
}
