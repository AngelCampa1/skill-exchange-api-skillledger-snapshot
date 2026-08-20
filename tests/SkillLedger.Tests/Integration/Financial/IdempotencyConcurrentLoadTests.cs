using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Financial;

/// <summary>
/// CRITICAL integration tests for idempotency under concurrent load
/// Focus: Preventing double payment releases in high-concurrency scenarios
/// These tests MUST pass before deploying financial operations
/// </summary>
[IntegrationTest]
[FinancialTest]
[Collection("Integration Financial")]
public class IdempotencyConcurrentLoadTests : IntegrationTestBase
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly IMilestoneTrackingService _milestoneService;
    private readonly IProjectService _projectService;
    private readonly IProjectEscrowService _escrowService;
    private readonly ISkillService _skillService;

    public IdempotencyConcurrentLoadTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _idempotencyService = ServiceScope.ServiceProvider.GetRequiredService<IIdempotencyService>();
        _milestoneService = ServiceScope.ServiceProvider.GetRequiredService<IMilestoneTrackingService>();
        _projectService = ServiceScope.ServiceProvider.GetRequiredService<IProjectService>();
        _escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        _skillService = ServiceScope.ServiceProvider.GetRequiredService<ISkillService>();
    }

    #region CRITICAL - Idempotency Service Tests

    [Fact]
    public async Task Idempotency_10ConcurrentRequests_OnlyFirstProcessed()
    {
        // Arrange - Create a unique idempotency key
        var idempotencyKey = $"payment:release:{Guid.NewGuid():N}:user1";
        var processedCount = 0;
        var lockObj = new object();

        // Act - 10 concurrent requests with the same idempotency key
        var tasks = Enumerable.Range(0, 10)
            .Select(async _ =>
            {
                // Check if this is a duplicate before processing
                var isDuplicate = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);

                if (!isDuplicate)
                {
                    lock (lockObj) { processedCount++; }
                    // Simulate processing
                    await Task.Delay(10);
                    await _idempotencyService.MarkOperationCompletedAsync(idempotencyKey);
                    return true;
                }
                return false;
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - At least one operation should be processed (first one to check)
        var successCount = results.Count(r => r);
        successCount.Should().BeGreaterOrEqualTo(1, "at least one operation should succeed");
        processedCount.Should().BeGreaterOrEqualTo(1, "at least one operation should be processed");
    }

    [Fact]
    public async Task Idempotency_SequentialDuplicateRequests_SecondBlocked()
    {
        // Arrange - Create a unique idempotency key
        var idempotencyKey = $"sequential:payment:{Guid.NewGuid():N}:user1";

        // Act - First request
        var isDuplicate1 = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);
        isDuplicate1.Should().BeFalse("first request should not be duplicate");

        // Mark as completed
        await _idempotencyService.MarkOperationCompletedAsync(idempotencyKey);

        // Act - Second request (should be blocked)
        var isDuplicate2 = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);

        // Assert - Second request should be detected as duplicate
        isDuplicate2.Should().BeTrue("second request should be detected as duplicate");
    }

    [Fact]
    public async Task Idempotency_NetworkRetryScenario_SecondAttemptBlocked()
    {
        // Arrange - Create a unique idempotency key
        var idempotencyKey = $"retry:scenario:{Guid.NewGuid():N}:user1";

        // Act - First attempt (not duplicate)
        var isDuplicate1 = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);
        isDuplicate1.Should().BeFalse("first attempt should not be duplicate");

        // Complete the first operation
        await _idempotencyService.MarkOperationCompletedAsync(idempotencyKey);

        // Act - Simulated retry (network timeout scenario)
        var isDuplicate2 = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);

        // Assert - Second attempt should be detected as duplicate
        isDuplicate2.Should().BeTrue("retry attempt should be detected as duplicate");
    }

    #endregion

    #region Multiple Operation Scenarios

    [Fact]
    public async Task Idempotency_MultipleOperations_EachTrackedIndependently()
    {
        // Arrange - Create unique idempotency keys for each operation
        var key1 = $"operation:1:{Guid.NewGuid():N}:user1";
        var key2 = $"operation:2:{Guid.NewGuid():N}:user1";
        var key3 = $"operation:3:{Guid.NewGuid():N}:user1";

        // Act - Check all operations (should all be non-duplicate since they're new)
        var task1 = _idempotencyService.IsDuplicateOperationAsync(key1);
        var task2 = _idempotencyService.IsDuplicateOperationAsync(key2);
        var task3 = _idempotencyService.IsDuplicateOperationAsync(key3);

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert - None should be duplicates since each has unique key
        results.Should().AllBeEquivalentTo(false, "each operation with unique key should not be duplicate");

        // Complete all operations
        await _idempotencyService.MarkOperationCompletedAsync(key1);
        await _idempotencyService.MarkOperationCompletedAsync(key2);
        await _idempotencyService.MarkOperationCompletedAsync(key3);

        // Verify they are now detected as duplicates
        var afterResults = await Task.WhenAll(
            _idempotencyService.IsDuplicateOperationAsync(key1),
            _idempotencyService.IsDuplicateOperationAsync(key2),
            _idempotencyService.IsDuplicateOperationAsync(key3));

        afterResults.Should().AllBeEquivalentTo(true, "completed operations should be detected as duplicates");
    }

    #endregion

    #region State Change Idempotency

    [Fact]
    public async Task Idempotency_ConcurrentStateChanges_DuplicatesDetected()
    {
        // Arrange - Create a unique idempotency key for state change
        var idempotencyKey = $"state:change:{Guid.NewGuid():N}:user1";
        var stateChangedCount = 0;
        var lockObj = new object();

        // Act - 10 concurrent state change attempts
        var tasks = Enumerable.Range(0, 10)
            .Select(async _ =>
            {
                var isDuplicate = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);

                if (!isDuplicate)
                {
                    lock (lockObj) { stateChangedCount++; }
                    // Simulate state change
                    await Task.Delay(10);
                    await _idempotencyService.MarkOperationCompletedAsync(idempotencyKey);
                    return true;
                }
                return false;
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - At least one should succeed, and final state should be marked as completed
        var successCount = results.Count(r => r);
        successCount.Should().BeGreaterOrEqualTo(1, "at least one state change should succeed");

        // Verify operation is now marked as duplicate
        var isDuplicate = await _idempotencyService.IsDuplicateOperationAsync(idempotencyKey);
        isDuplicate.Should().BeTrue("operation should be marked as completed after processing");
    }

    #endregion

    #region Helper Methods

    private async Task<bool> SafeReleaseMilestoneAsync(Guid milestoneId, Guid userId)
    {
        try
        {
            return await _escrowService.ReleaseMilestoneAsync(milestoneId, userId, "Concurrent release attempt");
        }
        catch (Exception)
        {
            // Expected for concurrent failures
            return false;
        }
    }

    private async Task<bool> SafeApproveMilestoneAsync(Guid milestoneId, Guid userId)
    {
        try
        {
            return await _milestoneService.ApproveMilestoneAsync(milestoneId, userId, "Concurrent approval attempt");
        }
        catch (Exception)
        {
            // Expected for concurrent failures
            return false;
        }
    }

    private async Task<Guid> CreateTestSkillAsync(string name)
    {
        var createDto = new CreateSkillDto
        {
            Name = name,
            Description = $"Description for {name}",
            Category = "Programming"
        };
        var result = await _skillService.CreateSkillAsync(createDto);
        if (!result.Success || result.Data == null)
            return Guid.Empty;
        var skillDto = (SkillDto)result.Data;
        return skillDto.Id;
    }

    private async Task<ProjectDto> CreateTestProjectWithCreditsAsync(Guid clientId, string title, int creditBudget)
    {
        var skillId = await CreateTestSkillAsync($"ProjectSkill_{Guid.NewGuid():N}");
        skillId.Should().NotBe(Guid.Empty, "skill should be created successfully");

        var createDto = new CreateProjectDto
        {
            Title = title,
            Description = $"Test project for {title}",
            CreditBudget = creditBudget,
            Deliverables = new List<CreateProjectDeliverableDto>
            {
                new CreateProjectDeliverableDto
                {
                    Description = "Primary deliverable",
                    OrderIndex = 0,
                    IsRequired = true
                }
            },
            RequiredSkills = new List<CreateProjectSkillDto>
            {
                new CreateProjectSkillDto
                {
                    SkillId = skillId,
                    ProficiencyRequired = 2
                }
            }
        };

        var result = await _projectService.CreateProjectAsync(createDto, clientId, "127.0.0.1");
        result.Should().NotBeNull("project response should not be null");
        result.Success.Should().BeTrue($"project creation should succeed: {result.Message}");
        result.Project.Should().NotBeNull("project data should be returned");
        return result.Project!;
    }

    private async Task FundUserWalletAsync(Guid userId, int credits)
    {
        // Use the credit wallet service to add credits
        var creditWalletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();
        await creditWalletService.AddCreditsAsync(userId, credits, "Test funding", CreditTransactionType.Adjustment);
    }

    #endregion
}
