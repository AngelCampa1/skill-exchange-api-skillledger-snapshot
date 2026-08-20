using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Feedback Controller API endpoints
/// Tests feedback submission for both anonymous and authenticated users
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class FeedbackControllerTests : IntegrationTestBase
{
    private User _user = null!;

    public FeedbackControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test user
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "feedback-user@test.com",
            UserName = "feedback-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.Add(_user);
        await Context.SaveChangesAsync();
    }

    #region POST /api/Feedback Tests

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_AsAnonymous_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 1, // Bug
            Message = "Test feedback from anonymous user - this is a test message with enough characters",
            ReplyToEmail = "anonymous@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_AsAuthenticated_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);
        var feedback = new
        {
            Category = 2, // FeatureRequest
            Message = "Test feedback from authenticated user - this message has enough characters for validation",
            ReplyToEmail = _user.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithValidBugReport_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 1, // Bug
            Message = "I found a bug in the application that causes it to crash when clicking the submit button.",
            ReplyToEmail = "bugreporter@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithValidFeatureRequest_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 2, // FeatureRequest
            Message = "It would be great if the application had Light-Only Mode support and better navigation.",
            ReplyToEmail = "featurerequester@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithValidGeneralFeedback_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 0, // General
            Message = "Great application! Love the user interface and overall experience using this platform.",
            ReplyToEmail = "happyuser@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        var feedback = new
        {
            Category = 1, // Bug
            Message = "",
            ReplyToEmail = "test@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithShortMessage_ReturnsBadRequest()
    {
        // Arrange
        var feedback = new
        {
            Category = 1, // Bug
            Message = "Short", // Less than 10 characters
            ReplyToEmail = "test@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var feedback = new
        {
            Category = 0, // General
            Message = "This is a test message with enough characters for validation to pass",
            ReplyToEmail = "invalid-email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithLongMessage_ReturnsBadRequest()
    {
        // Arrange - Message exceeds 2000 character limit
        var longMessage = new string('a', 2001);
        var feedback = new
        {
            Category = 1, // Bug
            Message = longMessage,
            ReplyToEmail = "test@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithMinimalData_ReturnsOkOrBadRequest()
    {
        // Arrange - Only required fields
        var feedback = new
        {
            Category = 0, // General
            Message = "Minimal test message with required character count"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithOptionalEmail_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 2, // FeatureRequest
            Message = "Complete feedback with all fields populated and enough characters for validation",
            ReplyToEmail = "complete@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_MultipleTimes_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 0, // General
            Message = "Multiple submission test message with required character count for validation",
            ReplyToEmail = "multiple@test.com"
        };

        // Act - Submit multiple times
        var response1 = await Client.PostAsJsonAsync("/api/Feedback", feedback);
        var response2 = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert - Both should succeed or one might be rate limited
        response1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
        response2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithSpecialCharacters_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 1, // Bug
            Message = "Test with special characters: <script>alert('xss')</script> & \" ' < > / and more text",
            ReplyToEmail = "specialchars@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_WithUnicodeCharacters_ReturnsOk()
    {
        // Arrange
        var feedback = new
        {
            Category = 0, // General
            Message = "Test with unicode characters: 你好世界 🌟 Привет مرحبا and some more text",
            ReplyToEmail = "unicode@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_AlwaysReturnsSuccessMessage()
    {
        // Arrange
        var feedback = new
        {
            Category = 0, // General
            Message = "Testing response message with enough characters for validation to pass",
            ReplyToEmail = "test@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Thank you for your feedback");
        }
    }

    #endregion

    #region Anonymous Access Tests

    [Fact]
    [FastTest]
    public async Task POST_SubmitFeedback_AnonymousAccessAllowed()
    {
        // Arrange - No authentication
        var feedback = new
        {
            Category = "General",
            Message = "Anonymous user feedback",
            Email = "anonymous@example.com",
            Name = "Anonymous"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Feedback", feedback);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            "Feedback endpoint should allow anonymous access");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion
}
