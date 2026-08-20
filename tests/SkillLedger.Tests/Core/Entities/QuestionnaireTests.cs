using SkillLedger.Core.Entities;
using Xunit;

namespace SkillLedger.Tests.Core.Entities;

public class QuestionnaireTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        // Act
        var questionnaire = new Questionnaire();

        // Assert
        Assert.NotEqual(Guid.Empty, questionnaire.Id);
        Assert.Equal(string.Empty, questionnaire.Title);
        Assert.Null(questionnaire.Description);
        Assert.Equal(QuestionnaireType.General, questionnaire.Type);
        Assert.True(questionnaire.IsActive);
        Assert.False(questionnaire.IsTemplate);
        Assert.False(questionnaire.RequiresReview);
        Assert.Null(questionnaire.MaxResponses);
        Assert.Null(questionnaire.StartDate);
        Assert.Null(questionnaire.EndDate);
        Assert.Equal(1, questionnaire.Version);
        Assert.Null(questionnaire.Metadata);
        Assert.NotNull(questionnaire.Questions);
        Assert.Empty(questionnaire.Questions);
        Assert.NotNull(questionnaire.Responses);
        Assert.Empty(questionnaire.Responses);
    }

    [Theory]
    [InlineData(true, null, null, null, true)]    // Active, no date/response constraints
    [InlineData(false, null, null, null, false)]  // Inactive
    [InlineData(true, 1, null, null, false)]      // Start date in future (positive offset)
    [InlineData(true, null, -1, null, false)]     // End date in past
    [InlineData(true, null, null, 0, false)]      // Max responses reached
    [InlineData(true, -1, 1, null, true)]         // Within date range
    public void IsAvailable_ShouldReturnCorrectValue(bool isActive, int? startDateDaysOffset, int? endDateDaysOffset, int? maxResponses, bool expectedAvailable)
    {
        // Arrange
        var questionnaire = new Questionnaire
        {
            IsActive = isActive,
            StartDate = startDateDaysOffset.HasValue ? DateTime.UtcNow.AddDays(startDateDaysOffset.Value) : null,
            EndDate = endDateDaysOffset.HasValue ? DateTime.UtcNow.AddDays(endDateDaysOffset.Value) : null,
            MaxResponses = maxResponses
        };

        // Add mock responses if needed to test max responses
        if (maxResponses.HasValue && maxResponses.Value <= 0)
        {
            for (int i = 0; i < Math.Abs(maxResponses.Value) + 1; i++)
            {
                questionnaire.Responses.Add(new QuestionnaireResponse());
            }
        }

        // Act
        var result = questionnaire.IsAvailable();

        // Assert
        Assert.Equal(expectedAvailable, result);
    }

    [Fact]
    public void GetQuestionCount_WithNoQuestions_ShouldReturnZero()
    {
        // Arrange
        var questionnaire = new Questionnaire();

        // Act
        var count = questionnaire.GetQuestionCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetQuestionCount_WithQuestions_ShouldReturnCorrectCount()
    {
        // Arrange
        var questionnaire = new Questionnaire();
        questionnaire.Questions.Add(new QuestionnaireQuestion());
        questionnaire.Questions.Add(new QuestionnaireQuestion());

        // Act
        var count = questionnaire.GetQuestionCount();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void GetResponseCount_WithNoResponses_ShouldReturnZero()
    {
        // Arrange
        var questionnaire = new Questionnaire();

        // Act
        var count = questionnaire.GetResponseCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetResponseCount_WithResponses_ShouldReturnCorrectCount()
    {
        // Arrange
        var questionnaire = new Questionnaire();
        questionnaire.Responses.Add(new QuestionnaireResponse());
        questionnaire.Responses.Add(new QuestionnaireResponse());
        questionnaire.Responses.Add(new QuestionnaireResponse());

        // Act
        var count = questionnaire.GetResponseCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Theory]
    [InlineData(QuestionnaireType.General)]
    [InlineData(QuestionnaireType.ProjectIntake)]
    [InlineData(QuestionnaireType.ClientOnboarding)]
    [InlineData(QuestionnaireType.ProviderVetting)]
    [InlineData(QuestionnaireType.ProjectFeedback)]
    [InlineData(QuestionnaireType.SkillAssessment)]
    [InlineData(QuestionnaireType.MarketResearch)]
    public void Type_ShouldAcceptAllValidTypes(QuestionnaireType type)
    {
        // Arrange & Act
        var questionnaire = new Questionnaire { Type = type };

        // Assert
        Assert.Equal(type, questionnaire.Type);
    }

    [Fact]
    public void Properties_ShouldAcceptValidValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(30);
        const string title = "Test Questionnaire";
        const string description = "Test Description";
        const string metadata = "{\"key\":\"value\"}";
        const int maxResponses = 100;
        const int version = 2;

        // Act
        var questionnaire = new Questionnaire
        {
            Title = title,
            Description = description,
            CreatedByUserId = userId,
            Type = QuestionnaireType.ProjectIntake,
            IsActive = false,
            IsTemplate = true,
            RequiresReview = true,
            MaxResponses = maxResponses,
            StartDate = startDate,
            EndDate = endDate,
            Version = version,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(title, questionnaire.Title);
        Assert.Equal(description, questionnaire.Description);
        Assert.Equal(userId, questionnaire.CreatedByUserId);
        Assert.Equal(QuestionnaireType.ProjectIntake, questionnaire.Type);
        Assert.False(questionnaire.IsActive);
        Assert.True(questionnaire.IsTemplate);
        Assert.True(questionnaire.RequiresReview);
        Assert.Equal(maxResponses, questionnaire.MaxResponses);
        Assert.Equal(startDate, questionnaire.StartDate);
        Assert.Equal(endDate, questionnaire.EndDate);
        Assert.Equal(version, questionnaire.Version);
        Assert.Equal(metadata, questionnaire.Metadata);
    }

    [Fact]
    public void IsAvailable_WithMaxResponsesReached_ShouldReturnFalse()
    {
        // Arrange
        var questionnaire = new Questionnaire
        {
            IsActive = true,
            MaxResponses = 2
        };

        // Add exactly max responses
        questionnaire.Responses.Add(new QuestionnaireResponse());
        questionnaire.Responses.Add(new QuestionnaireResponse());

        // Act
        var result = questionnaire.IsAvailable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAvailable_WithMaxResponsesNotReached_ShouldReturnTrue()
    {
        // Arrange
        var questionnaire = new Questionnaire
        {
            IsActive = true,
            MaxResponses = 3
        };

        // Add fewer than max responses
        questionnaire.Responses.Add(new QuestionnaireResponse());
        questionnaire.Responses.Add(new QuestionnaireResponse());

        // Act
        var result = questionnaire.IsAvailable();

        // Assert
        Assert.True(result);
    }
}