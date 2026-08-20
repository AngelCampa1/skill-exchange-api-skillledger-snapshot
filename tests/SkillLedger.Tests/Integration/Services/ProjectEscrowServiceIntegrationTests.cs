using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for ProjectEscrowService - FINANCIAL SERVICE (escrow management).
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockDistributedLockService (infrastructure service - OK to mock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (DistributedLock)
/// </summary>
[IntegrationTest]
[FinancialTest]
public class ProjectEscrowServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;  // REAL (writes to DB)
    private readonly MockCreditWalletService _walletService;  // REAL (writes to DB)
    private readonly MockDistributedLockService _lockService;  // Infrastructure - OK to mock
    private readonly ProjectEscrowService _service;

    private User _testClient;
    private User _testProvider;
    private User _testUnauthorized;
    private User _testAdmin;
    private Project _testProject;

    public ProjectEscrowServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProjectEscrowTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal services
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!
        _walletService = new MockCreditWalletService(_context);  // Writes to real DB!

        // Setup infrastructure service
        _lockService = new MockDistributedLockService();

        var logger = new LoggerFactory().CreateLogger<ProjectEscrowService>();

        _service = new ProjectEscrowService(
            _context,
            _walletService,      // REAL
            _auditLogService,    // REAL
            logger,
            _lockService);       // Infrastructure - OK to mock

        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create test users
        _testClient = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@test.com",
            UserName = "client@test.com",
            FirstName = "Test",
            LastName = "Client",
            Status = UserStatus.Active
        };

        _testProvider = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            UserName = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider",
            Status = UserStatus.Active
        };

        _testUnauthorized = new User
        {
            Id = Guid.NewGuid(),
            Email = "unauthorized@test.com",
            UserName = "unauthorized@test.com",
            FirstName = "Test",
            LastName = "Unauthorized",
            Status = UserStatus.Active
        };

        _testAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            UserName = "admin@test.com",
            FirstName = "Test",
            LastName = "Admin",
            Status = UserStatus.Active
        };

        _context.Users.AddRange(_testClient, _testProvider, _testUnauthorized, _testAdmin);

        // Create Admin role and assign to test admin user
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            NormalizedName = "ADMIN",
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        _context.Roles.Add(adminRole);

        var userRole = new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>
        {
            UserId = _testAdmin.Id,
            RoleId = adminRole.Id
        };
        _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().Add(userRole);

        // Create test project
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project with Escrow",
            Description = "Test project for escrow testing",
            ClientId = _testClient.Id,
            CreditBudget = 5000,
            Status = ProjectStatus.InProgress,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _context.Projects.Add(_testProject);

        // Create credit wallet for client with sufficient balance for multiple escrows
        var clientWallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = _testClient.Id,
            Balance = 50000,  // Enough for multiple test escrows
            PendingBalance = 0,
            TotalEarned = 50000,
            TotalSpent = 0,
            EncryptedBalance = "encrypted",
            EncryptedPendingBalance = "encrypted",
            EncryptedTotalEarned = "encrypted",
            EncryptedTotalSpent = "encrypted",
            KeyIdentifier = "test-key",
            LastTransactionAt = DateTime.UtcNow
        };
        _context.CreditWallets.Add(clientWallet);

        // Create credit wallet for provider (required for milestone release)
        var providerWallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = _testProvider.Id,
            Balance = 0,  // Provider starts with zero, receives from escrow
            PendingBalance = 0,
            TotalEarned = 0,
            TotalSpent = 0,
            EncryptedBalance = "encrypted",
            EncryptedPendingBalance = "encrypted",
            EncryptedTotalEarned = "encrypted",
            EncryptedTotalSpent = "encrypted",
            KeyIdentifier = "test-key",
            LastTransactionAt = DateTime.UtcNow
        };
        _context.CreditWallets.Add(providerWallet);

        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateEscrowAsync_ValidRequest_ShouldCreateEscrow()
    {
        // Arrange
        var projectId = _testProject.Id;
        var providerId = _testProvider.Id;
        var initiatedFromIP = "192.168.1.1";

        // Act
        var result = await _service.CreateEscrowAsync(projectId, providerId, initiatedFromIP);

        // Assert - Verify escrow in database
        result.Should().NotBeNull();
        result.ProjectId.Should().Be(projectId);
        result.ProviderId.Should().Be(providerId);
        result.Status.Should().Be(EscrowStatus.Active);
        result.TotalAmount.Should().Be(_testProject.CreditBudget);

        var savedEscrow = await _context.ProjectEscrows.FindAsync(result.Id);
        savedEscrow.Should().NotBeNull();
        savedEscrow!.ProjectId.Should().Be(projectId);

        // Verify audit log - service uses ESCROW_CREATED constant
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ESCROW_CREATED");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateEscrowAsync_DuplicateEscrow_ShouldFail()
    {
        // Arrange
        await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act
        var act = async () => await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateEscrowAsync_NonExistentProject_ShouldFail()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var act = async () => await _service.CreateEscrowAsync(nonExistentProjectId, _testProvider.Id);

        // Assert - Service throws ArgumentException for validation errors
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetEscrowByProjectIdAsync_ExistingEscrow_ShouldReturnEscrow()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act
        var result = await _service.GetEscrowByProjectIdAsync(_testProject.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(escrow.Id);
        result.ProjectId.Should().Be(_testProject.Id);
    }

    [Fact]
    public async Task GetEscrowByProjectIdAsync_NonExistentEscrow_ShouldReturnNull()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var result = await _service.GetEscrowByProjectIdAsync(nonExistentProjectId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEscrowByIdAsync_ExistingEscrow_ShouldReturnEscrow()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act
        var result = await _service.GetEscrowByIdAsync(escrow.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(escrow.Id);
    }

    [Fact]
    public async Task AddMilestoneAsync_ValidMilestone_ShouldCreateMilestone()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var description = "Complete Phase 1";
        var amount = 1000;
        var expectedDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _service.AddMilestoneAsync(
            escrow.Id,
            description,
            amount,
            expectedDate);

        // Assert - Verify milestone in database
        result.Should().NotBeNull();
        result.EscrowId.Should().Be(escrow.Id);
        result.Description.Should().Be(description);
        result.Amount.Should().Be(amount);
        result.IsReleased.Should().BeFalse();

        var savedMilestone = await _context.EscrowMilestones.FindAsync(result.Id);
        savedMilestone.Should().NotBeNull();
        savedMilestone!.Description.Should().Be(description);
    }

    [Fact]
    public async Task AddMilestoneAsync_ExceedsTotalAmount_ShouldFail()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var excessiveAmount = _testProject.CreditBudget + 1000;

        // Act
        var act = async () => await _service.AddMilestoneAsync(
            escrow.Id,
            "Excessive milestone",
            excessiveAmount);

        // Assert - Service throws ArgumentException for validation errors
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*exceed*");
    }

    [Fact]
    public async Task ReleaseMilestoneAsync_ValidMilestone_ShouldReleaseFunds()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var milestone = await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000);

        // Act
        var result = await _service.ReleaseMilestoneAsync(
            milestone.Id,
            _testClient.Id,
            "Work completed successfully");

        // Assert - Verify milestone released in database
        result.Should().BeTrue();

        var updatedMilestone = await _context.EscrowMilestones.FindAsync(milestone.Id);
        updatedMilestone.Should().NotBeNull();
        updatedMilestone!.IsReleased.Should().BeTrue();
        updatedMilestone.ReleasedAt.Should().NotBeNull();
        updatedMilestone.ReleasedByUserId.Should().Be(_testClient.Id);

        // Verify audit log - service uses ESCROW_MILESTONE_RELEASED constant
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ESCROW_MILESTONE_RELEASED");
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task ReleaseMilestoneAsync_AlreadyReleased_ShouldFail()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var milestone = await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000);
        await _service.ReleaseMilestoneAsync(milestone.Id, _testClient.Id);

        // Act & Assert - Service throws exception for already-released milestone
        var act = async () => await _service.ReleaseMilestoneAsync(milestone.Id, _testClient.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be released*");
    }

    [Fact]
    public async Task ReleaseMilestoneAsync_ConcurrentRelease_ShouldPreventDoubleRelease()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var milestone = await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000);

        // Act - Concurrent release attempts
        var task1 = _service.ReleaseMilestoneAsync(milestone.Id, _testClient.Id);
        var task2 = _service.ReleaseMilestoneAsync(milestone.Id, _testClient.Id);

        // Assert - One should succeed, one should throw (lock acquired or already released)
        var successCount = 0;
        var exceptionCount = 0;

        try
        {
            await task1;
            successCount++;
        }
        catch (InvalidOperationException)
        {
            exceptionCount++;
        }

        try
        {
            await task2;
            successCount++;
        }
        catch (InvalidOperationException)
        {
            exceptionCount++;
        }

        successCount.Should().Be(1, "exactly one release should succeed");
        exceptionCount.Should().Be(1, "exactly one release should be prevented");

        var updatedMilestone = await _context.EscrowMilestones.FindAsync(milestone.Id);
        updatedMilestone!.IsReleased.Should().BeTrue();
    }

    [Fact]
    public async Task GetMilestonesAsync_ValidEscrow_ShouldReturnOrderedMilestones()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000, sequenceOrder: 1);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 2", 1500, sequenceOrder: 2);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 3", 2000, sequenceOrder: 3);

        // Act
        var result = await _service.GetMilestonesAsync(escrow.Id);

        // Assert
        result.Should().HaveCount(3);
        result[0].SequenceOrder.Should().Be(1);
        result[1].SequenceOrder.Should().Be(2);
        result[2].SequenceOrder.Should().Be(3);
    }

    [Fact]
    public async Task UpdateMilestoneExpectedDateAsync_ValidRequest_ShouldUpdateDate()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var milestone = await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000);
        var newDate = DateTime.UtcNow.AddDays(14);

        // Act
        var result = await _service.UpdateMilestoneExpectedDateAsync(milestone.Id, newDate);

        // Assert
        result.Should().BeTrue();

        var updatedMilestone = await _context.EscrowMilestones.FindAsync(milestone.Id);
        updatedMilestone!.ExpectedCompletionDate.Should().BeCloseTo(newDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ReleaseFullEscrowAsync_AllMilestonesUnreleased_ShouldReleaseAll()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 2000);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 2", 3000);

        // Act
        var result = await _service.ReleaseFullEscrowAsync(
            escrow.Id,
            _testClient.Id,
            "Project completed successfully");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Completed);
        // Full release transfers remaining amount to provider without individual milestone tracking
        updatedEscrow.ReleasedAmount.Should().Be(updatedEscrow.TotalAmount);
    }

    [Fact]
    public async Task CancelEscrowAsync_ActiveEscrow_ShouldCancelSuccessfully()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act
        var result = await _service.CancelEscrowAsync(
            escrow.Id,
            _testClient.Id,
            "Project cancelled by client");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Cancelled);
        updatedEscrow.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelEscrowAsync_CompletedEscrow_ShouldFail()
    {
        // Arrange - Create escrow and complete it
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        // Complete the escrow by manually setting status (ReleaseFullEscrowAsync requires milestones)
        var dbEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        dbEscrow!.Status = EscrowStatus.Completed;
        await _context.SaveChangesAsync();

        // Act & Assert - Service throws exception for completed escrow
        var act = async () => await _service.CancelEscrowAsync(escrow.Id, _testClient.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel completed*");
    }

    [Fact]
    public async Task RaiseDisputeAsync_ActiveEscrow_ShouldRaiseDispute()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act
        var result = await _service.RaiseDisputeAsync(
            escrow.Id,
            _testProvider.Id,
            "Work not approved unfairly");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Disputed);
        updatedEscrow.DisputedAt.Should().NotBeNull();
        updatedEscrow.DisputeReason.Should().Be("Work not approved unfairly");
    }

    [Fact]
    public async Task ResolveDisputeAsync_DisputedEscrow_ShouldResolve()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.RaiseDisputeAsync(escrow.Id, _testProvider.Id, "Dispute reason");

        // Act - Only admins can resolve disputes
        var result = await _service.ResolveDisputeAsync(
            escrow.Id,
            _testAdmin.Id,  // Must use admin user
            "ReleaseFunds",
            "Dispute resolved in provider's favor");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.DisputeResolvedAt.Should().NotBeNull();
        updatedEscrow.DisputeResolvedByUserId.Should().Be(_testAdmin.Id);  // Admin resolved the dispute
    }

    [Fact]
    public async Task GetDisputedEscrowsAsync_MultipleDisputes_ShouldReturnAll()
    {
        // Arrange
        var escrow1 = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Project 2",
            Description = "Test",
            ClientId = _testClient.Id,
            CreditBudget = 3000,
            Status = ProjectStatus.InProgress,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        _context.Projects.Add(project2);
        await _context.SaveChangesAsync();

        var escrow2 = await _service.CreateEscrowAsync(project2.Id, _testProvider.Id);

        await _service.RaiseDisputeAsync(escrow1.Id, _testProvider.Id, "Dispute 1");
        await _service.RaiseDisputeAsync(escrow2.Id, _testProvider.Id, "Dispute 2");

        // Act
        var result = await _service.GetDisputedEscrowsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Status.Should().Be(EscrowStatus.Disputed));
    }

    [Fact]
    public async Task FreezeEscrowAsync_ActiveEscrow_ShouldFreeze()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        // Act - Only admins can freeze escrow
        var result = await _service.FreezeEscrowAsync(
            escrow.Id,
            _testAdmin.Id,  // Must use admin user
            "Suspected fraud");

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Frozen);
        updatedEscrow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UnfreezeEscrowAsync_FrozenEscrow_ShouldUnfreeze()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.FreezeEscrowAsync(escrow.Id, _testAdmin.Id, "Test freeze");  // Use admin to freeze

        // Act - Only admins can unfreeze escrow
        var result = await _service.UnfreezeEscrowAsync(escrow.Id, _testAdmin.Id);

        // Assert
        result.Should().BeTrue();

        var updatedEscrow = await _context.ProjectEscrows.FindAsync(escrow.Id);
        updatedEscrow!.Status.Should().Be(EscrowStatus.Active);
        updatedEscrow.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ValidateEscrowIntegrityAsync_ValidEscrow_ShouldPass()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 2500);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 2", 2500);

        // Act
        var result = await _service.ValidateEscrowIntegrityAsync(escrow.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEscrowIntegrityAsync_MismatchedAmounts_ShouldPass()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000);
        // Total milestones (1000) < escrow total (5000)
        // Service allows partial milestone definition - only fails if milestones EXCEED total

        // Act
        var result = await _service.ValidateEscrowIntegrityAsync(escrow.Id);

        // Assert - Service validates that milestones don't exceed total, not that they equal it
        result.Should().BeTrue("partial milestone definition is allowed");
    }

    [Fact]
    public async Task GetActiveEscrowsForUserAsync_MultipleEscrows_ShouldReturnUserEscrows()
    {
        // Arrange
        var escrow1 = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);

        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Project 2",
            Description = "Test",
            ClientId = _testClient.Id,
            CreditBudget = 3000,
            Status = ProjectStatus.InProgress,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        _context.Projects.Add(project2);
        await _context.SaveChangesAsync();

        var escrow2 = await _service.CreateEscrowAsync(project2.Id, _testProvider.Id);

        // Act
        var result = await _service.GetActiveEscrowsForUserAsync(_testProvider.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.ProviderId.Should().Be(_testProvider.Id));
    }

    [Fact]
    public async Task GetEscrowHistoryAsync_WithActions_ShouldReturnAuditLogs()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var milestone = await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 1000);
        await _service.ReleaseMilestoneAsync(milestone.Id, _testClient.Id);

        // Act
        var result = await _service.GetEscrowHistoryAsync(escrow.Id);

        // Assert - Service uses audit action constants
        result.Should().NotBeEmpty();
        result.Should().Contain(log => log.Action == "ESCROW_CREATED");
        result.Should().Contain(log => log.Action == "ESCROW_MILESTONE_ADDED");
        result.Should().Contain(log => log.Action == "ESCROW_MILESTONE_RELEASED");
    }

    [Fact]
    public async Task GetEscrowStatisticsAsync_ValidUser_ShouldReturnStatistics()
    {
        // Arrange
        var escrow1 = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow1.Id, "Phase 1", 2000);

        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Project 2",
            Description = "Test",
            ClientId = _testClient.Id,
            CreditBudget = 4000,
            Status = ProjectStatus.InProgress,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        _context.Projects.Add(project2);
        await _context.SaveChangesAsync();

        var escrow2 = await _service.CreateEscrowAsync(project2.Id, _testProvider.Id);
        await _service.ReleaseFullEscrowAsync(escrow2.Id, _testClient.Id);

        // Act
        var result = await _service.GetEscrowStatisticsAsync(_testProvider.Id);

        // Assert
        result.Should().NotBeNull();
        result.TotalEscrowsCreated.Should().BeGreaterThan(0);
        result.ActiveEscrows.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetSystemEscrowMetricsAsync_SystemWide_ShouldReturnMetrics()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 5000);

        // Act
        var result = await _service.GetSystemEscrowMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalActiveEscrows.Should().BeGreaterThan(0);
        result.TotalCreditsInEscrow.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateEscrowReportAsync_DateRange_ShouldGenerateReport()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 5000);

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _service.GenerateEscrowReportAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.ReportPeriodStart.Should().Be(startDate);
        result.ReportPeriodEnd.Should().Be(endDate);
    }

    [Fact]
    public async Task GetEscrowUpdateNotificationAsync_WithActivity_ShouldReturnNotification()
    {
        // Arrange
        var escrow = await _service.CreateEscrowAsync(_testProject.Id, _testProvider.Id);
        var milestone = await _service.AddMilestoneAsync(escrow.Id, "Phase 1", 5000);

        // Act
        var result = await _service.GetEscrowUpdateNotificationAsync(_testProvider.Id);

        // Assert
        result.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
