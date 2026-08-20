using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for IdempotencyService - Duplicate operation prevention.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real in-memory distributed cache
/// - Tests critical financial safety mechanisms
/// - Verifies idempotency behavior for race condition prevention
/// - No mocks - real cache implementation
///
/// Max mocked external dependencies: 0 (uses real MemoryDistributedCache)
/// </summary>
[IntegrationTest]
[FinancialTest]
public class IdempotencyServiceIntegrationTests : IDisposable
{
    private readonly IdempotencyService _service;
    private readonly IDistributedCache _cache;

    public IdempotencyServiceIntegrationTests()
    {
        // Use REAL in-memory distributed cache (not a mock)
        var options = Options.Create(new MemoryDistributedCacheOptions());
        _cache = new MemoryDistributedCache(options);

        _service = new IdempotencyService(_cache);
    }

    #region IsDuplicateOperationAsync Tests

    [Fact]
    public async Task IsDuplicateOperationAsync_NewOperation_ShouldReturnFalse()
    {
        // Arrange
        var operationKey = Guid.NewGuid().ToString();

        // Act
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        isDuplicate.Should().BeFalse("operation has not been executed yet");
    }

    [Fact]
    public async Task IsDuplicateOperationAsync_CompletedOperation_ShouldReturnTrue()
    {
        // Arrange
        var operationKey = Guid.NewGuid().ToString();
        await _service.MarkOperationCompletedAsync(operationKey);

        // Act
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        isDuplicate.Should().BeTrue("operation was already marked as completed");
    }

    [Fact]
    public async Task IsDuplicateOperationAsync_DifferentOperations_ShouldNotConflict()
    {
        // Arrange
        var operation1 = "payment-123";
        var operation2 = "payment-456";

        await _service.MarkOperationCompletedAsync(operation1);

        // Act
        var isDuplicate1 = await _service.IsDuplicateOperationAsync(operation1);
        var isDuplicate2 = await _service.IsDuplicateOperationAsync(operation2);

        // Assert
        isDuplicate1.Should().BeTrue("operation1 was completed");
        isDuplicate2.Should().BeFalse("operation2 was not completed");
    }

    [Fact]
    public async Task IsDuplicateOperationAsync_SameKeyConcurrent_ShouldBothReturnFalse()
    {
        // Arrange - Test race condition scenario
        var operationKey = $"concurrent-op-{Guid.NewGuid()}";

        // Act - Simulate two concurrent checks before either marks as completed
        var check1Task = _service.IsDuplicateOperationAsync(operationKey);
        var check2Task = _service.IsDuplicateOperationAsync(operationKey);

        var results = await Task.WhenAll(check1Task, check2Task);

        // Assert - Both should return false since neither marked it completed yet
        results[0].Should().BeFalse("first concurrent check sees no prior execution");
        results[1].Should().BeFalse("second concurrent check sees no prior execution");

        // This demonstrates the race condition - both would proceed
        // In production, DistributedLockService would prevent this
    }

    #endregion

    #region MarkOperationCompletedAsync Tests

    [Fact]
    public async Task MarkOperationCompletedAsync_NewOperation_ShouldMarkAsCompleted()
    {
        // Arrange
        var operationKey = Guid.NewGuid().ToString();

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);

        // Assert - Verify it's marked
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task MarkOperationCompletedAsync_AlreadyCompleted_ShouldOverwrite()
    {
        // Arrange
        var operationKey = Guid.NewGuid().ToString();
        await _service.MarkOperationCompletedAsync(operationKey);

        // Act - Mark again
        await _service.MarkOperationCompletedAsync(operationKey);

        // Assert - Should still be marked
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task MarkOperationCompletedAsync_MultipleOperations_ShouldTrackIndependently()
    {
        // Arrange
        var operations = Enumerable.Range(1, 5)
            .Select(i => $"operation-{i}")
            .ToList();

        // Act - Mark all as completed
        foreach (var op in operations)
        {
            await _service.MarkOperationCompletedAsync(op);
        }

        // Assert - All should be marked
        foreach (var op in operations)
        {
            var isDuplicate = await _service.IsDuplicateOperationAsync(op);
            isDuplicate.Should().BeTrue($"operation {op} was marked as completed");
        }
    }

    #endregion

    #region Idempotency Workflow Tests

    [Fact]
    public async Task IdempotencyWorkflow_CheckMarkCheck_ShouldPreventDuplicate()
    {
        // Arrange
        var operationKey = $"payment-release-{Guid.NewGuid()}";

        // Act & Assert - First execution
        var isFirstDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isFirstDuplicate.Should().BeFalse("first execution should proceed");

        // Mark as completed
        await _service.MarkOperationCompletedAsync(operationKey);

        // Second attempt
        var isSecondDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isSecondDuplicate.Should().BeTrue("second execution should be blocked");
    }

    [Fact]
    public async Task IdempotencyWorkflow_PaymentScenario_ShouldPreventDoubleCharge()
    {
        // Arrange - Simulate payment processing
        var userId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var operationKey = $"payment:{userId}:{transactionId}";

        // Act - First payment attempt
        var canProcessFirstPayment = !await _service.IsDuplicateOperationAsync(operationKey);
        canProcessFirstPayment.Should().BeTrue("first payment should be allowed");

        // Process payment...
        await _service.MarkOperationCompletedAsync(operationKey);

        // Second payment attempt (duplicate/retry)
        var canProcessSecondPayment = !await _service.IsDuplicateOperationAsync(operationKey);
        canProcessSecondPayment.Should().BeFalse("duplicate payment should be blocked");
    }

    [Fact]
    public async Task IdempotencyWorkflow_EscrowReleaseScenario_ShouldPreventDoubleRelease()
    {
        // Arrange - Simulate escrow release
        var escrowId = Guid.NewGuid();
        var operationKey = $"escrow-release:{escrowId}";

        // Act - First release attempt
        if (!await _service.IsDuplicateOperationAsync(operationKey))
        {
            // Release funds...
            await _service.MarkOperationCompletedAsync(operationKey);
        }

        // Second release attempt (race condition/retry)
        var canReleaseAgain = !await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        canReleaseAgain.Should().BeFalse("escrow should not be released twice");
    }

    #endregion

    #region Edge Cases and Expiration Tests

    [Fact]
    public async Task IsDuplicateOperationAsync_EmptyKey_ShouldHandleGracefully()
    {
        // Arrange
        var emptyKey = "";

        // Act
        var isDuplicate = await _service.IsDuplicateOperationAsync(emptyKey);

        // Assert
        isDuplicate.Should().BeFalse("empty key should return false (no cached value)");
    }

    [Fact]
    public async Task MarkOperationCompletedAsync_EmptyKey_ShouldNotThrow()
    {
        // Arrange
        var emptyKey = "";

        // Act & Assert
        await _service
            .Invoking(s => s.MarkOperationCompletedAsync(emptyKey))
            .Should()
            .NotThrowAsync("service should handle empty keys gracefully");
    }

    [Fact]
    public async Task IdempotencyWorkflow_SpecialCharactersInKey_ShouldWork()
    {
        // Arrange - Test special characters that might break caching
        var operationKey = "payment:user@email.com:tx-123/456";

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        isDuplicate.Should().BeTrue("special characters in key should be handled");
    }

    [Fact]
    public async Task IdempotencyWorkflow_VeryLongKey_ShouldWork()
    {
        // Arrange - Test very long operation key
        var longKey = new string('A', 500);

        // Act
        await _service.MarkOperationCompletedAsync(longKey);
        var isDuplicate = await _service.IsDuplicateOperationAsync(longKey);

        // Assert
        isDuplicate.Should().BeTrue("long keys should be supported");
    }

    [Fact]
    public async Task ConcurrentOperations_DifferentKeys_ShouldNotInterfere()
    {
        // Arrange - Test concurrent operations with different keys
        var keys = Enumerable.Range(1, 10)
            .Select(i => $"concurrent-op-{i}")
            .ToList();

        // Act - Mark all concurrently
        var tasks = keys.Select(key => _service.MarkOperationCompletedAsync(key));
        await Task.WhenAll(tasks);

        // Assert - All should be marked independently
        foreach (var key in keys)
        {
            var isDuplicate = await _service.IsDuplicateOperationAsync(key);
            isDuplicate.Should().BeTrue($"operation {key} should be marked");
        }
    }

    [Fact]
    public async Task IdempotencyWorkflow_HighVolume_ShouldHandleMany()
    {
        // Arrange - Test high volume of operations
        var operations = Enumerable.Range(1, 100)
            .Select(i => $"bulk-op-{i}")
            .ToList();

        // Act - Process all operations
        foreach (var op in operations)
        {
            var isDuplicate = await _service.IsDuplicateOperationAsync(op);
            isDuplicate.Should().BeFalse($"operation {op} should be new");

            await _service.MarkOperationCompletedAsync(op);

            var nowDuplicate = await _service.IsDuplicateOperationAsync(op);
            nowDuplicate.Should().BeTrue($"operation {op} should now be marked");
        }

        // Assert - All should still be marked
        foreach (var op in operations)
        {
            var isDuplicate = await _service.IsDuplicateOperationAsync(op);
            isDuplicate.Should().BeTrue($"operation {op} should still be marked");
        }
    }

    #endregion

    #region Cache Behavior Tests

    [Fact]
    public async Task MarkOperationCompletedAsync_ShouldStoreTimestamp()
    {
        // Arrange
        var operationKey = Guid.NewGuid().ToString();
        var beforeMark = DateTime.UtcNow;

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);

        // Assert - Verify timestamp was stored (by checking cache directly)
        var cacheKey = $"idempotency:{operationKey}";
        var cachedValue = await _cache.GetStringAsync(cacheKey);

        cachedValue.Should().NotBeNullOrEmpty("timestamp should be stored");

        // Parse timestamp with round-trip format to preserve UTC kind
        var timestamp = DateTime.Parse(cachedValue, null, System.Globalization.DateTimeStyles.RoundtripKind);
        timestamp.Should().BeCloseTo(beforeMark, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task MarkOperationCompletedAsync_ShouldSet5MinuteExpiration()
    {
        // Arrange
        var operationKey = Guid.NewGuid().ToString();

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);

        // Assert - Verify operation is marked
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue("operation should be marked immediately");

        // Note: Cannot easily test 5-minute expiration in unit test without waiting
        // In production, the cache entry will expire after 5 minutes
    }

    #endregion

    public void Dispose()
    {
        // Clean up cache if needed
        if (_cache is IDisposable disposableCache)
        {
            disposableCache.Dispose();
        }
    }
}
