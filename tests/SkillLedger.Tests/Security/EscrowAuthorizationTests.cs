using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SkillLedger.Tests.Security;

/// <summary>
/// Integration tests for escrow controller authorization (VULN-001, VULN-002, VULN-003, VULN-004)
/// Verifies that admin-only endpoints properly reject unauthorized access
/// </summary>
[Collection("Integration Security")]
public class EscrowAuthorizationTests
{
    private readonly SharedWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EscrowAuthorizationTests(SharedTestHostFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    #region VULN-001: ResolveDispute Authorization

    [Fact]
    public async Task ResolveDispute_WithoutAuth_Returns401Unauthorized()
    {
        // Arrange
        var request = new
        {
            EscrowId = Guid.NewGuid(),
            ResolutionAction = "Release funds to provider",
            ResolutionNotes = "Test resolution"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/escrow/dispute/resolve", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResolveDispute_WithNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsNonAdmin(client);

        var request = new
        {
            EscrowId = Guid.NewGuid(),
            ResolutionAction = "Release funds to provider",
            ResolutionNotes = "Test resolution"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/escrow/dispute/resolve", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResolveDispute_WithAdminUser_ReturnsSuccessOrBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsAdmin(client);

        var request = new
        {
            EscrowId = Guid.NewGuid(),
            ResolutionAction = "Release funds to provider",
            ResolutionNotes = "Test resolution"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/escrow/dispute/resolve", request);

        // Assert
        // Should not be 401 or 403 - should be 400 (bad request) or 200 (success) depending on escrow existence
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region VULN-002: FreezeEscrow and UnfreezeEscrow Authorization

    [Fact]
    public async Task FreezeEscrow_WithoutAuth_Returns401Unauthorized()
    {
        // Arrange
        var escrowId = Guid.NewGuid();
        var request = new { FreezeReason = "Test freeze reason for security" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/escrow/{escrowId}/freeze", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FreezeEscrow_WithNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsNonAdmin(client);

        var escrowId = Guid.NewGuid();
        var request = new { FreezeReason = "Test freeze reason for security" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/escrow/{escrowId}/freeze", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnfreezeEscrow_WithoutAuth_Returns401Unauthorized()
    {
        // Arrange
        var escrowId = Guid.NewGuid();

        // Act
        var response = await _client.PutAsync($"/api/escrow/{escrowId}/unfreeze", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnfreezeEscrow_WithNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsNonAdmin(client);

        var escrowId = Guid.NewGuid();

        // Act
        var response = await client.PutAsync($"/api/escrow/{escrowId}/unfreeze", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region VULN-003: GetSystemMetrics Authorization

    [Fact]
    public async Task GetSystemMetrics_WithoutAuth_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/escrow/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSystemMetrics_WithNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsNonAdmin(client);

        // Act
        var response = await client.GetAsync("/api/escrow/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSystemMetrics_WithAdminUser_Returns200OK()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsAdmin(client);

        // Act
        var response = await client.GetAsync("/api/escrow/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region VULN-004: GetDisputedEscrows Authorization

    [Fact]
    public async Task GetDisputedEscrows_WithoutAuth_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/escrow/disputes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDisputedEscrows_WithNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsNonAdmin(client);

        // Act
        var response = await client.GetAsync("/api/escrow/disputes");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDisputedEscrows_WithAdminUser_Returns200OK()
    {
        // Arrange
        var client = _factory.CreateClient();
        AuthenticateAsAdmin(client);

        // Act
        var response = await client.GetAsync("/api/escrow/disputes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Authenticate client as a non-admin user using TestAuthenticationHandler headers
    /// </summary>
    private void AuthenticateAsNonAdmin(HttpClient client)
    {
        var userId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", "testuser@skillledger.app");
        // No roles = non-admin user
    }

    /// <summary>
    /// Authenticate client as an admin user using TestAuthenticationHandler headers
    /// </summary>
    private void AuthenticateAsAdmin(HttpClient client)
    {
        var userId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", "admin@skillledger.app");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
    }

    #endregion
}
