using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock subscription service for testing billing workflows
/// Only implements methods needed for billing tests
/// Updates REAL database for integration testing
/// </summary>
public class MockSubscriptionService : ISubscriptionService
{
    private readonly SkillLedgerDbContext? _context;
    private readonly List<UserSubscription> _subscriptions = new();
    private readonly List<string> _operationLog = new();
    private bool _shouldSucceed = true;
    private string _errorMessage = "Operation failed";

    public List<string> OperationLog => _operationLog;
    public List<UserSubscription> Subscriptions => _subscriptions;

    public MockSubscriptionService(SkillLedgerDbContext? context = null)
    {
        _context = context;
    }

    public void SetupSuccess() => _shouldSucceed = true;
    public void SetupFailure(string errorMessage = "Operation failed")
    {
        _shouldSucceed = false;
        _errorMessage = errorMessage;
    }

    public void ClearOperationLog() => _operationLog.Clear();

    public async Task<UserSubscription> RenewSubscriptionAsync(Guid subscriptionId, string? createdFromIP = null)
    {
        _operationLog.Add($"RenewSubscription:{subscriptionId}");

        if (!_shouldSucceed)
            throw new InvalidOperationException(_errorMessage);

        // Write renewal outcome to REAL database so tests can verify actual DB state.
        if (_context != null)
        {
            var subscription = await _context.UserSubscriptions.FindAsync(subscriptionId);
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.NextBillingDate = subscription.NextBillingDate.HasValue
                    ? (subscription.IsAnnual
                        ? subscription.NextBillingDate.Value.AddYears(1)
                        : subscription.NextBillingDate.Value.AddMonths(1))
                    : DateTime.UtcNow.AddMonths(1);
                subscription.RetryCount = 0;
                subscription.NextRetryAt = null;
                subscription.BillingCycleCount++;
                subscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return subscription;
            }
        }

        return new UserSubscription { Id = subscriptionId, Status = SubscriptionStatus.Active };
    }

    public async Task<UserSubscription> ConvertTrialToPaidAsync(
        Guid userId,
        Guid paymentMethodId,
        string? createdFromIP = null)
    {
        _operationLog.Add($"ConvertTrialToPaid:{userId}:{paymentMethodId}");

        if (!_shouldSucceed)
            throw new InvalidOperationException(_errorMessage);

        // Write conversion outcome to REAL database so tests can verify actual DB state.
        if (_context != null)
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Trial);
            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.TrialEndDate = DateTime.UtcNow;
                subscription.PaymentMethodId = paymentMethodId;
                subscription.NextBillingDate = DateTime.UtcNow.AddMonths(1);
                subscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return subscription;
            }
        }

        return new UserSubscription
        {
            UserId = userId,
            PaymentMethodId = paymentMethodId,
            Status = SubscriptionStatus.Active
        };
    }

    public async Task<UserSubscription> CancelSubscriptionAsync(
        Guid userId,
        string? reason = null,
        bool immediate = false,
        string? createdFromIP = null)
    {
        _operationLog.Add($"CancelSubscription:{userId}:{reason}");

        if (!_shouldSucceed)
            throw new InvalidOperationException(_errorMessage);

        // Update REAL database for integration testing
        if (_context != null)
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);

            if (subscription != null)
            {
                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.CancelledAt = DateTime.UtcNow;
                subscription.CancellationReason = reason;
                subscription.EndDate = immediate ? DateTime.UtcNow : null;
                subscription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return subscription;
            }
        }

        // Fallback for tests without context
        return new UserSubscription
        {
            UserId = userId,
            Status = SubscriptionStatus.Cancelled,
            CancelledAt = DateTime.UtcNow,
            CancellationReason = reason,
            EndDate = immediate ? DateTime.UtcNow : null
        };
    }

    // Implement other interface methods as stubs (not needed for billing tests)
    public Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        Guid paymentMethodId,
        bool isTrial = false,
        bool isAnnual = false,
        string? createdFromIP = null)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        string? stripeSubscriptionId,
        string? stripeCustomerId)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription> CreateSubscriptionAsync(
        Guid userId,
        Guid subscriptionTierId,
        string? stripeSubscriptionId,
        string? stripeCustomerId,
        SubscriptionPromotionInfo? promotionInfo)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task RecordPaymentAsync(string stripeSubscriptionId, long amountPaid)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription?> GetUserActiveSubscriptionAsync(Guid userId)
    {
        return Task.FromResult(_subscriptions.FirstOrDefault(s =>
            s.UserId == userId &&
            s.Status == SubscriptionStatus.Active));
    }

    public Task<UserSubscription?> GetSubscriptionByExternalIdAsync(string externalSubscriptionId)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<(List<UserSubscription> subscriptions, int totalCount)> GetUserSubscriptionsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<List<SubscriptionTier>> GetSubscriptionTiersAsync()
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<SubscriptionTier?> GetSubscriptionTierAsync(Guid tierId)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription> UpgradeSubscriptionAsync(
        Guid userId,
        Guid newTierId,
        bool immediateCharge = true,
        string? createdFromIP = null)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription> DowngradeSubscriptionAsync(
        Guid userId,
        Guid newTierId,
        DateTime? effectiveDate = null,
        string? createdFromIP = null)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription> PauseSubscriptionAsync(
        Guid userId,
        TimeSpan pauseDuration,
        string? createdFromIP = null)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserSubscription> ResumeSubscriptionAsync(Guid userId, string? createdFromIP = null)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<bool> HasFeatureAccessAsync(Guid userId, string feature)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<SubscriptionLimitsDto> GetUserSubscriptionLimitsAsync(Guid userId)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<SubscriptionStatisticsDto> GetSubscriptionStatisticsAsync(
        DateTime startDate,
        DateTime endDate)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }

    public Task<UserUsageStatisticsDto> GetUserUsageStatisticsAsync(Guid userId)
    {
        throw new NotImplementedException("Use MockSubscriptionService setup methods for testing");
    }
}
