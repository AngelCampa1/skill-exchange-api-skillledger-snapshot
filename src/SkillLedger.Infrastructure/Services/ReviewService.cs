using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services;

// Audit action constants
internal static class ReviewAuditActions
{
    public const string ReviewSubmitted = "REVIEW_SUBMITTED";
    public const string ReviewRetracted = "REVIEW_RETRACTED";
    public const string ReviewResponseAdded = "REVIEW_RESPONSE_ADDED";
    public const string ReviewFlagged = "REVIEW_FLAGGED";
}

public class ReviewService : IReviewService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IContentModerationService _contentModerationService;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        SkillLedgerDbContext context,
        IAuditLogService auditLogService,
        IContentModerationService contentModerationService,
        ILogger<ReviewService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _contentModerationService = contentModerationService;
        _logger = logger;
    }

    public async Task<ReviewResponseDto> SubmitReviewAsync(CreateReviewDto createDto, Guid reviewerId, string ipAddress)
    {
        try
        {
            _logger.LogInformation("Attempting to submit review for project {ProjectId} by user {ReviewerId}",
                createDto.ProjectId, reviewerId);

            // Validate business rules
            var validationResult = await ValidateReviewSubmissionAsync(createDto, reviewerId);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            // Check for existing review
            var existingReview = await _context.ProjectReviews
                .FirstOrDefaultAsync(r => r.ProjectId == createDto.ProjectId &&
                                        r.ReviewerId == reviewerId &&
                                        r.Type == createDto.Type);

            if (existingReview != null)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review already submitted for this project and type"
                };
            }

            // BUG-BE-009 FIX: Validate review text length before database insertion
            var reviewText = createDto.ReviewText.Trim();
            if (reviewText.Length < 25)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review text must be at least 25 characters long"
                };
            }
            if (reviewText.Length > 2000)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review text cannot exceed 2000 characters"
                };
            }

            // Create new review
            var review = new ProjectReview
            {
                ProjectId = createDto.ProjectId,
                ReviewerId = reviewerId,
                RevieweeId = createDto.RevieweeId,
                Type = createDto.Type,
                OverallRating = createDto.OverallRating,
                QualityRating = createDto.QualityRating,
                CommunicationRating = createDto.CommunicationRating,
                TimelinessRating = createDto.TimelinessRating,
                ProfessionalismRating = createDto.ProfessionalismRating,
                ReviewText = reviewText,
                PhotoAttachmentCount = createDto.PhotoAttachmentIds.Count,
                HasPhotoAttachments = createDto.PhotoAttachmentIds.Count > 0
            };

            // Submit the review (changes to SubmittedBlind status)
            review.Submit(ipAddress);

            _context.ProjectReviews.Add(review);
            await _context.SaveChangesAsync();

            // Handle photo attachments if provided
            if (createDto.PhotoAttachmentIds.Count > 0)
            {
                await AttachPhotosToReviewAsync(review.Id, createDto.PhotoAttachmentIds);
            }

            // Log the submission
            await _auditLogService.LogEventAsync(reviewerId, ReviewAuditActions.ReviewSubmitted, ipAddress, null, true,
                $"Submitted review for project {createDto.ProjectId}, type: {createDto.Type}");

            // Process blind review system - check if counterpart has also submitted
            await ProcessBlindReviewsAsync(createDto.ProjectId);

            _logger.LogInformation("Review submitted successfully with ID {ReviewId}", review.Id);

            return new ReviewResponseDto
            {
                Success = true,
                Message = "Review submitted successfully",
                ReviewId = review.Id,
                Status = review.Status
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review for project {ProjectId} by user {ReviewerId}",
                createDto.ProjectId, reviewerId);

            await _auditLogService.LogEventAsync(reviewerId, ReviewAuditActions.ReviewSubmitted, ipAddress, null, false,
                "Review submission failed", ex.Message);

            return new ReviewResponseDto
            {
                Success = false,
                Message = "An error occurred while submitting the review"
            };
        }
    }

    public async Task<List<ReviewDisplayDto>> GetProjectReviewsAsync(Guid projectId, Guid requesterId)
    {
        try
        {
            // PERFORMANCE FIX: Add AsSplitQuery for multiple Includes + AsNoTracking for read-only query
            var reviews = await _context.ProjectReviews
                .AsNoTracking()
                .Where(r => r.ProjectId == projectId && r.Status == ProjectReviewStatus.Published)
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .Include(r => r.Project)
                .Include(r => r.PhotoAttachments)
                .AsSplitQuery()
                .OrderByDescending(r => r.PublishedAt)
                .Select(r => new ReviewDisplayDto
                {
                    Id = r.Id,
                    ProjectId = r.ProjectId,
                    ProjectTitle = r.Project.Title,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer.UserName!,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee.UserName!,
                    Type = r.Type,
                    OverallRating = r.OverallRating,
                    QualityRating = r.QualityRating,
                    CommunicationRating = r.CommunicationRating,
                    TimelinessRating = r.TimelinessRating,
                    ProfessionalismRating = r.ProfessionalismRating,
                    CalculatedAverageRating = r.CalculatedAverageRating,
                    ReviewText = r.ReviewText,
                    ResponseText = r.ResponseText,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    PublishedAt = r.PublishedAt,
                    HasPhotoAttachments = r.HasPhotoAttachments,
                    PhotoAttachmentCount = r.PhotoAttachmentCount,
                    PhotoAttachments = r.PhotoAttachments.Select(p => new ReviewPhotoDto
                    {
                        Id = p.Id,
                        FileName = p.FileName,
                        Url = p.BlobName ?? "",
                        FileSize = p.FileSizeBytes
                    }).ToList()
                })
                .ToListAsync();

            return reviews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for project {ProjectId}", projectId);
            return new List<ReviewDisplayDto>();
        }
    }

    public async Task<(List<ReviewDisplayDto> Reviews, int TotalCount)> GetUserReviewsAsync(
        Guid userId,
        Guid requesterId,
        ProjectReviewType? reviewType = null,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var query = _context.ProjectReviews
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published);

            if (reviewType.HasValue)
            {
                query = query.Where(r => r.Type == reviewType.Value);
            }

            var totalCount = await query.CountAsync();

            // PERFORMANCE FIX: Add AsSplitQuery for multiple Includes + AsNoTracking for read-only query
            var reviews = await query
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .Include(r => r.Project)
                .Include(r => r.PhotoAttachments)
                .AsSplitQuery()
                .OrderByDescending(r => r.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDisplayDto
                {
                    Id = r.Id,
                    ProjectId = r.ProjectId,
                    ProjectTitle = r.Project.Title,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer.UserName!,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee.UserName!,
                    Type = r.Type,
                    OverallRating = r.OverallRating,
                    QualityRating = r.QualityRating,
                    CommunicationRating = r.CommunicationRating,
                    TimelinessRating = r.TimelinessRating,
                    ProfessionalismRating = r.ProfessionalismRating,
                    CalculatedAverageRating = r.CalculatedAverageRating,
                    ReviewText = r.ReviewText,
                    ResponseText = r.ResponseText,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    PublishedAt = r.PublishedAt,
                    HasPhotoAttachments = r.HasPhotoAttachments,
                    PhotoAttachmentCount = r.PhotoAttachmentCount,
                    PhotoAttachments = r.PhotoAttachments.Select(p => new ReviewPhotoDto
                    {
                        Id = p.Id,
                        FileName = p.FileName,
                        Url = p.BlobName ?? "",
                        FileSize = p.FileSizeBytes
                    }).ToList()
                })
                .ToListAsync();

            return (reviews, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for user {UserId}", userId);
            return (new List<ReviewDisplayDto>(), 0);
        }
    }

    public async Task<ReviewSummaryDto?> GetUserReviewSummaryAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            var reviews = await _context.ProjectReviews
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published)
                .ToListAsync();

            if (!reviews.Any())
                return new ReviewSummaryDto { UserId = userId, UserName = user.UserName ?? "" };

            var summary = new ReviewSummaryDto
            {
                UserId = userId,
                UserName = user.UserName ?? "",
                TotalReviewsReceived = reviews.Count,
                AverageOverallRating = reviews.Average(r => r.OverallRating),
                ClientReviewsCount = reviews.Count(r => r.Type == ProjectReviewType.ClientToProvider),
                ProviderReviewsCount = reviews.Count(r => r.Type == ProjectReviewType.ProviderToClient),
                MostRecentReviewDate = reviews.Max(r => r.PublishedAt)
            };

            // Calculate dimensional averages only for reviews that have these ratings
            var reviewsWithQuality = reviews.Where(r => r.QualityRating.HasValue).ToList();
            if (reviewsWithQuality.Any())
                summary.AverageQualityRating = reviewsWithQuality.Average(r => r.QualityRating!.Value);

            var reviewsWithCommunication = reviews.Where(r => r.CommunicationRating.HasValue).ToList();
            if (reviewsWithCommunication.Any())
                summary.AverageCommunicationRating = reviewsWithCommunication.Average(r => r.CommunicationRating!.Value);

            var reviewsWithTimeliness = reviews.Where(r => r.TimelinessRating.HasValue).ToList();
            if (reviewsWithTimeliness.Any())
                summary.AverageTimelinessRating = reviewsWithTimeliness.Average(r => r.TimelinessRating!.Value);

            var reviewsWithProfessionalism = reviews.Where(r => r.ProfessionalismRating.HasValue).ToList();
            if (reviewsWithProfessionalism.Any())
                summary.AverageProfessionalismRating = reviewsWithProfessionalism.Average(r => r.ProfessionalismRating!.Value);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review summary for user {UserId}", userId);
            return null;
        }
    }

    public async Task<BlindReviewStatusDto?> GetBlindReviewStatusAsync(Guid projectId, Guid userId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                return null;

            // Determine user's role and review types
            bool isClient = project.ClientId == userId;
            if (!isClient)
            {
                // Check if user is the provider (could be determined through provider selection, etc.)
                // For now, we'll allow any user to check status
            }

            var userReviewType = isClient ? ProjectReviewType.ClientToProvider : ProjectReviewType.ProviderToClient;
            var counterpartReviewType = isClient ? ProjectReviewType.ProviderToClient : ProjectReviewType.ClientToProvider;

            var userReview = await _context.ProjectReviews
                .FirstOrDefaultAsync(r => r.ProjectId == projectId &&
                                        r.ReviewerId == userId &&
                                        r.Type == userReviewType);

            var counterpartReview = await _context.ProjectReviews
                .FirstOrDefaultAsync(r => r.ProjectId == projectId &&
                                        r.Type == counterpartReviewType);

            return new BlindReviewStatusDto
            {
                ProjectId = projectId,
                ReviewType = userReviewType,
                HasUserSubmittedReview = userReview != null,
                HasCounterpartSubmittedReview = counterpartReview != null,
                AreReviewsPublished = userReview?.Status == ProjectReviewStatus.Published,
                UserSubmissionDate = userReview?.SubmittedAt,
                CounterpartSubmissionDate = counterpartReview?.SubmittedAt,
                PublishDate = userReview?.PublishedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blind review status for project {ProjectId}", projectId);
            return null;
        }
    }

    public async Task<ReviewResponseDto> RetractReviewAsync(Guid reviewId, Guid userId, string ipAddress)
    {
        try
        {
            var review = await _context.ProjectReviews.FindAsync(reviewId);
            if (review == null)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            if (review.ReviewerId != userId)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "You are not authorized to retract this review"
                };
            }

            if (!review.CanBeRetracted)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review cannot be retracted in its current state"
                };
            }

            review.Retract();
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(userId, ReviewAuditActions.ReviewRetracted, ipAddress, null, true,
                $"Retracted review {reviewId}");

            return new ReviewResponseDto
            {
                Success = true,
                Message = "Review retracted successfully",
                Status = ProjectReviewStatus.Retracted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retracting review {ReviewId}", reviewId);
            return new ReviewResponseDto
            {
                Success = false,
                Message = "An error occurred while retracting the review"
            };
        }
    }

    public async Task<ReviewResponseDto> AddReviewResponseAsync(AddReviewResponseDto responseDto, Guid userId, string ipAddress)
    {
        try
        {
            var review = await _context.ProjectReviews.FindAsync(responseDto.ReviewId);
            if (review == null)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            if (review.RevieweeId != userId)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "You are not authorized to respond to this review"
                };
            }

            if (review.Status != ProjectReviewStatus.Published)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Can only respond to published reviews"
                };
            }

            review.ResponseText = responseDto.ResponseText.Trim();
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(userId, ReviewAuditActions.ReviewResponseAdded, ipAddress, null, true,
                $"Added response to review {responseDto.ReviewId}");

            return new ReviewResponseDto
            {
                Success = true,
                Message = "Response added successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding response to review {ReviewId}", responseDto.ReviewId);
            return new ReviewResponseDto
            {
                Success = false,
                Message = "An error occurred while adding the response"
            };
        }
    }

    public async Task<ReviewResponseDto> UpdateReviewPhotosAsync(Guid reviewId, List<Guid> photoIds, Guid userId)
    {
        try
        {
            var review = await _context.ProjectReviews
                .Include(r => r.PhotoAttachments)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            if (review.ReviewerId != userId)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "You are not authorized to update this review"
                };
            }

            if (!review.IsEditable)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review photos cannot be updated in its current state"
                };
            }

            await AttachPhotosToReviewAsync(reviewId, photoIds);

            return new ReviewResponseDto
            {
                Success = true,
                Message = "Review photos updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photos for review {ReviewId}", reviewId);
            return new ReviewResponseDto
            {
                Success = false,
                Message = "An error occurred while updating review photos"
            };
        }
    }

    public async Task<ReviewResponseDto> FlagReviewAsync(Guid reviewId, string reason, Guid reporterId, string ipAddress)
    {
        try
        {
            var review = await _context.ProjectReviews.FindAsync(reviewId);
            if (review == null)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            review.FlagForModeration(reason);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(reporterId, ReviewAuditActions.ReviewFlagged, ipAddress, null, true,
                $"Flagged review {reviewId} for moderation: {reason}");

            return new ReviewResponseDto
            {
                Success = true,
                Message = "Review flagged for moderation"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging review {ReviewId}", reviewId);
            return new ReviewResponseDto
            {
                Success = false,
                Message = "An error occurred while flagging the review"
            };
        }
    }

    public async Task<ReviewDisplayDto?> GetReviewByIdAsync(Guid reviewId, Guid requesterId)
    {
        try
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var review = await _context.ProjectReviews
                .Include(r => r.Reviewer)
                .Include(r => r.Reviewee)
                .Include(r => r.Project)
                .Include(r => r.PhotoAttachments)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                return null;

            // Check authorization - only published reviews are publicly visible
            if (review.Status != ProjectReviewStatus.Published)
            {
                // Only the reviewer and reviewee can see non-published reviews
                if (requesterId != review.ReviewerId && requesterId != review.RevieweeId)
                    return null;
            }

            return new ReviewDisplayDto
            {
                Id = review.Id,
                ProjectId = review.ProjectId,
                ProjectTitle = review.Project.Title,
                ReviewerId = review.ReviewerId,
                ReviewerName = review.Reviewer.UserName!,
                RevieweeId = review.RevieweeId,
                RevieweeName = review.Reviewee.UserName!,
                Type = review.Type,
                OverallRating = review.OverallRating,
                QualityRating = review.QualityRating,
                CommunicationRating = review.CommunicationRating,
                TimelinessRating = review.TimelinessRating,
                ProfessionalismRating = review.ProfessionalismRating,
                CalculatedAverageRating = review.CalculatedAverageRating,
                ReviewText = review.ReviewText,
                ResponseText = review.ResponseText,
                Status = review.Status,
                CreatedAt = review.CreatedAt,
                PublishedAt = review.PublishedAt,
                HasPhotoAttachments = review.HasPhotoAttachments,
                PhotoAttachmentCount = review.PhotoAttachmentCount,
                PhotoAttachments = review.PhotoAttachments.Select(p => new ReviewPhotoDto
                {
                    Id = p.Id,
                    FileName = p.FileName,
                    Url = p.BlobName ?? "",
                    FileSize = p.FileSizeBytes
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {ReviewId}", reviewId);
            return null;
        }
    }

    public async Task<bool> CanSubmitReviewAsync(Guid projectId, Guid reviewerId, Guid revieweeId, ProjectReviewType reviewType)
    {
        try
        {
            // Check if review already exists
            var existingReview = await _context.ProjectReviews
                .AnyAsync(r => r.ProjectId == projectId &&
                             r.ReviewerId == reviewerId &&
                             r.Type == reviewType);

            if (existingReview)
                return false;

            return await IsValidProjectCounterpartyReviewAsync(projectId, reviewerId, revieweeId, reviewType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking review eligibility");
            return false;
        }
    }

    public async Task<bool> ProcessBlindReviewsAsync(Guid projectId)
    {
        try
        {
            var reviews = await _context.ProjectReviews
                .Where(r => r.ProjectId == projectId && r.Status == ProjectReviewStatus.SubmittedBlind)
                .ToListAsync();

            // Check if we have both types of reviews submitted
            var clientToProvider = reviews.FirstOrDefault(r => r.Type == ProjectReviewType.ClientToProvider);
            var providerToClient = reviews.FirstOrDefault(r => r.Type == ProjectReviewType.ProviderToClient);

            if (clientToProvider != null && providerToClient != null)
            {
                // VULN-031 FIX: Normalize PublishedAt timestamp to prevent timing disclosure
                // Both reviews published at the exact same time to hide submission order
                var normalizedPublishTime = DateTime.UtcNow;

                clientToProvider.Publish();
                providerToClient.Publish();

                // Override PublishedAt to be identical for both reviews
                clientToProvider.PublishedAt = normalizedPublishTime;
                providerToClient.PublishedAt = normalizedPublishTime;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Published blind reviews for project {ProjectId}", projectId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing blind reviews for project {ProjectId}", projectId);
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<ReviewResponseDto> ValidateReviewSubmissionAsync(CreateReviewDto createDto, Guid reviewerId)
    {
        // Self-review check
        if (reviewerId == createDto.RevieweeId)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "You cannot review yourself"
            };
        }

        // Check if project exists and is completed
        var project = await _context.Projects.FindAsync(createDto.ProjectId);
        if (project == null)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "Project not found"
            };
        }

        if (project.Status != ProjectStatus.Completed)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "Reviews can only be submitted for completed projects"
            };
        }

        var providerId = await GetProjectProviderIdAsync(project);
        if (!providerId.HasValue)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "Project does not have an assigned provider"
            };
        }

        var isParticipant = reviewerId == project.ClientId || reviewerId == providerId.Value;
        if (!isParticipant)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "Only project participants can submit reviews"
            };
        }

        var isValidCounterpartyReview = createDto.Type switch
        {
            ProjectReviewType.ClientToProvider =>
                reviewerId == project.ClientId && createDto.RevieweeId == providerId.Value,
            ProjectReviewType.ProviderToClient =>
                reviewerId == providerId.Value && createDto.RevieweeId == project.ClientId,
            _ => false
        };

        if (!isValidCounterpartyReview)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "Review type and reviewee must match the project counterparty"
            };
        }

        // Check if reviewee exists
        var reviewee = await _context.Users.FindAsync(createDto.RevieweeId);
        if (reviewee == null)
        {
            return new ReviewResponseDto
            {
                Success = false,
                Message = "Reviewee not found"
            };
        }

        return new ReviewResponseDto { Success = true };
    }

    private async Task<bool> IsValidProjectCounterpartyReviewAsync(
        Guid projectId,
        Guid reviewerId,
        Guid revieweeId,
        ProjectReviewType reviewType)
    {
        if (reviewerId == revieweeId)
            return false;

        var project = await _context.Projects.FindAsync(projectId);
        if (project == null || project.Status != ProjectStatus.Completed)
            return false;

        var providerId = await GetProjectProviderIdAsync(project);
        if (!providerId.HasValue)
            return false;

        return reviewType switch
        {
            ProjectReviewType.ClientToProvider =>
                reviewerId == project.ClientId && revieweeId == providerId.Value,
            ProjectReviewType.ProviderToClient =>
                reviewerId == providerId.Value && revieweeId == project.ClientId,
            _ => false
        };
    }

    private async Task<Guid?> GetProjectProviderIdAsync(Project project)
    {
        if (project.ProviderId.HasValue)
            return project.ProviderId.Value;

        var workspace = await _context.ProjectWorkspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.ProjectId == project.Id && w.ClientId == project.ClientId);

        return workspace?.ProviderId;
    }

    private async Task AttachPhotosToReviewAsync(Guid reviewId, List<Guid> photoIds)
    {
        if (!photoIds.Any())
            return;

        // Update photo attachments to reference the review
        var photos = await _context.UploadedFiles
            .Where(f => photoIds.Contains(f.Id))
            .ToListAsync();

        foreach (var photo in photos)
        {
            // Add relationship - this would depend on your UploadedFile entity structure
            // For now, we'll assume there's a ProjectReviewId foreign key
        }

        // Update review photo counts
        var review = await _context.ProjectReviews.FindAsync(reviewId);
        if (review != null)
        {
            review.PhotoAttachmentCount = photos.Count;
            review.HasPhotoAttachments = photos.Count > 0;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region New Methods for Controller Support

    public async Task<PaginatedReviewsDto> GetUserReviewsAsync(Guid userId, ReviewFilterDto filter)
    {
        try
        {
            var query = _context.ProjectReviews
                .Where(r => r.RevieweeId == userId && r.Status == ProjectReviewStatus.Published);

            if (filter.ReviewType.HasValue)
            {
                query = query.Where(r => r.Type == filter.ReviewType.Value);
            }

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy.ToLower() switch
            {
                "rating" => filter.SortDescending
                    ? query.OrderByDescending(r => r.OverallRating)
                    : query.OrderBy(r => r.OverallRating),
                "createdat" or _ => filter.SortDescending
                    ? query.OrderByDescending(r => r.CreatedAt)
                    : query.OrderBy(r => r.CreatedAt)
            };

            // Note: Multiple Include statements but uses Select projection - no cartesian explosion
            var reviews = await query
                .Include(r => r.Reviewer)
                .Include(r => r.Project)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new ReviewDisplayDto
                {
                    Id = r.Id,
                    ProjectId = r.ProjectId,
                    ProjectTitle = r.Project.Title,
                    ReviewerId = r.ReviewerId,
                    ReviewerName = r.Reviewer.UserName!,
                    RevieweeId = r.RevieweeId,
                    RevieweeName = r.Reviewee.UserName!,
                    Type = r.Type,
                    OverallRating = r.OverallRating,
                    QualityRating = r.QualityRating,
                    CommunicationRating = r.CommunicationRating,
                    TimelinessRating = r.TimelinessRating,
                    ProfessionalismRating = r.ProfessionalismRating,
                    CalculatedAverageRating = r.CalculatedAverageRating,
                    ReviewText = r.ReviewText,
                    ResponseText = r.ResponseText,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    PublishedAt = r.PublishedAt,
                    HasPhotoAttachments = r.HasPhotoAttachments,
                    PhotoAttachmentCount = r.PhotoAttachmentCount
                })
                .ToListAsync();

            var statistics = await GetUserReviewSummaryAsync(userId);

            return new PaginatedReviewsDto
            {
                Reviews = reviews,
                TotalCount = totalCount,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user reviews for {UserId}", userId);
            return new PaginatedReviewsDto();
        }
    }

    public async Task<ProjectReviewsDto> GetProjectReviewsWithStatusAsync(Guid projectId, Guid userId)
    {
        try
        {
            var reviews = await GetProjectReviewsAsync(projectId, userId);

            // Check submission permissions
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return new ProjectReviewsDto
                {
                    Success = false,
                    Message = "Project not found"
                };
            }

            // Get the workspace to find the provider
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(w => w.ProjectId == projectId);

            var canSubmitClientReview = project.ClientId == userId &&
                !await _context.ProjectReviews.AnyAsync(r => r.ProjectId == projectId &&
                    r.ReviewerId == userId && r.Type == ProjectReviewType.ClientToProvider);

            var canSubmitProviderReview = workspace != null && workspace.ProviderId == userId &&
                !await _context.ProjectReviews.AnyAsync(r => r.ProjectId == projectId &&
                    r.ReviewerId == userId && r.Type == ProjectReviewType.ProviderToClient);

            return new ProjectReviewsDto
            {
                Success = true,
                Reviews = reviews,
                CanSubmitClientReview = canSubmitClientReview,
                CanSubmitProviderReview = canSubmitProviderReview
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project reviews for {ProjectId}", projectId);
            return new ProjectReviewsDto
            {
                Success = false,
                Message = "An error occurred while fetching project reviews"
            };
        }
    }

    public async Task<ReviewResponseDto> AddReviewResponseAsync(Guid reviewId, string response, Guid userId, string ipAddress)
    {
        try
        {
            var review = await _context.ProjectReviews
                .Include(r => r.Reviewee)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "Review not found"
                };
            }

            if (review.RevieweeId != userId)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "You can only respond to reviews about you"
                };
            }

            if (review.Status != ProjectReviewStatus.Published)
            {
                return new ReviewResponseDto
                {
                    Success = false,
                    Message = "You can only respond to published reviews"
                };
            }

            review.ResponseText = response;
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(userId, ReviewAuditActions.ReviewResponseAdded, ipAddress, null, true,
                $"Added response to review {reviewId}");

            return new ReviewResponseDto
            {
                Success = true,
                Message = "Response added successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding response to review {ReviewId}", reviewId);
            return new ReviewResponseDto
            {
                Success = false,
                Message = "An error occurred while adding the response"
            };
        }
    }

    public async Task<ReviewSummaryDto> GetReviewStatisticsAsync(Guid userId)
    {
        return await GetUserReviewSummaryAsync(userId) ?? new ReviewSummaryDto
        {
            UserId = userId,
            UserName = "Unknown"
        };
    }

    public async Task<FileUploadResultDto> UploadReviewEvidenceAsync(Guid projectId, List<object> files, Guid userId, string ipAddress)
    {
        try
        {
            // This is a placeholder implementation
            // In a real scenario, you would integrate with the file storage service
            var fileIds = new List<Guid>();

            // BUG-CRIT-004 FIX: Use proper type checking instead of unsafe Cast<dynamic>()
            foreach (var fileObj in files)
            {
                // Validate file object has required properties (duck typing for file-like objects)
                if (fileObj == null)
                {
                    _logger.LogWarning("Null file object encountered in upload");
                    continue;
                }

                var fileType = fileObj.GetType();
                var lengthProp = fileType.GetProperty("Length");
                var fileNameProp = fileType.GetProperty("FileName");

                if (lengthProp == null || fileNameProp == null)
                {
                    _logger.LogWarning("Invalid file object type: {FileType} - missing Length or FileName property", fileType.Name);
                    return new FileUploadResultDto
                    {
                        Success = false,
                        Message = "Invalid file object - must have Length and FileName properties"
                    };
                }

                var length = (long?)lengthProp.GetValue(fileObj) ?? 0;
                var fileName = fileNameProp.GetValue(fileObj)?.ToString() ?? "";

                if (length > 10 * 1024 * 1024) // 10MB limit
                {
                    return new FileUploadResultDto
                    {
                        Success = false,
                        Message = $"File {fileName} exceeds the 10MB size limit"
                    };
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(fileName).ToLower();

                if (!((IList<string>)allowedExtensions).Contains(fileExtension))
                {
                    return new FileUploadResultDto
                    {
                        Success = false,
                        Message = $"File type {fileExtension} is not allowed"
                    };
                }

                // Generate a file ID (in real implementation, save to storage)
                fileIds.Add(Guid.NewGuid());
            }

            await _auditLogService.LogEventAsync(userId, "REVIEW_EVIDENCE_UPLOADED", ipAddress, null, true,
                $"Uploaded {fileIds.Count} evidence files for project {projectId}");

            return new FileUploadResultDto
            {
                Success = true,
                Message = $"Successfully uploaded {fileIds.Count} files",
                FileIds = fileIds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading review evidence for project {ProjectId}", projectId);
            return new FileUploadResultDto
            {
                Success = false,
                Message = "An error occurred while uploading files"
            };
        }
    }

    #endregion
}
