using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using System.Security.Cryptography;
using FluentAssertions;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// TDD tests for CreditWallet service with AES-256 encryption and financial operations
/// Following Red-Green-Refactor methodology
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Financial")]
public class CreditWalletServiceTests : IntegrationTestBase
{
    private readonly ICreditWalletService _service;
    private User _testUser = null!;
    private User _testUser2 = null!;

    public CreditWalletServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _service = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "wallet-user@example.com",
            UserName = "wallet-user@example.com",
            NormalizedEmail = "WALLET-USER@EXAMPLE.COM",
            NormalizedUserName = "WALLET-USER@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_testUser);

        _testUser2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "wallet-user2@example.com",
            UserName = "wallet-user2@example.com",
            NormalizedEmail = "WALLET-USER2@EXAMPLE.COM",
            NormalizedUserName = "WALLET-USER2@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_testUser2);

        await Context.SaveChangesAsync();
    }

    #region Wallet Creation Tests (TDD Red Phase)

    [Fact]
    public async Task CreateWalletAsync_ForNewUser_ShouldCreateWalletWithStartingCredits()
    {
        // Arrange - this will fail initially (Red phase)
        var userId = _testUser.Id;

        // Act
        var result = await _service.CreateWalletAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Balance.Should().Be(100); // Starting credits as per user story
        result.PendingBalance.Should().Be(0);
        result.TotalEarned.Should().Be(100);
        result.TotalSpent.Should().Be(0);
    }

    [Fact]
    public async Task CreateWalletAsync_ForExistingUser_ShouldReturnExistingWallet()
    {
        // Arrange - this will fail initially (Red phase)
        var userId = _testUser.Id;
        var existingWallet = await _service.CreateWalletAsync(userId);

        // Act
        var result = await _service.CreateWalletAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingWallet.Id);
        result.Balance.Should().Be(100); // Should not duplicate starting credits
    }

    #endregion

    #region Encrypted Storage Tests (TDD Red Phase)

    [Fact]
    public async Task CreateWalletAsync_ShouldEncryptSensitiveData()
    {
        // Arrange - this will fail initially (Red phase)
        var userId = _testUser.Id;

        // Act
        var wallet = await _service.CreateWalletAsync(userId);

        // Assert - Verify encryption is applied
        var storedWallet = await Context.CreditWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        storedWallet.Should().NotBeNull();

        // Balance should be encrypted in storage
        storedWallet!.EncryptedBalance.Should().NotBeNullOrEmpty();
        storedWallet.EncryptedBalance.Should().NotBe(wallet.Balance.ToString());
    }

    [Fact]
    public async Task GetWalletAsync_ShouldDecryptStoredData()
    {
        // Arrange - this will fail initially (Red phase)
        var userId = _testUser.Id;
        var originalWallet = await _service.CreateWalletAsync(userId);

        // Act
        var retrievedWallet = await _service.GetWalletAsync(userId);

        // Assert - Verify decryption works correctly
        retrievedWallet.Should().NotBeNull();
        retrievedWallet!.Balance.Should().Be(originalWallet.Balance);
        retrievedWallet.TotalEarned.Should().Be(originalWallet.TotalEarned);
        retrievedWallet.TotalSpent.Should().Be(originalWallet.TotalSpent);
    }

    #endregion

    #region Transaction Tests (TDD Red Phase)

    [Fact]
    public async Task TransferCreditsAsync_ValidTransfer_ShouldUpdateBalancesAndCreateTransaction()
    {
        // Arrange - this will fail initially (Red phase)
        var fromWallet = await _service.CreateWalletAsync(_testUser.Id);
        var toWallet = await _service.CreateWalletAsync(_testUser2.Id);
        var transferAmount = 50;

        // Act
        var transaction = await _service.TransferCreditsAsync(
            fromUserId: _testUser.Id,
            toUserId: _testUser2.Id,
            amount: transferAmount,
            description: "Test credit transfer",
            transactionType: CreditTransactionType.ProjectPayment
        );

        // Assert
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(transferAmount);
        transaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.TransactionHash.Should().NotBeNullOrEmpty();

        // Verify balances updated
        var updatedFromWallet = await _service.GetWalletAsync(_testUser.Id);
        var updatedToWallet = await _service.GetWalletAsync(_testUser2.Id);

        updatedFromWallet!.Balance.Should().Be(50); // 100 - 50
        updatedFromWallet.TotalSpent.Should().Be(50);

        updatedToWallet!.Balance.Should().Be(150); // 100 + 50
        updatedToWallet.TotalEarned.Should().Be(150); // 100 starting + 50 transfer
    }

    [Fact]
    public async Task TransferCreditsAsync_InsufficientFunds_ShouldThrowException()
    {
        // Arrange - this will fail initially (Red phase)
        var fromWallet = await _service.CreateWalletAsync(_testUser.Id);
        var toWallet = await _service.CreateWalletAsync(_testUser2.Id);
        var transferAmount = 150; // More than available balance

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _service.TransferCreditsAsync(
                fromUserId: _testUser.Id,
                toUserId: _testUser2.Id,
                amount: transferAmount,
                description: "Invalid transfer - insufficient funds",
                transactionType: CreditTransactionType.ProjectPayment
            );
        });
    }

    #endregion

    #region Fraud Detection Tests (TDD Red Phase)

    [Fact]
    public async Task DetectFraudulentActivity_HighVelocityTransactions_ShouldFlagAsSpicious()
    {
        // Arrange - this will fail initially (Red phase)
        var fromWallet = await _service.CreateWalletAsync(_testUser.Id);
        var toWallet = await _service.CreateWalletAsync(_testUser2.Id);

        // Simulate multiple rapid transactions
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.TransferCreditsAsync(
                fromUserId: _testUser.Id,
                toUserId: _testUser2.Id,
                amount: 1,
                description: $"Rapid transaction {i}",
                transactionType: CreditTransactionType.ProjectPayment
            ));
        }

        // Wait for all transactions to complete
        await Task.WhenAll(tasks);

        // Act
        var fraudReport = await _service.AnalyzeFraudPatterns(_testUser.Id);

        // Assert
        fraudReport.Should().NotBeNull();
        fraudReport.IsHighRisk.Should().BeTrue();
        fraudReport.RiskFactors.Should().Contain("High transaction velocity");
    }

    #endregion

    #region Audit Trail Tests (TDD Red Phase)

    [Fact]
    public async Task GetTransactionHistory_ShouldReturnImmutableAuditTrail()
    {
        // Arrange - this will fail initially (Red phase)
        var fromWallet = await _service.CreateWalletAsync(_testUser.Id);
        var toWallet = await _service.CreateWalletAsync(_testUser2.Id);

        // Create some transactions
        await _service.TransferCreditsAsync(_testUser.Id, _testUser2.Id, 25, "Payment 1", CreditTransactionType.ProjectPayment);
        await _service.TransferCreditsAsync(_testUser.Id, _testUser2.Id, 25, "Payment 2", CreditTransactionType.ProjectPayment);

        // Act
        var history = await _service.GetTransactionHistoryAsync(_testUser.Id);

        // Assert
        history.Should().NotBeNull();
        history.Count.Should().Be(2);
        history.Should().BeInDescendingOrder(t => t.CreatedAt);

        // Verify transaction hashes for immutability
        foreach (var transaction in history)
        {
            transaction.TransactionHash.Should().NotBeNullOrEmpty();
            (await _service.ValidateTransactionIntegrity(transaction.Id)).Should().BeTrue();
        }
    }

    #endregion

    #region Balance Validation Tests (TDD Red Phase)

    [Fact]
    public async Task ReconcileBalance_ShouldMatchTransactionHistory()
    {
        // Arrange - this will fail initially (Red phase)
        var wallet = await _service.CreateWalletAsync(_testUser.Id);
        var toWallet = await _service.CreateWalletAsync(_testUser2.Id);

        // Perform transactions
        await _service.TransferCreditsAsync(_testUser.Id, _testUser2.Id, 30, "Transaction 1", CreditTransactionType.ProjectPayment);
        await _service.TransferCreditsAsync(_testUser.Id, _testUser2.Id, 20, "Transaction 2", CreditTransactionType.ProjectPayment);

        // Act
        var reconciliation = await _service.ReconcileWalletBalance(_testUser.Id);

        // Assert
        reconciliation.Should().NotBeNull();
        reconciliation.IsBalanced.Should().BeTrue();
        reconciliation.CalculatedBalance.Should().Be(50); // 100 - 30 - 20
        reconciliation.StoredBalance.Should().Be(50);
    }

    [Fact]
    public async Task ReconcileWalletBalance_WithEscrowTransactions_ShouldAccountForAllTypes()
    {
        // Arrange - Test CRIT-006 fix: EscrowDeposit and EscrowRefund must be included in reconciliation
        var clientWallet = await _service.CreateWalletAsync(_testUser.Id);
        var providerWallet = await _service.CreateWalletAsync(_testUser2.Id);

        // Add extra credits to client for escrow testing (starts with 100, need 200 for deposit)
        await _service.AddCreditsAsync(_testUser.Id, 200, "Test funding for escrow", CreditTransactionType.Purchase);

        var initialClientBalance = await _service.GetBalanceAsync(_testUser.Id);
        var initialProviderBalance = await _service.GetBalanceAsync(_testUser2.Id);

        // Create escrow deposit (client → provider: outgoing for client, incoming for provider)
        await _service.TransferCreditsAsync(
            _testUser.Id,
            _testUser2.Id,
            200,
            "Escrow deposit for project",
            CreditTransactionType.EscrowDeposit);

        // Simulate partial escrow refund (provider → client: outgoing for provider, incoming for client)
        await _service.TransferCreditsAsync(
            _testUser2.Id,
            _testUser.Id,
            50,
            "Partial escrow refund",
            CreditTransactionType.EscrowRefund);

        // Act - Reconcile both wallets
        var clientReconciliation = await _service.ReconcileWalletBalance(_testUser.Id);
        var providerReconciliation = await _service.ReconcileWalletBalance(_testUser2.Id);

        // Assert - Client wallet
        clientReconciliation.Should().NotBeNull();
        clientReconciliation.IsBalanced.Should().BeTrue(
            "client wallet should be balanced after escrow deposit and refund");

        // Client: Started with initialClientBalance (300), sent 200 (EscrowDeposit), received 50 (EscrowRefund) = 150
        var expectedClientBalance = initialClientBalance - 200 + 50;
        clientReconciliation.CalculatedBalance.Should().Be(expectedClientBalance,
            "reconciliation should include EscrowDeposit (outgoing) and EscrowRefund (incoming)");
        clientReconciliation.StoredBalance.Should().Be(expectedClientBalance);

        // Assert - Provider wallet
        providerReconciliation.Should().NotBeNull();
        providerReconciliation.IsBalanced.Should().BeTrue(
            "provider wallet should be balanced after receiving escrow and refunding");

        // Provider: Started with initialProviderBalance (100), received 200 (EscrowDeposit), sent 50 (EscrowRefund) = 250
        var expectedProviderBalance = initialProviderBalance + 200 - 50;
        providerReconciliation.CalculatedBalance.Should().Be(expectedProviderBalance,
            "reconciliation should include EscrowDeposit (incoming) and EscrowRefund (outgoing)");
        providerReconciliation.StoredBalance.Should().Be(expectedProviderBalance);

        // Verify transaction count includes all escrow types
        clientReconciliation.TransactionCount.Should().BeGreaterThan(0,
            "should have transactions including EscrowDeposit and EscrowRefund");
        providerReconciliation.TransactionCount.Should().BeGreaterThan(0,
            "should have transactions including EscrowDeposit and EscrowRefund");
    }

    #endregion

    #region Key Rotation Tests (TDD Red Phase)

    [Fact]
    public async Task RotateEncryptionKeys_ShouldMaintainDataIntegrity()
    {
        // Arrange - this will fail initially (Red phase)
        var wallet = await _service.CreateWalletAsync(_testUser.Id);
        var originalBalance = wallet.Balance;

        // Act - Simulate key rotation
        await _service.RotateEncryptionKeysAsync();

        // Re-retrieve wallet to ensure it can be decrypted with new key
        var walletAfterRotation = await _service.GetWalletAsync(_testUser.Id);

        // Assert
        walletAfterRotation.Should().NotBeNull();
        walletAfterRotation!.Balance.Should().Be(originalBalance);
    }

    #endregion

    #region Export Functionality Tests (TDD Red Phase)

    [Fact]
    public async Task ExportWalletData_ShouldProvideCompleteTransactionHistory()
    {
        // Arrange - this will fail initially (Red phase)
        var wallet = await _service.CreateWalletAsync(_testUser.Id);
        var toWallet = await _service.CreateWalletAsync(_testUser2.Id);

        // Create transaction history
        await _service.TransferCreditsAsync(_testUser.Id, _testUser2.Id, 40, "Export test", CreditTransactionType.ProjectPayment);

        // Act
        var exportData = await _service.ExportWalletDataAsync(_testUser.Id);

        // Assert
        exportData.Should().NotBeNull();
        exportData.WalletSummary.Should().NotBeNull();
        exportData.TransactionHistory.Should().NotBeEmpty();
        exportData.ExportTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion
}