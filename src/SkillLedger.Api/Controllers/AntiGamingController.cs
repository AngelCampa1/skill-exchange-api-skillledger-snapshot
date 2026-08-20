using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for anti-gaming and fraud detection operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AntiGamingController : ControllerBase
{
    private readonly IAntiGamingService _antiGamingService;
    private readonly ILogger<AntiGamingController> _logger;

    public AntiGamingController(
        IAntiGamingService antiGamingService,
        ILogger<AntiGamingController> logger)
    {
        _antiGamingService = antiGamingService;
        _logger = logger;
    }

    /// <summary>
    /// Get user's current risk score
    /// </summary>
    [HttpGet("risk-score")]
    public async Task<ActionResult<UserRiskScoreDto>> GetUserRiskScore()
    {
        try
        {
            var userId = GetCurrentUserId();
            var riskScore = await _antiGamingService.GetUserRiskScoreAsync(userId);

            return Ok(new UserRiskScoreDto
            {
                UserId = userId,
                RiskScore = riskScore,
                AssessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user risk score");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user's current risk score by admin
    /// </summary>
    [HttpGet("risk-score/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserRiskScoreDto>> GetUserRiskScore(Guid userId)
    {
        try
        {
            var riskScore = await _antiGamingService.GetUserRiskScoreAsync(userId);

            return Ok(new UserRiskScoreDto
            {
                UserId = userId,
                RiskScore = riskScore,
                AssessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user risk score for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Analyze user behavior patterns
    /// </summary>
    [HttpPost("analyze-behavior")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GamingRiskAssessmentDto>> AnalyzeUserBehavior([FromBody] AnalyzeUserBehaviorRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var assessment = await _antiGamingService.AnalyzeUserBehaviorAsync(request.UserId);

            var dto = new GamingRiskAssessmentDto
            {
                Id = assessment.Id,
                UserId = assessment.UserId,
                RiskScore = assessment.RiskScore,
                RiskFactors = assessment.RiskFactors,
                DetectedPatterns = assessment.DetectedPatterns,
                AnalyzedAt = assessment.AnalyzedAt,
                ModelVersion = assessment.ModelVersion
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing user behavior for user {UserId}", request.UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Report suspected gaming activity
    /// </summary>
    [HttpPost("report-gaming")]
    public async Task<ActionResult> ReportGamingActivity([FromBody] ReportGamingRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reportingUserId = GetCurrentUserId();

            var success = await _antiGamingService.ReportGamingActivityAsync(
                reportingUserId,
                request.SuspectedUserId,
                request.Reason,
                request.Evidence);

            if (success)
            {
                return Ok(new { Message = "Gaming activity report submitted successfully" });
            }

            return StatusCode(500, "Failed to submit report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting gaming activity");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user behavior metrics
    /// </summary>
    [HttpGet("behavior-metrics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserBehaviorMetricDto>>> GetBehaviorMetrics(
        [FromQuery] Guid userId,
        [FromQuery] string[]? metricNames = null)
    {
        try
        {
            var metrics = await _antiGamingService.CalculateBehaviorMetricsAsync(userId, metricNames);

            var dtos = metrics.Select(m => new UserBehaviorMetricDto
            {
                Id = m.Id,
                UserId = m.UserId,
                MetricName = m.MetricName,
                MetricValue = m.MetricValue,
                CalculationWindow = m.CalculationWindow,
                CalculatedAt = m.CalculatedAt,
                IsAnomaly = m.IsAnomaly
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting behavior metrics for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get suspicious network connections
    /// </summary>
    [HttpGet("network-connections/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserNetworkConnectionDto>>> GetNetworkConnections(Guid userId)
    {
        try
        {
            var connections = await _antiGamingService.DetectSuspiciousConnectionsAsync(userId);

            var dtos = connections.Select(c => new UserNetworkConnectionDto
            {
                Id = c.Id,
                User1Id = c.User1Id,
                User2Id = c.User2Id,
                ConnectionType = c.ConnectionType,
                ConnectionStrength = c.ConnectionStrength,
                DetectedAt = c.DetectedAt,
                IsValidated = c.IsValidated
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting network connections for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate review authenticity (for testing)
    /// </summary>
    [HttpPost("validate-review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReviewValidationResultDto>> ValidateReview([FromBody] ValidateReviewRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Create a review object for validation
            var review = new ProjectReview
            {
                Id = request.ReviewId ?? Guid.NewGuid(),
                ReviewerId = request.ReviewerId,
                RevieweeId = request.RevieweeId ?? Guid.NewGuid(), // Default for testing
                ProjectId = request.ProjectId,
                OverallRating = request.Rating,
                ReviewText = request.Comment ?? "",
                SubmittedAt = DateTime.UtcNow
            };

            var isAuthentic = await _antiGamingService.ValidateReviewAuthenticityAsync(review);

            return Ok(new ReviewValidationResultDto
            {
                ReviewId = review.Id,
                IsAuthentic = isAuthentic,
                ValidatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating review authenticity");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get anti-gaming alerts (admin only)
    /// </summary>
    [HttpGet("alerts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AntiGamingAlertDto>>> GetAlerts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AlertSeverity? severity = null,
        [FromQuery] AlertStatus? status = null)
    {
        try
        {
            // This would typically be implemented with a dedicated service method
            // For now, we'll return a placeholder response
            return Ok(new List<AntiGamingAlertDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting anti-gaming alerts");
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Unable to determine current user");
        }
        return userId;
    }
}

/// <summary>
/// Request model for analyzing user behavior
/// </summary>
public class AnalyzeUserBehaviorRequest
{
    public Guid UserId { get; set; }
}

/// <summary>
/// Request model for reporting gaming activity
/// </summary>
public class ReportGamingRequest
{
    public Guid SuspectedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object>? Evidence { get; set; }
}

/// <summary>
/// Request model for validating review authenticity
/// </summary>
public class ValidateReviewRequest
{
    public Guid? ReviewId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid? RevieweeId { get; set; }
    public Guid ProjectId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}