using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    [UnitTest]
    [CoreTest]
    public class DeliverableSubmissionTests
    {
        [Fact]
        public void DeliverableSubmission_Should_Initialize_With_Default_Values()
        {
            // Act
            var submission = new DeliverableSubmission();

            // Assert
            submission.Id.Should().NotBeEmpty();
            submission.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            submission.Type.Should().Be(DeliverableType.TextDescription);
            submission.IsReviewed.Should().BeFalse();
            submission.IsApproved.Should().BeFalse();
            submission.AttachedFiles.Should().NotBeNull();
            submission.AttachedFiles.Should().BeEmpty();
        }

        [Fact]
        public void DeliverableSubmission_Should_Allow_Valid_Properties()
        {
            // Arrange
            var milestoneId = Guid.NewGuid();
            var submittedByUserId = Guid.NewGuid();
            var title = "Completed Feature Implementation";
            var description = "This milestone has been completed successfully with all deliverables.";
            var type = DeliverableType.FileUpload;

            // Act
            var submission = new DeliverableSubmission
            {
                MilestoneId = milestoneId,
                SubmittedByUserId = submittedByUserId,
                Title = title,
                Description = description,
                Type = type,
                SubmissionNotes = "All tests passing"
            };

            // Assert
            submission.MilestoneId.Should().Be(milestoneId);
            submission.SubmittedByUserId.Should().Be(submittedByUserId);
            submission.Title.Should().Be(title);
            submission.Description.Should().Be(description);
            submission.Type.Should().Be(type);
            submission.SubmissionNotes.Should().Be("All tests passing");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Title_Should_Be_Required(string? invalidTitle)
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = invalidTitle!
            };

            // Act
            var context = new ValidationContext(submission);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(submission, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(DeliverableSubmission.Title)));
        }

        [Fact]
        public void Title_Should_Respect_MaxLength()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = new string('A', 301) // Exceeds MaxLength(300)
            };

            // Act
            var context = new ValidationContext(submission);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(submission, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(DeliverableSubmission.Title)));
        }

        [Fact]
        public void Description_Should_Respect_MaxLength()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Valid Title",
                Description = new string('A', 5001) // Exceeds MaxLength(5000)
            };

            // Act
            var context = new ValidationContext(submission);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(submission, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().Contain(r => r.MemberNames.Contains(nameof(DeliverableSubmission.Description)));
        }

        [Fact]
        public void CanBeReviewed_Should_Return_True_When_Not_Reviewed()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                IsReviewed = false
            };

            // Act & Assert
            submission.CanBeReviewed.Should().BeTrue();
        }

        [Fact]
        public void CanBeReviewed_Should_Return_False_When_Already_Reviewed()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                IsReviewed = true
            };

            // Act & Assert
            submission.CanBeReviewed.Should().BeFalse();
        }

        [Fact]
        public void Approve_Should_Set_Review_Properties()
        {
            // Arrange
            var reviewerId = Guid.NewGuid();
            var feedback = "Great work!";
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };

            // Act
            var result = submission.Approve(reviewerId, feedback);

            // Assert
            result.Should().BeTrue();
            submission.IsReviewed.Should().BeTrue();
            submission.IsApproved.Should().BeTrue();
            submission.ReviewedByUserId.Should().Be(reviewerId);
            submission.ReviewFeedback.Should().Be(feedback);
            submission.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Approve_Should_Fail_When_Already_Reviewed()
        {
            // Arrange
            var reviewerId = Guid.NewGuid();
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                IsReviewed = true
            };

            // Act
            var result = submission.Approve(reviewerId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Reject_Should_Set_Review_Properties()
        {
            // Arrange
            var reviewerId = Guid.NewGuid();
            var feedback = "Needs more work";
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };

            // Act
            var result = submission.Reject(reviewerId, feedback);

            // Assert
            result.Should().BeTrue();
            submission.IsReviewed.Should().BeTrue();
            submission.IsApproved.Should().BeFalse();
            submission.ReviewedByUserId.Should().Be(reviewerId);
            submission.ReviewFeedback.Should().Be(feedback);
            submission.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void Reject_Should_Fail_With_Empty_Feedback(string? invalidFeedback)
        {
            // Arrange
            var reviewerId = Guid.NewGuid();
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };

            // Act
            var result = submission.Reject(reviewerId, invalidFeedback!);

            // Assert
            result.Should().BeFalse();
            submission.IsReviewed.Should().BeFalse();
        }

        [Fact]
        public void AddFileAttachment_Should_Add_File()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };
            var file = new UploadedFile
            {
                FileName = "test.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1024
            };

            // Act
            var result = submission.AddFileAttachment(file);

            // Assert
            result.Should().BeTrue();
            submission.AttachedFiles.Should().Contain(file);
            submission.AttachmentCount.Should().Be(1);
        }

        [Fact]
        public void AddFileAttachment_Should_Fail_When_Already_Reviewed()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                IsReviewed = true
            };
            var file = new UploadedFile
            {
                FileName = "test.pdf"
            };

            // Act
            var result = submission.AddFileAttachment(file);

            // Assert
            result.Should().BeFalse();
            submission.AttachedFiles.Should().BeEmpty();
        }

        [Fact]
        public void RemoveFileAttachment_Should_Remove_File()
        {
            // Arrange
            var file = new UploadedFile
            {
                FileName = "test.pdf"
            };
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };
            submission.AttachedFiles.Add(file);

            // Act
            var result = submission.RemoveFileAttachment(file.Id);

            // Assert
            result.Should().BeTrue();
            submission.AttachedFiles.Should().NotContain(file);
        }

        [Fact]
        public void RemoveFileAttachment_Should_Fail_When_Already_Reviewed()
        {
            // Arrange
            var file = new UploadedFile
            {
                FileName = "test.pdf"
            };
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                IsReviewed = true
            };
            submission.AttachedFiles.Add(file);

            // Act
            var result = submission.RemoveFileAttachment(file.Id);

            // Assert
            result.Should().BeFalse();
            submission.AttachedFiles.Should().Contain(file);
        }

        [Theory]
        [InlineData(DeliverableType.FileUpload, true)] // Has files
        [InlineData(DeliverableType.TextDescription, true)] // Has text content
        [InlineData(DeliverableType.LinkSubmission, true)] // Has valid URL
        [InlineData(DeliverableType.CodeRepository, true)] // Has valid URL
        public void IsValid_Should_Return_True_For_Valid_Submissions(DeliverableType type, bool expectedValid)
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                Type = type
            };

            // Set up type-specific content
            switch (type)
            {
                case DeliverableType.FileUpload:
                    submission.AttachedFiles.Add(new UploadedFile { FileName = "test.pdf" });
                    break;
                case DeliverableType.TextDescription:
                    submission.TextContent = "Some text content";
                    break;
                case DeliverableType.LinkSubmission:
                case DeliverableType.CodeRepository:
                    submission.SubmissionUrl = "https://example.com/submission";
                    break;
            }

            // Act
            var result = submission.IsValid();

            // Assert
            result.Should().Be(expectedValid);
        }

        [Theory]
        [InlineData(DeliverableType.FileUpload)] // No files
        [InlineData(DeliverableType.TextDescription)] // No text content
        [InlineData(DeliverableType.LinkSubmission)] // No URL
        [InlineData(DeliverableType.CodeRepository)] // No URL
        public void IsValid_Should_Return_False_For_Invalid_Submissions(DeliverableType type)
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission",
                Type = type
                // No type-specific content provided
            };

            // Act
            var result = submission.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValid_Should_Return_False_For_Empty_Title()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "",
                Type = DeliverableType.TextDescription,
                TextContent = "Some content"
            };

            // Act
            var result = submission.IsValid();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void TotalFileSize_Should_Calculate_Correctly()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };
            submission.AttachedFiles.Add(new UploadedFile { FileName = "file1.pdf", FileSizeBytes = 1024 });
            submission.AttachedFiles.Add(new UploadedFile { FileName = "file2.pdf", FileSizeBytes = 2048 });

            // Act & Assert
            submission.TotalFileSize.Should().Be(3072);
        }

        [Fact]
        public void AttachmentCount_Should_Return_Correct_Count()
        {
            // Arrange
            var submission = new DeliverableSubmission
            {
                Title = "Test Submission"
            };
            submission.AttachedFiles.Add(new UploadedFile { FileName = "file1.pdf" });
            submission.AttachedFiles.Add(new UploadedFile { FileName = "file2.pdf" });

            // Act & Assert
            submission.AttachmentCount.Should().Be(2);
        }
    }
}