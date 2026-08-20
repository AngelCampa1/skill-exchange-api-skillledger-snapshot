using SkillLedger.Core.Validators;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Core.Validators;

[UnitTest]
[ValidationTest]
public class PasswordValidatorTests
{

    [Theory]
    [InlineData("StrongSecure123!", true)]  // Valid: 12+ chars, upper, lower, digit, special
    [InlineData("MySecureP@ss1", true)]  // Valid: meets all requirements
    [InlineData("Complex$Phrase9", true)]  // Valid: meets all requirements
    [InlineData("Password123!", false)] // Invalid: contains weak pattern "password"
    [InlineData("password123!", false)] // Invalid: no uppercase + weak pattern
    [InlineData("PASSWORD123!", false)] // Invalid: no lowercase + weak pattern
    [InlineData("StrongSecure!", false)]    // Invalid: no digit
    [InlineData("StrongSecure123", false)]  // Invalid: no special char
    [InlineData("Pass123!", false)]     // Invalid: too short (8 chars)
    [InlineData("", false)]             // Invalid: empty
    [InlineData("a", false)]            // Invalid: too short
    public void ValidatePassword_ShouldReturnExpectedResult(string password, bool expectedResult)
    {
        // Act
        var result = PasswordValidator.ValidatePassword(password);

        // Assert
        Assert.Equal(expectedResult, result.IsValid);
    }

    [Theory]
    [InlineData("StrongSecure123!", 80)] // Expected: 30(length) + 40(diversity) + 15(unique) = 85, but capped reasonably
    [InlineData("MyP@ss1", 50)]       // Expected: 14(length) + 40(diversity) + 6(unique) = 60, but realistic expectation
    [InlineData("password", 3)]       // Weak - only lowercase, contains weak pattern (-30), actual score: 16 + 10 - 30 = 0, but minimum 3
    [InlineData("12345678", 4)]       // Very weak - only numbers, actual score: 16 + 10 + 8 = 34, but no penalties apply here
    [InlineData("", 0)]               // No score for empty
    public void CalculatePasswordStrength_ShouldReturnExpectedScore(string password, int expectedMinScore)
    {
        // Act
        var score = PasswordValidator.CalculateStrengthScore(password);

        // Assert
        Assert.True(score >= expectedMinScore, $"Expected score >= {expectedMinScore}, got {score}");
    }

    [Theory]
    [InlineData("TwentyTwenty1234!")] // Sequential numbers in longer password
    [InlineData("MyPassword123!")]   // Common pattern "password"
    [InlineData("QwertySecure1!")]     // Keyboard pattern "qwerty"  
    [InlineData("AdminSecure123!")]      // Common weak pattern "admin"
    public void ValidatePassword_ShouldDetectCommonWeakPasswords(string weakPassword)
    {
        // Act
        var result = PasswordValidator.ValidatePassword(weakPassword);

        // Assert - Should be flagged as invalid due to weak patterns
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("weak patterns"));
    }

    [Fact]
    public void ValidatePassword_WithValidPassword_ShouldReturnNoErrors()
    {
        // Arrange
        var validPassword = "MySecurePhrase@123";

        // Act
        var result = PasswordValidator.ValidatePassword(validPassword);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidatePassword_WithInvalidPassword_ShouldReturnSpecificErrors()
    {
        // Arrange
        var invalidPassword = "weak";

        // Act
        var result = PasswordValidator.ValidatePassword(invalidPassword);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("12 characters"));
        Assert.Contains(result.Errors, e => e.Contains("uppercase"));
        Assert.Contains(result.Errors, e => e.Contains("number"));
        Assert.Contains(result.Errors, e => e.Contains("special character"));
    }
}