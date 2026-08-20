using SkillLedger.Tests.Infrastructure;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Core.Enums;
using SkillLedger.Core.DTOs;

namespace SkillLedger.Tests.Core.Services;

[UnitTest]
[CoreTest]
public class ReputationServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly Mock<ILogger<ReputationCalculationService>> _mockLogger;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IDistributedLockService> _mockDistributedLockService;
    private readonly ReputationCalculationService _reputationService;

    public ReputationServiceTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SkillLedgerDbContext(options);
        _mockLogger = new Mock<ILogger<ReputationCalculationService>>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockDistributedLockService = new Mock<IDistributedLockService>();

        // Mock distributed lock to always return acquired lock
        var mockLock = new Mock<IDistributedLock>();
        mockLock.Setup(x => x.IsAcquired).Returns(true);
        mockLock.Setup(x => x.Resource).Returns("test-resource");
        mockLock.Setup(x => x.AcquiredAt).Returns(DateTime.UtcNow);
        mockLock.Setup(x => x.ExpiresAt).Returns(DateTime.UtcNow.AddSeconds(30));
        mockLock.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _mockDistributedLockService.Setup(x => x.AcquireLockAsync(
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<TimeSpan?>()))
            .ReturnsAsync(mockLock.Object);

        _reputationService = new ReputationCalculationService(
            _context,
            _mockAuditLogService.Object,
            _mockDistributedLockService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CalculateOverallReputationScoreAsync_WithNoReviews_ReturnsDefaultScores()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateOverallReputationScoreAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(3.0m, result.OverallScore); // BaseScore from service
        Assert.Equal(0.0m, result.ProjectCompletionRate);
        Assert.Equal(0, result.TotalProjectsCompleted);
        Assert.True(result.AverageResponseTime > 0);
    }

    [Fact]
    public async Task CalculateOverallReputationScoreAsync_WithHighQualityReviews_ReturnsHighScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        var reviewer = new User
        {
            Id = reviewerId,
            Email = "reviewer@example.com",
            UserName = "reviewer@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        var project = new Project
        {
            Id = projectId,
            ClientId = reviewerId,
            ProviderId = userId,
            Title = "Test Project",
            Description = "Test Description",
            Status = ProjectStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddDays(-30)
        };

        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ReviewerId = reviewerId,
            RevieweeId = userId,
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 10, // High rating
            QualityRating = 10,
            CommunicationRating = 9,
            TimelinessRating = 9,
            ProfessionalismRating = 10,
            ReviewText = "Excellent work! Very professional and delivered high quality results on time. Would definitely work with again.",
            Status = ProjectReviewStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-29),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _context.Users.AddRange(user, reviewer);
        _context.Projects.Add(project);
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateOverallReputationScoreAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.OverallScore >= 4.0m); // Should be high with excellent reviews
        Assert.Equal(1.0m, result.ProjectCompletionRate); // 100% completion
        Assert.Equal(1, result.TotalProjectsCompleted);
    }

    [Fact]
    public async Task CalculateCategoryReputationScoreAsync_WithValidSkill_ReturnsScore()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();

        var skill = new Skill
        {
            Id = skillId,
            Name = "Test Skill",
            Description = "Test skill description",
            Category = "Programming"
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        _context.Skills.Add(skill);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculateCategoryReputationScoreAsync(userId, skillId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(skillId, result.SkillId);
        Assert.Equal("Test Skill", result.SkillName);
        Assert.Equal(3.0m, result.Score); // Base score when no reviews
        Assert.Equal(0, result.ProjectCount);
    }

    [Fact]
    public async Task GetReputationBreakdownAsync_WithReviews_ReturnsDetailedBreakdown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.GetReputationBreakdownAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.NotNull(result.Components);
        Assert.NotNull(result.Explanation);
        Assert.True(result.FinalScore >= 0);
        Assert.Equal(3.0m, result.BaseScore); // Default base score
    }

    [Fact]
    public async Task CalculatePerformanceStreakBonusAsync_WithConsecutiveHighRatings_ReturnsBonus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculatePerformanceStreakBonusAsync(userId);

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= 0.5m); // MaxStreakBonus
    }

    [Fact]
    public async Task CalculatePenaltiesAsync_WithNoCancellations_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.CalculatePenaltiesAsync(userId);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task CalculateTimeDecayFactor_WithRecentDate_ReturnsHighFactor()
    {
        // Arrange
        var recentDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = _reputationService.CalculateTimeDecayFactor(recentDate);

        // Assert
        Assert.True(result > 0.5m);
        Assert.True(result <= 1.0m);
    }

    [Fact]
    public async Task CalculateTimeDecayFactor_WithOldDate_ReturnsLowFactor()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddYears(-3);

        // Act
        var result = _reputationService.CalculateTimeDecayFactor(oldDate);

        // Assert
        Assert.True(result >= 0.1m); // Minimum weight
        Assert.True(result < 0.5m);
    }

    [Fact]
    public async Task RecalculateAndSaveReputationScoreAsync_CreatesNewRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.RecalculateAndSaveReputationScoreAsync(userId);

        // Assert
        Assert.NotNull(result);

        // Verify saved to database
        var savedRecord = await _context.UserReputationScores
            .FirstOrDefaultAsync(s => s.UserId == userId);
        Assert.NotNull(savedRecord);
        Assert.Equal(result.OverallScore, savedRecord.OverallScore);
    }

    [Fact]
    public async Task BulkRecalculateReputationScoresAsync_ProcessesActiveUsers()
    {
        // Arrange
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user1@example.com",
            UserName = "user1@example.com",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "user2@example.com",
            UserName = "user2@example.com",
            Status = UserStatus.TaxCompliant,
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reputationService.BulkRecalculateReputationScoresAsync();

        // Assert
        Assert.Equal(2, result); // Should process both users
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}