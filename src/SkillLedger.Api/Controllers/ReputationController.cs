using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for managing user reputation scores and calculations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReputationController : ControllerBase
{
    private readonly IReputationCalculationService _reputationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ReputationController> _logger;

    public ReputationController(
        IReputationCalculationService reputationService,
        IAuditLogService auditLogService,
        ILogger<ReputationController> logger)
    {
        _reputationService = reputationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get overall reputation score for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User's overall reputation score</returns>
    [HttpGet("user/{userId:guid}/score")]
    public async Task<IActionResult> GetUserReputationScore(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var score = await _reputationService.CalculateOverallReputationScoreAsync(userId);
            if (score == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            return Ok(score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation score for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving the reputation score");
        }
    }

    /// <summary>
    /// Get detailed reputation breakdown for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Detailed breakdown of how reputation score was calculated</returns>
    [HttpGet("user/{userId:guid}/breakdown")]
    public async Task<IActionResult> GetUserReputationBreakdown(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            // Check if user can access this breakdown (own data or admin)
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId && !IsAdmin())
            {
                return StatusCode(403, "You can only view your own reputation breakdown");
            }

            var breakdown = await _reputationService.GetReputationBreakdownAsync(userId);
            if (breakdown == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation breakdown for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving the reputation breakdown");
        }
    }

    /// <summary>
    /// Get all category-specific reputation scores for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of category reputation scores</returns>
    [HttpGet("user/{userId:guid}/categories")]
    public async Task<IActionResult> GetCategoryReputationScores(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var categoryScores = await _reputationService.GetAllCategoryScoresAsync(userId);
            return Ok(categoryScores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category reputation scores for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving category reputation scores");
        }
    }

    /// <summary>
    /// Get reputation score for a specific category
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="categoryId">Skill category ID</param>
    /// <returns>Category-specific reputation score</returns>
    [HttpGet("user/{userId:guid}/category/{categoryId:guid}")]
    public async Task<IActionResult> GetSpecificCategoryScore(Guid userId, Guid categoryId)
    {
        try
        {
            if (userId == Guid.Empty || categoryId == Guid.Empty)
            {
                return BadRequest("Invalid user ID or category ID");
            }

            var categoryScore = await _reputationService.CalculateCategoryReputationScoreAsync(userId, categoryId);
            if (categoryScore == null)
            {
                return NotFound($"Category score not found for user {userId} and category {categoryId}");
            }

            return Ok(categoryScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting specific category score for user {UserId}, category {CategoryId}", userId, categoryId);
            return StatusCode(500, "An error occurred while retrieving the category reputation score");
        }
    }

    /// <summary>
    /// Get reputation history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Number of days to look back (max 365)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size (max 100)</param>
    /// <returns>Historical reputation data</returns>
    [HttpGet("user/{userId:guid}/history")]
    public async Task<IActionResult> GetReputationHistory(Guid userId, int days = 90, int page = 1, int pageSize = 20)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            if (days < 1 || days > 365)
            {
                return BadRequest("Days must be between 1 and 365");
            }

            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Invalid pagination parameters");
            }

            // Limit days to maximum allowed
            days = Math.Min(days, 365);

            var history = await _reputationService.GetReputationHistoryAsync(userId, days, page, pageSize);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation history for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving reputation history");
        }
    }

    /// <summary>
    /// Get reputation trend analysis for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="days">Period to analyze (max 90)</param>
    /// <returns>Trend analysis data</returns>
    [HttpGet("user/{userId:guid}/trend")]
    public async Task<IActionResult> GetReputationTrend(Guid userId, int days = 30)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            if (days < 1 || days > 90)
            {
                return BadRequest("Days must be between 1 and 90");
            }

            var trend = await _reputationService.GetReputationTrendAsync(userId, days);
            if (trend == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reputation trend for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving reputation trend");
        }
    }

    /// <summary>
    /// Recalculate reputation score for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Updated reputation score</returns>
    [HttpPost("user/{userId:guid}/recalculate")]
    public async Task<IActionResult> RecalculateReputationScore(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            // Check authorization - users can only recalculate their own scores
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId && !IsAdmin())
            {
                return StatusCode(403, "You can only recalculate your own reputation score");
            }

            // Check rate limiting - prevent too frequent recalculations
            var existingScore = await _reputationService.CalculateOverallReputationScoreAsync(userId);
            if (existingScore != null && existingScore.LastUpdated > DateTime.UtcNow.AddMinutes(-5))
            {
                return StatusCode(429, "Reputation score was recently updated. Please wait before recalculating again.");
            }

            var updatedScore = await _reputationService.RecalculateAndSaveReputationScoreAsync(userId);
            if (updatedScore == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            // Log the recalculation
            await _auditLogService.LogEventAsync(
                userId,
                "ReputationRecalculated",
                GetClientIpAddress(),
                null,
                true,
                System.Text.Json.JsonSerializer.Serialize(new { updatedScore.OverallScore, updatedScore.TotalProjectsCompleted }),
                null);

            return Ok(updatedScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating reputation score for user {UserId}", userId);
            return StatusCode(500, "An error occurred while recalculating the reputation score");
        }
    }

    /// <summary>
    /// Update category reputation score
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <returns>Updated category reputation score</returns>
    [HttpPost("user/{userId:guid}/category/{categoryId:guid}/update")]
    public async Task<IActionResult> UpdateCategoryScore(Guid userId, Guid categoryId)
    {
        try
        {
            if (userId == Guid.Empty || categoryId == Guid.Empty)
            {
                return BadRequest("Invalid user ID or category ID");
            }

            // Check authorization
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId && !IsAdmin())
            {
                return StatusCode(403, "You can only update your own category scores");
            }

            var updatedScore = await _reputationService.RecalculateAndSaveCategoryScoreAsync(userId, categoryId);
            if (updatedScore == null)
            {
                return NotFound($"Category score not found for user {userId} and category {categoryId}");
            }

            return Ok(updatedScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category score for user {UserId}, category {CategoryId}", userId, categoryId);
            return StatusCode(500, "An error occurred while updating the category reputation score");
        }
    }

    /// <summary>
    /// Bulk recalculate reputation scores for all users (Admin only)
    /// </summary>
    /// <returns>Number of users processed</returns>
    [HttpPost("bulk-recalculate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkRecalculateReputationScores()
    {
        try
        {
            if (!IsAdmin())
            {
                return StatusCode(403, "Only administrators can perform bulk recalculation");
            }

            _logger.LogInformation("Starting bulk reputation recalculation requested by user {UserId}", GetCurrentUserId());

            var processedCount = await _reputationService.BulkRecalculateReputationScoresAsync();

            await _auditLogService.LogEventAsync(
                GetCurrentUserId(),
                "BulkReputationRecalculation",
                GetClientIpAddress(),
                null,
                true,
                System.Text.Json.JsonSerializer.Serialize(new { ProcessedCount = processedCount }),
                null);

            return Ok(new { ProcessedCount = processedCount, Message = $"Successfully recalculated reputation scores for {processedCount} users" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk reputation recalculation");
            return StatusCode(500, "An error occurred during bulk reputation recalculation");
        }
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin") || User.HasClaim("role", "Admin");
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    #endregion
}