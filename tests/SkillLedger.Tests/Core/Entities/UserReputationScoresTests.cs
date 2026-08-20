using SkillLedger.Core.Entities;
using SkillLedger.Tests.Infrastructure;
using Xunit;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace SkillLedger.Tests.Core.Entities;

[UnitTest]
[CoreTest]
public class UserReputationScoresTests
{
    [Fact]
    public void UserReputationScores_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var reputationScores = new UserReputationScores();

        // Assert
        Assert.NotEqual(Guid.Empty, reputationScores.Id);
        Assert.NotEqual(Guid.Empty, reputationScores.UserId);
        Assert.Equal(0.0m, reputationScores.OverallScore);
        Assert.Equal(0.0m, reputationScores.QualityScore);
        Assert.Equal(0.0m, reputationScores.CommunicationScore);
        Assert.Equal(0.0m, reputationScores.TimelinessScore);
        Assert.Equal(0.0m, reputationScores.ProfessionalismScore);
        Assert.Equal(0, reputationScores.TotalReviewsReceived);
        Assert.Equal(0, reputationScores.CompletedProjectsCount);
        Assert.Equal(0.0m, reputationScores.CompletionRate);
        Assert.Equal(0.0m, reputationScores.ResponseTimeHours);
        Assert.Equal(0, reputationScores.CurrentStreak);
        Assert.Equal(0, reputationScores.MaxStreak);
        Assert.True(reputationScores.CreatedAt <= DateTime.UtcNow);
        Assert.True(reputationScores.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void UserReputationScores_WithValidUserId_SetsUserIdCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var reputationScores = new UserReputationScores { UserId = userId };

        // Assert
        Assert.Equal(userId, reputationScores.UserId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void UserReputationScores_WithInvalidScoreRange_FailsValidation(decimal invalidScore)
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            OverallScore = invalidScore
        };

        // Act
        var validationResults = ValidateModel(reputationScores);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UserReputationScores.OverallScore)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.5)]
    [InlineData(10.0)]
    public void UserReputationScores_WithValidScoreRange_PassesValidation(decimal validScore)
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            OverallScore = validScore,
            QualityScore = validScore,
            CommunicationScore = validScore,
            TimelinessScore = validScore,
            ProfessionalismScore = validScore
        };

        // Act
        var validationResults = ValidateModel(reputationScores);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UserReputationScores_NegativeCompletionRate_FailsValidation()
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            CompletionRate = -0.1m
        };

        // Act
        var validationResults = ValidateModel(reputationScores);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UserReputationScores.CompletionRate)));
    }

    [Fact]
    public void UserReputationScores_CompletionRateOverOne_FailsValidation()
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            CompletionRate = 1.1m
        };

        // Act
        var validationResults = ValidateModel(reputationScores);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(UserReputationScores.CompletionRate)));
    }

    [Fact]
    public void UserReputationScores_CalculateScoreFromComponents_ReturnsWeightedAverage()
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            QualityScore = 8.0m,
            CommunicationScore = 7.0m,
            TimelinessScore = 9.0m,
            ProfessionalismScore = 6.0m
        };

        // Act
        var calculatedScore = reputationScores.CalculateWeightedScore();

        // Assert
        // Quality: 35% weight, Communication: 25%, Timeliness: 25%, Professionalism: 15%
        var expectedScore = (8.0m * 0.35m) + (7.0m * 0.25m) + (9.0m * 0.25m) + (6.0m * 0.15m);
        Assert.Equal(expectedScore, calculatedScore);
    }

    [Fact]
    public void UserReputationScores_IsNewUser_ReturnsTrueForZeroProjects()
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            CompletedProjectsCount = 0
        };

        // Act & Assert
        Assert.True(reputationScores.IsNewUser);
    }

    [Fact]
    public void UserReputationScores_IsNewUser_ReturnsFalseForCompletedProjects()
    {
        // Arrange
        var reputationScores = new UserReputationScores
        {
            CompletedProjectsCount = 5
        };

        // Act & Assert
        Assert.False(reputationScores.IsNewUser);
    }

    [Fact]
    public void UserReputationScores_UpdateTimestamps_UpdatesCorrectly()
    {
        // Arrange
        var reputationScores = new UserReputationScores();
        var originalCreated = reputationScores.CreatedAt;
        var originalUpdated = reputationScores.UpdatedAt;

        // Add small delay to ensure timestamp difference
        System.Threading.Thread.Sleep(10);

        // Act
        reputationScores.UpdateTimestamp();

        // Assert
        Assert.Equal(originalCreated, reputationScores.CreatedAt); // Should not change
        Assert.True(reputationScores.UpdatedAt > originalUpdated); // Should be updated
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }
}