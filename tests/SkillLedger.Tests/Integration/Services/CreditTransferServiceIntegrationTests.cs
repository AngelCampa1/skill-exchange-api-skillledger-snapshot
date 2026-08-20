using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for CreditTransferService - Financial Transfer Operations.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real internal services (wallet service, audit log writes to DB)
/// - Mocks only EXTERNAL services (none needed here)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 0
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Service", "CreditTransferService")]
public class CreditTransferServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly CreditTransferService _service;

    // REAL internal services
    private readonly MockCreditWalletService _walletService;  // Writes to DB!
    private readonly MockAuditLogService _auditLogService;    // Writes to DB!
    private readonly MockDistributedLockService _lockService; // Real locking behavior

    // Test data
    private readonly User _testSender;
    private readonly User _testRecipient;
    private readonly User _testRecipient2;

    public CreditTransferServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreditTransferTests_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal services
        _walletService = new MockCreditWalletService(_context);
        _auditLogService = new MockAuditLogService(_context);
        _lockService = new MockDistributedLockService();

        var logger = new LoggerFactory().CreateLogger<CreditTransferService>();

        // Configure receipt secret key
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CreditTransfer:ReceiptSecretKey"] = "test-secret-key-with-at-least-32-characters-for-security"
            })
            .Build();

        _service = new CreditTransferService(
            _context,
            _walletService,
            _auditLogService,
            _lockService,
            config,
            logger);

        // Initialize test data
        _testSender = new User
        {
            Id = Guid.NewGuid(),
            Email = "sender@test.com",
            UserName = "testsender",
            FirstName = "Test",
            LastName = "Sender",
            PasswordHash = "hash",
            EmailConfirmed = true
        };

        _testRecipient = new User
        {
            Id = Guid.NewGuid(),
            Email = "recipient@test.com",
            UserName = "testrecipient",
            FirstName = "Test",
            LastName = "Recipient",
            PasswordHash = "hash",
            EmailConfirmed = true
        };

        _testRecipient2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "recipient2@test.com",
            UserName = "testrecipient2",
            FirstName = "Test2",
            LastName = "Recipient2",
            PasswordHash = "hash",
            EmailConfirmed = true
        };

        _context.Users.AddRange(_testSender, _testRecipient, _testRecipient2);
        _context.SaveChanges();

        // Create wallets with starting credits
        _walletService.CreateWalletAsync(_testSender.Id).Wait();
        _walletService.CreateWalletAsync(_testRecipient.Id).Wait();
        _walletService.CreateWalletAsync(_testRecipient2.Id).Wait();
    }

    #region Transfer Operations Tests

    [Fact]
    public async Task TransferCreditsAsync_ValidTransfer_ShouldCompleteSuccessfully()
    {
        // Arrange
        var amount = 50;
        var message = "Test payment";

        // Act
        var result = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            amount,
            message,
            "127.0.0.1",
            "test-agent");

        // Assert - Verify response
        result.Should().NotBeNull();
        result.Status.Should().Be(TransferStatus.Completed);
        result.Amount.Should().Be(amount);
        result.RemainingBalance.Should().Be(50); // 100 - 50

        // Verify transfer persisted to database
        var transfer = await _context.CreditTransfers
            .FirstOrDefaultAsync(t => t.Id == result.TransferId);
        transfer.Should().NotBeNull();
        transfer!.FromUserId.Should().Be(_testSender.Id);
        transfer.ToUserId.Should().Be(_testRecipient.Id);
        transfer.Amount.Should().Be(amount);
        transfer.Message.Should().Be(message);
        transfer.Status.Should().Be(TransferStatus.Completed);
        transfer.TransactionHash.Should().NotBeNullOrEmpty();

        // Verify wallet balances updated
        var senderBalance = await _walletService.GetBalanceAsync(_testSender.Id);
        var recipientBalance = await _walletService.GetBalanceAsync(_testRecipient.Id);
        senderBalance.Should().Be(50);
        recipientBalance.Should().Be(150);

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CREDIT_TRANSFER" && a.UserId == _testSender.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TransferCreditsAsync_WithIdempotencyKey_ShouldReturnExistingTransfer()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var amount = 30;

        // Act - First transfer
        var firstResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            amount,
            "First attempt",
            idempotencyKey: idempotencyKey);

        // Act - Second transfer with same idempotency key
        var secondResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            amount,
            "Second attempt (should be ignored)",
            idempotencyKey: idempotencyKey);

        // Assert - Both results should be identical
        secondResult.TransferId.Should().Be(firstResult.TransferId);
        secondResult.TransactionHash.Should().Be(firstResult.TransactionHash);

        // Verify only ONE transfer exists in database
        var transferCount = await _context.CreditTransfers
            .CountAsync(t => t.IdempotencyKey == idempotencyKey);
        transferCount.Should().Be(1);

        // Verify balance only deducted once
        var senderBalance = await _walletService.GetBalanceAsync(_testSender.Id);
        senderBalance.Should().Be(70); // 100 - 30
    }

    [Fact]
    public async Task TransferCreditsAsync_IdempotencyKeyMismatch_ShouldThrowException()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();

        // First transfer with key
        await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            30,
            "First",
            idempotencyKey: idempotencyKey);

        // Act - Try to reuse key with DIFFERENT parameters
        var act = async () => await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient2.Id,  // DIFFERENT recipient
            30,
            "Second",
            idempotencyKey: idempotencyKey);

        // Assert - Should throw due to parameter mismatch
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different transfer parameters*");
    }

    [Fact]
    public async Task TransferCreditsAsync_ExceedsSingleLimit_ShouldThrowException()
    {
        // Arrange - Add credits to exceed limit
        await _walletService.AddCreditsAsync(_testSender.Id, 1000, "Top up", CreditTransactionType.Adjustment);

        // Act - Try to transfer more than MAX_SINGLE_TRANSFER (1000)
        var act = async () => await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            1001,
            "Too large");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TransferCreditsAsync_ExceedsDailyAmountLimit_ShouldThrowException()
    {
        // Arrange - Add credits
        await _walletService.AddCreditsAsync(_testSender.Id, 5000, "Top up", CreditTransactionType.Adjustment);

        // Transfer multiple times to reach daily limit (5000)
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 1000, "Transfer 1");
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 1000, "Transfer 2");
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 1000, "Transfer 3");
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 1000, "Transfer 4");
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 1000, "Transfer 5");

        // Act - Try one more transfer (exceeds 5000 daily limit)
        var act = async () => await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            100,
            "Should fail");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TransferCreditsAsync_ExceedsDailyCountLimit_ShouldThrowException()
    {
        // Arrange - Add credits
        await _walletService.AddCreditsAsync(_testSender.Id, 500, "Top up", CreditTransactionType.Adjustment);

        // Make 20 small transfers (MAX_DAILY_TRANSFER_COUNT = 20)
        for (int i = 0; i < 20; i++)
        {
            await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 10, $"Transfer {i + 1}");
        }

        // Act - Try 21st transfer
        var act = async () => await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            10,
            "21st transfer should fail");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TransferCreditsAsync_InsufficientBalance_ShouldThrowException()
    {
        // Act - Try to transfer more than available
        var act = async () => await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            200,  // More than 100 starting balance
            "Too much");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient credits*");
    }

    [Fact]
    public async Task TransferCreditsAsync_ToSameUser_ShouldThrowException()
    {
        // Act - Try to transfer to self
        var act = async () => await _service.TransferCreditsAsync(
            _testSender.Id,
            _testSender.Id,
            10,
            "Self transfer");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task BatchTransferAsync_ValidBatch_ShouldCompleteSuccessfully()
    {
        // Arrange
        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 20, Message = "Payment 1" },
            new() { ToUserId = _testRecipient2.Id, Amount = 30, Message = "Payment 2" }
        };

        // Act
        var result = await _service.BatchTransferAsync(
            _testSender.Id,
            batch,
            "127.0.0.1",
            "test-agent");

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulTransfers.Should().Be(2);
        result.FailedTransfers.Should().Be(0);
        result.TotalAmount.Should().Be(50);
        result.RemainingBalance.Should().Be(50); // 100 - 50

        // Verify transfers persisted with batch ID
        var transfers = await _context.CreditTransfers
            .Where(t => t.BatchId == result.BatchId)
            .ToListAsync();
        transfers.Should().HaveCount(2);

        // Verify balances
        var senderBalance = await _walletService.GetBalanceAsync(_testSender.Id);
        senderBalance.Should().Be(50);
    }

    [Fact]
    public async Task BatchTransferAsync_ExceedsMaxItems_ShouldThrowException()
    {
        // Arrange - Create batch with 11 items (limit is 10)
        var batch = Enumerable.Range(0, 11)
            .Select(i => new BatchTransferItemDto
            {
                ToUserId = _testRecipient.Id,
                Amount = 5,
                Message = $"Transfer {i}"
            })
            .ToList();

        // Act
        var act = async () => await _service.BatchTransferAsync(
            _testSender.Id,
            batch);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetTransferDetailsAsync_ExistingTransfer_ShouldReturnDetails()
    {
        // Arrange
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            40,
            "Test transfer");

        // Act
        var details = await _service.GetTransferDetailsAsync(transferResult.TransferId, _testSender.Id);

        // Assert
        details.Should().NotBeNull();
        details!.Id.Should().Be(transferResult.TransferId);
        details.FromUserId.Should().Be(_testSender.Id);
        details.FromUsername.Should().Be("testsender");
        details.ToUserId.Should().Be(_testRecipient.Id);
        details.ToUsername.Should().Be("testrecipient");
        details.Amount.Should().Be(40);
        details.Status.Should().Be(TransferStatus.Completed);
    }

    [Fact]
    public async Task GetTransferDetailsAsync_UnauthorizedUser_ShouldReturnNull()
    {
        // Arrange
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            30,
            "Private transfer");

        var unauthorizedUserId = Guid.NewGuid();

        // Act - Try to get details as unauthorized user
        var details = await _service.GetTransferDetailsAsync(transferResult.TransferId, unauthorizedUserId);

        // Assert
        details.Should().BeNull();
    }

    #endregion

    #region Transfer History Tests

    [Fact]
    public async Task GetTransferHistoryAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange - Create multiple transfers
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 10, "Transfer 1");
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 20, "Transfer 2");
        await _service.TransferCreditsAsync(_testRecipient.Id, _testSender.Id, 15, "Transfer 3");

        var request = new TransferHistoryRequestDto
        {
            Page = 1,
            PageSize = 10,
            Direction = TransferDirection.Sent  // Only sent transfers
        };

        // Act
        var history = await _service.GetTransferHistoryAsync(_testSender.Id, request);

        // Assert
        history.Should().NotBeNull();
        history.Transfers.Should().HaveCount(2);  // Only sent transfers
        history.Transfers.Should().OnlyContain(t => t.FromUserId == _testSender.Id);
        history.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTransferHistoryAsync_Pagination_ShouldReturnCorrectPage()
    {
        // Arrange - Create 15 transfers (reduced from 25 to avoid daily transfer limit of 20)
        for (int i = 0; i < 15; i++)
        {
            await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 1, $"Transfer {i}");
        }

        var request = new TransferHistoryRequestDto
        {
            Page = 2,
            PageSize = 10
        };

        // Act
        var history = await _service.GetTransferHistoryAsync(_testSender.Id, request);

        // Assert
        history.Should().NotBeNull();
        history.Transfers.Should().HaveCount(5);  // 15 total - 10 on page 1 = 5 on page 2
        history.Page.Should().Be(2);
        history.TotalPages.Should().Be(2);  // 15 transfers / 10 per page = 2 pages
        history.HasNextPage.Should().BeFalse();  // Page 2 is the last page
        history.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserTransfersAsync_ExcessivePageNumber_ShouldCapToMaxPage()
    {
        // Arrange
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 10, "Transfer");

        // Act - Request page 10000 (exceeds MAX_PAGE = 1000)
        var history = await _service.GetUserTransfersAsync(_testSender.Id, TransferDirection.Both, page: 10000);

        // Assert - Should be capped to MAX_PAGE (1000)
        history.Should().NotBeNull();
        history.Page.Should().Be(1000);
    }

    #endregion

    #region Reversal Operations Tests

    [Fact]
    public async Task ReverseTransferAsync_Within24Hours_ShouldSucceed()
    {
        // Arrange
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            40,
            "Will be reversed");

        // Act
        var reversed = await _service.ReverseTransferAsync(
            transferResult.TransferId,
            _testSender.Id,
            "Customer requested refund");

        // Assert
        reversed.Should().BeTrue();

        // Verify transfer marked as reversed
        var transfer = await _context.CreditTransfers
            .FirstOrDefaultAsync(t => t.Id == transferResult.TransferId);
        transfer!.Status.Should().Be(TransferStatus.Reversed);
        transfer.ReversalReason.Should().Be("Customer requested refund");

        // Verify balances restored
        var senderBalance = await _walletService.GetBalanceAsync(_testSender.Id);
        var recipientBalance = await _walletService.GetBalanceAsync(_testRecipient.Id);
        senderBalance.Should().Be(100);  // Refunded
        recipientBalance.Should().Be(100);  // Deducted
    }

    [Fact]
    public async Task CanReverseTransferAsync_RecentTransfer_ShouldReturnTrue()
    {
        // Arrange
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            30,
            "Recent transfer");

        // Act
        var canReverse = await _service.CanReverseTransferAsync(transferResult.TransferId, _testSender.Id);

        // Assert
        canReverse.Should().BeTrue();
    }

    [Fact]
    public async Task CancelTransferAsync_PendingTransfer_ShouldSucceed()
    {
        // Arrange - Create a pending transfer manually
        var transfer = new CreditTransfer
        {
            FromUserId = _testSender.Id,
            ToUserId = _testRecipient.Id,
            Amount = 25,
            Status = TransferStatus.Pending
        };
        _context.CreditTransfers.Add(transfer);
        await _context.SaveChangesAsync();

        // Act
        var cancelled = await _service.CancelTransferAsync(
            transfer.Id,
            _testSender.Id,
            "User cancelled");

        // Assert
        cancelled.Should().BeTrue();

        // Verify status updated
        var updatedTransfer = await _context.CreditTransfers.FindAsync(transfer.Id);
        updatedTransfer!.Status.Should().Be(TransferStatus.Cancelled);
        updatedTransfer.ReversalReason.Should().Be("User cancelled");
    }

    #endregion

    #region Validation & Limits Tests

    [Fact]
    public async Task ValidateTransferAsync_ValidTransfer_ShouldReturnTrue()
    {
        // Act
        var isValid = await _service.ValidateTransferAsync(_testSender.Id, _testRecipient.Id, 50);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTransferAsync_ExceedsSingleLimit_ShouldReturnFalse()
    {
        // Act - Amount exceeds MAX_SINGLE_TRANSFER (1000)
        var isValid = await _service.ValidateTransferAsync(_testSender.Id, _testRecipient.Id, 1001);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTransferAsync_SameUser_ShouldReturnFalse()
    {
        // Act
        var isValid = await _service.ValidateTransferAsync(_testSender.Id, _testSender.Id, 50);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task GetTransferLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Arrange - Make one transfer
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 30, "Test");

        // Act
        var limits = await _service.GetTransferLimitsAsync(_testSender.Id);

        // Assert
        limits.Should().NotBeNull();
        limits.MaxSingleTransfer.Should().Be(1000);
        limits.MaxDailyTransfer.Should().Be(5000);
        limits.MaxDailyCount.Should().Be(20);
        limits.DailyTransferredAmount.Should().Be(30);
        limits.DailyTransferCount.Should().Be(1);
        limits.RemainingDailyAmount.Should().Be(4970);  // 5000 - 30
        limits.RemainingDailyCount.Should().Be(19);  // 20 - 1
        limits.WalletBalance.Should().Be(70);  // 100 - 30
        limits.ReversalWindowHours.Should().Be(24);
    }

    [Fact]
    public async Task ValidateBatchTransferAsync_ValidBatch_ShouldReturnTrue()
    {
        // Arrange
        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 20, Message = "Payment 1" },
            new() { ToUserId = _testRecipient2.Id, Amount = 30, Message = "Payment 2" }
        };

        // Act
        var isValid = await _service.ValidateBatchTransferAsync(_testSender.Id, batch);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBatchTransferAsync_ExceedsMaxItems_ShouldReturnFalse()
    {
        // Arrange - 11 items (max is 10)
        var batch = Enumerable.Range(0, 11)
            .Select(i => new BatchTransferItemDto { ToUserId = _testRecipient.Id, Amount = 5 })
            .ToList();

        // Act
        var isValid = await _service.ValidateBatchTransferAsync(_testSender.Id, batch);

        // Assert
        isValid.Should().BeFalse();
    }

    #endregion

    #region Receipt Operations Tests

    [Fact]
    public async Task GenerateReceiptAsync_CompletedTransfer_ShouldReturnReceipt()
    {
        // Arrange
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            35,
            "Receipt test");

        // Act
        var receipt = await _service.GenerateReceiptAsync(transferResult.TransferId, _testSender.Id);

        // Assert
        receipt.Should().NotBeNull();
        receipt!.TransferId.Should().Be(transferResult.TransferId);
        receipt.TransactionHash.Should().Be(transferResult.TransactionHash);
        receipt.FromUser.Should().Be("testsender");
        receipt.ToUser.Should().Be("testrecipient");
        receipt.Amount.Should().Be(35);
        receipt.ReceiptSignature.Should().NotBeNullOrEmpty();
        receipt.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateReceiptAsync_NonExistentTransfer_ShouldReturnNull()
    {
        // Act
        var receipt = await _service.GenerateReceiptAsync(Guid.NewGuid(), _testSender.Id);

        // Assert
        receipt.Should().BeNull();
    }

    [Fact]
    public async Task VerifyReceiptAsync_ValidSignature_ShouldReturnValid()
    {
        // Arrange - Create transfer and generate receipt
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            40,
            "Verify test");

        var receipt = await _service.GenerateReceiptAsync(transferResult.TransferId, _testSender.Id);

        // Act
        var verification = await _service.VerifyReceiptAsync(
            transferResult.TransferId,
            receipt!.ReceiptSignature);

        // Assert
        verification.Should().NotBeNull();
        verification.IsValid.Should().BeTrue();
        verification.Message.Should().Be("Receipt is valid");
        verification.Transfer.Should().NotBeNull();
        verification.Transfer!.Amount.Should().Be(40);
    }

    [Fact]
    public async Task VerifyReceiptAsync_InvalidSignature_ShouldReturnInvalid()
    {
        // Arrange
        var transferResult = await _service.TransferCreditsAsync(
            _testSender.Id,
            _testRecipient.Id,
            40,
            "Invalid sig test");

        // Act
        var verification = await _service.VerifyReceiptAsync(
            transferResult.TransferId,
            "invalid-signature-12345");

        // Assert
        verification.Should().NotBeNull();
        verification.IsValid.Should().BeFalse();
        verification.Message.Should().Be("Invalid receipt signature");
        verification.Transfer.Should().BeNull();
    }

    #endregion

    #region Risk & Statistics Tests

    [Fact]
    public async Task AnalyzeTransferRiskAsync_HighAmount_ShouldReturnMediumRisk()
    {
        // Act - Amount >= 500 triggers risk
        var assessment = await _service.AnalyzeTransferRiskAsync(_testSender.Id, 600);

        // Assert
        assessment.Should().NotBeNull();
        assessment.RiskScore.Should().BeGreaterThanOrEqualTo(30);
        assessment.RiskLevel.Should().BeOneOf(RiskLevel.Medium, RiskLevel.High);
        assessment.RiskFactors.Should().Contain("High transfer amount");
    }

    [Fact]
    public async Task AnalyzeTransferRiskAsync_HighVelocity_ShouldReturnHighRisk()
    {
        // Arrange - Create 5 transfers in last hour
        for (int i = 0; i < 5; i++)
        {
            await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 10, $"Tx {i}");
        }

        // Act - 6th transfer triggers velocity risk
        var assessment = await _service.AnalyzeTransferRiskAsync(_testSender.Id, 10);

        // Assert
        assessment.Should().NotBeNull();
        assessment.RiskScore.Should().BeGreaterThanOrEqualTo(40);
        assessment.RiskLevel.Should().BeOneOf(RiskLevel.Medium, RiskLevel.High);
        assessment.RiskFactors.Should().Contain("High transfer frequency");
    }

    [Fact]
    public async Task GetTransferStatisticsAsync_ShouldReturnAccurateStats()
    {
        // Arrange - Create multiple transfers
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 20, "Tx 1");
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 30, "Tx 2");
        await _service.TransferCreditsAsync(_testRecipient.Id, _testSender.Id, 15, "Tx 3");

        // Act
        var stats = await _service.GetTransferStatisticsAsync(_testSender.Id, TimeSpan.FromDays(1));

        // Assert
        stats.Should().NotBeNull();
        stats.TransfersSent.Should().Be(2);
        stats.TransfersReceived.Should().Be(1);
        stats.TotalAmountSent.Should().Be(50);  // 20 + 30
        stats.TotalAmountReceived.Should().Be(15);
        stats.AverageTransferAmount.Should().Be(25);  // (20 + 30) / 2
    }

    [Fact]
    public async Task GetSystemTransferStatisticsAsync_ShouldReturnPlatformStats()
    {
        // Arrange - Create transfers across multiple users
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 40, "Tx 1");
        await _service.TransferCreditsAsync(_testRecipient.Id, _testRecipient2.Id, 25, "Tx 2");

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var stats = await _service.GetSystemTransferStatisticsAsync(startDate, endDate);

        // Assert
        stats.Should().NotBeNull();
        stats.TotalTransfers.Should().Be(2);
        stats.SuccessfulTransfers.Should().Be(2);
        stats.TotalVolume.Should().Be(65);  // 40 + 25
        stats.AverageTransferAmount.Should().Be(32.5m);  // (40 + 25) / 2
        stats.ActiveTransferUsers.Should().Be(3);  // All 3 users involved
    }

    #endregion

    #region Edge Case Tests for Coverage (Phase 1.2)

    [Fact]
    public async Task AnalyzeTransferRiskAsync_HighIPActivity_ShouldReturnHighRisk()
    {
        // Arrange - Create 11 transfers from same IP in last hour
        var testIP = "192.168.1.100";

        for (int i = 0; i < 11; i++)
        {
            await _service.TransferCreditsAsync(
                _testSender.Id,
                _testRecipient.Id,
                5,
                $"Transfer {i}",
                initiatedFromIP: testIP);
        }

        // Act - Analyze risk with same IP
        var assessment = await _service.AnalyzeTransferRiskAsync(_testSender.Id, 10, testIP);

        // Assert - Should detect high IP activity (> 10 transfers from same IP in last hour)
        assessment.Should().NotBeNull();
        assessment.RiskFactors.Should().Contain("High IP activity");
        assessment.RiskScore.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public async Task GetUserTransfersAsync_InvalidPageSize_ShouldCapToValidRange()
    {
        // Arrange
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 10, "Test");

        // Act - Request with page size too small (< 1)
        var result1 = await _service.GetUserTransfersAsync(_testSender.Id, pageSize: 0);

        // Assert - Should be capped to MIN_PAGE_SIZE (1)
        result1.Should().NotBeNull();
        result1.PageSize.Should().BeGreaterThanOrEqualTo(1);

        // Act - Request with page size too large (> 100)
        var result2 = await _service.GetUserTransfersAsync(_testSender.Id, pageSize: 200);

        // Assert - Should be capped to MAX_PAGE_SIZE (100)
        result2.Should().NotBeNull();
        result2.PageSize.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task GetUserTransfersAsync_NegativePageNumber_ShouldResetToMinPage()
    {
        // Arrange
        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 10, "Test");

        // Act - Request with negative page number
        var result = await _service.GetUserTransfersAsync(_testSender.Id, page: -5);

        // Assert - Should be reset to MIN_PAGE (1)
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetTransferHistoryAsync_WithDateRangeFilters_ShouldReturnFilteredResults()
    {
        // Arrange - Create transfers at different times
        var oldTransfer = new CreditTransfer
        {
            FromUserId = _testSender.Id,
            ToUserId = _testRecipient.Id,
            Amount = 10,
            Status = TransferStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        _context.CreditTransfers.Add(oldTransfer);
        await _context.SaveChangesAsync();

        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 20, "Recent transfer");

        var request = new TransferHistoryRequestDto
        {
            Page = 1,
            PageSize = 10,
            StartDate = DateTime.UtcNow.AddDays(-1),  // Only last day
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var history = await _service.GetTransferHistoryAsync(_testSender.Id, request);

        // Assert - Should only return recent transfer, not the old one
        history.Should().NotBeNull();
        history.Transfers.Should().HaveCount(1);
        history.Transfers.First().Amount.Should().Be(20);
    }

    [Fact]
    public async Task GetTransferHistoryAsync_WithStatusFilter_ShouldReturnOnlyMatchingStatus()
    {
        // Arrange - Create transfers with different statuses
        var pendingTransfer = new CreditTransfer
        {
            FromUserId = _testSender.Id,
            ToUserId = _testRecipient.Id,
            Amount = 15,
            Status = TransferStatus.Pending
        };
        _context.CreditTransfers.Add(pendingTransfer);
        await _context.SaveChangesAsync();

        await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 25, "Completed transfer");

        var request = new TransferHistoryRequestDto
        {
            Page = 1,
            PageSize = 10,
            Status = TransferStatus.Completed  // Only completed
        };

        // Act
        var history = await _service.GetTransferHistoryAsync(_testSender.Id, request);

        // Assert - Should only return completed transfer
        history.Should().NotBeNull();
        history.Transfers.Should().OnlyContain(t => t.Status == TransferStatus.Completed);
        history.Transfers.Should().HaveCount(1);
        history.Transfers.First().Amount.Should().Be(25);
    }

    #endregion

    #region Additional Coverage Tests (Phase 5.1) - Targeted Line Coverage

    [Fact]
    public async Task BatchTransferAsync_InsufficientBalance_ShouldThrowException()
    {
        // Arrange - Sender has 100 credits, trying to send 150 total
        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 80, Message = "Transfer 1" },
            new() { ToUserId = _testRecipient2.Id, Amount = 70, Message = "Transfer 2" }
        };

        // Act & Assert - Should throw due to insufficient balance (covers lines 306-308)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.BatchTransferAsync(_testSender.Id, batch, "127.0.0.1", "test-agent")
        );
    }

    [Fact]
    public async Task BatchTransferAsync_ExceedingDailyAmountLimit_ShouldThrowException()
    {
        // Arrange - Add enough credits but exceed daily limit (5000)
        await _walletService.AddCreditsAsync(_testSender.Id, 6000, "Top up", CreditTransactionType.Adjustment);

        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 3000, Message = "Transfer 1" },
            new() { ToUserId = _testRecipient2.Id, Amount = 2500, Message = "Transfer 2" }
        };

        // Act & Assert - Should throw due to daily limit (covers lines 310-312)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.BatchTransferAsync(_testSender.Id, batch, "127.0.0.1", "test-agent")
        );
    }

    [Fact]
    public async Task BatchTransferAsync_ExceedingDailyCountLimit_ShouldThrowException()
    {
        // Arrange - Create 19 transfers already (limit is 20)
        await _walletService.AddCreditsAsync(_testSender.Id, 500, "Top up", CreditTransactionType.Adjustment);

        for (int i = 0; i < 19; i++)
        {
            await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 5, $"Transfer {i}");
        }

        // Now try batch with 2 more (would exceed 20 total)
        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 5, Message = "Batch 1" },
            new() { ToUserId = _testRecipient2.Id, Amount = 5, Message = "Batch 2" }
        };

        // Act & Assert - Should throw due to daily count limit (covers lines 314-316)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.BatchTransferAsync(_testSender.Id, batch, "127.0.0.1", "test-agent")
        );
    }

    [Fact]
    public async Task BatchTransferAsync_LockAcquisitionFailure_ShouldThrowException()
    {
        // Arrange - Force lock service to fail acquisition
        _lockService.SetShouldFail(true);

        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 10, Message = "Test" }
        };

        // Act & Assert - Should throw due to lock failure (covers lines 278-280)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.BatchTransferAsync(_testSender.Id, batch, "127.0.0.1", "test-agent")
        );

        // Cleanup
        _lockService.SetShouldFail(false);
    }

    [Fact]
    public async Task AnalyzeTransferRiskAsync_LowRiskScenario_ShouldReturnLowRisk()
    {
        // Arrange - Small amount, no recent activity
        var testIP = "10.0.0.50";

        // Act - Small transfer with no prior activity (covers line 770: RiskLevel.Low)
        var assessment = await _service.AnalyzeTransferRiskAsync(_testSender.Id, 10, testIP);

        // Assert
        assessment.Should().NotBeNull();
        assessment.RiskScore.Should().BeLessThan(30);
        assessment.RiskLevel.Should().Be(RiskLevel.Low);
        assessment.IsAllowed.Should().BeTrue();
        assessment.RecommendedAction.Should().Be("Allow");
    }

    [Fact]
    public async Task BatchTransferAsync_TotalAmountCalculation_ShouldSumCorrectly()
    {
        // Arrange - Ensure sufficient balance
        await _walletService.AddCreditsAsync(_testSender.Id, 200, "Top up", CreditTransactionType.Adjustment);

        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 25, Message = "Transfer 1" },
            new() { ToUserId = _testRecipient2.Id, Amount = 35, Message = "Transfer 2" },
            new() { ToUserId = _testRecipient.Id, Amount = 40, Message = "Transfer 3" }
        };

        // Act - Covers lines 303-304 (total amount calculation)
        var result = await _service.BatchTransferAsync(_testSender.Id, batch, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulTransfers.Should().Be(3);
        result.TotalAmount.Should().Be(100);  // 25 + 35 + 40
    }

    [Fact]
    public async Task AnalyzeTransferRiskAsync_CombinedHighRiskFactors_ShouldReturnCriticalRisk()
    {
        // Arrange - Create extreme high-risk scenario
        await _walletService.AddCreditsAsync(_testSender.Id, 1500, "Top up", CreditTransactionType.Adjustment);
        var testIP = "192.168.100.200";

        // Create 5 recent transfers for velocity risk (riskScore += 40)
        for (int i = 0; i < 5; i++)
        {
            await _service.TransferCreditsAsync(_testSender.Id, _testRecipient.Id, 20, $"Velocity {i}", initiatedFromIP: testIP);
        }

        // Create 11 more transfers from same IP for high IP activity (riskScore += 30)
        // Total: 5 + 11 = 16 transfers (under 20 daily limit)
        for (int i = 0; i < 11; i++)
        {
            await _service.TransferCreditsAsync(_testSender.Id, _testRecipient2.Id, 10, $"IP {i}", initiatedFromIP: testIP);
        }

        // Act - Analyze with high amount (500+) + velocity + high IP activity
        // Expected: 30 (amount >= 500) + 40 (velocity >= 5) + 30 (IP > 10) = 100 (Critical)
        var assessment = await _service.AnalyzeTransferRiskAsync(_testSender.Id, 550, testIP);

        // Assert - Covers line 773 (Critical risk level)
        assessment.Should().NotBeNull();
        assessment.RiskScore.Should().BeGreaterThanOrEqualTo(80);
        assessment.RiskLevel.Should().Be(RiskLevel.Critical);
        assessment.IsAllowed.Should().BeFalse();
        assessment.RecommendedAction.Should().Be("Block transaction");
    }

    [Fact]
    public async Task BatchTransferAsync_ItemFailureDueToInvalidData_ShouldCaptureError()
    {
        // Arrange - Create batch with invalid recipient (non-existent user)
        await _walletService.AddCreditsAsync(_testSender.Id, 300, "Top up", CreditTransactionType.Adjustment);

        var batch = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _testRecipient.Id, Amount = 50, Message = "Valid transfer" },
            new() { ToUserId = Guid.NewGuid(), Amount = 75, Message = "Invalid - no wallet exists" },
            new() { ToUserId = _testRecipient2.Id, Amount = 100, Message = "Another valid" }
        };

        // Act - Batch should partially succeed (covers lines 340-353: exception handling)
        var result = await _service.BatchTransferAsync(_testSender.Id, batch, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulTransfers.Should().Be(2);  // Two valid recipients
        result.FailedTransfers.Should().Be(1);      // One invalid recipient
        result.TotalAmount.Should().Be(150);        // Only successful amounts: 50 + 100
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Amount.Should().Be(75);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
