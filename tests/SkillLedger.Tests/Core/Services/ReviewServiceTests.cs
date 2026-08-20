using SkillLedger.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;

namespace SkillLedger.Tests.Core.Services;

[UnitTest]
[CoreTest]
public class ReviewServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IContentModerationService> _mockContentModerationService;
    private readonly Mock<ILogger<ReviewService>> _mockLogger;
    private readonly ReviewService _reviewService;

    private readonly Guid _testClientId = Guid.NewGuid();
    private readonly Guid _testProviderId = Guid.NewGuid();
    private readonly Guid _testThirdPartyId = Guid.NewGuid();
    private readonly Guid _testProjectId = Guid.NewGuid();

    public ReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SkillLedgerDbContext(options);
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockContentModerationService = new Mock<IContentModerationService>();
        _mockLogger = new Mock<ILogger<ReviewService>>();

        _reviewService = new ReviewService(
            _context,
            _mockAuditLogService.Object,
            _mockContentModerationService.Object,
            _mockLogger.Object);

        SetupTestData();
    }

    private void SetupTestData()
    {
        // Create test users
        var client = new User
        {
            Id = _testClientId,
            Email = "client@test.com",
            UserName = "client@test.com",
            Status = UserStatus.Active
        };

        var provider = new User
        {
            Id = _testProviderId,
            Email = "provider@test.com",
            UserName = "provider@test.com",
            Status = UserStatus.Active
        };

        var thirdParty = new User
        {
            Id = _testThirdPartyId,
            Email = "thirdparty@test.com",
            UserName = "thirdparty@test.com",
            Status = UserStatus.Active
        };

        // Create test project
        var project = new Project
        {
            Id = _testProjectId,
            ClientId = _testClientId,
            ProviderId = _testProviderId,
            Title = "Test Project",
            Description = "Test project description",
            Status = ProjectStatus.Completed,
            CreditBudget = 500
        };

        _context.Users.AddRange(client, provider, thirdParty);
        _context.Projects.Add(project);
        _context.SaveChanges();
    }

    [Fact]
    public async Task SubmitReviewAsync_ValidReview_ShouldCreateReviewInBlindState()
    {
        // Arrange
        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            QualityRating = 9,
            CommunicationRating = 7,
            TimelinessRating = 8,
            ProfessionalismRating = 8,
            ReviewText = "This is a comprehensive review that meets all requirements for length and content."
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testClientId, "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ReviewId.Should().NotBeNull();
        result.Status.Should().Be(ProjectReviewStatus.SubmittedBlind);

        var review = await _context.ProjectReviews.FindAsync(result.ReviewId);
        review.Should().NotBeNull();
        review!.Status.Should().Be(ProjectReviewStatus.SubmittedBlind);
        review.SubmittedAt.Should().NotBeNull();
        review.SubmittedFromIP.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task SubmitReviewAsync_AttackerNotProjectParticipant_ShouldReturnErrorAndNotCreateReview()
    {
        // Arrange
        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Third party forged review attempt should be blocked by counterparty validation."
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testThirdPartyId, "192.168.1.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Only project participants");

        var reviewExists = await _context.ProjectReviews
            .AnyAsync(r => r.ProjectId == _testProjectId && r.ReviewerId == _testThirdPartyId);
        reviewExists.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReviewAsync_WrongReviewee_ShouldReturnErrorAndNotCreateReview()
    {
        // Arrange
        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testThirdPartyId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Client attempting to review someone other than the project provider."
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testClientId, "192.168.1.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("counterparty");

        var reviewExists = await _context.ProjectReviews
            .AnyAsync(r => r.ProjectId == _testProjectId && r.RevieweeId == _testThirdPartyId);
        reviewExists.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReviewAsync_WrongReviewTypeForReviewerRole_ShouldReturnErrorAndNotCreateReview()
    {
        // Arrange
        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ProviderToClient,
            OverallRating = 8,
            ReviewText = "Client attempting to use provider-to-client review type should be blocked."
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testClientId, "192.168.1.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("counterparty");

        var reviewExists = await _context.ProjectReviews
            .AnyAsync(r => r.ProjectId == _testProjectId && r.ReviewerId == _testClientId);
        reviewExists.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReviewAsync_ValidProviderToClientCounterpartyReview_ShouldCreateReview()
    {
        // Arrange
        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testClientId,
            Type = ProjectReviewType.ProviderToClient,
            OverallRating = 9,
            ReviewText = "Provider reviewing the exact client counterparty for the completed project."
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testProviderId, "192.168.1.1");

        // Assert
        result.Success.Should().BeTrue();
        result.ReviewId.Should().NotBeNull();

        var review = await _context.ProjectReviews.FindAsync(result.ReviewId);
        review.Should().NotBeNull();
        review!.ReviewerId.Should().Be(_testProviderId);
        review.RevieweeId.Should().Be(_testClientId);
        review.Type.Should().Be(ProjectReviewType.ProviderToClient);
    }

    [Fact]
    public async Task SubmitReviewAsync_DuplicateReview_ShouldReturnError()
    {
        // Arrange
        var existingReview = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 5,
            ReviewText = "Existing review text that meets requirements"
        };
        _context.ProjectReviews.Add(existingReview);
        await _context.SaveChangesAsync();

        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Duplicate review attempt with sufficient length"
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testClientId, "192.168.1.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already submitted");
        result.ReviewId.Should().BeNull();
    }

    [Fact]
    public async Task SubmitReviewAsync_SelfReview_ShouldReturnError()
    {
        // Arrange
        var createDto = new CreateReviewDto
        {
            ProjectId = _testProjectId,
            RevieweeId = _testClientId, // Same as reviewer
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Self-review attempt that should be blocked by business rules"
        };

        // Act
        var result = await _reviewService.SubmitReviewAsync(createDto, _testClientId, "192.168.1.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot review yourself");
    }

    [Fact]
    public async Task ProcessBlindReviewsAsync_BothPartiesSubmitted_ShouldPublishReviews()
    {
        // Arrange - Create both reviews in SubmittedBlind state
        var clientReview = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Client review with sufficient length for validation",
            Status = ProjectReviewStatus.SubmittedBlind,
            SubmittedAt = DateTime.UtcNow
        };

        var providerReview = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testProviderId,
            RevieweeId = _testClientId,
            Type = ProjectReviewType.ProviderToClient,
            OverallRating = 9,
            ReviewText = "Provider review with sufficient length for validation requirements",
            Status = ProjectReviewStatus.SubmittedBlind,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProjectReviews.AddRange(clientReview, providerReview);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.ProcessBlindReviewsAsync(_testProjectId);

        // Assert
        result.Should().BeTrue();

        var updatedClientReview = await _context.ProjectReviews.FindAsync(clientReview.Id);
        var updatedProviderReview = await _context.ProjectReviews.FindAsync(providerReview.Id);

        updatedClientReview!.Status.Should().Be(ProjectReviewStatus.Published);
        updatedClientReview.PublishedAt.Should().NotBeNull();

        updatedProviderReview!.Status.Should().Be(ProjectReviewStatus.Published);
        updatedProviderReview.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessBlindReviewsAsync_OnlyOnePartySubmitted_ShouldNotPublish()
    {
        // Arrange - Only client submitted review
        var clientReview = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Single review submission with adequate length",
            Status = ProjectReviewStatus.SubmittedBlind,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProjectReviews.Add(clientReview);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.ProcessBlindReviewsAsync(_testProjectId);

        // Assert
        result.Should().BeFalse();

        var updatedReview = await _context.ProjectReviews.FindAsync(clientReview.Id);
        updatedReview!.Status.Should().Be(ProjectReviewStatus.SubmittedBlind);
        updatedReview.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetProjectReviewsAsync_OnlyPublishedReviews_ShouldReturnVisibleReviews()
    {
        // Arrange
        var publishedReview = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Published review visible to all users who can access it",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };

        var blindReview = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testProviderId,
            RevieweeId = _testClientId,
            Type = ProjectReviewType.ProviderToClient,
            OverallRating = 9,
            ReviewText = "Blind review that should not be visible until published",
            Status = ProjectReviewStatus.SubmittedBlind
        };

        _context.ProjectReviews.AddRange(publishedReview, blindReview);
        await _context.SaveChangesAsync();

        // Act
        var reviews = await _reviewService.GetProjectReviewsAsync(_testProjectId, _testClientId);

        // Assert
        reviews.Should().HaveCount(1);
        reviews.First().Status.Should().Be(ProjectReviewStatus.Published);
    }

    [Fact]
    public async Task RetractReviewAsync_ValidRetraction_ShouldRetractBlindReview()
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Review submitted in blind state that can be retracted",
            Status = ProjectReviewStatus.SubmittedBlind,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.RetractReviewAsync(review.Id, _testClientId, "192.168.1.1");

        // Assert
        result.Success.Should().BeTrue();
        result.Status.Should().Be(ProjectReviewStatus.Retracted);

        var updatedReview = await _context.ProjectReviews.FindAsync(review.Id);
        updatedReview!.Status.Should().Be(ProjectReviewStatus.Retracted);
    }

    [Fact]
    public async Task RetractReviewAsync_PublishedReview_ShouldReturnError()
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Published review that cannot be retracted anymore",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };

        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reviewService.RetractReviewAsync(review.Id, _testClientId, "192.168.1.1");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot be retracted");
    }

    [Fact]
    public async Task GetUserReviewSummaryAsync_ValidUser_ShouldReturnAccurateSummary()
    {
        // Arrange
        var reviews = new List<ProjectReview>
        {
            new ProjectReview
            {
                ProjectId = _testProjectId,
                ReviewerId = _testClientId,
                RevieweeId = _testProviderId,
                Type = ProjectReviewType.ClientToProvider,
                OverallRating = 8,
                QualityRating = 9,
                CommunicationRating = 7,
                ReviewText = "First review for calculating summary statistics",
                Status = ProjectReviewStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-10)
            },
            new ProjectReview
            {
                ProjectId = Guid.NewGuid(),
                ReviewerId = Guid.NewGuid(),
                RevieweeId = _testProviderId,
                Type = ProjectReviewType.ClientToProvider,
                OverallRating = 6,
                QualityRating = 5,
                CommunicationRating = 8,
                ReviewText = "Second review for summary calculation accuracy",
                Status = ProjectReviewStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _context.ProjectReviews.AddRange(reviews);
        await _context.SaveChangesAsync();

        // Act
        var summary = await _reviewService.GetUserReviewSummaryAsync(_testProviderId);

        // Assert
        summary.Should().NotBeNull();
        summary!.TotalReviewsReceived.Should().Be(2);
        summary.AverageOverallRating.Should().Be(7.0); // (8 + 6) / 2
        summary.AverageQualityRating.Should().Be(7.0); // (9 + 5) / 2
        summary.AverageCommunicationRating.Should().Be(7.5); // (7 + 8) / 2
        summary.MostRecentReviewDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(-5), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CanSubmitReviewAsync_ValidScenario_ShouldReturnTrue()
    {
        // Act
        var canSubmit = await _reviewService.CanSubmitReviewAsync(
            _testProjectId,
            _testClientId,
            _testProviderId,
            ProjectReviewType.ClientToProvider);

        // Assert
        canSubmit.Should().BeTrue();
    }

    [Fact]
    public async Task CanSubmitReviewAsync_SelfReview_ShouldReturnFalse()
    {
        // Act
        var canSubmit = await _reviewService.CanSubmitReviewAsync(
            _testProjectId,
            _testClientId,
            _testClientId, // Same as reviewer
            ProjectReviewType.ClientToProvider);

        // Assert
        canSubmit.Should().BeFalse();
    }

    [Fact]
    public async Task AddReviewResponseAsync_ValidResponse_ShouldAddResponse()
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = _testProjectId,
            ReviewerId = _testClientId,
            RevieweeId = _testProviderId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            ReviewText = "Original review text that meets minimum requirements",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };

        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        var responseDto = new AddReviewResponseDto
        {
            ReviewId = review.Id,
            ResponseText = "Thank you for the feedback, it was a pleasure working with you!"
        };

        // Act
        var result = await _reviewService.AddReviewResponseAsync(responseDto, _testProviderId, "192.168.1.1");

        // Assert
        result.Success.Should().BeTrue();

        var updatedReview = await _context.ProjectReviews.FindAsync(review.Id);
        updatedReview!.ResponseText.Should().Be(responseDto.ResponseText);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
