using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Admin subscription management API controller
/// Handles subscription administration, billing operations, and analytics
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("AdminPolicy")]
public class SubscriptionAdminController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISubscriptionBillingService _billingService;
    private readonly SkillLedgerDbContext _context;
    private readonly ILogger<SubscriptionAdminController> _logger;

    public SubscriptionAdminController(
        ISubscriptionService subscriptionService,
        ISubscriptionBillingService billingService,
        SkillLedgerDbContext context,
        ILogger<SubscriptionAdminController> logger)
    {
        _subscriptionService = subscriptionService;
        _billingService = billingService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get subscription statistics
    /// </summary>
    /// <param name="startDate">Start date for statistics</param>
    /// <param name="endDate">End date for statistics</param>
    /// <returns>Subscription statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(SubscriptionStatisticsResponseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SubscriptionStatisticsResponseDto>> GetSubscriptionStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (start >= end)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var statistics = await _subscriptionService.GetSubscriptionStatisticsAsync(start, end);

            var statisticsDto = new SubscriptionStatisticsResponseDto
            {
                TotalSubscriptions = statistics.TotalSubscriptions,
                ActiveSubscriptions = statistics.ActiveSubscriptions,
                TrialSubscriptions = statistics.TrialSubscriptions,
                CancelledSubscriptions = statistics.CancelledSubscriptions,
                ExpiredSubscriptions = statistics.ExpiredSubscriptions,
                MonthlyRecurringRevenue = statistics.MonthlyRecurringRevenue,
                AnnualRecurringRevenue = statistics.AnnualRecurringRevenue,
                NewSubscriptionsThisPeriod = statistics.NewSubscriptionsThisPeriod,
                ChurnedSubscriptionsThisPeriod = statistics.ChurnedSubscriptionsThisPeriod,
                SubscriptionsByTier = statistics.SubscriptionsByTier,
                SubscriptionsByStatus = statistics.SubscriptionsByStatus
            };

            return Ok(statisticsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get billing analytics
    /// </summary>
    /// <param name="startDate">Start date for analytics</param>
    /// <param name="endDate">End date for analytics</param>
    /// <returns>Billing analytics</returns>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(BillingAnalyticsDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BillingAnalyticsDto>> GetBillingAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            if (start >= end)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            // Get transactions for the period
            var transactions = await _context.SubscriptionTransactions
                .Where(t => t.CreatedAt >= start && t.CreatedAt <= end)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            // Calculate analytics
            var grossRevenue = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var refunds = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
            var netRevenue = grossRevenue - refunds;

            var newSubscriptions = transactions.Count(t => t.Type == SubscriptionTransactionType.Purchase);
            var cancelledSubscriptions = transactions.Count(t => t.Type == SubscriptionTransactionType.Cancellation);
            var upgradedSubscriptions = transactions.Count(t => t.Type == SubscriptionTransactionType.Upgrade);
            var downgradedSubscriptions = transactions.Count(t => t.Type == SubscriptionTransactionType.Downgrade);

            // Group by day for daily revenue
            var dailyRevenue = transactions
                .Where(t => t.Status == TransactionStatus.Completed)
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new DailyRevenueDto
                {
                    Date = g.Key,
                    Revenue = g.Where(t => t.Amount > 0).Sum(t => t.Amount),
                    Transactions = g.Count(),
                    NewSubscriptions = g.Count(t => t.Type == SubscriptionTransactionType.Purchase),
                    CancelledSubscriptions = g.Count(t => t.Type == SubscriptionTransactionType.Cancellation)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var analytics = new BillingAnalyticsDto
            {
                PeriodStart = start,
                PeriodEnd = end,
                GrossRevenue = grossRevenue,
                NetRevenue = netRevenue,
                NewSubscriptions = newSubscriptions,
                CancelledSubscriptions = cancelledSubscriptions,
                UpgradedSubscriptions = upgradedSubscriptions,
                DowngradedSubscriptions = downgradedSubscriptions,
                AverageRevenuePerUser = netRevenue / Math.Max(newSubscriptions, 1),
                CustomerLifetimeValue = netRevenue / Math.Max(cancelledSubscriptions, 1),
                DailyRevenue = dailyRevenue
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving billing analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process due renewals manually
    /// </summary>
    /// <returns>Billing process result</returns>
    [HttpPost("process-renewals")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ProcessDueRenewals()
    {
        try
        {
            var result = await _billingService.ProcessDueRenewalsAsync();

            return Ok(new
            {
                message = "Renewals processed successfully",
                totalProcessed = result.TotalProcessed,
                successfulRenewals = result.SuccessfulRenewals,
                failedRenewals = result.FailedRenewals,
                totalRevenue = result.TotalRevenue,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing due renewals");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process expiring trials manually
    /// </summary>
    /// <returns>Trial conversion result</returns>
    [HttpPost("process-trials")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ProcessExpiringTrials()
    {
        try
        {
            var result = await _billingService.ProcessExpiringTrialsAsync();

            return Ok(new
            {
                message = "Trials processed successfully",
                trialsProcessed = result.TrialsProcessed,
                successfulConversions = result.SuccessfulConversions,
                failedConversions = result.FailedConversions,
                trialsCancelled = result.TrialsCancelled,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expiring trials");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process failed payment retries manually
    /// </summary>
    /// <param name="maxRetries">Maximum retry attempts</param>
    /// <returns>Retry result</returns>
    [HttpPost("process-retries")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ProcessFailedPaymentRetries([FromQuery] int maxRetries = 3)
    {
        try
        {
            var result = await _billingService.ProcessFailedPaymentRetriesAsync(maxRetries);

            return Ok(new
            {
                message = "Retries processed successfully",
                retriesAttempted = result.RetriesAttempted,
                successfulRetries = result.SuccessfulRetries,
                failedRetries = result.FailedRetries,
                subscriptionsCancelled = result.SubscriptionsCancelled,
                revenueRecovered = result.RevenueRecovered,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed payment retries");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Process past due cancellations manually
    /// </summary>
    /// <param name="gracePeriodDays">Grace period in days</param>
    /// <returns>Cancellation result</returns>
    [HttpPost("process-cancellations")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ProcessPastDueCancellations([FromQuery] int gracePeriodDays = 7)
    {
        try
        {
            var result = await _billingService.ProcessPastDueCancellationsAsync(gracePeriodDays);

            return Ok(new
            {
                message = "Cancellations processed successfully",
                subscriptionsCancelled = result.SubscriptionsCancelled,
                usersNotified = result.UsersNotified,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing past due cancellations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Send billing reminders manually
    /// </summary>
    /// <param name="daysBefore">Days before renewal</param>
    /// <returns>Reminder result</returns>
    [HttpPost("send-reminders")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> SendBillingReminders([FromQuery] int daysBefore = 3)
    {
        try
        {
            var result = await _billingService.SendBillingRemindersAsync(daysBefore);

            return Ok(new
            {
                message = "Reminders sent successfully",
                remindersSent = result.RemindersSent,
                usersNotified = result.UsersNotified,
                errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending billing reminders");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update subscription statistics manually
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("update-statistics")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> UpdateSubscriptionStatistics()
    {
        try
        {
            await _billingService.UpdateSubscriptionStatisticsAsync();

            return Ok(new { message = "Statistics updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate active subscriptions manually
    /// </summary>
    /// <returns>Validation result</returns>
    [HttpPost("validate-subscriptions")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> ValidateActiveSubscriptions()
    {
        try
        {
            var result = await _billingService.ValidateActiveSubscriptionsAsync();

            return Ok(new
            {
                message = "Validation completed",
                totalValidated = result.TotalValidated,
                validSubscriptions = result.ValidSubscriptions,
                invalidSubscriptions = result.InvalidSubscriptions,
                validationIssues = result.ValidationIssues,
                problematicSubscriptionIds = result.ProblematicSubscriptionIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating active subscriptions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all subscriptions (admin view)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="status">Filter by status</param>
    /// <param name="tierId">Filter by tier ID</param>
    /// <returns>Paginated list of subscriptions</returns>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult> GetAllSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        [FromQuery] Guid? tierId = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _context.UserSubscriptions
                .Include(us => us.SubscriptionTier)
                .Include(us => us.User)
                .Include(us => us.PaymentMethod)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(us => us.Status == (SubscriptionStatus)status.Value);
            }

            if (tierId.HasValue)
            {
                query = query.Where(us => us.SubscriptionTierId == tierId.Value);
            }

            var totalCount = await query.CountAsync();
            var subscriptions = await query
                .OrderByDescending(us => us.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var subscriptionDtos = subscriptions.Select(sub => new
            {
                sub.Id,
                sub.Status,
                sub.StartDate,
                sub.EndDate,
                sub.NextBillingDate,
                sub.TrialEndDate,
                sub.AutoRenew,
                sub.IsAnnual,
                sub.BillingCycleCount,
                sub.CreatedAt,
                sub.UpdatedAt,
                sub.CancelledAt,
                sub.CancellationReason,
                Tier = new
                {
                    sub.SubscriptionTier.Id,
                    sub.SubscriptionTier.Name,
                    sub.SubscriptionTier.Price,
                    sub.SubscriptionTier.CreditBonus
                },
                User = new
                {
                    sub.User.Id,
                    sub.User.Email,
                    FirstName = sub.User.FirstName,
                    LastName = sub.User.LastName
                },
                PaymentMethod = sub.PaymentMethod != null ? new
                {
                    sub.PaymentMethod.Id,
                    sub.PaymentMethod.Provider,
                    sub.PaymentMethod.Last4Digits,
                    sub.PaymentMethod.Brand,
                    sub.PaymentMethod.IsDefault
                } : null
            }).ToList();

            return Ok(new
            {
                subscriptions = subscriptionDtos,
                totalCount,
                currentPage = page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                hasNextPage = page * pageSize < totalCount,
                hasPreviousPage = page > 1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all subscriptions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}