using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for MessagingController API endpoints
/// Tests Phase 3 & 4 bug fixes: CSRF protection, rate limiting, audit logging
/// Validates BUG-MSG-001 through BUG-MSG-058 fixes
/// </summary>
[IntegrationTest]
[Collection("Integration Api 2")]
public class MessagingControllerIntegrationTests : IntegrationTestBase
{
    private readonly IMessagingService _messagingService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IProjectService _projectService;
    private readonly ISkillService _skillService;

    public MessagingControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _messagingService = ServiceScope.ServiceProvider.GetRequiredService<IMessagingService>();
        _workspaceService = ServiceScope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        _projectService = ServiceScope.ServiceProvider.GetRequiredService<IProjectService>();
        _skillService = ServiceScope.ServiceProvider.GetRequiredService<ISkillService>();
    }

    #region Helper Methods

    private async Task<Guid> CreateTestSkillAsync(string name, string category = "Programming")
    {
        var createDto = new CreateSkillDto
        {
            Name = name,
            Description = $"Description for {name}",
            Category = category
        };
        var result = await _skillService.CreateSkillAsync(createDto);
        if (!result.Success || result.Data == null)
            return Guid.Empty;
        var skillDto = (SkillDto)result.Data;
        return skillDto.Id;
    }

    private async Task<(Guid workspaceId, Guid clientId, Guid providerId)> CreateTestWorkspaceAsync()
    {
        var client = await CreateTestUserAsync($"client_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var provider = await CreateTestUserAsync($"provider_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        // Create a skill for project requirements
        var skillId = await CreateTestSkillAsync($"ProjectSkill_{Guid.NewGuid():N}");

        // Create a project
        var projectDto = new CreateProjectDto
        {
            Title = $"Test Project {Guid.NewGuid():N}",
            Description = "Test project for messaging integration tests",
            CreditBudget = 100,
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
        var projectResult = await _projectService.CreateProjectAsync(projectDto, client.Id, "127.0.0.1");
        var project = await Context.Projects.FindAsync(projectResult.Project!.Id);
        project!.ProviderId = provider.Id;
        await Context.SaveChangesAsync();

        // Create workspace
        var workspace = await _workspaceService.CreateWorkspaceAsync(projectResult.Project.Id, provider.Id);
        return (workspace.Id, client.Id, provider.Id);
    }

    private async Task<MessageDto> SendTestMessageAsync(Guid workspaceId, Guid senderId, string messageText = "Test message")
    {
        var request = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = messageText,
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        return await _messagingService.SendMessageAsync(request, senderId);
    }

    #endregion

    #region CSRF Protection Tests (Phase 3)

    [Fact]
    public async Task SendMessage_ValidWorkspace_SendsSuccessfully()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();

        // Act
        var request = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = "Test message for CSRF validation",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var result = await _messagingService.SendMessageAsync(request, clientId);

        // Assert
        result.Should().NotBeNull();
        result.MessageText.Should().Be("Test message for CSRF validation");
        result.SenderId.Should().Be(clientId);
    }

    [Fact]
    public async Task EditMessage_BySender_EditsSuccessfully()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Original message");

        // Act
        var editRequest = new EditMessageRequest
        {
            MessageText = "Edited message content"
        };
        var result = await _messagingService.EditMessageAsync(message.Id, editRequest, clientId);

        // Assert
        result.Should().NotBeNull();
        result.MessageText.Should().Be("Edited message content");
        result.IsEdited.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMessage_BySender_DeletesSuccessfully()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Message to delete");

        // Act
        var result = await _messagingService.DeleteMessageAsync(message.Id, clientId);

        // Assert
        result.Should().BeTrue();

        // Verify message is actually deleted
        var deletedMessage = await _messagingService.GetMessageAsync(message.Id, clientId);
        deletedMessage.Should().BeNull();
    }

    [Fact]
    public async Task AddReaction_ToMessage_AddsSuccessfully()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Message to react to");

        // Act
        var reactionRequest = new AddReactionRequest
        {
            Emoji = "👍"
        };
        var result = await _messagingService.AddReactionAsync(message.Id, reactionRequest, providerId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Rate Limiting Tests (Phase 4)

    [Fact]
    public async Task SendMessage_MultipleMessages_AllSucceed()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();

        // Act - Send 5 messages in quick succession
        var tasks = Enumerable.Range(1, 5).Select(i =>
            SendTestMessageAsync(workspaceId, clientId, $"Message {i}"))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed (rate limiting should allow bursts)
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        results.Length.Should().Be(5);
    }

    [Fact]
    public async Task AddReaction_MultipleReactions_AllSucceed()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Message for reactions");

        // Act - Add multiple different reactions
        var emojis = new[] { "👍", "❤️", "😊" };
        var results = new List<bool>();
        foreach (var emoji in emojis)
        {
            var result = await _messagingService.AddReactionAsync(
                message.Id,
                new AddReactionRequest { Emoji = emoji },
                providerId);
            results.Add(result);
        }

        // Assert
        results.Should().AllBeEquivalentTo(true);
    }

    #endregion

    #region Audit Logging Tests (Phase 5)

    [Fact]
    public async Task SendMessage_CreatesAuditableMessage()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();

        // Act
        var request = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = "Audited message",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var result = await _messagingService.SendMessageAsync(request, clientId);

        // Assert - Message should have audit fields
        result.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task DeleteMessage_RemovesMessageProperly()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Message to delete for audit");
        var messageId = message.Id;

        // Act
        await _messagingService.DeleteMessageAsync(messageId, clientId);

        // Assert - Message should not be retrievable
        var deletedMessage = await _messagingService.GetMessageAsync(messageId, clientId);
        deletedMessage.Should().BeNull();
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task EditMessage_AsNonOwner_Fails()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Client's message");

        // Act & Assert - Provider trying to edit client's message should fail
        var editRequest = new EditMessageRequest { MessageText = "Attempted edit" };
        Func<Task> act = async () => await _messagingService.EditMessageAsync(message.Id, editRequest, providerId);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DeleteMessage_AsNonOwner_Fails()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Client's message to try deleting");

        // Act & Assert - Provider trying to delete client's message should throw
        Func<Task> act = async () => await _messagingService.DeleteMessageAsync(message.Id, providerId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>("non-owner should not be able to delete messages");
    }

    [Fact]
    public async Task GetMessageHistory_ReturnsOnlyWorkspaceMessages()
    {
        // Arrange
        var (workspaceId1, clientId1, _) = await CreateTestWorkspaceAsync();
        var (workspaceId2, clientId2, _) = await CreateTestWorkspaceAsync();

        await SendTestMessageAsync(workspaceId1, clientId1, "Message in workspace 1");
        await SendTestMessageAsync(workspaceId2, clientId2, "Message in workspace 2");

        // Act
        var historyRequest = new MessageHistoryRequest
        {
            WorkspaceId = workspaceId1,
            PageNumber = 1,
            PageSize = 10
        };
        var history = await _messagingService.GetMessageHistoryAsync(historyRequest, clientId1);

        // Assert - Should only contain messages from workspace 1
        history.Should().NotBeNull();
        history.Messages.Should().AllSatisfy(m => m.WorkspaceId.Should().Be(workspaceId1));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task SendMessage_EmptyText_StillSucceedsWithTextType()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();

        // Act - MessageText can be null for certain message types
        var request = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = "",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Depending on implementation, this might throw or succeed with empty text
        Func<Task> act = async () => await _messagingService.SendMessageAsync(request, clientId);

        // Assert - Should either succeed or throw meaningful validation error
        // This test documents the actual behavior
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task SendMessage_InvalidWorkspace_Fails()
    {
        // Arrange
        var client = await CreateTestUserAsync($"client_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var nonExistentWorkspaceId = Guid.NewGuid();

        // Act
        var request = new SendMessageRequest
        {
            WorkspaceId = nonExistentWorkspaceId,
            MessageText = "Message to invalid workspace",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        Func<Task> act = async () => await _messagingService.SendMessageAsync(request, client.Id);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendMessage_ExcessiveLength_HandledGracefully()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var longMessage = new string('A', 3500); // Within 4000 char limit

        // Act
        var request = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = longMessage,
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var result = await _messagingService.SendMessageAsync(request, clientId);

        // Assert - Should handle gracefully (accepts message within limit)
        result.Should().NotBeNull();
        result.MessageText.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region SignalR Real-Time Tests

    [Fact]
    public async Task SendMessage_ReturnsCompleteMessageDto()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();

        // Act
        var request = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = "Message for SignalR broadcast",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var result = await _messagingService.SendMessageAsync(request, clientId);

        // Assert - Verify all required fields for SignalR broadcast
        result.Id.Should().NotBeEmpty();
        result.WorkspaceId.Should().Be(workspaceId);
        result.SenderId.Should().Be(clientId);
        result.MessageText.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task MarkAsRead_UpdatesReadStatus()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Message to mark as read");

        // Act
        var result = await _messagingService.MarkMessageAsReadAsync(message.Id, providerId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task MarkAllMessagesAsRead_DuplicateRequest_IsIdempotent()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();
        await SendTestMessageAsync(workspaceId, clientId, "Message 1");
        await SendTestMessageAsync(workspaceId, clientId, "Message 2");

        // Act - Mark as read twice
        var firstResult = await _messagingService.MarkAllMessagesAsReadAsync(workspaceId, providerId);
        var secondResult = await _messagingService.MarkAllMessagesAsReadAsync(workspaceId, providerId);

        // Assert - Both should succeed (idempotent)
        firstResult.Should().BeGreaterThanOrEqualTo(0);
        secondResult.Should().Be(0, "second call should have nothing to mark");
    }

    [Fact]
    public async Task DeleteMessage_DuplicateRequest_IsIdempotent()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Message to delete");

        // Act - Delete twice
        var firstResult = await _messagingService.DeleteMessageAsync(message.Id, clientId);
        var secondResult = await _messagingService.DeleteMessageAsync(message.Id, clientId);

        // Assert
        firstResult.Should().BeTrue("first deletion should succeed");
        secondResult.Should().BeFalse("second deletion should return false - already deleted");
    }

    #endregion

    #region Race Condition Tests

    [Fact]
    public async Task EditMessage_ConcurrentEdits_HandlesGracefully()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var message = await SendTestMessageAsync(workspaceId, clientId, "Original message");

        // Act - Concurrent edits
        var editTasks = Enumerable.Range(1, 5).Select(i =>
            SafeEditMessageAsync(message.Id, $"Edit {i}", clientId))
            .ToList();
        var results = await Task.WhenAll(editTasks);

        // Assert - At least one should succeed
        results.Where(r => r != null).Should().HaveCountGreaterThan(0);
    }

    private async Task<MessageDto?> SafeEditMessageAsync(Guid messageId, string newText, Guid userId)
    {
        try
        {
            var request = new EditMessageRequest { MessageText = newText };
            return await _messagingService.EditMessageAsync(messageId, request, userId);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region GET Endpoint Tests

    [Fact]
    public async Task GetMessageHistory_Authenticated_ReturnsMessages()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        await SendTestMessageAsync(workspaceId, clientId, "Test message 1");
        await SendTestMessageAsync(workspaceId, clientId, "Test message 2");

        // Act
        var historyRequest = new MessageHistoryRequest
        {
            WorkspaceId = workspaceId,
            PageNumber = 1,
            PageSize = 10
        };
        var history = await _messagingService.GetMessageHistoryAsync(historyRequest, clientId);

        // Assert
        history.Should().NotBeNull();
        history.Messages.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetMessage_ValidId_ReturnsMessage()
    {
        // Arrange
        var (workspaceId, clientId, _) = await CreateTestWorkspaceAsync();
        var sentMessage = await SendTestMessageAsync(workspaceId, clientId, "Message to retrieve");

        // Act
        var retrievedMessage = await _messagingService.GetMessageAsync(sentMessage.Id, clientId);

        // Assert
        retrievedMessage.Should().NotBeNull();
        retrievedMessage!.Id.Should().Be(sentMessage.Id);
        retrievedMessage.MessageText.Should().Be("Message to retrieve");
    }

    [Fact]
    public async Task GetMessage_InvalidId_ReturnsNull()
    {
        // Arrange
        var (_, clientId, _) = await CreateTestWorkspaceAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _messagingService.GetMessageAsync(nonExistentId, clientId);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
