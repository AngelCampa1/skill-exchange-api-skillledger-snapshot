using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for project review management following US-5.1.1 specifications
/// </summary>
[Authorize]
public class ReviewController : BaseApiController
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a project review (Client-to-Provider or Provider-to-Client)
    /// </summary>
    [HttpPost("submit")]
    [EnableRateLimiting("ReviewSubmissionPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview([FromBody] CreateReviewDto createDto)
    {
        try
        {
            var reviewerId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();

            _logger.LogInformation("Review submission attempt for project {ProjectId} by user {ReviewerId}",
                createDto.ProjectId, reviewerId);

            var result = await _reviewService.SubmitReviewAsync(createDto, reviewerId, ipAddress);

            if (result.Success)
            {
                _logger.LogInformation("Review submitted successfully: {ReviewId}", result.ReviewId);
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    reviewId = result.ReviewId
                });
            }

            _logger.LogWarning("Review submission failed for project {ProjectId}: {Message}",
                createDto.ProjectId, result.Message);
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting review for project {ProjectId}", createDto.ProjectId);
            return StatusCode(500, new { success = false, message = "An error occurred while submitting the review" });
        }
    }

    /// <summary>
    /// Retract a review (only allowed before counterpart submits)
    /// </summary>
    [HttpDelete("{reviewId:guid}")]
    [EnableRateLimiting("ReviewActionPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetractReview(Guid reviewId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();

            _logger.LogInformation("Review retraction attempt for review {ReviewId} by user {UserId}",
                reviewId, userId);

            var result = await _reviewService.RetractReviewAsync(reviewId, userId, ipAddress);

            if (result.Success)
            {
                _logger.LogInformation("Review retracted successfully: {ReviewId}", reviewId);
                return Ok(new { success = true, message = result.Message });
            }

            _logger.LogWarning("Review retraction failed for review {ReviewId}: {Message}",
                reviewId, result.Message);
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retracting review {ReviewId}", reviewId);
            return StatusCode(500, new { success = false, message = "An error occurred while retracting the review" });
        }
    }

    /// <summary>
    /// Get public reviews for a user (displayed on profiles)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<IActionResult> GetUserReviews(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ProjectReviewType? type = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] bool sortDescending = true)
    {
        try
        {
            if (pageSize > 50) pageSize = 50; // Limit page size

            _logger.LogDebug("Fetching public reviews for user {UserId}, page {Page}, type {Type}",
                userId, page, type);

            var result = await _reviewService.GetUserReviewsAsync(userId, new ReviewFilterDto
            {
                Page = page,
                PageSize = pageSize,
                ReviewType = type,
                SortBy = sortBy,
                SortDescending = sortDescending,
                PublicOnly = true
            });

            // Add pagination headers
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());
            Response.Headers.Append("X-Page-Number", page.ToString());
            Response.Headers.Append("X-Total-Pages",
                Math.Ceiling((double)result.TotalCount / pageSize).ToString());

            return Ok(new
            {
                success = true,
                data = result.Reviews,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = result.TotalCount,
                    totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize)
                },
                statistics = result.Statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching reviews" });
        }
    }

    /// <summary>
    /// Get reviews for a specific project (for involved parties only)
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<IActionResult> GetProjectReviews(Guid projectId)
    {
        try
        {
            var userId = GetCurrentUserId();

            _logger.LogDebug("Fetching project reviews for project {ProjectId} by user {UserId}",
                projectId, userId);

            var result = await _reviewService.GetProjectReviewsWithStatusAsync(projectId, userId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Reviews,
                    canSubmitClientReview = result.CanSubmitClientReview,
                    canSubmitProviderReview = result.CanSubmitProviderReview
                });
            }

            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project reviews for project {ProjectId}", projectId);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching project reviews" });
        }
    }

    /// <summary>
    /// Add a response to a review
    /// </summary>
    [HttpPost("{reviewId:guid}/respond")]
    [EnableRateLimiting("ReviewActionPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondToReview(Guid reviewId, [FromBody] ReviewResponseRequestDto responseDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();

            _logger.LogInformation("Review response submission for review {ReviewId} by user {UserId}",
                reviewId, userId);

            var result = await _reviewService.AddReviewResponseAsync(reviewId, responseDto.Response, userId, ipAddress);

            if (result.Success)
            {
                _logger.LogInformation("Review response submitted successfully for review {ReviewId}", reviewId);
                return Ok(new { success = true, message = result.Message });
            }

            _logger.LogWarning("Review response failed for review {ReviewId}: {Message}",
                reviewId, result.Message);
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to review {ReviewId}", reviewId);
            return StatusCode(500, new { success = false, message = "An error occurred while responding to the review" });
        }
    }

    /// <summary>
    /// Flag a review for moderation
    /// </summary>
    [HttpPost("{reviewId:guid}/flag")]
    [EnableRateLimiting("ReviewActionPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FlagReview(Guid reviewId, [FromBody] FlagReviewRequestDto flagDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();

            _logger.LogInformation("Review flagging attempt for review {ReviewId} by user {UserId}",
                reviewId, userId);

            var result = await _reviewService.FlagReviewAsync(reviewId, flagDto.Reason, userId, ipAddress);

            if (result.Success)
            {
                _logger.LogInformation("Review flagged successfully: {ReviewId}", reviewId);
                return Ok(new { success = true, message = result.Message });
            }

            _logger.LogWarning("Review flagging failed for review {ReviewId}: {Message}",
                reviewId, result.Message);
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging review {ReviewId}", reviewId);
            return StatusCode(500, new { success = false, message = "An error occurred while flagging the review" });
        }
    }

    /// <summary>
    /// Get review statistics for a user
    /// </summary>
    [HttpGet("statistics/{userId:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting("GeneralApiPolicy")]
    public async Task<IActionResult> GetReviewStatistics(Guid userId)
    {
        try
        {
            _logger.LogDebug("Fetching review statistics for user {UserId}", userId);

            var statistics = await _reviewService.GetReviewStatisticsAsync(userId);

            return Ok(new
            {
                success = true,
                data = statistics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching review statistics for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "An error occurred while fetching review statistics" });
        }
    }

    /// <summary>
    /// Upload evidence files for a review
    /// </summary>
    [HttpPost("evidence/upload")]
    [EnableRateLimiting("FileUploadPolicy")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<IActionResult> UploadReviewEvidence([FromForm] ReviewEvidenceUploadDto uploadDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ipAddress = GetClientIpAddress();

            _logger.LogInformation("Review evidence upload attempt by user {UserId} for project {ProjectId}",
                userId, uploadDto.ProjectId);

            if (uploadDto.Files == null || !uploadDto.Files.Any())
            {
                return BadRequest(new { success = false, message = "No files provided for upload" });
            }

            if (uploadDto.Files.Count() > 5)
            {
                return BadRequest(new { success = false, message = "Maximum 5 files allowed per review" });
            }

            var result = await _reviewService.UploadReviewEvidenceAsync(uploadDto.ProjectId, uploadDto.Files.Cast<object>().ToList(), userId, ipAddress);

            if (result.Success)
            {
                _logger.LogInformation("Review evidence uploaded successfully: {FileIds}",
                    string.Join(", ", result.FileIds));
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    fileIds = result.FileIds
                });
            }

            _logger.LogWarning("Review evidence upload failed: {Message}", result.Message);
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading review evidence for project {ProjectId}", uploadDto?.ProjectId);
            return StatusCode(500, new { success = false, message = "An error occurred while uploading evidence files" });
        }
    }

    #region Helper Methods

    // VULN-005 FIX: Removed duplicate GetCurrentUserId() method
    // Now using standardized implementation from BaseApiController which uses ClaimTypes.NameIdentifier

    private string GetClientIpAddress()
    {
        return SkillLedger.Infrastructure.Services.TrustedClientIpResolver.GetClientIpAddress(HttpContext);
    }

    #endregion
}

/// <summary>
/// DTO for review response requests
/// </summary>
public class ReviewResponseRequestDto
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Response { get; set; } = string.Empty;
}

/// <summary>
/// DTO for flagging reviews
/// </summary>
public class FlagReviewRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for review evidence file uploads
/// </summary>
public class ReviewEvidenceUploadDto
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public IEnumerable<IFormFile> Files { get; set; } = new List<IFormFile>();

    public string? Description { get; set; }
}
