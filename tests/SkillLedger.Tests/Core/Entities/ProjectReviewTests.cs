using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Tests.Core.Entities;

[UnitTest]
[CoreTest]
public class ProjectReviewTests
{
    [Fact]
    public void ProjectReview_Constructor_SetsDefaultValues()
    {
        // Act
        var review = new ProjectReview();

        // Assert
        review.Id.Should().NotBeEmpty();
        review.Status.Should().Be(ProjectReviewStatus.Pending);
        review.ModerationStatus.Should().Be(ModerationStatus.Pending);
        review.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        review.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void ProjectReview_OverallRating_ValidValues_ShouldBeAccepted(int rating)
    {
        // Arrange
        var review = new ProjectReview();

        // Act
        review.OverallRating = rating;

        // Assert
        review.OverallRating.Should().Be(rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void ProjectReview_OverallRating_InvalidValues_ShouldFailValidation(int rating)
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = Guid.NewGuid(),
            ReviewerId = Guid.NewGuid(),
            RevieweeId = Guid.NewGuid(),
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = rating,
            ReviewText = "Valid review text that meets minimum requirements"
        };

        var context = new ValidationContext(review);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(review, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ProjectReview.OverallRating)));
    }

    [Fact]
    public void ProjectReview_ReviewText_TooShort_ShouldFailValidation()
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = Guid.NewGuid(),
            ReviewerId = Guid.NewGuid(),
            RevieweeId = Guid.NewGuid(),
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 5,
            ReviewText = "Short" // Less than minimum 25 characters
        };

        var context = new ValidationContext(review);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(review, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ProjectReview.ReviewText)));
    }

    [Fact]
    public void ProjectReview_ReviewText_TooLong_ShouldFailValidation()
    {
        // Arrange
        var longText = new string('A', 2001); // Exceeds max length
        var review = new ProjectReview
        {
            ProjectId = Guid.NewGuid(),
            ReviewerId = Guid.NewGuid(),
            RevieweeId = Guid.NewGuid(),
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 5,
            ReviewText = longText
        };

        var context = new ValidationContext(review);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(review, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ProjectReview.ReviewText)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void ProjectReview_DimensionalRatings_ValidValues_ShouldBeAccepted(int rating)
    {
        // Arrange
        var review = new ProjectReview();

        // Act
        review.QualityRating = rating;
        review.CommunicationRating = rating;
        review.TimelinessRating = rating;
        review.ProfessionalismRating = rating;

        // Assert
        review.QualityRating.Should().Be(rating);
        review.CommunicationRating.Should().Be(rating);
        review.TimelinessRating.Should().Be(rating);
        review.ProfessionalismRating.Should().Be(rating);
    }

    [Fact]
    public void ProjectReview_CalculatedAverageRating_ShouldBeCorrect()
    {
        // Arrange
        var review = new ProjectReview
        {
            QualityRating = 8,
            CommunicationRating = 6,
            TimelinessRating = 10,
            ProfessionalismRating = 4
        };

        // Act & Assert
        review.CalculatedAverageRating.Should().Be(7.0);
    }

    [Fact]
    public void ProjectReview_IsEditable_PendingStatus_ShouldBeTrue()
    {
        // Arrange
        var review = new ProjectReview
        {
            Status = ProjectReviewStatus.Pending
        };

        // Act & Assert
        review.IsEditable.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectReviewStatus.SubmittedBlind)]
    [InlineData(ProjectReviewStatus.Published)]
    [InlineData(ProjectReviewStatus.UnderModeration)]
    [InlineData(ProjectReviewStatus.Rejected)]
    public void ProjectReview_IsEditable_NonPendingStatus_ShouldBeFalse(ProjectReviewStatus status)
    {
        // Arrange
        var review = new ProjectReview
        {
            Status = status
        };

        // Act & Assert
        review.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void ProjectReview_CanBeRetracted_SubmittedBlindStatus_ShouldBeTrue()
    {
        // Arrange
        var review = new ProjectReview
        {
            Status = ProjectReviewStatus.SubmittedBlind
        };

        // Act & Assert
        review.CanBeRetracted.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectReviewStatus.Pending)]
    [InlineData(ProjectReviewStatus.Published)]
    [InlineData(ProjectReviewStatus.UnderModeration)]
    [InlineData(ProjectReviewStatus.Rejected)]
    [InlineData(ProjectReviewStatus.Retracted)]
    public void ProjectReview_CanBeRetracted_NonSubmittedBlindStatus_ShouldBeFalse(ProjectReviewStatus status)
    {
        // Arrange
        var review = new ProjectReview
        {
            Status = status
        };

        // Act & Assert
        review.CanBeRetracted.Should().BeFalse();
    }

    [Fact]
    public void ProjectReview_IsVisible_PublishedStatus_ShouldBeTrue()
    {
        // Arrange
        var review = new ProjectReview
        {
            Status = ProjectReviewStatus.Published
        };

        // Act & Assert
        review.IsVisible.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProjectReviewStatus.Pending)]
    [InlineData(ProjectReviewStatus.SubmittedBlind)]
    [InlineData(ProjectReviewStatus.UnderModeration)]
    [InlineData(ProjectReviewStatus.Rejected)]
    [InlineData(ProjectReviewStatus.Retracted)]
    public void ProjectReview_IsVisible_NonPublishedStatus_ShouldBeFalse(ProjectReviewStatus status)
    {
        // Arrange
        var review = new ProjectReview
        {
            Status = status
        };

        // Act & Assert
        review.IsVisible.Should().BeFalse();
    }

    [Fact]
    public void ProjectReview_RequiredFields_MissingValues_ShouldFailValidation()
    {
        // Arrange
        var review = new ProjectReview
        {
            // ProjectId, ReviewerId, RevieweeId will be default Guids (empty)
            // ReviewText is null which should fail
            // OverallRating is 0 which should fail (must be 1-10)
        };

        var context = new ValidationContext(review);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(review, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ProjectReview.OverallRating)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(ProjectReview.ReviewText)));
    }

    [Fact]
    public void ProjectReview_SelfReview_SameReviewerAndReviewee_ShouldFailBusinessRule()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var review = new ProjectReview
        {
            ProjectId = Guid.NewGuid(),
            ReviewerId = userId,
            RevieweeId = userId, // Same as reviewer
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 5,
            ReviewText = "This should not be allowed as self-review"
        };

        // Act & Assert
        review.IsSelfReview.Should().BeTrue();
        // Business rule validation should prevent this scenario
    }

    [Fact]
    public void ProjectReview_ValidReview_ShouldPassValidation()
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = Guid.NewGuid(),
            ReviewerId = Guid.NewGuid(),
            RevieweeId = Guid.NewGuid(),
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 8,
            QualityRating = 9,
            CommunicationRating = 7,
            TimelinessRating = 8,
            ProfessionalismRating = 8,
            ReviewText = "This is a comprehensive review that meets all minimum requirements for text length and provides meaningful feedback."
        };

        var context = new ValidationContext(review);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(review, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void ProjectReview_ResponseText_OptionalField_ShouldAcceptNullOrEmpty()
    {
        // Arrange
        var review = new ProjectReview
        {
            ProjectId = Guid.NewGuid(),
            ReviewerId = Guid.NewGuid(),
            RevieweeId = Guid.NewGuid(),
            Type = ProjectReviewType.ClientToProvider,
            OverallRating = 5,
            ReviewText = "Valid review text that meets requirements",
            ResponseText = null // Optional field
        };

        var context = new ValidationContext(review);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(review, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        review.ResponseText.Should().BeNull();
    }
}