using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for MessagingService - Workspace messaging functionality.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database
/// - Uses MockEncryptionService (internal service - fast Base64 encoding for tests)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 0 (all services are internal)
/// </summary>
[IntegrationTest]
public class MessagingServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;
    private readonly MockEncryptionService _encryptionService;
    private readonly MessagingService _messagingService;
    private readonly ILogger<MessagingService> _logger;

    // Test data
    private User _clientUser = null!;
    private User _providerUser = null!;
    private User _unauthorizedUser = null!;
    private Project _testProject = null!;
    private ProjectWorkspace _activeWorkspace = null!;

    public MessagingServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"MessagingServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        _auditLogService = new MockAuditLogService(_context);
        _encryptionService = new MockEncryptionService();
        _logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<MessagingService>();

        _messagingService = new MessagingService(
            _context,
            _encryptionService,
            _auditLogService,
            _logger
        );

        SetupTestData();
    }

    private void SetupTestData()
    {
        _clientUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@test.com",
            UserName = "testclient",
            FirstName = "Test",
            LastName = "Client"
        };

        _providerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            UserName = "testprovider",
            FirstName = "Test",
            LastName = "Provider"
        };

        _unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "unauthorized@test.com",
            UserName = "unauthorized",
            FirstName = "Unauthorized",
            LastName = "User"
        };

        _context.Users.AddRange(_clientUser, _providerUser, _unauthorizedUser);

        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Test Project",
            Description = "A test project for messaging tests",
            ClientId = _clientUser.Id,
            Status = ProjectStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(_testProject);

        _activeWorkspace = new ProjectWorkspace
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ClientId = _clientUser.Id,
            ProviderId = _providerUser.Id,
            Status = WorkspaceStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectWorkspaces.Add(_activeWorkspace);
        _context.SaveChanges();
    }

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_ValidRequest_CreatesMessageInDatabase()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Hello, this is a test message!",
            MessageType = MessageType.Text,
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0"
        };

        // Act
        var result = await _messagingService.SendMessageAsync(request, _clientUser.Id);

        // Assert - Verify operation success
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.SenderId.Should().Be(_clientUser.Id);
        result.MessageText.Should().Be("Hello, this is a test message!");
        result.MessageType.Should().Be(MessageType.Text);
        result.Status.Should().Be(MessageStatus.Sent);

        // Assert - Verify REAL database state
        var savedMessage = await _context.WorkspaceMessages
            .FirstOrDefaultAsync(m => m.Id == result.Id);
        savedMessage.Should().NotBeNull();
        savedMessage!.SenderId.Should().Be(_clientUser.Id);
        savedMessage.WorkspaceId.Should().Be(_activeWorkspace.Id);
        savedMessage.SenderIpAddress.Should().Be("192.168.1.1");

        // Assert - Verify audit log was written
        _auditLogService.LoggedEvents.Should().Contain(e => e.Action == "SendMessage" && e.UserId == _clientUser.Id);
    }

    [Fact]
    public async Task SendMessageAsync_WithIdempotencyKey_ReturnsSameMessageOnRetry()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Idempotent message",
            MessageType = MessageType.Text,
            IdempotencyKey = idempotencyKey
        };

        // Act - Send twice with same idempotency key
        var result1 = await _messagingService.SendMessageAsync(request, _clientUser.Id);
        var result2 = await _messagingService.SendMessageAsync(request, _clientUser.Id);

        // Assert - Both should return the same message
        result1.Id.Should().Be(result2.Id);
        result1.MessageText.Should().Be(result2.MessageText);

        // Verify only ONE message was created in database
        var messageCount = await _context.WorkspaceMessages
            .CountAsync(m => m.IdempotencyKey == idempotencyKey);
        messageCount.Should().Be(1);
    }

    [Fact]
    public async Task SendMessageAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Unauthorized message",
            MessageType = MessageType.Text
        };

        // Act & Assert
        await _messagingService.Invoking(s => s.SendMessageAsync(request, _unauthorizedUser.Id))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*does not have access*");
    }

    [Fact]
    public async Task SendMessageAsync_AsProvider_CreatesMessageSuccessfully()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Provider message",
            MessageType = MessageType.Text
        };

        // Act
        var result = await _messagingService.SendMessageAsync(request, _providerUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.SenderId.Should().Be(_providerUser.Id);
    }

    [Fact]
    public async Task SendMessageAsync_WithReplyTo_CreatesReplyMessageSuccessfully()
    {
        // Arrange - Create original message
        var originalRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Original message",
            MessageType = MessageType.Text
        };
        var originalMessage = await _messagingService.SendMessageAsync(originalRequest, _clientUser.Id);

        // Arrange - Create reply
        var replyRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Reply message",
            MessageType = MessageType.Text,
            ReplyToMessageId = originalMessage.Id
        };

        // Act
        var reply = await _messagingService.SendMessageAsync(replyRequest, _providerUser.Id);

        // Assert
        reply.Should().NotBeNull();
        reply.ReplyToMessageId.Should().Be(originalMessage.Id);

        // Verify database state
        var savedReply = await _context.WorkspaceMessages
            .FirstOrDefaultAsync(m => m.Id == reply.Id);
        savedReply!.ReplyToMessageId.Should().Be(originalMessage.Id);
    }

    [Fact]
    public async Task SendMessageAsync_ReplyToNonExistentMessage_ThrowsArgumentException()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Reply to nothing",
            MessageType = MessageType.Text,
            ReplyToMessageId = Guid.NewGuid() // Non-existent
        };

        // Act & Assert
        await _messagingService.Invoking(s => s.SendMessageAsync(request, _clientUser.Id))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Reply-to message not found*");
    }

    [Fact]
    public async Task SendMessageAsync_WithAttachment_CreatesMessageWithAttachmentDetails()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = null,
            MessageType = MessageType.File,
            AttachmentUrl = "https://storage.example.com/files/document.pdf",
            AttachmentFileName = "document.pdf",
            AttachmentSize = 1024 * 100, // 100KB
            AttachmentMimeType = "application/pdf"
        };

        // Act
        var result = await _messagingService.SendMessageAsync(request, _clientUser.Id);

        // Assert
        result.MessageType.Should().Be(MessageType.File);
        result.AttachmentUrl.Should().Be("https://storage.example.com/files/document.pdf");
        result.AttachmentFileName.Should().Be("document.pdf");
        result.AttachmentSize.Should().Be(1024 * 100);
        result.AttachmentMimeType.Should().Be("application/pdf");
    }

    #endregion

    #region EditMessageAsync Tests

    [Fact]
    public async Task EditMessageAsync_ValidEdit_UpdatesMessageInDatabase()
    {
        // Arrange - Create message first
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Original text",
            MessageType = MessageType.Text
        };
        var originalMessage = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        var editRequest = new EditMessageRequest
        {
            MessageText = "Edited text",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _messagingService.EditMessageAsync(originalMessage.Id, editRequest, _clientUser.Id);

        // Assert
        result.MessageText.Should().Be("Edited text");
        result.IsEdited.Should().BeTrue();
        result.EditedAt.Should().NotBeNull();
        result.EditedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify database state
        var savedMessage = await _context.WorkspaceMessages.FirstOrDefaultAsync(m => m.Id == originalMessage.Id);
        savedMessage!.IsEdited.Should().BeTrue();
    }

    [Fact]
    public async Task EditMessageAsync_ByOtherUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange - Client creates message
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Client message",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        var editRequest = new EditMessageRequest { MessageText = "Provider tries to edit" };

        // Act & Assert - Provider tries to edit
        await _messagingService.Invoking(s => s.EditMessageAsync(message.Id, editRequest, _providerUser.Id))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*cannot edit*");
    }

    [Fact]
    public async Task EditMessageAsync_NonExistentMessage_ThrowsArgumentException()
    {
        // Arrange
        var editRequest = new EditMessageRequest { MessageText = "Edit nothing" };

        // Act & Assert
        await _messagingService.Invoking(s => s.EditMessageAsync(Guid.NewGuid(), editRequest, _clientUser.Id))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Message not found*");
    }

    #endregion

    #region DeleteMessageAsync Tests

    [Fact]
    public async Task DeleteMessageAsync_ValidDelete_RemovesMessageFromDatabase()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Message to delete",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act
        var result = await _messagingService.DeleteMessageAsync(message.Id, _clientUser.Id);

        // Assert
        result.Should().BeTrue();

        // Verify message is deleted from database
        var deletedMessage = await _context.WorkspaceMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        deletedMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMessageAsync_WithReactions_CascadeDeletesReactions()
    {
        // Arrange - Create message
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Message with reactions",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Add reaction
        var reactionRequest = new AddReactionRequest { Emoji = "👍", IpAddress = "192.168.1.1" };
        await _messagingService.AddReactionAsync(message.Id, reactionRequest, _providerUser.Id);

        var reactionCountBefore = await _context.MessageReactions.CountAsync(r => r.MessageId == message.Id);
        reactionCountBefore.Should().Be(1);

        // Act - Delete message
        var result = await _messagingService.DeleteMessageAsync(message.Id, _clientUser.Id);

        // Assert
        result.Should().BeTrue();

        // Verify reactions are also deleted
        var reactionCountAfter = await _context.MessageReactions.CountAsync(r => r.MessageId == message.Id);
        reactionCountAfter.Should().Be(0);
    }

    [Fact]
    public async Task DeleteMessageAsync_NonExistentMessage_ReturnsFalse()
    {
        // Act
        var result = await _messagingService.DeleteMessageAsync(Guid.NewGuid(), _clientUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMessageAsync_ByOtherUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Client message",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act & Assert
        await _messagingService.Invoking(s => s.DeleteMessageAsync(message.Id, _providerUser.Id))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*cannot delete*");
    }

    #endregion

    #region MarkMessageAsReadAsync Tests

    [Fact]
    public async Task MarkMessageAsReadAsync_ValidMessage_MarksAsRead()
    {
        // Arrange - Client sends message
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Read me",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act - Provider reads message
        var result = await _messagingService.MarkMessageAsReadAsync(message.Id, _providerUser.Id);

        // Assert
        result.Should().BeTrue();

        var savedMessage = await _context.WorkspaceMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        savedMessage!.Status.Should().Be(MessageStatus.Read);
        savedMessage.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_OwnMessage_DoesNotChangeStatus()
    {
        // Arrange - Client sends message
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "My own message",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act - Client tries to mark their own message as read
        var result = await _messagingService.MarkMessageAsReadAsync(message.Id, _clientUser.Id);

        // Assert - Should succeed but status should remain Sent
        result.Should().BeTrue();

        var savedMessage = await _context.WorkspaceMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        savedMessage!.Status.Should().Be(MessageStatus.Sent);
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_NonExistentMessage_ReturnsFalse()
    {
        // Act
        var result = await _messagingService.MarkMessageAsReadAsync(Guid.NewGuid(), _clientUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region MarkAllMessagesAsReadAsync Tests

    [Fact]
    public async Task MarkAllMessagesAsReadAsync_MultipleUnreadMessages_MarksAllAsRead()
    {
        // Arrange - Client sends multiple messages
        for (int i = 0; i < 5; i++)
        {
            var request = new SendMessageRequest
            {
                WorkspaceId = _activeWorkspace.Id,
                MessageText = $"Message {i}",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(request, _clientUser.Id);
        }

        // Act - Provider marks all as read
        var count = await _messagingService.MarkAllMessagesAsReadAsync(_activeWorkspace.Id, _providerUser.Id);

        // Assert
        count.Should().Be(5);

        // Verify all messages are now read
        var unreadCount = await _context.WorkspaceMessages
            .CountAsync(m => m.WorkspaceId == _activeWorkspace.Id && m.Status != MessageStatus.Read);
        unreadCount.Should().Be(0);
    }

    [Fact]
    public async Task MarkAllMessagesAsReadAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Act & Assert
        await _messagingService.Invoking(s => s.MarkAllMessagesAsReadAsync(_activeWorkspace.Id, _unauthorizedUser.Id))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region GetMessageHistoryAsync Tests

    [Fact]
    public async Task GetMessageHistoryAsync_ValidRequest_ReturnsPaginatedMessages()
    {
        // Arrange - Create 15 messages
        for (int i = 0; i < 15; i++)
        {
            var request = new SendMessageRequest
            {
                WorkspaceId = _activeWorkspace.Id,
                MessageText = $"History message {i}",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(request, _clientUser.Id);
        }

        var historyRequest = new MessageHistoryRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _messagingService.GetMessageHistoryAsync(historyRequest, _clientUser.Id);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().HaveCount(10);
        result.TotalCount.Should().Be(15);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetMessageHistoryAsync_FilterByMessageType_ReturnsFilteredMessages()
    {
        // Arrange - Create mixed message types
        var textRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Text message",
            MessageType = MessageType.Text
        };
        await _messagingService.SendMessageAsync(textRequest, _clientUser.Id);

        var fileRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = null,
            MessageType = MessageType.File,
            AttachmentUrl = "https://example.com/file.pdf",
            AttachmentFileName = "file.pdf"
        };
        await _messagingService.SendMessageAsync(fileRequest, _clientUser.Id);

        var historyRequest = new MessageHistoryRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageType = MessageType.Text
        };

        // Act
        var result = await _messagingService.GetMessageHistoryAsync(historyRequest, _clientUser.Id);

        // Assert
        result.Messages.Should().AllSatisfy(m => m.MessageType.Should().Be(MessageType.Text));
    }

    [Fact]
    public async Task GetMessageHistoryAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var historyRequest = new MessageHistoryRequest { WorkspaceId = _activeWorkspace.Id };

        // Act & Assert
        await _messagingService.Invoking(s => s.GetMessageHistoryAsync(historyRequest, _unauthorizedUser.Id))
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region SearchMessagesAsync Tests

    [Fact]
    public async Task SearchMessagesAsync_ValidQuery_ReturnsMatchingMessages()
    {
        // Arrange - Create messages with different content
        var request1 = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Hello world from SkillLedger",
            MessageType = MessageType.Text
        };
        await _messagingService.SendMessageAsync(request1, _clientUser.Id);

        var request2 = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Goodbye world",
            MessageType = MessageType.Text
        };
        await _messagingService.SendMessageAsync(request2, _clientUser.Id);

        var request3 = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Something completely different",
            MessageType = MessageType.Text
        };
        await _messagingService.SendMessageAsync(request3, _clientUser.Id);

        var searchRequest = new SearchMessagesRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            Query = "world"
        };

        // Act
        var result = await _messagingService.SearchMessagesAsync(searchRequest, _clientUser.Id);

        // Assert
        result.Messages.Should().HaveCount(2);
        result.Messages.Should().AllSatisfy(m => m.MessageText!.ToLower().Should().Contain("world"));
        result.SearchDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task SearchMessagesAsync_EmptyQuery_ReturnsAllMessages()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Test message",
            MessageType = MessageType.Text
        };
        await _messagingService.SendMessageAsync(request, _clientUser.Id);

        var searchRequest = new SearchMessagesRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            Query = ""
        };

        // Act
        var result = await _messagingService.SearchMessagesAsync(searchRequest, _clientUser.Id);

        // Assert
        result.Messages.Should().NotBeEmpty();
    }

    #endregion

    #region GetMessageAsync Tests

    [Fact]
    public async Task GetMessageAsync_ExistingMessage_ReturnsMessage()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Get me",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act
        var result = await _messagingService.GetMessageAsync(message.Id, _providerUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(message.Id);
        result.MessageText.Should().Be("Get me");
    }

    [Fact]
    public async Task GetMessageAsync_NonExistentMessage_ReturnsNull()
    {
        // Act
        var result = await _messagingService.GetMessageAsync(Guid.NewGuid(), _clientUser.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMessageAsync_UnauthorizedUser_ReturnsNull()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Secret message",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act
        var result = await _messagingService.GetMessageAsync(message.Id, _unauthorizedUser.Id);

        // Assert - Should return null (not reveal existence)
        result.Should().BeNull();
    }

    #endregion

    #region Reaction Tests

    [Fact]
    public async Task AddReactionAsync_ValidRequest_AddsReactionToDatabase()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "React to me",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        var reactionRequest = new AddReactionRequest
        {
            Emoji = "👍",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _providerUser.Id);

        // Assert
        result.Should().BeTrue();

        // Verify database state
        var reaction = await _context.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == message.Id && r.UserId == _providerUser.Id);
        reaction.Should().NotBeNull();
        reaction!.Emoji.Should().Be("👍");
    }

    [Fact]
    public async Task AddReactionAsync_DuplicateReaction_ReturnsTrue()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "React twice",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        var reactionRequest = new AddReactionRequest { Emoji = "❤️" };

        // Act - Add same reaction twice
        var result1 = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _providerUser.Id);
        var result2 = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _providerUser.Id);

        // Assert - Both should succeed but only one reaction should exist
        result1.Should().BeTrue();
        result2.Should().BeTrue();

        var reactionCount = await _context.MessageReactions
            .CountAsync(r => r.MessageId == message.Id && r.UserId == _providerUser.Id && r.Emoji == "❤️");
        reactionCount.Should().Be(1);
    }

    [Fact]
    public async Task RemoveReactionAsync_ExistingReaction_RemovesFromDatabase()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "Remove reaction",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        var reactionRequest = new AddReactionRequest { Emoji = "😄" };
        await _messagingService.AddReactionAsync(message.Id, reactionRequest, _providerUser.Id);

        // Act
        var result = await _messagingService.RemoveReactionAsync(message.Id, "😄", _providerUser.Id);

        // Assert
        result.Should().BeTrue();

        var reaction = await _context.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == message.Id && r.UserId == _providerUser.Id);
        reaction.Should().BeNull();
    }

    [Fact]
    public async Task RemoveReactionAsync_NonExistentReaction_ReturnsFalse()
    {
        // Arrange
        var createRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageText = "No reaction here",
            MessageType = MessageType.Text
        };
        var message = await _messagingService.SendMessageAsync(createRequest, _clientUser.Id);

        // Act
        var result = await _messagingService.RemoveReactionAsync(message.Id, "🎉", _providerUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Typing Indicator Tests

    [Fact]
    public async Task UpdateTypingIndicatorAsync_ValidRequest_CreatesIndicator()
    {
        // Act
        var result = await _messagingService.UpdateTypingIndicatorAsync(_activeWorkspace.Id, _clientUser.Id, "connection-1");

        // Assert
        result.Should().BeTrue();

        var indicator = await _context.TypingIndicators
            .FirstOrDefaultAsync(t => t.WorkspaceId == _activeWorkspace.Id && t.UserId == _clientUser.Id);
        indicator.Should().NotBeNull();
        indicator!.ConnectionId.Should().Be("connection-1");
    }

    [Fact]
    public async Task UpdateTypingIndicatorAsync_ExistingIndicator_UpdatesTimestamp()
    {
        // Arrange - Create initial indicator
        await _messagingService.UpdateTypingIndicatorAsync(_activeWorkspace.Id, _clientUser.Id, "connection-1");

        var initialIndicator = await _context.TypingIndicators
            .FirstOrDefaultAsync(t => t.WorkspaceId == _activeWorkspace.Id && t.UserId == _clientUser.Id);
        var initialTime = initialIndicator!.LastTypingAt;

        await Task.Delay(10); // Small delay

        // Act - Update indicator
        await _messagingService.UpdateTypingIndicatorAsync(_activeWorkspace.Id, _clientUser.Id, "connection-1");

        // Assert
        var updatedIndicator = await _context.TypingIndicators
            .FirstOrDefaultAsync(t => t.WorkspaceId == _activeWorkspace.Id && t.UserId == _clientUser.Id);
        updatedIndicator!.LastTypingAt.Should().BeAfter(initialTime);
    }

    [Fact]
    public async Task StopTypingIndicatorAsync_ExistingIndicator_RemovesIndicator()
    {
        // Arrange
        await _messagingService.UpdateTypingIndicatorAsync(_activeWorkspace.Id, _clientUser.Id, "connection-1");

        // Act
        await _messagingService.StopTypingIndicatorAsync(_activeWorkspace.Id, _clientUser.Id);

        // Assert
        var indicator = await _context.TypingIndicators
            .FirstOrDefaultAsync(t => t.WorkspaceId == _activeWorkspace.Id && t.UserId == _clientUser.Id);
        indicator.Should().BeNull();
    }

    [Fact]
    public async Task GetTypingIndicatorsAsync_WithActiveIndicators_ReturnsActiveOnly()
    {
        // Arrange - Create indicator for client
        await _messagingService.UpdateTypingIndicatorAsync(_activeWorkspace.Id, _clientUser.Id);

        // Act - Get indicators excluding client
        var result = await _messagingService.GetTypingIndicatorsAsync(_activeWorkspace.Id, _providerUser.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(_clientUser.Id);
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupInactiveTypingIndicatorsAsync_WithInactiveIndicators_RemovesThem()
    {
        // Arrange - Manually create an old indicator
        var oldIndicator = new TypingIndicator
        {
            WorkspaceId = _activeWorkspace.Id,
            UserId = _clientUser.Id,
            LastTypingAt = DateTime.UtcNow.AddSeconds(-10) // 10 seconds ago (older than 5s threshold)
        };
        _context.TypingIndicators.Add(oldIndicator);
        await _context.SaveChangesAsync();

        // Act
        var count = await _messagingService.CleanupInactiveTypingIndicatorsAsync();

        // Assert
        count.Should().BeGreaterOrEqualTo(1);

        var remainingIndicators = await _context.TypingIndicators
            .CountAsync(t => t.WorkspaceId == _activeWorkspace.Id);
        remainingIndicators.Should().Be(0);
    }

    #endregion

    #region GetMessageStatsAsync Tests

    [Fact]
    public async Task GetMessageStatsAsync_ValidRequest_ReturnsAccurateStats()
    {
        // Arrange - Create various messages
        for (int i = 0; i < 5; i++)
        {
            var request = new SendMessageRequest
            {
                WorkspaceId = _activeWorkspace.Id,
                MessageText = $"Stats message {i}",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(request, _clientUser.Id);
        }

        // Add a file message
        var fileRequest = new SendMessageRequest
        {
            WorkspaceId = _activeWorkspace.Id,
            MessageType = MessageType.File,
            AttachmentUrl = "https://example.com/file.pdf"
        };
        await _messagingService.SendMessageAsync(fileRequest, _providerUser.Id);

        // Act
        var stats = await _messagingService.GetMessageStatsAsync(_activeWorkspace.Id, _clientUser.Id);

        // Assert
        stats.Should().NotBeNull();
        stats.TotalMessages.Should().Be(6);
        stats.UnreadMessages.Should().Be(1); // The file message from provider
        stats.MessagesByType.Should().ContainKey(MessageType.Text);
        stats.MessagesByType[MessageType.Text].Should().Be(5);
        stats.MessagesByType[MessageType.File].Should().Be(1);
    }

    #endregion

    #region Access Control Tests

    [Fact]
    public async Task HasMessagingAccessAsync_ClientUser_ReturnsTrue()
    {
        // Act
        var result = await _messagingService.HasMessagingAccessAsync(_activeWorkspace.Id, _clientUser.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMessagingAccessAsync_ProviderUser_ReturnsTrue()
    {
        // Act
        var result = await _messagingService.HasMessagingAccessAsync(_activeWorkspace.Id, _providerUser.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMessagingAccessAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Act
        var result = await _messagingService.HasMessagingAccessAsync(_activeWorkspace.Id, _unauthorizedUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasMessagingAccessAsync_ArchivedWorkspace_ReturnsFalse()
    {
        // Arrange - Archive the workspace
        _activeWorkspace.ArchiveWorkspace();
        await _context.SaveChangesAsync();

        // Act
        var result = await _messagingService.HasMessagingAccessAsync(_activeWorkspace.Id, _clientUser.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Unread Count Tests

    [Fact]
    public async Task GetUnreadMessageCountAsync_MultipleWorkspaces_ReturnsCorrectCount()
    {
        // Arrange - Create a second project and workspace
        var secondProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Second Project",
            Description = "A second project for testing",
            ClientId = _clientUser.Id,
            Status = ProjectStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(secondProject);
        await _context.SaveChangesAsync();

        var secondWorkspace = new ProjectWorkspace
        {
            Id = Guid.NewGuid(),
            ProjectId = secondProject.Id,
            ClientId = _clientUser.Id,
            ProviderId = _providerUser.Id,
            Status = WorkspaceStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectWorkspaces.Add(secondWorkspace);
        await _context.SaveChangesAsync();

        // Detach and re-attach to ensure tracking is clean
        _context.ChangeTracker.Clear();

        // Send messages in first workspace from provider
        for (int i = 0; i < 3; i++)
        {
            var request = new SendMessageRequest
            {
                WorkspaceId = _activeWorkspace.Id,
                MessageText = $"Workspace 1 message {i}",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(request, _providerUser.Id);
        }

        // Send messages in second workspace from provider
        for (int i = 0; i < 2; i++)
        {
            var request = new SendMessageRequest
            {
                WorkspaceId = secondWorkspace.Id,
                MessageText = $"Workspace 2 message {i}",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(request, _providerUser.Id);
        }

        // Act - Get unread count for client
        var count = await _messagingService.GetUnreadMessageCountAsync(_clientUser.Id);

        // Assert
        count.Should().Be(5); // 3 + 2 from both workspaces
    }

    [Fact]
    public async Task GetWorkspaceUnreadCountAsync_ValidWorkspace_ReturnsCorrectCount()
    {
        // Arrange - Send messages from provider
        for (int i = 0; i < 4; i++)
        {
            var request = new SendMessageRequest
            {
                WorkspaceId = _activeWorkspace.Id,
                MessageText = $"Unread message {i}",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(request, _providerUser.Id);
        }

        // Act
        var count = await _messagingService.GetWorkspaceUnreadCountAsync(_activeWorkspace.Id, _clientUser.Id);

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public async Task GetWorkspaceUnreadCountAsync_UnauthorizedUser_ReturnsZero()
    {
        // Act
        var count = await _messagingService.GetWorkspaceUnreadCountAsync(_activeWorkspace.Id, _unauthorizedUser.Id);

        // Assert
        count.Should().Be(0);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
