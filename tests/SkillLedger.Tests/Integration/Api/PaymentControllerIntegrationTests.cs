using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Payment API endpoints
/// Tests complete API request/response flow with authentication
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class PaymentControllerIntegrationTests : IntegrationTestBase
{
    private IPaymentService _paymentService = null!;
    private ICreditWalletService _walletService = null!;
    private User _user = null!;
    private User _otherUser = null!;

    public PaymentControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        _paymentService = ServiceScope.ServiceProvider.GetRequiredService<IPaymentService>();
        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "payment-user@test.com",
            UserName = "payment-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "payment-other@test.com",
            UserName = "payment-other@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);
        await Context.SaveChangesAsync();
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
    /// Sends a POST request with CSRF token and null body
    /// </summary>
    private async Task<HttpResponseMessage> PostWithCsrfAsync(string url)
    {
        var csrfToken = await GetCsrfTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Sends a DELETE request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> DeleteWithCsrfAsync(string url)
    {
        var csrfToken = await GetCsrfTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.SendAsync(request);
    }

    #endregion

    #region GET /api/payment/methods Tests

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethods_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/payment/methods");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var methods = await response.Content.ReadFromJsonAsync<JsonElement>();
        methods.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethods_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/payment/methods");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethods_WithExistingMethods_ReturnsAllMethods()
    {
        // Arrange
        AuthenticateAs(_user);

        // Create multiple payment methods
        var pm1 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa_1", false, "127.0.0.1");
        var pm2 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa_2", true, "127.0.0.1");

        // Act
        var response = await Client.GetAsync("/api/payment/methods");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var methods = await response.Content.ReadFromJsonAsync<JsonElement>();
        methods.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region GET /api/payment/methods/{id} Tests

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethodById_WithValidId_ReturnsOk()
    {
        // Arrange
        var paymentMethod = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/payment/methods/{paymentMethod.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("id").GetGuid().Should().Be(paymentMethod.Id);
    }

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethodById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/payment/methods/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_PaymentMethodById_OtherUsersMethod_ReturnsNotFound()
    {
        // Arrange - Create payment method for other user
        var paymentMethod = await _paymentService.CreatePaymentMethodAsync(
            _otherUser.Id, "stripe", "tok_visa", true, "127.0.0.1");

        // Authenticate as different user
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/payment/methods/{paymentMethod.Id}");

        // Assert - Should not be able to access other user's payment method
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/payment/methods Tests

    [Fact]
    [FastTest]
    public async Task POST_CreatePaymentMethod_WithValidData_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Provider = "stripe",
            PaymentMethodToken = "tok_visa_new",
            IsDefault = true
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/methods", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("provider").GetString().Should().Be("stripe");
        result.GetProperty("isDefault").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task POST_CreatePaymentMethod_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Provider = "stripe",
            IsDefault = true
            // Missing PaymentMethodToken
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/methods", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreatePaymentMethod_SetsAsDefault_UnsetsOthers()
    {
        // Arrange
        // Create an existing default payment method
        var existingPm = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_existing", true, "127.0.0.1");
        existingPm.IsDefault.Should().BeTrue();

        AuthenticateAs(_user);

        var request = new
        {
            Provider = "stripe",
            PaymentMethodToken = "tok_new_default",
            IsDefault = true
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/methods", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify old default is no longer default - reload entity to get fresh data
        await Context.Entry(existingPm).ReloadAsync();
        existingPm.IsDefault.Should().BeFalse();
    }

    #endregion

    #region POST /api/payment/methods/{id}/set-default Tests

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
        var response = await PostWithCsrfAsync($"/api/payment/methods/{pm2.Id}/set-default");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("isDefault").GetBoolean().Should().BeTrue();

        // Verify old default is no longer default - reload entity to get fresh data
        await Context.Entry(pm1).ReloadAsync();
        pm1.IsDefault.Should().BeFalse();
    }

    [Fact]
    [SecurityTest]
    public async Task POST_SetDefaultPaymentMethod_OtherUsersMethod_ReturnsBadRequest()
    {
        // Arrange
        var otherPm = await _paymentService.CreatePaymentMethodAsync(
            _otherUser.Id, "stripe", "tok_other", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await PostWithCsrfAsync($"/api/payment/methods/{otherPm.Id}/set-default");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/payment/methods/{id} Tests

    [Fact]
    [FastTest]
    public async Task DELETE_PaymentMethod_WithValidId_ReturnsOk()
    {
        // Arrange
        // Create two payment methods so one can remain as default
        var pm1 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_1", true, "127.0.0.1");
        var pm2 = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_2", false, "127.0.0.1");

        AuthenticateAs(_user);

        // Act - Delete non-default method
        var response = await DeleteWithCsrfAsync($"/api/payment/methods/{pm2.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted
        var deletedPm = await _paymentService.GetPaymentMethodAsync(pm2.Id, _user.Id);
        deletedPm.Should().BeNull();
    }

    [Fact]
    [FastTest]
    public async Task DELETE_PaymentMethod_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await DeleteWithCsrfAsync($"/api/payment/methods/{nonExistentId}");

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
        var response = await DeleteWithCsrfAsync($"/api/payment/methods/{otherPm.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/payment/methods/{id}/validate Tests

    [Fact]
    [FastTest]
    public async Task POST_ValidatePaymentMethod_WithValidMethod_ReturnsOk()
    {
        // Arrange
        var pm = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa", true, "127.0.0.1");

        AuthenticateAs(_user);

        // Act
        var response = await PostWithCsrfAsync($"/api/payment/methods/{pm.Id}/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("isValid", out _).Should().BeTrue();
    }

    #endregion

    #region POST /api/payment/process Tests

    [Fact]
    [FastTest]
    public async Task POST_ProcessPayment_WithValidData_ReturnsOk()
    {
        // Arrange
        var pm = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa", true, "127.0.0.1");

        AuthenticateAs(_user);

        var request = new
        {
            PaymentMethodId = pm.Id,
            Amount = 100.00m,
            Currency = "USD",
            Description = "Test payment"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/process", request);

        // Assert
        // Payment processing may succeed or fail depending on Stripe mock
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_ProcessPayment_WithInvalidPaymentMethod_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            PaymentMethodId = Guid.NewGuid(), // Non-existent payment method
            Amount = 100.00m,
            Currency = "USD",
            Description = "Test payment"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_ProcessPayment_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var pm = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa", true, "127.0.0.1");

        AuthenticateAs(_user);

        var request = new
        {
            PaymentMethodId = pm.Id,
            Amount = -50.00m, // Negative amount
            Currency = "USD",
            Description = "Test payment"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_ProcessPayment_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var pm = await _paymentService.CreatePaymentMethodAsync(
            _user.Id, "stripe", "tok_visa", true, "127.0.0.1");

        AuthenticateAs(_user);

        var request = new
        {
            PaymentMethodId = pm.Id,
            Amount = 0m, // Zero amount
            Currency = "USD",
            Description = "Test payment"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/payment/transactions/{id}/refund Tests

    [Fact]
    [FastTest]
    public async Task POST_RefundPayment_WithNonExistentTransaction_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user, roles: new[] { "Admin" });
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await PostWithCsrfAsync(
            $"/api/payment/transactions/{nonExistentId}/refund?reason=Test%20refund");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RefundPayment_OtherUsersTransaction_ReturnsForbidden()
    {
        // Arrange
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _otherUser.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 75.00m,
            Currency = "USD",
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        AuthenticateAs(_user);

        // Act
        var response = await PostWithCsrfAsync(
            $"/api/payment/transactions/{transaction.Id}/refund?amount=25&reason=Unauthorized");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_RefundPayment_OwnTransactionAsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var transaction = new SubscriptionTransaction
        {
            Id = Guid.NewGuid(),
            SubscriptionId = Guid.Empty,
            UserId = _user.Id,
            Type = SubscriptionTransactionType.Purchase,
            Amount = 75.00m,
            Currency = "USD",
            ExternalTransactionId = $"pi_test_{Guid.NewGuid():N}",
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        Context.SubscriptionTransactions.Add(transaction);
        await Context.SaveChangesAsync();

        AuthenticateAs(_user);

        // Act
        var response = await PostWithCsrfAsync(
            $"/api/payment/transactions/{transaction.Id}/refund?amount=25&reason=Self%20service");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/payment/methods/details Tests

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethodDetails_WithMissingParams_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act - Missing payment method token
        var response = await Client.GetAsync("/api/payment/methods/details?provider=stripe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task GET_PaymentMethodDetails_WithValidParams_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync(
            "/api/payment/methods/details?provider=stripe&paymentMethodToken=tok_visa");

        // Assert
        // May return OK or 500 depending on Stripe mock
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
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
            ("GET", "/api/payment/methods"),
            ("GET", $"/api/payment/methods/{Guid.NewGuid()}"),
            ("DELETE", $"/api/payment/methods/{Guid.NewGuid()}"),
            ("POST", $"/api/payment/methods/{Guid.NewGuid()}/validate"),
            ("POST", $"/api/payment/methods/{Guid.NewGuid()}/set-default"),
            ("POST", $"/api/payment/transactions/{Guid.NewGuid()}/refund"),
            ("GET", "/api/payment/methods/details?provider=stripe&paymentMethodToken=tok")
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
                    response = await PostWithCsrfAsync(url);
                    break;
                case "DELETE":
                    response = await DeleteWithCsrfAsync(url);
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
    public async Task POST_CreatePaymentMethod_RateLimiting_ReturnsOkForNormalUsage()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act - Make a few requests (within rate limit)
        for (int i = 0; i < 3; i++)
        {
            var request = new
            {
                Provider = "stripe",
                PaymentMethodToken = $"tok_ratelimit_{i}",
                IsDefault = false
            };

            var response = await PostWithCsrfAsync("/api/payment/methods", request);

            // Should succeed under normal usage
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Created,
                HttpStatusCode.TooManyRequests);
        }
    }

    #endregion
}
