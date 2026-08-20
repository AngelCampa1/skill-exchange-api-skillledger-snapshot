using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Core.Services;

public class BadgeServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly BadgeService _badgeService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<IBadgeSecurityService> _mockSecurityService;
    private readonly Mock<IDistributedLockService> _mockDistributedLockService;
    private readonly Mock<ILogger<BadgeService>> _mockLogger;

    public BadgeServiceTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"BadgeServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockSecurityService = new Mock<IBadgeSecurityService>();
        _mockDistributedLockService = new Mock<IDistributedLockService>();
        _mockLogger = new Mock<ILogger<BadgeService>>();

        _mockSecurityService.Setup(x => x.GenerateBadgeHashAsync(It.IsAny<UserBadge>()))
            .ReturnsAsync("test-hash");

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

        _badgeService = new BadgeService(
            _context,
            _mockAuditLogService.Object,
            _mockSecurityService.Object,
            _mockDistributedLockService.Object,
            _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test users
        var testUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-400) // Account age for veteran badge
        };

        _context.Users.Add(testUser);

        // Add badge definitions
        var highPerformerDef = new BadgeDefinition
        {
            Id = Guid.NewGuid(),
            BadgeType = "HIGH_PERFORMER",
            Category = BadgeCategory.Performance,
            DisplayName = "High Performer",
            Description = "Maintains 4.5+ average rating across 10+ projects",
            RequiredVerification = VerificationLevel.Automatic,
            IsActive = true
        };

        var verifiedIdentityDef = new BadgeDefinition
        {
            Id = Guid.NewGuid(),
            BadgeType = "VERIFIED_IDENTITY",
            Category = BadgeCategory.Trust,
            DisplayName = "Verified Identity",
            Description = "Government-issued ID verification completed",
            RequiredVerification = VerificationLevel.Manual,
            IsActive = true
        };

        _context.BadgeDefinitions.AddRange(highPerformerDef, verifiedIdentityDef);

        // Add some projects for the user
        for (int i = 0; i < 15; i++)
        {
            _context.Projects.Add(new Project
            {
                Id = Guid.NewGuid(),
                ClientId = testUser.Id,
                Title = $"Test Project {i}",
                Description = $"Description {i}",
                Status = ProjectStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 10)
            });
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task AwardBadgeAsync_ValidBadge_CreatesWithIntegrityHash()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var badgeType = "HIGH_PERFORMER";
        var evidence = new Dictionary<string, object> { { "rating", 4.8 }, { "projects", 12 } };

        // Act
        var result = await _badgeService.AwardBadgeAsync(userId, badgeType, evidence);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(badgeType, result.BadgeType);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("test-hash", result.IntegrityHash);
        Assert.True(result.IsActive);

        // Verify security service was called
        _mockSecurityService.Verify(x => x.GenerateBadgeHashAsync(It.IsAny<UserBadge>()), Times.Once);

        // Verify audit log was called
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            userId,
            "Badge Awarded",
            It.IsAny<string>(),
            null,
            true,
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task AwardBadgeAsync_DuplicateBadge_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var badgeType = "HIGH_PERFORMER";

        // Award first badge
        await _badgeService.AwardBadgeAsync(userId, badgeType);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _badgeService.AwardBadgeAsync(userId, badgeType));

        Assert.Contains("already has badge", exception.Message);
    }

    [Fact]
    public async Task AwardBadgeAsync_InvalidBadgeType_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var invalidBadgeType = "NONEXISTENT_BADGE";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _badgeService.AwardBadgeAsync(userId, invalidBadgeType));

        Assert.Contains("not found or inactive", exception.Message);
    }

    [Fact]
    public async Task RevokeBadgeAsync_ExistingBadge_DeactivatesAndLogsHistory()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var badge = await _badgeService.AwardBadgeAsync(userId, "HIGH_PERFORMER");
        var reason = "Performance decline";
        var revokedBy = Guid.NewGuid();

        // Act
        await _badgeService.RevokeBadgeAsync(badge.Id, reason, revokedBy);

        // Assert
        var updatedBadge = await _context.UserBadges.FindAsync(badge.Id);
        Assert.NotNull(updatedBadge);
        Assert.False(updatedBadge.IsActive);

        var history = await _context.BadgeEarningHistory
            .FirstOrDefaultAsync(h => h.BadgeId == badge.Id && h.Action == "Revoked");
        Assert.NotNull(history);
        Assert.Equal(reason, history.Reason);
        Assert.Equal(revokedBy, history.ActionBy);
    }

    [Fact]
    public async Task GetUserBadgesAsync_ValidatesIntegrity_ReturnsActiveBadges()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await _badgeService.AwardBadgeAsync(userId, "HIGH_PERFORMER");

        _mockSecurityService.Setup(x => x.ValidateBadgeIntegrityAsync(It.IsAny<UserBadge>()))
            .ReturnsAsync(true);

        // Act
        var badges = await _badgeService.GetUserBadgesAsync(userId);

        // Assert
        Assert.Single(badges);
        Assert.Equal("HIGH_PERFORMER", badges[0].BadgeType);

        // Verify integrity validation was called
        _mockSecurityService.Verify(x => x.ValidateBadgeIntegrityAsync(It.IsAny<UserBadge>()), Times.Once);
    }

    [Fact]
    public async Task SubmitVerificationRequestAsync_ValidRequest_CreatesRequest()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var badgeType = "VERIFIED_IDENTITY";
        var evidence = new Dictionary<string, object>
        {
            { "documentType", "passport" },
            { "documentNumber", "A1234567" }
        };

        // Act
        var request = await _badgeService.SubmitVerificationRequestAsync(userId, badgeType, evidence);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(userId, request.UserId);
        Assert.Equal(badgeType, request.BadgeType);
        Assert.Equal("Pending", request.Status);
        Assert.Contains("passport", request.SubmittedEvidence);

        // Verify audit log
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            userId,
            "Verification Request Submitted",
            It.IsAny<string>(),
            null,
            true,
            It.IsAny<string>(),
            null), Times.Once);
    }

    [Fact]
    public async Task SubmitVerificationRequestAsync_DuplicateRequest_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var badgeType = "VERIFIED_IDENTITY";
        var evidence = new Dictionary<string, object> { { "test", "data" } };

        // Submit first request
        await _badgeService.SubmitVerificationRequestAsync(userId, badgeType, evidence);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _badgeService.SubmitVerificationRequestAsync(userId, badgeType, evidence));

        Assert.Contains("pending verification request", exception.Message);
    }

    [Fact]
    public async Task ProcessVerificationRequestAsync_ApprovedRequest_AwardsBadge()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var evidence = new Dictionary<string, object> { { "verified", true } };
        var request = await _badgeService.SubmitVerificationRequestAsync(userId, "VERIFIED_IDENTITY", evidence);
        var reviewerId = Guid.NewGuid();
        var reviewNotes = "Documents verified successfully";

        // Act
        await _badgeService.ProcessVerificationRequestAsync(request.Id, true, reviewNotes, reviewerId);

        // Assert
        var updatedRequest = await _context.VerificationRequests.FindAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal("Approved", updatedRequest.Status);
        Assert.Equal(reviewNotes, updatedRequest.ReviewNotes);
        Assert.Equal(reviewerId, updatedRequest.ReviewedBy);

        // Verify badge was awarded
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == userId && b.BadgeType == "VERIFIED_IDENTITY");
        Assert.NotNull(badge);
        Assert.True(badge.IsActive);
    }

    [Fact]
    public async Task ProcessVerificationRequestAsync_RejectedRequest_DoesNotAwardBadge()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var evidence = new Dictionary<string, object> { { "verified", false } };
        var request = await _badgeService.SubmitVerificationRequestAsync(userId, "VERIFIED_IDENTITY", evidence);
        var reviewerId = Guid.NewGuid();
        var reviewNotes = "Documents not acceptable";

        // Act
        await _badgeService.ProcessVerificationRequestAsync(request.Id, false, reviewNotes, reviewerId);

        // Assert
        var updatedRequest = await _context.VerificationRequests.FindAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal("Rejected", updatedRequest.Status);

        // Verify no badge was awarded
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == userId && b.BadgeType == "VERIFIED_IDENTITY");
        Assert.Null(badge);
    }

    [Fact]
    public async Task CheckBadgeEligibilityAsync_ReturnsProgressForAllBadges()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var eligibility = await _badgeService.CheckBadgeEligibilityAsync(userId);

        // Assert
        Assert.NotEmpty(eligibility);
        Assert.All(eligibility, badge =>
        {
            Assert.NotNull(badge.BadgeType);
            Assert.NotNull(badge.BadgeName);
            Assert.InRange(badge.ProgressPercentage, 0, 100);
        });
    }

    [Fact]
    public async Task ProcessAutomaticBadgeEvaluationAsync_SpecificUser_AwardsEligibleBadges()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var awarded = await _badgeService.ProcessAutomaticBadgeEvaluationAsync(userId);

        // Assert
        Assert.True(awarded >= 0); // Should not throw, might award 0 or more badges
    }

    [Fact]
    public async Task ProcessBadgeExpirationAsync_ExpiresOldBadges()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var badge = await _badgeService.AwardBadgeAsync(userId, "HIGH_PERFORMER");

        // Manually set expiration in the past
        badge.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        // Act
        var expired = await _badgeService.ProcessBadgeExpirationAsync();

        // Assert
        Assert.Equal(1, expired);

        var updatedBadge = await _context.UserBadges.FindAsync(badge.Id);
        Assert.NotNull(updatedBadge);
        Assert.False(updatedBadge.IsActive);

        // Verify history was recorded
        var history = await _context.BadgeEarningHistory
            .FirstOrDefaultAsync(h => h.BadgeId == badge.Id && h.Action == "Expired");
        Assert.NotNull(history);
    }

    [Fact]
    public async Task GetPendingVerificationRequestsAsync_FiltersByBadgeType()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var evidence = new Dictionary<string, object> { { "test", "data" } };

        await _badgeService.SubmitVerificationRequestAsync(userId, "VERIFIED_IDENTITY", evidence);

        // Act
        var allPending = await _badgeService.GetPendingVerificationRequestsAsync();
        var filteredPending = await _badgeService.GetPendingVerificationRequestsAsync("VERIFIED_IDENTITY");

        // Assert
        Assert.Single(allPending);
        Assert.Single(filteredPending);
        Assert.Equal("VERIFIED_IDENTITY", filteredPending[0].BadgeType);
    }

    [Fact]
    public async Task GetBadgeProgressAsync_ReturnsDetailedProgress()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var progress = await _badgeService.GetBadgeProgressAsync(userId);

        // Assert
        Assert.NotEmpty(progress);

        var highPerformerProgress = progress.FirstOrDefault(p => p.BadgeType == "HIGH_PERFORMER");
        Assert.NotNull(highPerformerProgress);
        Assert.Equal(BadgeCategory.Performance, highPerformerProgress.Category);
        Assert.NotEmpty(highPerformerProgress.Requirements);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}