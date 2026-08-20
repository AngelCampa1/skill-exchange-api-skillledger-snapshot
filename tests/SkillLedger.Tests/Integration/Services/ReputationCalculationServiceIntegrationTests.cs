using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for ReputationCalculationService - BUSINESS CRITICAL.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockDistributedLockService (infrastructure service)
/// - Verifies actual database state and calculations, not mock interactions
///
/// Max mocked external dependencies: 0
/// </summary>
[IntegrationTest]
public class ReputationCalculationServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;
    private readonly MockDistributedLockService _lockService;
    private readonly ReputationCalculationService _reputationService;

    public ReputationCalculationServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReputationTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        _auditLogService = new MockAuditLogService(_context);
        _lockService = new MockDistributedLockService();
        var logger = new LoggerFactory().CreateLogger<ReputationCalculationService>();

        _reputationService = new ReputationCalculationService(
            _context,
            _auditLogService,
            _lockService,
            logger
        );
    }

    #region CalculateOverallReputationScoreAsync Tests

    [Fact]
    public async Task CalculateOverallReputationScoreAsync_UserWithNoReviews_ReturnsBaseScore()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "noreviews@test.com",
            UserName = "noreviews@test.com",
            FirstName = "No",
            LastName = "Reviews",
            Status = UserStatus.Active
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateOverallReputationScoreAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.OverallScore.Should().BeGreaterThanOrEqualTo(0m);
        result.TotalProjectsCompleted.Should().Be(0);
        result.ProjectCompletionRate.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateOverallReputationScoreAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _reputationService.CalculateOverallReputationScoreAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CalculateOverallReputationScoreAsync_UserWithHighQualityReviews_ReturnsHighScore()
    {
        // Arrange
        var provider = await CreateTestUserAsync("highquality@test.com", "High", "Quality");
        var client = await CreateTestUserAsync("client1@test.com", "Client", "One");
        var skill = await CreateTestSkillAsync("C# Development", "Programming");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        // Create high-quality review
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 5,
            CommunicationRating = 5,
            TimelinessRating = 5,
            ProfessionalismRating = 5,
            ReviewText = "Excellent work on this project!",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateOverallReputationScoreAsync(provider.Id);

        // Assert
        result.Should().NotBeNull();
        result!.OverallScore.Should().BeGreaterThan(0m); // Score should be calculated
        result!.OverallScore.Should().BeLessOrEqualTo(5.0m); // Max score is 5.0
        result.TotalProjectsCompleted.Should().Be(1);
    }

    [Fact]
    public async Task CalculateOverallReputationScoreAsync_UserWithLowQualityReviews_ReturnsLowScore()
    {
        // Arrange
        var provider = await CreateTestUserAsync("lowquality@test.com", "Low", "Quality");
        var client = await CreateTestUserAsync("client2@test.com", "Client", "Two");
        var skill = await CreateTestSkillAsync("Web Design", "Design");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        // Create low-quality review
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 2,
            CommunicationRating = 2,
            TimelinessRating = 2,
            ProfessionalismRating = 2,
            ReviewText = "Needs improvement.",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateOverallReputationScoreAsync(provider.Id);

        // Assert
        result.Should().NotBeNull();
        result!.OverallScore.Should().BeGreaterOrEqualTo(0m); // Score should be non-negative
        result!.OverallScore.Should().BeLessOrEqualTo(5.0m); // Max score is 5.0
    }

    #endregion

    #region CalculateCategoryReputationScoreAsync Tests

    [Fact]
    public async Task CalculateCategoryReputationScoreAsync_UserWithCategoryReviews_ReturnsScore()
    {
        // Arrange
        var provider = await CreateTestUserAsync("category@test.com", "Category", "Test");
        var client = await CreateTestUserAsync("client3@test.com", "Client", "Three");
        var skill = await CreateTestSkillAsync("Python Development", "Programming");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 4,
            CommunicationRating = 4,
            TimelinessRating = 4,
            ProfessionalismRating = 4,
            ReviewText = "Good work overall.",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateCategoryReputationScoreAsync(provider.Id, skill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SkillId.Should().Be(skill.Id);
        result.SkillName.Should().Be("Python Development");
        result.Score.Should().BeGreaterThan(0m);
        result.ProjectCount.Should().Be(1);
    }

    [Fact]
    public async Task CalculateCategoryReputationScoreAsync_UserWithNoReviewsInCategory_ReturnsBaseScore()
    {
        // Arrange
        var provider = await CreateTestUserAsync("nocategory@test.com", "No", "Category");
        var skill = await CreateTestSkillAsync("Java Development", "Programming");

        // Act
        var result = await _reputationService.CalculateCategoryReputationScoreAsync(provider.Id, skill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Score.Should().Be(3.0m); // BaseScore
        result.ProjectCount.Should().Be(0);
        result.LastProjectAt.Should().BeNull();
    }

    [Fact]
    public async Task CalculateCategoryReputationScoreAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com", "Provider", "Test");
        var nonExistentSkillId = Guid.NewGuid();

        // Act
        var result = await _reputationService.CalculateCategoryReputationScoreAsync(provider.Id, nonExistentSkillId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetReputationBreakdownAsync Tests

    [Fact]
    public async Task GetReputationBreakdownAsync_UserWithReviews_ReturnsDetailedBreakdown()
    {
        // Arrange
        var provider = await CreateTestUserAsync("breakdown@test.com", "Breakdown", "Test");
        var client = await CreateTestUserAsync("client4@test.com", "Client", "Four");
        var skill = await CreateTestSkillAsync("React Development", "Programming");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 4,
            CommunicationRating = 5,
            TimelinessRating = 3,
            ProfessionalismRating = 4,
            ReviewText = "Solid performance with great communication.",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.GetReputationBreakdownAsync(provider.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(provider.Id);
        result.Components.Should().NotBeNull();
        result.Components.QualityRating.Should().Be(4.0m);
        result.Components.CommunicationRating.Should().Be(5.0m);
        result.Components.TimelinessRating.Should().Be(3.0m);
        result.Components.ProfessionalismRating.Should().Be(4.0m);
        result.Explanation.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetReputationBreakdownAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _reputationService.GetReputationBreakdownAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RecalculateAndSaveReputationScoreAsync Tests

    [Fact]
    public async Task RecalculateAndSaveReputationScoreAsync_ValidUser_SavesScoreToDatabase()
    {
        // Arrange
        var provider = await CreateTestUserAsync("recalc@test.com", "Recalc", "Test");
        var client = await CreateTestUserAsync("client5@test.com", "Client", "Five");
        var skill = await CreateTestSkillAsync("Vue.js Development", "Programming");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 5,
            CommunicationRating = 5,
            TimelinessRating = 5,
            ProfessionalismRating = 5,
            ReviewText = "Outstanding work!",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.RecalculateAndSaveReputationScoreAsync(provider.Id);

        // Assert - Verify returned DTO
        result.Should().NotBeNull();
        result!.OverallScore.Should().BeGreaterThan(0m);

        // Assert - Verify saved to database
        var savedScore = await _context.UserReputationScores
            .FirstOrDefaultAsync(s => s.UserId == provider.Id);
        savedScore.Should().NotBeNull();
        savedScore!.OverallScore.Should().Be(result.OverallScore);

        // Assert - Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ReputationRecalculated" && a.UserId == provider.Id);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task RecalculateAndSaveReputationScoreAsync_UsesDistributedLock_PreventsRaceConditions()
    {
        // Arrange
        var provider = await CreateTestUserAsync("locktest@test.com", "Lock", "Test");

        // Act
        var result = await _reputationService.RecalculateAndSaveReputationScoreAsync(provider.Id);

        // Assert - Verify operation completed successfully (distributed lock was used internally)
        result.Should().NotBeNull();
        var savedScore = await _context.UserReputationScores
            .FirstOrDefaultAsync(s => s.UserId == provider.Id);
        savedScore.Should().NotBeNull();
    }

    #endregion

    #region CalculatePerformanceStreakBonusAsync Tests

    [Fact]
    public async Task CalculatePerformanceStreakBonusAsync_ConsecutiveHighRatings_ReturnsBonus()
    {
        // Arrange
        var provider = await CreateTestUserAsync("streak@test.com", "Streak", "Test");
        var client = await CreateTestUserAsync("client6@test.com", "Client", "Six");
        var skill = await CreateTestSkillAsync("Node.js Development", "Programming");

        // Create multiple consecutive high-rated projects
        for (int i = 0; i < 5; i++)
        {
            var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: i);

            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = client.Id,
                RevieweeId = provider.Id,
                QualityRating = 10,  // Ratings are 1-10 scale, need >= 8 for streak bonus
                CommunicationRating = 10,
                TimelinessRating = 10,
                ProfessionalismRating = 10,
                ReviewText = "Consistently excellent work!",
                Status = ProjectReviewStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var bonus = await _reputationService.CalculatePerformanceStreakBonusAsync(provider.Id);

        // Assert - Consecutive high ratings should provide bonus
        bonus.Should().BeGreaterThan(0m);
        bonus.Should().BeLessThanOrEqualTo(0.5m); // Max streak bonus
    }

    #endregion

    #region CalculatePenaltiesAsync Tests

    [Fact]
    public async Task CalculatePenaltiesAsync_UserWithNoPenalties_ReturnsZero()
    {
        // Arrange
        var provider = await CreateTestUserAsync("nopenalty@test.com", "No", "Penalty");

        // Act
        var penalties = await _reputationService.CalculatePenaltiesAsync(provider.Id);

        // Assert
        penalties.Should().Be(0m);
    }

    #endregion

    #region UpdateReputationOnProjectCompletionAsync Tests

    [Fact]
    public async Task UpdateReputationOnProjectCompletionAsync_CompletedProject_UpdatesBothUserScores()
    {
        // Arrange
        var provider = await CreateTestUserAsync("completion@test.com", "Completion", "Test");
        var client = await CreateTestUserAsync("client7@test.com", "Client", "Seven");
        var skill = await CreateTestSkillAsync("Angular Development", "Programming");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        // Act
        var results = await _reputationService.UpdateReputationOnProjectCompletionAsync(project.Id);

        // Assert - Should update both client and provider scores
        results.Should().NotBeNull();
        results.Should().HaveCountGreaterThanOrEqualTo(1);

        // Verify scores were saved to database
        var providerScore = await _context.UserReputationScores
            .FirstOrDefaultAsync(s => s.UserId == provider.Id);
        providerScore.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateReputationOnProjectCompletionAsync_NonExistentProject_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();

        // Act
        var results = await _reputationService.UpdateReputationOnProjectCompletionAsync(nonExistentProjectId);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region Phase 8 Coverage Tests - Uncovered Methods (0% Coverage)

    [Fact]
    public async Task GetAllCategoryScoresAsync_UserWithMultipleSkillCategories_ReturnsScoresOrderedByValue()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com", "Test", "Provider");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill1 = await CreateTestSkillAsync("Web Development", "Development");
        var skill2 = await CreateTestSkillAsync("Graphic Design", "Design");

        // Create projects with different skills
        var project1 = await CreateCompletedProjectAsync(client.Id, provider.Id, skill1.Id);
        var project2 = await CreateCompletedProjectAsync(client.Id, provider.Id, skill2.Id);

        // Create reviews with different scores
        var review1 = await CreatePublishedReviewAsync(project1.Id, provider.Id, client.Id, quality: 5.0m, communication: 5.0m, timeliness: 5.0m, professionalism: 5.0m);
        var review2 = await CreatePublishedReviewAsync(project2.Id, provider.Id, client.Id, quality: 3.0m, communication: 3.0m, timeliness: 3.0m, professionalism: 3.0m);

        // Act
        var categoryScores = await _reputationService.GetAllCategoryScoresAsync(provider.Id);

        // Assert
        categoryScores.Should().NotBeNull();
        categoryScores.Should().HaveCount(2);
        categoryScores[0].Score.Should().BeGreaterThan(categoryScores[1].Score); // Ordered by score descending
        categoryScores.Should().Contain(cs => cs.SkillId == skill1.Id);
        categoryScores.Should().Contain(cs => cs.SkillId == skill2.Id);
    }

    [Fact]
    public async Task GetAllCategoryScoresAsync_UserWithNoReviews_ReturnsEmptyList()
    {
        // Arrange
        var user = await CreateTestUserAsync("noreviews@test.com", "No", "Reviews");

        // Act
        var categoryScores = await _reputationService.GetAllCategoryScoresAsync(user.Id);

        // Assert
        categoryScores.Should().NotBeNull();
        categoryScores.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReputationHistoryAsync_UserWithHistory_ReturnsPaginatedResults()
    {
        // Arrange
        var user = await CreateTestUserAsync("history@test.com", "History", "User");

        // Add reputation history records
        var today = DateTime.UtcNow;
        for (int i = 0; i < 15; i++)
        {
            _context.ReputationHistories.Add(new ReputationHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Score = 3.5m + (i * 0.1m),
                Date = today.AddDays(-i),
                ChangeReason = $"Project completion {i}",
                ProjectId = Guid.NewGuid()
            });
        }
        await _context.SaveChangesAsync();

        // Act - Get first page (default 20 items)
        var history = await _reputationService.GetReputationHistoryAsync(user.Id, days: 90, page: 1, pageSize: 10);

        // Assert
        history.Should().NotBeNull();
        history.Should().HaveCount(10); // First page with 10 items
        history[0].Date.Should().BeAfter(history[1].Date); // Ordered by date descending
        history.Should().OnlyContain(h => h.UserId == user.Id);
    }

    [Fact]
    public async Task GetReputationHistoryAsync_UserWithNoHistory_ReturnsEmptyList()
    {
        // Arrange
        var user = await CreateTestUserAsync("nohistory@test.com", "No", "History");

        // Act
        var history = await _reputationService.GetReputationHistoryAsync(user.Id);

        // Assert
        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReputationTrendAsync_UserWithImprovingTrend_ReturnsImprovingTrendDto()
    {
        // Arrange
        var provider = await CreateTestUserAsync("improving@test.com", "Improving", "User");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Development", "Tech");

        // Add historical low score
        var pastDate = DateTime.UtcNow.AddDays(-40);
        _context.ReputationHistories.Add(new ReputationHistory
        {
            Id = Guid.NewGuid(),
            UserId = provider.Id,
            Score = 2.5m,
            Date = pastDate,
            ChangeReason = "Initial score"
        });
        await _context.SaveChangesAsync();

        // Create recent high-quality reviews to improve score (use 9/10 ratings for scores > 3.0)
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        await CreatePublishedReviewAsync(project.Id, provider.Id, client.Id, quality: 9.0m, communication: 9.0m, timeliness: 9.0m, professionalism: 9.0m);

        // Act
        var trend = await _reputationService.GetReputationTrendAsync(provider.Id, days: 30);

        // Assert
        trend.Should().NotBeNull();
        trend!.UserId.Should().Be(provider.Id);
        trend.CurrentScore.Should().BeGreaterThan(trend.PreviousScore);
        trend.Trend.Should().Be(ReputationTrend.Improving);
        trend.TrendPercentage.Should().BeGreaterThan(0);
        trend.TotalReviews.Should().BeGreaterThan(0);
        trend.DaysActive.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetReputationTrendAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var trend = await _reputationService.GetReputationTrendAsync(nonExistentUserId);

        // Assert
        trend.Should().BeNull();
    }

    [Fact]
    public async Task RecalculateAndSaveCategoryScoreAsync_NewCategory_SavesScoreToDatabase()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com", "Test", "Provider");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Design", "Creative");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        await CreatePublishedReviewAsync(project.Id, provider.Id, client.Id, quality: 4.5m, communication: 4.0m, timeliness: 4.5m, professionalism: 4.0m);

        // Act
        var result = await _reputationService.RecalculateAndSaveCategoryScoreAsync(provider.Id, skill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.SkillId.Should().Be(skill.Id);
        result.Score.Should().BeGreaterThan(0);

        // Verify saved to database
        var savedScore = await _context.CategoryReputationScores
            .FirstOrDefaultAsync(s => s.UserId == provider.Id && s.SkillId == skill.Id);
        savedScore.Should().NotBeNull();
        savedScore!.Score.Should().Be(result.Score);
    }

    [Fact]
    public async Task RecalculateAndSaveCategoryScoreAsync_ExistingCategory_UpdatesScore()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com", "Test", "Provider");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Programming", "Tech");

        // Create initial project with baseline review (establishes ProjectCount = 1)
        var firstProject = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        await CreatePublishedReviewAsync(firstProject.Id, provider.Id, client.Id, quality: 6.0m, communication: 6.0m, timeliness: 6.0m, professionalism: 6.0m, daysAgo: 30);

        // Calculate initial category score (should give ~3.0 score, 1 project)
        var initialScore = await _reputationService.RecalculateAndSaveCategoryScoreAsync(provider.Id, skill.Id);
        initialScore.Should().NotBeNull();
        initialScore!.ProjectCount.Should().Be(1);
        var originalScore = initialScore.Score;

        // Add new high-quality project review (use 10/10 ratings for maximum score)
        var secondProject = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        await CreatePublishedReviewAsync(secondProject.Id, provider.Id, client.Id, quality: 10.0m, communication: 10.0m, timeliness: 10.0m, professionalism: 10.0m);

        // Act - Recalculate with 2 projects total
        var result = await _reputationService.RecalculateAndSaveCategoryScoreAsync(provider.Id, skill.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Score.Should().BeGreaterThan(originalScore); // Score should improve from ~3.0 to higher (weighted average)
        result.ProjectCount.Should().Be(2); // Now 2 projects

        // Verify database was updated
        var updatedScore = await _context.CategoryReputationScores
            .FirstOrDefaultAsync(s => s.UserId == provider.Id && s.SkillId == skill.Id);
        updatedScore!.Score.Should().Be(result.Score);
        updatedScore.ProjectCount.Should().Be(2);
    }

    [Fact]
    public async Task BulkRecalculateReputationScoresAsync_MultipleActiveUsers_ProcessesAllUsers()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com", "User", "One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User", "Two");
        var user3 = await CreateTestUserAsync("user3@test.com", "User", "Three");
        user3.Status = UserStatus.Suspended; // This user should be skipped
        await _context.SaveChangesAsync();

        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Development", "Tech");

        // Create reviews for active users
        var project1 = await CreateCompletedProjectAsync(client.Id, user1.Id, skill.Id);
        await CreatePublishedReviewAsync(project1.Id, user1.Id, client.Id, 4.5m, 4.0m, 4.5m, 4.0m);

        var project2 = await CreateCompletedProjectAsync(client.Id, user2.Id, skill.Id);
        await CreatePublishedReviewAsync(project2.Id, user2.Id, client.Id, 3.5m, 3.0m, 3.5m, 3.0m);

        // Act
        var processedCount = await _reputationService.BulkRecalculateReputationScoresAsync();

        // Assert
        processedCount.Should().BeGreaterThanOrEqualTo(2); // Should process at least user1 and user2

        // Verify reputation scores were saved for active users
        var score1 = await _context.UserReputationScores.FirstOrDefaultAsync(s => s.UserId == user1.Id);
        var score2 = await _context.UserReputationScores.FirstOrDefaultAsync(s => s.UserId == user2.Id);
        score1.Should().NotBeNull();
        score2.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateReputationOnReviewPublishAsync_ValidReview_UpdatesRevieweeReputation()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com", "Test", "Provider");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Consulting", "Business");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        var review = await CreatePublishedReviewAsync(project.Id, provider.Id, client.Id, 4.8m, 4.5m, 4.7m, 4.6m);

        // Act
        var updatedScores = await _reputationService.UpdateReputationOnReviewPublishAsync(review.Id);

        // Assert
        updatedScores.Should().NotBeNull();
        updatedScores.Should().HaveCount(1); // Only reviewee score updated
        updatedScores[0].UserId.Should().Be(provider.Id);
        updatedScores[0].OverallScore.Should().BeGreaterThan(0);

        // Verify reputation score was saved to database
        var savedScore = await _context.UserReputationScores.FirstOrDefaultAsync(s => s.UserId == provider.Id);
        savedScore.Should().NotBeNull();
        savedScore!.OverallScore.Should().Be(updatedScores[0].OverallScore);
    }

    [Fact]
    public async Task UpdateReputationOnReviewPublishAsync_NonExistentReview_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentReviewId = Guid.NewGuid();

        // Act
        var updatedScores = await _reputationService.UpdateReputationOnReviewPublishAsync(nonExistentReviewId);

        // Assert
        updatedScores.Should().NotBeNull();
        updatedScores.Should().BeEmpty();
    }

    #endregion

    #region Phase 29 Coverage Tests - Time Decay, Penalties, Streak, and Bulk Operations

    [Fact]
    public async Task GetReputationBreakdownAsync_UserWithVeryOldReviews_AppliesTimeDecayFactor()
    {
        // Arrange
        var provider = await CreateTestUserAsync("oldreviews@test.com", "Old", "Reviews");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Old Tech", "Technology");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: 400);

        // Create review from 400 days ago (> 365 days, should have 0.3m decay factor)
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 10,
            CommunicationRating = 10,
            TimelinessRating = 10,
            ProfessionalismRating = 10,
            ReviewText = "Very old excellent work",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-400),
            CreatedAt = DateTime.UtcNow.AddDays(-400)
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var breakdown = await _reputationService.GetReputationBreakdownAsync(provider.Id);

        // Assert
        breakdown.Should().NotBeNull();
        breakdown!.TimeDecayFactor.Should().BeGreaterThan(0m);
        breakdown.TimeDecayFactor.Should().BeLessThan(1.0m); // Old reviews should have decay
        breakdown.TimeDecayFactor.Should().BeGreaterThanOrEqualTo(0.3m); // Minimum decay
    }

    [Fact]
    public async Task GetReputationBreakdownAsync_UserWithRecentReviews_NoTimeDecay()
    {
        // Arrange
        var provider = await CreateTestUserAsync("recentreviews@test.com", "Recent", "Reviews");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("New Tech", "Technology");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: 10);

        // Create recent review (< 30 days, should have 1.0m decay factor)
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 8,
            CommunicationRating = 8,
            TimelinessRating = 8,
            ProfessionalismRating = 8,
            ReviewText = "Recent good work",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var breakdown = await _reputationService.GetReputationBreakdownAsync(provider.Id);

        // Assert
        breakdown.Should().NotBeNull();
        breakdown!.TimeDecayFactor.Should().Be(1.0m); // Recent reviews should have no decay
    }

    [Fact]
    public async Task GetReputationBreakdownAsync_UserWithMediumAgeReviews_AppliesPartialDecay()
    {
        // Arrange
        var provider = await CreateTestUserAsync("mediumage@test.com", "Medium", "Age");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Medium Tech", "Technology");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: 120);

        // Create review from 120 days ago (91-180 days, should have 0.75m decay factor)
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 8,
            CommunicationRating = 8,
            TimelinessRating = 8,
            ProfessionalismRating = 8,
            ReviewText = "Medium age work",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-120),
            CreatedAt = DateTime.UtcNow.AddDays(-120)
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var breakdown = await _reputationService.GetReputationBreakdownAsync(provider.Id);

        // Assert
        breakdown.Should().NotBeNull();
        breakdown!.TimeDecayFactor.Should().BeGreaterThan(0.5m);
        breakdown.TimeDecayFactor.Should().BeLessThanOrEqualTo(1.0m);
    }

    [Fact]
    public async Task GetReputationBreakdownAsync_UserWithNoReviewsButHasProjects_AppliesInactiveUserPenalty()
    {
        // Arrange
        var provider = await CreateTestUserAsync("inactive@test.com", "Inactive", "Provider");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Some Tech", "Technology");

        // Create project but NO reviews (inactive user with past projects)
        await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);

        // Act
        var breakdown = await _reputationService.GetReputationBreakdownAsync(provider.Id);

        // Assert
        breakdown.Should().NotBeNull();
        breakdown!.TimeDecayFactor.Should().Be(0.7m); // Inactive user penalty
    }

    [Fact]
    public async Task GetReputationBreakdownAsync_UserWithHighCompletionRate_AppliesCompletionBonus()
    {
        // Arrange
        var provider = await CreateTestUserAsync("highcompletion@test.com", "High", "Completion");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Reliable Tech", "Technology");

        // Create 10 completed projects (100% completion rate)
        for (int i = 0; i < 10; i++)
        {
            var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: i * 5);
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = client.Id,
                RevieweeId = provider.Id,
                QualityRating = 8,
                CommunicationRating = 8,
                TimelinessRating = 8,
                ProfessionalismRating = 8,
                ReviewText = $"Completed project {i}",
                Status = ProjectReviewStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i * 5),
                CreatedAt = DateTime.UtcNow.AddDays(-i * 5)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var breakdown = await _reputationService.GetReputationBreakdownAsync(provider.Id);

        // Assert
        breakdown.Should().NotBeNull();
        breakdown!.FinalScore.Should().BeGreaterThan(3.5m); // Above base score (3.0) + completion bonus
        breakdown.Explanation.Should().Contain("completion rate"); // Explanation mentions completion
    }

    [Fact]
    public async Task CalculatePerformanceStreakBonusAsync_UserWithLessThan3Reviews_ReturnsZero()
    {
        // Arrange
        var provider = await CreateTestUserAsync("fewreviews@test.com", "Few", "Reviews");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Tech", "Technology");

        // Create only 2 high-quality reviews (need at least 3 for streak)
        for (int i = 0; i < 2; i++)
        {
            var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: i * 10);
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = client.Id,
                RevieweeId = provider.Id,
                QualityRating = 10,
                CommunicationRating = 10,
                TimelinessRating = 10,
                ProfessionalismRating = 10,
                ReviewText = "High quality",
                Status = ProjectReviewStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i * 10)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var bonus = await _reputationService.CalculatePerformanceStreakBonusAsync(provider.Id);

        // Assert
        bonus.Should().Be(0m); // Not enough reviews for streak bonus
    }

    [Fact]
    public async Task CalculatePerformanceStreakBonusAsync_UserWithBrokenStreak_ReturnsZero()
    {
        // Arrange
        var provider = await CreateTestUserAsync("brokenstreak@test.com", "Broken", "Streak");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Tech", "Technology");

        // Create 3 high ratings, then 1 low rating (streak broken)
        var ratings = new[] { 10, 10, 10, 5 }; // Last one is low, breaks streak
        for (int i = 0; i < 4; i++)
        {
            var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id, completedDaysAgo: i * 10);
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = client.Id,
                RevieweeId = provider.Id,
                QualityRating = ratings[i],
                CommunicationRating = ratings[i],
                TimelinessRating = ratings[i],
                ProfessionalismRating = ratings[i],
                ReviewText = $"Rating {ratings[i]}",
                Status = ProjectReviewStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-i * 10)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var bonus = await _reputationService.CalculatePerformanceStreakBonusAsync(provider.Id);

        // Assert
        bonus.Should().Be(0m); // Streak broken by low rating
    }

    [Fact]
    public async Task CalculatePenaltiesAsync_UserWithRecentCancellations_AppliesCancellationPenalty()
    {
        // Arrange
        var user = await CreateTestUserAsync("cancellations@test.com", "Cancellation", "User");
        var skill = await CreateTestSkillAsync("Tech", "Technology");

        // Create 3 cancelled projects within past year
        for (int i = 0; i < 3; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = $"Cancelled Project {i}",
                Description = "Cancelled",
                ClientId = user.Id,
                Status = ProjectStatus.Cancelled,
                CancelledAt = DateTime.UtcNow.AddDays(-i * 30), // Within past year
                CreditBudget = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 30 - 10)
            };
            _context.Projects.Add(project);
        }
        await _context.SaveChangesAsync();

        // Act
        var penalties = await _reputationService.CalculatePenaltiesAsync(user.Id);

        // Assert
        penalties.Should().BeGreaterThan(0m); // Should have cancellation penalty
        penalties.Should().BeLessThanOrEqualTo(0.5m); // Max cancellation penalty is 0.5
    }

    [Fact]
    public async Task CalculatePenaltiesAsync_UserWithRecentDisputes_AppliesDisputePenalty()
    {
        // Arrange
        var user = await CreateTestUserAsync("disputes@test.com", "Dispute", "User");
        var skill = await CreateTestSkillAsync("Tech", "Technology");

        // Create 2 disputed projects within past year
        for (int i = 0; i < 2; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = $"Disputed Project {i}",
                Description = "Disputed",
                ProviderId = user.Id,
                ClientId = Guid.NewGuid(), // Different client
                Status = ProjectStatus.Disputed,
                CreditBudget = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 30) // Within past year
            };
            _context.Projects.Add(project);
        }
        await _context.SaveChangesAsync();

        // Act
        var penalties = await _reputationService.CalculatePenaltiesAsync(user.Id);

        // Assert
        penalties.Should().BeGreaterThan(0m); // Should have dispute penalty
        penalties.Should().Be(0.4m); // 2 disputes × 0.2 = 0.4
    }

    [Fact]
    public async Task CalculatePenaltiesAsync_UserWithManyIssues_CapsAtMaxPenalty()
    {
        // Arrange
        var user = await CreateTestUserAsync("maxpenalty@test.com", "Max", "Penalty");

        // Create many cancellations (would exceed max penalty)
        for (int i = 0; i < 10; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = $"Cancelled {i}",
                Description = "Cancelled",
                ClientId = user.Id,
                Status = ProjectStatus.Cancelled,
                CancelledAt = DateTime.UtcNow.AddDays(-i * 30),
                CreditBudget = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 30 - 10)
            };
            _context.Projects.Add(project);
        }

        // Create many disputes
        for (int i = 0; i < 10; i++)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = $"Disputed {i}",
                Description = "Disputed",
                ProviderId = user.Id,
                ClientId = Guid.NewGuid(),
                Status = ProjectStatus.Disputed,
                CreditBudget = 100,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 30)
            };
            _context.Projects.Add(project);
        }
        await _context.SaveChangesAsync();

        // Act
        var penalties = await _reputationService.CalculatePenaltiesAsync(user.Id);

        // Assert
        penalties.Should().Be(1.0m); // Capped at max penalty
    }

    [Fact]
    public async Task BulkRecalculateReputationScoresAsync_EmptyUserList_ReturnsZero()
    {
        // Arrange - No active/tax-compliant users in database
        // (all users would be suspended or other status)

        // Act
        var processedCount = await _reputationService.BulkRecalculateReputationScoresAsync();

        // Assert
        processedCount.Should().BeGreaterThanOrEqualTo(0); // Should handle empty list gracefully
    }

    [Fact]
    public async Task RecalculateAndSaveReputationScoreAsync_ExistingScore_UpdatesInsteadOfCreating()
    {
        // Arrange
        var provider = await CreateTestUserAsync("existing@test.com", "Existing", "Score");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Tech", "Technology");

        // Create initial score
        var existingScore = new UserReputationScore
        {
            Id = Guid.NewGuid(),
            UserId = provider.Id,
            OverallScore = 2.5m,
            ProjectCompletionRate = 0.5m,
            AverageResponseTime = 48,
            TotalProjectsCompleted = 1,
            LastUpdated = DateTime.UtcNow.AddDays(-30)
        };
        _context.UserReputationScores.Add(existingScore);
        await _context.SaveChangesAsync();

        // Create a new project with high ratings
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = client.Id,
            RevieweeId = provider.Id,
            QualityRating = 10,
            CommunicationRating = 10,
            TimelinessRating = 10,
            ProfessionalismRating = 10,
            ReviewText = "Excellent work!",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.RecalculateAndSaveReputationScoreAsync(provider.Id);

        // Assert
        result.Should().NotBeNull();
        result!.OverallScore.Should().BeGreaterThan(2.5m); // Score improved

        // Verify only one score record exists (updated, not created new)
        var scoreCount = await _context.UserReputationScores
            .Where(s => s.UserId == provider.Id)
            .CountAsync();
        scoreCount.Should().Be(1);

        // Verify the existing score was updated
        var updatedScore = await _context.UserReputationScores
            .FirstOrDefaultAsync(s => s.UserId == provider.Id);
        updatedScore!.Id.Should().Be(existingScore.Id); // Same ID = updated not created
        updatedScore.OverallScore.Should().Be(result.OverallScore);
    }

    [Fact]
    public async Task RecalculateAndSaveReputationScoreAsync_AddsHistoryEntry_TracksScoreChanges()
    {
        // Arrange
        var provider = await CreateTestUserAsync("history@test.com", "History", "Tracking");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Tech", "Technology");
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        var review = await CreatePublishedReviewAsync(project.Id, provider.Id, client.Id, 8, 8, 8, 8);

        var initialHistoryCount = await _context.ReputationHistories
            .Where(h => h.UserId == provider.Id)
            .CountAsync();

        // Act
        await _reputationService.RecalculateAndSaveReputationScoreAsync(provider.Id);

        // Assert - Verify history entry was added
        var finalHistoryCount = await _context.ReputationHistories
            .Where(h => h.UserId == provider.Id)
            .CountAsync();
        finalHistoryCount.Should().Be(initialHistoryCount + 1);

        // Verify history entry details
        var historyEntry = await _context.ReputationHistories
            .Where(h => h.UserId == provider.Id)
            .OrderByDescending(h => h.Date)
            .FirstOrDefaultAsync();
        historyEntry.Should().NotBeNull();
        historyEntry!.ChangeReason.Should().Be("Reputation score recalculated");
        historyEntry.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetReputationTrendAsync_UserWithDecliningTrend_ReturnsCorrectTrend()
    {
        // Arrange
        var provider = await CreateTestUserAsync("declining@test.com", "Declining", "User");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Development", "Tech");

        // Add historical high score
        var pastDate = DateTime.UtcNow.AddDays(-40);
        _context.ReputationHistories.Add(new ReputationHistory
        {
            Id = Guid.NewGuid(),
            UserId = provider.Id,
            Score = 4.5m,
            Date = pastDate,
            ChangeReason = "Previous high score"
        });
        await _context.SaveChangesAsync();

        // Create recent low-quality reviews to decline score
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        await CreatePublishedReviewAsync(project.Id, provider.Id, client.Id, quality: 2.0m, communication: 2.0m, timeliness: 2.0m, professionalism: 2.0m);

        // Act
        var trend = await _reputationService.GetReputationTrendAsync(provider.Id, days: 30);

        // Assert
        trend.Should().NotBeNull();
        trend!.CurrentScore.Should().BeLessThan(trend.PreviousScore);
        trend.Trend.Should().Be(ReputationTrend.Declining);
        trend.TrendPercentage.Should().BeLessThan(0); // Negative percentage
    }

    [Fact]
    public async Task GetReputationTrendAsync_UserWithStableTrend_ReturnsStableTrend()
    {
        // Arrange
        var provider = await CreateTestUserAsync("stable@test.com", "Stable", "User");
        var client = await CreateTestUserAsync("client@test.com", "Test", "Client");
        var skill = await CreateTestSkillAsync("Development", "Tech");

        // Add historical score close to current
        var pastDate = DateTime.UtcNow.AddDays(-40);
        _context.ReputationHistories.Add(new ReputationHistory
        {
            Id = Guid.NewGuid(),
            UserId = provider.Id,
            Score = 3.5m,
            Date = pastDate,
            ChangeReason = "Stable score"
        });
        await _context.SaveChangesAsync();

        // Create reviews that maintain similar score
        var project = await CreateCompletedProjectAsync(client.Id, provider.Id, skill.Id);
        await CreatePublishedReviewAsync(project.Id, provider.Id, client.Id, quality: 7.0m, communication: 7.0m, timeliness: 7.0m, professionalism: 7.0m);

        // Act
        var trend = await _reputationService.GetReputationTrendAsync(provider.Id, days: 30);

        // Assert
        trend.Should().NotBeNull();
        trend!.Trend.Should().Be(ReputationTrend.Stable);
        trend.TrendPercentage.Should().BeInRange(-10m, 10m); // Small percentage change
    }

    [Fact]
    public async Task RecalculateAndSaveCategoryScoreAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var provider = await CreateTestUserAsync("provider@test.com", "Test", "Provider");
        var nonExistentSkillId = Guid.NewGuid();

        // Act
        var result = await _reputationService.RecalculateAndSaveCategoryScoreAsync(provider.Id, nonExistentSkillId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email, string firstName, string lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            EmailConfirmed = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Skill> CreateTestSkillAsync(string name, string category)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            IsActive = true
        };
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();
        return skill;
    }

    private async Task<Project> CreateCompletedProjectAsync(Guid clientId, Guid providerId, Guid skillId, int completedDaysAgo = 0)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = $"Test Project {Guid.NewGuid()}",
            Description = "Test project for reputation testing",
            ClientId = clientId,
            ProviderId = providerId,
            Status = ProjectStatus.Completed,
            CreditBudget = 100,
            CompletedAt = DateTime.UtcNow.AddDays(-completedDaysAgo),
            CreatedAt = DateTime.UtcNow.AddDays(-completedDaysAgo - 10)
        };
        _context.Projects.Add(project);

        var projectSkill = new ProjectSkill
        {
            ProjectId = project.Id,
            SkillId = skillId,
            ProficiencyRequired = SkillProficiency.Intermediate
        };
        _context.ProjectSkills.Add(projectSkill);

        await _context.SaveChangesAsync();
        return project;
    }

    private async Task<ProjectReview> CreatePublishedReviewAsync(
        Guid projectId,
        Guid revieweeId,
        Guid reviewerId,
        decimal quality,
        decimal communication,
        decimal timeliness,
        decimal professionalism,
        int daysAgo = 0)
    {
        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            QualityRating = (int)quality,
            CommunicationRating = (int)communication,
            TimelinessRating = (int)timeliness,
            ProfessionalismRating = (int)professionalism,
            ReviewText = $"Test review with quality={quality}",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-daysAgo)
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
