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
public class CategoryReputationScoresTests
{
    [Fact]
    public void CategoryReputationScores_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var categoryScores = new CategoryReputationScores();

        // Assert
        Assert.NotEqual(Guid.Empty, categoryScores.Id);
        Assert.NotEqual(Guid.Empty, categoryScores.UserReputationScoresId);
        Assert.NotEqual(Guid.Empty, categoryScores.SkillId);
        Assert.Equal(0.0m, categoryScores.AverageScore);
        Assert.Equal(0, categoryScores.ProjectCount);
        Assert.Equal(0, categoryScores.ReviewCount);
        Assert.True(categoryScores.CreatedAt <= DateTime.UtcNow);
        Assert.True(categoryScores.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CategoryReputationScores_WithValidSkillId_SetsSkillIdCorrectly()
    {
        // Arrange
        var skillId = Guid.NewGuid();

        // Act
        var categoryScores = new CategoryReputationScores { SkillId = skillId };

        // Assert
        Assert.Equal(skillId, categoryScores.SkillId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void CategoryReputationScores_WithInvalidScoreRange_FailsValidation(decimal invalidScore)
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            AverageScore = invalidScore
        };

        // Act
        var validationResults = ValidateModel(categoryScores);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CategoryReputationScores.AverageScore)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.5)]
    [InlineData(10.0)]
    public void CategoryReputationScores_WithValidScoreRange_PassesValidation(decimal validScore)
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            AverageScore = validScore
        };

        // Act
        var validationResults = ValidateModel(categoryScores);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CategoryReputationScores_NegativeProjectCount_FailsValidation()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            ProjectCount = -1
        };

        // Act
        var validationResults = ValidateModel(categoryScores);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CategoryReputationScores.ProjectCount)));
    }

    [Fact]
    public void CategoryReputationScores_NegativeReviewCount_FailsValidation()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            ReviewCount = -1
        };

        // Act
        var validationResults = ValidateModel(categoryScores);

        // Assert
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(CategoryReputationScores.ReviewCount)));
    }

    [Fact]
    public void CategoryReputationScores_HasEnoughData_ReturnsTrueForSufficientReviews()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            ReviewCount = 5
        };

        // Act & Assert
        Assert.True(categoryScores.HasEnoughData);
    }

    [Fact]
    public void CategoryReputationScores_HasEnoughData_ReturnsFalseForInsufficientReviews()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            ReviewCount = 2
        };

        // Act & Assert
        Assert.False(categoryScores.HasEnoughData);
    }

    [Fact]
    public void CategoryReputationScores_IsExpert_ReturnsTrueForHighScoreAndProjects()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            AverageScore = 8.5m,
            ProjectCount = 10
        };

        // Act & Assert
        Assert.True(categoryScores.IsExpert);
    }

    [Fact]
    public void CategoryReputationScores_IsExpert_ReturnsFalseForLowScore()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            AverageScore = 7.0m,
            ProjectCount = 10
        };

        // Act & Assert
        Assert.False(categoryScores.IsExpert);
    }

    [Fact]
    public void CategoryReputationScores_IsExpert_ReturnsFalseForFewProjects()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores
        {
            AverageScore = 8.5m,
            ProjectCount = 3
        };

        // Act & Assert
        Assert.False(categoryScores.IsExpert);
    }

    [Fact]
    public void CategoryReputationScores_UpdateTimestamps_UpdatesCorrectly()
    {
        // Arrange
        var categoryScores = new CategoryReputationScores();
        var originalCreated = categoryScores.CreatedAt;
        var originalUpdated = categoryScores.UpdatedAt;

        // Add small delay to ensure timestamp difference
        System.Threading.Thread.Sleep(10);

        // Act
        categoryScores.UpdateTimestamp();

        // Assert
        Assert.Equal(originalCreated, categoryScores.CreatedAt); // Should not change
        Assert.True(categoryScores.UpdatedAt > originalUpdated); // Should be updated
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }
}