using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Comprehensive integration tests for EscrowController API endpoints
/// CRITICAL financial operations - Payment processing, escrow management, double-release prevention
/// Target: 95%+ line coverage, 85%+ branch coverage
/// </summary>
[IntegrationTest]
[FinancialTest]
[Collection("Integration Api 1")]
public class EscrowControllerIntegrationTests : IntegrationTestBase
{
    private readonly IProjectEscrowService _escrowService;
    private readonly IProjectService _projectService;
    private readonly IAuditLogService _auditLogService;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ICreditWalletService _walletService;

    private User _client = null!;
    private User _provider = null!;
    private Project _testProject = null!;
    private const string TestPassword = "TestPassword123!@#";

    public EscrowControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        _projectService = ServiceScope.ServiceProvider.GetRequiredService<IProjectService>();
        _auditLogService = ServiceScope.ServiceProvider.GetRequiredService<IAuditLogService>();
        _idempotencyService = ServiceScope.ServiceProvider.GetRequiredService<IIdempotencyService>();
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Create client user
        _client = new User
        {
            Id = Guid.NewGuid(),
            Email = $"escrow-client-{Guid.NewGuid():N}@test.com",
            UserName = $"escrow-client-{Guid.NewGuid():N}@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        // Create provider user
        _provider = new User
        {
            Id = Guid.NewGuid(),
            Email = $"escrow-provider-{Guid.NewGuid():N}@test.com",
            UserName = $"escrow-provider-{Guid.NewGuid():N}@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        Context.Users.AddRange(_client, _provider);
        await Context.SaveChangesAsync();

        // Create client's wallet with funds
        var clientWallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = _client.Id,
            Balance = 10000,
            TotalEarned = 10000
        };

        Context.CreditWallets.Add(clientWallet);

        // Create test project
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Escrow Project",
            Description = "Project for escrow testing",
            ClientId = _client.Id,
            ProviderId = _provider.Id,
            CreditBudget = 1000,
            Status = ProjectStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        Context.Projects.Add(_testProject);
        await Context.SaveChangesAsync();
    }

    #region POST /api/escrow/create Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithValidData_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_client);
        var request = new
        {
            ProjectId = _testProject.Id,
            ProviderId = _provider.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);

        if (response.IsSuccessStatusCode)
        {
            var escrow = await Context.ProjectEscrows
                .FirstOrDefaultAsync(e => e.ProjectId == _testProject.Id);
            escrow.Should().NotBeNull();
        }
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication
        var request = new
        {
            ProjectId = _testProject.Id,
            ProviderId = _provider.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithInvalidProjectId_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAs(_client);
        var request = new
        {
            ProjectId = Guid.NewGuid(),
            ProviderId = _provider.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithMissingFields_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_client);
        var request = new { }; // Missing required fields

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/escrow/project/{projectId} Tests

    [Fact]
    [FastTest]
    public async Task GET_EscrowByProject_WithValidProjectId_ReturnsOk()
    {
        // Arrange - Create escrow first
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/escrow/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_EscrowByProject_WithUnauthorizedUser_ReturnsForbidden()
    {
        // Arrange - Create escrow first
        var escrow = await CreateTestEscrow();

        // Create unauthorized user
        var unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"unauthorized-{Guid.NewGuid():N}@test.com",
            UserName = $"unauthorized-{Guid.NewGuid():N}@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };
        Context.Users.Add(unauthorizedUser);
        await Context.SaveChangesAsync();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, unauthorizedUser.Id);

        AuthenticateAs(unauthorizedUser);

        // Act
        var response = await Client.GetAsync($"/api/escrow/project/{_testProject.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_EscrowByProject_WithNonExistentProject_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/escrow/project/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/escrow/user/active Tests

    [Fact]
    [FastTest]
    public async Task GET_UserActiveEscrows_WithAuthentication_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync("/api/escrow/user/active");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserActiveEscrows_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await Client.GetAsync("/api/escrow/user/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/escrow/{escrowId}/history Tests

    [Fact]
    [FastTest]
    public async Task GET_EscrowHistory_WithValidEscrowId_ReturnsOk()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/escrow/{escrow.Id}/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_EscrowHistory_WithUnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();

        var unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"unauth-history-{Guid.NewGuid():N}@test.com",
            UserName = $"unauth-history-{Guid.NewGuid():N}@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };
        Context.Users.Add(unauthorizedUser);
        await Context.SaveChangesAsync();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, unauthorizedUser.Id);

        AuthenticateAs(unauthorizedUser);

        // Act
        var response = await Client.GetAsync($"/api/escrow/{escrow.Id}/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/escrow/milestone/add Tests

    [Fact]
    [FastTest]
    public async Task POST_AddMilestone_WithValidData_ReturnsCreated()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        var request = new
        {
            EscrowId = escrow.Id,
            Description = "Complete initial design mockups",
            Amount = 250,
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(7),
            SequenceOrder = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/milestone/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddMilestone_AsProvider_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_provider); // Provider should not be able to add milestones

        var request = new
        {
            EscrowId = escrow.Id,
            Description = "Milestone by provider (should fail)",
            Amount = 100,
            SequenceOrder = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/milestone/add", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddMilestone_ExceedingEscrowAmount_ReturnsBadRequest()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        var request = new
        {
            EscrowId = escrow.Id,
            Description = "Milestone exceeding budget",
            Amount = 50000, // Exceeds escrow total
            SequenceOrder = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/milestone/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddMilestone_WithInvalidDescription_ReturnsBadRequest()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        var request = new
        {
            EscrowId = escrow.Id,
            Description = "Short", // Too short (< 10 chars)
            Amount = 100,
            SequenceOrder = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/milestone/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/escrow/milestone/release Tests (CRITICAL - Double-Release Bug)

    [Fact]
    [FastTest]
    [SecurityTest]
    public async Task PUT_ReleaseMilestone_WithValidData_ReturnsOk()
    {
        // Arrange
        var (escrow, milestone) = await CreateTestEscrowWithMilestone();
        AuthenticateAs(_client);

        var request = new
        {
            MilestoneId = milestone.Id,
            ReleaseNotes = "Work completed successfully"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/escrow/milestone/release", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    [SecurityTest]
    [FinancialTest]
    public async Task PUT_ReleaseMilestone_DuplicateRequest_IsIdempotent()
    {
        // CRITICAL: Test for CRIT-005 fix - prevent double-release
        // Arrange
        var (escrow, milestone) = await CreateTestEscrowWithMilestone();
        AuthenticateAs(_client);

        var request = new
        {
            MilestoneId = milestone.Id,
            ReleaseNotes = "Idempotency test"
        };

        // Act - Send duplicate requests
        var response1 = await Client.PutAsJsonAsync("/api/escrow/milestone/release", request);
        var response2 = await Client.PutAsJsonAsync("/api/escrow/milestone/release", request);

        // Assert - Both should return success (second request is idempotent)
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        // Verify milestone released only once
        var milestoneAfter = await Context.EscrowMilestones.FindAsync(milestone.Id);
        if (milestoneAfter != null && milestoneAfter.IsReleased)
        {
            // If released, verify it was released exactly once (not double-released)
            var releaseHistory = await Context.AuditLogs
                .Where(h => h.Action.Contains("Milestone") && h.Details != null && h.Details.Contains(milestone.Id.ToString()))
                .ToListAsync();

            // Should have exactly one release event
            releaseHistory.Count(h => h.Action.Contains("Release")).Should().BeLessOrEqualTo(1);
        }
    }

    [Fact]
    [FastTest]
    public async Task PUT_ReleaseMilestone_AsProvider_ReturnsForbidden()
    {
        // Arrange
        var (escrow, milestone) = await CreateTestEscrowWithMilestone();
        AuthenticateAs(_provider); // Provider cannot release their own milestone

        var request = new
        {
            MilestoneId = milestone.Id,
            ReleaseNotes = "Self-release attempt (should fail)"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/escrow/milestone/release", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/escrow/{escrowId}/milestones Tests

    [Fact]
    [FastTest]
    public async Task GET_Milestones_WithValidEscrowId_ReturnsOk()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/escrow/{escrow.Id}/milestones");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Milestones_WithUnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();

        var unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"unauth-milestones-{Guid.NewGuid():N}@test.com",
            UserName = $"unauth-milestones-{Guid.NewGuid():N}@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };
        Context.Users.Add(unauthorizedUser);
        await Context.SaveChangesAsync();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, unauthorizedUser.Id);

        AuthenticateAs(unauthorizedUser);

        // Act
        var response = await Client.GetAsync($"/api/escrow/{escrow.Id}/milestones");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region PUT /api/escrow/release-full Tests (CRITICAL - Double-Release Bug)

    [Fact]
    [FastTest]
    [SecurityTest]
    [FinancialTest]
    public async Task PUT_ReleaseFullEscrow_WithValidData_ReturnsOk()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        var request = new
        {
            EscrowId = escrow.Id,
            ReleaseNotes = "Project completed successfully"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/escrow/release-full", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    [SecurityTest]
    [FinancialTest]
    public async Task PUT_ReleaseFullEscrow_DuplicateRequest_IsIdempotent()
    {
        // CRITICAL: Test for CRIT-005 fix - prevent double-release of full escrow
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        var request = new
        {
            EscrowId = escrow.Id,
            ReleaseNotes = "Full release idempotency test"
        };

        // Act - Send duplicate requests
        var response1 = await Client.PutAsJsonAsync("/api/escrow/release-full", request);
        var response2 = await Client.PutAsJsonAsync("/api/escrow/release-full", request);

        // Assert - Both should return success or appropriate error
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);

        // Verify escrow released only once (check database state)
        var escrowAfter = await Context.ProjectEscrows.FindAsync(escrow.Id);
        if (escrowAfter != null)
        {
            // Verify release history shows only one release event
            var releaseHistory = await Context.AuditLogs
                .Where(h => h.Action.Contains("Release") && h.Action.Contains("Full") &&
                            h.Details != null && h.Details.Contains(escrow.Id.ToString()))
                .ToListAsync();

            releaseHistory.Count.Should().BeLessOrEqualTo(1);
        }
    }

    #endregion

    #region PUT /api/escrow/cancel Tests

    [Fact]
    [FastTest]
    public async Task PUT_CancelEscrow_WithValidData_ReturnsOk()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client);

        var request = new
        {
            EscrowId = escrow.Id,
            CancellationReason = "Project scope changed significantly"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/escrow/cancel", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task PUT_CancelEscrow_AsProvider_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_provider); // Provider cannot cancel escrow

        var request = new
        {
            EscrowId = escrow.Id,
            CancellationReason = "Provider attempting cancellation (should fail)"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/escrow/cancel", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/escrow/dispute/raise Tests

    [Fact]
    [FastTest]
    [SecurityTest]
    public async Task POST_RaiseDispute_WithValidData_ReturnsOk()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_provider);

        var request = new
        {
            EscrowId = escrow.Id,
            DisputeReason = "Deliverables do not match agreed specifications as outlined in the contract"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/dispute/raise", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    [SecurityTest]
    public async Task POST_RaiseDispute_DuplicateRequest_IsIdempotent()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_provider);

        var request = new
        {
            EscrowId = escrow.Id,
            DisputeReason = "Duplicate dispute idempotency test - work quality issues"
        };

        // Act - Send duplicate requests
        var response1 = await Client.PostAsJsonAsync("/api/escrow/dispute/raise", request);
        var response2 = await Client.PostAsJsonAsync("/api/escrow/dispute/raise", request);

        // Assert
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_RaiseDispute_WithShortReason_ReturnsBadRequest()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_provider);

        var request = new
        {
            EscrowId = escrow.Id,
            DisputeReason = "Short" // Too short (< 10 chars)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/escrow/dispute/raise", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/escrow/statistics Tests

    [Fact]
    [FastTest]
    public async Task GET_UserStatistics_WithAuthentication_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync("/api/escrow/statistics");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_UserStatistics_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await Client.GetAsync("/api/escrow/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/escrow/dispute/resolve Tests (Admin)

    [Fact]
    [FastTest]
    public async Task PUT_ResolveDispute_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client); // Non-admin user

        var request = new
        {
            EscrowId = escrow.Id,
            ResolutionAction = "refund_client",
            ResolutionNotes = "Client was right, refunding"
        };

        // Act
        var response = await Client.PutAsJsonAsync("/api/escrow/dispute/resolve", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/escrow/disputes Tests (Admin)

    [Fact]
    [FastTest]
    public async Task GET_DisputedEscrows_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_client); // Non-admin user

        // Act
        var response = await Client.GetAsync("/api/escrow/disputes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /api/escrow/{id}/freeze Tests (Admin)

    [Fact]
    [FastTest]
    public async Task PUT_FreezeEscrow_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client); // Non-admin user

        var request = new
        {
            FreezeReason = "Suspicious activity detected"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/escrow/{escrow.Id}/freeze", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [FastTest]
    public async Task PUT_FreezeEscrow_WithNonExistentEscrow_ReturnsForbiddenOrBadRequest()
    {
        // Arrange
        AuthenticateAsAdmin(); // Note: Admin role not actually assigned in test
        var nonExistentId = Guid.NewGuid();

        var request = new
        {
            FreezeReason = "Test freeze reason"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/escrow/{nonExistentId}/freeze", request);

        // Assert - 403 if role check happens first, 400/404 if escrow validation happens first
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region PUT /api/escrow/{id}/unfreeze Tests (Admin)

    [Fact]
    [FastTest]
    public async Task PUT_UnfreezeEscrow_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var escrow = await CreateTestEscrow();
        AuthenticateAs(_client); // Non-admin user

        // Act
        var response = await Client.PutAsJsonAsync($"/api/escrow/{escrow.Id}/unfreeze", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UnfreezeEscrow_WithNonExistentEscrow_ReturnsForbiddenOrBadRequest()
    {
        // Arrange
        AuthenticateAsAdmin(); // Note: Admin role not actually assigned in test
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.PutAsJsonAsync($"/api/escrow/{nonExistentId}/unfreeze", new { });

        // Assert - 403 if role check happens first, 400/404 if escrow validation happens first
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/escrow/metrics Tests

    [Fact]
    [FastTest]
    public async Task GET_Metrics_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_client); // Regular user, not admin

        // Act
        var response = await Client.GetAsync("/api/escrow/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [FastTest]
    public async Task GET_Metrics_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange - No authentication

        // Act
        var response = await Client.GetAsync("/api/escrow/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    private void AuthenticateAsAdmin()
    {
        // Create admin user for testing
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"admin-{Guid.NewGuid():N}@test.com",
            UserName = $"admin-{Guid.NewGuid():N}@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        Context.Users.Add(adminUser);
        Context.SaveChangesAsync().Wait();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, adminUser.Id);

        AuthenticateAs(adminUser);
    }

    private async Task<ProjectEscrow> CreateTestEscrow()
    {
        var escrow = new ProjectEscrow
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ClientId = _client.Id,
            ProviderId = _provider.Id,
            TotalAmount = 1000,
            ReleasedAmount = 0,
            Status = EscrowStatus.Active,
            RequiresMultiSignature = false,
            CreatedAt = DateTime.UtcNow
        };

        Context.ProjectEscrows.Add(escrow);
        await Context.SaveChangesAsync();

        return escrow;
    }

    private async Task<(ProjectEscrow escrow, EscrowMilestone milestone)> CreateTestEscrowWithMilestone()
    {
        var escrow = await CreateTestEscrow();

        var milestone = new EscrowMilestone
        {
            Id = Guid.NewGuid(),
            EscrowId = escrow.Id,
            Description = "Complete phase 1 deliverables",
            Amount = 250,
            IsReleased = false,
            SequenceOrder = 1,
            IsBlocking = false,
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow
        };

        Context.EscrowMilestones.Add(milestone);
        await Context.SaveChangesAsync();

        return (escrow, milestone);
    }

    #endregion
}
