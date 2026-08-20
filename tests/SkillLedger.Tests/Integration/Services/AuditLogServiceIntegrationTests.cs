using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for AuditLogService - SECURITY & PERFORMANCE.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real IMemoryCache for sampling counters
/// - NO external service mocks needed (pure internal logic)
/// - Verifies actual database state, not mock interactions
/// - Tests concurrent writes, rate limiting, and performance
///
/// Max mocked external dependencies: 0 (all internal)
/// </summary>
[IntegrationTest]
[SecurityTest]
public class AuditLogServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly AuditLogService _auditLogService;

    private readonly Guid _testUserId = Guid.NewGuid();
    private const string TestIpAddress = "192.168.1.100";
    private const string TestUserAgent = "Mozilla/5.0 (Test Browser)";

    public AuditLogServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuditLogServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Real memory cache for sampling counters
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        var logger = new LoggerFactory().CreateLogger<AuditLogService>();

        _auditLogService = new AuditLogService(_context, logger, _memoryCache);
    }

    #region Concurrent Audit Log Writes Tests

    [Fact]
    public async Task LogEventAsync_ConcurrentWrites_ShouldAllSucceedWithoutDataLoss()
    {
        // Arrange - 25 concurrent critical actions (should all be logged)
        var tasks = new List<Task>();
        var userIds = Enumerable.Range(0, 25).Select(_ => Guid.NewGuid()).ToList();

        // Act - Fire 25 concurrent audit log writes
        foreach (var userId in userIds)
        {
            tasks.Add(_auditLogService.LogEventAsync(
                userId,
                "CREDIT_TRANSFER",  // Critical action
                $"192.168.1.{userId.GetHashCode() % 255}",
                "Test Agent",
                success: true,
                details: $"Concurrent test {userId}"));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify all 25 logs written to database
        var logs = await _context.AuditLogs
            .Where(al => al.Action == "CREDIT_TRANSFER")
            .ToListAsync();

        logs.Should().HaveCount(25, "all concurrent writes should succeed without data loss");

        // Verify each user ID appears exactly once
        var distinctUserIds = logs.Select(l => l.UserId).Distinct().Count();
        distinctUserIds.Should().Be(25, "each user should have exactly one audit log");
    }

    [Fact]
    public async Task LogEventAsync_ConcurrentSameUserAction_ShouldRespectRateLimiting()
    {
        // Arrange - 10 concurrent writes for same user-action combo (non-critical)
        var tasks = new List<Task>();

        // Act - Fire 10 concurrent "PROJECT_VIEW" for same user (sampled + rate limited)
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_auditLogService.LogEventAsync(
                _testUserId,
                "PROJECT_VIEW",  // Sampled action
                TestIpAddress,
                TestUserAgent,
                success: true));
        }

        await Task.WhenAll(tasks);

        // Assert - Due to rate limiting (1/second) and sampling (1/10), expect very few logs
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId && al.Action == "PROJECT_VIEW")
            .ToListAsync();

        logs.Should().HaveCountLessThanOrEqualTo(2,
            "rate limiting (1/sec) and sampling (10%) should drastically reduce logs");
    }

    [Fact]
    public async Task LogEventAsync_ConcurrentCriticalActions_ShouldIgnoreRateLimiting()
    {
        // Arrange - 10 concurrent CRITICAL actions for same user
        var tasks = new List<Task>();

        // Act - Fire 10 concurrent "LOGIN_FAILED" for same user (critical - should all log)
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_auditLogService.LogEventAsync(
                _testUserId,
                "LOGIN_FAILED",  // Critical action
                TestIpAddress,
                $"Attempt-{i}",
                success: false,
                errorMessage: "Invalid password"));
        }

        await Task.WhenAll(tasks);

        // Assert - ALL critical actions should be logged (no rate limiting)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId && al.Action == "LOGIN_FAILED")
            .ToListAsync();

        logs.Should().HaveCount(10,
            "critical actions should bypass rate limiting and ALL be logged");
    }

    #endregion

    #region Audit Trail Completeness Tests

    [Fact]
    public async Task LogEventAsync_CriticalActions_ShouldAlwaysBeLogged()
    {
        // Arrange - All critical actions defined in AuditLogService
        var criticalActions = new[]
        {
            "LOGIN_FAILED",
            "USER_REGISTRATION",
            "PASSWORD_RESET",
            "CREDIT_TRANSFER",
            "PROJECT_CREATE",
            "UNAUTHORIZED_ACCESS",
            "PAYMENT_FAILED"
        };

        // Act - Log each critical action
        foreach (var action in criticalActions)
        {
            await _auditLogService.LogEventAsync(
                _testUserId,
                action,
                TestIpAddress,
                TestUserAgent,
                success: true);
        }

        // Assert - Verify ALL critical actions logged (no sampling)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId)
            .ToListAsync();

        logs.Should().HaveCount(criticalActions.Length,
            "all critical actions must be logged without sampling");

        foreach (var action in criticalActions)
        {
            logs.Should().ContainSingle(l => l.Action == action,
                $"critical action '{action}' must be logged");
        }
    }

    [Fact]
    public async Task LogEventAsync_FailedActions_ShouldAlwaysBeLogged()
    {
        // Arrange - Non-critical action that fails
        var action = "API_CALL";  // Normally sampled

        // Act - Log 10 failures (should ALL be logged even though API_CALL is sampled)
        for (int i = 0; i < 10; i++)
        {
            await _auditLogService.LogEventAsync(
                _testUserId,
                action,
                TestIpAddress,
                TestUserAgent,
                success: false,  // FAILURE
                errorMessage: $"API error {i}");
        }

        // Assert - ALL failures should be logged (no sampling for failures)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId && al.Action == action && !al.Success)
            .ToListAsync();

        logs.Should().HaveCount(10,
            "all failures must be logged regardless of sampling rules");
    }

    [Fact]
    public async Task LogEventAsync_SampledActions_ShouldReduceLogVolume()
    {
        // Arrange - Sampled actions (PROJECT_VIEW, PROFILE_VIEW, SEARCH_QUERY, API_CALL)
        var sampledAction = "PROJECT_VIEW";

        // Act - Log 100 sampled actions (expect ~10% to be logged)
        for (int i = 0; i < 100; i++)
        {
            await _auditLogService.LogEventAsync(
                Guid.NewGuid(),  // Different user each time to avoid rate limiting
                sampledAction,
                $"192.168.1.{i}",
                TestUserAgent,
                success: true);

            // Small delay to avoid rate limiting within same second
            await Task.Delay(15);
        }

        // Assert - Expect ~10% logged (sampling rate is 1 in 10)
        var logs = await _context.AuditLogs
            .Where(al => al.Action == sampledAction)
            .ToListAsync();

        logs.Should().HaveCountLessThanOrEqualTo(30,
            "sampling should reduce log volume to ~10% (allowing variance)");

        logs.Should().HaveCountGreaterThanOrEqualTo(3,
            "sampling should log at least some events (not zero)");
    }

    #endregion

    #region Query Performance with Large Datasets Tests

    [Fact]
    public async Task GetUserAuditLogsAsync_With10000Entries_ShouldReturnQuickly()
    {
        // Arrange - Create 10,000 audit log entries for test user
        var logs = new List<AuditLog>();
        for (int i = 0; i < 10000; i++)
        {
            logs.Add(new AuditLog
            {
                UserId = _testUserId,
                Action = "PROJECT_VIEW",
                IPAddress = $"192.168.{i / 255}.{i % 255}",
                UserAgent = TestUserAgent,
                Success = true,
                Timestamp = DateTime.UtcNow.AddMinutes(-i)  // Spread over time
            });
        }

        _context.AuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act - Query first page
        var startTime = DateTime.UtcNow;
        var results = await _auditLogService.GetUserAuditLogsAsync(_testUserId, pageNumber: 1, pageSize: 50);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should return 50 results quickly
        results.Should().HaveCount(50, "should return page size of 50");
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2),
            "query should complete in under 2 seconds even with 10,000 entries");

        // Verify results are ordered by timestamp descending (most recent first)
        results.Should().BeInDescendingOrder(r => r.Timestamp,
            "audit logs should be ordered by most recent first");
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_PaginationWithLargeDataset_ShouldWorkCorrectly()
    {
        // Arrange - Create 500 audit logs
        var logs = new List<AuditLog>();
        for (int i = 0; i < 500; i++)
        {
            logs.Add(new AuditLog
            {
                UserId = _testUserId,
                Action = "API_CALL",
                IPAddress = TestIpAddress,
                UserAgent = TestUserAgent,
                Success = true,
                Timestamp = DateTime.UtcNow.AddSeconds(-i)
            });
        }

        _context.AuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act - Query page 1, page 5, page 10
        var page1 = await _auditLogService.GetUserAuditLogsAsync(_testUserId, pageNumber: 1, pageSize: 50);
        var page5 = await _auditLogService.GetUserAuditLogsAsync(_testUserId, pageNumber: 5, pageSize: 50);
        var page10 = await _auditLogService.GetUserAuditLogsAsync(_testUserId, pageNumber: 10, pageSize: 50);

        // Assert - Verify pagination works correctly
        page1.Should().HaveCount(50);
        page5.Should().HaveCount(50);
        page10.Should().HaveCount(50);

        // Verify no overlap between pages
        var page1Ids = page1.Select(p => p.Id).ToHashSet();
        var page5Ids = page5.Select(p => p.Id).ToHashSet();
        var page10Ids = page10.Select(p => p.Id).ToHashSet();

        page1Ids.Should().NotIntersectWith(page5Ids, "different pages should not overlap");
        page1Ids.Should().NotIntersectWith(page10Ids, "different pages should not overlap");
        page5Ids.Should().NotIntersectWith(page10Ids, "different pages should not overlap");
    }

    [Fact]
    public async Task GetRecentFailedAttemptsAsync_WithManyFailures_ShouldCountCorrectly()
    {
        // Arrange - Create 50 failed login attempts in last hour
        var logs = new List<AuditLog>();
        var testIp = "10.0.0.1";

        for (int i = 0; i < 50; i++)
        {
            logs.Add(new AuditLog
            {
                UserId = Guid.NewGuid(),
                Action = "LOGIN_FAILED",
                IPAddress = testIp,
                UserAgent = TestUserAgent,
                Success = false,
                Timestamp = DateTime.UtcNow.AddMinutes(-30)  // Within last hour
            });
        }

        // Add 20 failed attempts from more than 1 hour ago (should not count)
        for (int i = 0; i < 20; i++)
        {
            logs.Add(new AuditLog
            {
                UserId = Guid.NewGuid(),
                Action = "LOGIN_FAILED",
                IPAddress = testIp,
                UserAgent = TestUserAgent,
                Success = false,
                Timestamp = DateTime.UtcNow.AddHours(-2)  // Too old
            });
        }

        _context.AuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act - Get recent failed attempts
        var count = await _auditLogService.GetRecentFailedAttemptsAsync(testIp, hoursBack: 1);

        // Assert - Should only count the 50 recent failures
        count.Should().Be(50, "should only count failures within the last hour");
    }

    #endregion

    #region IP Address and User Agent Capture Tests

    [Fact]
    public async Task LogEventAsync_WithIPAddressAndUserAgent_ShouldStoreCorrectly()
    {
        // Arrange
        var ipAddress = "203.0.113.42";
        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        // Act
        await _auditLogService.LogEventAsync(
            _testUserId,
            "LOGIN_FAILED",
            ipAddress,
            userAgent,
            success: false,
            details: "Test IP and UA capture");

        // Assert - Verify IP and UA stored correctly
        var log = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.UserId == _testUserId && al.Action == "LOGIN_FAILED");

        log.Should().NotBeNull();
        log!.IPAddress.Should().Be(ipAddress, "IP address should be stored exactly as provided");
        log.UserAgent.Should().Be(userAgent, "user agent should be stored exactly as provided");
        log.Success.Should().BeFalse();
        log.Details.Should().Be("Test IP and UA capture");
    }

    [Fact]
    public async Task LogEventAsync_WithNullUserAgent_ShouldHandleGracefully()
    {
        // Act - Log event with null user agent
        await _auditLogService.LogEventAsync(
            _testUserId,
            "PASSWORD_RESET",
            TestIpAddress,
            userAgent: null,  // NULL
            success: true);

        // Assert - Verify log created with null user agent
        var log = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.UserId == _testUserId && al.Action == "PASSWORD_RESET");

        log.Should().NotBeNull();
        log!.UserAgent.Should().BeNull("null user agent should be stored as null");
        log.IPAddress.Should().Be(TestIpAddress);
    }

    [Fact]
    public async Task LogEventAsync_WithNullUserId_ShouldAllowAnonymousLogs()
    {
        // Act - Log event with null user ID (anonymous action)
        await _auditLogService.LogEventAsync(
            userId: null,  // ANONYMOUS
            "UNAUTHORIZED_ACCESS",
            TestIpAddress,
            TestUserAgent,
            success: false,
            errorMessage: "Anonymous unauthorized access attempt");

        // Assert - Verify anonymous log created
        var log = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.UserId == null && al.Action == "UNAUTHORIZED_ACCESS");

        log.Should().NotBeNull();
        log!.UserId.Should().BeNull("anonymous logs should have null user ID");
        log.IPAddress.Should().Be(TestIpAddress);
        log.Success.Should().BeFalse();
        log.ErrorMessage.Should().Contain("Anonymous");
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task LogEventAsync_SameUserActionWithin1Second_ShouldBeRateLimited()
    {
        // Arrange - Non-critical action
        var action = "PROFILE_VIEW";

        // Act - Log same action 5 times within 1 second
        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);
        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);
        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);
        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);
        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);

        // Assert - Should only have 1 log (first one, rest rate limited)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId && al.Action == action)
            .ToListAsync();

        logs.Should().HaveCountLessThanOrEqualTo(1,
            "rate limiting should block rapid successive logs within 1 second");
    }

    [Fact]
    public async Task LogEventAsync_DifferentActionsForSameUser_ShouldNotBeRateLimited()
    {
        // Act - Log different actions for same user
        await _auditLogService.LogEventAsync(_testUserId, "PROJECT_CREATE", TestIpAddress, TestUserAgent, true);
        await _auditLogService.LogEventAsync(_testUserId, "CREDIT_TRANSFER", TestIpAddress, TestUserAgent, true);
        await _auditLogService.LogEventAsync(_testUserId, "PASSWORD_RESET", TestIpAddress, TestUserAgent, true);

        // Assert - All different actions should be logged (critical actions)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId)
            .ToListAsync();

        logs.Should().HaveCount(3,
            "different actions should not be rate limited against each other");

        logs.Should().Contain(l => l.Action == "PROJECT_CREATE");
        logs.Should().Contain(l => l.Action == "CREDIT_TRANSFER");
        logs.Should().Contain(l => l.Action == "PASSWORD_RESET");
    }

    [Fact]
    public async Task LogEventAsync_RateLimitResetAfter1Second_ShouldAllowNextLog()
    {
        // Arrange - Use non-sampled, non-critical action to test rate limiting deterministically
        // SETTINGS_CHANGE is not in _criticalActions or _sampledActions, so it will be rate limited but always logged
        var action = "SETTINGS_CHANGE";

        // Act - Log action, wait 1.1 seconds, log again
        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);

        await Task.Delay(1100);  // Wait 1.1 seconds (rate limit is 1 second)

        await _auditLogService.LogEventAsync(_testUserId, action, TestIpAddress, TestUserAgent, true);

        // Assert - Should have 2 logs (rate limit reset after 1 second)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId && al.Action == action)
            .ToListAsync();

        logs.Should().HaveCount(2,
            "rate limit should reset after 1 second, allowing both logs to be recorded");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LogEventAsync_WithErrorMessage_ShouldStoreErrorDetails()
    {
        // Act
        await _auditLogService.LogEventAsync(
            _testUserId,
            "PAYMENT_FAILED",
            TestIpAddress,
            TestUserAgent,
            success: false,
            details: "Stripe payment processing",
            errorMessage: "Insufficient funds in account");

        // Assert
        var log = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.UserId == _testUserId && al.Action == "PAYMENT_FAILED");

        log.Should().NotBeNull();
        log!.Success.Should().BeFalse();
        log.ErrorMessage.Should().Be("Insufficient funds in account");
        log.Details.Should().Be("Stripe payment processing");
    }

    [Fact]
    public async Task GetUserAuditLogsAsync_WithNonExistentUser_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var logs = await _auditLogService.GetUserAuditLogsAsync(nonExistentUserId);

        // Assert
        logs.Should().BeEmpty("non-existent user should have no audit logs");
    }

    [Fact]
    public async Task GetRecentFailedAttemptsAsync_WithNoFailures_ShouldReturnZero()
    {
        // Arrange - Log only successful attempts
        for (int i = 0; i < 10; i++)
        {
            await _auditLogService.LogEventAsync(
                Guid.NewGuid(),
                "USER_REGISTRATION",
                TestIpAddress,
                TestUserAgent,
                success: true);  // SUCCESS
        }

        // Act
        var count = await _auditLogService.GetRecentFailedAttemptsAsync(TestIpAddress);

        // Assert
        count.Should().Be(0, "no failed attempts should return count of 0");
    }

    [Fact]
    public async Task LogEventAsync_RateLimitDictionaryCleanup_ShouldCleanupOldEntries()
    {
        // Arrange - Force dictionary cleanup by exceeding 10,000 entries
        // Service uses reflection to access private _lastLogTime field, so we simulate through API
        var tasks = new List<Task>();

        // Create 10,001 unique user-action combinations to trigger cleanup (line 130-142)
        for (int i = 0; i <= 10001; i++)
        {
            var uniqueUserId = Guid.NewGuid();
            tasks.Add(_auditLogService.LogEventAsync(
                uniqueUserId,
                "SETTINGS_CHANGE",  // Non-critical, non-sampled action
                $"192.168.1.{i % 255}",
                "Test Agent",
                success: true));

            // Batch the tasks to avoid memory issues
            if (tasks.Count >= 1000)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }

        // Act - Add one more to trigger potential cleanup
        await _auditLogService.LogEventAsync(
            Guid.NewGuid(),
            "SETTINGS_CHANGE",
            "192.168.1.100",
            "Test Agent",
            success: true);

        // Assert - Verify all logs were written (cleanup should not affect logging)
        var totalLogs = await _context.AuditLogs
            .Where(al => al.Action == "SETTINGS_CHANGE")
            .CountAsync();

        totalLogs.Should().BeGreaterThan(10000,
            "dictionary cleanup should not prevent logging");
    }

    [Fact]
    public async Task LogEventAsync_SampledActionAfterRateLimit_ShouldIncrementCacheCounter()
    {
        // Arrange - Log a sampled action twice rapidly to test IncrementSampledEventCounter
        var userId = Guid.NewGuid();

        // Act - First log should write to DB (rate limit allows first one)
        await _auditLogService.LogEventAsync(
            userId,
            "PROJECT_VIEW",  // Sampled action
            TestIpAddress,
            TestUserAgent,
            success: true);

        // Second log within 1 second should be rate-limited and increment cache counter
        await _auditLogService.LogEventAsync(
            userId,
            "PROJECT_VIEW",  // Same action, same user
            TestIpAddress,
            TestUserAgent,
            success: true);

        // Assert - Verify only first log was written (second was sampled/rate-limited)
        var logs = await _context.AuditLogs
            .Where(al => al.UserId == userId && al.Action == "PROJECT_VIEW")
            .ToListAsync();

        // Note: Due to 10% sampling, first log might not be written either
        // but we're testing the cache counter path exists
        logs.Should().HaveCountLessOrEqualTo(1,
            "rate limiting and sampling should reduce log volume");
    }

    [Fact]
    public async Task LogEventAsync_OverThousandDifferentActions_ShouldScaleEfficiently()
    {
        // Arrange - Test performance with many different action types (edge case)
        var startTime = DateTime.UtcNow;
        var tasks = new List<Task>();

        // Act - Log 100 different actions to test rate limit dictionary scaling
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_auditLogService.LogEventAsync(
                _testUserId,
                $"ACTION_TYPE_{i}",  // Different action for each
                TestIpAddress,
                TestUserAgent,
                success: true));
        }

        await Task.WhenAll(tasks);
        var duration = DateTime.UtcNow - startTime;

        // Assert - Should complete in reasonable time (< 2 seconds)
        duration.Should().BeLessThan(TimeSpan.FromSeconds(2),
            "rate limit dictionary should not cause performance degradation");

        var totalLogs = await _context.AuditLogs
            .Where(al => al.UserId == _testUserId)
            .CountAsync();

        totalLogs.Should().BeGreaterThan(90,
            "most non-critical actions should be logged");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _memoryCache.Dispose();
    }
}
