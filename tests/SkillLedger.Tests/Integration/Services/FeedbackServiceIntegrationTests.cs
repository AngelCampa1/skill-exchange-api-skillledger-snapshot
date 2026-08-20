using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for FeedbackService - EMAIL FEEDBACK SYSTEM.
///
/// Pattern (per TDD_GUIDE.md):
/// - Uses MockEmailService (external email service - OK to mock)
/// - Tests the email construction and sending logic
/// - Verifies correct subject formatting, HTML content, and category handling
///
/// Max mocked external dependencies: 1 (Email Service)
/// </summary>
[IntegrationTest]
public class FeedbackServiceIntegrationTests
{
    private readonly Mocks.MockEmailService _emailService;
    private readonly FeedbackService _service;
    private readonly IConfiguration _configuration;

    public FeedbackServiceIntegrationTests()
    {
        _emailService = new Mocks.MockEmailService();

        var configValues = new Dictionary<string, string?>
        {
            ["FeedbackSettings:AdminEmail"] = "admin@skillledger.test",
            ["FeedbackSettings:EmailSubjectPrefix"] = "[Test Feedback]"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new LoggerFactory().CreateLogger<FeedbackService>();

        _service = new FeedbackService(
            _emailService,
            _configuration,
            logger
        );
    }

    #region SubmitFeedbackAsync Tests

    [Fact]
    public async Task SubmitFeedbackAsync_GeneralFeedback_SendsEmailWithCorrectSubject()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "This is general feedback about the platform."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, "user-123", "user@test.com");

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.ToEmail.Should().Be("admin@skillledger.test");
        sentEmail.Subject.Should().Contain("[Test Feedback]");
        sentEmail.Subject.Should().Contain("General Feedback");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_BugReport_SendsEmailWithBugSubject()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.Bug,
            Message = "I found a bug in the application."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, "user-123", "user@test.com");

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Subject.Should().Contain("Bug Report");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_FeatureRequest_SendsEmailWithFeatureSubject()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.FeatureRequest,
            Message = "It would be great to have a Light-Only Mode."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, "user-123", "user@test.com");

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Subject.Should().Contain("Feature Request");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_OtherCategory_SendsEmailWithOtherSubject()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.Other,
            Message = "Some other type of feedback."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, "user-123", "user@test.com");

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Subject.Should().Contain("Other");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_WithReplyToEmail_IncludesInEmailBody()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "Feedback with reply email.",
            ReplyToEmail = "reply@custom.com"
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, "user-123", "user@test.com");

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Body.Should().Contain("reply@custom.com");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_AuthenticatedUser_IncludesUserInfoInEmail()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "Feedback from authenticated user."
        };
        var userId = "user-abc-123";
        var userEmail = "authenticated@test.com";

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, userId, userEmail);

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Body.Should().Contain(userId);
        sentEmail.Body.Should().Contain(userEmail);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_AnonymousUser_IndicatesAnonymous()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "Feedback from anonymous user."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Body.Should().Contain("Anonymous");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_MessageContent_IsIncludedInEmail()
    {
        // Arrange
        var feedbackMessage = "This is a detailed feedback message with specific content.";
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = feedbackMessage
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Body.Should().Contain(feedbackMessage);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_HtmlEscaping_PreventsXssInMessage()
    {
        // Arrange
        var xssAttempt = "<script>alert('xss')</script>";
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = xssAttempt
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        // Should be HTML encoded
        sentEmail.Body.Should().NotContain("<script>");
        sentEmail.Body.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_HtmlEscaping_PreventsXssInReplyEmail()
    {
        // Arrange
        var xssAttempt = "<img src=x onerror=alert('xss')>";
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "Normal message.",
            ReplyToEmail = xssAttempt
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeTrue();
        _emailService.SentEmails.Should().HaveCount(1);

        var sentEmail = _emailService.SentEmails.First();
        // Should be HTML encoded
        sentEmail.Body.Should().NotContain("<img");
        sentEmail.Body.Should().Contain("&lt;img");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_EmailServiceFails_ReturnsFalse()
    {
        // Arrange
        _emailService.ShouldFail = true;

        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "This feedback will not be sent."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitFeedbackAsync_EmailServiceThrows_ReturnsFalse()
    {
        // Arrange
        _emailService.ShouldThrowException = true;

        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "This feedback will cause an exception."
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task SubmitFeedbackAsync_UsesConfiguredAdminEmail()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "Test message."
        };

        // Act
        await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        var sentEmail = _emailService.SentEmails.First();
        sentEmail.ToEmail.Should().Be("admin@skillledger.test");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_UsesConfiguredSubjectPrefix()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.Bug,
            Message = "Test message."
        };

        // Act
        await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Subject.Should().StartWith("[Test Feedback]");
    }

    [Fact]
    public void Constructor_MissingConfiguration_UsesDefaults()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();
        var logger = new LoggerFactory().CreateLogger<FeedbackService>();

        // Act
        var service = new FeedbackService(_emailService, emptyConfig, logger);

        // Assert - just verify it doesn't throw
        service.Should().NotBeNull();
    }

    #endregion

    #region Category Color Tests

    [Fact]
    public async Task SubmitFeedbackAsync_BugCategory_HasRedColorInEmail()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.Bug,
            Message = "Bug report."
        };

        // Act
        await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        var sentEmail = _emailService.SentEmails.First();
        // Bug category should have red color
        sentEmail.Body.Should().Contain("#ef4444");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_FeatureRequestCategory_HasBlueColorInEmail()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.FeatureRequest,
            Message = "Feature request."
        };

        // Act
        await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        var sentEmail = _emailService.SentEmails.First();
        // Feature request category should have blue color
        sentEmail.Body.Should().Contain("#3b82f6");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_GeneralCategory_HasGreenColorInEmail()
    {
        // Arrange
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = "General feedback."
        };

        // Act
        await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        var sentEmail = _emailService.SentEmails.First();
        // General category should have green color
        sentEmail.Body.Should().Contain("#10b981");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SubmitFeedbackAsync_VeryLongMessage_HandlesCorrectly()
    {
        // Arrange
        var longMessage = new string('x', 2000);  // Max allowed length
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = longMessage
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeTrue();
        var sentEmail = _emailService.SentEmails.First();
        sentEmail.Body.Should().Contain(longMessage);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_SpecialCharactersInMessage_HandlesCorrectly()
    {
        // Arrange
        var specialMessage = "Test with special chars: <>&\"' and unicode: \u00E9\u00F1\u00FC";
        var dto = new SubmitFeedbackDto
        {
            Category = FeedbackCategory.General,
            Message = specialMessage
        };

        // Act
        var result = await _service.SubmitFeedbackAsync(dto, null, null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitFeedbackAsync_MultipleFeedbacks_SendsMultipleEmails()
    {
        // Arrange
        var dto1 = new SubmitFeedbackDto { Category = FeedbackCategory.General, Message = "Feedback 1" };
        var dto2 = new SubmitFeedbackDto { Category = FeedbackCategory.Bug, Message = "Feedback 2" };

        // Act
        await _service.SubmitFeedbackAsync(dto1, null, null);
        await _service.SubmitFeedbackAsync(dto2, null, null);

        // Assert
        _emailService.SentEmails.Should().HaveCount(2);
    }

    #endregion
}
