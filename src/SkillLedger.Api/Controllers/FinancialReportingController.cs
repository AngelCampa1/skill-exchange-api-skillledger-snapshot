using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("DefaultPolicy")]
public class FinancialReportingController : ControllerBase
{
    private readonly IFinancialReportingService _financialReportingService;
    private readonly ILogger<FinancialReportingController> _logger;

    public FinancialReportingController(
        IFinancialReportingService financialReportingService,
        ILogger<FinancialReportingController> logger)
    {
        _financialReportingService = financialReportingService;
        _logger = logger;
    }

    [HttpPost("credit-summary")]
    [ProducesResponseType(typeof(CreditSummaryReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditSummaryReport>> GenerateCreditSummaryReport(
        [FromBody] CreditReportRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // If no UserId specified in request, use current user
            if (!request.UserId.HasValue)
                request.UserId = currentUserId;

            // Users can only access their own reports (unless admin)
            if (request.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            var report = await _financialReportingService.GenerateCreditSummaryReportAsync(request);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating credit summary report for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(UserDashboardData), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDashboardData>> GetUserDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dashboard = await _financialReportingService.GetUserDashboardDataAsync(userId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    [HttpPost("analytics")]
    [ProducesResponseType(typeof(AnalyticsData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalyticsData>> GetAnalytics([FromBody] AnalyticsRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Users can only access their own analytics
            if (request.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            var analytics = await _financialReportingService.GetRealTimeAnalyticsAsync(request);
            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while retrieving analytics");
        }
    }

    [HttpPost("export")]
    [ProducesResponseType(typeof(FinancialExportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EnableRateLimiting("ExportPolicy")]
    public async Task<ActionResult<FinancialExportResult>> ExportFinancialData(
        [FromBody] FinancialExportRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Users can only export their own data
            if (request.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            var export = await _financialReportingService.ExportFinancialDataAsync(request);

            if (!export.Success)
                return BadRequest(export.ErrorMessage);

            return Ok(export);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting financial data for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while exporting data");
        }
    }

    [HttpGet("monthly-reports")]
    [ProducesResponseType(typeof(List<UserCreditReport>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserCreditReport>>> GetMonthlyReports(
        [FromQuery] int? startMonth = null,
        [FromQuery] int? endMonth = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var reports = await _financialReportingService.GetExistingMonthlyReportsAsync(
                userId, startMonth, endMonth);

            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly reports for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving monthly reports");
        }
    }

    [HttpPost("budget-tracking")]
    [ProducesResponseType(typeof(WalletOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletOperationResponse>> SetupBudgetTracking(
        [FromBody] BudgetTrackingRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Users can only set up budget tracking for themselves
            if (request.UserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            var result = await _financialReportingService.SetupBudgetTrackingAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up budget tracking for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while setting up budget tracking");
        }
    }

    [HttpGet("goal-progress")]
    [ProducesResponseType(typeof(GoalTrackingData), StatusCodes.Status200OK)]
    public async Task<ActionResult<GoalTrackingData>> GetGoalProgress()
    {
        try
        {
            var userId = GetCurrentUserId();
            var progress = await _financialReportingService.GetGoalTrackingProgressAsync(userId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting goal progress for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving goal progress");
        }
    }

    [HttpGet("transaction-breakdown")]
    [ProducesResponseType(typeof(List<TransactionCategoryBreakdown>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TransactionCategoryBreakdown>>> GetTransactionBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate < startDate)
                return BadRequest("End date cannot be before start date");

            var userId = GetCurrentUserId();
            var breakdown = await _financialReportingService.GetCategorizedTransactionBreakdownAsync(
                userId, startDate, endDate);

            return Ok(breakdown);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction breakdown for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving transaction breakdown");
        }
    }

    [HttpGet("trends")]
    [ProducesResponseType(typeof(List<PeriodTrendData>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PeriodTrendData>>> GetTrends([FromQuery] int months = 12)
    {
        try
        {
            if (months < 1 || months > 36)
                return BadRequest("Months must be between 1 and 36");

            var userId = GetCurrentUserId();
            var trends = await _financialReportingService.GetHistoricalTrendDataAsync(userId, months);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trends for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "An error occurred while retrieving trend data");
        }
    }

    [HttpGet("insights")]
    [ProducesResponseType(typeof(ActivityInsights), StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityInsights>> GetActivityInsights(
        [FromQuery] int analysisWindowDays = 90)
    {
        try
        {
            if (analysisWindowDays < 1 || analysisWindowDays > 365)
                return BadRequest("Analysis window must be between 1 and 365 days");

            var userId = GetCurrentUserId();
            var insights = await _financialReportingService.GenerateActivityInsightsAsync(
                userId, analysisWindowDays);

            return Ok(insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating insights for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "An error occurred while generating activity insights");
        }
    }

    // Admin endpoints
    [HttpGet("admin/system-analytics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SystemFinancialAnalytics), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemFinancialAnalytics>> GetSystemAnalytics(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate < startDate)
                return BadRequest("End date cannot be before start date");

            var analytics = await _financialReportingService.GenerateSystemAnalyticsAsync(startDate, endDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system analytics");
            return StatusCode(500, "An error occurred while generating system analytics");
        }
    }

    [HttpGet("admin/top-earners")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<TopUserEarnings>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TopUserEarnings>>> GetTopEarners(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (endDate < startDate)
                return BadRequest("End date cannot be before start date");

            if (limit < 1 || limit > 100)
                return BadRequest("Limit must be between 1 and 100");

            var topEarners = await _financialReportingService.GetTopEarningUsersAsync(
                startDate, endDate, limit);

            return Ok(topEarners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top earners");
            return StatusCode(500, "An error occurred while retrieving top earners");
        }
    }

    [HttpPost("admin/data-integrity")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DataIntegrityReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataIntegrityReport>> ValidateDataIntegrity(
        [FromQuery] Guid userId,
        [FromQuery] int? reportMonth = null)
    {
        try
        {
            var report = await _financialReportingService.ValidateReportIntegrityAsync(userId, reportMonth);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating data integrity for user {UserId}", userId);
            return StatusCode(500, "An error occurred while validating data integrity");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}