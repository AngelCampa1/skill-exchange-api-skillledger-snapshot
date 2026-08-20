using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock implementation of IProjectEscrowService for integration testing.
/// INTERNAL SERVICE - uses real database operations, not mock behavior.
/// </summary>
public class MockProjectEscrowService : IProjectEscrowService
{
    private readonly SkillLedgerDbContext _context;

    public MockProjectEscrowService(SkillLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectEscrow> CreateEscrowAsync(Guid projectId, Guid providerId, string? initiatedFromIP = null)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<ProjectEscrow?> GetEscrowByProjectIdAsync(Guid projectId)
    {
        return await _context.ProjectEscrows
            .FirstOrDefaultAsync(e => e.ProjectId == projectId);
    }

    public async Task<ProjectEscrow?> GetEscrowByIdAsync(Guid escrowId)
    {
        return await _context.ProjectEscrows.FindAsync(escrowId);
    }

    public async Task<IList<ProjectEscrow>> GetActiveEscrowsForUserAsync(Guid userId)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<IList<AuditLog>> GetEscrowHistoryAsync(Guid escrowId)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<EscrowMilestone> AddMilestoneAsync(Guid escrowId, string description, int amount, DateTime? expectedCompletionDate = null, Guid? linkedDeliverableId = null, int? sequenceOrder = null)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    /// <summary>
    /// Release a milestone - writes to real database
    /// </summary>
    public async Task<bool> ReleaseMilestoneAsync(Guid milestoneId, Guid approvedByUserId, string? releaseNotes = null)
    {
        var escrowMilestone = await _context.EscrowMilestones.FindAsync(milestoneId);
        if (escrowMilestone == null)
            return false;

        escrowMilestone.IsReleased = true;
        escrowMilestone.ReleasedAt = DateTime.UtcNow;
        escrowMilestone.ReleasedByUserId = approvedByUserId;

        await _context.SaveChangesAsync();
        return true;
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
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> ReleaseFullEscrowAsync(Guid escrowId, Guid approvedByUserId, string? releaseNotes = null)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> CancelEscrowAsync(Guid escrowId, Guid cancelledByUserId, string? cancellationReason = null)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> RaiseDisputeAsync(Guid escrowId, Guid raisedByUserId, string disputeReason)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> ResolveDisputeAsync(Guid escrowId, Guid resolvedByUserId, string resolutionAction, string? resolutionNotes = null)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<IList<ProjectEscrow>> GetDisputedEscrowsAsync()
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> FreezeEscrowAsync(Guid escrowId, Guid frozenByUserId, string freezeReason)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> UnfreezeEscrowAsync(Guid escrowId, Guid unfrozenByUserId)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<bool> ValidateEscrowIntegrityAsync(Guid escrowId)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<EscrowStatistics> GetEscrowStatisticsAsync(Guid userId)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<SystemEscrowMetrics> GetSystemEscrowMetricsAsync()
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<EscrowComplianceReport> GenerateEscrowReportAsync(DateTime startDate, DateTime endDate)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }

    public async Task<EscrowUpdateNotification> GetEscrowUpdateNotificationAsync(Guid userId)
    {
        throw new NotImplementedException("Use real ProjectEscrowService for integration tests");
    }
}
