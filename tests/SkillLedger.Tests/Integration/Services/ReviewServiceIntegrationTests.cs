using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for ReviewService - BUSINESS LOGIC (blind review system).
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockContentModerationService (external AI service - OK to mock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (Content Moderation AI)
/// </summary>
[IntegrationTest]
[CoreTest]
public class ReviewServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;  // REAL (writes to DB)
    private readonly Mocks.MockContentModerationService _contentModerationService;  // EXTERNAL - OK to mock
    private readonly ReviewService _reviewService;

    private readonly Guid _testClientId = Guid.NewGuid();
    private readonly Guid _testProviderId = Guid.NewGuid();
    private readonly Guid _testProjectId = Guid.NewGuid();

    public ReviewServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReviewServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal service
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!

        // Setup EXTERNAL service (OK to mock)
        _contentModerationService = new Mocks.MockContentModerationService();

        var logger = new LoggerFactory().CreateLogger<ReviewService>();

        _reviewService = new ReviewService(
            _context,
            _auditLogService,
            _contentModerationService,
            logger);

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

        _context.Users.AddRange(client, provider);
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

        // Assert - Verify review in database
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ReviewId.Should().NotBeNull();
        result.Status.Should().Be(ProjectReviewStatus.SubmittedBlind);

        var review = await _context.ProjectReviews.FindAsync(result.ReviewId);
        review.Should().NotBeNull();
        review!.Status.Should().Be(ProjectReviewStatus.SubmittedBlind);
        review.SubmittedAt.Should().NotBeNull();
        review.SubmittedFromIP.Should().Be("192.168.1.1");
        review.OverallRating.Should().Be(8);
        review.ReviewText.Should().Be(createDto.ReviewText);
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

        // Verify only one review exists
        var reviewCount = await _context.ProjectReviews
            .CountAsync(r => r.ProjectId == _testProjectId && r.ReviewerId == _testClientId);
        reviewCount.Should().Be(1);
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

        // Verify no review was created
        var reviewExists = await _context.ProjectReviews
            .AnyAsync(r => r.ReviewerId == _testClientId && r.RevieweeId == _testClientId);
        reviewExists.Should().BeFalse();
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

        // Assert - Verify database state
        result.Should().BeTrue();

        var updatedClientReview = await _context.ProjectReviews.FindAsync(clientReview.Id);
        var updatedProviderReview = await _context.ProjectReviews.FindAsync(providerReview.Id);

        updatedClientReview!.Status.Should().Be(ProjectReviewStatus.Published);
        updatedClientReview.PublishedAt.Should().NotBeNull();
        updatedClientReview.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        updatedProviderReview!.Status.Should().Be(ProjectReviewStatus.Published);
        updatedProviderReview.PublishedAt.Should().NotBeNull();
        updatedProviderReview.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

        // Assert - Verify review remains blind
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

        // Assert - Verify only published reviews returned
        reviews.Should().HaveCount(1);
        reviews.First().Status.Should().Be(ProjectReviewStatus.Published);
        reviews.First().Id.Should().Be(publishedReview.Id);
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

        // Assert - Verify database state
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

        // Verify review remains published
        var updatedReview = await _context.ProjectReviews.FindAsync(review.Id);
        updatedReview!.Status.Should().Be(ProjectReviewStatus.Published);
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

        // Assert - Verify calculated statistics
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

        // Assert - Verify database state
        result.Success.Should().BeTrue();

        var updatedReview = await _context.ProjectReviews.FindAsync(review.Id);
        updatedReview!.ResponseText.Should().Be(responseDto.ResponseText);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
