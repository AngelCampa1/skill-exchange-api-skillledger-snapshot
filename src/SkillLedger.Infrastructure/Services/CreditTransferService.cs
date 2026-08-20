using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Infrastructure.Services;

public class CreditTransferService : ICreditTransferService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ICreditWalletService _walletService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDistributedLockService _lockService;
    private readonly ILogger<CreditTransferService> _logger;
    private readonly IConfiguration _configuration;

    // P1 SECURITY FIX: Remove hardcoded secret key
    // Secret key now loaded from configuration/Azure Key Vault
    private readonly string _receiptSecretKey;

    private const int MAX_SINGLE_TRANSFER = 1000;
    private const int MAX_DAILY_TRANSFER_AMOUNT = 5000;
    private const int MAX_DAILY_TRANSFER_COUNT = 20;
    private const int REVERSAL_WINDOW_HOURS = 24;

    public CreditTransferService(
        SkillLedgerDbContext context,
        ICreditWalletService walletService,
        IAuditLogService auditLogService,
        IDistributedLockService lockService,
        IConfiguration configuration,
        ILogger<CreditTransferService> logger)
    {
        _context = context;
        _walletService = walletService;
        _auditLogService = auditLogService;
        _lockService = lockService;
        _configuration = configuration;
        _logger = logger;

        // P1 SECURITY FIX: Load receipt secret key from configuration
        // BUG-MEDIUM-005 FIX: Sanitized exception messages to prevent information disclosure
        // Priority order: Azure Key Vault > Configuration > Environment Variable
        _receiptSecretKey =
            _configuration["AzureKeyVault:ReceiptSignatureKey"] ??
            _configuration["CreditTransfer:ReceiptSecretKey"] ??
            Environment.GetEnvironmentVariable("RECEIPT_SECRET_KEY") ??
            throw new InvalidOperationException(
                "Receipt secret key not configured. Check application configuration.");

        // Validate key strength
        if (_receiptSecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Receipt secret key does not meet minimum security requirements.");
        }

        _logger.LogInformation("CreditTransferService initialized with secure receipt signing key");
    }

    public async Task<TransferCreditsResponseDto> TransferCreditsAsync(
        Guid fromUserId,
        Guid toUserId,
        int amount,
        string? message = null,
        string? initiatedFromIP = null,
        string? userAgent = null,
        string? idempotencyKey = null) // BUG-040 FIX: Add idempotency key parameter
    {
        // BUG-040 FIX: Check for existing transfer with same idempotency key
        // BUG-HIGH-009 FIX: Validate that idempotency key matches ALL original request parameters
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingTransfer = await _context.CreditTransfers
                .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey && t.FromUserId == fromUserId);

            if (existingTransfer != null)
            {
                // BUG-HIGH-009 FIX: Verify that amount and recipient match the original request
                // This prevents replay attacks where attacker reuses idempotency key with different parameters
                if (existingTransfer.ToUserId != toUserId || existingTransfer.Amount != amount)
                {
                    _logger.LogWarning(
                        "Idempotency key mismatch: Key {IdempotencyKey} used with different parameters. " +
                        "Original: ToUserId={OriginalTo}, Amount={OriginalAmount}. " +
                        "Current: ToUserId={CurrentTo}, Amount={CurrentAmount}",
                        idempotencyKey, existingTransfer.ToUserId, existingTransfer.Amount, toUserId, amount);

                    throw new InvalidOperationException(
                        "Idempotency key has already been used with different transfer parameters. " +
                        "Please use a new idempotency key for a different transfer.");
                }

                _logger.LogInformation("Returning existing transfer for idempotency key: {IdempotencyKey}, Transfer ID: {TransferId}",
                    idempotencyKey, existingTransfer.Id);

                // Return existing transfer details
                var existingBalance = await _walletService.GetAvailableBalanceAsync(fromUserId) ?? 0;

                return new TransferCreditsResponseDto
                {
                    TransferId = existingTransfer.Id,
                    TransactionHash = existingTransfer.TransactionHash,
                    Status = existingTransfer.Status,
                    Amount = existingTransfer.Amount,
                    TransferFee = existingTransfer.TransferFee,
                    CreatedAt = existingTransfer.CreatedAt,
                    RemainingBalance = existingBalance
                };
            }
        }

        // CRITICAL FIX: Acquire distributed lock to prevent concurrent transfers from same user
        // This prevents race conditions where multiple simultaneous transfers could exceed the user's balance
        await using var distributedLock = await _lockService.AcquireLockAsync(
            $"transfer:from:{fromUserId}",
            TimeSpan.FromSeconds(30),  // Lock expires after 30 seconds
            TimeSpan.FromSeconds(5),    // Wait up to 5 seconds to acquire
            TimeSpan.FromMilliseconds(100)); // Retry every 100ms

        if (!distributedLock.IsAcquired)
        {
            _logger.LogWarning("Could not acquire lock for transfer from user {FromUserId}", fromUserId);
            throw new InvalidOperationException("Another transfer is currently in progress. Please try again in a moment.");
        }

        // BUG-HIGH-010 FIX: Use Serializable isolation for credit transfers to prevent race conditions
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var isValid = await ValidateTransferAsync(fromUserId, toUserId, amount);
            if (!isValid)
            {
                // Determine specific validation failure reason
                if (fromUserId == toUserId)
                {
                    throw new InvalidOperationException("Cannot transfer credits to yourself.");
                }
                if (amount <= 0 || amount > MAX_SINGLE_TRANSFER)
                {
                    throw new InvalidOperationException($"Transfer amount must be between 1 and {MAX_SINGLE_TRANSFER} credits.");
                }

                var limits = await GetTransferLimitsAsync(fromUserId);
                if (amount > limits.WalletBalance)
                {
                    throw new InvalidOperationException("Insufficient credits in wallet.");
                }
                if (amount > limits.RemainingDailyAmount)
                {
                    throw new InvalidOperationException($"Transfer would exceed daily limit of {MAX_DAILY_TRANSFER_AMOUNT} credits.");
                }
                if (limits.DailyTransferCount >= limits.MaxDailyCount)
                {
                    throw new InvalidOperationException($"Daily transfer limit of {MAX_DAILY_TRANSFER_COUNT} transfers reached.");
                }

                throw new InvalidOperationException("Transfer validation failed.");
            }

            var transfer = new CreditTransfer
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Amount = amount,
                Message = message,
                InitiatedFromIP = initiatedFromIP,
                UserAgent = userAgent,
                Status = TransferStatus.Pending,
                IdempotencyKey = idempotencyKey // BUG-040 FIX: Store idempotency key
            };

            transfer.TransactionHash = transfer.GenerateTransactionHash();

            await _context.CreditTransfers.AddAsync(transfer);
            await _context.SaveChangesAsync();

            var walletTransaction = await _walletService.TransferCreditsAsync(
                fromUserId,
                toUserId,
                amount,
                $"Direct transfer: {message}",
                CreditTransactionType.DirectPayment,
                null,
                initiatedFromIP,
                userAgent);

            transfer.CreditTransactionId = walletTransaction.Id;
            transfer.Complete();

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var senderBalance = await _walletService.GetAvailableBalanceAsync(fromUserId) ?? 0;

            await _auditLogService.LogEventAsync(fromUserId, "CREDIT_TRANSFER",
                initiatedFromIP ?? "Unknown", userAgent, true,
                $"Transferred {amount} credits to user {toUserId}");

            _logger.LogInformation("Credit transfer completed: {FromUserId} -> {ToUserId}, Amount: {Amount}, Transfer ID: {TransferId}",
                fromUserId, toUserId, amount, transfer.Id);

            return new TransferCreditsResponseDto
            {
                TransferId = transfer.Id,
                TransactionHash = transfer.TransactionHash,
                Status = transfer.Status,
                Amount = amount,
                TransferFee = transfer.TransferFee,
                CreatedAt = transfer.CreatedAt,
                RemainingBalance = senderBalance
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Credit transfer failed: {FromUserId} -> {ToUserId}, Amount: {Amount}",
                fromUserId, toUserId, amount);
            throw;
        }
        // Lock is automatically released when distributedLock is disposed
    }

    public async Task<CreditTransferDetailDto?> GetTransferDetailsAsync(Guid transferId, Guid userId)
    {
        // BUG-MED-008 FIX: Use AsSplitQuery for multiple includes to prevent cartesian explosion
        var transfer = await _context.CreditTransfers
            .Include(t => t.FromUser)
            .Include(t => t.ToUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == transferId &&
                (t.FromUserId == userId || t.ToUserId == userId));

        if (transfer == null) return null;

        return new CreditTransferDetailDto
        {
            Id = transfer.Id,
            FromUserId = transfer.FromUserId,
            FromUsername = transfer.FromUser.UserName ?? "Unknown",
            ToUserId = transfer.ToUserId,
            ToUsername = transfer.ToUser.UserName ?? "Unknown",
            Amount = transfer.Amount,
            TransferFee = transfer.TransferFee,
            Message = transfer.Message,
            Status = transfer.Status,
            TransactionHash = transfer.TransactionHash,
            BatchId = transfer.BatchId,
            CreatedAt = transfer.CreatedAt,
            CompletedAt = transfer.CompletedAt,
            ReversedAt = transfer.ReversedAt,
            ReversalReason = transfer.ReversalReason,
            CanBeReversed = transfer.CanBeReversed()
        };
    }

    public async Task<BatchTransferResponseDto> BatchTransferAsync(
        Guid fromUserId,
        List<BatchTransferItemDto> transfers,
        string? initiatedFromIP = null,
        string? userAgent = null)
    {
        // CRITICAL FIX: Use single distributed lock for entire batch to prevent nested transaction deadlocks
        // This replaces individual transfer locks and prevents deadlock scenarios
        await using var distributedLock = await _lockService.AcquireLockAsync(
            $"transfer:from:{fromUserId}",
            TimeSpan.FromSeconds(60),  // Longer timeout for batch operations
            TimeSpan.FromSeconds(10),   // Wait up to 10 seconds to acquire
            TimeSpan.FromMilliseconds(100));

        if (!distributedLock.IsAcquired)
        {
            _logger.LogWarning("Could not acquire lock for batch transfer from user {FromUserId}", fromUserId);
            throw new InvalidOperationException("Another transfer is currently in progress. Please try again in a moment.");
        }

        var batchId = Guid.NewGuid();
        var response = new BatchTransferResponseDto
        {
            BatchId = batchId
        };

        // Single transaction for entire batch
        // BUG-HIGH-010 FIX: Use Serializable isolation for credit transfers to prevent race conditions
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var isValid = await ValidateBatchTransferAsync(fromUserId, transfers);
            if (!isValid)
            {
                // Determine specific validation failure reason
                if (transfers.Count > 10)
                {
                    throw new InvalidOperationException("Batch transfer cannot exceed 10 items.");
                }

                var totalAmount = transfers.Sum(t => t.Amount);
                var limits = await GetTransferLimitsAsync(fromUserId);

                if (totalAmount > limits.WalletBalance)
                {
                    throw new InvalidOperationException("Insufficient credits for batch transfer.");
                }
                if (totalAmount > limits.RemainingDailyAmount)
                {
                    throw new InvalidOperationException($"Batch transfer would exceed daily limit of {MAX_DAILY_TRANSFER_AMOUNT} credits.");
                }
                if (limits.DailyTransferCount + transfers.Count > limits.MaxDailyCount)
                {
                    throw new InvalidOperationException($"Batch transfer would exceed daily limit of {MAX_DAILY_TRANSFER_COUNT} transfers.");
                }

                throw new InvalidOperationException("Batch transfer validation failed.");
            }

            foreach (var transfer in transfers)
            {
                try
                {
                    // Use internal method that doesn't acquire locks or start transactions
                    var transferResponse = await TransferCreditsInternalAsync(
                        fromUserId,
                        transfer.ToUserId,
                        transfer.Amount,
                        transfer.Message,
                        batchId,
                        initiatedFromIP,
                        userAgent);

                    response.Transfers.Add(transferResponse);
                    response.SuccessfulTransfers++;
                    response.TotalAmount += transfer.Amount;
                }
                catch (Exception ex)
                {
                    // BUG-021 FIX: Capture error details for failed transfers
                    response.FailedTransfers++;
                    response.Errors.Add(new Core.DTOs.BatchTransferError
                    {
                        ToUserId = transfer.ToUserId,
                        Amount = transfer.Amount,
                        ErrorMessage = ex.Message
                    });

                    _logger.LogWarning(ex, "Batch transfer item failed: {ToUserId}, Amount: {Amount}, Error: {Error}",
                        transfer.ToUserId, transfer.Amount, ex.Message);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var senderBalance = await _walletService.GetAvailableBalanceAsync(fromUserId) ?? 0;
            response.RemainingBalance = senderBalance;

            await _auditLogService.LogEventAsync(fromUserId, "BATCH_TRANSFER",
                initiatedFromIP ?? "Unknown", userAgent, true,
                $"Batch transfer completed: {response.SuccessfulTransfers}/{transfers.Count} successful");

            _logger.LogInformation("Batch transfer completed: {SuccessfulTransfers}/{TotalTransfers} successful for user {FromUserId}",
                response.SuccessfulTransfers, transfers.Count, fromUserId);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Batch transfer failed for user {FromUserId}", fromUserId);
            throw;
        }
        // Lock is automatically released when distributedLock is disposed
    }

    /// <summary>
    /// Internal transfer method without transaction management or locking (for use within batch operations)
    /// </summary>
    private async Task<TransferCreditsResponseDto> TransferCreditsInternalAsync(
        Guid fromUserId,
        Guid toUserId,
        int amount,
        string? message,
        Guid? batchId,
        string? initiatedFromIP,
        string? userAgent)
    {
        // Validation still happens
        var isValid = await ValidateTransferAsync(fromUserId, toUserId, amount);
        if (!isValid)
        {
            throw new InvalidOperationException("Transfer validation failed.");
        }

        var transfer = new CreditTransfer
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            Message = message,
            BatchId = batchId,
            InitiatedFromIP = initiatedFromIP,
            UserAgent = userAgent,
            Status = TransferStatus.Pending
        };

        transfer.TransactionHash = transfer.GenerateTransactionHash();

        await _context.CreditTransfers.AddAsync(transfer);
        await _context.SaveChangesAsync();

        var walletTransaction = await _walletService.TransferCreditsAsync(
            fromUserId,
            toUserId,
            amount,
            $"Direct transfer: {message}",
            CreditTransactionType.DirectPayment,
            null,
            initiatedFromIP,
            userAgent);

        transfer.CreditTransactionId = walletTransaction.Id;
        transfer.Complete();

        await _context.SaveChangesAsync();

        var senderBalance = await _walletService.GetAvailableBalanceAsync(fromUserId) ?? 0;

        return new TransferCreditsResponseDto
        {
            TransferId = transfer.Id,
            TransactionHash = transfer.TransactionHash,
            Status = transfer.Status,
            Amount = amount,
            TransferFee = transfer.TransferFee,
            CreatedAt = transfer.CreatedAt,
            RemainingBalance = senderBalance
        };
    }

    public async Task<TransferHistoryResponseDto> GetTransferHistoryAsync(
        Guid userId,
        TransferHistoryRequestDto request)
    {
        // BUG-MED-008 FIX: Use AsSplitQuery for multiple includes to prevent cartesian explosion
        var query = _context.CreditTransfers
            .Include(t => t.FromUser)
            .Include(t => t.ToUser)
            .AsSplitQuery()
            .Where(t => t.FromUserId == userId || t.ToUserId == userId);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        if (request.Direction.HasValue)
        {
            query = request.Direction.Value switch
            {
                TransferDirection.Sent => query.Where(t => t.FromUserId == userId),
                TransferDirection.Received => query.Where(t => t.ToUserId == userId),
                _ => query
            };
        }

        if (request.StartDate.HasValue)
            query = query.Where(t => t.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(t => t.CreatedAt <= request.EndDate.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        var transfers = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new CreditTransferDetailDto
            {
                Id = t.Id,
                FromUserId = t.FromUserId,
                FromUsername = t.FromUser.UserName ?? "Unknown",
                ToUserId = t.ToUserId,
                ToUsername = t.ToUser.UserName ?? "Unknown",
                Amount = t.Amount,
                TransferFee = t.TransferFee,
                Message = t.Message,
                Status = t.Status,
                TransactionHash = t.TransactionHash,
                BatchId = t.BatchId,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt,
                ReversedAt = t.ReversedAt,
                ReversalReason = t.ReversalReason,
                CanBeReversed = t.CanBeReversed()
            })
            .ToListAsync();

        return new TransferHistoryResponseDto
        {
            Transfers = transfers,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.Page < totalPages,
            HasPreviousPage = request.Page > 1
        };
    }

    public async Task<TransferHistoryResponseDto> GetUserTransfersAsync(
        Guid userId,
        TransferDirection direction = TransferDirection.Both,
        int page = 1,
        int pageSize = 20)
    {
        // BUG-027 FIX: Validate and cap pagination parameters to prevent DoS
        const int MAX_PAGE_SIZE = 100;
        const int MIN_PAGE_SIZE = 1;
        const int MIN_PAGE = 1;
        // BE-MED-005 FIX: Add max page number to prevent excessive offset scans
        const int MAX_PAGE = 1000;

        if (pageSize < MIN_PAGE_SIZE || pageSize > MAX_PAGE_SIZE)
        {
            _logger.LogWarning("Invalid page size {PageSize} for user {UserId}. Capping to range [{Min}, {Max}]",
                pageSize, userId, MIN_PAGE_SIZE, MAX_PAGE_SIZE);
            pageSize = Math.Clamp(pageSize, MIN_PAGE_SIZE, MAX_PAGE_SIZE);
        }

        if (page < MIN_PAGE)
        {
            _logger.LogWarning("Invalid page number {Page} for user {UserId}. Setting to {MinPage}",
                page, userId, MIN_PAGE);
            page = MIN_PAGE;
        }

        // BE-MED-005 FIX: Cap maximum page number to prevent DoS via excessive offset
        if (page > MAX_PAGE)
        {
            _logger.LogWarning("Page number {Page} exceeds maximum {MaxPage} for user {UserId}. Capping.",
                page, MAX_PAGE, userId);
            page = MAX_PAGE;
        }

        var request = new TransferHistoryRequestDto
        {
            Page = page,
            PageSize = pageSize,
            Direction = direction
        };

        return await GetTransferHistoryAsync(userId, request);
    }

    public async Task<bool> ReverseTransferAsync(Guid transferId, Guid reversedByUserId, string reason)
    {
        // BUG-HIGH-010 FIX: Use Serializable isolation for credit transfers to prevent race conditions
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var transfer = await _context.CreditTransfers
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null || !transfer.CanBeReversed())
                return false;

            if (transfer.FromUserId != reversedByUserId && transfer.ToUserId != reversedByUserId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to reverse transfer {TransferId} without being a party to it",
                    reversedByUserId, transferId);
                return false;
            }

            // Reverse the wallet transaction
            await _walletService.TransferCreditsAsync(
                transfer.ToUserId,
                transfer.FromUserId,
                transfer.Amount,
                $"Reversal of transfer {transferId}: {reason}",
                CreditTransactionType.Refund);

            transfer.Reverse(reversedByUserId, reason);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLogService.LogEventAsync(reversedByUserId, "TRANSFER_REVERSED",
                "System", null, true,
                $"Reversed transfer {transferId}: {reason}");

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Transfer reversal failed: {TransferId}", transferId);
            return false;
        }
    }

    public async Task<bool> CanReverseTransferAsync(Guid transferId, Guid userId)
    {
        var transfer = await _context.CreditTransfers
            .FirstOrDefaultAsync(t => t.Id == transferId &&
                (t.FromUserId == userId || t.ToUserId == userId));

        return transfer?.CanBeReversed() ?? false;
    }

    public async Task<TransferLimitsDto> GetTransferLimitsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var startOfDay = now.Date;

        // PERFORMANCE FIX: Use database-side aggregations instead of loading all transfers into memory
        // BE-MED-004 FIX: Include both Completed AND Pending transfers in rate limiting
        // This prevents circumventing limits by initiating many simultaneous transfers
        var transferQuery = _context.CreditTransfers
            .AsNoTracking()
            .Where(t => t.FromUserId == userId &&
                       t.CreatedAt >= startOfDay &&
                       (t.Status == TransferStatus.Completed || t.Status == TransferStatus.Pending));

        var dailyTransferredAmount = await transferQuery.SumAsync(t => (int?)t.Amount) ?? 0;
        var dailyTransferCount = await transferQuery.CountAsync();
        var walletBalance = await _walletService.GetAvailableBalanceAsync(userId) ?? 0;

        return new TransferLimitsDto
        {
            MaxSingleTransfer = MAX_SINGLE_TRANSFER,
            MaxDailyTransfer = MAX_DAILY_TRANSFER_AMOUNT,
            MaxDailyCount = MAX_DAILY_TRANSFER_COUNT,
            DailyTransferredAmount = dailyTransferredAmount,
            DailyTransferCount = dailyTransferCount,
            RemainingDailyAmount = Math.Max(0, MAX_DAILY_TRANSFER_AMOUNT - dailyTransferredAmount),
            RemainingDailyCount = Math.Max(0, MAX_DAILY_TRANSFER_COUNT - dailyTransferCount),
            WalletBalance = walletBalance,
            TransferFeePercentage = 0m,
            ReversalWindowHours = REVERSAL_WINDOW_HOURS
        };
    }

    public async Task<bool> ValidateTransferAsync(Guid fromUserId, Guid toUserId, int amount)
    {
        if (fromUserId == toUserId) return false;
        if (amount <= 0 || amount > MAX_SINGLE_TRANSFER) return false;

        var limits = await GetTransferLimitsAsync(fromUserId);
        return amount <= limits.RemainingDailyAmount &&
               limits.DailyTransferCount < limits.MaxDailyCount &&
               amount <= limits.WalletBalance;
    }

    public async Task<bool> ValidateBatchTransferAsync(Guid fromUserId, List<BatchTransferItemDto> transfers)
    {
        if (transfers.Count > 10) return false;
        if (transfers.Any(t => t.ToUserId == fromUserId || t.Amount <= 0 || t.Amount > MAX_SINGLE_TRANSFER))
            return false;

        var totalAmount = transfers.Sum(t => t.Amount);
        var limits = await GetTransferLimitsAsync(fromUserId);

        return totalAmount <= limits.RemainingDailyAmount &&
               limits.DailyTransferCount + transfers.Count <= limits.MaxDailyCount &&
               totalAmount <= limits.WalletBalance;
    }

    public async Task<TransferReceiptDto?> GenerateReceiptAsync(Guid transferId, Guid userId)
    {
        // BUG-MED-008 FIX: Use AsSplitQuery for multiple includes to prevent cartesian explosion
        var transfer = await _context.CreditTransfers
            .Include(t => t.FromUser)
            .Include(t => t.ToUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == transferId &&
                (t.FromUserId == userId || t.ToUserId == userId) &&
                t.Status == TransferStatus.Completed);

        if (transfer == null || !transfer.CompletedAt.HasValue) return null;

        var signature = transfer.GenerateReceiptSignature(_receiptSecretKey);

        return new TransferReceiptDto
        {
            TransferId = transfer.Id,
            TransactionHash = transfer.TransactionHash,
            FromUser = transfer.FromUser.UserName ?? "Unknown",
            ToUser = transfer.ToUser.UserName ?? "Unknown",
            Amount = transfer.Amount,
            TransferFee = transfer.TransferFee,
            Message = transfer.Message,
            CompletedAt = transfer.CompletedAt.Value,
            ReceiptSignature = signature,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<VerifyReceiptResponseDto> VerifyReceiptAsync(Guid transferId, string signature)
    {
        // BUG-MED-008 FIX: Use AsSplitQuery for multiple includes to prevent cartesian explosion
        var transfer = await _context.CreditTransfers
            .Include(t => t.FromUser)
            .Include(t => t.ToUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == transferId);

        if (transfer == null)
        {
            return new VerifyReceiptResponseDto
            {
                IsValid = false,
                Message = "Transfer not found"
            };
        }

        var isValid = transfer.VerifyReceiptSignature(signature, _receiptSecretKey);

        return new VerifyReceiptResponseDto
        {
            IsValid = isValid,
            Transfer = isValid ? new CreditTransferDetailDto
            {
                Id = transfer.Id,
                FromUserId = transfer.FromUserId,
                FromUsername = transfer.FromUser.UserName ?? "Unknown",
                ToUserId = transfer.ToUserId,
                ToUsername = transfer.ToUser.UserName ?? "Unknown",
                Amount = transfer.Amount,
                TransferFee = transfer.TransferFee,
                Message = transfer.Message,
                Status = transfer.Status,
                TransactionHash = transfer.TransactionHash,
                CreatedAt = transfer.CreatedAt,
                CompletedAt = transfer.CompletedAt,
                CanBeReversed = transfer.CanBeReversed()
            } : null,
            Message = isValid ? "Receipt is valid" : "Invalid receipt signature"
        };
    }

    public async Task<FraudAssessmentResult> AnalyzeTransferRiskAsync(Guid userId, int amount, string? ipAddress = null)
    {
        var riskScore = 0;
        var riskFactors = new List<string>();

        // High amount risk
        if (amount >= 500)
        {
            riskScore += 30;
            riskFactors.Add("High transfer amount");
        }

        // Velocity risk
        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var recentTransfers = await _context.CreditTransfers
            .CountAsync(t => t.FromUserId == userId && t.CreatedAt >= hourAgo);

        if (recentTransfers >= 5)
        {
            riskScore += 40;
            riskFactors.Add("High transfer frequency");
        }

        // IP risk
        if (!string.IsNullOrEmpty(ipAddress))
        {
            var ipTransfers = await _context.CreditTransfers
                .CountAsync(t => t.InitiatedFromIP == ipAddress && t.CreatedAt >= hourAgo);

            if (ipTransfers > 10)
            {
                riskScore += 30;
                riskFactors.Add("High IP activity");
            }
        }

        var riskLevel = riskScore switch
        {
            < 30 => RiskLevel.Low,
            < 60 => RiskLevel.Medium,
            < 80 => RiskLevel.High,
            _ => RiskLevel.Critical
        };

        return new FraudAssessmentResult
        {
            RiskLevel = riskLevel,
            RiskScore = riskScore,
            IsAllowed = riskScore < 70,
            RiskFactors = riskFactors,
            RecommendedAction = riskScore >= 70 ? "Block transaction" :
                              riskScore >= 30 ? "Additional verification" : "Allow"
        };
    }

    public async Task<TransferStatistics> GetTransferStatisticsAsync(Guid userId, TimeSpan? timeframe = null)
    {
        var period = timeframe ?? TimeSpan.FromDays(1);
        var startTime = DateTime.UtcNow.Subtract(period);

        // PERFORMANCE FIX: Use database-side aggregations instead of loading all transfers into memory
        var sentQuery = _context.CreditTransfers
            .AsNoTracking()
            .Where(t => t.FromUserId == userId && t.CreatedAt >= startTime);

        var receivedQuery = _context.CreditTransfers
            .AsNoTracking()
            .Where(t => t.ToUserId == userId && t.CreatedAt >= startTime);

        // Execute all aggregations at database level
        var transfersSent = await sentQuery.CountAsync();
        var transfersReceived = await receivedQuery.CountAsync();

        var totalAmountSent = await sentQuery
            .Where(t => t.Status == TransferStatus.Completed)
            .SumAsync(t => (int?)t.Amount) ?? 0;

        var totalAmountReceived = await receivedQuery
            .Where(t => t.Status == TransferStatus.Completed)
            .SumAsync(t => (int?)t.Amount) ?? 0;

        var failedTransfers = await sentQuery.CountAsync(t => t.Status == TransferStatus.Failed) +
                             await receivedQuery.CountAsync(t => t.Status == TransferStatus.Failed);

        var reversedTransfers = await sentQuery.CountAsync(t => t.Status == TransferStatus.Reversed) +
                               await receivedQuery.CountAsync(t => t.Status == TransferStatus.Reversed);

        var completedSentCount = await sentQuery.CountAsync(t => t.Status == TransferStatus.Completed);
        var averageTransferAmount = completedSentCount > 0 ?
            (decimal)totalAmountSent / completedSentCount : 0;

        return new TransferStatistics
        {
            TransfersSent = transfersSent,
            TransfersReceived = transfersReceived,
            TotalAmountSent = totalAmountSent,
            TotalAmountReceived = totalAmountReceived,
            FailedTransfers = failedTransfers,
            ReversedTransfers = reversedTransfers,
            AverageTransferAmount = averageTransferAmount,
            TimePeriod = period
        };
    }

    public async Task<SystemTransferStatistics> GetSystemTransferStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        // PERFORMANCE FIX: Use database-side aggregations instead of loading all transfers into memory
        var transferQuery = _context.CreditTransfers
            .AsNoTracking()
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);

        var completedQuery = transferQuery.Where(t => t.Status == TransferStatus.Completed);

        // Execute all aggregations at database level
        var totalTransfers = await transferQuery.CountAsync();
        var totalVolume = await completedQuery.SumAsync(t => (long?)t.Amount) ?? 0;
        var successfulTransfers = await completedQuery.CountAsync();
        var failedTransfers = await transferQuery.CountAsync(t => t.Status == TransferStatus.Failed);
        var reversedTransfers = await transferQuery.CountAsync(t => t.Status == TransferStatus.Reversed);
        var totalFees = await completedQuery.SumAsync(t => (int?)t.TransferFee) ?? 0;

        var totalAmount = await completedQuery.SumAsync(t => (int?)t.Amount) ?? 0;
        var averageTransferAmount = successfulTransfers > 0 ?
            (decimal)totalAmount / successfulTransfers : 0;

        // Peak hour - use database grouping
        var peakTransferHour = await transferQuery
            .GroupBy(t => t.CreatedAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Select(g => g.Hour)
            .FirstOrDefaultAsync();

        // Active users - use database distinct count (Note: Union requires in-memory for FromUserId + ToUserId)
        var fromUsers = await transferQuery.Select(t => t.FromUserId).Distinct().ToListAsync();
        var toUsers = await transferQuery.Select(t => t.ToUserId).Distinct().ToListAsync();
        var activeTransferUsers = fromUsers.Union(toUsers).Distinct().Count();

        return new SystemTransferStatistics
        {
            TotalTransfers = totalTransfers,
            TotalVolume = totalVolume,
            SuccessfulTransfers = successfulTransfers,
            FailedTransfers = failedTransfers,
            ReversedTransfers = reversedTransfers,
            TotalFees = totalFees,
            AverageTransferAmount = averageTransferAmount,
            PeakTransferHour = peakTransferHour,
            ActiveTransferUsers = activeTransferUsers,
            ReportDate = DateTime.UtcNow
        };
    }

    public async Task<bool> CancelTransferAsync(Guid transferId, Guid cancelledByUserId, string reason)
    {
        var transfer = await _context.CreditTransfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.Status == TransferStatus.Pending);

        if (transfer == null) return false;

        transfer.Status = TransferStatus.Cancelled;
        transfer.ReversalReason = reason;
        transfer.ReversedByUserId = cancelledByUserId;

        await _context.SaveChangesAsync();

        await _auditLogService.LogEventAsync(cancelledByUserId, "TRANSFER_CANCELLED",
            "System", null, true, $"Cancelled transfer {transferId}: {reason}");

        return true;
    }
}
