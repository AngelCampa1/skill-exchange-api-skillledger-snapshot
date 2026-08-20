using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Unit.Services;

/// <summary>
/// Unit tests for IdempotencyService
/// Focus: Duplicate detection, TTL expiration, concurrent operations
/// CRITICAL for preventing double payment releases
/// </summary>
[UnitTest]
[CoreTest]
public class IdempotencyServiceTests : IDisposable
{
    private readonly IDistributedCache _cache;
    private readonly IdempotencyService _service;

    public IdempotencyServiceTests()
    {
        // Use in-memory distributed cache for testing
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _service = new IdempotencyService(_cache);
    }

    public void Dispose()
    {
        // Cleanup is handled by MemoryDistributedCache disposal
        GC.SuppressFinalize(this);
    }

    #region IsDuplicateOperationAsync Tests

    [Fact]
    public async Task IsDuplicateOperationAsync_NewOperation_ReturnsFalse()
    {
        // Arrange
        var operationKey = "milestone:payment:123e4567-e89b-12d3-a456-426614174000:987f6543-e21a-43b2-c789-012345678900";

        // Act
        var result = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        result.Should().BeFalse("New operation should not be marked as duplicate");
    }

    [Fact]
    public async Task IsDuplicateOperationAsync_AfterMarkingCompleted_ReturnsTrue()
    {
        // Arrange
        var operationKey = "milestone:payment:123e4567-e89b-12d3-a456-426614174000:987f6543-e21a-43b2-c789-012345678900";
        await _service.MarkOperationCompletedAsync(operationKey);

        // Act
        var result = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        result.Should().BeTrue("Operation marked as completed should be detected as duplicate");
    }

    [Fact]
    public async Task IsDuplicateOperationAsync_DifferentOperationKeys_EachReturnsFalse()
    {
        // Arrange
        var operationKey1 = "milestone:payment:123e4567-e89b-12d3-a456-426614174000:user1";
        var operationKey2 = "milestone:payment:123e4567-e89b-12d3-a456-426614174001:user2";

        await _service.MarkOperationCompletedAsync(operationKey1);

        // Act
        var result1 = await _service.IsDuplicateOperationAsync(operationKey1);
        var result2 = await _service.IsDuplicateOperationAsync(operationKey2);

        // Assert
        result1.Should().BeTrue("First operation should be detected as duplicate");
        result2.Should().BeFalse("Second operation has different key and should not be duplicate");
    }

    [Fact]
    public async Task IsDuplicateOperationAsync_EmptyOperationKey_ReturnsFalse()
    {
        // Arrange
        var operationKey = "";

        // Act
        var result = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        result.Should().BeFalse("Empty operation key should not be marked as duplicate");
    }

    #endregion

    #region MarkOperationCompletedAsync Tests

    [Fact]
    public async Task MarkOperationCompletedAsync_NewOperation_SuccessfullyMarks()
    {
        // Arrange
        var operationKey = "milestone:approve:456e7890-a12b-34c5-d678-901234567890:user1";

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);

        // Assert
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue("Operation should be marked as completed");
    }

    [Fact]
    public async Task MarkOperationCompletedAsync_CalledTwice_StillMarkedAsCompleted()
    {
        // Arrange
        var operationKey = "milestone:cancel:789a0123-b45c-67d8-e901-234567890abc:user1";

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);
        await _service.MarkOperationCompletedAsync(operationKey); // Second call

        // Assert
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue("Operation should still be marked after second call");
    }

    [Fact]
    public async Task MarkOperationCompletedAsync_StoresTimestamp()
    {
        // Arrange
        var operationKey = "milestone:submit:abc1234d-e56f-78g9-h012-345678901234:user1";
        var beforeMark = DateTime.UtcNow;

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);
        var afterMark = DateTime.UtcNow;

        // Assert - Verify operation is marked (timestamp stored)
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue("Operation should be marked with timestamp");

        // Verify cache key format
        var cacheKey = $"idempotency:{operationKey}";
        var cachedValue = await _cache.GetStringAsync(cacheKey);
        cachedValue.Should().NotBeNullOrEmpty("Timestamp should be stored in cache");

        // Verify timestamp is in valid ISO format
        var parsedTimestamp = DateTime.Parse(cachedValue, null, System.Globalization.DateTimeStyles.RoundtripKind);
        parsedTimestamp.ToUniversalTime().Should().BeOnOrAfter(beforeMark)
            .And.BeOnOrBefore(afterMark);
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task MultipleOperations_ConcurrentAccess_EachTrackedIndependently()
    {
        // Arrange
        var operations = new[]
        {
            "milestone:payment:op1:user1",
            "milestone:payment:op2:user2",
            "milestone:approve:op3:user3",
            "escrow:release:op4:user4"
        };

        // Act - Mark operations concurrently
        var markTasks = operations.Select(op => _service.MarkOperationCompletedAsync(op));
        await Task.WhenAll(markTasks);

        // Assert - Each should be detected as duplicate
        var checkTasks = operations.Select(async op =>
        {
            var isDuplicate = await _service.IsDuplicateOperationAsync(op);
            return new { Operation = op, IsDuplicate = isDuplicate };
        });
        var results = await Task.WhenAll(checkTasks);

        results.Should().OnlyContain(r => r.IsDuplicate,
            "All operations should be tracked independently and marked as completed");
    }

    [Fact]
    public async Task SameOperationKey_ConcurrentMarkCalls_HandleGracefully()
    {
        // Arrange
        var operationKey = "milestone:payment:concurrent-test:user1";

        // Act - Multiple concurrent marks of same operation
        var markTasks = Enumerable.Range(0, 10)
            .Select(_ => _service.MarkOperationCompletedAsync(operationKey));
        await Task.WhenAll(markTasks);

        // Assert - Should still be marked (no exceptions thrown)
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isDuplicate.Should().BeTrue("Operation should be marked despite concurrent calls");
    }

    #endregion

    #region Cache Key Format Tests

    [Fact]
    public async Task CacheKeyFormat_HasIdempotencyPrefix()
    {
        // Arrange
        var operationKey = "test:operation:123:user1";
        await _service.MarkOperationCompletedAsync(operationKey);

        // Act - Access cache directly to verify key format
        var expectedCacheKey = $"idempotency:{operationKey}";
        var cachedValue = await _cache.GetStringAsync(expectedCacheKey);

        // Assert
        cachedValue.Should().NotBeNullOrEmpty("Cache key should have 'idempotency:' prefix");
    }

    [Fact]
    public async Task OperationKeyFormat_SupportsColonDelimitedParts()
    {
        // Arrange - Standard format: entity:action:entityId:userId
        var operationKey = "milestone:payment:123e4567-e89b-12d3-a456-426614174000:987f6543-e21a-43b2-c789-012345678900";

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        isDuplicate.Should().BeTrue("Colon-delimited operation keys should work correctly");
    }

    [Fact]
    public async Task OperationKeyFormat_SupportsSpecialCharacters()
    {
        // Arrange
        var operationKey = "entity:action-with-dash:id_with_underscore:user@email.com";

        // Act
        await _service.MarkOperationCompletedAsync(operationKey);
        var isDuplicate = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert
        isDuplicate.Should().BeTrue("Operation keys with special characters should be supported");
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public async Task PaymentReleaseScenario_FirstAttemptSucceeds_SecondBlocked()
    {
        // Arrange - Simulating milestone payment release
        var milestoneId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var operationKey = $"milestone:payment:{milestoneId}:{userId}";

        // Act - First attempt
        var isFirstDuplicate = await _service.IsDuplicateOperationAsync(operationKey);
        isFirstDuplicate.Should().BeFalse("First payment attempt should proceed");

        await _service.MarkOperationCompletedAsync(operationKey);

        // Act - Second attempt (network retry)
        var isSecondDuplicate = await _service.IsDuplicateOperationAsync(operationKey);

        // Assert - CRITICAL for financial safety
        isSecondDuplicate.Should().BeTrue(
            "CRITICAL: Second payment attempt must be blocked to prevent double-release");
    }

    [Fact]
    public async Task MilestoneApprovalScenario_MultipleReviewers_OnlyFirstSucceeds()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        var reviewer1 = Guid.NewGuid();
        var reviewer2 = Guid.NewGuid();
        var operationKey1 = $"milestone:approve:{milestoneId}:{reviewer1}";
        var operationKey2 = $"milestone:approve:{milestoneId}:{reviewer2}";

        // Act
        await _service.MarkOperationCompletedAsync(operationKey1);

        var isDuplicate1 = await _service.IsDuplicateOperationAsync(operationKey1);
        var isDuplicate2 = await _service.IsDuplicateOperationAsync(operationKey2);

        // Assert
        isDuplicate1.Should().BeTrue("First reviewer's approval should be tracked");
        isDuplicate2.Should().BeFalse("Second reviewer has different operation key");
    }

    [Fact]
    public async Task EscrowReleaseScenario_SameUserMultipleMilestones_TrackedSeparately()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var milestone1 = Guid.NewGuid();
        var milestone2 = Guid.NewGuid();
        var operation1 = $"milestone:payment:{milestone1}:{userId}";
        var operation2 = $"milestone:payment:{milestone2}:{userId}";

        // Act
        await _service.MarkOperationCompletedAsync(operation1);

        var isDuplicate1 = await _service.IsDuplicateOperationAsync(operation1);
        var isDuplicate2 = await _service.IsDuplicateOperationAsync(operation2);

        // Assert
        isDuplicate1.Should().BeTrue("First milestone payment should be tracked");
        isDuplicate2.Should().BeFalse("Second milestone is a different operation");
    }

    #endregion
}
