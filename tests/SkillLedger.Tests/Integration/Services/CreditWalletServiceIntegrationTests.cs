using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for CreditWalletService - Financial Core Service.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real internal services (audit log writes to DB)
/// - Mocks only EXTERNAL services (encryption)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (IEncryptionService)
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
[Trait("Service", "CreditWalletService")]
public class CreditWalletServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly CreditWalletService _service;

    // REAL internal services
    private readonly MockAuditLogService _auditLogService;  // Writes to DB!

    // EXTERNAL services (OK to mock)
    private readonly MockEncryptionService _mockEncryptionService;

    // Test data
    private readonly User _testUser;
    private readonly User _testClient;
    private readonly User _testProvider;
    private readonly Project _testProject;

    public CreditWalletServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"CreditWalletTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal service
        _auditLogService = new MockAuditLogService(_context);

        // Setup EXTERNAL services
        _mockEncryptionService = new MockEncryptionService();

        var logger = new LoggerFactory().CreateLogger<CreditWalletService>();
        var encryptionConfig = Options.Create(new EncryptionConfiguration
        {
            KeyVaultEndpoint = new Uri("https://test-vault.vault.azure.net/"),
            MasterKeyName = "test-key"
        });

        _service = new CreditWalletService(
            _context,
            _mockEncryptionService,
            _auditLogService,
            logger,
            encryptionConfig);

        // Initialize test data
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            EmailConfirmed = true
        };

        _testClient = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@test.com",
            UserName = "testclient",
            FirstName = "Test",
            LastName = "Client",
            PasswordHash = "hash",
            EmailConfirmed = true
        };

        _testProvider = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            UserName = "testprovider",
            FirstName = "Test",
            LastName = "Provider",
            PasswordHash = "hash",
            EmailConfirmed = true
        };

        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Integration test project",
            ClientId = _testClient.Id,
            ProviderId = _testProvider.Id,
            Status = ProjectStatus.InProgress,
            CreditBudget = 500
        };

        _context.Users.AddRange(_testUser, _testClient, _testProvider);
        _context.Projects.Add(_testProject);
        _context.SaveChanges();
    }

    #region Wallet Management Tests

    [Fact]
    public async Task CreateWalletAsync_NewUser_ShouldCreateWalletWithStartingCredits()
    {
        // Arrange
        var userId = _testUser.Id;

        // Act
        var wallet = await _service.CreateWalletAsync(userId);

        // Assert - Verify REAL database state
        wallet.Should().NotBeNull();
        wallet.UserId.Should().Be(userId);
        wallet.Balance.Should().Be(100); // STARTING_CREDITS
        wallet.PendingBalance.Should().Be(0);
        wallet.TotalEarned.Should().Be(100);
        wallet.TotalSpent.Should().Be(0);

        // Verify wallet persisted to database
        var dbWallet = await _context.CreditWallets
            .FirstOrDefaultAsync(w => w.UserId == userId);
        dbWallet.Should().NotBeNull();
        dbWallet!.EncryptedBalance.Should().NotBeNullOrEmpty();

        // Verify starting credit transaction created
        var startingCreditTx = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.ToUserId == userId &&
                                     t.Type == CreditTransactionType.StartingCredit);
        startingCreditTx.Should().NotBeNull();
        startingCreditTx!.Amount.Should().Be(100);
        startingCreditTx.Status.Should().Be(TransactionStatus.Completed);

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "WalletCreated" && a.UserId == userId);
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateWalletAsync_ExistingWallet_ShouldReturnExistingWallet()
    {
        // Arrange
        var userId = _testUser.Id;
        var firstWallet = await _service.CreateWalletAsync(userId);

        // Act
        var secondWallet = await _service.CreateWalletAsync(userId);

        // Assert
        secondWallet.Should().NotBeNull();
        secondWallet.Id.Should().Be(firstWallet.Id);

        // Verify only one wallet exists
        var walletCount = await _context.CreditWallets
            .CountAsync(w => w.UserId == userId);
        walletCount.Should().Be(1);
    }

    [Fact]
    public async Task GetWalletAsync_ExistingWallet_ShouldReturnDecryptedWallet()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var wallet = await _service.GetWalletAsync(_testUser.Id);

        // Assert
        wallet.Should().NotBeNull();
        wallet!.UserId.Should().Be(_testUser.Id);
        wallet.Balance.Should().Be(100);
        wallet.PendingBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletAsync_NonExistingWallet_ShouldReturnNull()
    {
        // Arrange
        var nonExistingUserId = Guid.NewGuid();

        // Act
        var wallet = await _service.GetWalletAsync(nonExistingUserId);

        // Assert
        wallet.Should().BeNull();
    }

    [Fact]
    public async Task GetBalanceAsync_ExistingWallet_ShouldReturnBalance()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var balance = await _service.GetBalanceAsync(_testUser.Id);

        // Assert
        balance.Should().Be(100);
    }

    [Fact]
    public async Task GetAvailableBalanceAsync_WithPendingBalance_ShouldReturnCorrectAmount()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 30);

        // Act
        var availableBalance = await _service.GetAvailableBalanceAsync(_testClient.Id);

        // Assert - Available = Balance - Pending = 100 - 30 = 70
        availableBalance.Should().Be(70);
    }

    #endregion

    #region Transaction Operations Tests

    [Fact]
    public async Task TransferCreditsAsync_ValidTransfer_ShouldUpdateBothWallets()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);

        // Act
        var transaction = await _service.TransferCreditsAsync(
            fromUserId: _testClient.Id,
            toUserId: _testProvider.Id,
            amount: 50,
            description: "Payment for services",
            transactionType: CreditTransactionType.DirectPayment,
            initiatedFromIP: "127.0.0.1",
            userAgent: "test");

        // Assert - Verify transaction
        transaction.Should().NotBeNull();
        transaction.FromUserId.Should().Be(_testClient.Id);
        transaction.ToUserId.Should().Be(_testProvider.Id);
        transaction.Amount.Should().Be(50);
        transaction.Status.Should().Be(TransactionStatus.Completed);

        // Verify sender wallet in database
        var clientBalance = await _service.GetBalanceAsync(_testClient.Id);
        clientBalance.Should().Be(50); // 100 - 50

        // Verify recipient wallet in database
        var providerBalance = await _service.GetBalanceAsync(_testProvider.Id);
        providerBalance.Should().Be(150); // 100 + 50

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CreditTransfer" && a.UserId == _testClient.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TransferCreditsAsync_InsufficientCredits_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);

        // Act
        var act = async () => await _service.TransferCreditsAsync(
            fromUserId: _testClient.Id,
            toUserId: _testProvider.Id,
            amount: 200, // More than available
            description: "Payment for services",
            transactionType: CreditTransactionType.DirectPayment);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient credits for transfer");

        // Verify balances unchanged
        var clientBalance = await _service.GetBalanceAsync(_testClient.Id);
        clientBalance.Should().Be(100);
    }

    [Fact]
    public async Task TransferCreditsAsync_ToSameUser_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var act = async () => await _service.TransferCreditsAsync(
            fromUserId: _testUser.Id,
            toUserId: _testUser.Id,
            amount: 10,
            description: "Self transfer",
            transactionType: CreditTransactionType.DirectPayment);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Cannot transfer credits to the same user");
    }

    [Fact]
    public async Task TransferCreditsAsync_BlockedWallet_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.BlockWalletAsync(_testClient.Id, "Fraud detected");

        // Act
        var act = async () => await _service.TransferCreditsAsync(
            fromUserId: _testClient.Id,
            toUserId: _testProvider.Id,
            amount: 10,
            description: "Payment",
            transactionType: CreditTransactionType.DirectPayment);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*blocked*");
    }

    [Fact]
    public async Task TransferCreditsAsync_HighVelocity_ShouldDetectFraud()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);

        // Create 11 rapid small transactions to trigger fraud detection
        // - Velocity threshold (>5 in last hour): +50 points
        // - Small transactions (>10 with amount <= 5): +25 points
        // - Total: 75 points (>= 70 for RequiresAdditionalVerification)
        for (int i = 0; i < 11; i++)
        {
            await _service.TransferCreditsAsync(
                fromUserId: _testClient.Id,
                toUserId: _testProvider.Id,
                amount: 1,
                description: $"Rapid transaction {i}",
                transactionType: CreditTransactionType.DirectPayment);
        }

        // Act - 12th transaction should be blocked by fraud detection (riskScore >= 70)
        var act = async () => await _service.TransferCreditsAsync(
            fromUserId: _testClient.Id,
            toUserId: _testProvider.Id,
            amount: 1,
            description: "Blocked transaction",
            transactionType: CreditTransactionType.DirectPayment);

        // Assert - Should throw due to fraud detection (IsHighRisk && RequiresAdditionalVerification)
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*fraud detection*");
    }

    [Fact]
    public async Task AddCreditsAsync_ValidAmount_ShouldIncreaseBalance()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var transaction = await _service.AddCreditsAsync(
            userId: _testUser.Id,
            amount: 50,
            description: "Bonus credits",
            transactionType: CreditTransactionType.Adjustment);

        // Assert
        transaction.Should().NotBeNull();
        transaction.ToUserId.Should().Be(_testUser.Id);
        transaction.Amount.Should().Be(50);
        transaction.Status.Should().Be(TransactionStatus.Completed);

        // Verify balance in database
        var balance = await _service.GetBalanceAsync(_testUser.Id);
        balance.Should().Be(150); // 100 + 50
    }

    [Fact]
    public async Task AddCreditsAsync_NoWallet_ShouldAutoCreateWallet()
    {
        // Arrange
        var newUserId = Guid.NewGuid();
        var newUser = new User
        {
            Id = newUserId,
            Email = "newuser@test.com",
            UserName = "newuser",
            FirstName = "New",
            LastName = "User",
            PasswordHash = "hash",
            EmailConfirmed = true
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Act
        var transaction = await _service.AddCreditsAsync(
            userId: newUserId,
            amount: 50,
            description: "First credits",
            transactionType: CreditTransactionType.Adjustment);

        // Assert
        transaction.Should().NotBeNull();

        // Verify wallet was auto-created
        var wallet = await _service.GetWalletAsync(newUserId);
        wallet.Should().NotBeNull();
        wallet!.Balance.Should().Be(50); // Auto-created with 0, then added 50
    }

    [Fact]
    public async Task DeductCreditsAsync_ValidAmount_ShouldDecreaseBalance()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var transaction = await _service.DeductCreditsAsync(
            userId: _testUser.Id,
            amount: 30,
            description: "Platform fee",
            transactionType: CreditTransactionType.PlatformFee);

        // Assert
        transaction.Should().NotBeNull();
        transaction.FromUserId.Should().Be(_testUser.Id);
        transaction.Amount.Should().Be(30);
        transaction.Status.Should().Be(TransactionStatus.Completed);

        // Verify balance in database
        var balance = await _service.GetBalanceAsync(_testUser.Id);
        balance.Should().Be(70); // 100 - 30
    }

    [Fact]
    public async Task DeductCreditsAsync_InsufficientCredits_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var act = async () => await _service.DeductCreditsAsync(
            userId: _testUser.Id,
            amount: 200,
            description: "Large deduction",
            transactionType: CreditTransactionType.PlatformFee);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient credits for deduction");
    }

    #endregion

    #region Escrow Operations Tests

    [Fact]
    public async Task CreateEscrowAsync_ValidAmount_ShouldIncreasePendingBalance()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);

        // Act
        var transaction = await _service.CreateEscrowAsync(
            clientUserId: _testClient.Id,
            projectId: _testProject.Id,
            amount: 50);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Type.Should().Be(CreditTransactionType.EscrowDeposit);
        transaction.Amount.Should().Be(50);
        transaction.ProjectId.Should().Be(_testProject.Id);

        // Verify pending balance increased
        var wallet = await _service.GetWalletAsync(_testClient.Id);
        wallet!.PendingBalance.Should().Be(50);
        wallet.Balance.Should().Be(100); // Balance remains same
        wallet.AvailableBalance.Should().Be(50); // 100 - 50 = 50 available
    }

    [Fact]
    public async Task CreateEscrowAsync_InsufficientCredits_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);

        // Act
        var act = async () => await _service.CreateEscrowAsync(
            clientUserId: _testClient.Id,
            projectId: _testProject.Id,
            amount: 200);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient credits for escrow");
    }

    [Fact]
    public async Task ReleaseMilestoneFromEscrowAsync_ValidAmount_ShouldTransferCredits()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 50);

        // Act
        var transaction = await _service.ReleaseMilestoneFromEscrowAsync(
            clientUserId: _testClient.Id,
            providerUserId: _testProvider.Id,
            projectId: _testProject.Id,
            amount: 20);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Type.Should().Be(CreditTransactionType.EscrowRelease);
        transaction.Amount.Should().Be(20);

        // Verify client wallet - balance and pending reduced
        var clientWallet = await _service.GetWalletAsync(_testClient.Id);
        clientWallet!.Balance.Should().Be(80); // 100 - 20
        clientWallet.PendingBalance.Should().Be(30); // 50 - 20
        clientWallet.TotalSpent.Should().Be(20);

        // Verify provider wallet - balance and earned increased
        var providerWallet = await _service.GetWalletAsync(_testProvider.Id);
        providerWallet!.Balance.Should().Be(120); // 100 + 20
        providerWallet.TotalEarned.Should().Be(120); // 100 + 20
    }

    [Fact]
    public async Task ReleaseMilestoneFromEscrowAsync_InsufficientPendingBalance_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 30);

        // Act - Try to release more than pending
        var act = async () => await _service.ReleaseMilestoneFromEscrowAsync(
            clientUserId: _testClient.Id,
            providerUserId: _testProvider.Id,
            projectId: _testProject.Id,
            amount: 50);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient pending balance*");
    }

    [Fact]
    public async Task ReleaseEscrowAsync_FullEscrow_ShouldTransferAllCredits()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        var escrowTx = await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 50);

        // Act
        var releaseTx = await _service.ReleaseEscrowAsync(_testProject.Id, _testProvider.Id);

        // Assert
        releaseTx.Should().NotBeNull();
        releaseTx.Type.Should().Be(CreditTransactionType.EscrowRelease);
        releaseTx.Amount.Should().Be(50);

        // Verify client pending balance cleared
        var clientWallet = await _service.GetWalletAsync(_testClient.Id);
        clientWallet!.PendingBalance.Should().Be(0);

        // Verify provider received credits
        var providerWallet = await _service.GetWalletAsync(_testProvider.Id);
        providerWallet!.Balance.Should().Be(150); // 100 + 50
    }

    [Fact]
    public async Task ReleaseEscrowAsync_AlreadyReleased_ShouldThrowException()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 50);
        await _service.ReleaseEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act - Try to release again
        var act = async () => await _service.ReleaseEscrowAsync(_testProject.Id, _testProvider.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been released*");
    }

    [Fact]
    public async Task RefundEscrowAsync_RemainingAmount_ShouldReducePendingBalance()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 50);

        // Release partial milestone
        await _service.ReleaseMilestoneFromEscrowAsync(_testClient.Id, _testProvider.Id, _testProject.Id, 20);

        // Act - Refund remaining 30
        var refundTx = await _service.RefundEscrowAsync(_testProject.Id, remainingAmount: 30);

        // Assert
        refundTx.Should().NotBeNull();
        refundTx.Type.Should().Be(CreditTransactionType.EscrowRefund);
        refundTx.Amount.Should().Be(30);

        // Verify client pending balance cleared
        var clientWallet = await _service.GetWalletAsync(_testClient.Id);
        clientWallet!.PendingBalance.Should().Be(0); // Was 30 (50 - 20), now 0
        clientWallet.Balance.Should().Be(80); // 100 - 20 (milestone released)
    }

    [Fact]
    public async Task RefundEscrowAsync_ZeroAmount_ShouldReturnPlaceholderTransaction()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 50);

        // Act - Refund zero (all escrow released)
        var refundTx = await _service.RefundEscrowAsync(_testProject.Id, remainingAmount: 0);

        // Assert
        refundTx.Should().NotBeNull();
        refundTx.Amount.Should().Be(0);
        refundTx.Type.Should().Be(CreditTransactionType.EscrowRefund);
    }

    #endregion

    #region Transaction History & Validation Tests

    [Fact]
    public async Task GetTransactionHistoryAsync_MultipleTransactions_ShouldReturnOrderedHistory()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);

        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 10, "Payment 1", CreditTransactionType.DirectPayment);
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 20, "Payment 2", CreditTransactionType.DirectPayment);
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 15, "Payment 3", CreditTransactionType.DirectPayment);

        // Act
        var history = await _service.GetTransactionHistoryAsync(_testClient.Id, limit: 10);

        // Assert
        history.Should().NotBeNull();
        history.Count.Should().Be(3); // Excludes StartingCredit
        history.Should().BeInDescendingOrder(t => t.CreatedAt);

        // Most recent transaction should be first
        history.First().Amount.Should().Be(15);
        history.First().Description.Should().Be("Payment 3");
    }

    [Fact]
    public async Task GetProjectTransactionsAsync_ProjectEscrow_ShouldReturnAllProjectTransactions()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);

        await _service.CreateEscrowAsync(_testClient.Id, _testProject.Id, 50);
        await _service.ReleaseMilestoneFromEscrowAsync(_testClient.Id, _testProvider.Id, _testProject.Id, 20);
        await _service.RefundEscrowAsync(_testProject.Id, remainingAmount: 30);

        // Act
        var projectTransactions = await _service.GetProjectTransactionsAsync(_testProject.Id);

        // Assert
        projectTransactions.Should().NotBeNull();
        projectTransactions.Count.Should().Be(3);
        projectTransactions.Should().Contain(t => t.Type == CreditTransactionType.EscrowDeposit);
        projectTransactions.Should().Contain(t => t.Type == CreditTransactionType.EscrowRelease);
        projectTransactions.Should().Contain(t => t.Type == CreditTransactionType.EscrowRefund);
    }

    [Fact]
    public async Task ValidateTransactionIntegrity_ValidTransaction_ShouldReturnTrue()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        var transaction = await _service.TransferCreditsAsync(
            _testClient.Id, _testProvider.Id, 10, "Test", CreditTransactionType.DirectPayment);

        // Act
        var isValid = await _service.ValidateTransactionIntegrity(transaction.Id);

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Fraud Detection & Security Tests

    [Fact]
    public async Task AnalyzeFraudPatterns_HighVelocity_ShouldDetectHighRisk()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);

        // Create 6 rapid transactions (FRAUD_VELOCITY_THRESHOLD = 5)
        for (int i = 0; i < 6; i++)
        {
            await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 1, $"Transaction {i}", CreditTransactionType.DirectPayment);
        }

        // Act
        var fraudReport = await _service.AnalyzeFraudPatterns(_testClient.Id);

        // Assert
        fraudReport.Should().NotBeNull();
        fraudReport.IsHighRisk.Should().BeTrue();
        fraudReport.RiskScore.Should().BeGreaterThanOrEqualTo(50);
        fraudReport.RiskFactors.Should().Contain("High transaction velocity");
        fraudReport.TransactionsAnalyzed.Should().Be(6);
    }

    [Fact]
    public async Task AnalyzeFraudPatterns_LargeTransactions_ShouldDetectRisk()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.AddCreditsAsync(_testClient.Id, 2000, "Test credits", CreditTransactionType.Adjustment);
        await _service.CreateWalletAsync(_testProvider.Id);

        // Act - Create large transaction (FRAUD_AMOUNT_THRESHOLD = 1000)
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 1500, "Large payment", CreditTransactionType.DirectPayment);
        var fraudReport = await _service.AnalyzeFraudPatterns(_testClient.Id);

        // Assert
        fraudReport.Should().NotBeNull();
        fraudReport.RiskFactors.Should().Contain("Large transaction amounts");
    }

    [Fact]
    public async Task BlockWalletAsync_ValidReason_ShouldBlockWallet()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var result = await _service.BlockWalletAsync(_testUser.Id, "Fraud detected");

        // Assert
        result.Should().BeTrue();

        // Verify wallet is blocked in database
        var wallet = await _context.CreditWallets
            .FirstOrDefaultAsync(w => w.UserId == _testUser.Id);
        wallet.Should().NotBeNull();
        wallet!.IsBlocked.Should().BeTrue();
        wallet.BlockedReason.Should().Be("Fraud detected");

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "WalletBlocked" && a.UserId == _testUser.Id);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task UnblockWalletAsync_BlockedWallet_ShouldUnblockWallet()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);
        await _service.BlockWalletAsync(_testUser.Id, "Test block");

        // Act
        var result = await _service.UnblockWalletAsync(_testUser.Id);

        // Assert
        result.Should().BeTrue();

        // Verify wallet is unblocked in database
        var wallet = await _context.CreditWallets
            .FirstOrDefaultAsync(w => w.UserId == _testUser.Id);
        wallet.Should().NotBeNull();
        wallet!.IsBlocked.Should().BeFalse();
        wallet.BlockedReason.Should().BeNull();
    }

    #endregion

    #region Balance Reconciliation Tests

    [Fact]
    public async Task ReconcileWalletBalance_BalancedWallet_ShouldReportBalanced()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 30, "Payment", CreditTransactionType.DirectPayment);

        // Act
        var report = await _service.ReconcileWalletBalance(_testClient.Id);

        // Assert
        report.Should().NotBeNull();
        report.UserId.Should().Be(_testClient.Id);
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(70); // 100 - 30
        report.CalculatedBalance.Should().Be(70); // 100 starting - 30 outgoing
        report.Discrepancy.Should().Be(0);
    }

    [Fact]
    public async Task ReconcileAllWallets_MultipleWallets_ShouldReconcileAll()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var systemReport = await _service.ReconcileAllWallets();

        // Assert
        systemReport.Should().NotBeNull();
        systemReport.WalletsReconciled.Should().Be(3);
        systemReport.WalletsWithDiscrepancies.Should().Be(0);
    }

    #endregion

    #region Encryption & Export Tests

    [Fact]
    public async Task VerifyEncryptionIntegrityAsync_ValidWallet_ShouldReturnTrue()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var isValid = await _service.VerifyEncryptionIntegrityAsync(_testUser.Id);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task ExportWalletDataAsync_WithTransactions_ShouldReturnCompleteExport()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);
        await _service.CreateWalletAsync(_testProvider.Id);
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 20, "Payment", CreditTransactionType.DirectPayment);

        // Act
        var exportData = await _service.ExportWalletDataAsync(_testClient.Id);

        // Assert
        exportData.Should().NotBeNull();
        exportData.UserId.Should().Be(_testClient.Id);
        exportData.WalletSummary.Should().NotBeNull();
        exportData.WalletSummary.CurrentBalance.Should().Be(80);
        exportData.TransactionHistory.Should().NotBeNull();
        exportData.TransactionHistory.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GenerateFinancialReportAsync_DateRange_ShouldReturnAccurateSummary()
    {
        // Arrange
        await _service.CreateWalletAsync(_testClient.Id);  // Gives 100 starting credits
        await _service.CreateWalletAsync(_testProvider.Id);

        // Small delay to ensure wallet creation transactions are in the past
        await Task.Delay(100);

        // Set startDate AFTER wallet creation to exclude the initial 100 credits
        var startDate = DateTime.UtcNow;
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 30, "Payment 1", CreditTransactionType.DirectPayment);
        await _service.TransferCreditsAsync(_testClient.Id, _testProvider.Id, 20, "Payment 2", CreditTransactionType.DirectPayment);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var report = await _service.GenerateFinancialReportAsync(_testClient.Id, startDate, endDate);

        // Assert
        report.Should().NotBeNull();
        report.UserId.Should().Be(_testClient.Id);
        report.TotalCreditsSpent.Should().Be(50); // 30 + 20 (within date range)
        report.TotalCreditsReceived.Should().Be(0); // Initial 100 credits excluded (before startDate)
        report.TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetWalletUpdateNotificationAsync_ExistingWallet_ShouldReturnNotification()
    {
        // Arrange
        await _service.CreateWalletAsync(_testUser.Id);

        // Act
        var notification = await _service.GetWalletUpdateNotificationAsync(_testUser.Id);

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be(_testUser.Id);
        notification.NewBalance.Should().Be(100);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
