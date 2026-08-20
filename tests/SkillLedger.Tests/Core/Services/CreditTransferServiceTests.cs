using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using FluentAssertions;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// TDD tests for CreditTransferService covering peer-to-peer transfers,
/// batch operations, reversals, fraud detection, and receipt generation.
/// Following Red-Green-Refactor methodology.
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Financial")]
public class CreditTransferServiceTests : IntegrationTestBase
{
    private readonly ICreditTransferService _transferService;
    private readonly ICreditWalletService _walletService;
    private User _senderUser = null!;
    private User _recipientUser = null!;
    private User _thirdUser = null!;

    public CreditTransferServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _transferService = ServiceScope.ServiceProvider.GetRequiredService<ICreditTransferService>();
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _senderUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "sender@transfer-test.com",
            UserName = "sender@transfer-test.com",
            NormalizedEmail = "SENDER@TRANSFER-TEST.COM",
            NormalizedUserName = "SENDER@TRANSFER-TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_senderUser);

        _recipientUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "recipient@transfer-test.com",
            UserName = "recipient@transfer-test.com",
            NormalizedEmail = "RECIPIENT@TRANSFER-TEST.COM",
            NormalizedUserName = "RECIPIENT@TRANSFER-TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_recipientUser);

        _thirdUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "third@transfer-test.com",
            UserName = "third@transfer-test.com",
            NormalizedEmail = "THIRD@TRANSFER-TEST.COM",
            NormalizedUserName = "THIRD@TRANSFER-TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_thirdUser);

        await Context.SaveChangesAsync();

        // Create wallets for all users with starting credits
        await _walletService.CreateWalletAsync(_senderUser.Id);
        await _walletService.CreateWalletAsync(_recipientUser.Id);
        await _walletService.CreateWalletAsync(_thirdUser.Id);

        // Add extra credits to sender for testing
        await _walletService.AddCreditsAsync(_senderUser.Id, 900, "Test funding", CreditTransactionType.Purchase);
    }

    #region Single Transfer Operations Tests

    [Fact]
    public async Task TransferCreditsAsync_ValidTransfer_ReturnsCompletedTransfer()
    {
        // Arrange
        var amount = 50;
        var message = "Test transfer";

        // Act
        var result = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            amount,
            message);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(amount);
        result.Status.Should().Be(TransferStatus.Completed);
        result.TransactionHash.Should().NotBeNullOrEmpty();
        result.TransferId.Should().NotBe(Guid.Empty);

        // Verify wallet balances updated
        var senderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        var recipientBalance = await _walletService.GetBalanceAsync(_recipientUser.Id);

        senderBalance.Should().Be(950); // 1000 - 50
        recipientBalance.Should().Be(150); // 100 + 50
    }

    [Fact]
    public async Task TransferCreditsAsync_WithIdempotencyKey_ReturnsSameTransferOnDuplicate()
    {
        // Arrange - BUG-040 FIX test
        var amount = 30;
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act - First transfer
        var firstResult = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            amount,
            "First request",
            idempotencyKey: idempotencyKey);

        // Act - Duplicate request with same idempotency key
        var secondResult = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            amount,
            "Duplicate request",
            idempotencyKey: idempotencyKey);

        // Assert - Should return same transfer
        secondResult.TransferId.Should().Be(firstResult.TransferId);
        secondResult.TransactionHash.Should().Be(firstResult.TransactionHash);

        // Verify only one transfer was made
        var senderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        senderBalance.Should().Be(970); // 1000 - 30 (only once)
    }

    [Fact]
    public async Task TransferCreditsAsync_IdempotencyKeyWithDifferentParams_ThrowsException()
    {
        // Arrange - BUG-HIGH-009 FIX test
        var idempotencyKey = Guid.NewGuid().ToString();

        // First transfer
        await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "First request",
            idempotencyKey: idempotencyKey);

        // Act & Assert - Different amount with same key should fail
        Func<Task> act = async () => await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            100, // Different amount
            "Tampered request",
            idempotencyKey: idempotencyKey);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different transfer parameters*");
    }

    [Fact]
    public async Task TransferCreditsAsync_SelfTransfer_ReturnsFalseOnValidation()
    {
        // Arrange - Self transfer should be rejected

        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _senderUser.Id, // Same user
            50);

        // Assert
        isValid.Should().BeFalse("self-transfers should not be allowed");
    }

    [Fact]
    public async Task TransferCreditsAsync_InsufficientBalance_ReturnsFalseOnValidation()
    {
        // Arrange
        var excessiveAmount = 10000; // More than sender's balance

        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _recipientUser.Id,
            excessiveAmount);

        // Assert
        isValid.Should().BeFalse("transfers exceeding balance should not be allowed");
    }

    [Fact]
    public async Task TransferCreditsAsync_ExceedsSingleTransferLimit_ReturnsFalseOnValidation()
    {
        // Arrange - MAX_SINGLE_TRANSFER is 1000
        var excessiveAmount = 1001;

        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _recipientUser.Id,
            excessiveAmount);

        // Assert
        isValid.Should().BeFalse("transfers exceeding single transfer limit should not be allowed");
    }

    [Fact]
    public async Task GetTransferDetailsAsync_ValidTransfer_ReturnsDetails()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            25,
            "Test transfer for details");

        // Act
        var details = await _transferService.GetTransferDetailsAsync(
            transfer.TransferId,
            _senderUser.Id);

        // Assert
        details.Should().NotBeNull();
        details!.Id.Should().Be(transfer.TransferId);
        details.FromUserId.Should().Be(_senderUser.Id);
        details.ToUserId.Should().Be(_recipientUser.Id);
        details.Amount.Should().Be(25);
        details.Status.Should().Be(TransferStatus.Completed);
        details.Message.Should().Contain("Test transfer for details");
    }

    [Fact]
    public async Task GetTransferDetailsAsync_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            25,
            "Test transfer");

        // Act - Third user tries to access transfer
        var details = await _transferService.GetTransferDetailsAsync(
            transfer.TransferId,
            _thirdUser.Id);

        // Assert
        details.Should().BeNull("unauthorized users should not see transfer details");
    }

    #endregion

    #region Batch Transfer Tests

    [Fact]
    public async Task BatchTransferAsync_ValidBatch_CompletesAllTransfers()
    {
        // Arrange
        var transfers = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _recipientUser.Id, Amount = 50, Message = "Batch 1" },
            new() { ToUserId = _thirdUser.Id, Amount = 30, Message = "Batch 2" }
        };

        // Act
        var result = await _transferService.BatchTransferAsync(
            _senderUser.Id,
            transfers);

        // Assert
        result.Should().NotBeNull();
        result.SuccessfulTransfers.Should().Be(2);
        result.FailedTransfers.Should().Be(0);
        result.TotalAmount.Should().Be(80);
        result.Transfers.Should().HaveCount(2);

        // Verify balances
        var senderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        var recipientBalance = await _walletService.GetBalanceAsync(_recipientUser.Id);
        var thirdBalance = await _walletService.GetBalanceAsync(_thirdUser.Id);

        senderBalance.Should().Be(920); // 1000 - 80
        recipientBalance.Should().Be(150); // 100 + 50
        thirdBalance.Should().Be(130); // 100 + 30
    }

    [Fact]
    public async Task BatchTransferAsync_PartialFailure_ReportsErrors()
    {
        // Arrange - Include an invalid transfer (self-transfer)
        var initialSenderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        var transfers = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _recipientUser.Id, Amount = 50, Message = "Valid" },
            new() { ToUserId = _senderUser.Id, Amount = 30, Message = "Self-transfer" } // Invalid
        };

        // Act
        Func<Task> act = async () => await _transferService.BatchTransferAsync(
            _senderUser.Id,
            transfers);

        // Assert - Security-sensitive invalid batch items reject the whole batch before moving credits
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Batch transfer validation failed*");

        var senderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        senderBalance.Should().Be(initialSenderBalance);
    }

    [Fact]
    public async Task ValidateBatchTransferAsync_ExceedsMaxItems_ReturnsFalse()
    {
        // Arrange - More than 10 transfers in batch
        var transfers = Enumerable.Range(1, 11)
            .Select(i => new BatchTransferItemDto
            {
                ToUserId = _recipientUser.Id,
                Amount = 1,
                Message = $"Transfer {i}"
            })
            .ToList();

        // Act
        var isValid = await _transferService.ValidateBatchTransferAsync(
            _senderUser.Id,
            transfers);

        // Assert
        isValid.Should().BeFalse("batch transfers are limited to 10 items");
    }

    [Fact]
    [SecurityTest]
    public async Task ValidateBatchTransferAsync_ItemExceedsSingleTransferLimit_ReturnsFalse()
    {
        // Arrange - A batch total can be within daily limits while one item exceeds the single-transfer cap
        var transfers = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _recipientUser.Id, Amount = 1001, Message = "Oversized item" }
        };

        // Act
        var isValid = await _transferService.ValidateBatchTransferAsync(
            _senderUser.Id,
            transfers);

        // Assert
        isValid.Should().BeFalse("each batch item must respect the single-transfer limit");
    }

    [Fact]
    [SecurityTest]
    public async Task BatchTransferAsync_WithSelfTransferItem_ThrowsAndDoesNotTransfer()
    {
        // Arrange
        var initialSenderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        var transfers = new List<BatchTransferItemDto>
        {
            new() { ToUserId = _senderUser.Id, Amount = 50, Message = "Self-transfer" }
        };

        // Act
        Func<Task> act = async () => await _transferService.BatchTransferAsync(
            _senderUser.Id,
            transfers);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Batch transfer validation failed*");

        var senderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        senderBalance.Should().Be(initialSenderBalance);
    }

    #endregion

    #region Transfer History Tests

    [Fact]
    public async Task GetTransferHistoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create multiple transfers
        for (int i = 0; i < 5; i++)
        {
            await _transferService.TransferCreditsAsync(
                _senderUser.Id,
                _recipientUser.Id,
                10,
                $"Transfer {i}");
        }

        var request = new TransferHistoryRequestDto
        {
            Page = 1,
            PageSize = 3
        };

        // Act
        var result = await _transferService.GetTransferHistoryAsync(
            _senderUser.Id,
            request);

        // Assert
        result.Should().NotBeNull();
        result.Transfers.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetTransferHistoryAsync_FilterByDirection_ReturnsSentOnly()
    {
        // Arrange - Create transfers in both directions
        await _transferService.TransferCreditsAsync(_senderUser.Id, _recipientUser.Id, 20, "Sent");
        await _transferService.TransferCreditsAsync(_recipientUser.Id, _senderUser.Id, 10, "Received");

        var request = new TransferHistoryRequestDto
        {
            Direction = TransferDirection.Sent,
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _transferService.GetTransferHistoryAsync(
            _senderUser.Id,
            request);

        // Assert
        result.Transfers.Should().AllSatisfy(t =>
            t.FromUserId.Should().Be(_senderUser.Id));
    }

    [Fact]
    public async Task GetUserTransfersAsync_ValidRequest_ReturnsTransfers()
    {
        // Arrange
        await _transferService.TransferCreditsAsync(_senderUser.Id, _recipientUser.Id, 25, "Test");

        // Act
        var result = await _transferService.GetUserTransfersAsync(
            _senderUser.Id,
            TransferDirection.Both,
            page: 1,
            pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Transfers.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    #endregion

    #region Transfer Reversal Tests

    [Fact]
    public async Task CanReverseTransferAsync_WithinWindow_ReturnsTrue()
    {
        // Arrange - Complete a transfer
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Test transfer");

        // Act
        var canReverse = await _transferService.CanReverseTransferAsync(
            transfer.TransferId,
            _senderUser.Id);

        // Assert - Within 24-hour window
        canReverse.Should().BeTrue("completed transfers should be reversible within 24 hours");
    }

    [Fact]
    public async Task ReverseTransferAsync_ValidReversal_ReturnsTrue()
    {
        // Arrange
        var initialSenderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Test transfer");

        var balanceAfterTransfer = await _walletService.GetBalanceAsync(_senderUser.Id);
        balanceAfterTransfer.Should().Be(initialSenderBalance - 50);

        // Act
        var reversed = await _transferService.ReverseTransferAsync(
            transfer.TransferId,
            _senderUser.Id,
            "Customer requested refund");

        // Assert
        reversed.Should().BeTrue();

        // Verify balance restored
        var balanceAfterReversal = await _walletService.GetBalanceAsync(_senderUser.Id);
        balanceAfterReversal.Should().Be(initialSenderBalance);

        // Verify transfer status
        var details = await _transferService.GetTransferDetailsAsync(
            transfer.TransferId,
            _senderUser.Id);
        details!.Status.Should().Be(TransferStatus.Reversed);
    }

    [Fact]
    public async Task ReverseTransferAsync_AlreadyReversed_ReturnsFalse()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Test transfer");

        // First reversal
        await _transferService.ReverseTransferAsync(
            transfer.TransferId,
            _senderUser.Id,
            "First reversal");

        // Act - Try second reversal
        var secondReversal = await _transferService.ReverseTransferAsync(
            transfer.TransferId,
            _senderUser.Id,
            "Second reversal attempt");

        // Assert
        secondReversal.Should().BeFalse("already reversed transfers cannot be reversed again");
    }

    [Fact]
    [SecurityTest]
    public async Task ReverseTransferAsync_UnauthorizedUser_ReturnsFalseAndDoesNotReverse()
    {
        // Arrange
        var initialSenderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Test transfer");

        // Act - Third user is not a party to this transfer
        var reversed = await _transferService.ReverseTransferAsync(
            transfer.TransferId,
            _thirdUser.Id,
            "Unauthorized reversal");

        // Assert
        reversed.Should().BeFalse();

        var senderBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        senderBalance.Should().Be(initialSenderBalance - 50);

        var details = await _transferService.GetTransferDetailsAsync(
            transfer.TransferId,
            _senderUser.Id);
        details!.Status.Should().Be(TransferStatus.Completed);
    }

    #endregion

    #region Transfer Limits Tests

    [Fact]
    public async Task GetTransferLimitsAsync_NewUser_ReturnsDefaultLimits()
    {
        // Act
        var limits = await _transferService.GetTransferLimitsAsync(_senderUser.Id);

        // Assert
        limits.Should().NotBeNull();
        limits.MaxSingleTransfer.Should().Be(1000);
        limits.MaxDailyTransfer.Should().Be(5000);
        limits.MaxDailyCount.Should().Be(20);
        limits.ReversalWindowHours.Should().Be(24);
    }

    [Fact]
    public async Task GetTransferLimitsAsync_AfterTransfers_ReflectsUsage()
    {
        // Arrange - Make a transfer
        await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            100,
            "Test transfer");

        // Act
        var limits = await _transferService.GetTransferLimitsAsync(_senderUser.Id);

        // Assert
        limits.DailyTransferredAmount.Should().Be(100);
        limits.DailyTransferCount.Should().Be(1);
        limits.RemainingDailyAmount.Should().Be(4900); // 5000 - 100
        limits.RemainingDailyCount.Should().Be(19); // 20 - 1
    }

    [Fact]
    public async Task ValidateTransferAsync_WithinLimits_ReturnsTrue()
    {
        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _recipientUser.Id,
            100);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTransferAsync_NegativeAmount_ReturnsFalse()
    {
        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _recipientUser.Id,
            -50);

        // Assert
        isValid.Should().BeFalse("negative amounts should not be allowed");
    }

    [Fact]
    public async Task ValidateTransferAsync_ZeroAmount_ReturnsFalse()
    {
        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _recipientUser.Id,
            0);

        // Assert
        isValid.Should().BeFalse("zero amounts should not be allowed");
    }

    #endregion

    #region Receipt Generation Tests

    [Fact]
    public async Task GenerateReceiptAsync_CompletedTransfer_ReturnsReceipt()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Receipt test transfer");

        // Act
        var receipt = await _transferService.GenerateReceiptAsync(
            transfer.TransferId,
            _senderUser.Id);

        // Assert
        receipt.Should().NotBeNull();
        receipt!.TransferId.Should().Be(transfer.TransferId);
        receipt.Amount.Should().Be(50);
        receipt.ReceiptSignature.Should().NotBeNullOrEmpty();
        receipt.TransactionHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateReceiptAsync_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Receipt test");

        // Act - Third user tries to get receipt
        var receipt = await _transferService.GenerateReceiptAsync(
            transfer.TransferId,
            _thirdUser.Id);

        // Assert
        receipt.Should().BeNull("unauthorized users should not generate receipts");
    }

    [Fact]
    public async Task VerifyReceiptAsync_ValidSignature_ReturnsValid()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Verify test");

        var receipt = await _transferService.GenerateReceiptAsync(
            transfer.TransferId,
            _senderUser.Id);

        // Act
        var verification = await _transferService.VerifyReceiptAsync(
            transfer.TransferId,
            receipt!.ReceiptSignature);

        // Assert
        verification.Should().NotBeNull();
        verification.IsValid.Should().BeTrue();
        verification.Transfer.Should().NotBeNull();
        verification.Transfer!.Id.Should().Be(transfer.TransferId);
    }

    [Fact]
    public async Task VerifyReceiptAsync_InvalidSignature_ReturnsInvalid()
    {
        // Arrange
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Verify test");

        // Act
        var verification = await _transferService.VerifyReceiptAsync(
            transfer.TransferId,
            "InvalidSignature123");

        // Assert
        verification.IsValid.Should().BeFalse();
        verification.Transfer.Should().BeNull();
    }

    [Fact]
    public async Task VerifyReceiptAsync_NonExistentTransfer_ReturnsInvalid()
    {
        // Act
        var verification = await _transferService.VerifyReceiptAsync(
            Guid.NewGuid(),
            "AnySignature");

        // Assert
        verification.IsValid.Should().BeFalse();
        verification.Message.Should().Contain("not found");
    }

    #endregion

    #region Fraud Detection Tests

    [Fact]
    public async Task AnalyzeTransferRiskAsync_LowAmountNormalVelocity_ReturnsLowRisk()
    {
        // Arrange - Single small transfer
        await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            10,
            "Normal transfer");

        // Act
        var riskAssessment = await _transferService.AnalyzeTransferRiskAsync(
            _senderUser.Id,
            10,
            "192.168.1.1");

        // Assert
        riskAssessment.Should().NotBeNull();
        riskAssessment.RiskLevel.Should().Be(RiskLevel.Low);
        riskAssessment.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeTransferRiskAsync_HighAmount_IncreasesRiskScore()
    {
        // Act - High amount transfer (>= 500)
        var riskAssessment = await _transferService.AnalyzeTransferRiskAsync(
            _senderUser.Id,
            600,
            "192.168.1.1");

        // Assert
        riskAssessment.RiskScore.Should().BeGreaterOrEqualTo(30);
        riskAssessment.RiskFactors.Should().Contain("High transfer amount");
    }

    [Fact]
    public async Task AnalyzeTransferRiskAsync_HighVelocity_IncreasesRiskScore()
    {
        // Arrange - Create multiple transfers within the hour
        for (int i = 0; i < 5; i++)
        {
            await _transferService.TransferCreditsAsync(
                _senderUser.Id,
                _recipientUser.Id,
                10,
                $"Rapid transfer {i}");
        }

        // Act
        var riskAssessment = await _transferService.AnalyzeTransferRiskAsync(
            _senderUser.Id,
            10,
            "192.168.1.1");

        // Assert
        riskAssessment.RiskScore.Should().BeGreaterOrEqualTo(40);
        riskAssessment.RiskFactors.Should().Contain("High transfer frequency");
    }

    [Fact]
    public async Task GetTransferStatisticsAsync_ValidUser_ReturnsStatistics()
    {
        // Arrange - Create some transfers
        await _transferService.TransferCreditsAsync(_senderUser.Id, _recipientUser.Id, 50, "Test 1");
        await _transferService.TransferCreditsAsync(_senderUser.Id, _recipientUser.Id, 30, "Test 2");

        // Act
        var stats = await _transferService.GetTransferStatisticsAsync(
            _senderUser.Id,
            TimeSpan.FromDays(1));

        // Assert
        stats.Should().NotBeNull();
        stats.TransfersSent.Should().BeGreaterOrEqualTo(2);
        stats.TotalAmountSent.Should().BeGreaterOrEqualTo(80);
        stats.TimePeriod.Should().Be(TimeSpan.FromDays(1));
    }

    #endregion

    #region System Operations Tests

    [Fact]
    public async Task GetSystemTransferStatisticsAsync_WithTransfers_ReturnsAccurateStats()
    {
        // Arrange
        await _transferService.TransferCreditsAsync(_senderUser.Id, _recipientUser.Id, 100, "Stat test 1");
        await _transferService.TransferCreditsAsync(_senderUser.Id, _thirdUser.Id, 50, "Stat test 2");

        var startDate = DateTime.UtcNow.AddHours(-1);
        var endDate = DateTime.UtcNow.AddHours(1);

        // Act
        var stats = await _transferService.GetSystemTransferStatisticsAsync(startDate, endDate);

        // Assert
        stats.Should().NotBeNull();
        stats.TotalTransfers.Should().BeGreaterOrEqualTo(2);
        stats.TotalVolume.Should().BeGreaterOrEqualTo(150);
        stats.SuccessfulTransfers.Should().BeGreaterOrEqualTo(2);
        stats.ActiveTransferUsers.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task CancelTransferAsync_PendingTransfer_DoesNotExist()
    {
        // Arrange - Transfers are completed immediately in normal flow
        // A pending transfer would exist only briefly during processing
        var nonExistentId = Guid.NewGuid();

        // Act
        var cancelled = await _transferService.CancelTransferAsync(
            nonExistentId,
            _senderUser.Id,
            "Test cancellation");

        // Assert
        cancelled.Should().BeFalse("non-existent transfers cannot be cancelled");
    }

    [Fact]
    public async Task CancelTransferAsync_CompletedTransfer_ReturnsFalse()
    {
        // Arrange - Create and complete a transfer
        var transfer = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            50,
            "Test transfer");

        // Act - Try to cancel completed transfer
        var cancelled = await _transferService.CancelTransferAsync(
            transfer.TransferId,
            _senderUser.Id,
            "Attempted cancellation");

        // Assert
        cancelled.Should().BeFalse("completed transfers cannot be cancelled, only reversed");
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task TransferCreditsAsync_ConcurrentTransfers_OnlyAllowsWithinBalance()
    {
        // Arrange - Set up sender with known balance
        var initialBalance = await _walletService.GetBalanceAsync(_senderUser.Id);

        // Act - Attempt concurrent transfers that would exceed balance
        var tasks = new List<Task<TransferCreditsResponseDto>>();
        var transferAmount = (int)(initialBalance!.Value * 0.6); // 60% of balance each

        // Two concurrent transfers of 60% each = 120% of balance (should fail one)
        for (int i = 0; i < 2; i++)
        {
            var task = Task.Run(() => _transferService.TransferCreditsAsync(
                _senderUser.Id,
                _recipientUser.Id,
                transferAmount,
                $"Concurrent transfer {i}"));
            tasks.Add(task);
        }

        // Wait for all to complete (some may throw)
        var results = await Task.WhenAll(
            tasks.Select(t => t.ContinueWith(r =>
            {
                if (r.IsFaulted)
                    return null;
                return r.Result;
            })));

        // Assert - At least one should succeed, at least one may fail
        var successfulTransfers = results.Count(r => r != null);
        successfulTransfers.Should().BeGreaterOrEqualTo(1, "at least one concurrent transfer should succeed");

        // Verify final balance is non-negative
        var finalBalance = await _walletService.GetBalanceAsync(_senderUser.Id);
        finalBalance.Should().BeGreaterOrEqualTo(0, "balance should never go negative");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task TransferCreditsAsync_MaxAmount_Succeeds()
    {
        // Arrange - Transfer exactly the max single transfer amount
        var maxAmount = 1000;

        // Act
        var isValid = await _transferService.ValidateTransferAsync(
            _senderUser.Id,
            _recipientUser.Id,
            maxAmount);

        // Assert
        isValid.Should().BeTrue("max amount should be valid");
    }

    [Fact]
    public async Task TransferCreditsAsync_OneCredit_Succeeds()
    {
        // Arrange
        var minAmount = 1;

        // Act
        var result = await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            minAmount,
            "Minimum transfer");

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(1);
        result.Status.Should().Be(TransferStatus.Completed);
    }

    [Fact]
    public async Task GetTransferHistoryAsync_NoTransfers_ReturnsEmptyList()
    {
        // Arrange - Use third user who has no transfers
        var request = new TransferHistoryRequestDto
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _transferService.GetTransferHistoryAsync(
            _thirdUser.Id,
            request);

        // Assert
        result.Transfers.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetTransferHistoryAsync_FilterByDateRange_ReturnsMatchingTransfers()
    {
        // Arrange
        await _transferService.TransferCreditsAsync(
            _senderUser.Id,
            _recipientUser.Id,
            25,
            "Date range test");

        var request = new TransferHistoryRequestDto
        {
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddHours(1),
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _transferService.GetTransferHistoryAsync(
            _senderUser.Id,
            request);

        // Assert
        result.Transfers.Should().NotBeEmpty();
        result.Transfers.Should().AllSatisfy(t =>
        {
            t.CreatedAt.Should().BeAfter(request.StartDate!.Value);
            t.CreatedAt.Should().BeBefore(request.EndDate!.Value);
        });
    }

    #endregion
}
