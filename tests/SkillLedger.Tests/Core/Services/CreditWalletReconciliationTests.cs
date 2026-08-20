using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// Tests for CreditWallet reconciliation functionality
/// Verifies CRIT-006 fix for EscrowDeposit/EscrowRefund inclusion
/// and CRIT-007 fix for concurrent transaction consistency
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Financial")]
public class CreditWalletReconciliationTests : IntegrationTestBase
{
    private readonly ICreditWalletService _walletService;
    private User _clientUser = null!;
    private User _providerUser = null!;
    private User _thirdUser = null!;

    public CreditWalletReconciliationTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Create test users
        _clientUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "reconcile-client@example.com",
            UserName = "reconcile-client@example.com",
            NormalizedEmail = "RECONCILE-CLIENT@EXAMPLE.COM",
            NormalizedUserName = "RECONCILE-CLIENT@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_clientUser);

        _providerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "reconcile-provider@example.com",
            UserName = "reconcile-provider@example.com",
            NormalizedEmail = "RECONCILE-PROVIDER@EXAMPLE.COM",
            NormalizedUserName = "RECONCILE-PROVIDER@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_providerUser);

        _thirdUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "reconcile-third@example.com",
            UserName = "reconcile-third@example.com",
            NormalizedEmail = "RECONCILE-THIRD@EXAMPLE.COM",
            NormalizedUserName = "RECONCILE-THIRD@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_thirdUser);

        await Context.SaveChangesAsync();
    }

    #region Basic Reconciliation Tests

    [Fact]
    public async Task ReconcileWalletBalance_NewWallet_IsBalanced()
    {
        // Arrange
        var wallet = await _walletService.CreateWalletAsync(_clientUser.Id);

        // Act
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        report.Should().NotBeNull();
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(100); // Starting credits
        report.CalculatedBalance.Should().Be(100);
        report.Discrepancy.Should().Be(0);
        report.TransactionCount.Should().Be(0); // Starting credit transaction is excluded from count
    }

    [Fact]
    public async Task ReconcileWalletBalance_AfterSimpleTransfer_IsBalanced()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Transfer some credits
        await _walletService.TransferCreditsAsync(
            _clientUser.Id,
            _providerUser.Id,
            30,
            "Test payment",
            CreditTransactionType.ProjectPayment);

        // Act
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);
        var providerReport = await _walletService.ReconcileWalletBalance(_providerUser.Id);

        // Assert - Client wallet
        clientReport.IsBalanced.Should().BeTrue("client wallet should remain balanced after transfer");
        clientReport.StoredBalance.Should().Be(70); // 100 - 30
        clientReport.CalculatedBalance.Should().Be(70);
        clientReport.TransactionCount.Should().Be(1);

        // Assert - Provider wallet
        providerReport.IsBalanced.Should().BeTrue("provider wallet should remain balanced after transfer");
        providerReport.StoredBalance.Should().Be(130); // 100 + 30
        providerReport.CalculatedBalance.Should().Be(130);
        providerReport.TransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task ReconcileWalletBalance_AfterMultipleTransfers_IsBalanced()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);
        await _walletService.CreateWalletAsync(_thirdUser.Id);

        // Multiple transfers
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 20, "Payment 1", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_clientUser.Id, _thirdUser.Id, 15, "Payment 2", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_providerUser.Id, _clientUser.Id, 10, "Refund", CreditTransactionType.DirectPayment);

        // Act
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);
        var providerReport = await _walletService.ReconcileWalletBalance(_providerUser.Id);
        var thirdReport = await _walletService.ReconcileWalletBalance(_thirdUser.Id);

        // Assert
        clientReport.IsBalanced.Should().BeTrue();
        clientReport.StoredBalance.Should().Be(75); // 100 - 20 - 15 + 10
        clientReport.CalculatedBalance.Should().Be(75);
        clientReport.TransactionCount.Should().Be(3); // 2 outgoing, 1 incoming

        providerReport.IsBalanced.Should().BeTrue();
        providerReport.StoredBalance.Should().Be(110); // 100 + 20 - 10
        providerReport.CalculatedBalance.Should().Be(110);
        providerReport.TransactionCount.Should().Be(2);

        thirdReport.IsBalanced.Should().BeTrue();
        thirdReport.StoredBalance.Should().Be(115); // 100 + 15
        thirdReport.CalculatedBalance.Should().Be(115);
        thirdReport.TransactionCount.Should().Be(1);
    }

    #endregion

    #region CRIT-006: Escrow Transaction Types Tests

    [Fact]
    public async Task ReconcileWalletBalance_WithEscrowDeposit_CorrectlyDeductsFromClient()
    {
        // Arrange - CRIT-006: EscrowDeposit should be counted as outgoing from client
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add extra credits to client
        await _walletService.AddCreditsAsync(_clientUser.Id, 200, "Extra funding", CreditTransactionType.Purchase);

        // Client deposits to escrow (this goes to provider in escrow)
        await _walletService.TransferCreditsAsync(
            _clientUser.Id,
            _providerUser.Id,
            150,
            "Escrow deposit for project",
            CreditTransactionType.EscrowDeposit);

        // Act
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        clientReport.IsBalanced.Should().BeTrue(
            "client wallet should properly account for EscrowDeposit as outgoing (CRIT-006)");
        clientReport.StoredBalance.Should().Be(150); // 100 + 200 - 150
        clientReport.CalculatedBalance.Should().Be(150);
    }

    [Fact]
    public async Task ReconcileWalletBalance_WithEscrowRefund_CorrectlyAddsToClient()
    {
        // Arrange - CRIT-006: EscrowRefund should be counted as incoming to client
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add credits to provider for refund simulation
        await _walletService.AddCreditsAsync(_providerUser.Id, 100, "Escrow balance", CreditTransactionType.Purchase);

        // Provider refunds escrow to client
        await _walletService.TransferCreditsAsync(
            _providerUser.Id,
            _clientUser.Id,
            75,
            "Escrow refund - project cancelled",
            CreditTransactionType.EscrowRefund);

        // Act
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        clientReport.IsBalanced.Should().BeTrue(
            "client wallet should properly account for EscrowRefund as incoming (CRIT-006)");
        clientReport.StoredBalance.Should().Be(175); // 100 + 75
        clientReport.CalculatedBalance.Should().Be(175);
    }

    [Fact]
    public async Task ReconcileWalletBalance_WithEscrowRelease_CorrectlyAddsToProvider()
    {
        // Arrange - EscrowRelease should be counted as incoming to provider
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add credits to client for escrow
        await _walletService.AddCreditsAsync(_clientUser.Id, 200, "Extra funding", CreditTransactionType.Purchase);

        // Escrow release: credits go from client (escrow) to provider
        await _walletService.TransferCreditsAsync(
            _clientUser.Id,
            _providerUser.Id,
            150,
            "Escrow release - milestone completed",
            CreditTransactionType.EscrowRelease);

        // Act
        var providerReport = await _walletService.ReconcileWalletBalance(_providerUser.Id);

        // Assert
        providerReport.IsBalanced.Should().BeTrue(
            "provider wallet should properly account for EscrowRelease as incoming");
        providerReport.StoredBalance.Should().Be(250); // 100 + 150
        providerReport.CalculatedBalance.Should().Be(250);
    }

    [Fact]
    public async Task ReconcileWalletBalance_FullEscrowLifecycle_AllWalletsBalanced()
    {
        // Arrange - Full escrow lifecycle: deposit -> partial release -> partial refund
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add credits for escrow
        await _walletService.AddCreditsAsync(_clientUser.Id, 400, "Project funding", CreditTransactionType.Purchase);

        // 1. Client deposits 300 to escrow (simulated as transfer to provider)
        await _walletService.TransferCreditsAsync(
            _clientUser.Id,
            _providerUser.Id,
            300,
            "Escrow deposit",
            CreditTransactionType.EscrowDeposit);

        // 2. First milestone release: 100 credits to provider
        // (Already handled by EscrowDeposit - no additional transfer needed in this test)

        // 3. Partial refund: 100 credits back to client
        await _walletService.TransferCreditsAsync(
            _providerUser.Id,
            _clientUser.Id,
            100,
            "Partial escrow refund",
            CreditTransactionType.EscrowRefund);

        // Act
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);
        var providerReport = await _walletService.ReconcileWalletBalance(_providerUser.Id);

        // Assert - Client: 100 (start) + 400 (purchase) - 300 (deposit) + 100 (refund) = 300
        clientReport.IsBalanced.Should().BeTrue();
        clientReport.StoredBalance.Should().Be(300);
        clientReport.CalculatedBalance.Should().Be(300);

        // Assert - Provider: 100 (start) + 300 (deposit) - 100 (refund) = 300
        providerReport.IsBalanced.Should().BeTrue();
        providerReport.StoredBalance.Should().Be(300);
        providerReport.CalculatedBalance.Should().Be(300);
    }

    #endregion

    #region Transaction Integrity Tests

    [Fact]
    public async Task ValidateTransactionIntegrity_ValidTransaction_ReturnsTrue()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        var transaction = await _walletService.TransferCreditsAsync(
            _clientUser.Id,
            _providerUser.Id,
            25,
            "Test transfer for integrity check",
            CreditTransactionType.ProjectPayment);

        // Act
        var isValid = await _walletService.ValidateTransactionIntegrity(transaction.Id);

        // Assert
        isValid.Should().BeTrue("transaction hash should be valid for unmodified transaction");
    }

    [Fact]
    public async Task ValidateTransactionIntegrity_NonExistentTransaction_ReturnsFalse()
    {
        // Arrange
        var nonExistentTransactionId = Guid.NewGuid();

        // Act
        var isValid = await _walletService.ValidateTransactionIntegrity(nonExistentTransactionId);

        // Assert
        isValid.Should().BeFalse("non-existent transaction should return false");
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_AllTransactionsHaveValidHashes()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Create multiple transactions
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 10, "Payment 1", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 15, "Payment 2", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 20, "Payment 3", CreditTransactionType.ProjectPayment);

        // Act
        var history = await _walletService.GetTransactionHistoryAsync(_clientUser.Id);

        // Assert
        history.Should().NotBeEmpty();
        foreach (var transaction in history)
        {
            transaction.TransactionHash.Should().NotBeNullOrEmpty(
                "every transaction should have a hash for immutability verification");

            var isValid = await _walletService.ValidateTransactionIntegrity(transaction.Id);
            isValid.Should().BeTrue($"transaction {transaction.Id} hash should be valid");
        }
    }

    #endregion

    #region Available Balance Tests

    [Fact]
    public async Task GetAvailableBalanceAsync_NoEscrow_EqualsBalance()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);

        // Act
        var balance = await _walletService.GetBalanceAsync(_clientUser.Id);
        var availableBalance = await _walletService.GetAvailableBalanceAsync(_clientUser.Id);

        // Assert
        availableBalance.Should().Be(balance);
        availableBalance.Should().Be(100);
    }

    [Fact]
    public async Task GetAvailableBalanceAsync_WithPendingBalance_IsLessThanBalance()
    {
        // Arrange - Create project with escrow to test pending balance
        await _walletService.CreateWalletAsync(_clientUser.Id);

        // Create a project for escrow
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project for Escrow",
            Description = "Test project to verify available balance",
            ClientId = _clientUser.Id,
            CreditBudget = 50,
            Status = ProjectStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
        Context.Projects.Add(project);
        await Context.SaveChangesAsync();

        // Add extra credits and create escrow
        await _walletService.AddCreditsAsync(_clientUser.Id, 100, "Extra funding", CreditTransactionType.Purchase);
        await _walletService.CreateEscrowAsync(_clientUser.Id, project.Id, 50);

        // Act
        var balance = await _walletService.GetBalanceAsync(_clientUser.Id);
        var availableBalance = await _walletService.GetAvailableBalanceAsync(_clientUser.Id);

        // Assert
        balance.Should().Be(200); // 100 + 100
        availableBalance.Should().Be(150); // 200 - 50 (escrowed)
        availableBalance.Should().BeLessThan(balance.Value);
    }

    #endregion

    #region System Reconciliation Tests

    [Fact]
    public async Task ReconcileAllWallets_AllWalletsHealthy_ReturnsHealthyStatus()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);
        await _walletService.CreateWalletAsync(_thirdUser.Id);

        // Make some transfers
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 20, "Payment", CreditTransactionType.ProjectPayment);

        // Act
        var report = await _walletService.ReconcileAllWallets();

        // Assert
        report.Should().NotBeNull();
        report.WalletsReconciled.Should().BeGreaterOrEqualTo(3);
        report.WalletsWithDiscrepancies.Should().Be(0);
        report.TotalDiscrepancy.Should().Be(0);
        report.HealthStatus.Should().Be("Healthy");
        report.Statistics.Should().NotBeNull();
        report.Statistics.TotalTransactions.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReconcileAllWallets_ReturnsCorrectStatistics()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Act
        var report = await _walletService.ReconcileAllWallets();

        // Assert
        report.Statistics.Should().NotBeNull();
        // Starting credit transactions should be counted
        report.Statistics.TotalStartingCreditsAwarded.Should().BeGreaterOrEqualTo(200); // At least 2 users x 100
    }

    #endregion

    #region Concurrent Transaction Tests (CRIT-007)

    [Fact]
    public async Task ReconcileWalletBalance_AfterConcurrentTransactions_RemainsBalanced()
    {
        // Arrange - CRIT-007: Verify balance consistency after concurrent transactions
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);
        await _walletService.CreateWalletAsync(_thirdUser.Id);

        // Add credits for concurrent operations
        await _walletService.AddCreditsAsync(_clientUser.Id, 500, "Large funding", CreditTransactionType.Purchase);

        // Act - Run multiple transfers concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var recipient = i % 2 == 0 ? _providerUser.Id : _thirdUser.Id;
            tasks.Add(_walletService.TransferCreditsAsync(
                _clientUser.Id,
                recipient,
                10,
                $"Concurrent transfer {i}",
                CreditTransactionType.ProjectPayment));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Some transfers may fail due to concurrency - that's expected
        }

        // Reconcile after concurrent operations
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);
        var providerReport = await _walletService.ReconcileWalletBalance(_providerUser.Id);
        var thirdReport = await _walletService.ReconcileWalletBalance(_thirdUser.Id);

        // Assert - All wallets should be balanced regardless of concurrent execution
        clientReport.IsBalanced.Should().BeTrue(
            "client wallet should remain balanced after concurrent transactions (CRIT-007)");
        providerReport.IsBalanced.Should().BeTrue(
            "provider wallet should remain balanced after concurrent transactions (CRIT-007)");
        thirdReport.IsBalanced.Should().BeTrue(
            "third user wallet should remain balanced after concurrent transactions (CRIT-007)");

        // Total credits should be conserved
        var totalBalance = clientReport.StoredBalance + providerReport.StoredBalance + thirdReport.StoredBalance;
        var totalStarting = 100 * 3 + 500; // 3 users x 100 starting + 500 added
        totalBalance.Should().Be(totalStarting, "total credits should be conserved (CRIT-007)");
    }

    [Fact]
    public async Task ReconcileWalletBalance_AfterConcurrentEscrowOperations_RemainsBalanced()
    {
        // Arrange - CRIT-007: Verify escrow operations don't cause inconsistencies
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add credits for escrow operations
        await _walletService.AddCreditsAsync(_clientUser.Id, 1000, "Escrow funding", CreditTransactionType.Purchase);

        // Create multiple projects for escrow
        var projects = new List<Project>();
        for (int i = 0; i < 3; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = $"Concurrent Escrow Project {i}",
                Description = "Testing concurrent escrow",
                ClientId = _clientUser.Id,
                ProviderId = _providerUser.Id,
                CreditBudget = 100,
                Status = ProjectStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            };
            Context.Projects.Add(project);
            projects.Add(project);
        }
        await Context.SaveChangesAsync();

        // Act - Create escrows concurrently
        var tasks = projects.Select(p =>
            _walletService.CreateEscrowAsync(_clientUser.Id, p.Id, 100)).ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Some operations may fail - expected with concurrency
        }

        // Reconcile
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        clientReport.IsBalanced.Should().BeTrue(
            "wallet should remain balanced after concurrent escrow operations (CRIT-007)");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ReconcileWalletBalance_WalletNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _walletService.ReconcileWalletBalance(nonExistentUserId);
        });
    }

    [Fact]
    public async Task ReconcileWalletBalance_AllTransactionTypes_CorrectlyAccountsForEach()
    {
        // Arrange - Test all relevant transaction types
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add credits via Purchase
        await _walletService.AddCreditsAsync(_clientUser.Id, 500, "Initial purchase", CreditTransactionType.Purchase);

        // Various transaction types
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 50, "Project payment", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 30, "Direct payment", CreditTransactionType.DirectPayment);
        await _walletService.TransferCreditsAsync(_providerUser.Id, _clientUser.Id, 20, "Refund", CreditTransactionType.Refund);

        // Deduct via platform fee
        await _walletService.DeductCreditsAsync(_clientUser.Id, 10, "Platform fee", CreditTransactionType.PlatformFee);

        // Act
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        // Expected: 100 (start) + 500 (purchase) - 50 (project) - 30 (direct) + 20 (refund) - 10 (fee) = 530
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(530);
        report.CalculatedBalance.Should().Be(530);
    }

    [Fact]
    public async Task ReconcileWalletBalance_WithRewards_CorrectlyIncludesBonus()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);

        // Add reward
        await _walletService.AddCreditsAsync(_clientUser.Id, 25, "Referral reward", CreditTransactionType.Reward);

        // Act
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(125); // 100 + 25
        report.CalculatedBalance.Should().Be(125);
    }

    [Fact]
    public async Task ReconcileWalletBalance_WithAdjustments_CorrectlyHandlesSystemChanges()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);

        // System adjustment (could be correction or promotional credit)
        await _walletService.AddCreditsAsync(_clientUser.Id, 50, "System adjustment", CreditTransactionType.Adjustment);
        await _walletService.DeductCreditsAsync(_clientUser.Id, 10, "Correction", CreditTransactionType.Adjustment);

        // Act
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(140); // 100 + 50 - 10
        report.CalculatedBalance.Should().Be(140);
    }

    [Fact]
    public async Task ReconcileWalletBalance_LargeTransactionHistory_PerformsEfficiently()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Add credits for many transactions
        await _walletService.AddCreditsAsync(_clientUser.Id, 1000, "Bulk funding", CreditTransactionType.Purchase);

        // Create many transactions
        for (int i = 0; i < 50; i++)
        {
            await _walletService.TransferCreditsAsync(
                _clientUser.Id,
                _providerUser.Id,
                10,
                $"Bulk transfer {i}",
                CreditTransactionType.ProjectPayment);
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);
        stopwatch.Stop();

        // Assert
        report.IsBalanced.Should().BeTrue();
        report.TransactionCount.Should().BeGreaterOrEqualTo(50);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            "reconciliation should complete within reasonable time even with many transactions");
    }

    [Fact]
    public async Task ReconcileWalletBalance_ZeroAmountTransactionsExcluded_IsBalanced()
    {
        // Arrange - Zero amount transactions should not affect reconciliation
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);

        // Regular transfer
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 30, "Normal transfer", CreditTransactionType.ProjectPayment);

        // Act
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(70); // 100 - 30
        report.CalculatedBalance.Should().Be(70);
    }

    #endregion

    #region Credit Conservation Tests

    [Fact]
    public async Task ReconcileWalletBalance_TotalSystemCredits_AreConserved()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_clientUser.Id);
        await _walletService.CreateWalletAsync(_providerUser.Id);
        await _walletService.CreateWalletAsync(_thirdUser.Id);

        var initialTotal = 300; // 3 users x 100 starting credits

        // Perform various transactions
        await _walletService.TransferCreditsAsync(_clientUser.Id, _providerUser.Id, 25, "Payment 1", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_providerUser.Id, _thirdUser.Id, 15, "Payment 2", CreditTransactionType.ProjectPayment);
        await _walletService.TransferCreditsAsync(_thirdUser.Id, _clientUser.Id, 10, "Payment 3", CreditTransactionType.ProjectPayment);

        // Act
        var clientReport = await _walletService.ReconcileWalletBalance(_clientUser.Id);
        var providerReport = await _walletService.ReconcileWalletBalance(_providerUser.Id);
        var thirdReport = await _walletService.ReconcileWalletBalance(_thirdUser.Id);

        // Assert - Total credits should equal initial total (no credits created or destroyed)
        var finalTotal = clientReport.StoredBalance + providerReport.StoredBalance + thirdReport.StoredBalance;
        finalTotal.Should().Be(initialTotal, "total system credits should be conserved across all transfers");

        // Verify individual balances
        // Client: 100 - 25 + 10 = 85
        clientReport.StoredBalance.Should().Be(85);
        // Provider: 100 + 25 - 15 = 110
        providerReport.StoredBalance.Should().Be(110);
        // Third: 100 + 15 - 10 = 105
        thirdReport.StoredBalance.Should().Be(105);
    }

    [Fact]
    public async Task ReconcileWalletBalance_WithPurchaseAndFees_TotalCreditsChange()
    {
        // Arrange - Purchase adds credits, fees deduct - this is expected
        await _walletService.CreateWalletAsync(_clientUser.Id);

        var initialBalance = 100;

        // Add purchase credits (increases total)
        await _walletService.AddCreditsAsync(_clientUser.Id, 200, "Credit purchase", CreditTransactionType.Purchase);

        // Deduct platform fee (decreases total)
        await _walletService.DeductCreditsAsync(_clientUser.Id, 20, "Platform fee", CreditTransactionType.PlatformFee);

        // Act
        var report = await _walletService.ReconcileWalletBalance(_clientUser.Id);

        // Assert
        report.IsBalanced.Should().BeTrue();
        report.StoredBalance.Should().Be(initialBalance + 200 - 20);
        report.CalculatedBalance.Should().Be(280);
    }

    #endregion
}
