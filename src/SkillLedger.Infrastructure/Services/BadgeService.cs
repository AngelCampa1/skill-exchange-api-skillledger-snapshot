using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Service for managing user badges and verification
/// </summary>
public class BadgeService : IBadgeService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IBadgeSecurityService _badgeSecurityService;
    private readonly IDistributedLockService _distributedLockService;
    private readonly ILogger<BadgeService> _logger;

    public BadgeService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        IBadgeSecurityService badgeSecurityService,
        IDistributedLockService distributedLockService,
        ILogger<BadgeService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _badgeSecurityService = badgeSecurityService;
        _distributedLockService = distributedLockService;
        _logger = logger;
    }

    public async Task<List<BadgeProgressDto>> CheckBadgeEligibilityAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for badge eligibility check: {UserId}", userId);
                return new List<BadgeProgressDto>();
            }

            // PERFORMANCE FIX: Add AsNoTracking for read-only queries and AsSplitQuery to prevent cartesian explosion
            var badgeDefinitions = await _context.BadgeDefinitions
                .Where(bd => bd.IsActive)
                .Include(bd => bd.Criteria.Where(c => c.IsActive))
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            // Get user's existing badges
            var existingBadges = await _context.UserBadges
                .AsNoTracking()
                .Where(ub => ub.UserId == userId && ub.IsActive)
                .Select(ub => ub.BadgeType)
                .ToListAsync();

            var progress = new List<BadgeProgressDto>();

            foreach (var definition in badgeDefinitions)
            {
                // Skip if user already has this badge
                if (existingBadges.Contains(definition.BadgeType))
                    continue;

                var badgeProgress = await CalculateBadgeProgressAsync(userId, definition);
                progress.Add(badgeProgress);
            }

            return progress.OrderByDescending(p => p.ProgressPercentage).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking badge eligibility for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<BadgeProgressDto>> GetBadgeProgressAsync(Guid userId)
    {
        return await CheckBadgeEligibilityAsync(userId);
    }

    public async Task<UserBadge> AwardBadgeAsync(Guid userId, string badgeType, Dictionary<string, object>? evidence = null, Guid? awardedBy = null)
    {
        // VULN-011 FIX: Add distributed lock to prevent race condition in badge awarding
        var lockKey = $"badge:award:{userId}:{badgeType}";
        await using var lockHandle = await _distributedLockService.AcquireLockAsync(
            lockKey,
            TimeSpan.FromSeconds(30),  // Lock expires after 30 seconds
            TimeSpan.FromSeconds(10),  // Wait up to 10 seconds to acquire
            TimeSpan.FromMilliseconds(100)); // Retry every 100ms

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("Could not acquire lock for badge award: {BadgeType} to user {UserId}", badgeType, userId);
            throw new InvalidOperationException("Another badge award operation is currently in progress for this user and badge type. Please try again.");
        }

        try
        {
            // Get badge definition
            var definition = await _context.BadgeDefinitions
                .FirstOrDefaultAsync(bd => bd.BadgeType == badgeType && bd.IsActive);

            if (definition == null)
                throw new ArgumentException($"Badge type '{badgeType}' not found or inactive");

            // Re-check if user already has this badge (after acquiring lock)
            var existingBadge = await _context.UserBadges
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeType == badgeType && ub.IsActive);

            if (existingBadge != null)
                throw new InvalidOperationException($"User already has badge '{badgeType}'");

            // Create new badge
            var badge = new UserBadge
            {
                UserId = userId,
                BadgeType = badgeType,
                BadgeName = definition.DisplayName,
                BadgeDescription = definition.Description,
                Category = definition.Category,
                IconUrl = definition.IconUrl,
                VerificationLevel = definition.RequiredVerification,
                VerificationEvidence = evidence != null ? JsonSerializer.Serialize(evidence) : null,
                VerifiedBy = awardedBy,
                VerifiedAt = awardedBy.HasValue ? DateTime.UtcNow : null,
                ExpiresAt = definition.ExpirationPeriod.HasValue ? DateTime.UtcNow.Add(definition.ExpirationPeriod.Value) : null
            };

            // Generate integrity hash
            badge.IntegrityHash = await _badgeSecurityService.GenerateBadgeHashAsync(badge);

            _context.UserBadges.Add(badge);

            // Add to history
            var history = new BadgeEarningHistory
            {
                UserId = userId,
                BadgeId = badge.Id,
                Action = "Earned",
                Evidence = evidence != null ? JsonSerializer.Serialize(evidence) : null,
                ActionBy = awardedBy
            };

            _context.BadgeEarningHistory.Add(history);

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(userId, "Badge Awarded", "127.0.0.1", null, true, $"Badge '{badgeType}' awarded to user");

            _logger.LogInformation("Badge '{BadgeType}' awarded to user {UserId}", badgeType, userId);

            return badge;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding badge '{BadgeType}' to user {UserId}", badgeType, userId);
            throw;
        }
    }

    public async Task RevokeBadgeAsync(Guid badgeId, string reason, Guid revokedBy)
    {
        try
        {
            var badge = await _context.UserBadges.FindAsync(badgeId);
            if (badge == null)
                throw new ArgumentException($"Badge with ID '{badgeId}' not found");

            badge.IsActive = false;

            // Add to history
            var history = new BadgeEarningHistory
            {
                UserId = badge.UserId,
                BadgeId = badgeId,
                Action = "Revoked",
                Reason = reason,
                ActionBy = revokedBy
            };

            _context.BadgeEarningHistory.Add(history);

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(badge.UserId, "Badge Revoked", "127.0.0.1", null, true, $"Badge '{badge.BadgeType}' revoked: {reason}");

            _logger.LogInformation("Badge '{BadgeType}' revoked from user {UserId}: {Reason}", badge.BadgeType, badge.UserId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking badge {BadgeId}", badgeId);
            throw;
        }
    }

    public async Task<List<UserBadge>> GetUserBadgesAsync(Guid userId, bool includeExpired = false)
    {
        try
        {
            // PERFORMANCE FIX: Use AsNoTracking for read-only query
            var query = _context.UserBadges
                .AsNoTracking()
                .Where(ub => ub.UserId == userId);

            if (!includeExpired)
            {
                query = query.Where(ub => ub.IsActive && (ub.ExpiresAt == null || ub.ExpiresAt > DateTime.UtcNow));
            }

            var badges = await query.OrderByDescending(ub => ub.EarnedAt).ToListAsync();

            // BUG FIX BADGE-003: Validate integrity of badges and filter out tampered ones
            var validBadges = new List<UserBadge>();
            foreach (var badge in badges)
            {
                if (!string.IsNullOrEmpty(badge.IntegrityHash))
                {
                    var isValid = await _badgeSecurityService.ValidateBadgeIntegrityAsync(badge);
                    if (!isValid)
                    {
                        _logger.LogWarning("BADGE-003 FIX: Badge {BadgeId} failed integrity validation - excluding from results", badge.Id);
                        // Log to audit for security investigation
                        await _auditLogService.LogEventAsync(badge.UserId, "Badge Integrity Failure", "127.0.0.1", null, false,
                            $"Badge {badge.Id} ({badge.BadgeType}) failed integrity validation and was excluded");
                        continue; // Skip tampered badges
                    }
                }
                validBadges.Add(badge);
            }

            return validBadges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for user {UserId}", userId);
            throw;
        }
    }

    public async Task<VerificationRequest> SubmitVerificationRequestAsync(Guid userId, string badgeType, Dictionary<string, object> evidence)
    {
        try
        {
            // Check if badge definition exists
            var definition = await _context.BadgeDefinitions
                .FirstOrDefaultAsync(bd => bd.BadgeType == badgeType && bd.IsActive);

            if (definition == null)
                throw new ArgumentException($"Badge type '{badgeType}' not found or inactive");

            // BUG FIX BADGE-002: Check if user already has this badge (active and not expired)
            var existingBadge = await _context.UserBadges
                .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeType == badgeType && ub.IsActive
                    && (ub.ExpiresAt == null || ub.ExpiresAt > DateTime.UtcNow));

            if (existingBadge != null)
                throw new InvalidOperationException($"User already owns an active '{badgeType}' badge");

            // Check if user already has a pending request for this badge
            var existingRequest = await _context.VerificationRequests
                .FirstOrDefaultAsync(vr => vr.UserId == userId && vr.BadgeType == badgeType && vr.Status == "Pending");

            if (existingRequest != null)
                throw new InvalidOperationException($"User already has a pending verification request for badge '{badgeType}'");

            var request = new VerificationRequest
            {
                UserId = userId,
                BadgeType = badgeType,
                SubmittedEvidence = JsonSerializer.Serialize(evidence)
            };

            _context.VerificationRequests.Add(request);
            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(userId, "Verification Request Submitted", "127.0.0.1", null, true, $"Verification request submitted for badge '{badgeType}'");

            _logger.LogInformation("Verification request submitted by user {UserId} for badge '{BadgeType}'", userId, badgeType);

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting verification request for user {UserId}, badge '{BadgeType}'", userId, badgeType);
            throw;
        }
    }

    public async Task ProcessVerificationRequestAsync(Guid requestId, bool approved, string? reviewNotes, Guid reviewedBy)
    {
        try
        {
            var request = await _context.VerificationRequests.FindAsync(requestId);
            if (request == null)
                throw new ArgumentException($"Verification request with ID '{requestId}' not found");

            request.Status = approved ? "Approved" : "Rejected";
            request.ReviewNotes = reviewNotes;
            request.ReviewedBy = reviewedBy;
            request.ReviewedAt = DateTime.UtcNow;

            if (approved)
            {
                // Award the badge
                var evidence = request.SubmittedEvidence != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.SubmittedEvidence)
                    : null;

                await AwardBadgeAsync(request.UserId, request.BadgeType, evidence, reviewedBy);
            }

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogEventAsync(request.UserId, "Verification Request Processed", "127.0.0.1", null, true,
                $"Verification request for badge '{request.BadgeType}' {(approved ? "approved" : "rejected")}");

            _logger.LogInformation("Verification request {RequestId} {Status} by user {ReviewedBy}",
                requestId, approved ? "approved" : "rejected", reviewedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing verification request {RequestId}", requestId);
            throw;
        }
    }

    public async Task<List<VerificationRequest>> GetPendingVerificationRequestsAsync(string? badgeType = null)
    {
        try
        {
            var query = _context.VerificationRequests
                .Where(vr => vr.Status == "Pending");

            if (!string.IsNullOrEmpty(badgeType))
            {
                query = query.Where(vr => vr.BadgeType == badgeType);
            }

            query = query.Include(vr => vr.User);

            return await query.OrderBy(vr => vr.RequestedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending verification requests");
            throw;
        }
    }

    public async Task<int> ProcessAutomaticBadgeEvaluationAsync()
    {
        // VULN-015 FIX: Add system-wide lock to prevent multiple concurrent badge evaluation jobs
        var lockKey = "badge:auto-evaluation:system";
        await using var lockHandle = await _distributedLockService.AcquireLockAsync(
            lockKey,
            TimeSpan.FromMinutes(10),  // Lock expires after 10 minutes (batch processing may take longer)
            TimeSpan.FromSeconds(5),   // Wait up to 5 seconds to acquire
            TimeSpan.FromMilliseconds(500)); // Retry every 500ms

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("Badge auto-evaluation already in progress (another job is running). Skipping this execution.");
            return 0; // Return 0 instead of throwing, as this is expected when jobs overlap
        }

        try
        {
            var users = await _context.Users.Select(u => u.Id).ToListAsync();
            int totalAwarded = 0;

            foreach (var userId in users)
            {
                totalAwarded += await ProcessAutomaticBadgeEvaluationAsync(userId);
            }

            _logger.LogInformation("Automatic badge evaluation completed. {TotalAwarded} badges awarded", totalAwarded);
            return totalAwarded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic badge evaluation");
            throw;
        }
    }

    public async Task<int> ProcessAutomaticBadgeEvaluationAsync(Guid userId)
    {
        try
        {
            int awarded = 0;
            var eligibleBadges = await CheckBadgeEligibilityAsync(userId);

            foreach (var badgeProgress in eligibleBadges.Where(bp => bp.IsEligible))
            {
                try
                {
                    await AwardBadgeAsync(userId, badgeProgress.BadgeType);
                    awarded++;
                }
                catch (InvalidOperationException)
                {
                    // User already has the badge, continue
                }
            }

            return awarded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic badge evaluation for user {UserId}", userId);
            throw;
        }
    }

    public async Task<int> ProcessBadgeExpirationAsync()
    {
        // BUG FIX BADGE-001: Add distributed lock to prevent multiple concurrent expiration processes
        var lockKey = "badge:expiration:system";
        await using var lockHandle = await _distributedLockService.AcquireLockAsync(
            lockKey,
            TimeSpan.FromMinutes(5),  // Lock expires after 5 minutes
            TimeSpan.FromSeconds(5),   // Wait up to 5 seconds to acquire
            TimeSpan.FromMilliseconds(500)); // Retry every 500ms

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("BADGE-001 FIX: Badge expiration already running, skipping duplicate execution");
            return 0; // Another process is already handling expiration
        }

        try
        {
            var expiredBadges = await _context.UserBadges
                .Where(ub => ub.IsActive && ub.ExpiresAt.HasValue && ub.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            int expired = 0;

            foreach (var badge in expiredBadges)
            {
                badge.IsActive = false;

                // Add to history
                var history = new BadgeEarningHistory
                {
                    UserId = badge.UserId,
                    BadgeId = badge.Id,
                    Action = "Expired",
                    Reason = "Badge expired automatically"
                };

                _context.BadgeEarningHistory.Add(history);
                expired++;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Badge expiration processing completed. {ExpiredCount} badges expired", expired);
            return expired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during badge expiration processing");
            throw;
        }
    }

    private async Task<BadgeProgressDto> CalculateBadgeProgressAsync(Guid userId, BadgeDefinition definition)
    {
        var progress = new BadgeProgressDto
        {
            BadgeType = definition.BadgeType,
            BadgeName = definition.DisplayName,
            Description = definition.Description,
            Category = definition.Category,
            IconUrl = definition.IconUrl
        };

        var requirements = new List<BadgeRequirementProgressDto>();

        // This is a simplified implementation - in a real system, you'd evaluate complex criteria
        // For now, we'll create some basic examples based on common badge types

        switch (definition.BadgeType)
        {
            case "HIGH_PERFORMER":
                requirements.Add(await CalculateRatingRequirementAsync(userId, 4.5m));
                requirements.Add(await CalculateProjectCountRequirementAsync(userId, 10));
                break;

            case "VETERAN":
                requirements.Add(await CalculateProjectCountRequirementAsync(userId, 50));
                requirements.Add(await CalculateRatingRequirementAsync(userId, 4.0m));
                requirements.Add(await CalculateAccountAgeRequirementAsync(userId, 365));
                break;

            default:
                // For unknown badge types, check if there are specific criteria defined
                var criteria = definition.Criteria?.Where(c => c.IsActive).ToList() ?? new List<BadgeCriteria>();
                foreach (var criterion in criteria)
                {
                    var requirement = new BadgeRequirementProgressDto
                    {
                        Name = criterion.CriteriaName,
                        Description = $"Meet requirement: {criterion.CriteriaValue}",
                        IsMet = false, // Default to false for manual verification badges
                        ProgressPercentage = 0m,
                        Current = 0,
                        Required = decimal.TryParse(criterion.CriteriaValue, out var val) ? val : 1,
                        Unit = "requirement"
                    };

                    // For automatic badges, try to evaluate
                    if (definition.RequiredVerification == VerificationLevel.Automatic)
                    {
                        // Basic evaluation based on criteria name
                        if (criterion.CriteriaName.Contains("Rating", StringComparison.OrdinalIgnoreCase))
                        {
                            if (decimal.TryParse(criterion.CriteriaValue, out var requiredRating))
                            {
                                requirement = await CalculateRatingRequirementAsync(userId, requiredRating);
                            }
                        }
                        else if (criterion.CriteriaName.Contains("Project", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(criterion.CriteriaValue, out var requiredProjects))
                            {
                                requirement = await CalculateProjectCountRequirementAsync(userId, requiredProjects);
                            }
                        }
                    }

                    requirements.Add(requirement);
                }

                // If no criteria defined, create a default requirement
                if (!requirements.Any())
                {
                    requirements.Add(new BadgeRequirementProgressDto
                    {
                        Name = "Manual Verification",
                        Description = definition.Description,
                        IsMet = false,
                        ProgressPercentage = 0m,
                        Current = 0,
                        Required = 1,
                        Unit = "approval"
                    });
                }
                break;
        }

        progress.Requirements = requirements;

        if (requirements.Any())
        {
            progress.ProgressPercentage = requirements.Average(r => r.ProgressPercentage);
            progress.IsEligible = requirements.All(r => r.IsMet);
        }

        return progress;
    }

    private async Task<BadgeRequirementProgressDto> CalculateRatingRequirementAsync(Guid userId, decimal requiredRating)
    {
        // PERFORMANCE FIX: Add AsNoTracking for read-only query
        var reputationScore = await _context.UserReputationScores
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId);

        var currentRating = reputationScore?.OverallScore ?? 3.0m; // Default if no score exists

        return new BadgeRequirementProgressDto
        {
            Name = "Average Rating",
            Description = $"Maintain {requiredRating}+ average rating",
            Current = currentRating,
            Required = requiredRating,
            Unit = "stars",
            IsMet = currentRating >= requiredRating,
            ProgressPercentage = Math.Min(100, (currentRating / requiredRating) * 100)
        };
    }

    private async Task<BadgeRequirementProgressDto> CalculateProjectCountRequirementAsync(Guid userId, int requiredCount)
    {
        // PERFORMANCE FIX: CountAsync is already optimal (no need for AsNoTracking with aggregations)
        var completedProjects = await _context.Projects
            .Where(p => (p.ClientId == userId || p.ProviderId == userId) && p.Status == ProjectStatus.Completed)
            .CountAsync();

        return new BadgeRequirementProgressDto
        {
            Name = "Completed Projects",
            Description = $"Complete {requiredCount} projects",
            Current = completedProjects,
            Required = requiredCount,
            Unit = "projects",
            IsMet = completedProjects >= requiredCount,
            ProgressPercentage = Math.Min(100, ((decimal)completedProjects / requiredCount) * 100)
        };
    }

    private async Task<BadgeRequirementProgressDto> CalculateAccountAgeRequirementAsync(Guid userId, int requiredDays)
    {
        // BUG-BE-001 FIX: Add null check to prevent NullReferenceException
        // PERFORMANCE FIX: Use FirstOrDefaultAsync with AsNoTracking instead of FindAsync for consistency
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("Cannot calculate account age for non-existent user {UserId}", userId);
            throw new InvalidOperationException($"User {userId} not found");
        }

        var accountAge = (DateTime.UtcNow - user.CreatedAt).Days;

        return new BadgeRequirementProgressDto
        {
            Name = "Account Age",
            Description = $"Account active for {requiredDays} days",
            Current = accountAge,
            Required = requiredDays,
            Unit = "days",
            IsMet = accountAge >= requiredDays,
            ProgressPercentage = Math.Min(100, ((decimal)accountAge / requiredDays) * 100)
        };
    }
}