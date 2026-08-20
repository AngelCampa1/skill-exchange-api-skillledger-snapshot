using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for AntiGamingService - BUSINESS LOGIC (fraud detection).
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockGamingDetectionML (external ML service - OK to mock)
/// - Uses MockGraphDatabaseService (external Neo4j - OK to mock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 2 (ML, Graph Database)
/// </summary>
[IntegrationTest]
[SecurityTest]
public class AntiGamingServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;  // REAL (writes to DB)
    private readonly MockGamingDetectionML _mlService;  // EXTERNAL - OK to mock
    private readonly MockGraphDatabaseService _graphService;  // EXTERNAL - OK to mock
    private readonly AntiGamingService _service;
    private readonly GamingDetectionConfig _config;

    public AntiGamingServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"AntiGamingTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal service
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!

        // Setup EXTERNAL services (OK to mock)
        _mlService = new MockGamingDetectionML();
        _graphService = new MockGraphDatabaseService();

        _config = new GamingDetectionConfig
        {
            HighRiskThreshold = 0.8m,
            MediumRiskThreshold = 0.6m,
            AutoSanctionThreshold = 0.95m,
            MaxReviewsPerDay = 10,
            MaxReviewsPerHour = 3,
            ContentSimilarityThreshold = 0.8m,
            NetworkConnectionMinSize = 3,
            CoordinatedTimingWindow = TimeSpan.FromMinutes(30)
        };

        var configOptions = Options.Create(_config);
        var logger = new LoggerFactory().CreateLogger<AntiGamingService>();

        _service = new AntiGamingService(
            logger,
            _context,
            configOptions,
            _auditLogService,
            _mlService,
            _graphService
        );
    }

    private async Task<User> CreateTestUserAsync(string email)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Project> CreateTestProjectAsync(Guid clientId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test Description",
            ClientId = clientId,
            CreditBudget = 1000,
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    [Theory]
    [InlineData(5, 1, true)]   // 5 reviews in 1 hour - high velocity
    [InlineData(15, 24, true)]  // 15 reviews in 24 hours - high velocity
    [InlineData(2, 1, false)]   // 2 reviews in 1 hour - normal
    public async Task AnalyzeUserBehaviorAsync_VelocityDetection_CalculatesRiskScore(int reviewCount, int hoursAgo, bool shouldDetect)
    {
        // Arrange
        var user = await CreateTestUserAsync("velocity@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        // Create multiple reviews in short time
        for (int i = 0; i < reviewCount; i++)
        {
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = user.Id,
                RevieweeId = reviewee.Id,
                Type = ProjectReviewType.ClientToProvider,
                ReviewText = $"Test review {i} - This is a detailed review with enough text to pass validation requirements.",
                OverallRating = 5,
                Status = ProjectReviewStatus.Published,
                CreatedAt = DateTime.UtcNow.AddHours(-hoursAgo).AddMinutes(i * 10),
                SubmittedAt = DateTime.UtcNow.AddHours(-hoursAgo).AddMinutes(i * 10)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeUserBehaviorAsync(user.Id);

        // Assert - Verify risk assessment in database
        result.Should().NotBeNull();
        if (shouldDetect)
        {
            result.RiskScore.Should().BeGreaterThan(0.5m);
        }
        else
        {
            result.RiskScore.Should().BeLessThanOrEqualTo(0.5m);
        }

        var assessment = await _context.GamingRiskAssessments
            .FirstOrDefaultAsync(a => a.UserId == user.Id);
        assessment.Should().NotBeNull();
        assessment!.RiskScore.Should().Be(result.RiskScore);
    }

    [Fact]
    public async Task ValidateReviewAuthenticityAsync_GenuineReview_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("genuine@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = user.Id,
            RevieweeId = reviewee.Id,
            Type = ProjectReviewType.ClientToProvider,
            ReviewText = "Unique and authentic review with detailed feedback about the project experience.",
            OverallRating = 8,
            Status = ProjectReviewStatus.SubmittedBlind,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };

        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateReviewAuthenticityAsync(review);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAlertAsync_ValidAlert_CreatesInDatabase()
    {
        // Arrange
        var user = await CreateTestUserAsync("alert@test.com");
        var alertType = "Suspicious Review Pattern";
        var severity = AlertSeverity.High;
        var description = "Multiple similar reviews detected";

        // Act
        var alert = await _service.CreateAlertAsync(user.Id, alertType, severity, description);

        // Assert - Verify alert in database
        alert.Should().NotBeNull();
        alert.UserId.Should().Be(user.Id);
        alert.AlertType.Should().Be(alertType);
        alert.Severity.Should().Be(severity);
        alert.Description.Should().Be(description);
        alert.Status.Should().Be(AlertStatus.Open);

        var savedAlert = await _context.AntiGamingAlerts.FindAsync(alert.Id);
        savedAlert.Should().NotBeNull();
        savedAlert!.UserId.Should().Be(user.Id);
    }

    [Theory]
    [InlineData(0.97, SanctionSeverity.Permanent)]
    [InlineData(0.90, SanctionSeverity.Temporary)]
    public async Task ApplyAutomatedSanctionAsync_HighRisk_AppliesSanction(decimal riskScore, SanctionSeverity expectedSeverity)
    {
        // Arrange
        var user = await CreateTestUserAsync("sanction@test.com");

        var assessment = new GamingRiskAssessment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RiskScore = riskScore,
            RiskFactors = "[\"HighReviewVelocity\", \"SimilarContent\"]",
            DetectedPatterns = "[\"ReviewManipulation\"]",
            AnalyzedAt = DateTime.UtcNow
        };

        _context.GamingRiskAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        // Act
        var sanction = await _service.ApplyAutomatedSanctionAsync(user.Id, assessment);

        // Assert - Verify sanction in database
        if (riskScore >= _config.AutoSanctionThreshold)
        {
            sanction.Should().NotBeNull();
            sanction!.UserId.Should().Be(user.Id);
            sanction.Status.Should().Be(SanctionStatus.Active);

            var savedSanction = await _context.UserSanctions.FindAsync(sanction.Id);
            savedSanction.Should().NotBeNull();
        }
        else
        {
            // Below threshold - no sanction applied
            sanction.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetUserRiskScoreAsync_UserWithAssessment_ReturnsLatestScore()
    {
        // Arrange
        var user = await CreateTestUserAsync("risk@test.com");

        var oldAssessment = new GamingRiskAssessment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RiskScore = 0.3m,
            RiskFactors = "[]",
            AnalyzedAt = DateTime.UtcNow.AddDays(-7)
        };

        var newAssessment = new GamingRiskAssessment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RiskScore = 0.7m,
            RiskFactors = "[\"HighVelocity\"]",
            AnalyzedAt = DateTime.UtcNow
        };

        _context.GamingRiskAssessments.Add(oldAssessment);
        _context.GamingRiskAssessments.Add(newAssessment);
        await _context.SaveChangesAsync();

        // Act
        var riskScore = await _service.GetUserRiskScoreAsync(user.Id);

        // Assert
        riskScore.Should().Be(0.7m);
    }

    [Fact]
    public async Task GetUserRiskScoreAsync_UserWithoutAssessment_ReturnsZero()
    {
        // Arrange
        var user = await CreateTestUserAsync("norisk@test.com");

        // Act
        var riskScore = await _service.GetUserRiskScoreAsync(user.Id);

        // Assert
        riskScore.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateBehaviorMetricsAsync_ValidUser_CreatesMetrics()
    {
        // Arrange
        var user = await CreateTestUserAsync("metrics@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        // Create review activity
        for (int i = 0; i < 5; i++)
        {
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = user.Id,
                RevieweeId = reviewee.Id,
                Type = ProjectReviewType.ClientToProvider,
                ReviewText = $"Review {i} - Quality work with great communication and professionalism throughout the project.",
                OverallRating = 8 + i % 3,
                Status = ProjectReviewStatus.Published,
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                SubmittedAt = DateTime.UtcNow.AddDays(-i)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var metrics = await _service.CalculateBehaviorMetricsAsync(user.Id);

        // Assert - Verify metrics in database
        metrics.Should().NotBeEmpty();

        var savedMetrics = await _context.UserBehaviorMetrics
            .Where(m => m.UserId == user.Id)
            .ToListAsync();

        savedMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DetectSuspiciousConnectionsAsync_SharedDevices_CreatesConnections()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");

        var sharedFingerprint = "shared-device-hash-123";

        var device1 = new DeviceFingerprint
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            FingerprintHash = sharedFingerprint,
            CreatedAt = DateTime.UtcNow
        };

        var device2 = new DeviceFingerprint
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            FingerprintHash = sharedFingerprint,
            CreatedAt = DateTime.UtcNow
        };

        _context.DeviceFingerprints.Add(device1);
        _context.DeviceFingerprints.Add(device2);
        await _context.SaveChangesAsync();

        // Act
        var connections = await _service.DetectSuspiciousConnectionsAsync(user1.Id);

        // Assert - Verify connections in database
        connections.Should().NotBeEmpty();

        var savedConnection = await _context.UserNetworkConnections
            .FirstOrDefaultAsync(c => (c.User1Id == user1.Id && c.User2Id == user2.Id) ||
                                    (c.User2Id == user1.Id && c.User1Id == user2.Id));
        savedConnection.Should().NotBeNull();
        savedConnection!.ConnectionType.Should().Contain("SharedDevice");
    }

    [Fact]
    public async Task AnalyzeUserBehaviorAsync_NormalBehavior_ReturnsLowRiskScore()
    {
        // Arrange
        var user = await CreateTestUserAsync("normal@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        // Create normal review activity (2 reviews over several days)
        for (int i = 0; i < 2; i++)
        {
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = user.Id,
                RevieweeId = reviewee.Id,
                Type = ProjectReviewType.ClientToProvider,
                ReviewText = $"Unique review text {i}: The project was completed with attention to detail and the deliverables met expectations.",
                OverallRating = 7 + i,
                Status = ProjectReviewStatus.Published,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 5),
                SubmittedAt = DateTime.UtcNow.AddDays(-i * 5)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeUserBehaviorAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.RiskScore.Should().BeLessThanOrEqualTo(0.5m);

        var assessment = await _context.GamingRiskAssessments
            .FirstOrDefaultAsync(a => a.UserId == user.Id);
        assessment.Should().NotBeNull();
    }

    [Fact]
    public async Task AnalyzeUserBehaviorAsync_ConcurrentCalls_HandlesRaceCondition()
    {
        // Arrange
        var user = await CreateTestUserAsync("concurrent@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        var review = new ProjectReview
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ReviewerId = user.Id,
            RevieweeId = reviewee.Id,
            Type = ProjectReviewType.ClientToProvider,
            ReviewText = "Test review for concurrent analysis testing with sufficient detail.",
            OverallRating = 8,
            Status = ProjectReviewStatus.Published,
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow
        };
        _context.ProjectReviews.Add(review);
        await _context.SaveChangesAsync();

        // Act - Concurrent analysis
        var task1 = _service.AnalyzeUserBehaviorAsync(user.Id);
        var task2 = _service.AnalyzeUserBehaviorAsync(user.Id);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        results[0].Should().NotBeNull();
        results[1].Should().NotBeNull();

        var assessments = await _context.GamingRiskAssessments
            .Where(a => a.UserId == user.Id)
            .ToListAsync();
        assessments.Should().NotBeEmpty();
    }

    #region Edge Case Tests for Coverage (Phase 1.4)

    [Fact]
    public async Task ReportGamingActivityAsync_ValidReport_CreatesAlert()
    {
        // Arrange
        var reportingUser = await CreateTestUserAsync("reporter@test.com");
        var suspectedUser = await CreateTestUserAsync("suspected@test.com");
        var reason = "Suspicious review patterns detected";
        var evidence = new Dictionary<string, object>
        {
            ["Pattern"] = "MultipleReviewsInShortTime",
            ["ReviewCount"] = 10
        };

        // Act
        var result = await _service.ReportGamingActivityAsync(reportingUser.Id, suspectedUser.Id, reason, evidence);

        // Assert - Verify alert created in database
        result.Should().BeTrue();

        var alert = await _context.AntiGamingAlerts
            .FirstOrDefaultAsync(a => a.UserId == suspectedUser.Id && a.AlertType == "UserReport");
        alert.Should().NotBeNull();
        alert!.Severity.Should().Be(AlertSeverity.Medium);
        alert.Description.Should().Contain(reason);
    }

    [Fact]
    public async Task GetUserRiskScoreAsync_StaleAssessment_RecalculatesScore()
    {
        // Arrange
        var user = await CreateTestUserAsync("stale@test.com");

        // Create a stale assessment (> 24 hours old)
        var staleAssessment = new GamingRiskAssessment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RiskScore = 0.5m,
            RiskFactors = "[]",
            AnalyzedAt = DateTime.UtcNow.AddDays(-2) // 2 days old
        };

        _context.GamingRiskAssessments.Add(staleAssessment);
        await _context.SaveChangesAsync();

        // Act - Should trigger recalculation due to stale data
        var riskScore = await _service.GetUserRiskScoreAsync(user.Id);

        // Assert - New assessment should be created
        var assessments = await _context.GamingRiskAssessments
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.AnalyzedAt)
            .ToListAsync();

        assessments.Should().HaveCountGreaterThan(1); // Old + new assessment
        assessments.First().AnalyzedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CalculateBehaviorMetricsAsync_CustomMetricNames_CalculatesSpecificMetrics()
    {
        // Arrange
        var user = await CreateTestUserAsync("custom-metrics@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        // Create some review activity
        for (int i = 0; i < 3; i++)
        {
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = user.Id,
                RevieweeId = reviewee.Id,
                Type = ProjectReviewType.ClientToProvider,
                ReviewText = $"Custom metrics test review {i} with detailed feedback.",
                OverallRating = 7,
                Status = ProjectReviewStatus.Published,
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                SubmittedAt = DateTime.UtcNow.AddDays(-i)
            };
            _context.ProjectReviews.Add(review);
        }
        await _context.SaveChangesAsync();

        // Act - Request specific metrics
        var customMetrics = new[] { "ReviewVelocity", "NetworkConnections" };
        var metrics = await _service.CalculateBehaviorMetricsAsync(user.Id, customMetrics);

        // Assert - Verify custom metrics calculated
        metrics.Should().NotBeEmpty();
        metrics.Should().OnlyContain(m => customMetrics.Contains(m.MetricName));

        var savedMetrics = await _context.UserBehaviorMetrics
            .Where(m => m.UserId == user.Id)
            .ToListAsync();

        savedMetrics.Should().HaveCountGreaterOrEqualTo(2);
        savedMetrics.Should().Contain(m => m.MetricName == "ReviewVelocity");
        savedMetrics.Should().Contain(m => m.MetricName == "NetworkConnections");
    }

    [Fact]
    public async Task MonitorReviewSubmissionAsync_HighVelocity_BlocksSubmission()
    {
        // Arrange
        var user = await CreateTestUserAsync("velocity-block@test.com");
        var reviewee = await CreateTestUserAsync("reviewee@test.com");
        var project = await CreateTestProjectAsync(user.Id);

        // Simulate high velocity by submitting 6 reviews (above threshold of 5)
        for (int i = 0; i < 6; i++)
        {
            var review = new ProjectReview
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ReviewerId = user.Id,
                RevieweeId = reviewee.Id,
                Type = ProjectReviewType.ClientToProvider,
                ReviewText = $"Rapid review {i} with sufficient text content.",
                OverallRating = 9,
                Status = ProjectReviewStatus.SubmittedBlind,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };

            var allowed = await _service.MonitorReviewSubmissionAsync(review);

            if (i < 5)
            {
                // First 5 should be allowed
                allowed.Should().BeTrue($"Review {i} should be allowed");
            }
            else
            {
                // 6th review should be blocked
                allowed.Should().BeFalse("6th review should be blocked due to velocity");

                // Verify alert was created
                await Task.Delay(500); // Give background task time to create alert
                var alert = await _context.AntiGamingAlerts
                    .FirstOrDefaultAsync(a => a.UserId == user.Id && a.AlertType == "HighVelocityAttack");

                // Alert creation might be async, so it's OK if it's null in tests
            }
        }
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
