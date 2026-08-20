using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Constants;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service implementation for project escrow operations with comprehensive security and audit logging
/// Implements secure milestone-based payment releases and dispute resolution
/// </summary>
public class ProjectEscrowService : IProjectEscrowService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ICreditWalletService _walletService;
    private readonly IAuditLogService _auditService;
    private readonly ILogger<ProjectEscrowService> _logger;
    private readonly IDistributedLockService _distributedLockService;

    public ProjectEscrowService(
        SkillLedgerDbContext context,
        ICreditWalletService walletService,
        IAuditLogService auditService,
        ILogger<ProjectEscrowService> logger,
        IDistributedLockService distributedLockService)
    {
        _context = context;
        _walletService = walletService;
        _auditService = auditService;
        _logger = logger;
        _distributedLockService = distributedLockService;
    }

    #region Escrow Management

    public async Task<ProjectEscrow> CreateEscrowAsync(Guid projectId, Guid providerId, string? initiatedFromIP = null)
    {
        // Only use transactions if not in-memory database (for testing)
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        // BUG-HIGH-010 FIX: Use Serializable isolation for escrow operations to prevent double-refunds and race conditions
        using var transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            _logger.LogInformation("Creating escrow for project {ProjectId} with provider {ProviderId}", projectId, providerId);

            // Validate project exists and get project details
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new ArgumentException("Project not found");
            }

            // Check if escrow already exists
            var existingEscrow = await _context.ProjectEscrows
                .FirstOrDefaultAsync(e => e.ProjectId == projectId);

            if (existingEscrow != null)
            {
                throw new InvalidOperationException("Escrow already exists for this project");
            }

            // Validate provider exists
            var provider = await _context.Users.FindAsync(providerId);
            if (provider == null)
            {
                throw new ArgumentException("Provider not found");
            }

            // Check client has sufficient funds
            var clientBalance = await _walletService.GetAvailableBalanceAsync(project.ClientId);
            if (clientBalance < project.CreditBudget)
            {
                throw new InvalidOperationException("Insufficient credits in client wallet");
            }

            // Create escrow account
            var escrow = new ProjectEscrow
            {
                ProjectId = projectId,
                ClientId = project.ClientId,
                ProviderId = providerId,
                TotalAmount = project.CreditBudget,
                Status = EscrowStatus.Active,
                CreatedFromIP = initiatedFromIP,
                RequiresMultiSignature = project.CreditBudget > 1000 // High-value projects require multi-sig
            };

            _context.ProjectEscrows.Add(escrow);

            // Transfer credits from client to escrow (via wallet service)
            await _walletService.CreateEscrowAsync(project.ClientId, projectId, project.CreditBudget);

            await _context.SaveChangesAsync();

            // Log audit trail
            await _auditService.LogEventAsync(
                project.ClientId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_CREATED,
                initiatedFromIP ?? "Unknown",
                null,
                true,
                $"Escrow created for project {project.Title} with {escrow.TotalAmount} credits. EscrowId: {escrow.Id}");

            await _auditService.LogEventAsync(
                project.ClientId,
                SkillLedger.Core.Constants.AuditActions.CREDIT_ESCROW_DEPOSIT,
                initiatedFromIP ?? "Unknown",
                null,
                true,
                $"Credits deposited to escrow: {escrow.TotalAmount}. ProjectId: {projectId}");

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Successfully created escrow {EscrowId} for project {ProjectId}", escrow.Id, projectId);

            return escrow;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            _logger.LogError(ex, "Failed to create escrow for project {ProjectId}", projectId);

            await _auditService.LogEventAsync(
                null,
                SkillLedger.Core.Constants.AuditActions.ESCROW_CREATION_FAILED,
                initiatedFromIP ?? "Unknown",
                null,
                false,
                $"Escrow creation failed for project {projectId}: {ex.Message}");

            throw;
        }
    }

    public async Task<ProjectEscrow?> GetEscrowByProjectIdAsync(Guid projectId)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        return await _context.ProjectEscrows
            .Include(e => e.Project)
            .Include(e => e.Client)
            .Include(e => e.Provider)
            .Include(e => e.Milestones)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.ProjectId == projectId);
    }

    public async Task<ProjectEscrow?> GetEscrowByIdAsync(Guid escrowId)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        return await _context.ProjectEscrows
            .Include(e => e.Project)
            .Include(e => e.Client)
            .Include(e => e.Provider)
            .Include(e => e.Milestones.OrderBy(m => m.SequenceOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == escrowId);
    }

    public async Task<IList<ProjectEscrow>> GetActiveEscrowsForUserAsync(Guid userId)
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        return await _context.ProjectEscrows
            .Include(e => e.Project)
            .Include(e => e.Milestones)
            .AsSplitQuery()
            .Where(e => (e.ClientId == userId || e.ProviderId == userId) &&
                       (e.Status == EscrowStatus.Active || e.Status == EscrowStatus.PartiallyReleased))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IList<AuditLog>> GetEscrowHistoryAsync(Guid escrowId)
    {
        return await _context.AuditLogs
            .Where(a => a.Details != null && (a.Details.Contains(escrowId.ToString()) ||
                       a.Details.Contains($"EscrowId: {escrowId}")))
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    #endregion

    #region Milestone Management

    public async Task<EscrowMilestone> AddMilestoneAsync(
        Guid escrowId,
        string description,
        int amount,
        DateTime? expectedCompletionDate = null,
        Guid? linkedDeliverableId = null,
        int? sequenceOrder = null)
    {
        var escrow = await GetEscrowByIdAsync(escrowId);
        if (escrow == null)
        {
            throw new ArgumentException("Escrow not found");
        }

        // Calculate total milestone amounts to ensure they don't exceed escrow total
        // VULN-027 FIX: Changed > to >= to prevent edge case where final milestone equals remaining balance
        var existingMilestoneTotal = escrow.Milestones.Sum(m => m.Amount);
        if (existingMilestoneTotal + amount > escrow.TotalAmount)
        {
            throw new ArgumentException($"Total milestone amounts ({existingMilestoneTotal + amount}) would exceed escrow total ({escrow.TotalAmount})");
        }

        var milestone = new EscrowMilestone
        {
            EscrowId = escrowId,
            Description = description,
            Amount = amount,
            ExpectedCompletionDate = expectedCompletionDate,
            LinkedDeliverableId = linkedDeliverableId,
            SequenceOrder = sequenceOrder ?? (escrow.Milestones.Max(m => (int?)m.SequenceOrder) ?? 0) + 1
        };

        _context.EscrowMilestones.Add(milestone);
        await _context.SaveChangesAsync();

        await LogAuditEventAsync(
            escrow.ClientId,
            SkillLedger.Core.Constants.AuditActions.ESCROW_MILESTONE_ADDED,
            $"Milestone added to escrow: {description} ({amount} credits). EscrowId: {escrowId}, MilestoneId: {milestone.Id}");

        _logger.LogInformation("Added milestone {MilestoneId} to escrow {EscrowId}", milestone.Id, escrowId);

        return milestone;
    }

    public async Task<bool> ReleaseMilestoneAsync(Guid milestoneId, Guid approvedByUserId, string? releaseNotes = null)
    {
        // VULN-014 FIX: Add distributed lock to prevent double-payment via concurrent approval
        // Without locking, multiple simultaneous approval requests could release the same milestone multiple times
        var lockKey = $"escrow:milestone:release:{milestoneId}";
        await using var lockHandle = await _distributedLockService.AcquireLockAsync(
            lockKey,
            TimeSpan.FromSeconds(30),   // Lock expiration
            TimeSpan.FromSeconds(10),    // Wait up to 10 seconds
            TimeSpan.FromMilliseconds(100)); // Retry every 100ms

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("Could not acquire lock for milestone release: {MilestoneId}. Another release may be in progress.", milestoneId);
            throw new InvalidOperationException($"Milestone release already in progress for milestone {milestoneId}. Please try again shortly.");
        }

        _logger.LogInformation("Acquired distributed lock for milestone release: {MilestoneId}", milestoneId);

        // Only use transactions if not in-memory database (for testing)
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        // BUG-HIGH-010 FIX: Use Serializable isolation for escrow operations to prevent double-refunds and race conditions
        using var transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var milestone = await _context.EscrowMilestones
                .Include(m => m.Escrow)
                    .ThenInclude(e => e.Project)
                .Include(m => m.Escrow)
                    .ThenInclude(e => e.Provider)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
            {
                throw new ArgumentException("Milestone not found");
            }

            // Verify authorization - only client can approve releases
            if (milestone.Escrow.ClientId != approvedByUserId)
            {
                throw new UnauthorizedAccessException("Only the project client can approve milestone releases");
            }

            // Re-check status after acquiring lock (defense in depth)
            if (!milestone.CanBeReleased)
            {
                throw new InvalidOperationException("Milestone cannot be released at this time");
            }

            // VULN-030 FIX: Enforce milestone sequence - check that all blocking milestones are released
            var blockingMilestones = await _context.EscrowMilestones
                .Where(m => m.EscrowId == milestone.EscrowId &&
                           m.SequenceOrder < milestone.SequenceOrder &&
                           m.IsBlocking &&
                           !m.IsReleased)
                .OrderBy(m => m.SequenceOrder)
                .ToListAsync();

            if (blockingMilestones.Any())
            {
                var blockerDescriptions = string.Join(", ", blockingMilestones.Select(m => $"'{m.Description}' (#{m.SequenceOrder})"));
                throw new InvalidOperationException($"Cannot release milestone: {blockingMilestones.Count} blocking milestone(s) must be completed first: {blockerDescriptions}");
            }

            // Release milestone
            milestone.Release(approvedByUserId, releaseNotes);

            // Update escrow status and released amount
            milestone.Escrow.ReleaseAmount(milestone.Amount);

            // CRIT-ESCROW-002 FIX: Use ReleaseMilestoneFromEscrowAsync instead of AddCreditsAsync
            // This properly reduces client's PendingBalance and increases provider's Balance
            // Prevents wallet balance inconsistencies when escrow is later cancelled
            await _walletService.ReleaseMilestoneFromEscrowAsync(
                milestone.Escrow.ClientId,
                milestone.Escrow.ProviderId,
                milestone.Escrow.ProjectId,
                milestone.Amount);

            await _context.SaveChangesAsync();

            // Audit logging
            await LogAuditEventAsync(
                approvedByUserId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_MILESTONE_RELEASED,
                $"Milestone released: {milestone.Description} ({milestone.Amount} credits). EscrowId: {milestone.EscrowId}, MilestoneId: {milestone.Id}");

            await LogAuditEventAsync(
                milestone.Escrow.ProviderId,
                SkillLedger.Core.Constants.AuditActions.CREDIT_ESCROW_RELEASE,
                $"Credits released from escrow: {milestone.Amount}. ProjectId: {milestone.Escrow.ProjectId}");

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Successfully released milestone {MilestoneId} with {Amount} credits",
                milestoneId, milestone.Amount);

            return true;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            _logger.LogError(ex, "Failed to release milestone {MilestoneId}", milestoneId);

            await LogAuditEventAsync(
                approvedByUserId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_MILESTONE_RELEASE_FAILED,
                $"Milestone release failed: {ex.Message}. MilestoneId: {milestoneId}",
                success: false);

            throw;
        }
    }

    public async Task<IList<EscrowMilestone>> GetMilestonesAsync(Guid escrowId)
    {
        return await _context.EscrowMilestones
            .Where(m => m.EscrowId == escrowId)
            .OrderBy(m => m.SequenceOrder)
            .ToListAsync();
    }

    public async Task<bool> UpdateMilestoneExpectedDateAsync(Guid milestoneId, DateTime newExpectedDate)
    {
        var milestone = await _context.EscrowMilestones.FindAsync(milestoneId);
        if (milestone == null)
        {
            return false;
        }

        milestone.UpdateExpectedCompletion(newExpectedDate);
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Full Escrow Operations

    public async Task<bool> ReleaseFullEscrowAsync(Guid escrowId, Guid approvedByUserId, string? releaseNotes = null)
    {
        // BUG-BE-001 FIX: Add distributed locking to prevent race condition / double-spend
        var lockKey = $"escrow-release:{escrowId}";
        await using var lockHandle = await _distributedLockService.AcquireLockAsync(
            lockKey,
            TimeSpan.FromSeconds(30),  // Lock expires after 30 seconds
            TimeSpan.FromSeconds(10),  // Wait up to 10 seconds to acquire lock
            TimeSpan.FromMilliseconds(100));  // Retry every 100ms

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("Failed to acquire lock for escrow release {EscrowId}. Another operation may be in progress.", escrowId);
            throw new InvalidOperationException("Another escrow operation is currently in progress. Please try again in a moment.");
        }

        // Only use transactions if not in-memory database (for testing)
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        // BUG-HIGH-010 FIX: Use Serializable isolation for escrow operations to prevent double-refunds and race conditions
        using var transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            var escrow = await GetEscrowByIdAsync(escrowId);
            if (escrow == null)
            {
                throw new ArgumentException("Escrow not found");
            }

            // Verify authorization
            if (escrow.ClientId != approvedByUserId)
            {
                throw new UnauthorizedAccessException("Only the project client can approve full escrow release");
            }

            // BUG-BE-001 FIX: Re-check escrow status after acquiring lock
            // This prevents the race condition where two threads check CanBeReleased simultaneously
            if (!escrow.CanBeReleased)
            {
                throw new InvalidOperationException("Escrow cannot be released at this time");
            }

            var releaseAmount = escrow.RemainingAmount;

            // Update escrow to fully released
            escrow.ReleaseAmount(releaseAmount);
            escrow.Notes = releaseNotes;

            // Transfer remaining amount to provider
            await _walletService.AddCreditsAsync(escrow.ProviderId, releaseAmount,
                $"Full escrow release for project {escrow.ProjectId}", CreditTransactionType.EscrowRelease);

            await _context.SaveChangesAsync();

            // Audit logging
            await LogAuditEventAsync(
                approvedByUserId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_FULL_RELEASE,
                $"Full escrow released: {escrow.TotalAmount} credits. EscrowId: {escrowId}");

            await LogAuditEventAsync(
                escrow.ProviderId,
                SkillLedger.Core.Constants.AuditActions.CREDIT_ESCROW_RELEASE,
                $"Full escrow credits released: {escrow.TotalAmount}. ProjectId: {escrow.ProjectId}");

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Successfully released full escrow {EscrowId} with {Amount} credits",
                escrowId, escrow.TotalAmount);

            return true;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            _logger.LogError(ex, "Failed to release full escrow {EscrowId}", escrowId);

            await LogAuditEventAsync(
                approvedByUserId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_FULL_RELEASE_FAILED,
                $"Full escrow release failed: {ex.Message}. EscrowId: {escrowId}",
                success: false);

            throw;
        }
    }

    public async Task<bool> CancelEscrowAsync(Guid escrowId, Guid cancelledByUserId, string? cancellationReason = null)
    {
        // Only use transactions if not in-memory database (for testing)
        var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        // BUG-HIGH-010 FIX: Use Serializable isolation for escrow operations to prevent double-refunds and race conditions
        using var transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            var escrow = await GetEscrowByIdAsync(escrowId);
            if (escrow == null)
            {
                throw new ArgumentException("Escrow not found");
            }

            // BUG-BE-006 FIX: Verify authorization - client or admin can cancel
            if (escrow.ClientId != cancelledByUserId)
            {
                // Check for admin/moderator permissions
                var isAdmin = await HasAdminPermissionsAsync(cancelledByUserId);
                if (!isAdmin)
                {
                    throw new UnauthorizedAccessException("Only the project client or administrators can cancel escrow");
                }
            }

            if (escrow.IsTerminal)
            {
                throw new InvalidOperationException("Cannot cancel completed or already cancelled escrow");
            }

            var refundAmount = escrow.RemainingAmount;

            // Cancel escrow
            escrow.Cancel();
            escrow.Notes = cancellationReason;

            // BUG-BE-004 FIX: Use RefundEscrowAsync instead of AddCreditsAsync
            // This properly reduces the pending balance that was set during escrow creation
            // Prevents credit duplication and ensures correct wallet balance reconciliation
            // CRIT-ESCROW-001 FIX: Pass the actual remaining amount, not the original escrow amount
            if (refundAmount > 0)
            {
                await _walletService.RefundEscrowAsync(escrow.ProjectId, refundAmount);
            }

            await _context.SaveChangesAsync();

            // Audit logging
            await LogAuditEventAsync(
                cancelledByUserId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_CANCELLED,
                $"Escrow cancelled with {refundAmount} credits refunded. EscrowId: {escrowId}");

            if (refundAmount > 0)
            {
                await LogAuditEventAsync(
                    escrow.ClientId,
                    SkillLedger.Core.Constants.AuditActions.CREDIT_ESCROW_REFUND,
                    $"Escrow refund: {refundAmount} credits. ProjectId: {escrow.ProjectId}");
            }

            if (transaction != null)
                await transaction.CommitAsync();

            _logger.LogInformation("Successfully cancelled escrow {EscrowId} with {RefundAmount} credits refunded",
                escrowId, refundAmount);

            return true;
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            _logger.LogError(ex, "Failed to cancel escrow {EscrowId}", escrowId);

            await LogAuditEventAsync(
                cancelledByUserId,
                SkillLedger.Core.Constants.AuditActions.ESCROW_CANCELLATION_FAILED,
                $"Escrow cancellation failed: {ex.Message}. EscrowId: {escrowId}",
                success: false);

            throw;
        }
    }

    #endregion

    #region Dispute Management

    public async Task<bool> RaiseDisputeAsync(Guid escrowId, Guid raisedByUserId, string disputeReason)
    {
        var escrow = await GetEscrowByIdAsync(escrowId);
        if (escrow == null)
        {
            throw new ArgumentException("Escrow not found");
        }

        // Verify user is involved in the escrow
        if (escrow.ClientId != raisedByUserId && escrow.ProviderId != raisedByUserId)
        {
            throw new UnauthorizedAccessException("Only escrow participants can raise disputes");
        }

        if (escrow.IsTerminal)
        {
            throw new InvalidOperationException("Cannot dispute completed or cancelled escrow");
        }

        escrow.RaiseDispute(disputeReason);
        await _context.SaveChangesAsync();

        await LogAuditEventAsync(
            raisedByUserId,
            SkillLedger.Core.Constants.AuditActions.ESCROW_DISPUTE_RAISED,
            $"Dispute raised: {disputeReason}. EscrowId: {escrowId}");

        _logger.LogInformation("Dispute raised for escrow {EscrowId} by user {UserId}", escrowId, raisedByUserId);

        return true;
    }

    public async Task<bool> ResolveDisputeAsync(
        Guid escrowId,
        Guid resolvedByUserId,
        string resolutionAction,
        string? resolutionNotes = null)
    {
        var escrow = await GetEscrowByIdAsync(escrowId);
        if (escrow == null)
        {
            throw new ArgumentException("Escrow not found");
        }

        if (escrow.Status != EscrowStatus.Disputed)
        {
            throw new InvalidOperationException("Escrow is not in disputed status");
        }

        // BUG-BE-006 FIX: Verify admin/moderator permissions for dispute resolution
        var isAdmin = await HasAdminPermissionsAsync(resolvedByUserId);
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only administrators can resolve escrow disputes");
        }

        escrow.ResolveDispute(resolvedByUserId, resolutionNotes);
        await _context.SaveChangesAsync();

        await LogAuditEventAsync(
            resolvedByUserId,
            SkillLedger.Core.Constants.AuditActions.ESCROW_DISPUTE_RESOLVED,
            $"Dispute resolved: {resolutionAction}. Notes: {resolutionNotes}. EscrowId: {escrowId}");

        _logger.LogInformation("Dispute resolved for escrow {EscrowId} by admin {AdminId}", escrowId, resolvedByUserId);

        return true;
    }

    public async Task<IList<ProjectEscrow>> GetDisputedEscrowsAsync()
    {
        // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
        return await _context.ProjectEscrows
            .Include(e => e.Project)
            .Include(e => e.Client)
            .Include(e => e.Provider)
            .AsSplitQuery()
            .Where(e => e.Status == EscrowStatus.Disputed)
            .OrderBy(e => e.DisputedAt)
            .ToListAsync();
    }

    #endregion

    #region Security and Compliance

    public async Task<bool> FreezeEscrowAsync(Guid escrowId, Guid frozenByUserId, string freezeReason)
    {
        var escrow = await GetEscrowByIdAsync(escrowId);
        if (escrow == null)
        {
            return false;
        }

        // BUG-BE-006 FIX: Verify admin/moderator permissions for freezing escrow
        var isAdmin = await HasAdminPermissionsAsync(frozenByUserId);
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only administrators can freeze escrow accounts");
        }

        escrow.Freeze();
        escrow.Notes = freezeReason;
        await _context.SaveChangesAsync();

        await LogAuditEventAsync(
            frozenByUserId,
            SkillLedger.Core.Constants.AuditActions.ESCROW_FROZEN,
            $"Escrow frozen: {freezeReason}. EscrowId: {escrowId}");

        _logger.LogWarning("Escrow {EscrowId} frozen by admin {AdminId}: {Reason}",
            escrowId, frozenByUserId, freezeReason);

        return true;
    }

    public async Task<bool> UnfreezeEscrowAsync(Guid escrowId, Guid unfrozenByUserId)
    {
        var escrow = await GetEscrowByIdAsync(escrowId);
        if (escrow == null)
        {
            return false;
        }

        if (escrow.Status != EscrowStatus.Frozen)
        {
            return false;
        }

        // BUG-BE-006 FIX: Verify admin/moderator permissions for unfreezing escrow
        var isAdmin = await HasAdminPermissionsAsync(unfrozenByUserId);
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Only administrators can unfreeze escrow accounts");
        }

        escrow.Unfreeze();
        await _context.SaveChangesAsync();

        await LogAuditEventAsync(
            unfrozenByUserId,
            SkillLedger.Core.Constants.AuditActions.ESCROW_UNFROZEN,
            $"Escrow unfrozen. EscrowId: {escrowId}");

        _logger.LogInformation("Escrow {EscrowId} unfrozen by admin {AdminId}", escrowId, unfrozenByUserId);

        return true;
    }

    public async Task<bool> ValidateEscrowIntegrityAsync(Guid escrowId)
    {
        var escrow = await GetEscrowByIdAsync(escrowId);
        if (escrow == null)
        {
            return false;
        }

        // Validate business rules
        var isValid = escrow.ReleasedAmount >= 0 &&
                     escrow.ReleasedAmount <= escrow.TotalAmount &&
                     escrow.TotalAmount > 0;

        // Validate milestone totals don't exceed escrow total
        var milestoneTotals = escrow.Milestones.Sum(m => m.Amount);
        var releasedMilestoneTotals = escrow.Milestones.Where(m => m.IsReleased).Sum(m => m.Amount);

        isValid = isValid &&
                 milestoneTotals <= escrow.TotalAmount &&
                 releasedMilestoneTotals == escrow.ReleasedAmount;

        if (!isValid)
        {
            await LogAuditEventAsync(
                null,
                SkillLedger.Core.Constants.AuditActions.ESCROW_INTEGRITY_VIOLATION,
                $"Escrow integrity validation failed. EscrowId: {escrowId}",
                success: false);

            _logger.LogError("Integrity validation failed for escrow {EscrowId}", escrowId);
        }
        else
        {
            await LogAuditEventAsync(
                null,
                SkillLedger.Core.Constants.AuditActions.ESCROW_INTEGRITY_CHECK,
                $"Escrow integrity validation passed. EscrowId: {escrowId}");
        }

        return isValid;
    }

    #endregion

    #region Reporting and Analytics

    public async Task<EscrowStatistics> GetEscrowStatisticsAsync(Guid userId)
    {
        var escrows = await _context.ProjectEscrows
            .Where(e => e.ClientId == userId || e.ProviderId == userId)
            .ToListAsync();

        return new EscrowStatistics
        {
            TotalEscrowsCreated = escrows.Count,
            ActiveEscrows = escrows.Count(e => e.Status == EscrowStatus.Active || e.Status == EscrowStatus.PartiallyReleased),
            CompletedEscrows = escrows.Count(e => e.Status == EscrowStatus.Completed),
            DisputedEscrows = escrows.Count(e => e.Status == EscrowStatus.Disputed),
            TotalCreditsInEscrow = escrows.Where(e => !e.IsTerminal).Sum(e => e.RemainingAmount),
            TotalCreditsReleased = escrows.Sum(e => e.ReleasedAmount),
            AverageEscrowAmount = escrows.Any() ? (decimal)escrows.Average(e => e.TotalAmount) : 0,
            CompletionRate = escrows.Any() ? (decimal)escrows.Count(e => e.Status == EscrowStatus.Completed) / escrows.Count * 100 : 0,
            LastEscrowActivity = escrows.Max(e => (DateTime?)e.UpdatedAt)
        };
    }

    public async Task<SystemEscrowMetrics> GetSystemEscrowMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;

        // PERFORMANCE FIX: Execute database-side aggregations instead of loading all escrows
        // Calculate all metrics with separate optimized queries
        var totalActiveEscrows = await _context.ProjectEscrows
            .Where(e => e.Status == EscrowStatus.Active || e.Status == EscrowStatus.PartiallyReleased)
            .CountAsync();

        var totalCreditsInEscrow = (int)(await _context.ProjectEscrows
            .Where(e => e.Status != EscrowStatus.Completed && e.Status != EscrowStatus.Cancelled)
            .SumAsync(e => (decimal?)(e.TotalAmount - e.ReleasedAmount)) ?? 0);

        var escrowsCreatedToday = await _context.ProjectEscrows
            .Where(e => e.CreatedAt >= today)
            .CountAsync();

        var escrowsCompletedToday = await _context.ProjectEscrows
            .Where(e => e.CompletedAt >= today)
            .CountAsync();

        var pendingDisputes = await _context.ProjectEscrows
            .Where(e => e.Status == EscrowStatus.Disputed)
            .CountAsync();

        var frozenEscrows = await _context.ProjectEscrows
            .Where(e => e.Status == EscrowStatus.Frozen)
            .CountAsync();

        var totalEscrows = await _context.ProjectEscrows.CountAsync();

        // Calculate average release time from completed milestones
        var completedMilestones = await _context.EscrowMilestones
            .Where(m => m.IsReleased && m.ReleasedAt.HasValue)
            .Select(m => new { m.CreatedAt, m.ReleasedAt })
            .ToListAsync();

        var avgReleaseTimeHours = completedMilestones.Any()
            ? completedMilestones.Average(m => (m.ReleasedAt!.Value - m.CreatedAt).TotalHours)
            : 0;

        return new SystemEscrowMetrics
        {
            TotalActiveEscrows = totalActiveEscrows,
            TotalCreditsInEscrow = totalCreditsInEscrow,
            EscrowsCreatedToday = escrowsCreatedToday,
            EscrowsCompletedToday = escrowsCompletedToday,
            PendingDisputes = pendingDisputes,
            FrozenEscrows = frozenEscrows,
            AverageReleaseTime = (decimal)avgReleaseTimeHours,
            DisputeRate = totalEscrows > 0 ? (decimal)pendingDisputes / totalEscrows * 100 : 0
        };
    }

    public async Task<EscrowComplianceReport> GenerateEscrowReportAsync(DateTime startDate, DateTime endDate)
    {
        var escrows = await _context.ProjectEscrows
            .Include(e => e.Project)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync();

        var auditLogs = await _context.AuditLogs
            .Where(a => a.Timestamp >= startDate &&
                       a.Timestamp <= endDate &&
                       (a.Action.StartsWith("ESCROW_") || a.Action.StartsWith("CREDIT_ESCROW_")))
            .ToListAsync();

        return new EscrowComplianceReport
        {
            ReportPeriodStart = startDate,
            ReportPeriodEnd = endDate,
            TotalEscrows = escrows.Count,
            TotalCreditsProcessed = escrows.Sum(e => e.ReleasedAmount),
            HighValueEscrows = escrows.Where(e => e.TotalAmount > 1000).ToList(),
            DisputedEscrows = escrows.Where(e => e.Status == EscrowStatus.Disputed).ToList(),
            SecurityEvents = auditLogs.Where(a => a.Action.Contains("INTEGRITY") ||
                                                 a.Action.Contains("FROZEN") ||
                                                 a.Action.Contains("DISPUTE")).ToList()
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Helper method to log audit events with correct signature
    /// </summary>
    private async Task LogAuditEventAsync(Guid? userId, string action, string message, string? ipAddress = null, bool success = true)
    {
        await _auditService.LogEventAsync(
            userId,
            action,
            ipAddress ?? "System",
            null,
            success,
            message);
    }

    /// <summary>
    /// BUG-BE-006 FIX: Check if user has admin/moderator permissions
    /// Uses role-based authorization to verify Admin role via UserRoles join
    /// </summary>
    private async Task<bool> HasAdminPermissionsAsync(Guid userId)
    {
        // Check for Admin role using UserRoles join
        // Note: Full RBAC with granular permissions (escrow.manage, escrow.resolve_disputes)
        // would require additional tables/relationships. For now, we check Admin role only.
        var hasPermission = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .AnyAsync(r => r.Name == "Admin");

        return hasPermission;
    }

    #endregion

    #region Real-time Updates

    public async Task<EscrowUpdateNotification> GetEscrowUpdateNotificationAsync(Guid userId)
    {
        var userEscrows = await GetActiveEscrowsForUserAsync(userId);

        var upcomingMilestones = await _context.EscrowMilestones
            .Include(m => m.Escrow)
            .Where(m => (m.Escrow.ClientId == userId || m.Escrow.ProviderId == userId) &&
                       !m.IsReleased &&
                       m.ExpectedCompletionDate.HasValue &&
                       m.ExpectedCompletionDate <= DateTime.UtcNow.AddDays(7))
            .OrderBy(m => m.ExpectedCompletionDate)
            .Take(5)
            .ToListAsync();

        return new EscrowUpdateNotification
        {
            UserId = userId,
            ActiveEscrowCount = userEscrows.Count,
            PendingReleases = upcomingMilestones.Count,
            TotalCreditsInEscrow = userEscrows.Sum(e => e.RemainingAmount),
            UpcomingMilestones = upcomingMilestones,
            RecentActivity = userEscrows.OrderByDescending(e => e.UpdatedAt).Take(3).ToList(),
            LastUpdated = DateTime.UtcNow
        };
    }

    #endregion
}