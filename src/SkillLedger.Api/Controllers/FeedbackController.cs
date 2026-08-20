using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for handling user feedback submissions
/// Allows both authenticated and anonymous users to submit feedback
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : BaseApiController
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IFeedbackService feedbackService,
        ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    /// Submit user feedback. Available to both authenticated and anonymous users.
    /// Rate limited to 5 submissions per hour per IP address.
    /// </summary>
    /// <param name="dto">The feedback submission data</param>
    /// <returns>Success response (does not expose email delivery status for security)</returns>
    [HttpPost]
    [EnableRateLimiting("FeedbackLimit")]
    [ProducesResponseType(typeof(FeedbackResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new FeedbackResponseDto
            {
                Success = false,
                Message = "Invalid feedback data. Please check your input."
            });
        }

        // Get user info if authenticated (optional)
        var userId = TryGetCurrentUserId()?.ToString();
        var userEmail = GetCurrentUserEmail();

        _logger.LogInformation(
            "Feedback submission received. Category: {Category}, Authenticated: {IsAuthenticated}",
            dto.Category,
            userId != null);

        // Submit feedback - we always return success to the client
        // to avoid exposing email delivery status (security best practice)
        await _feedbackService.SubmitFeedbackAsync(dto, userId, userEmail);

        return Ok(new FeedbackResponseDto
        {
            Success = true,
            Message = "Thank you for your feedback! We appreciate you taking the time to help us improve."
        });
    }
}
