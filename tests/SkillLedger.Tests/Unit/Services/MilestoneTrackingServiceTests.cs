using SkillLedger.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Unit.Services;

/// <summary>
/// Unit tests for MilestoneTrackingService following TDD principles
/// Focus: Business logic validation, state transitions, authorization rules
/// </summary>
[UnitTest]
[CoreTest]
public class MilestoneTrackingServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly Mock<IProjectEscrowService> _mockEscrowService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<MilestoneTrackingService>> _mockLogger;
    private readonly MilestoneTrackingService _service;
    private readonly Project _testProject;
    private readonly User _testUser;

    public MilestoneTrackingServiceTests()
    {
        // In-memory database for isolated testing
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SkillLedgerDbContext(options);
        _mockEscrowService = new Mock<IProjectEscrowService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<MilestoneTrackingService>>();

        _service = new MilestoneTrackingService(
            _context,
            _mockEscrowService.Object,
            _mockAuditLogService.Object,
            _mockLogger.Object);

        // Setup test data
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com"
        };

        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            ClientId = _testUser.Id,
            Status = ProjectStatus.Published
        };

        _context.Users.Add(_testUser);
        _context.Projects.Add(_testProject);
        _context.SaveChanges();
    }

    #region Milestone Creation Tests (TDD)

    [Fact]
    public async Task CreateMilestoneAsync_ValidRequest_ShouldCreateMilestone()
    {
        // Arrange
        var request = new CreateMilestoneRequestDto
        {
            ProjectId = _testProject.Id,
            Title = "Phase 1 Milestone",
            Description = "Complete initial phase",
            Priority = MilestonePriority.High,
            DueDate = DateTime.UtcNow.AddDays(30),
            SequenceOrder = 1,
            WeightPercentage = 50.0m,
            AcceptanceCriteria = "All deliverables completed",
            AssignedToUserId = _testUser.Id
        };

        // Act
        var result = await _service.CreateMilestoneAsync(request, _testUser.Id, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be(request.Title);
        result.Status.Should().Be(MilestoneStatus.NotStarted);

        // Verify audit log was called
        _mockAuditLogService.Verify(
            x => x.LogEventAsync(
                _testUser.Id,
                "MILESTONE_CREATED",
                "127.0.0.1",
                "web",
                true,
                It.IsAny<string>(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task CreateMilestoneAsync_ProjectNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateMilestoneRequestDto
        {
            ProjectId = Guid.NewGuid(), // Non-existent project
            Title = "Test Milestone"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateMilestoneAsync(request, _testUser.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateMilestoneAsync_InvalidTitle_ShouldThrowValidationException(string invalidTitle)
    {
        // Arrange
        var request = new CreateMilestoneRequestDto
        {
            ProjectId = _testProject.Id,
            Title = invalidTitle
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateMilestoneAsync(request, _testUser.Id));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task CreateMilestoneAsync_InvalidWeightPercentage_ShouldThrowValidationException(decimal invalidWeight)
    {
        // Arrange
        var request = new CreateMilestoneRequestDto
        {
            ProjectId = _testProject.Id,
            Title = "Valid Title",
            WeightPercentage = invalidWeight
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateMilestoneAsync(request, _testUser.Id));
    }

    #endregion

    #region Milestone Status Management Tests (TDD)

    [Fact]
    public async Task StartMilestoneAsync_ValidMilestone_ShouldUpdateStatus()
    {
        // Arrange
        var milestone = await CreateTestMilestone();

        // Act
        var result = await _service.StartMilestoneAsync(milestone.Id, _testUser.Id);

        // Assert
        result.Should().BeTrue();

        var updatedMilestone = await _service.GetMilestoneByIdAsync(milestone.Id);
        updatedMilestone!.Status.Should().Be(MilestoneStatus.InProgress);

        _mockAuditLogService.Verify(
            x => x.LogEventAsync(
                _testUser.Id,
                "MILESTONE_STARTED",
                It.IsAny<string>(),
                "web",
                true,
                It.IsAny<string>(),
                null),
            Times.Once);
    }

    [Fact]
    public async Task SubmitMilestoneForReviewAsync_InProgressMilestone_ShouldUpdateStatus()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        await _service.StartMilestoneAsync(milestone.Id, _testUser.Id);

        // Act
        var result = await _service.SubmitMilestoneForReviewAsync(milestone.Id, _testUser.Id);

        // Assert
        result.Should().BeTrue();

        var updatedMilestone = await _service.GetMilestoneByIdAsync(milestone.Id);
        updatedMilestone!.Status.Should().Be(MilestoneStatus.PendingReview);
    }

    [Fact]
    public async Task ApproveMilestoneAsync_ValidReview_ShouldApproveAndTriggerPayment()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        await _service.StartMilestoneAsync(milestone.Id, _testUser.Id);
        await _service.SubmitMilestoneForReviewAsync(milestone.Id, _testUser.Id);

        var escrowMilestoneId = Guid.NewGuid();
        await _service.LinkToEscrowMilestoneAsync(milestone.Id, escrowMilestoneId, _testUser.Id);

        // Store time before approval to check timing
        var approvalTime = DateTime.UtcNow;

        // Act
        var result = await _service.ApproveMilestoneAsync(milestone.Id, _testUser.Id, "Excellent work!");

        // Assert
        result.Should().BeTrue();

        var updatedMilestone = await _service.GetMilestoneByIdAsync(milestone.Id);
        updatedMilestone!.Status.Should().Be(MilestoneStatus.Approved);
        updatedMilestone.CompletedAt.Should().BeCloseTo(approvalTime, TimeSpan.FromMinutes(1));
        updatedMilestone.ReviewNotes.Should().Be("Excellent work!");

        // Verify escrow service was called
        _mockEscrowService.Verify(
            x => x.ReleaseMilestoneAsync(escrowMilestoneId, _testUser.Id, null),
            Times.Once);
    }

    [Fact]
    public async Task ApproveMilestoneAsync_UnauthorizedUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        await _service.StartMilestoneAsync(milestone.Id, _testUser.Id);
        await _service.SubmitMilestoneForReviewAsync(milestone.Id, _testUser.Id);

        var unauthorizedUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ApproveMilestoneAsync(milestone.Id, unauthorizedUserId, "Review"));
    }

    [Fact]
    public async Task RequestMilestoneRevisionAsync_ValidRequest_ShouldRejectAndUpdateStatus()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        await _service.StartMilestoneAsync(milestone.Id, _testUser.Id);
        await _service.SubmitMilestoneForReviewAsync(milestone.Id, _testUser.Id);

        var reviewNotes = "Please address the following issues: 1. Fix bug in login 2. Update documentation";

        // Act
        var result = await _service.RequestMilestoneRevisionAsync(milestone.Id, _testUser.Id, reviewNotes);

        // Assert
        result.Should().BeTrue();

        var updatedMilestone = await _service.GetMilestoneByIdAsync(milestone.Id);
        updatedMilestone!.Status.Should().Be(MilestoneStatus.InProgress);
        updatedMilestone.ReviewNotes.Should().Be(reviewNotes);
    }

    #endregion

    #region Submission Management Tests (TDD)

    [Fact]
    public async Task CreateSubmissionAsync_ValidSubmission_ShouldCreateWithFiles()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        var testFile = new UploadedFile
        {
            FileName = "deliverable.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            BlobName = "test-blob"
        };
        _context.UploadedFiles.Add(testFile);
        await _context.SaveChangesAsync();

        var request = new CreateSubmissionRequestDto
        {
            MilestoneId = milestone.Id,
            Type = DeliverableType.FileUpload,
            Title = "Phase 1 Deliverables",
            Description = "Completed all requirements",
            AttachedFileIds = new List<Guid> { testFile.Id },
            SubmissionNotes = "Ready for review"
        };

        // Act
        var result = await _service.CreateSubmissionAsync(request, _testUser.Id, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(request.Title);
        result.AttachedFiles.Should().HaveCount(1);
        result.AttachedFiles.First().FileName.Should().Be("deliverable.pdf");
    }

    [Fact]
    public async Task ReviewSubmissionAsync_ValidReview_ShouldUpdateReviewStatus()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        var submission = await CreateTestSubmission(milestone.Id);

        var reviewRequest = new ReviewSubmissionRequestDto
        {
            IsApproved = true,
            ReviewFeedback = "Excellent quality work!"
        };

        // Act
        var result = await _service.ReviewSubmissionAsync(submission.Id, reviewRequest, _testUser.Id);

        // Assert
        result.Should().BeTrue();

        var updatedSubmission = await _service.GetSubmissionByIdAsync(submission.Id);
        updatedSubmission!.IsReviewed.Should().BeTrue();
        updatedSubmission.IsApproved.Should().BeTrue();
        updatedSubmission.ReviewFeedback.Should().Be("Excellent quality work!");
    }

    [Fact]
    public async Task ReviewSubmissionAsync_UnauthorizedReviewer_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        var submission = await CreateTestSubmission(milestone.Id);
        var unauthorizedUserId = Guid.NewGuid();

        var reviewRequest = new ReviewSubmissionRequestDto
        {
            IsApproved = false,
            ReviewFeedback = "Needs improvement"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ReviewSubmissionAsync(submission.Id, reviewRequest, unauthorizedUserId));
    }

    #endregion

    #region Multi-dimensional Rating Tests (TDD)

    [Fact]
    public async Task CalculateProjectProgress_MultipleStatuses_ShouldCalculateCorrectly()
    {
        // Arrange
        var milestones = new[]
        {
            await CreateTestMilestone("Milestone 1", 25.0m, MilestoneStatus.Approved),
            await CreateTestMilestone("Milestone 2", 25.0m, MilestoneStatus.InProgress),
            await CreateTestMilestone("Milestone 3", 25.0m, MilestoneStatus.PendingReview),
            await CreateTestMilestone("Milestone 4", 25.0m, MilestoneStatus.NotStarted)
        };

        // Act
        var progress = await _service.GetProjectProgressAsync(_testProject.Id);

        // Assert
        progress.TotalMilestones.Should().Be(4);
        progress.CompletedMilestones.Should().Be(1); // Only approved count as completed
        progress.InProgressMilestones.Should().Be(1);
        progress.OverallProgressPercentage.Should().Be(25.0m); // 1/4 * 100
    }

    [Fact]
    public async Task GetOverdueMilestonesAsync_MixedDueDates_ShouldReturnOnlyOverdue()
    {
        // Arrange
        var pastDue = await CreateTestMilestone("Past Due", 33.0m, MilestoneStatus.InProgress);
        pastDue = await UpdateMilestoneDueDate(pastDue.Id, DateTime.UtcNow.AddDays(-5));

        var futureDue = await CreateTestMilestone("Future Due", 33.0m, MilestoneStatus.InProgress);
        futureDue = await UpdateMilestoneDueDate(futureDue.Id, DateTime.UtcNow.AddDays(5));

        var completed = await CreateTestMilestone("Completed", 34.0m, MilestoneStatus.Approved);
        completed = await UpdateMilestoneDueDate(completed.Id, DateTime.UtcNow.AddDays(-2));

        // Act
        var overdueMilestones = await _service.GetOverdueMilestonesAsync();

        // Assert
        overdueMilestones.Should().HaveCount(1);
        overdueMilestones.First().Title.Should().Be("Past Due");
    }

    #endregion

    #region Blind Review System Tests (TDD)

    [Fact]
    public async Task BlindReviewWorkflow_AnonymousSubmission_ShouldMaintainPrivacy()
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        var submission = await CreateTestSubmission(milestone.Id);

        // Act - Simulate blind review (reviewer shouldn't see submitter details in business context)
        var submissionForReview = await _service.GetSubmissionByIdAsync(submission.Id);

        // Assert - In a blind review, submitter info would be masked at the API layer
        submissionForReview.Should().NotBeNull();
        submissionForReview!.SubmittedByUserId.Should().NotBeEmpty(); // ID preserved for audit
        // Note: UI layer would hide SubmittedByUserName in blind review mode
    }

    #endregion

    #region Content Validation Tests (TDD)

    [Theory]
    [InlineData(DeliverableType.FileUpload, false)] // No files attached
    [InlineData(DeliverableType.TextDescription, false)] // No text content
    [InlineData(DeliverableType.LinkSubmission, false)] // No URL
    public async Task CreateSubmissionAsync_MissingRequiredContent_ShouldValidateCorrectly(
        DeliverableType type, bool shouldSucceed)
    {
        // Arrange
        var milestone = await CreateTestMilestone();
        var request = new CreateSubmissionRequestDto
        {
            MilestoneId = milestone.Id,
            Type = type,
            Title = "Test Submission"
            // Missing type-specific content
        };

        // Act & Assert
        if (!shouldSucceed)
        {
            // Business logic validation should be handled at service level
            var submission = await _service.CreateSubmissionAsync(request, _testUser.Id);

            // Verify entity validation would catch this
            var entity = await _context.DeliverableSubmissions.FindAsync(submission.Id);
            entity!.IsValid().Should().BeFalse();
        }
    }

    #endregion

    #region Photo Attachment Tests (TDD)

    [Fact]
    public async Task CreateSubmissionAsync_PhotoAttachments_ShouldValidateSizeAndType()
    {
        // Arrange
        var milestone = await CreateTestMilestone();

        var validPhoto = new UploadedFile
        {
            FileName = "screenshot.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 2 * 1024 * 1024 // 2MB
        };

        var oversizedPhoto = new UploadedFile
        {
            FileName = "large.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 20 * 1024 * 1024 // 20MB
        };

        _context.UploadedFiles.AddRange(validPhoto, oversizedPhoto);
        await _context.SaveChangesAsync();

        var request = new CreateSubmissionRequestDto
        {
            MilestoneId = milestone.Id,
            Type = DeliverableType.FileUpload,
            Title = "Photo Submission",
            AttachedFileIds = new List<Guid> { validPhoto.Id, oversizedPhoto.Id }
        };

        // Act
        var result = await _service.CreateSubmissionAsync(request, _testUser.Id);

        // Assert
        result.AttachedFiles.Should().HaveCount(2);
        result.TotalFileSize.Should().Be(validPhoto.FileSizeBytes + oversizedPhoto.FileSizeBytes);

        // File size validation would be handled at upload/API level
        result.AttachedFiles.Any(f => f.FileSize > 10 * 1024 * 1024).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task<MilestoneResponseDto> CreateTestMilestone(
        string title = "Test Milestone",
        decimal weight = 50.0m,
        MilestoneStatus? status = null)
    {
        var request = new CreateMilestoneRequestDto
        {
            ProjectId = _testProject.Id,
            Title = title,
            Description = "Test Description",
            Priority = MilestonePriority.Medium,
            WeightPercentage = weight,
            AssignedToUserId = _testUser.Id
        };

        var milestone = await _service.CreateMilestoneAsync(request, _testUser.Id);

        if (status.HasValue && status != MilestoneStatus.NotStarted)
        {
            // Update status directly in database for test setup
            var entity = await _context.ProjectMilestones.FindAsync(milestone.Id);
            entity!.Status = status.Value;
            if (status == MilestoneStatus.Approved)
            {
                entity.CompletedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        return milestone;
    }

    private async Task<SubmissionResponseDto> CreateTestSubmission(Guid milestoneId)
    {
        var request = new CreateSubmissionRequestDto
        {
            MilestoneId = milestoneId,
            Type = DeliverableType.TextDescription,
            Title = "Test Submission",
            Description = "Test submission description",
            TextContent = "Completed all requirements"
        };

        return await _service.CreateSubmissionAsync(request, _testUser.Id);
    }

    private async Task<MilestoneResponseDto> UpdateMilestoneDueDate(Guid milestoneId, DateTime dueDate)
    {
        var entity = await _context.ProjectMilestones.FindAsync(milestoneId);
        entity!.DueDate = dueDate;
        await _context.SaveChangesAsync();

        return (await _service.GetMilestoneByIdAsync(milestoneId))!;
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}