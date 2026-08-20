using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using FluentAssertions;

namespace SkillLedger.Tests.Security;

/// <summary>
/// Security tests for escrow operations focusing on concurrency and race conditions.
/// Tests critical bugs: CRIT-005 (double-release), CRIT-008 (double-refund)
/// Following TDD Red-Green-Refactor methodology
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Security")]
public class EscrowConcurrencyTests : IntegrationTestBase
{
    private readonly IProjectEscrowService _escrowService;
    private readonly ICreditWalletService _walletService;
    private User _client = null!;
    private User _provider = null!;
    private Project _project = null!;

    public EscrowConcurrencyTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _client = new User
        {
            Id = Guid.NewGuid(),
            Email = "concurrency-client@example.com",
            UserName = "concurrency-client@example.com",
            NormalizedEmail = "CONCURRENCY-CLIENT@EXAMPLE.COM",
            NormalizedUserName = "CONCURRENCY-CLIENT@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_client);

        _provider = new User
        {
            Id = Guid.NewGuid(),
            Email = "concurrency-provider@example.com",
            UserName = "concurrency-provider@example.com",
            NormalizedEmail = "CONCURRENCY-PROVIDER@EXAMPLE.COM",
            NormalizedUserName = "CONCURRENCY-PROVIDER@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        Context.Users.Add(_provider);

        // Setup test project
        _project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _client.Id,
            Title = "Concurrency Test Project",
            Description = "A project for testing escrow concurrency",
            CreditBudget = 500,
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };
        Context.Projects.Add(_project);

        await Context.SaveChangesAsync();
    }

    #region Double-Release Prevention Tests (CRIT-005)

    [Fact]
    public async Task ReleaseFullEscrow_AfterAlreadyReleased_ShouldThrowException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // First release (should succeed)
        var firstRelease = await _escrowService.ReleaseFullEscrowAsync(escrow.Id, _client.Id, "First release");
        firstRelease.Should().BeTrue();

        // Act - Second release attempt
        Func<Task> secondRelease = () => _escrowService.ReleaseFullEscrowAsync(escrow.Id, _client.Id, "Second release");

        // Assert
        await secondRelease.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be released*");

        // Verify provider only received credits once
        var providerBalance = await _walletService.GetBalanceAsync(_provider.Id);
        providerBalance.Should().Be(600, "provider should have 100 starting + 500 from escrow (not 1100)");
    }

    [Fact]
    public async Task ReleaseMilestone_AfterAlreadyReleased_ShouldThrowException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await _escrowService.AddMilestoneAsync(escrow.Id, "Test milestone", 200, DateTime.UtcNow.AddDays(7));

        // First release (should succeed)
        var firstRelease = await _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, "First release");
        firstRelease.Should().BeTrue();

        // Act - Second release attempt
        Func<Task> secondRelease = () => _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, "Second release");

        // Assert
        await secondRelease.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be released*");

        // Verify provider only received credits once
        var providerBalance = await _walletService.GetBalanceAsync(_provider.Id);
        providerBalance.Should().Be(300, "provider should have 100 starting + 200 milestone (not 500)");
    }

    [Fact]
    public async Task ReleaseMilestone_ConcurrentRequests_ShouldOnlyReleaseOnce()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var initialProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await _escrowService.AddMilestoneAsync(escrow.Id, "Concurrent test milestone", 200, DateTime.UtcNow.AddDays(7));

        // Act - Simulate race condition: multiple concurrent release attempts
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, $"Concurrent release {i}");
                }
                catch (Exception)
                {
                    return false;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Only one release should succeed
        var successCount = results.Count(r => r);
        successCount.Should().Be(1, "only one concurrent release should succeed");

        // Verify provider balance increased by exactly the milestone amount
        var finalProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        finalProviderBalance.Should().Be(initialProviderBalance + 200,
            "provider should receive exactly 200 credits, not multiple times");

        // Verify milestone is marked as released exactly once
        var updatedMilestone = await Context.EscrowMilestones.FindAsync(milestone.Id);
        updatedMilestone!.IsReleased.Should().BeTrue();

        // Verify only one EscrowRelease transaction was created
        var releaseTransactions = await Context.CreditTransactions
            .Where(t => t.ToUserId == _provider.Id && t.Type == CreditTransactionType.EscrowRelease)
            .ToListAsync();
        releaseTransactions.Count.Should().Be(1, "should create exactly one EscrowRelease transaction");
    }

    #endregion

    #region Double-Refund Prevention Tests (CRIT-008)

    [Fact]
    public async Task CancelEscrow_AfterAlreadyCancelled_ShouldThrowException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        var initialClientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // First cancellation (should succeed)
        var firstCancel = await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "First cancellation");
        firstCancel.Should().BeTrue();

        // Act - Second cancellation attempt
        Func<Task> secondCancel = () => _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "Second cancellation");

        // Assert
        await secondCancel.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel completed or already cancelled*");

        // Verify client balance is correct (no double-refund)
        var finalClientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        finalClientBalance.Should().Be(initialClientBalance,
            "client balance should return to original amount after cancellation, not more");
    }

    [Fact]
    public async Task CancelEscrow_ConcurrentRequests_ShouldOnlyRefundOnce()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var initialClientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // After escrow creation, available balance should be reduced
        var balanceAfterEscrow = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        balanceAfterEscrow.Should().BeLessThan(initialClientBalance, "escrow should lock funds");

        // Act - Simulate race condition: multiple concurrent cancellation attempts
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, $"Concurrent cancel {i}");
                }
                catch (Exception)
                {
                    return false;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Only one cancellation should succeed
        var successCount = results.Count(r => r);
        successCount.Should().Be(1, "only one concurrent cancellation should succeed");

        // Verify client balance is restored exactly once (no double-refund)
        var finalClientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        finalClientBalance.Should().Be(initialClientBalance,
            "client balance should be restored to original, not doubled");

        // Verify escrow status is cancelled
        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Cancelled);
    }

    [Fact]
    public async Task CancelEscrow_AfterPartialRelease_ShouldRefundOnlyRemainingAmount()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var initialClientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await _escrowService.AddMilestoneAsync(escrow.Id, "Partial milestone", 200, DateTime.UtcNow.AddDays(7));

        // Release partial milestone
        await _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, "Partial work completed");

        // Act - Cancel escrow (should only refund remaining 300 credits)
        var cancelResult = await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "Cancel after partial release");

        // Assert
        cancelResult.Should().BeTrue();

        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Cancelled);
        updatedEscrow.ReleasedAmount.Should().Be(200, "200 credits were released to provider");
        updatedEscrow.RemainingAmount.Should().Be(300, "300 credits remaining should be refunded");

        // Verify client only gets back the remaining amount
        var finalClientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        // Initial: 1100 (100 starting + 1000 added)
        // After escrow: 600 (1100 - 500 locked)
        // After milestone release: 600 (provider got 200, escrow still has 300)
        // After cancel: 900 (600 + 300 refunded)
        // Total lost: 200 (went to provider for completed work)
        finalClientBalance.Should().Be(initialClientBalance - 200,
            "client should be refunded remaining escrow minus the released milestone amount");
    }

    #endregion

    #region Escrow Creation Concurrency Tests

    [Fact]
    public async Task CreateEscrow_InsufficientBalance_ShouldRejectImmediately()
    {
        // Arrange - Client with no extra funds (only starting 100 credits from wallet creation)
        await _walletService.CreateWalletAsync(_client.Id);

        // Project requires 500 credits, client only has 100
        var clientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        clientBalance.Should().Be(100, "client should only have starting credits");

        // Act & Assert
        await FluentActions.Invoking(() => _escrowService.CreateEscrowAsync(_project.Id, _provider.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient credits in client wallet");
    }

    [Fact]
    public async Task CreateEscrow_ConcurrentAttempts_ShouldOnlyCreateOne()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        // Act - Simulate race condition: multiple concurrent escrow creation attempts
        var tasks = new List<Task<ProjectEscrow?>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    return await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
                }
                catch (Exception)
                {
                    return null;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Only one escrow should be created
        var successfulEscrows = results.Where(r => r != null).ToList();
        successfulEscrows.Should().HaveCount(1, "only one escrow should be created for the project");

        // Verify only one escrow exists in database
        var escrowCount = await Context.ProjectEscrows
            .Where(e => e.ProjectId == _project.Id)
            .CountAsync();
        escrowCount.Should().Be(1, "database should contain exactly one escrow for the project");

        // Verify client balance was only debited once
        var clientBalance = (await _walletService.GetAvailableBalanceAsync(_client.Id)).GetValueOrDefault();
        // Started with 1100 (100 starting + 1000 added), escrow locks 500
        clientBalance.Should().Be(600, "client should only have 500 credits locked once");
    }

    #endregion

    #region Release After Cancel Tests

    [Fact]
    public async Task ReleaseFullEscrow_AfterCancellation_ShouldThrowException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Cancel the escrow first
        await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "Cancellation");

        // Act - Attempt to release the cancelled escrow
        Func<Task> releaseAttempt = () => _escrowService.ReleaseFullEscrowAsync(escrow.Id, _client.Id, "Attempt after cancel");

        // Assert
        await releaseAttempt.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be released*");

        // Verify provider did not receive any credits from this escrow
        var providerBalance = await _walletService.GetBalanceAsync(_provider.Id);
        providerBalance.Should().Be(100, "provider should only have starting credits");
    }

    [Fact]
    public async Task ReleaseMilestone_AfterEscrowCancellation_ShouldThrowException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await _escrowService.AddMilestoneAsync(escrow.Id, "Test milestone", 200, DateTime.UtcNow.AddDays(7));

        // Cancel the escrow first
        await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "Cancellation");

        // Act - Attempt to release milestone on cancelled escrow
        Func<Task> releaseAttempt = () => _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, "Attempt after cancel");

        // Assert
        await releaseAttempt.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be released*");
    }

    #endregion

    #region Transaction Integrity Tests

    [Fact]
    public async Task EscrowOperations_ShouldMaintainWalletIntegrity()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var initialClientBalance = await _walletService.GetBalanceAsync(_client.Id);
        var initialProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        var totalInitialCredits = initialClientBalance + initialProviderBalance;

        // Act - Perform complete escrow flow
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        await _escrowService.AddMilestoneAsync(escrow.Id, "Milestone 1", 200, DateTime.UtcNow.AddDays(7));
        await _escrowService.AddMilestoneAsync(escrow.Id, "Milestone 2", 300, DateTime.UtcNow.AddDays(14));

        // Get milestones
        var milestones = await _escrowService.GetMilestonesAsync(escrow.Id);

        // Release first milestone
        await _escrowService.ReleaseMilestoneAsync(milestones[0].Id, _client.Id, "First milestone complete");

        // Release second milestone
        await _escrowService.ReleaseMilestoneAsync(milestones[1].Id, _client.Id, "Second milestone complete");

        // Assert - Total credits in system should remain constant
        var finalClientBalance = await _walletService.GetBalanceAsync(_client.Id);
        var finalProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        var totalFinalCredits = finalClientBalance + finalProviderBalance;

        totalFinalCredits.Should().Be(totalInitialCredits,
            "total credits in the system should remain constant (no credits created or destroyed)");

        // Verify the correct distribution
        // Client: Started with 1100, escrowed 500, now has 600
        // Provider: Started with 100, received 500 from escrow, now has 600
        finalClientBalance.Should().Be(600);
        finalProviderBalance.Should().Be(600);
    }

    [Fact]
    public async Task EscrowCancellation_ShouldMaintainWalletIntegrity()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var initialClientBalance = await _walletService.GetBalanceAsync(_client.Id);
        var initialProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        var totalInitialCredits = initialClientBalance + initialProviderBalance;

        // Act - Create and immediately cancel escrow
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "Changed mind");

        // Assert - Total credits should be unchanged
        var finalClientBalance = await _walletService.GetBalanceAsync(_client.Id);
        var finalProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        var totalFinalCredits = finalClientBalance + finalProviderBalance;

        totalFinalCredits.Should().Be(totalInitialCredits,
            "total credits should remain constant after escrow creation and cancellation");

        // Client should have their original balance restored
        finalClientBalance.Should().Be(initialClientBalance);
        // Provider should be unchanged
        finalProviderBalance.Should().Be(initialProviderBalance);
    }

    #endregion
}
