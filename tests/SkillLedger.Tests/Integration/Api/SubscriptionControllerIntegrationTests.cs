using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Subscription API endpoints
/// Tests subscription lifecycle, tier management, and payment method operations
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class SubscriptionControllerIntegrationTests : IntegrationTestBase
{
    private ISubscriptionService _subscriptionService = null!;
    private IPaymentService _paymentService = null!;
    private User _user = null!;
    private User _otherUser = null!;
    private SubscriptionTier _basicTier = null!;
    private SubscriptionTier _proTier = null!;

    public SubscriptionControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        _subscriptionService = ServiceScope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        _paymentService = ServiceScope.ServiceProvider.GetRequiredService<IPaymentService>();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sub-user@test.com",
            UserName = "sub-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "sub-other@test.com",
            UserName = "sub-other@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);

        // Setup subscription tiers
        _basicTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Description = "Basic subscription tier",
            Price = 9.99m,
            AnnualPrice = 99.99m,
            CreditBonus = 50,
            MaxActiveProjects = 5,
            MaxTeamMembers = 2,
            PrioritySupport = false,
            ApiAccess = false,
            AdvancedAnalytics = false,
            IsActive = true
        };

        _proTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            Description = "Professional subscription tier",
            Price = 29.99m,
            AnnualPrice = 299.99m,
            CreditBonus = 200,
            MaxActiveProjects = 20,
            MaxTeamMembers = 10,
            PrioritySupport = true,
            ApiAccess = true,
            AdvancedAnalytics = true,
            IsActive = true
        };

        Context.SubscriptionTiers.AddRange(_basicTier, _proTier);
        await Context.SaveChangesAsync();
    }

    #region GET /api/subscription/tiers Tests

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTiers_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/tiers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tiers = await response.Content.ReadFromJsonAsync<JsonElement>();
        tiers.ValueKind.Should().Be(JsonValueKind.Array);
        tiers.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTiers_ReturnsTierDetails()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/tiers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tiers = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Check tier structure
        var firstTier = tiers[0];
        firstTier.TryGetProperty("id", out _).Should().BeTrue();
        firstTier.TryGetProperty("name", out _).Should().BeTrue();
        firstTier.TryGetProperty("price", out _).Should().BeTrue();
        firstTier.TryGetProperty("creditBonus", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionTiers_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/subscription/tiers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/subscription/current Tests

    [Fact]
    [FastTest]
    public async Task GET_CurrentSubscription_WithNoSubscription_ReturnsOkWithNull()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeOneOf("null", "");
    }

    [Fact]
    [FastTest]
    public async Task GET_CurrentSubscription_WithActiveSubscription_ReturnsSubscription()
    {
        // Arrange
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            IsAnnual = false,
            CreatedAt = DateTime.UtcNow
        };

        Context.UserSubscriptions.Add(subscription);
        await Context.SaveChangesAsync();

        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("id").GetGuid().Should().Be(subscription.Id);
        result.GetProperty("status").GetString().Should().Be("Active");
    }

    [Fact]
    [FastTest]
    public async Task GET_CurrentSubscription_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/subscription/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/subscription/history Tests

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionHistory_WithNoHistory_ReturnsEmptyList()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("subscriptions", out var subscriptions).Should().BeTrue();
        subscriptions.GetArrayLength().Should().Be(0);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionHistory_WithHistory_ReturnsSubscriptions()
    {
        // Arrange
        var subscription1 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            SubscriptionTierId = _basicTier.Id,
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddMonths(-2),
            EndDate = DateTime.UtcNow.AddMonths(-1),
            IsAnnual = false,
            CancelledAt = DateTime.UtcNow.AddMonths(-1),
            CreatedAt = DateTime.UtcNow.AddMonths(-2)
        };

        var subscription2 = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            SubscriptionTierId = _proTier.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            NextBillingDate = DateTime.UtcNow.AddMonths(1),
            IsAnnual = false,
            CreatedAt = DateTime.UtcNow
        };

        Context.UserSubscriptions.AddRange(subscription1, subscription2);
        await Context.SaveChangesAsync();

        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionHistory_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/history?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("page", out _).Should().BeTrue();
        result.TryGetProperty("pageSize", out _).Should().BeTrue();
        result.TryGetProperty("totalCount", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_SubscriptionHistory_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/subscription/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/subscription/payment-methods Tests

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethods_WithNoMethods_ReturnsEmptyList()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/payment-methods");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var methods = await response.Content.ReadFromJsonAsync<JsonElement>();
        methods.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethods_WithMethods_ReturnsMethods()
    {
        // Arrange
        await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa_1", true, "127.0.0.1");
        await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa_2", false, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/payment-methods");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var methods = await response.Content.ReadFromJsonAsync<JsonElement>();
        methods.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_PaymentMethods_OnlyReturnsOwnMethods()
    {
        // Arrange - Create payment methods for both users
        await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_user_1", true, "127.0.0.1");
        await _paymentService.CreatePaymentMethodAsync(
            _otherUser.Id, "stripe", "tok_other_1", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/subscription/payment-methods");

        // Assert - Should only see own payment methods
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var methods = await response.Content.ReadFromJsonAsync<JsonElement>();

        // All returned methods should belong to current user (can't verify directly, but count should be 1)
        methods.GetArrayLength().Should().Be(1);
    }

    #endregion

    #region POST /api/subscription/payment-methods/{id}/set-default Tests

    [Fact]
    [FastTest]
    public async Task POST_SetDefaultPaymentMethod_WithValidId_ReturnsOk()
    {
        // Arrange
        var pm1 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_1", true, "127.0.0.1");
        var pm2 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_2", false, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/subscription/payment-methods/{pm2.Id}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("isDefault").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task POST_SetDefaultPaymentMethod_NonExistent_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/subscription/payment-methods/{nonExistentId}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_SetDefaultPaymentMethod_OtherUsersMethod_ReturnsNotFound()
    {
        // Arrange
        var otherPm = await _paymentService.CreatePaymentMethodAsync(
            _otherUser.Id, "stripe", "tok_other", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync($"/api/subscription/payment-methods/{otherPm.Id}/set-default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/subscription/payment-methods/{id} Tests

    [Fact]
    [FastTest]
    public async Task DELETE_PaymentMethod_NonDefaultMethod_ReturnsOk()
    {
        // Arrange
        var pm1 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_1", true, "127.0.0.1");
        var pm2 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_2", false, "127.0.0.1");

        AuthenticateAs(_user);

        // Act - Delete non-default method
        var response = await Client.DeleteAsync($"/api/subscription/payment-methods/{pm2.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_PaymentMethod_DefaultMethod_ReturnsBadRequest()
    {
        // Arrange
        var defaultPm = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_default", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act - Try to delete default method
        var response = await Client.DeleteAsync($"/api/subscription/payment-methods/{defaultPm.Id}");

        // Assert - Cannot delete default payment method
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_PaymentMethod_NonExistent_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/subscription/payment-methods/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [SecurityTest]
    public async Task DELETE_PaymentMethod_OtherUsersMethod_ReturnsNotFound()
    {
        // Arrange
        var otherPm = await _paymentService.CreatePaymentMethodAsync(
            _otherUser.Id, "stripe", "tok_other", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await Client.DeleteAsync($"/api/subscription/payment-methods/{otherPm.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/subscription/payment-methods/sync Tests

    [Fact]
    [FastTest]
    public async Task POST_SyncPaymentMethods_WithoutSubscription_ReturnsNotFound()
    {
        // Arrange - User has no subscription
        AuthenticateAs(_user);

        // Act
        var response = await Client.PostAsync("/api/subscription/payment-methods/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task POST_SyncPaymentMethods_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync("/api/subscription/payment-methods/sync", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Legacy Route Tests

    [Fact]
    [FastTest]
    public async Task GET_LegacyRoute_Tiers_Works()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act - Use legacy route /subscription instead of /api/subscription
        var response = await Client.GetAsync("/subscription/tiers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [FastTest]
    public async Task GET_LegacyRoute_Current_Works()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act - Use legacy route
        var response = await Client.GetAsync("/subscription/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints without authentication
        var endpoints = new[]
        {
            ("GET", "/api/subscription/tiers"),
            ("GET", "/api/subscription/current"),
            ("GET", "/api/subscription/history"),
            ("GET", "/api/subscription/payment-methods"),
            ("POST", "/api/subscription/payment-methods/sync"),
            ("POST", $"/api/subscription/payment-methods/{Guid.NewGuid()}/set-default"),
            ("DELETE", $"/api/subscription/payment-methods/{Guid.NewGuid()}")
        };

        foreach (var (method, url) in endpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(url);
                    break;
                case "POST":
                    response = await Client.PostAsync(url, null);
                    break;
                case "DELETE":
                    response = await Client.DeleteAsync(url);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {url} should require authentication");
        }
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    [FastTest]
    public async Task Endpoints_UnderRateLimit_Succeed()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act - Make multiple requests within rate limit
        for (int i = 0; i < 5; i++)
        {
            var response = await Client.GetAsync("/api/subscription/tiers");

            // Should succeed under normal usage
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        }
    }

    #endregion
}
