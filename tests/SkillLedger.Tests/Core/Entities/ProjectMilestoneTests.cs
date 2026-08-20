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
    public class ProjectMilestoneTests
    {
        [Fact]
        public void ProjectMilestone_Should_Initialize_With_Default_Values()
        {
            // Act
            var milestone = new ProjectMilestone();

            // Assert
            milestone.Id.Should().NotBeEmpty();
            milestone.WeightPercentage.Should().Be(0);
            // Note: CompletionPercentage property doesn't exist in the actual entity
            milestone.Status.Should().Be(MilestoneStatus.NotStarted);
            milestone.Priority.Should().Be(MilestonePriority.Medium);
            milestone.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            milestone.SequenceOrder.Should().Be(1);
            // Note: SortOrder property doesn't exist - only SequenceOrder exists
        }

        [Fact]
        public void ProjectMilestone_Should_Allow_Valid_Properties()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var title = "Phase 1 Completion";
            var description = "Complete the initial phase of the project";
            var dueDate = DateTime.UtcNow.AddDays(30);
            var weightPercentage = 75.5m;
            var status = MilestoneStatus.InProgress;
            var priority = MilestonePriority.High;
            var sequenceOrder = 1;
            var createdByUserId = Guid.NewGuid();

            // Act
            var milestone = new ProjectMilestone
            {
                ProjectId = projectId,
                Title = title,
                Description = description,
                DueDate = dueDate,
                WeightPercentage = weightPercentage,
                Status = status,
                Priority = priority,
                SequenceOrder = sequenceOrder,
                CreatedByUserId = createdByUserId
            };

            // Assert
            milestone.ProjectId.Should().Be(projectId);
            milestone.Title.Should().Be(title);
            milestone.Description.Should().Be(description);
            milestone.DueDate.Should().Be(dueDate);
            milestone.WeightPercentage.Should().Be(weightPercentage);
            // Note: CompletionPercentage property doesn't exist in the actual entity
            milestone.Status.Should().Be(status);
            milestone.Priority.Should().Be(priority);
            milestone.SequenceOrder.Should().Be(sequenceOrder);
            // Note: SortOrder property doesn't exist - only SequenceOrder exists
            milestone.CreatedByUserId.Should().Be(createdByUserId);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(100.1)]
        [InlineData(-10)]
        [InlineData(150)]
        public void ProjectMilestone_Should_Validate_WeightPercentage_Range(decimal invalidPercentage)
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                WeightPercentage = invalidPercentage
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(milestone);
            var isValid = Validator.TryValidateObject(milestone, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("WeightPercentage") || vr.ErrorMessage!.Contains("field") && vr.ErrorMessage!.Contains("0") && vr.ErrorMessage!.Contains("100"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(25.5)]
        [InlineData(50)]
        [InlineData(75.25)]
        [InlineData(100)]
        public void ProjectMilestone_Should_Accept_Valid_WeightPercentage(decimal validPercentage)
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                WeightPercentage = validPercentage
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(milestone);
            var isValid = Validator.TryValidateObject(milestone, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeTrue();
            milestone.WeightPercentage.Should().Be(validPercentage);
            // Note: CompletionPercentage property doesn't exist in the actual entity
        }

        [Fact]
        public void ProjectMilestone_Should_Require_ProjectId()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.Empty,
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(milestone);
            var isValid = Validator.TryValidateObject(milestone, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("ProjectId"));
        }

        [Fact]
        public void ProjectMilestone_Should_Require_Title()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = string.Empty,
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(milestone);
            var isValid = Validator.TryValidateObject(milestone, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Title"));
        }

        [Fact]
        public void ProjectMilestone_Should_Limit_Title_Length()
        {
            // Arrange
            var longTitle = new string('A', 201); // Assuming max length of 200
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = longTitle,
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid()
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(milestone);
            var isValid = Validator.TryValidateObject(milestone, validationContext, validationResults, true);

            // Assert
            isValid.Should().BeFalse();
            validationResults.Should().Contain(vr => vr.MemberNames.Contains("Title"));
        }

        [Fact]
        public void SubmitForReview_Should_Change_Status_To_PendingReview()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.InProgress
            };

            // Act
            var result = milestone.SubmitForReview();

            // Assert
            result.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.PendingReview);
            milestone.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Approve_Should_Set_CompletedAt_And_Status()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.PendingReview
            };

            // Act
            var result = milestone.Approve("Looks good!");

            // Assert
            result.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Approved);
            milestone.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            milestone.ReviewNotes.Should().Be("Looks good!");
        }

        [Fact]
        public void RequestRevision_Should_Change_Status_And_Set_ReviewNotes()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.PendingReview
            };

            var reviewNotes = "Please address the following issues...";

            // Act
            var result = milestone.RequestRevision(reviewNotes);

            // Assert
            result.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.RequiresRevision);
            milestone.ReviewNotes.Should().Be(reviewNotes);
            milestone.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void IsOverdue_Should_Return_True_When_DueDate_Passed()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(-1),
                Status = MilestoneStatus.InProgress
            };

            // Act
            var isOverdue = milestone.IsOverdue;

            // Assert
            isOverdue.Should().BeTrue();
        }

        [Fact]
        public void IsOverdue_Should_Return_False_When_DueDate_Future()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(1),
                Status = MilestoneStatus.InProgress
            };

            // Act
            var isOverdue = milestone.IsOverdue;

            // Assert
            isOverdue.Should().BeFalse();
        }

        [Fact]
        public void IsOverdue_Should_Return_False_When_No_DueDate()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                DueDate = null,
                Status = MilestoneStatus.InProgress
            };

            // Act
            var isOverdue = milestone.IsOverdue;

            // Assert
            isOverdue.Should().BeFalse();
        }

        [Fact]
        public void IsOverdue_Should_Return_False_When_Completed()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(-1),
                Status = MilestoneStatus.Approved
            };

            // Act
            var isOverdue = milestone.IsOverdue;

            // Assert
            isOverdue.Should().BeFalse();
        }

        [Fact]
        public void StartWork_Should_Return_True_When_NotStarted_And_Assigned()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.NotStarted,
                AssignedToUserId = userId
            };

            // Act
            var result = milestone.StartWork(userId);

            // Assert
            result.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.InProgress);
            milestone.AssignedToUserId.Should().Be(userId);
            milestone.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void StartWork_Should_Return_False_When_Already_InProgress()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.InProgress,
                AssignedToUserId = userId
            };

            // Act
            var result = milestone.StartWork(userId);

            // Assert
            result.Should().BeFalse();
            milestone.Status.Should().Be(MilestoneStatus.InProgress);
        }

        [Fact]
        public void Cancel_Should_Set_Status_And_Reason()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.InProgress
            };

            var reason = "Project requirements changed";

            // Act
            var result = milestone.Cancel(reason);

            // Assert
            result.Should().BeTrue();
            milestone.Status.Should().Be(MilestoneStatus.Cancelled);
            milestone.ReviewNotes.Should().Be(reason);
            milestone.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Cancel_Should_Return_False_When_Already_Approved()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.Approved
            };

            // Act
            var result = milestone.Cancel("Some reason");

            // Assert
            result.Should().BeFalse();
            milestone.Status.Should().Be(MilestoneStatus.Approved);
        }

        [Fact]
        public void CanBeStarted_Should_Return_True_When_NotStarted_And_Assigned()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.NotStarted,
                AssignedToUserId = Guid.NewGuid()
            };

            // Act & Assert
            milestone.CanBeStarted.Should().BeTrue();
        }

        [Fact]
        public void CanBeSubmitted_Should_Return_True_When_InProgress()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.InProgress
            };

            // Act & Assert
            milestone.CanBeSubmitted.Should().BeTrue();
        }

        [Fact]
        public void CanBeApproved_Should_Return_True_When_PendingReview()
        {
            // Arrange
            var milestone = new ProjectMilestone
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Milestone",
                Description = "Test Description",
                CreatedByUserId = Guid.NewGuid(),
                Status = MilestoneStatus.PendingReview
            };

            // Act & Assert
            milestone.CanBeApproved.Should().BeTrue();
        }
    }
}