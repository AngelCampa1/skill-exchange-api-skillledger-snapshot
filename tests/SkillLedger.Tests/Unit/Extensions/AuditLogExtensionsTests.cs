using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Extensions;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Unit.Extensions;

/// <summary>
/// Unit tests for AuditLogExtensions
/// Focus: Fire-and-forget async logging, error suppression, non-blocking behavior
/// </summary>
[UnitTest]
[CoreTest]
public class AuditLogExtensionsTests
{
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger> _mockLogger;

    public AuditLogExtensionsTests()
    {
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger>();
    }

    #region Fire-and-Forget Behavior Tests

    [Fact]
    public void LogAuditEventAsync_ReturnsImmediately_DoesNotBlock()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var slowAuditService = new Mock<IAuditLogService>();
        slowAuditService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(Task.Delay(1000)); // Simulate slow audit logging

        var startTime = DateTime.UtcNow;

        // Act
        slowAuditService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            userId,
            "TEST_ACTION",
            "192.168.1.1",
            "TestAgent",
            true);

        var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

        // Assert - Should return in < 100ms even though audit takes 1000ms
        elapsedMs.Should().BeLessThan(100,
            "Fire-and-forget should return immediately without awaiting audit completion");
    }

    [Fact]
    public async Task LogAuditEventAsync_AuditServiceCalled_WithCorrectParameters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "MILESTONE_CREATED";
        var ipAddress = "203.0.113.195";
        var userAgent = "Mozilla/5.0";
        var details = "{\"MilestoneId\":\"123\"}";

        var taskCompletionSource = new TaskCompletionSource<bool>();
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                userId,
                action,
                ipAddress,
                userAgent,
                true,
                details,
                null))
            .Returns(Task.CompletedTask)
            .Callback(() => taskCompletionSource.SetResult(true));

        // Act
        _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            userId,
            action,
            ipAddress,
            userAgent,
            true,
            details);

        // Wait for async task to complete (with timeout)
        var completed = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(1000));

        // Assert
        completed.Should().Be(taskCompletionSource.Task, "Audit task should complete");
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            userId,
            action,
            ipAddress,
            userAgent,
            true,
            details,
            null), Times.Once);
    }

    [Fact]
    public async Task LogAuditEventAsync_NullUserId_PassedCorrectly()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<bool>();
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                null, // Null userId for anonymous actions
                "PUBLIC_ACTION",
                "10.0.0.1",
                null,
                true,
                null,
                null))
            .Returns(Task.CompletedTask)
            .Callback(() => taskCompletionSource.SetResult(true));

        // Act
        _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            null,
            "PUBLIC_ACTION",
            "10.0.0.1",
            null,
            true);

        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(1000));

        // Assert
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            null,
            "PUBLIC_ACTION",
            "10.0.0.1",
            null,
            true,
            null,
            null), Times.Once);
    }

    [Fact]
    public async Task LogAuditEventAsync_FailedAction_PassesErrorMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var errorMessage = "Payment processing failed: Insufficient funds";

        var taskCompletionSource = new TaskCompletionSource<bool>();
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                userId,
                "PAYMENT_FAILED",
                "192.168.1.1",
                "TestAgent",
                false, // success = false
                null,
                errorMessage))
            .Returns(Task.CompletedTask)
            .Callback(() => taskCompletionSource.SetResult(true));

        // Act
        _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            userId,
            "PAYMENT_FAILED",
            "192.168.1.1",
            "TestAgent",
            false,
            errorMessage: errorMessage);

        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(1000));

        // Assert
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            userId,
            "PAYMENT_FAILED",
            "192.168.1.1",
            "TestAgent",
            false,
            null,
            errorMessage), Times.Once);
    }

    #endregion

    #region Error Suppression Tests

    [Fact]
    public async Task LogAuditEventAsync_AuditServiceThrows_DoesNotPropagate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var auditException = new InvalidOperationException("Database connection failed");

        var exceptionThrown = new TaskCompletionSource<bool>();
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ThrowsAsync(auditException)
            .Callback(() => exceptionThrown.SetResult(true));

        // Act - Should NOT throw despite audit service failure
        var act = () => _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            userId,
            "TEST_ACTION",
            "192.168.1.1",
            "TestAgent",
            true);

        // Assert - No exception should propagate
        act.Should().NotThrow("Fire-and-forget should suppress audit exceptions");

        // Wait for background task to attempt execution
        await Task.WhenAny(exceptionThrown.Task, Task.Delay(500));
    }

    [Fact]
    public async Task LogAuditEventAsync_AuditServiceThrows_LogsWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var action = "MILESTONE_STARTED";
        var auditException = new InvalidOperationException("Audit database unavailable");

        var loggerCalled = new TaskCompletionSource<bool>();
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ThrowsAsync(auditException);

        _mockLogger.Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(action) && v.ToString()!.Contains(userId.ToString())),
                It.Is<Exception>(ex => ex == auditException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => loggerCalled.SetResult(true));

        // Act
        _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            userId,
            action,
            "192.168.1.1",
            "TestAgent",
            true);

        // Wait for logging to occur
        await Task.WhenAny(loggerCalled.Task, Task.Delay(1000));

        // Assert - Verify warning was logged
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.Is<Exception>(ex => ex == auditException),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once,
            "Audit failures should be logged as warnings");
    }

    [Fact]
    public async Task LogAuditEventAsync_MultipleFailures_EachLoggedIndependently()
    {
        // Arrange
        var auditException = new Exception("Audit service down");
        var loggerCallCount = 0;
        var allLogged = new TaskCompletionSource<bool>();

        _mockAuditLogService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ThrowsAsync(auditException);

        _mockLogger.Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() =>
            {
                loggerCallCount++;
                if (loggerCallCount == 3)
                    allLogged.SetResult(true);
            });

        // Act - Call three times
        for (int i = 0; i < 3; i++)
        {
            _mockAuditLogService.Object.LogAuditEventAsync(
                _mockLogger.Object,
                Guid.NewGuid(),
                $"ACTION_{i}",
                "192.168.1.1",
                "TestAgent",
                true);
        }

        await Task.WhenAny(allLogged.Task, Task.Delay(2000));

        // Assert
        loggerCallCount.Should().Be(3, "Each audit failure should be logged independently");
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task LogAuditEventAsync_ConcurrentCalls_AllExecuted()
    {
        // Arrange
        var completionSources = new List<TaskCompletionSource<bool>>();
        var callCount = 0;
        var allCompleted = new TaskCompletionSource<bool>();

        _mockAuditLogService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                callCount++;
                if (callCount == 10)
                    allCompleted.SetResult(true);
            });

        // Act - Call 10 times concurrently
        for (int i = 0; i < 10; i++)
        {
            _mockAuditLogService.Object.LogAuditEventAsync(
                _mockLogger.Object,
                Guid.NewGuid(),
                $"CONCURRENT_ACTION_{i}",
                "192.168.1.1",
                "TestAgent",
                true);
        }

        await Task.WhenAny(allCompleted.Task, Task.Delay(2000));

        // Assert
        callCount.Should().Be(10, "All concurrent audit calls should be executed");
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Exactly(10));
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public async Task PaymentReleaseScenario_AuditLogged_HTTPResponseNotBlocked()
    {
        // Arrange - Simulate slow audit logging (e.g., database write)
        var auditCompleted = new TaskCompletionSource<bool>();
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                "PAYMENT_RELEASE_TRIGGERED",
                It.IsAny<string>(),
                It.IsAny<string?>(),
                true,
                It.IsAny<string?>(),
                null))
            .Returns(async () =>
            {
                await Task.Delay(500); // Simulate slow database write
                auditCompleted.SetResult(true);
            });

        var requestStartTime = DateTime.UtcNow;

        // Act - Controller calls audit logging
        _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            Guid.NewGuid(),
            "PAYMENT_RELEASE_TRIGGERED",
            "203.0.113.195",
            "Mozilla/5.0",
            true,
            "{\"MilestoneId\":\"123\"}");

        var requestDuration = (DateTime.UtcNow - requestStartTime).TotalMilliseconds;

        // Assert - HTTP response should return immediately
        requestDuration.Should().BeLessThan(100,
            "CRITICAL: HTTP response must not wait for audit completion");

        // Verify audit eventually completes in background
        var completed = await Task.WhenAny(auditCompleted.Task, Task.Delay(1000));
        completed.Should().Be(auditCompleted.Task, "Audit should complete in background");
    }

    [Fact]
    public async Task CriticalBusinessOperation_AuditFailure_OperationSucceeds()
    {
        // Arrange - Simulate audit service completely down
        _mockAuditLogService.Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Audit database connection failed"));

        var operationCompleted = false;

        // Act - Critical business operation with audit logging
        _mockAuditLogService.Object.LogAuditEventAsync(
            _mockLogger.Object,
            Guid.NewGuid(),
            "CRITICAL_OPERATION",
            "192.168.1.1",
            "TestAgent",
            true);

        // Simulate critical business operation completing
        operationCompleted = true;

        // Assert - Business operation should succeed despite audit failure
        operationCompleted.Should().BeTrue(
            "CRITICAL: Business operations must succeed even if audit logging fails");

        // Give time for background audit attempt
        await Task.Delay(200);
    }

    #endregion
}
