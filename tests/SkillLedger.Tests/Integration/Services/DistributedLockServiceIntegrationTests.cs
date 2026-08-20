using FluentAssertions;
using Microsoft.Extensions.Logging;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for DistributedLockService - Race condition prevention.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real local lock implementation (Redis fallback mode)
/// - Tests CRITICAL race condition prevention for financial operations
/// - Verifies lock acquisition, expiration, and release
/// - No mocks - tests real lock behavior
///
/// Max mocked external dependencies: 0 (uses local locks, Redis=null)
/// Note: Redis is external, so testing with local fallback is acceptable
/// </summary>
[IntegrationTest]
[FinancialTest]
public class DistributedLockServiceIntegrationTests : IDisposable
{
    private readonly DistributedLockService _service;
    private readonly ILogger<DistributedLockService> _logger;

    public DistributedLockServiceIntegrationTests()
    {
        _logger = LoggerFactory
            .Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<DistributedLockService>();

        // Use local lock mode (Redis=null) to test fallback behavior
        _service = new DistributedLockService(redis: null, _logger);
    }

    #region TryAcquireLockAsync Tests

    [Fact]
    public async Task TryAcquireLockAsync_NewResource_ShouldAcquireLock()
    {
        // Arrange
        var resource = $"payment-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Assert
        lock1.Should().NotBeNull("lock should be acquired");
        lock1!.IsAcquired.Should().BeTrue();
        lock1.Resource.Should().Be(resource);
        lock1.AcquiredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        lock1.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.Add(expiration), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TryAcquireLockAsync_LockedResource_ShouldReturnNull()
    {
        // Arrange
        var resource = $"escrow-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Act - Try to acquire same resource
        var lock2 = await _service.TryAcquireLockAsync(resource, expiration);

        // Assert
        lock1.Should().NotBeNull("first lock should succeed");
        lock2.Should().BeNull("second lock should fail - resource is locked");
    }

    [Fact]
    public async Task TryAcquireLockAsync_EmptyResource_ShouldThrowException()
    {
        // Arrange
        var emptyResource = "";
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        await _service
            .Invoking(s => s.TryAcquireLockAsync(emptyResource, expiration))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Resource name cannot be empty*");
    }

    [Fact]
    public async Task TryAcquireLockAsync_NullResource_ShouldThrowException()
    {
        // Arrange
        string nullResource = null!;
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        await _service
            .Invoking(s => s.TryAcquireLockAsync(nullResource, expiration))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TryAcquireLockAsync_AfterDispose_ShouldAllowReacquisition()
    {
        // Arrange
        var resource = $"transaction-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        // Act - Acquire, dispose, then try again
        var lock1 = await _service.TryAcquireLockAsync(resource, expiration);
        await lock1!.DisposeAsync();

        var lock2 = await _service.TryAcquireLockAsync(resource, expiration);

        // Assert
        lock2.Should().NotBeNull("lock should be reacquired after first is disposed");
        lock2!.IsAcquired.Should().BeTrue();

        await lock2.DisposeAsync();
    }

    #endregion

    #region AcquireLockAsync Tests (With Retry)

    [Fact]
    public async Task AcquireLockAsync_NewResource_ShouldAcquireImmediately()
    {
        // Arrange
        var resource = $"payment-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromSeconds(10);

        // Act
        await using var lock1 = await _service.AcquireLockAsync(resource, expiration);

        // Assert
        lock1.Should().NotBeNull();
        lock1.IsAcquired.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireLockAsync_LockedResource_ShouldThrowTimeout()
    {
        // Arrange
        var resource = $"escrow-release-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);
        var waitTime = TimeSpan.FromMilliseconds(500);

        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Act & Assert - Should timeout trying to acquire locked resource
        await _service
            .Invoking(s => s.AcquireLockAsync(resource, expiration, waitTime))
            .Should()
            .ThrowAsync<TimeoutException>()
            .WithMessage($"*{resource}*");
    }

    [Fact]
    public async Task AcquireLockAsync_WithRetry_ShouldAcquireAfterRelease()
    {
        // Arrange
        var resource = $"concurrent-op-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromSeconds(1);
        var waitTime = TimeSpan.FromSeconds(3);

        var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Act - Start acquisition that will wait, then release first lock
        var lock2Task = _service.AcquireLockAsync(resource, expiration, waitTime);

        // Release first lock after short delay
        await Task.Delay(200);
        await lock1!.DisposeAsync();

        // Wait for second lock to be acquired
        await using var lock2 = await lock2Task;

        // Assert
        lock2.Should().NotBeNull("should acquire after first lock released");
        lock2.IsAcquired.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireLockAsync_EmptyResource_ShouldThrowException()
    {
        // Arrange
        var emptyResource = "";
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert
        await _service
            .Invoking(s => s.AcquireLockAsync(emptyResource, expiration))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsLockedAsync Tests

    [Fact]
    public async Task IsLockedAsync_UnlockedResource_ShouldReturnFalse()
    {
        // Arrange
        var resource = $"unlocked-{Guid.NewGuid()}";

        // Act
        var isLocked = await _service.IsLockedAsync(resource);

        // Assert
        isLocked.Should().BeFalse("resource was never locked");
    }

    [Fact]
    public async Task IsLockedAsync_LockedResource_ShouldReturnTrue()
    {
        // Arrange
        var resource = $"locked-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Act
        var isLocked = await _service.IsLockedAsync(resource);

        // Assert
        isLocked.Should().BeTrue("resource is currently locked");
    }

    [Fact]
    public async Task IsLockedAsync_AfterRelease_ShouldReturnFalse()
    {
        // Arrange
        var resource = $"released-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        var lock1 = await _service.TryAcquireLockAsync(resource, expiration);
        await lock1!.DisposeAsync();

        // Act
        var isLocked = await _service.IsLockedAsync(resource);

        // Assert
        isLocked.Should().BeFalse("resource was released");
    }

    [Fact]
    public async Task IsLockedAsync_ExpiredLock_ShouldReturnFalse()
    {
        // Arrange - Short expiration
        var resource = $"expiring-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMilliseconds(100);

        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Act - Wait for expiration
        await Task.Delay(200);
        var isLocked = await _service.IsLockedAsync(resource);

        // Assert
        isLocked.Should().BeFalse("lock should have expired");
    }

    [Fact]
    public async Task IsLockedAsync_EmptyResource_ShouldThrowException()
    {
        // Arrange
        var emptyResource = "";

        // Act & Assert
        await _service
            .Invoking(s => s.IsLockedAsync(emptyResource))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Lock Extension Tests

    [Fact]
    public async Task ExtendAsync_ActiveLock_ShouldExtend()
    {
        // Arrange
        var resource = $"extendable-{Guid.NewGuid()}";
        var initialExpiration = TimeSpan.FromSeconds(2);
        var extension = TimeSpan.FromSeconds(5);

        await using var lock1 = await _service.TryAcquireLockAsync(resource, initialExpiration);
        var initialExpiresAt = lock1!.ExpiresAt;

        // Act
        var extended = await lock1.ExtendAsync(extension);

        // Assert
        extended.Should().BeTrue("lock should be extendable");
        lock1.ExpiresAt.Should().BeAfter(initialExpiresAt!.Value);
    }

    [Fact]
    public async Task ExtendAsync_AfterDispose_ShouldReturnFalse()
    {
        // Arrange
        var resource = $"disposed-lock-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        var lock1 = await _service.TryAcquireLockAsync(resource, expiration);
        await lock1!.DisposeAsync();

        // Act
        var extended = await lock1.ExtendAsync(TimeSpan.FromMinutes(5));

        // Assert
        extended.Should().BeFalse("cannot extend disposed lock");
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task ConcurrentAcquisition_SameResource_OnlyOneShouldSucceed()
    {
        // Arrange
        var resource = $"race-condition-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMinutes(5);

        // Act - Concurrent attempts
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => _service.TryAcquireLockAsync(resource, expiration))
            .ToList();

        var locks = await Task.WhenAll(tasks);

        // Assert - Only one should succeed
        var successfulLocks = locks.Where(l => l != null).ToList();
        successfulLocks.Should().HaveCount(1, "only one concurrent acquisition should succeed");

        // Cleanup
        foreach (var lock1 in successfulLocks)
        {
            await lock1!.DisposeAsync();
        }
    }

    [Fact]
    public async Task ConcurrentOperations_DifferentResources_AllShouldSucceed()
    {
        // Arrange - Different resources
        var resources = Enumerable.Range(1, 10)
            .Select(i => $"resource-{i}-{Guid.NewGuid()}")
            .ToList();

        var expiration = TimeSpan.FromMinutes(5);

        // Act - Acquire all concurrently
        var tasks = resources.Select(r => _service.TryAcquireLockAsync(r, expiration));
        var locks = await Task.WhenAll(tasks);

        // Assert - All should succeed
        locks.Should().AllSatisfy(l => l.Should().NotBeNull());
        locks.Should().AllSatisfy(l => l!.IsAcquired.Should().BeTrue());

        // Cleanup
        foreach (var lock1 in locks)
        {
            await lock1!.DisposeAsync();
        }
    }

    [Fact]
    public async Task HighVolumeLocking_SequentialAcquireRelease_ShouldHandleMany()
    {
        // Arrange
        var resource = $"high-volume-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromSeconds(10);

        // Act - 100 sequential acquire/release cycles
        for (int i = 0; i < 100; i++)
        {
            await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

            lock1.Should().NotBeNull($"iteration {i} should acquire lock");
            lock1!.IsAcquired.Should().BeTrue();
        }

        // Assert - Final check
        var isLocked = await _service.IsLockedAsync(resource);
        isLocked.Should().BeFalse("all locks should be released");
    }

    #endregion

    #region Financial Operation Scenarios

    [Fact]
    public async Task PaymentProcessing_DuplicateRequest_SecondShouldWait()
    {
        // Arrange - Simulate duplicate payment request
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var resource = $"payment:{userId}:{transactionId}";
        var expiration = TimeSpan.FromSeconds(5);

        // Act - First request acquires lock
        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);
        lock1.Should().NotBeNull("first payment request should acquire lock");

        // Second request (duplicate) should fail to acquire
        var lock2 = await _service.TryAcquireLockAsync(resource, expiration);
        lock2.Should().BeNull("duplicate payment request should not acquire lock");

        // Assert - Verify resource is locked
        var isLocked = await _service.IsLockedAsync(resource);
        isLocked.Should().BeTrue("payment is being processed");
    }

    [Fact]
    public async Task EscrowRelease_ConcurrentAttempts_OnlyOneSucceeds()
    {
        // Arrange
        var escrowId = Guid.NewGuid();
        var resource = $"escrow-release:{escrowId}";
        var expiration = TimeSpan.FromSeconds(30);

        // Act - Simulate two concurrent release attempts
        var lock1Task = _service.TryAcquireLockAsync(resource, expiration);
        var lock2Task = _service.TryAcquireLockAsync(resource, expiration);

        var results = await Task.WhenAll(lock1Task, lock2Task);

        // Assert
        var successfulLocks = results.Where(r => r != null).ToList();
        successfulLocks.Should().HaveCount(1, "only one escrow release should proceed");

        // Cleanup
        foreach (var lock1 in successfulLocks)
        {
            await lock1!.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreditTransfer_WithRetry_ShouldComplete()
    {
        // Arrange - Simulate credit transfer that retries
        var transferId = Guid.NewGuid();
        var resource = $"credit-transfer:{transferId}";
        var expiration = TimeSpan.FromSeconds(2);
        var waitTime = TimeSpan.FromSeconds(5);

        // Simulate a blocking operation
        var blockingLock = await _service.TryAcquireLockAsync(resource, expiration);

        // Act - Start transfer that will wait for lock
        var transferTask = Task.Run(async () =>
        {
            await Task.Delay(500); // Simulate some processing before retry
            return await _service.AcquireLockAsync(resource, expiration, waitTime);
        });

        // Release blocking lock after short delay
        await Task.Delay(300);
        await blockingLock!.DisposeAsync();

        // Wait for transfer to acquire lock
        await using var transferLock = await transferTask;

        // Assert
        transferLock.Should().NotBeNull("transfer should eventually acquire lock");
        transferLock.IsAcquired.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Lock_VeryShortExpiration_ShouldStillWork()
    {
        // Arrange
        var resource = $"quick-lock-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromMilliseconds(10);

        // Act
        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Assert
        lock1.Should().NotBeNull("even very short locks should work");
        lock1!.IsAcquired.Should().BeTrue();
    }

    [Fact]
    public async Task Lock_VeryLongExpiration_ShouldStillWork()
    {
        // Arrange
        var resource = $"long-lock-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromHours(24);

        // Act
        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Assert
        lock1.Should().NotBeNull();
        lock1!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Lock_SpecialCharactersInResource_ShouldWork()
    {
        // Arrange
        var resource = "payment:user@email.com:tx-123/456:special!chars";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await using var lock1 = await _service.TryAcquireLockAsync(resource, expiration);

        // Assert
        lock1.Should().NotBeNull();
        lock1!.Resource.Should().Be(resource);
    }

    [Fact]
    public async Task Lock_VeryLongResourceName_ShouldWork()
    {
        // Arrange
        var longResource = new string('A', 500);
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await using var lock1 = await _service.TryAcquireLockAsync(longResource, expiration);

        // Assert
        lock1.Should().NotBeNull();
    }

    #endregion

    public void Dispose()
    {
        // Cleanup any remaining locks if needed
    }
}
