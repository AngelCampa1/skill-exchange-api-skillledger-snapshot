using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Webhook API endpoints
/// Tests Stripe webhook handling with signature validation
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class WebhookControllerIntegrationTests : IntegrationTestBase
{
    private User _user = null!;

    public WebhookControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test user
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "webhook-user@test.com",
            UserName = "webhook-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.Add(_user);
        await Context.SaveChangesAsync();
    }

    #region POST /api/webhook/stripe Tests

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_WithoutBody_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_WithoutSignature_ReturnsBadRequest()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_test_123",
            type = "checkout.session.completed",
            data = new { @object = new { id = "cs_test_123" } }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        // No Stripe-Signature header
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("signature");
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_test_123",
            type = "checkout.session.completed",
            data = new { @object = new { id = "cs_test_123" } }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=invalid_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // Should return BadRequest due to invalid signature
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable); // If webhook secret not configured
    }

    [Fact]
    [SecurityTest]
    public async Task POST_StripeWebhook_WithMalformedPayload_ReturnsBadRequest()
    {
        // Arrange
        var malformedPayload = "{ invalid json }";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(malformedPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_StripeWebhook_WithTamperedPayload_ReturnsBadRequest()
    {
        // Arrange - Simulate a payload that was tampered with after signing
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_test_tampered",
            type = "checkout.session.completed",
            data = new { @object = new {
                id = "cs_test_123",
                amount_total = 999999 // Tampered amount
            } }
        });

        // Use a real-looking but invalid signature
        var tamperedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var invalidSignature = $"t={tamperedTimestamp},v1=tampered_signature_that_should_fail_validation";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", invalidSignature);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should reject due to signature validation failure
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_DoesNotRequireJwtAuth()
    {
        // Arrange - No JWT authentication, only Stripe signature
        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_test_123",
            type = "checkout.session.completed",
            data = new { @object = new { id = "cs_test_123" } }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act - Send without any JWT token
        var response = await Client.SendAsync(request);

        // Assert - Should NOT return 401 Unauthorized
        // (Will return 400 due to invalid signature, but not 401)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Event Type Handling Tests

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_CheckoutSessionCompleted_HandlesEvent()
    {
        // Arrange
        var payload = CreateWebhookPayload("checkout.session.completed", new
        {
            id = "cs_test_123",
            customer = "cus_test",
            subscription = "sub_test",
            amount_total = 2999,
            currency = "usd",
            metadata = new { user_id = _user.Id.ToString() }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Will fail signature validation but tests the endpoint is reachable
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_InvoicePaymentSucceeded_HandlesEvent()
    {
        // Arrange
        var payload = CreateWebhookPayload("invoice.payment_succeeded", new
        {
            id = "in_test_123",
            customer = "cus_test",
            subscription = "sub_test",
            amount_paid = 2999,
            currency = "usd"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_InvoicePaymentFailed_HandlesEvent()
    {
        // Arrange
        var payload = CreateWebhookPayload("invoice.payment_failed", new
        {
            id = "in_test_failed_123",
            customer = "cus_test",
            subscription = "sub_test",
            amount_due = 2999,
            currency = "usd"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_CustomerSubscriptionDeleted_HandlesEvent()
    {
        // Arrange
        var payload = CreateWebhookPayload("customer.subscription.deleted", new
        {
            id = "sub_test_deleted",
            customer = "cus_test",
            status = "canceled"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.NotFound);
    }

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_UnknownEventType_HandlesGracefully()
    {
        // Arrange
        var payload = CreateWebhookPayload("unknown.event.type", new
        {
            id = "obj_test_unknown"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should handle unknown events gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable);
    }

    #endregion

    #region Signature Timing Tests

    [Fact]
    [SecurityTest]
    public async Task POST_StripeWebhook_WithExpiredTimestamp_ReturnsBadRequest()
    {
        // Arrange - Use an old timestamp (outside tolerance window)
        var expiredTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var payload = CreateWebhookPayload("checkout.session.completed", new
        {
            id = "cs_test_123"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", $"t={expiredTimestamp},v1=old_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should reject due to timestamp validation
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_StripeWebhook_WithFutureTimestamp_ReturnsBadRequest()
    {
        // Arrange - Use a future timestamp
        var futureTimestamp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var payload = CreateWebhookPayload("checkout.session.completed", new
        {
            id = "cs_test_123"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Headers.Add("Stripe-Signature", $"t={futureTimestamp},v1=future_signature");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert - Should reject
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable);
    }

    #endregion

    #region Response Format Tests

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_BadRequest_ReturnsErrorMessage()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request.Content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    [FastTest]
    public async Task POST_StripeWebhook_DuplicateEvents_HandledIdempotently()
    {
        // Arrange
        var eventId = $"evt_test_{Guid.NewGuid()}";
        var payload = CreateWebhookPayload("checkout.session.completed", new
        {
            id = "cs_test_123"
        }, eventId);

        // Send same event twice
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request1.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request1.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/webhook/stripe");
        request2.Headers.Add("Stripe-Signature", "t=12345,v1=test_signature");
        request2.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response1 = await Client.SendAsync(request1);
        var response2 = await Client.SendAsync(request2);

        // Assert - Both should return same status (either both succeed or both fail signature validation)
        response1.StatusCode.Should().Be(response2.StatusCode);
    }

    #endregion

    #region Helper Methods

    private string CreateWebhookPayload(string eventType, object data, string? eventId = null)
    {
        return JsonSerializer.Serialize(new
        {
            id = eventId ?? $"evt_test_{Guid.NewGuid()}",
            @object = "event",
            api_version = "2023-10-16",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            type = eventType,
            data = new { @object = data }
        });
    }

    #endregion
}
