using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

public class MilestoneTrackingService : IMilestoneTrackingService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IProjectEscrowService _escrowService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<MilestoneTrackingService> _logger;

    public MilestoneTrackingService(
        SkillLedgerDbContext context,
        IProjectEscrowService escrowService,
        IAuditLogService auditLogService,
        ILogger<MilestoneTrackingService> logger)
    {
        _context = context;
        _escrowService = escrowService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    #region Milestone Management

    public async Task<MilestoneResponseDto> CreateMilestoneAsync(CreateMilestoneRequestDto request, Guid createdByUserId, string? ipAddress = null)
    {
        try
        {
            // Validate title
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title cannot be empty or whitespace", nameof(request.Title));

            // Validate weight percentage
            if (request.WeightPercentage < 0 || request.WeightPercentage > 100)
                throw new ArgumentException("Weight percentage must be between 0 and 100", nameof(request.WeightPercentage));

            // Validate user has access to project
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId);

            if (project == null)
                throw new ArgumentException("Project not found", nameof(request.ProjectId));

            if (!CanManageMilestones(project, createdByUserId))
                throw new UnauthorizedAccessException("Only the project client can create milestones");

            if (request.AssignedToUserId.HasValue && !IsProjectParticipant(project, request.AssignedToUserId.Value))
                throw new UnauthorizedAccessException("Milestones can only be assigned to project participants");

            var milestone = new ProjectMilestone
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                EscrowMilestoneId = request.EscrowMilestoneId,
                Title = request.Title,
                Description = request.Description,
                Status = MilestoneStatus.NotStarted,
                Priority = request.Priority,
                DueDate = request.DueDate,
                SequenceOrder = request.SequenceOrder,
                WeightPercentage = request.WeightPercentage,
                AcceptanceCriteria = request.AcceptanceCriteria,
                AssignedToUserId = request.AssignedToUserId,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProjectMilestones.Add(milestone);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                createdByUserId,
                "MILESTONE_CREATED",
                ipAddress ?? "unknown",
                "web",
                true,
                $"Created milestone: {milestone.Title}. MilestoneId: {milestone.Id}, ProjectId: {request.ProjectId}");

            return MapToMilestoneResponseDto(milestone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating milestone for project {ProjectId}", request.ProjectId);
            throw;
        }
    }

    public async Task<MilestoneResponseDto?> GetMilestoneByIdAsync(Guid milestoneId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .Include(m => m.AssignedToUser)
                .Include(m => m.Submissions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return null;

            return MapToMilestoneResponseDto(milestone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    public async Task<MilestoneResponseDto?> UpdateMilestoneAsync(Guid milestoneId, UpdateMilestoneRequestDto request, Guid updatedByUserId)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return null;

            if (!CanManageMilestones(milestone.Project, updatedByUserId))
                throw new UnauthorizedAccessException("Only the project client can update milestones");

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Title))
                milestone.Title = request.Title;

            if (!string.IsNullOrEmpty(request.Description))
                milestone.Description = request.Description;

            if (request.Priority.HasValue)
                milestone.Priority = request.Priority.Value;

            if (request.DueDate.HasValue)
                milestone.DueDate = request.DueDate.Value;

            if (request.SequenceOrder.HasValue)
                milestone.SequenceOrder = request.SequenceOrder.Value;

            if (request.WeightPercentage.HasValue)
                milestone.WeightPercentage = request.WeightPercentage.Value;

            if (request.AcceptanceCriteria != null)
                milestone.AcceptanceCriteria = request.AcceptanceCriteria;

            if (request.AssignedToUserId.HasValue)
                milestone.AssignedToUserId = request.AssignedToUserId.Value;

            milestone.UpdatedAt = DateTime.UtcNow;
            // Note: UpdatedBy property would be added to entity if needed for audit trail

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                updatedByUserId,
                "MILESTONE_UPDATED",
                "unknown",
                "web",
                true,
                $"Updated milestone: {milestone.Title}. MilestoneId: {milestoneId}");

            return MapToMilestoneResponseDto(milestone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    public async Task<bool> DeleteMilestoneAsync(Guid milestoneId, Guid deletedByUserId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .Include(m => m.Submissions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return false;

            if (!CanManageMilestones(milestone.Project, deletedByUserId))
                throw new UnauthorizedAccessException("Only the project client can delete milestones");

            if (milestone.Status == MilestoneStatus.Approved)
                throw new InvalidOperationException("Cannot delete approved milestones");

            // Delete associated submissions
            _context.DeliverableSubmissions.RemoveRange(milestone.Submissions);
            _context.ProjectMilestones.Remove(milestone);

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                deletedByUserId,
                "MILESTONE_DELETED",
                "unknown",
                "web",
                true,
                $"Deleted milestone: {milestone.Title}. MilestoneId: {milestoneId}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    public async Task<PaginatedMilestonesDto> GetMilestonesAsync(MilestoneFilterDto filter, Guid? userId = null)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery + AsNoTracking for read-only pagination
            var query = _context.ProjectMilestones
                .AsNoTracking()
                .Include(m => m.Project)
                .Include(m => m.AssignedToUser)
                .Include(m => m.Submissions)
                .AsSplitQuery()
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(m => m.Project.ClientId == userId.Value || m.Project.ProviderId == userId.Value);

            // Apply filters
            if (filter.ProjectId.HasValue)
                query = query.Where(m => m.ProjectId == filter.ProjectId.Value);

            if (filter.Status.HasValue)
                query = query.Where(m => m.Status == filter.Status.Value);

            if (filter.Priority.HasValue)
                query = query.Where(m => m.Priority == filter.Priority.Value);

            if (filter.AssignedToUserId.HasValue)
                query = query.Where(m => m.AssignedToUserId == filter.AssignedToUserId.Value);

            if (filter.CreatedByUserId.HasValue)
                query = query.Where(m => m.CreatedByUserId == filter.CreatedByUserId.Value);

            if (filter.DueDateFrom.HasValue)
                query = query.Where(m => m.DueDate >= filter.DueDateFrom.Value);

            if (filter.DueDateTo.HasValue)
                query = query.Where(m => m.DueDate <= filter.DueDateTo.Value);

            if (filter.OverdueOnly == true)
                query = query.Where(m => m.DueDate < DateTime.UtcNow && m.Status != MilestoneStatus.Approved);

            // Apply sorting
            query = filter.SortBy?.ToLowerInvariant() switch
            {
                "title" => filter.SortDirection == "desc"
                    ? query.OrderByDescending(m => m.Title)
                    : query.OrderBy(m => m.Title),
                "duedate" => filter.SortDirection == "desc"
                    ? query.OrderByDescending(m => m.DueDate)
                    : query.OrderBy(m => m.DueDate),
                "priority" => filter.SortDirection == "desc"
                    ? query.OrderByDescending(m => m.Priority)
                    : query.OrderBy(m => m.Priority),
                "status" => filter.SortDirection == "desc"
                    ? query.OrderByDescending(m => m.Status)
                    : query.OrderBy(m => m.Status),
                _ => query.OrderBy(m => m.SequenceOrder).ThenBy(m => m.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // PERFORMANCE FIX: Remove async loop - mapping is synchronous
            var milestones = items.Select(MapToMilestoneResponseDto).ToList();

            return new PaginatedMilestonesDto
            {
                Items = milestones,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
                HasPreviousPage = filter.Page > 1,
                HasNextPage = filter.Page * filter.PageSize < totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving milestones with filter");
            throw;
        }
    }

    public async Task<ProjectProgressDto> GetProjectProgressAsync(Guid projectId, Guid? userId = null)
    {
        try
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
            {
                return EmptyProjectProgress(projectId);
            }

            if (userId.HasValue && !IsProjectParticipant(project, userId.Value))
                throw new UnauthorizedAccessException("Access denied to project progress");

            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var milestones = await _context.ProjectMilestones
                .Where(m => m.ProjectId == projectId)
                .Include(m => m.AssignedToUser)
                .Include(m => m.Submissions)
                .AsSplitQuery()
                .ToListAsync();

            var totalMilestones = milestones.Count;
            var completedMilestones = milestones.Count(m => m.Status == MilestoneStatus.Approved);
            var inProgressMilestones = milestones.Count(m => m.Status == MilestoneStatus.InProgress);
            var overdueMilestones = milestones.Count(m => m.DueDate < DateTime.UtcNow && m.Status != MilestoneStatus.Approved);

            var overallProgress = totalMilestones > 0 ? (decimal)completedMilestones / totalMilestones * 100 : 0;

            var upcomingMilestones = milestones
                .Where(m => m.DueDate > DateTime.UtcNow && m.Status != MilestoneStatus.Approved)
                .OrderBy(m => m.DueDate)
                .Take(5);

            var overdueMilestonesList = milestones
                .Where(m => m.DueDate < DateTime.UtcNow && m.Status != MilestoneStatus.Approved)
                .OrderBy(m => m.DueDate);

            // PERFORMANCE FIX: Remove async loops - mapping is now synchronous
            var upcomingDtos = upcomingMilestones.Select(MapToMilestoneResponseDto).ToList();
            var overdueDtos = overdueMilestonesList.Select(MapToMilestoneResponseDto).ToList();

            return new ProjectProgressDto
            {
                ProjectId = projectId,
                TotalMilestones = totalMilestones,
                CompletedMilestones = completedMilestones,
                InProgressMilestones = inProgressMilestones,
                OverdueMilestones = overdueMilestones,
                OverallProgressPercentage = Math.Round(overallProgress, 2),
                NextMilestoneDue = upcomingMilestones.FirstOrDefault()?.DueDate,
                UpcomingMilestones = upcomingDtos,
                OverdueMilestonesList = overdueDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating progress for project {ProjectId}", projectId);
            throw;
        }
    }

    #endregion

    #region Milestone Status Management

    public async Task<bool> StartMilestoneAsync(Guid milestoneId, Guid userId)
    {
        return await UpdateMilestoneStatusAsync(milestoneId, MilestoneStatus.InProgress, userId, "MILESTONE_STARTED");
    }

    public async Task<bool> SubmitMilestoneForReviewAsync(Guid milestoneId, Guid userId)
    {
        return await UpdateMilestoneStatusAsync(milestoneId, MilestoneStatus.PendingReview, userId, "MILESTONE_SUBMITTED");
    }

    public async Task<bool> ApproveMilestoneAsync(Guid milestoneId, Guid approvedByUserId, string? reviewNotes = null)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null || milestone.Status != MilestoneStatus.PendingReview)
                return false;

            // Only project client can approve milestones
            if (milestone.Project.ClientId != approvedByUserId)
                throw new UnauthorizedAccessException("Only the project client can approve milestones");

            // VULN-029 FIX: Check escrow status before approving milestone
            if (milestone.EscrowMilestoneId.HasValue)
            {
                var escrowMilestone = await _context.EscrowMilestones
                    .Include(em => em.Escrow)
                    .FirstOrDefaultAsync(em => em.Id == milestone.EscrowMilestoneId.Value);

                if (escrowMilestone != null && escrowMilestone.Escrow != null)
                {
                    // Don't approve if escrow is frozen or disputed
                    if (escrowMilestone.Escrow.Status == EscrowStatus.Frozen)
                    {
                        throw new InvalidOperationException($"Cannot approve milestone: escrow is frozen (Status: {escrowMilestone.Escrow.Status})");
                    }

                    if (escrowMilestone.Escrow.Status == EscrowStatus.Disputed)
                    {
                        throw new InvalidOperationException("Cannot approve milestone: escrow is currently disputed");
                    }

                    // Don't approve if milestone is already released
                    if (escrowMilestone.IsReleased)
                    {
                        throw new InvalidOperationException("Cannot approve milestone: payment has already been released");
                    }
                }
            }

            milestone.Status = MilestoneStatus.Approved;
            milestone.CompletedAt = DateTime.UtcNow;
            milestone.ReviewNotes = reviewNotes;
            milestone.UpdatedAt = DateTime.UtcNow;
            // Note: UpdatedBy property would be added to entity if needed for audit trail

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                approvedByUserId,
                "MILESTONE_APPROVED",
                "unknown",
                "web",
                true,
                $"Milestone approved: {milestone.Title}. MilestoneId: {milestoneId}");

            // Trigger payment release if linked to escrow
            if (milestone.EscrowMilestoneId.HasValue)
            {
                try
                {
                    await _escrowService.ReleaseMilestoneAsync(milestone.EscrowMilestoneId.Value, approvedByUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to release payment for milestone {MilestoneId}", milestoneId);
                    // Don't fail the approval if payment release fails
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    public async Task<bool> RequestMilestoneRevisionAsync(Guid milestoneId, Guid reviewedByUserId, string reviewNotes)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null || milestone.Status != MilestoneStatus.PendingReview)
                return false;

            if (milestone.Project.ClientId != reviewedByUserId)
                throw new UnauthorizedAccessException("Only the project client can request revisions");

            milestone.Status = MilestoneStatus.InProgress;
            milestone.ReviewNotes = reviewNotes;
            milestone.UpdatedAt = DateTime.UtcNow;
            // Note: UpdatedBy property would be added to entity if needed for audit trail

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                reviewedByUserId,
                "MILESTONE_REVISION_REQUESTED",
                "unknown",
                "web",
                true,
                $"Revision requested for milestone: {milestone.Title}. MilestoneId: {milestoneId}, ReviewNotes: {reviewNotes}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting revision for milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    public async Task<bool> CancelMilestoneAsync(Guid milestoneId, Guid cancelledByUserId, string? reason = null)
    {
        return await UpdateMilestoneStatusAsync(milestoneId, MilestoneStatus.Cancelled, cancelledByUserId, "MILESTONE_CANCELLED", reason);
    }

    #endregion

    #region Deliverable Submission Management

    public async Task<SubmissionResponseDto> CreateSubmissionAsync(CreateSubmissionRequestDto request, Guid submittedByUserId, string? ipAddress = null)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == request.MilestoneId);

            if (milestone == null)
                throw new ArgumentException("Milestone not found");

            if (!CanSubmitMilestone(milestone, submittedByUserId))
                throw new UnauthorizedAccessException("Only the project provider or assigned user can create submissions");

            var submission = new DeliverableSubmission
            {
                Id = Guid.NewGuid(),
                MilestoneId = request.MilestoneId,
                SubmittedByUserId = submittedByUserId,
                Type = request.Type,
                Title = request.Title,
                Description = request.Description,
                SubmissionUrl = request.SubmissionUrl,
                TextContent = request.TextContent,
                SubmissionNotes = request.SubmissionNotes,
                SubmittedAt = DateTime.UtcNow,
                IsReviewed = false
            };

            // Handle file attachments
            if (request.AttachedFileIds?.Any() == true)
            {
                var attachedFiles = await _context.UploadedFiles
                    .Where(f => request.AttachedFileIds.Contains(f.Id))
                    .ToListAsync();

                foreach (var file in attachedFiles)
                {
                    submission.AttachedFiles.Add(file);
                }
            }

            _context.DeliverableSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                submittedByUserId,
                "SUBMISSION_CREATED",
                ipAddress ?? "unknown",
                "web",
                true,
                $"Created submission: {submission.Title} for milestone {milestone.Title}. SubmissionId: {submission.Id}, MilestoneId: {request.MilestoneId}");

            return await MapToSubmissionResponseDto(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating submission for milestone {MilestoneId}", request.MilestoneId);
            throw;
        }
    }

    public async Task<SubmissionResponseDto?> GetSubmissionByIdAsync(Guid submissionId, Guid? userId = null)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var submission = await _context.DeliverableSubmissions
                .Include(s => s.Milestone)
                    .ThenInclude(m => m.Project)
                .Include(s => s.AttachedFiles)
                .Include(s => s.SubmittedByUser)
                .Include(s => s.ReviewedByUser)
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return null;

            if (userId.HasValue && !IsProjectParticipant(submission.Milestone.Project, userId.Value))
                throw new UnauthorizedAccessException("Access denied to submission");

            return await MapToSubmissionResponseDto(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submission {SubmissionId}", submissionId);
            throw;
        }
    }

    public async Task<List<SubmissionResponseDto>> GetMilestoneSubmissionsAsync(Guid milestoneId, Guid? userId = null)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return new List<SubmissionResponseDto>();

            if (userId.HasValue && !IsProjectParticipant(milestone.Project, userId.Value))
                throw new UnauthorizedAccessException("Access denied to milestone submissions");

            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var submissions = await _context.DeliverableSubmissions
                .Include(s => s.AttachedFiles)
                .Include(s => s.SubmittedByUser)
                .Include(s => s.ReviewedByUser)
                .AsSplitQuery()
                .Where(s => s.MilestoneId == milestoneId)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            var result = new List<SubmissionResponseDto>();
            foreach (var submission in submissions)
            {
                result.Add(await MapToSubmissionResponseDto(submission));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submissions for milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    public async Task<bool> ReviewSubmissionAsync(Guid submissionId, ReviewSubmissionRequestDto request, Guid reviewedByUserId)
    {
        try
        {
            var submission = await _context.DeliverableSubmissions
                .Include(s => s.Milestone)
                    .ThenInclude(m => m.Project)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return false;

            if (submission.Milestone.Project.ClientId != reviewedByUserId)
                throw new UnauthorizedAccessException("Only the project client can review submissions");

            submission.IsReviewed = true;
            submission.IsApproved = request.IsApproved;
            submission.ReviewFeedback = request.ReviewFeedback;
            submission.ReviewedAt = DateTime.UtcNow;
            submission.ReviewedByUserId = reviewedByUserId;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                reviewedByUserId,
                request.IsApproved ? "SUBMISSION_APPROVED" : "SUBMISSION_REJECTED",
                "unknown",
                "web",
                true,
                $"Reviewed submission: {submission.Title}. SubmissionId: {submissionId}, IsApproved: {request.IsApproved}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing submission {SubmissionId}", submissionId);
            throw;
        }
    }

    #endregion

    #region Progress and Analytics

    public async Task<List<MilestoneResponseDto>> GetOverdueMilestonesAsync(Guid? userId = null)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var query = _context.ProjectMilestones
                .Include(m => m.Project)
                .Include(m => m.AssignedToUser)
                .Include(m => m.Submissions)
                .AsSplitQuery()
                .Where(m => m.DueDate < DateTime.UtcNow && m.Status != MilestoneStatus.Approved && m.Status != MilestoneStatus.Cancelled);

            if (userId.HasValue)
            {
                query = query.Where(m => m.AssignedToUserId == userId.Value ||
                                        m.CreatedByUserId == userId.Value);
                // Note: Project workspace access check would be added when workspace feature is available
            }

            var milestones = await query
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            // PERFORMANCE FIX: Remove async loop - mapping is now synchronous
            return milestones.Select(MapToMilestoneResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue milestones");
            throw;
        }
    }

    public async Task<List<MilestoneResponseDto>> GetUpcomingMilestonesAsync(Guid userId, int daysAhead = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var milestones = await _context.ProjectMilestones
                .Include(m => m.Project)
                .Include(m => m.AssignedToUser)
                .Include(m => m.Submissions)
                .AsSplitQuery()
                .Where(m => m.DueDate >= DateTime.UtcNow &&
                           m.DueDate <= cutoffDate &&
                           m.Status != MilestoneStatus.Approved &&
                           m.Status != MilestoneStatus.Cancelled &&
                           (m.AssignedToUserId == userId ||
                            m.CreatedByUserId == userId))
                .OrderBy(m => m.DueDate)
                .ToListAsync();
            // Note: Project workspace access check would be added when workspace feature is available

            // PERFORMANCE FIX: Remove async loop - mapping is now synchronous
            return milestones.Select(MapToMilestoneResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming milestones");
            throw;
        }
    }

    #endregion

    #region Escrow Integration

    public async Task<bool> LinkToEscrowMilestoneAsync(Guid milestoneId, Guid escrowMilestoneId, Guid linkedByUserId)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return false;

            if (!CanManageMilestones(milestone.Project, linkedByUserId))
                throw new UnauthorizedAccessException("Only the project client can link milestone escrow");

            milestone.EscrowMilestoneId = escrowMilestoneId;
            milestone.UpdatedAt = DateTime.UtcNow;
            // Note: UpdatedBy property would be added to entity if needed for audit trail

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                linkedByUserId,
                "MILESTONE_ESCROW_LINKED",
                "unknown",
                "web",
                true,
                $"Linked milestone {milestone.Title} to escrow milestone {escrowMilestoneId}. MilestoneId: {milestoneId}, EscrowMilestoneId: {escrowMilestoneId}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking milestone {MilestoneId} to escrow {EscrowMilestoneId}", milestoneId, escrowMilestoneId);
            throw;
        }
    }

    public async Task<bool> TriggerPaymentReleaseAsync(Guid milestoneId, Guid triggeredByUserId)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null || milestone.Status != MilestoneStatus.Approved || !milestone.EscrowMilestoneId.HasValue)
                return false;

            if (milestone.Project.ClientId != triggeredByUserId)
                throw new UnauthorizedAccessException("Only the project client can trigger payment releases");

            await _escrowService.ReleaseMilestoneAsync(milestone.EscrowMilestoneId.Value, triggeredByUserId);

            await _auditLogService.LogEventAsync(
                triggeredByUserId,
                "MILESTONE_PAYMENT_RELEASED",
                "unknown",
                "web",
                true,
                $"Payment released for milestone: {milestone.Title}. MilestoneId: {milestoneId}, EscrowMilestoneId: {milestone.EscrowMilestoneId}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering payment release for milestone {MilestoneId}", milestoneId);
            throw;
        }
    }

    #endregion

    #region Security and Validation

    public async Task<bool> ValidateUserPermissionsAsync(Guid milestoneId, Guid userId, string operation)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return false;

            return operation.ToUpperInvariant() switch
            {
                "READ" => IsProjectParticipant(milestone.Project, userId),
                "CREATE" or "UPDATE" or "DELETE" or "MANAGE" or "CANCEL" or "LINK" => CanManageMilestones(milestone.Project, userId),
                "START" or "SUBMIT" => CanSubmitMilestone(milestone, userId),
                "REVIEW" or "APPROVE" => CanManageMilestones(milestone.Project, userId),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permissions for milestone {MilestoneId}", milestoneId);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<bool> UpdateMilestoneStatusAsync(Guid milestoneId, MilestoneStatus newStatus, Guid userId, string auditAction, string? reason = null)
    {
        try
        {
            var milestone = await _context.ProjectMilestones
                .Include(m => m.Project)
                .FirstOrDefaultAsync(m => m.Id == milestoneId);

            if (milestone == null)
                return false;

            if (newStatus == MilestoneStatus.InProgress || newStatus == MilestoneStatus.PendingReview)
            {
                if (!CanSubmitMilestone(milestone, userId))
                    throw new UnauthorizedAccessException("Only the project provider or assigned user can change work status");
            }
            else if (newStatus == MilestoneStatus.Cancelled)
            {
                if (!CanManageMilestones(milestone.Project, userId))
                    throw new UnauthorizedAccessException("Only the project client can cancel milestones");
            }

            milestone.Status = newStatus;
            milestone.UpdatedAt = DateTime.UtcNow;
            // Note: UpdatedBy property would be added to entity if needed for audit trail

            if (newStatus == MilestoneStatus.Approved)
                milestone.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                userId,
                auditAction,
                "unknown",
                "web",
                true,
                $"Milestone {milestone.Title} status changed to {newStatus}" + (reason != null ? $": {reason}" : "") + $". MilestoneId: {milestoneId}, NewStatus: {newStatus}, Reason: {reason}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating milestone {MilestoneId} status to {Status}", milestoneId, newStatus);
            throw;
        }
    }

    // PERFORMANCE FIX: Made synchronous - no async operations needed
    private MilestoneResponseDto MapToMilestoneResponseDto(ProjectMilestone milestone)
    {
        var submissions = new List<SubmissionSummaryDto>();

        if (milestone.Submissions?.Any() == true)
        {
            submissions = milestone.Submissions.Select(s => new SubmissionSummaryDto
            {
                Id = s.Id,
                Title = s.Title,
                Type = s.Type,
                SubmittedAt = s.SubmittedAt,
                IsReviewed = s.IsReviewed,
                IsApproved = s.IsApproved,
                AttachmentCount = s.AttachedFiles?.Count ?? 0,
                TotalFileSize = s.AttachedFiles?.Sum(f => f.FileSizeBytes) ?? 0
            }).ToList();
        }

        var now = DateTime.UtcNow;

        return new MilestoneResponseDto
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            EscrowMilestoneId = milestone.EscrowMilestoneId,
            Title = milestone.Title,
            Description = milestone.Description,
            Status = milestone.Status,
            Priority = milestone.Priority,
            DueDate = milestone.DueDate,
            CompletedAt = milestone.CompletedAt,
            SequenceOrder = milestone.SequenceOrder,
            WeightPercentage = milestone.WeightPercentage,
            AcceptanceCriteria = milestone.AcceptanceCriteria,
            ReviewNotes = milestone.ReviewNotes,
            CreatedByUserId = milestone.CreatedByUserId,
            CreatedByUserName = "", // Would need to load user details
            AssignedToUserId = milestone.AssignedToUserId,
            AssignedToUserName = milestone.AssignedToUser?.UserName ?? "",
            CreatedAt = milestone.CreatedAt,
            UpdatedAt = milestone.UpdatedAt,

            // Calculated properties
            IsOverdue = milestone.DueDate.HasValue && milestone.DueDate.Value < now && milestone.Status != MilestoneStatus.Approved,
            CanBeStarted = milestone.Status == MilestoneStatus.NotStarted,
            CanBeSubmitted = milestone.Status == MilestoneStatus.InProgress,
            CanBeApproved = milestone.Status == MilestoneStatus.PendingReview,
            DaysUntilDue = milestone.DueDate.HasValue ? (int?)(milestone.DueDate.Value - now).TotalDays : null,

            Submissions = submissions
        };
    }

    private async Task<SubmissionResponseDto> MapToSubmissionResponseDto(DeliverableSubmission submission)
    {
        var attachedFiles = submission.AttachedFiles?.Select(f => new AttachedFileDto
        {
            Id = f.Id,
            FileName = f.FileName,
            ContentType = f.ContentType,
            FileSize = f.FileSizeBytes,
            UploadedAt = f.CreatedAt,
            FileUrl = f.BlobName
        }).ToList() ?? new List<AttachedFileDto>();

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            MilestoneId = submission.MilestoneId,
            SubmittedByUserId = submission.SubmittedByUserId,
            SubmittedByUserName = submission.SubmittedByUser?.UserName ?? "",
            Type = submission.Type,
            Title = submission.Title,
            Description = submission.Description,
            SubmissionUrl = submission.SubmissionUrl,
            TextContent = submission.TextContent,
            SubmittedAt = submission.SubmittedAt,
            SubmissionNotes = submission.SubmissionNotes,
            IsReviewed = submission.IsReviewed,
            IsApproved = submission.IsApproved,
            ReviewedAt = submission.ReviewedAt,
            ReviewedByUserId = submission.ReviewedByUserId,
            ReviewedByUserName = submission.ReviewedByUser?.UserName ?? "",
            ReviewFeedback = submission.ReviewFeedback,
            AttachedFiles = attachedFiles,

            // Calculated properties
            CanBeReviewed = !submission.IsReviewed,
            TotalFileSize = attachedFiles.Sum(f => f.FileSize),
            AttachmentCount = attachedFiles.Count
        };
    }

    private static bool IsProjectParticipant(Project project, Guid userId)
    {
        return project.ClientId == userId || project.ProviderId == userId;
    }

    private static bool CanManageMilestones(Project project, Guid userId)
    {
        return project.ClientId == userId;
    }

    private static bool CanSubmitMilestone(ProjectMilestone milestone, Guid userId)
    {
        return milestone.Project.ProviderId == userId || milestone.AssignedToUserId == userId;
    }

    private static ProjectProgressDto EmptyProjectProgress(Guid projectId)
    {
        return new ProjectProgressDto
        {
            ProjectId = projectId,
            TotalMilestones = 0,
            CompletedMilestones = 0,
            InProgressMilestones = 0,
            OverdueMilestones = 0,
            OverallProgressPercentage = 0
        };
    }

    #endregion
}
