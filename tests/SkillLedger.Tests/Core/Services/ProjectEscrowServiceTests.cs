using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using FluentAssertions;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// TDD tests for Project Escrow service with secure escrow operations
/// Following Red-Green-Refactor methodology for financial critical flows
/// </summary>
[UnitTest]
[FinancialTest]
[Collection("Integration Financial")]
public class ProjectEscrowServiceTests : IntegrationTestBase
{
    private readonly IProjectEscrowService _escrowService;
    private readonly ICreditWalletService _walletService;
    private readonly UserManager<User> _userManager;
    private User _client = null!;
    private User _provider = null!;
    private Project _project = null!;

    public ProjectEscrowServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
        _userManager = ServiceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _client = new User
        {
            Id = Guid.NewGuid(),
            Email = "escrow-client@example.com",
            UserName = "escrow-client@example.com",
            NormalizedEmail = "ESCROW-CLIENT@EXAMPLE.COM",
            NormalizedUserName = "ESCROW-CLIENT@EXAMPLE.COM",
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
            Email = "escrow-provider@example.com",
            UserName = "escrow-provider@example.com",
            NormalizedEmail = "ESCROW-PROVIDER@EXAMPLE.COM",
            NormalizedUserName = "ESCROW-PROVIDER@EXAMPLE.COM",
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
            Title = "Test Escrow Project",
            Description = "A project requiring escrow services",
            CreditBudget = 500,
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };
        Context.Projects.Add(_project);

        await Context.SaveChangesAsync();
    }

    #region Escrow Creation Tests (TDD)

    [Fact]
    public async Task CreateEscrowAsync_WithValidData_ShouldCreateEscrowSuccessfully()
    {
        // Arrange - Ensure client has sufficient funds
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        // Act
        var result = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Assert
        result.Should().NotBeNull();
        result.ProjectId.Should().Be(_project.Id);
        result.ClientId.Should().Be(_client.Id);
        result.ProviderId.Should().Be(_provider.Id);
        result.TotalAmount.Should().Be(500);
        result.Status.Should().Be(EscrowStatus.Active);
        result.ReleasedAmount.Should().Be(0);
        result.RemainingAmount.Should().Be(500);
    }

    [Fact]
    public async Task CreateEscrowAsync_WithInsufficientFunds_ShouldThrowInvalidOperationException()
    {
        // Arrange - Client with insufficient funds
        await _walletService.CreateWalletAsync(_client.Id);

        // Act & Assert
        await FluentActions.Invoking(() => _escrowService.CreateEscrowAsync(_project.Id, _provider.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient credits in client wallet");
    }

    [Fact]
    public async Task CreateEscrowAsync_WithNonExistentProject_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();

        // Act & Assert
        await FluentActions.Invoking(() => _escrowService.CreateEscrowAsync(nonExistentProjectId, _provider.Id))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Project not found");
    }

    [Fact]
    public async Task CreateEscrowAsync_WithExistingEscrow_ShouldThrowInvalidOperationException()
    {
        // Arrange - Create initial escrow
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Act & Assert - Try to create duplicate escrow
        await FluentActions.Invoking(() => _escrowService.CreateEscrowAsync(_project.Id, _provider.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Escrow already exists for this project");
    }

    #endregion

    #region Milestone Management Tests (TDD)

    [Fact]
    public async Task AddMilestoneAsync_WithValidData_ShouldCreateMilestoneSuccessfully()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Act
        var milestone = await _escrowService.AddMilestoneAsync(
            escrow.Id,
            "Complete initial design",
            150,
            DateTime.UtcNow.AddDays(7));

        // Assert
        milestone.Should().NotBeNull();
        milestone.EscrowId.Should().Be(escrow.Id);
        milestone.Description.Should().Be("Complete initial design");
        milestone.Amount.Should().Be(150);
        milestone.IsReleased.Should().BeFalse();
        milestone.SequenceOrder.Should().Be(1);
    }

    [Fact]
    public async Task AddMilestoneAsync_WithAmountExceedingEscrow_ShouldThrowArgumentException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Act & Assert
        // VULN-027 FIX: Enhanced error message now includes specific amounts for better debugging
        await FluentActions.Invoking(() => _escrowService.AddMilestoneAsync(
            escrow.Id, "Excessive milestone", 600, DateTime.UtcNow.AddDays(7)))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Total milestone amounts (600) would exceed escrow total (500)*");
    }

    [Fact]
    public async Task ReleaseMilestoneAsync_WithValidConditions_ShouldReleaseSuccessfully()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await _escrowService.AddMilestoneAsync(
            escrow.Id, "Complete design", 150, DateTime.UtcNow.AddDays(7));

        // Check provider balance BEFORE release for debugging
        var providerBalanceBeforeRelease = await _walletService.GetBalanceAsync(_provider.Id);
        Console.WriteLine($"Provider balance before release: {providerBalanceBeforeRelease}");

        // Act
        var result = await _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, "Work completed successfully");

        // Assert
        result.Should().BeTrue();

        // Verify milestone is released
        var updatedMilestone = await Context.EscrowMilestones.FindAsync(milestone.Id);
        updatedMilestone!.IsReleased.Should().BeTrue();
        updatedMilestone.ReleasedByUserId.Should().Be(_client.Id);

        // Verify escrow status updated
        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.ReleasedAmount.Should().Be(150);
        updatedEscrow.Status.Should().Be(EscrowStatus.PartiallyReleased);


        // Verify provider received credits (starting credits + milestone amount)
        var providerBalance = await _walletService.GetBalanceAsync(_provider.Id);
        Console.WriteLine($"Provider balance after release: {providerBalance}");
        providerBalance.Should().Be(250); // 100 starting + 150 milestone
    }

    #endregion

    #region Dispute Management Tests (TDD)

    [Fact]
    public async Task RaiseDisputeAsync_WithValidReason_ShouldCreateDisputeSuccessfully()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Act
        var result = await _escrowService.RaiseDisputeAsync(escrow.Id, _client.Id, "Work not completed as agreed");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Disputed);
        updatedEscrow.DisputeReason.Should().Be("Work not completed as agreed");
        updatedEscrow.DisputedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ResolveDisputeAsync_ByAdmin_ShouldResolveSuccessfully()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        await _escrowService.RaiseDisputeAsync(escrow.Id, _client.Id, "Test dispute");

        // Create Admin role directly in the database using custom Role entity
        var adminRole = new Role("Admin")
        {
            NormalizedName = "ADMIN",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        Context.Roles.Add(adminRole);
        await Context.SaveChangesAsync();

        // Create admin user with all required Identity fields
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@skillledger.app",
            UserName = "admin@skillledger.app",
            NormalizedEmail = "ADMIN@SKILLLEDGER.APP",
            NormalizedUserName = "ADMIN@SKILLLEDGER.APP",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        // Use UserManager to create the admin user with proper Identity integration
        var createResult = await _userManager.CreateAsync(adminUser, "AdminPassword123!");
        if (!createResult.Succeeded)
        {
            throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        // Assign Admin role to the user by creating a UserRole relationship
        var userRole = new IdentityUserRole<Guid>
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        };
        Context.UserRoles.Add(userRole);
        await Context.SaveChangesAsync();

        // Reload the user from context to ensure proper tracking
        var createdAdminUser = await Context.Users.FindAsync(adminUser.Id);
        if (createdAdminUser == null)
        {
            throw new Exception("Failed to find created admin user in database context");
        }
        adminUser = createdAdminUser;

        // Act
        var result = await _escrowService.ResolveDisputeAsync(
            escrow.Id, adminUser.Id, "Resolved in favor of client", "Dispute resolved through mediation");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Active);
        updatedEscrow.DisputeResolvedByUserId.Should().Be(adminUser.Id);
        updatedEscrow.DisputeResolutionNotes.Should().Be("Dispute resolved through mediation");
    }

    #endregion

    #region Full Escrow Release Tests (TDD)

    [Fact]
    public async Task ReleaseFullEscrowAsync_WithValidConditions_ShouldCompleteEscrowSuccessfully()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Act
        var result = await _escrowService.ReleaseFullEscrowAsync(escrow.Id, _client.Id, "Project completed successfully");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Completed);
        updatedEscrow.ReleasedAmount.Should().Be(500);
        updatedEscrow.IsFullyReleased.Should().BeTrue();
        updatedEscrow.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify provider received all credits (starting + escrow amount)
        var providerBalance = await _walletService.GetBalanceAsync(_provider.Id);
        providerBalance.Should().Be(600); // 100 starting + 500 escrow
    }

    [Fact]
    public async Task ReleaseFullEscrowAsync_ConcurrentRequests_ShouldOnlyReleaseOnce()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var initialProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Act - Simulate race condition: two concurrent release attempts
        var task1 = Task.Run(async () =>
        {
            try
            {
                return await _escrowService.ReleaseFullEscrowAsync(escrow.Id, _client.Id, "Concurrent release 1");
            }
            catch (Exception)
            {
                return false;
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                return await _escrowService.ReleaseFullEscrowAsync(escrow.Id, _client.Id, "Concurrent release 2");
            }
            catch (Exception)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert - Only one release should succeed
        var successCount = results.Count(r => r);
        successCount.Should().Be(1, "only one concurrent release should succeed");

        // Verify escrow is marked as completed exactly once
        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Completed);
        updatedEscrow.ReleasedAmount.Should().Be(500, "full escrow amount should be released once");
        updatedEscrow.IsFullyReleased.Should().BeTrue();

        // Verify provider balance increased by exactly the escrow amount (no double-release)
        var finalProviderBalance = await _walletService.GetBalanceAsync(_provider.Id);
        finalProviderBalance.Should().Be(initialProviderBalance + 500,
            "provider should receive exactly 500 credits, not double");

        // Verify only one EscrowRelease transaction was created
        var escrowReleaseTransactions = await Context.CreditTransactions
            .Where(t => t.ToUserId == _provider.Id && t.Type == CreditTransactionType.EscrowRelease)
            .ToListAsync();
        escrowReleaseTransactions.Count.Should().Be(1, "should create exactly one EscrowRelease transaction");
    }

    #endregion

    #region Escrow Cancellation Tests (TDD)

    [Fact]
    public async Task CancelEscrowAsync_WithValidConditions_ShouldRefundClientSuccessfully()
    {
        // Arrange
        // CreateWalletAsync gives 100 starting credits, then we add 1000 more for testing
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        // Verify initial balance before creating escrow (100 starting + 1000 added = 1100)
        var initialBalance = await _walletService.GetAvailableBalanceAsync(_client.Id);
        initialBalance.Should().Be(1100, "wallet should have 1100 credits (100 starting + 1000 added) before escrow");

        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // BUG-CRIT-001 FIX: Use GetAvailableBalanceAsync instead of GetBalanceAsync
        // GetBalanceAsync returns total balance, GetAvailableBalanceAsync returns balance - pending
        // After escrow creation, available balance should be reduced by escrow amount (500)
        var clientBalanceBeforeCancel = await _walletService.GetAvailableBalanceAsync(_client.Id);
        clientBalanceBeforeCancel.Should().Be(600, "available balance should be 600 (1100 - 500 locked in escrow)");

        // Act
        var result = await _escrowService.CancelEscrowAsync(escrow.Id, _client.Id, "Project cancelled by client");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await Context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Cancelled);
        updatedEscrow.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // BUG-TEST-001 FIXED: RefundEscrowAsync now correctly only reduces pending balance
        // When escrow is refunded, it should restore the available balance to original amount
        // Expected: 1100 (100 starting + 1000 added - no double crediting)
        var clientBalanceAfterCancel = await _walletService.GetAvailableBalanceAsync(_client.Id);
        clientBalanceAfterCancel.Should().Be(1100, "refund should restore available balance to original 1100 (no double-crediting)");
    }

    #endregion

    #region Security Tests (TDD)

    [Fact]
    public async Task CreateEscrowAsync_WithHighValueProject_ShouldRequireMultiSignature()
    {
        // Arrange - High value project
        var highValueProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _client.Id,
            Title = "High Value Project",
            Description = "Project requiring multi-signature approval",
            CreditBudget = 1500, // > 1000 credits
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(60)
        };
        Context.Projects.Add(highValueProject);
        await Context.SaveChangesAsync();

        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 2000, "High value funding", CreditTransactionType.Purchase);

        // Act
        var escrow = await _escrowService.CreateEscrowAsync(highValueProject.Id, _provider.Id);

        // Assert
        escrow.RequiresMultiSignature.Should().BeTrue();
        escrow.TotalAmount.Should().Be(1500);
    }

    [Fact]
    public async Task ReleaseMilestoneAsync_ByUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await _escrowService.AddMilestoneAsync(
            escrow.Id, "Test milestone", 150, DateTime.UtcNow.AddDays(7));

        var unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "unauthorized@example.com",
            UserName = "unauthorized@example.com",
            Status = UserStatus.Active
        };
        Context.Users.Add(unauthorizedUser);
        await Context.SaveChangesAsync();

        // Act & Assert
        await FluentActions.Invoking(() => _escrowService.ReleaseMilestoneAsync(
            milestone.Id, unauthorizedUser.Id, "Unauthorized release attempt"))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only the project client can approve milestone releases");
    }

    #endregion

    #region Audit and Tracking Tests (TDD)

    [Fact]
    public async Task GetEscrowHistoryAsync_ShouldReturnCompleteAuditTrail()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);
        var escrow = await _escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        // Perform various operations
        var milestone = await _escrowService.AddMilestoneAsync(
            escrow.Id, "Design phase", 200, DateTime.UtcNow.AddDays(7));
        await _escrowService.ReleaseMilestoneAsync(milestone.Id, _client.Id, "Design completed");
        await _escrowService.RaiseDisputeAsync(escrow.Id, _provider.Id, "Payment issue");

        // Act
        var history = await _escrowService.GetEscrowHistoryAsync(escrow.Id);

        // Assert
        history.Should().NotBeEmpty();
        history.Count.Should().BeGreaterThan(2); // Creation, milestone release, dispute
        history.Should().Contain(h => h.Details != null && h.Details.Contains("Escrow created"));
        history.Should().Contain(h => h.Details != null && h.Details.Contains("Milestone released"));
        history.Should().Contain(h => h.Details != null && h.Details.Contains("Dispute raised"));
    }

    #endregion

    #region Performance and Scalability Tests (TDD)

    [Fact]
    public async Task GetActiveEscrowsForUser_WithManyEscrows_ShouldReturnResultsEfficiently()
    {
        // Arrange - Create multiple escrows
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 5000, "Large funding", CreditTransactionType.Purchase);

        var projects = new List<Project>();
        for (int i = 0; i < 10; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = _client.Id,
                Title = $"Performance Test Project {i}",
                Description = $"Project for performance testing {i}",
                CreditBudget = 100,
                Status = ProjectStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            projects.Add(project);
            Context.Projects.Add(project);
        }
        await Context.SaveChangesAsync();

        // Create escrows
        foreach (var project in projects)
        {
            await _escrowService.CreateEscrowAsync(project.Id, _provider.Id);
        }

        // Act
        var startTime = DateTime.UtcNow;
        var activeEscrows = await _escrowService.GetActiveEscrowsForUserAsync(_client.Id);
        var executionTime = DateTime.UtcNow - startTime;

        // Assert
        activeEscrows.Should().HaveCount(10);
        executionTime.Should().BeLessThan(TimeSpan.FromSeconds(1), "Query should complete within 1 second");
    }

    #endregion
}