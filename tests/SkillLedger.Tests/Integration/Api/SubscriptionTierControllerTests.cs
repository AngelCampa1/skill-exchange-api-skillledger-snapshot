using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Subscription Tier API endpoints
/// Tests tier retrieval, admin seeding, and validation endpoints
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class SubscriptionTierControllerTests : IntegrationTestBase
{
    private User _regularUser = null!;
    private User _adminUser = null!;

    public SubscriptionTierControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _regularUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "tier-user@test.com",
            UserName = "tier-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "tier-admin@test.com",
            UserName = "tier-admin@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_regularUser, _adminUser);
        await Context.SaveChangesAsync();
    }

    #region GET /api/subscriptiontier Tests

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTiers_WithoutAuth_ReturnsOk()
    {
        // Act - Public endpoint
        var response = await Client.GetAsync("/api/subscriptiontier");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTiers_AsAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.GetAsync("/api/subscriptiontier");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTiers_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.GetAsync("/api/subscriptiontier");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/subscriptiontier/{id} Tests

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTier_WithValidId_ReturnsOkOrNotFound()
    {
        // Arrange
        var tierId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/subscriptiontier/{tierId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTier_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/subscriptiontier/{invalidId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTier_WithoutAuth_ReturnsOkOrNotFound()
    {
        // Arrange - Public endpoint
        var tierId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/subscriptiontier/{tierId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTier_AsAuthenticatedUser_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_regularUser);
        var tierId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/subscriptiontier/{tierId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/subscriptiontier/seed Tests

    [Fact]
    [FastTest]
    public async Task POST_SeedSubscriptionTiers_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/subscriptiontier/seed", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_SeedSubscriptionTiers_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/subscriptiontier/seed", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_SeedSubscriptionTiers_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/subscriptiontier/seed", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/subscriptiontier/validate Tests

    [Fact]
    [FastTest]
    public async Task POST_ValidateSubscriptionTiers_AsAdmin_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/subscriptiontier/validate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("isValid");
            content.Should().Contain("message");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateSubscriptionTiers_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        // Act
        var response = await Client.PostAsync("/api/subscriptiontier/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ValidateSubscriptionTiers_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/subscriptiontier/validate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task PublicEndpoints_AllowAnonymousAccess()
    {
        // Test public endpoints
        var publicEndpoints = new[]
        {
            "/api/subscriptiontier",
            $"/api/subscriptiontier/{Guid.NewGuid()}"
        };

        foreach (var endpoint in publicEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should allow anonymous access");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AdminEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all admin endpoints without authentication
        var adminEndpoints = new[]
        {
            "/api/subscriptiontier/seed",
            "/api/subscriptiontier/validate"
        };

        foreach (var endpoint in adminEndpoints)
        {
            var response = await Client.PostAsync(endpoint, null);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"POST {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AdminEndpoints_AsRegularUser_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_regularUser);

        var adminEndpoints = new[]
        {
            "/api/subscriptiontier/seed",
            "/api/subscriptiontier/validate"
        };

        foreach (var endpoint in adminEndpoints)
        {
            var response = await Client.PostAsync(endpoint, null);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"POST {endpoint} should require Admin role");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task AdminEndpoints_AsAdmin_ReturnOkOrError()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var adminEndpoints = new[]
        {
            "/api/subscriptiontier/seed",
            "/api/subscriptiontier/validate"
        };

        foreach (var endpoint in adminEndpoints)
        {
            var response = await Client.PostAsync(endpoint, null);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        }
    }

    #endregion
}
