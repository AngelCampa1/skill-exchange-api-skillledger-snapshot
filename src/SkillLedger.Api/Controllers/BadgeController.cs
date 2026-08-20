using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for managing user badges and verification
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BadgeController : ControllerBase
{
    private readonly IBadgeService _badgeService;
    private readonly IBadgeSecurityService _badgeSecurityService;
    private readonly IExternalIntegrationService _externalIntegrationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BadgeController> _logger;

    public BadgeController(
        IBadgeService badgeService,
        IBadgeSecurityService badgeSecurityService,
        IExternalIntegrationService externalIntegrationService,
        IAuditLogService auditLogService,
        ILogger<BadgeController> logger)
    {
        _badgeService = badgeService;
        _badgeSecurityService = badgeSecurityService;
        _externalIntegrationService = externalIntegrationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all badges for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeExpired">Whether to include expired badges</param>
    /// <returns>List of user badges</returns>
    [HttpGet("user/{userId:guid}/badges")]
    public async Task<IActionResult> GetUserBadges(Guid userId, [FromQuery] bool includeExpired = false)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var badges = await _badgeService.GetUserBadgesAsync(userId, includeExpired);
            return Ok(badges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badges for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user badges");
        }
    }

    /// <summary>
    /// Get badge progress for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Badge progress information</returns>
    [HttpGet("user/{userId:guid}/progress")]
    public async Task<IActionResult> GetBadgeProgress(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var progress = await _badgeService.GetBadgeProgressAsync(userId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting badge progress for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving badge progress");
        }
    }

    /// <summary>
    /// Check badge eligibility for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of eligible badges</returns>
    [HttpGet("user/{userId:guid}/eligibility")]
    public async Task<IActionResult> CheckBadgeEligibility(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest("Invalid user ID");
            }

            var eligibleBadges = await _badgeService.CheckBadgeEligibilityAsync(userId);
            return Ok(eligibleBadges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking badge eligibility for user {UserId}", userId);
            return StatusCode(500, "An error occurred while checking badge eligibility");
        }
    }

    /// <summary>
    /// Submit a verification request for manual badge verification
    /// </summary>
    /// <param name="request">Verification request details</param>
    /// <returns>Created verification request</returns>
    [HttpPost("verification/request")]
    public async Task<IActionResult> SubmitVerificationRequest([FromBody] SubmitVerificationRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Submit verification request validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token");
            }

            if (string.IsNullOrEmpty(request.BadgeType))
            {
                return BadRequest("Badge type is required");
            }

            var verificationRequest = await _badgeService.SubmitVerificationRequestAsync(
                userId,
                request.BadgeType,
                request.Evidence ?? new Dictionary<string, object>());

            return CreatedAtAction(nameof(GetVerificationRequest),
                new { requestId = verificationRequest.Id }, verificationRequest);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting verification request");
            return StatusCode(500, "An error occurred while submitting the verification request");
        }
    }

    /// <summary>
    /// Get a specific verification request
    /// </summary>
    /// <param name="requestId">Verification request ID</param>
    /// <returns>Verification request details</returns>
    [HttpGet("verification/request/{requestId:guid}")]
    public async Task<IActionResult> GetVerificationRequest(Guid requestId)
    {
        try
        {
            // This would need to be implemented in the badge service
            // For now, return NotImplemented
            return StatusCode(501, "Not implemented yet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting verification request {RequestId}", requestId);
            return StatusCode(500, "An error occurred while retrieving the verification request");
        }
    }

    /// <summary>
    /// Get pending verification requests (Admin only)
    /// </summary>
    /// <param name="badgeType">Optional filter by badge type</param>
    /// <returns>List of pending verification requests</returns>
    [HttpGet("verification/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingVerificationRequests([FromQuery] string? badgeType = null)
    {
        try
        {
            var pendingRequests = await _badgeService.GetPendingVerificationRequestsAsync(badgeType);
            return Ok(pendingRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending verification requests");
            return StatusCode(500, "An error occurred while retrieving pending verification requests");
        }
    }

    /// <summary>
    /// Process a verification request (Admin only)
    /// </summary>
    /// <param name="requestId">Verification request ID</param>
    /// <param name="decision">Approval decision</param>
    /// <returns>Success response</returns>
    [HttpPost("verification/request/{requestId:guid}/process")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProcessVerificationRequest(Guid requestId, [FromBody] ProcessVerificationRequestDto decision)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var reviewerId))
            {
                return Unauthorized("Invalid user token");
            }

            await _badgeService.ProcessVerificationRequestAsync(
                requestId,
                decision.Approved,
                decision.ReviewNotes,
                reviewerId);

            return Ok(new { message = "Verification request processed successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing verification request {RequestId}", requestId);
            return StatusCode(500, "An error occurred while processing the verification request");
        }
    }

    /// <summary>
    /// Award a badge manually (Admin only)
    /// </summary>
    /// <param name="request">Badge award request</param>
    /// <returns>Awarded badge</returns>
    [HttpPost("award")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AwardBadge([FromBody] AwardBadgeRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Award badge validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var awardedBy))
            {
                return Unauthorized("Invalid user token");
            }

            if (request.UserId == Guid.Empty || string.IsNullOrEmpty(request.BadgeType))
            {
                return BadRequest("User ID and Badge Type are required");
            }

            var badge = await _badgeService.AwardBadgeAsync(
                request.UserId,
                request.BadgeType,
                request.Evidence,
                awardedBy);

            return CreatedAtAction(nameof(GetUserBadges),
                new { userId = request.UserId }, badge);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding badge");
            return StatusCode(500, "An error occurred while awarding the badge");
        }
    }

    /// <summary>
    /// Revoke a badge (Admin only)
    /// </summary>
    /// <param name="badgeId">Badge ID to revoke</param>
    /// <param name="request">Revocation details</param>
    /// <returns>Success response</returns>
    [HttpPost("revoke/{badgeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeBadge(Guid badgeId, [FromBody] RevokeBadgeRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var revokedBy))
            {
                return Unauthorized("Invalid user token");
            }

            if (string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest("Reason is required for badge revocation");
            }

            await _badgeService.RevokeBadgeAsync(badgeId, request.Reason, revokedBy);

            return Ok(new { message = "Badge revoked successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking badge {BadgeId}", badgeId);
            return StatusCode(500, "An error occurred while revoking the badge");
        }
    }

    /// <summary>
    /// Run automatic badge evaluation for all users (Admin only)
    /// </summary>
    /// <returns>Number of badges awarded</returns>
    [HttpPost("evaluation/run")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]  // BUG-BE-003 FIX: Add CSRF protection for admin endpoint
    public async Task<IActionResult> RunAutomaticBadgeEvaluation()
    {
        try
        {
            var badgesAwarded = await _badgeService.ProcessAutomaticBadgeEvaluationAsync();
            return Ok(new { message = $"Automatic badge evaluation completed. {badgesAwarded} badges awarded." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running automatic badge evaluation");
            return StatusCode(500, "An error occurred while running automatic badge evaluation");
        }
    }

    /// <summary>
    /// Generate a verification code for a badge
    /// </summary>
    /// <param name="badgeId">Badge ID</param>
    /// <returns>Verification code</returns>
    [HttpPost("verify/{badgeId:guid}/generate-code")]
    public async Task<IActionResult> GenerateVerificationCode(Guid badgeId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token");
            }

            var verificationCode = await _badgeSecurityService.GenerateVerificationCodeAsync(badgeId, userId);
            return Ok(new { verificationCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verification code for badge {BadgeId}", badgeId);
            return StatusCode(500, "An error occurred while generating the verification code");
        }
    }

    /// <summary>
    /// Verify a badge using a verification code (Public endpoint)
    /// </summary>
    /// <param name="badgeId">Badge ID</param>
    /// <param name="verificationCode">Verification code</param>
    /// <returns>Verification result</returns>
    [HttpGet("verify/{badgeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyBadgeCode(Guid badgeId, [FromQuery] string verificationCode)
    {
        try
        {
            if (string.IsNullOrEmpty(verificationCode))
            {
                return BadRequest("Verification code is required");
            }

            var isValid = await _badgeSecurityService.VerifyBadgeCodeAsync(badgeId, verificationCode);
            return Ok(new { isValid, message = isValid ? "Badge is valid" : "Badge verification failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying badge {BadgeId}", badgeId);
            return StatusCode(500, "An error occurred while verifying the badge");
        }
    }

    /// <summary>
    /// Verify LinkedIn profile for external badge verification
    /// </summary>
    /// <param name="request">LinkedIn verification request</param>
    /// <returns>Verification result</returns>
    [HttpPost("external/linkedin/verify")]
    public async Task<IActionResult> VerifyLinkedInProfile([FromBody] LinkedInVerificationRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token");
            }

            if (string.IsNullOrEmpty(request.LinkedInUrl))
            {
                return BadRequest("LinkedIn URL is required");
            }

            var result = await _externalIntegrationService.VerifyLinkedInProfileAsync(request.LinkedInUrl);

            if (result.IsVerified)
            {
                await _externalIntegrationService.CacheVerificationResultAsync(userId, "LinkedIn", result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying LinkedIn profile");
            return StatusCode(500, "An error occurred while verifying the LinkedIn profile");
        }
    }

    /// <summary>
    /// Verify GitHub contributions for external badge verification
    /// </summary>
    /// <param name="request">GitHub verification request</param>
    /// <returns>Verification result</returns>
    [HttpPost("external/github/verify")]
    public async Task<IActionResult> VerifyGitHubContributions([FromBody] GitHubVerificationRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token");
            }

            if (string.IsNullOrEmpty(request.GitHubUsername))
            {
                return BadRequest("GitHub username is required");
            }

            var result = await _externalIntegrationService.VerifyGitHubContributionsAsync(request.GitHubUsername);

            if (result.IsVerified)
            {
                await _externalIntegrationService.CacheVerificationResultAsync(userId, "GitHub", result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying GitHub contributions");
            return StatusCode(500, "An error occurred while verifying the GitHub contributions");
        }
    }

    /// <summary>
    /// Get cached external verification result
    /// </summary>
    /// <param name="platform">Platform name (LinkedIn, GitHub)</param>
    /// <returns>Cached verification result</returns>
    [HttpGet("external/{platform}/cached")]
    public async Task<IActionResult> GetCachedVerification(string platform)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user token");
            }

            var result = await _externalIntegrationService.GetCachedVerificationAsync(userId, platform);

            if (result == null)
            {
                return NotFound($"No cached verification found for platform {platform}");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached verification for platform {Platform}", platform);
            return StatusCode(500, "An error occurred while retrieving the cached verification");
        }
    }
}