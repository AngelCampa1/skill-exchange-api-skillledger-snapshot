using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Secure credit wallet API controller with enterprise-grade financial operations
/// Implements rate limiting, CSRF protection, and comprehensive audit logging
/// </summary>
[ApiController]
[Route("api/credit-wallet")]
[Authorize]
[EnableRateLimiting("WalletPolicy")]
public class CreditWalletController : BaseApiController
{
    private readonly ICreditWalletService _creditWalletService;
    private readonly ILogger<CreditWalletController> _logger;

    public CreditWalletController(
        ICreditWalletService creditWalletService,
        ILogger<CreditWalletController> logger)
    {
        _creditWalletService = creditWalletService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's wallet information
    /// </summary>
    /// <returns>Wallet details with decrypted financial data</returns>
    [HttpGet]
    [ProducesResponseType(typeof(WalletDashboardData), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<WalletDashboardData>> GetWallet()
    {
        try
        {
            var userId = GetCurrentUserId();
            var wallet = await _creditWalletService.GetWalletAsync(userId);

            if (wallet == null)
            {
                return NotFound(new { message = "Wallet not found" });
            }

            // Get recent transactions for dashboard
            var recentTransactions = await _creditWalletService.GetTransactionHistoryAsync(userId, 10);

            // Get active escrows (transactions with pending status)
            // BUG-PERF-011 FIX: Pre-compute released/refunded project IDs to avoid O(n²) nested Any()
            var releasedOrRefundedProjectIds = recentTransactions
                .Where(t => t.ProjectId.HasValue &&
                           (t.Type == CreditTransactionType.EscrowRelease || t.Type == CreditTransactionType.EscrowRefund))
                .Select(t => t.ProjectId!.Value)
                .ToHashSet();

            // Filter out transactions without ProjectId before projection to avoid NullReferenceException
            var activeEscrows = recentTransactions
                .Where(t => t.Type == CreditTransactionType.EscrowDeposit && t.Status == TransactionStatus.Completed)
                .Where(t => t.ProjectId.HasValue && !releasedOrRefundedProjectIds.Contains(t.ProjectId.Value))
                .Select(t => new EscrowSummary
                {
                    EscrowTransactionId = t.Id,
                    ProjectId = t.ProjectId.GetValueOrDefault(),
                    ProjectTitle = t.Project?.Title ?? "Unknown Project",
                    Amount = t.Amount,
                    CreatedAt = t.CreatedAt,
                    Status = "Active"
                })
                .ToList();

            // LOW-PRIORITY FIX: Calculate monthly earnings, spending, and project completion
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthlyTransactions = await _creditWalletService.GetTransactionHistoryAsync(userId, 500);

            var monthlyEarnings = monthlyTransactions
                .Where(t => t.ToUserId == userId && t.Status == TransactionStatus.Completed && t.CompletedAt >= startOfMonth)
                .Sum(t => t.Amount);

            var monthlySpending = monthlyTransactions
                .Where(t => t.FromUserId == userId && t.Status == TransactionStatus.Completed && t.CompletedAt >= startOfMonth)
                .Sum(t => t.Amount);

            var projectsCompleted = monthlyTransactions
                .Where(t => t.ProjectId.HasValue && t.Status == TransactionStatus.Completed && t.CompletedAt >= startOfMonth)
                .Select(t => t.ProjectId)
                .Distinct()
                .Count();

            var dashboardData = new WalletDashboardData
            {
                Wallet = new WalletSummary
                {
                    WalletId = wallet.Id,
                    CurrentBalance = wallet.Balance,
                    PendingBalance = wallet.PendingBalance,
                    TotalEarned = wallet.TotalEarned,
                    TotalSpent = wallet.TotalSpent,
                    CreatedAt = wallet.CreatedAt,
                    LastTransactionAt = wallet.LastTransactionAt,
                    IsBlocked = wallet.IsBlocked,
                    BlockedReason = wallet.BlockedReason
                },
                // BUG-SEC-012 FIX: Mask user emails to prevent email enumeration
                RecentTransactions = recentTransactions.Select(t => new TransactionExportRecord
                {
                    TransactionId = t.Id,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    Amount = t.Amount,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    CompletedAt = t.CompletedAt,
                    FromUser = MaskEmail(t.FromUser?.Email),
                    ToUser = MaskEmail(t.ToUser?.Email),
                    ProjectReference = t.ProjectId?.ToString(),
                    WasIncoming = t.ToUserId == userId
                }).ToList(),
                ActiveEscrows = activeEscrows,
                MonthlyEarnings = monthlyEarnings, // LOW-PRIORITY FIX: Calculated from monthly incoming transactions
                MonthlySpending = monthlySpending, // LOW-PRIORITY FIX: Calculated from monthly outgoing transactions
                ProjectsCompleted = projectsCompleted // LOW-PRIORITY FIX: Calculated from distinct projects in monthly transactions
            };

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wallet for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Failed to retrieve wallet information" });
        }
    }

    /// <summary>
    /// Get current user's wallet balance
    /// </summary>
    /// <returns>Current balance</returns>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<object>> GetBalance()
    {
        try
        {
            var userId = GetCurrentUserId();
            var balance = await _creditWalletService.GetBalanceAsync(userId);
            var availableBalance = await _creditWalletService.GetAvailableBalanceAsync(userId);

            if (balance == null)
            {
                return NotFound(new { message = "Wallet not found" });
            }

            return Ok(new
            {
                balance = balance.Value,
                availableBalance = availableBalance ?? 0,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Failed to retrieve balance" });
        }
    }

    /// <summary>
    /// Transfer credits to another user
    /// </summary>
    /// <param name="request">Transfer request details</param>
    /// <returns>Transfer operation result</returns>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(WalletOperationResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [EnableRateLimiting("TransferPolicy")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WalletOperationResponse>> TransferCredits([FromBody] TransferCreditsRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Transfer credits validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var fromUserId = GetCurrentUserId();

            // BUG-FIN-001 FIX: Prevent self-transfers to avoid fraud detection gaming
            if (fromUserId == request.ToUserId)
            {
                return BadRequest(new WalletOperationResponse
                {
                    Success = false,
                    Message = "Cannot transfer credits to yourself",
                    Timestamp = DateTime.UtcNow
                });
            }

            var clientIP = GetClientIPAddress();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var transaction = await _creditWalletService.TransferCreditsAsync(
                fromUserId: fromUserId,
                toUserId: request.ToUserId,
                amount: request.Amount,
                description: request.Description,
                transactionType: request.TransactionType,
                projectId: request.ProjectId,
                initiatedFromIP: clientIP,
                userAgent: userAgent
            );

            var newBalance = await _creditWalletService.GetBalanceAsync(fromUserId);

            var response = new WalletOperationResponse
            {
                Success = true,
                Message = "Credits transferred successfully",
                TransactionId = transaction.Id,
                NewBalance = newBalance,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Transfer validation failed for user {UserId}", GetCurrentUserId());
            return BadRequest(new WalletOperationResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Transfer failed for user {UserId}", GetCurrentUserId());
            return BadRequest(new WalletOperationResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transfer failed for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new WalletOperationResponse
            {
                Success = false,
                Message = "Transfer failed due to system error",
                Errors = new List<string> { "System error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Add credits to own wallet (demo/testing feature)
    /// In production, this would be replaced by a payment gateway integration
    /// </summary>
    /// <param name="request">Add credits request</param>
    /// <returns>Operation result with new balance</returns>
    [HttpPost("add-credits")]
    [ProducesResponseType(typeof(WalletOperationResponse), 200)]
    [ProducesResponseType(400)]
    [EnableRateLimiting("WalletPolicy")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<WalletOperationResponse>> AddCredits([FromBody] AddCreditsRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
            _logger.LogWarning("Add credits validation failed: {@ValidationErrors}", errors);
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetCurrentUserId();

            // Use AddCreditsAsync to add credits directly to the user's wallet
            // This is a demo/testing feature - in production would be replaced by payment gateway
            // BUG-010 FIX: Use Purchase type instead of StartingCredit so transactions appear in history
            var transaction = await _creditWalletService.AddCreditsAsync(
                userId: userId,
                amount: request.Amount,
                description: request.Description,
                transactionType: CreditTransactionType.Purchase
            );

            var newBalance = await _creditWalletService.GetBalanceAsync(userId);

            _logger.LogInformation(
                "User {UserId} added {Amount} credits to their wallet. Package: {PackageId}",
                userId, request.Amount, request.PackageId ?? "none");

            var response = new WalletOperationResponse
            {
                Success = true,
                Message = $"Successfully added {request.Amount} credits to your wallet",
                TransactionId = transaction.Id,
                NewBalance = newBalance,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Add credits failed for user {UserId}", GetCurrentUserId());
            return BadRequest(new WalletOperationResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add credits failed for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new WalletOperationResponse
            {
                Success = false,
                Message = "Failed to add credits due to system error",
                Errors = new List<string> { "System error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Create escrow for a project
    /// </summary>
    /// <param name="request">Escrow creation request</param>
    /// <returns>Escrow operation result</returns>
    [HttpPost("escrow")]
    [ProducesResponseType(typeof(WalletOperationResponse), 200)]
    [ProducesResponseType(400)]
    [EnableRateLimiting("EscrowPolicy")]
    public async Task<ActionResult<WalletOperationResponse>> CreateEscrow([FromBody] CreateEscrowRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            var transaction = await _creditWalletService.CreateEscrowAsync(
                clientUserId: userId,
                projectId: request.ProjectId,
                amount: request.Amount
            );

            var newBalance = await _creditWalletService.GetAvailableBalanceAsync(userId);

            var response = new WalletOperationResponse
            {
                Success = true,
                Message = "Escrow created successfully",
                TransactionId = transaction.Id,
                NewBalance = newBalance,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Escrow creation failed for user {UserId}", GetCurrentUserId());
            return BadRequest(new WalletOperationResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message },
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Escrow creation failed for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new WalletOperationResponse
            {
                Success = false,
                Message = "Escrow creation failed due to system error",
                Errors = new List<string> { "System error occurred" },
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get transaction history for current user
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of transactions per page (max 100)</param>
    /// <returns>Paginated transaction history</returns>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<object>> GetTransactionHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            pageSize = Math.Min(pageSize, 100); // Limit page size
            var offset = (page - 1) * pageSize;

            var transactions = await _creditWalletService.GetTransactionHistoryAsync(userId, pageSize, offset);

            var response = new
            {
                transactions = transactions.Select(t => new
                {
                    id = t.Id,
                    type = t.Type.ToString(),
                    status = t.Status.ToString(),
                    amount = t.Amount,
                    description = t.Description,
                    createdAt = t.CreatedAt,
                    completedAt = t.CompletedAt,
                    fromUser = t.FromUser != null ? new { email = MaskEmail(t.FromUser.Email) } : null,
                    toUser = t.ToUser != null ? new { email = MaskEmail(t.ToUser.Email) } : null,
                    projectId = t.ProjectId,
                    wasIncoming = t.ToUserId == userId,
                    transactionHash = t.TransactionHash
                }),
                pagination = new
                {
                    page = page,
                    pageSize = pageSize,
                    hasMore = transactions.Count == pageSize
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction history for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Failed to retrieve transaction history" });
        }
    }

    /// <summary>
    /// Get fraud analysis report for current user (admin/self only)
    /// </summary>
    /// <returns>Fraud analysis report</returns>
    [HttpGet("fraud-analysis")]
    [ProducesResponseType(typeof(FraudAnalysisReport), 200)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<FraudAnalysisReport>> GetFraudAnalysis()
    {
        try
        {
            var userId = GetCurrentUserId();

            // Only allow users to view their own fraud analysis (or admins can view any)
            // For now, allow self-access only
            var report = await _creditWalletService.AnalyzeFraudPatterns(userId);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fraud analysis for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Failed to retrieve fraud analysis" });
        }
    }

    /// <summary>
    /// Export wallet data for GDPR compliance
    /// </summary>
    /// <returns>Complete wallet export data</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(WalletExportData), 200)]
    public async Task<ActionResult<WalletExportData>> ExportWalletData()
    {
        try
        {
            var userId = GetCurrentUserId();
            var exportData = await _creditWalletService.ExportWalletDataAsync(userId);

            return Ok(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export wallet data for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Failed to export wallet data" });
        }
    }

    /// <summary>
    /// Get financial summary report for a date range
    /// </summary>
    /// <param name="startDate">Start date for report</param>
    /// <param name="endDate">End date for report</param>
    /// <returns>Financial summary report</returns>
    [HttpGet("financial-report")]
    [ProducesResponseType(typeof(FinancialSummaryReport), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<FinancialSummaryReport>> GetFinancialReport(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (endDate <= startDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            if ((endDate - startDate).TotalDays > 365)
            {
                return BadRequest(new { message = "Date range cannot exceed 365 days" });
            }

            var userId = GetCurrentUserId();
            var report = await _creditWalletService.GenerateFinancialReportAsync(userId, startDate, endDate);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate financial report for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "Failed to generate financial report" });
        }
    }

    /// <summary>
    /// Validate transaction integrity by hash
    /// </summary>
    /// <param name="transactionId">Transaction ID to validate</param>
    /// <returns>Integrity validation result</returns>
    [HttpGet("validate/{transactionId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<object>> ValidateTransaction(Guid transactionId)
    {
        try
        {
            var isValid = await _creditWalletService.ValidateTransactionIntegrity(transactionId);

            return Ok(new
            {
                transactionId = transactionId,
                isValid = isValid,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate transaction {TransactionId}", transactionId);
            return StatusCode(500, new { message = "Failed to validate transaction" });
        }
    }

    #region Admin Operations

    /// <summary>
    /// Block a user's wallet (admin only)
    /// </summary>
    /// <param name="userId">User ID to block</param>
    /// <param name="reason">Reason for blocking</param>
    /// <returns>Block operation result</returns>
    [HttpPost("admin/block/{userId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [Authorize(Roles = "Admin")] // LOW-PRIORITY FIX: Enforce admin role authorization
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<object>> BlockWallet(Guid userId, [FromBody] string reason)
    {
        try
        {
            // LOW-PRIORITY FIX: Admin authorization enforced via [Authorize(Roles = "Admin")]
            // Additional runtime check for extra safety
            if (!User.IsInRole("Admin"))
            {
                _logger.LogWarning("Non-admin user attempted to block wallet. User: {User}", User.Identity?.Name);
                return Forbid();
            }

            var result = await _creditWalletService.BlockWalletAsync(userId, reason);

            if (!result)
            {
                return NotFound(new { message = "Wallet not found" });
            }

            return Ok(new
            {
                success = true,
                message = "Wallet blocked successfully",
                userId = userId,
                reason = reason,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block wallet for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to block wallet" });
        }
    }

    /// <summary>
    /// Unblock a user's wallet (admin only)
    /// </summary>
    /// <param name="userId">User ID to unblock</param>
    /// <returns>Unblock operation result</returns>
    [HttpPost("admin/unblock/{userId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [Authorize(Roles = "Admin")] // LOW-PRIORITY FIX: Enforce admin role authorization
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<object>> UnblockWallet(Guid userId)
    {
        try
        {
            // LOW-PRIORITY FIX: Admin authorization enforced via [Authorize(Roles = "Admin")]
            // Additional runtime check for extra safety
            if (!User.IsInRole("Admin"))
            {
                _logger.LogWarning("Non-admin user attempted to unblock wallet. User: {User}", User.Identity?.Name);
                return Forbid();
            }

            var result = await _creditWalletService.UnblockWalletAsync(userId);

            if (!result)
            {
                return NotFound(new { message = "Wallet not found" });
            }

            return Ok(new
            {
                success = true,
                message = "Wallet unblocked successfully",
                userId = userId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unblock wallet for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to unblock wallet" });
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Get current authenticated user ID from JWT claims
    /// </summary>
    /// <returns>Current user ID</returns>
    private new Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user authentication");
        }
        return userId;
    }

    /// <summary>
    /// Get client IP address for audit logging
    /// </summary>
    /// <returns>Client IP address</returns>
    private new string GetClientIPAddress()
    {
        return SkillLedger.Infrastructure.Services.TrustedClientIpResolver.GetClientIpAddress(HttpContext, "unknown");
    }

    /// <summary>
    /// BUG-SEC-012 FIX: Mask email addresses to prevent email enumeration
    /// </summary>
    /// <param name="email">Email address to mask</param>
    /// <returns>Masked email or null</returns>
    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "*****@***.***";

        // Mask format: "a*****@example.com" - show first char, replace rest of local part with *****
        var localPart = email.Substring(0, atIndex);
        var domainPart = email.Substring(atIndex);

        if (localPart.Length <= 1)
            return localPart + "*****" + domainPart;

        return localPart[0] + "*****" + domainPart;
    }

    #endregion
}
