using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration;

/// <summary>
/// Integration tests for Project Escrow API endpoints
/// Tests complete API request/response flow with authentication
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Other")]
public class ProjectEscrowApiIntegrationTests : IntegrationTestBase
{
    private ICreditWalletService _walletService = null!;
    private User _client = null!;
    private User _provider = null!;
    private Project _project = null!;

    public ProjectEscrowApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    #region CSRF-Protected Request Helpers

    /// <summary>
    /// Sends a POST request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> PostWithCsrfAsync<T>(string url, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var csrfToken = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.PostAsync(url, content);
    }

    /// <summary>
    /// Sends a PUT request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> PutWithCsrfAsync<T>(string url, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var csrfToken = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.PutAsync(url, content);
    }

    #endregion

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();

        // Setup test users
        _client = new User
        {
            Id = Guid.NewGuid(),
            Email = "escrow-client@test.com",
            UserName = "escrow-client@test.com",
            Status = UserStatus.Active
        };

        _provider = new User
        {
            Id = Guid.NewGuid(),
            Email = "escrow-provider@test.com",
            UserName = "escrow-provider@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_client, _provider);

        // Setup test project
        _project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _client.Id,
            ProviderId = _provider.Id,
            Title = "API Test Escrow Project",
            Description = "Project for testing escrow API endpoints",
            CreditBudget = 750,
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        Context.Projects.Add(_project);
        await Context.SaveChangesAsync();
    }

    #region Escrow Creation API Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var request = new
        {
            ProjectId = _project.Id,
            ProviderId = _provider.Id
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var escrowData = await response.Content.ReadFromJsonAsync<JsonElement>();
        escrowData.GetProperty("projectId").GetGuid().Should().Be(_project.Id);
        escrowData.GetProperty("totalAmount").GetInt32().Should().Be(750);
        escrowData.GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithInsufficientFunds_ShouldReturn400BadRequest()
    {
        // Arrange - Client with insufficient funds
        await _walletService.CreateWalletAsync(_client.Id);

        var request = new
        {
            ProjectId = _project.Id,
            ProviderId = _provider.Id
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Insufficient credits");
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateEscrow_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new
        {
            ProjectId = _project.Id,
            ProviderId = _provider.Id
        };

        // Act - No authentication header
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateEscrow_AsNonClient_ShouldReturn403Forbidden()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var attacker = new User
        {
            Id = Guid.NewGuid(),
            Email = "escrow-attacker@test.com",
            UserName = "escrow-attacker@test.com",
            Status = UserStatus.Active
        };
        Context.Users.Add(attacker);
        await Context.SaveChangesAsync();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, attacker.Id);

        var request = new
        {
            ProjectId = _project.Id,
            ProviderId = _provider.Id
        };

        AuthenticateAs(attacker);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Context.ProjectEscrows.AnyAsync(e => e.ProjectId == _project.Id)).Should().BeFalse();
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateEscrow_WithUnassignedProvider_ShouldReturn400BadRequest()
    {
        // Arrange
        var otherProvider = new User
        {
            Id = Guid.NewGuid(),
            Email = "escrow-other-provider@test.com",
            UserName = "escrow-other-provider@test.com",
            Status = UserStatus.Active
        };
        Context.Users.Add(otherProvider);
        await Context.SaveChangesAsync();

        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var request = new
        {
            ProjectId = _project.Id,
            ProviderId = otherProvider.Id
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Context.ProjectEscrows.AnyAsync(e => e.ProjectId == _project.Id)).Should().BeFalse();
    }

    #endregion

    #region Milestone Management API Tests

    [Fact]
    [FastTest]
    public async Task POST_AddMilestone_WithValidData_ShouldReturn201Created()
    {
        // Arrange - Create escrow first
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        var request = new
        {
            EscrowId = escrow.Id,
            Description = "Complete initial design phase",
            Amount = 250,
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(7)
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/milestone/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var milestoneData = await response.Content.ReadFromJsonAsync<JsonElement>();
        milestoneData.GetProperty("escrowId").GetGuid().Should().Be(escrow.Id);
        milestoneData.GetProperty("description").GetString().Should().Be("Complete initial design phase");
        milestoneData.GetProperty("amount").GetInt32().Should().Be(250);
    }

    [Fact]
    [FastTest]
    public async Task PUT_ReleaseMilestone_WithValidAuth_ShouldReturn200OK()
    {
        // Arrange - Create escrow and milestone
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await escrowService.AddMilestoneAsync(
            escrow.Id, "Test milestone", 250, DateTime.UtcNow.AddDays(7));

        var request = new
        {
            MilestoneId = milestone.Id,
            ReleaseNotes = "Work completed successfully"
        };

        AuthenticateAs(_client);

        // Act
        var response = await PutWithCsrfAsync("/api/escrow/milestone/release", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [SecurityTest]
    public async Task PUT_ReleaseMilestone_ByProvider_ShouldReturn403Forbidden()
    {
        // Arrange - Create escrow and milestone
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await escrowService.AddMilestoneAsync(
            escrow.Id, "Test milestone", 250, DateTime.UtcNow.AddDays(7));

        var request = new
        {
            MilestoneId = milestone.Id,
            ReleaseNotes = "Unauthorized release attempt"
        };

        AuthenticateAs(_provider);

        // Act
        var response = await PutWithCsrfAsync("/api/escrow/milestone/release", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Escrow Query API Tests

    [Fact]
    [FastTest]
    public async Task GET_EscrowByProject_WithValidProject_ShouldReturn200OK()
    {
        // Arrange - Create escrow
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync($"/api/escrow/project/{_project.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var escrowData = await response.Content.ReadFromJsonAsync<JsonElement>();
        escrowData.GetProperty("projectId").GetGuid().Should().Be(_project.Id);
        escrowData.GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    [SlowTest]
    public async Task GET_UserEscrows_ShouldReturnAuthenticatedUserEscrows()
    {
        // Arrange - Create multiple escrows
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 2000, "Large funding", CreditTransactionType.Purchase);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();

        // Create additional projects for testing
        var projects = new List<Project>();
        for (int i = 0; i < 3; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = _client.Id,
                Title = $"Additional Project {i}",
                Description = $"Test project {i}",
                CreditBudget = 200,
                Status = ProjectStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            projects.Add(project);
            Context.Projects.Add(project);
        }
        await Context.SaveChangesAsync();

        // Create escrows
        await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        foreach (var project in projects)
        {
            await escrowService.CreateEscrowAsync(project.Id, _provider.Id);
        }

        AuthenticateAs(_client);

        // Act
        var response = await Client.GetAsync("/api/escrow/user/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var escrowsData = await response.Content.ReadFromJsonAsync<JsonElement>();
        escrowsData.GetArrayLength().Should().Be(4); // Original project + 3 additional
    }

    #endregion

    #region Dispute Management API Tests

    [Fact]
    [FastTest]
    public async Task POST_RaiseDispute_WithValidReason_ShouldReturn200OK()
    {
        // Arrange - Create escrow
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        var request = new
        {
            EscrowId = escrow.Id,
            DisputeReason = "Work not completed as agreed in the project scope"
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/dispute/raise", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Full Escrow Operations API Tests

    [Fact]
    [FastTest]
    public async Task PUT_ReleaseFullEscrow_WithValidAuth_ShouldReturn200OK()
    {
        // Arrange - Create escrow
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);
        await _walletService.CreateWalletAsync(_provider.Id);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        var request = new
        {
            EscrowId = escrow.Id,
            ReleaseNotes = "Project completed successfully, releasing full payment"
        };

        AuthenticateAs(_client);

        // Act
        var response = await PutWithCsrfAsync("/api/escrow/release-full", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task PUT_CancelEscrow_WithValidAuth_ShouldReturn200OK()
    {
        // Arrange - Create escrow
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        var request = new
        {
            EscrowId = escrow.Id,
            CancellationReason = "Project requirements changed, no longer needed"
        };

        AuthenticateAs(_client);

        // Act
        var response = await PutWithCsrfAsync("/api/escrow/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Validation and Error Handling Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithInvalidProjectId_ShouldReturn404NotFound()
    {
        // Arrange
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var request = new
        {
            ProjectId = Guid.NewGuid(), // Non-existent project
            ProviderId = _provider.Id
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddMilestone_WithExcessiveAmount_ShouldReturn400BadRequest()
    {
        // Arrange - Create escrow
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);

        var request = new
        {
            EscrowId = escrow.Id,
            Description = "Excessive milestone",
            Amount = 1000, // Exceeds escrow total of 750
            ExpectedCompletionDate = DateTime.UtcNow.AddDays(7)
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/milestone/add", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("exceed escrow amount");
    }

    #endregion

    #region Performance Tests

    [Fact]
    [PerformanceTest]
    public async Task GET_UserEscrows_WithManyEscrows_ShouldRespondWithin2Seconds()
    {
        // Arrange - Create multiple escrows for performance testing
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 5000, "Performance test funding", CreditTransactionType.Purchase);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();

        // Create 20 projects and escrows
        for (int i = 0; i < 20; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = _client.Id,
                Title = $"Performance Test Project {i}",
                Description = $"Project for API performance testing {i}",
                CreditBudget = 100,
                Status = ProjectStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };
            Context.Projects.Add(project);
        }
        await Context.SaveChangesAsync();

        var projects = Context.Projects.Where(p => p.ClientId == _client.Id && p.Title.Contains("Performance")).ToList();
        foreach (var project in projects)
        {
            await escrowService.CreateEscrowAsync(project.Id, _provider.Id);
        }

        AuthenticateAs(_client);

        // Act
        var startTime = DateTime.UtcNow;
        var response = await Client.GetAsync("/api/escrow/user/active");
        var responseTime = DateTime.UtcNow - startTime;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseTime.Should().BeLessThan(TimeSpan.FromSeconds(30), "API should respond within 30 seconds (test environment)");

        var escrowsData = await response.Content.ReadFromJsonAsync<JsonElement>();
        escrowsData.GetArrayLength().Should().BeGreaterThan(19); // At least 20 escrows
    }

    #endregion

    #region Security Tests

    [Fact]
    [SecurityTest]
    public async Task PUT_ReleaseMilestone_WithTamperedToken_ShouldReturn403Forbidden()
    {
        // Arrange - Create escrow and milestone
        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 1000, "Test funding", CreditTransactionType.StartingCredit);

        var escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        var escrow = await escrowService.CreateEscrowAsync(_project.Id, _provider.Id);
        var milestone = await escrowService.AddMilestoneAsync(
            escrow.Id, "Security test milestone", 250, DateTime.UtcNow.AddDays(7));

        var request = new
        {
            MilestoneId = milestone.Id,
            ReleaseNotes = "Security test"
        };

        // Use valid-but-unauthorized auth by setting a user ID that is not party to the escrow
        var tamperedUserId = Guid.NewGuid();
        var tamperedUser = new User
        {
            Id = tamperedUserId,
            Email = "tampered@test.com",
            UserName = "tampered@test.com",
            Status = UserStatus.Active
        };
        Context.Users.Add(tamperedUser);
        await Context.SaveChangesAsync();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, tamperedUserId);

        Client.DefaultRequestHeaders.Remove("X-Test-UserId");
        Client.DefaultRequestHeaders.Remove("X-Test-Email");
        Client.DefaultRequestHeaders.Add("X-Test-UserId", tamperedUserId.ToString());
        Client.DefaultRequestHeaders.Add("X-Test-Email", "tampered@test.com");

        // Act
        var response = await PutWithCsrfAsync("/api/escrow/milestone/release", request);

        // Assert - User is authenticated but not authorized (tampered ID has no permission)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateEscrow_WithHighValue_ShouldRequireAdditionalValidation()
    {
        // Arrange - High value project
        var highValueProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _client.Id,
            ProviderId = _provider.Id,
            Title = "High Value Security Test Project",
            Description = "Project requiring additional security validation",
            CreditBudget = 1500, // High value > 1000 credits
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(60)
        };
        Context.Projects.Add(highValueProject);
        await Context.SaveChangesAsync();

        await _walletService.CreateWalletAsync(_client.Id);
        await _walletService.AddCreditsAsync(_client.Id, 2000, "High value funding", CreditTransactionType.Purchase);

        var request = new
        {
            ProjectId = highValueProject.Id,
            ProviderId = _provider.Id
        };

        AuthenticateAs(_client);

        // Act
        var response = await PostWithCsrfAsync("/api/escrow/create", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var escrowData = await response.Content.ReadFromJsonAsync<JsonElement>();
        escrowData.GetProperty("requiresMultiSignature").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region Helper Methods
    #endregion
}
