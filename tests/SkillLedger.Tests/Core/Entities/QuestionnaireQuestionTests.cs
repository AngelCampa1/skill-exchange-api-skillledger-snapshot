using SkillLedger.Core.Entities;
using Xunit;

namespace SkillLedger.Tests.Core.Entities;

public class QuestionnaireQuestionTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaultValues()
    {
        // Act
        var question = new QuestionnaireQuestion();

        // Assert
        Assert.NotEqual(Guid.Empty, question.Id);
        Assert.Equal(string.Empty, question.QuestionText);
        Assert.Null(question.Description);
        Assert.Equal(QuestionType.Text, question.Type);
        Assert.False(question.IsRequired);
        Assert.Equal(0, question.DisplayOrder);
        Assert.Null(question.Configuration);
        Assert.Null(question.DefaultValue);
        Assert.Null(question.PlaceholderText);
        Assert.Null(question.ValidationRegex);
        Assert.Null(question.ValidationMessage);
        Assert.Null(question.MinValue);
        Assert.Null(question.MaxValue);
        Assert.True(question.IsActive);
        Assert.NotNull(question.Options);
        Assert.Empty(question.Options);
        Assert.NotNull(question.Responses);
        Assert.Empty(question.Responses);
    }

    [Theory]
    [InlineData(QuestionType.MultipleChoice, true)]
    [InlineData(QuestionType.Dropdown, true)]
    [InlineData(QuestionType.Checkbox, true)]
    [InlineData(QuestionType.Radio, true)]
    [InlineData(QuestionType.Text, false)]
    [InlineData(QuestionType.Number, false)]
    [InlineData(QuestionType.Email, false)]
    [InlineData(QuestionType.Boolean, false)]
    public void SupportsOptions_ShouldReturnCorrectValue(QuestionType type, bool expectedSupportsOptions)
    {
        // Arrange
        var question = new QuestionnaireQuestion { Type = type };

        // Act
        var result = question.SupportsOptions();

        // Assert
        Assert.Equal(expectedSupportsOptions, result);
    }

    [Theory]
    [InlineData(QuestionType.Checkbox, true)]
    [InlineData(QuestionType.MultipleChoice, false)]
    [InlineData(QuestionType.Dropdown, false)]
    [InlineData(QuestionType.Radio, false)]
    [InlineData(QuestionType.Text, false)]
    public void AllowsMultipleSelections_ShouldReturnCorrectValue(QuestionType type, bool expectedAllowsMultiple)
    {
        // Arrange
        var question = new QuestionnaireQuestion { Type = type };

        // Act
        var result = question.AllowsMultipleSelections();

        // Assert
        Assert.Equal(expectedAllowsMultiple, result);
    }

    [Theory]
    [InlineData(true, null, false, "This field is required.")]
    [InlineData(true, "", false, "This field is required.")]
    [InlineData(true, "   ", false, "This field is required.")]
    [InlineData(true, "Valid input", true, null)]
    [InlineData(false, null, true, null)]
    [InlineData(false, "", true, null)]
    public void ValidateResponse_RequiredField_ShouldValidateCorrectly(bool isRequired, string? response, bool expectedValid, string? expectedError)
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = QuestionType.Text,
            IsRequired = isRequired
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse(response);

        // Assert
        Assert.Equal(expectedValid, isValid);
        Assert.Equal(expectedError, errorMessage);
    }

    [Theory]
    [InlineData(QuestionType.Text, 5, 10, "abc", false, "Minimum length is 5 characters.")]
    [InlineData(QuestionType.Text, 5, 10, "abcdef", true, null)]
    [InlineData(QuestionType.Text, 5, 10, "abcdefghijk", false, "Maximum length is 10 characters.")]
    [InlineData(QuestionType.LongText, 0, 100, "Valid text", true, null)]
    public void ValidateResponse_TextLength_ShouldValidateCorrectly(QuestionType type, int minValue, int maxValue, string response, bool expectedValid, string? expectedError)
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = type,
            MinValue = minValue,
            MaxValue = maxValue,
            IsRequired = false
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse(response);

        // Assert
        Assert.Equal(expectedValid, isValid);
        Assert.Equal(expectedError, errorMessage);
    }

    [Theory]
    [InlineData("5", 10, 20, false, "Minimum value is 10.")]
    [InlineData("15", 10, 20, true, null)]
    [InlineData("25", 10, 20, false, "Maximum value is 20.")]
    [InlineData("abc", null, null, false, "Please enter a valid number.")]
    public void ValidateResponse_NumberConstraints_ShouldValidateCorrectly(string response, int? minValue, int? maxValue, bool expectedValid, string? expectedError)
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = QuestionType.Number,
            MinValue = minValue,
            MaxValue = maxValue,
            IsRequired = false
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse(response);

        // Assert
        Assert.Equal(expectedValid, isValid);
        Assert.Equal(expectedError, errorMessage);
    }

    [Theory]
    [InlineData(@"^\d{3}-\d{3}-\d{4}$", "123-456-7890", true, null)]
    [InlineData(@"^\d{3}-\d{3}-\d{4}$", "1234567890", false, "Invalid format.")]
    [InlineData(@"^[A-Za-z]+$", "OnlyLetters", true, null)]
    [InlineData(@"^[A-Za-z]+$", "Letters123", false, "Custom error message")]
    public void ValidateResponse_RegexValidation_ShouldValidateCorrectly(string regex, string response, bool expectedValid, string? validationMessage)
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = QuestionType.Text,
            ValidationRegex = regex,
            ValidationMessage = validationMessage,
            IsRequired = false
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse(response);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid && validationMessage != null)
        {
            Assert.Equal(validationMessage, errorMessage);
        }
    }

    [Fact]
    public void ValidateResponse_InvalidRegex_ShouldSkipValidation()
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = QuestionType.Text,
            ValidationRegex = "[", // Invalid regex
            IsRequired = false
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse("any text");

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(QuestionType.Text)]
    [InlineData(QuestionType.LongText)]
    [InlineData(QuestionType.Number)]
    [InlineData(QuestionType.Email)]
    [InlineData(QuestionType.Phone)]
    [InlineData(QuestionType.Date)]
    [InlineData(QuestionType.Time)]
    [InlineData(QuestionType.DateTime)]
    [InlineData(QuestionType.Boolean)]
    [InlineData(QuestionType.Radio)]
    [InlineData(QuestionType.Checkbox)]
    [InlineData(QuestionType.Dropdown)]
    [InlineData(QuestionType.MultipleChoice)]
    [InlineData(QuestionType.Rating)]
    [InlineData(QuestionType.FileUpload)]
    [InlineData(QuestionType.Url)]
    public void Type_ShouldAcceptAllValidTypes(QuestionType type)
    {
        // Arrange & Act
        var question = new QuestionnaireQuestion { Type = type };

        // Assert
        Assert.Equal(type, question.Type);
    }

    [Fact]
    public void Properties_ShouldAcceptValidValues()
    {
        // Arrange
        var questionnaireId = Guid.NewGuid();
        const string questionText = "What is your name?";
        const string description = "Please provide your full name";
        const string configuration = "{\"style\":\"modern\"}";
        const string defaultValue = "Default answer";
        const string placeholderText = "Enter your answer here";
        const string validationRegex = @"^[A-Za-z\s]+$";
        const string validationMessage = "Only letters and spaces allowed";
        const int displayOrder = 5;
        const int minValue = 1;
        const int maxValue = 100;

        // Act
        var question = new QuestionnaireQuestion
        {
            QuestionnaireId = questionnaireId,
            QuestionText = questionText,
            Description = description,
            Type = QuestionType.Text,
            IsRequired = true,
            DisplayOrder = displayOrder,
            Configuration = configuration,
            DefaultValue = defaultValue,
            PlaceholderText = placeholderText,
            ValidationRegex = validationRegex,
            ValidationMessage = validationMessage,
            MinValue = minValue,
            MaxValue = maxValue,
            IsActive = false
        };

        // Assert
        Assert.Equal(questionnaireId, question.QuestionnaireId);
        Assert.Equal(questionText, question.QuestionText);
        Assert.Equal(description, question.Description);
        Assert.Equal(QuestionType.Text, question.Type);
        Assert.True(question.IsRequired);
        Assert.Equal(displayOrder, question.DisplayOrder);
        Assert.Equal(configuration, question.Configuration);
        Assert.Equal(defaultValue, question.DefaultValue);
        Assert.Equal(placeholderText, question.PlaceholderText);
        Assert.Equal(validationRegex, question.ValidationRegex);
        Assert.Equal(validationMessage, question.ValidationMessage);
        Assert.Equal(minValue, question.MinValue);
        Assert.Equal(maxValue, question.MaxValue);
        Assert.False(question.IsActive);
    }

    [Fact]
    public void ValidateResponse_EmptyNonRequired_ShouldReturnValid()
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = QuestionType.Text,
            IsRequired = false,
            MinValue = 5 // This should be ignored for empty values
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse("");

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateResponse_NullNonRequired_ShouldReturnValid()
    {
        // Arrange
        var question = new QuestionnaireQuestion
        {
            Type = QuestionType.Number,
            IsRequired = false,
            MinValue = 10 // This should be ignored for null values
        };

        // Act
        var (isValid, errorMessage) = question.ValidateResponse(null);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}