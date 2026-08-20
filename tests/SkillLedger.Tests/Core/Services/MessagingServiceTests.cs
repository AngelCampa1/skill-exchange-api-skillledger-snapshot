using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Services
{
    [UnitTest]
    [MessagingTest]
    [Collection("Integration Financial")]
public class MessagingServiceTests : IntegrationTestBase
    {
        private readonly IMessagingService _messagingService;
        private User _testUser1 = null!;
        private User _testUser2 = null!;
        private ProjectWorkspace _testWorkspace = null!;

        public MessagingServiceTests(SharedTestHostFixture fixture) : base(fixture)
        {
            var encryptionService = ServiceScope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var auditLogService = ServiceScope.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logger = ServiceScope.ServiceProvider.GetRequiredService<ILogger<MessagingService>>();
            _messagingService = new MessagingService(Context, encryptionService, auditLogService, logger);
        }

        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();

            // Set up test data
            _testUser1 = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser1@example.com",
                Email = "testuser1@example.com",
                NormalizedEmail = "TESTUSER1@EXAMPLE.COM",
                NormalizedUserName = "TESTUSER1@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _testUser2 = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser2@example.com",
                Email = "testuser2@example.com",
                NormalizedEmail = "TESTUSER2@EXAMPLE.COM",
                NormalizedUserName = "TESTUSER2@EXAMPLE.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = "Test Project",
                Description = "Test Description",
                ClientId = _testUser1.Id,
                CreditBudget = 1000,
                Status = ProjectStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _testWorkspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ClientId = _testUser1.Id,
                ProviderId = _testUser2.Id,
                Status = WorkspaceStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            Context.Users.AddRange(_testUser1, _testUser2);
            Context.Projects.Add(project);
            Context.ProjectWorkspaces.Add(_testWorkspace);
            await Context.SaveChangesAsync();
        }

        [Fact]
        public async Task SendMessageAsync_Should_Send_Text_Message_Successfully()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Hello, this is a test message!",
                MessageType = MessageType.Text,
                IpAddress = "192.168.1.1",
                UserAgent = "Test Agent"
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.WorkspaceId.Should().Be(_testWorkspace.Id);
            result.SenderId.Should().Be(_testUser1.Id);
            result.MessageText.Should().Be("Hello, this is a test message!");
            result.MessageType.Should().Be(MessageType.Text);
            result.Status.Should().Be(MessageStatus.Sent);
            result.CanEdit.Should().BeTrue();
            result.CanDelete.Should().BeTrue();
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task SendMessageAsync_Should_Fail_For_Unauthorized_User()
        {
            // Arrange
            var unauthorizedUserId = Guid.NewGuid();
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Unauthorized message",
                MessageType = MessageType.Text
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.SendMessageAsync(request, unauthorizedUserId));
        }

        [Fact]
        public async Task SendMessageAsync_Should_Handle_Reply_Messages()
        {
            // Arrange
            // First, send an original message
            var originalRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Original message",
                MessageType = MessageType.Text
            };
            var originalMessage = await _messagingService.SendMessageAsync(originalRequest, _testUser1.Id);

            // Now send a reply
            var replyRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "This is a reply",
                MessageType = MessageType.Text,
                ReplyToMessageId = originalMessage.Id
            };

            // Act
            var replyMessage = await _messagingService.SendMessageAsync(replyRequest, _testUser2.Id);

            // Assert
            replyMessage.Should().NotBeNull();
            replyMessage.ReplyToMessageId.Should().Be(originalMessage.Id);
            replyMessage.ReplyToMessage.Should().NotBeNull();
            replyMessage.ReplyToMessage!.Id.Should().Be(originalMessage.Id);
        }

        [Fact]
        public async Task EditMessageAsync_Should_Update_Message_Text()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Original message text",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var editRequest = new EditMessageRequest
            {
                MessageText = "Edited message text"
            };

            // Act
            var editedMessage = await _messagingService.EditMessageAsync(message.Id, editRequest, _testUser1.Id);

            // Assert
            editedMessage.Should().NotBeNull();
            editedMessage.MessageText.Should().Be("Edited message text");
            editedMessage.IsEdited.Should().BeTrue();
            editedMessage.EditedAt.Should().NotBeNull();
            editedMessage.EditedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task EditMessageAsync_Should_Fail_For_Other_User()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Original message text",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var editRequest = new EditMessageRequest
            {
                MessageText = "Attempted edit by other user"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.EditMessageAsync(message.Id, editRequest, _testUser2.Id));
        }

        [Fact]
        public async Task DeleteMessageAsync_Should_Remove_Message()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to delete",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Act
            var result = await _messagingService.DeleteMessageAsync(message.Id, _testUser1.Id);

            // Assert
            result.Should().BeTrue();

            // Verify message is actually deleted
            var deletedMessage = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            deletedMessage.Should().BeNull();
        }

        [Fact]
        public async Task MarkMessageAsReadAsync_Should_Update_Message_Status()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to mark as read",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Act
            var result = await _messagingService.MarkMessageAsReadAsync(message.Id, _testUser2.Id);

            // Assert
            result.Should().BeTrue();

            // Verify message status is updated
            var readMessage = await _messagingService.GetMessageAsync(message.Id, _testUser2.Id);
            readMessage.Should().NotBeNull();
            readMessage!.Status.Should().Be(MessageStatus.Read);
            readMessage.ReadAt.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMessageHistoryAsync_Should_Return_Paginated_Messages()
        {
            // Arrange
            // Send multiple messages
            for (int i = 1; i <= 5; i++)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Test message {i}",
                    MessageType = MessageType.Text
                };
                await _messagingService.SendMessageAsync(request, i % 2 == 0 ? _testUser2.Id : _testUser1.Id);
            }

            var historyRequest = new MessageHistoryRequest
            {
                WorkspaceId = _testWorkspace.Id,
                PageNumber = 1,
                PageSize = 3
            };

            // Act
            var result = await _messagingService.GetMessageHistoryAsync(historyRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(3);
            result.TotalCount.Should().Be(5);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(3);
            result.HasNextPage.Should().BeTrue();
            result.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task AddReactionAsync_Should_Add_Emoji_Reaction()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to react to",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var reactionRequest = new AddReactionRequest
            {
                Emoji = "👍",
                IpAddress = "192.168.1.1"
            };

            // Act
            var result = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _testUser2.Id);

            // Assert
            result.Should().BeTrue();

            // Verify reaction is added
            var messageWithReaction = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            messageWithReaction.Should().NotBeNull();
            messageWithReaction!.Reactions.Should().HaveCount(1);
            messageWithReaction.Reactions[0].Emoji.Should().Be("👍");
            messageWithReaction.Reactions[0].UserId.Should().Be(_testUser2.Id);
        }

        [Fact]
        public async Task RemoveReactionAsync_Should_Remove_Emoji_Reaction()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to react to",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Add reaction first
            var reactionRequest = new AddReactionRequest { Emoji = "👍" };
            await _messagingService.AddReactionAsync(message.Id, reactionRequest, _testUser2.Id);

            // Act
            var result = await _messagingService.RemoveReactionAsync(message.Id, "👍", _testUser2.Id);

            // Assert
            result.Should().BeTrue();

            // Verify reaction is removed
            var messageAfterRemoval = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            messageAfterRemoval.Should().NotBeNull();
            messageAfterRemoval!.Reactions.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateTypingIndicatorAsync_Should_Create_Or_Update_Indicator()
        {
            // Arrange & Act
            var result = await _messagingService.UpdateTypingIndicatorAsync(_testWorkspace.Id, _testUser1.Id, "connection-123");

            // Assert
            result.Should().BeTrue();

            // Verify typing indicator exists
            var indicators = await _messagingService.GetTypingIndicatorsAsync(_testWorkspace.Id, _testUser2.Id);
            indicators.Should().HaveCount(1);
            indicators[0].UserId.Should().Be(_testUser1.Id);
            indicators[0].IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task StopTypingIndicatorAsync_Should_Remove_Indicator()
        {
            // Arrange
            await _messagingService.UpdateTypingIndicatorAsync(_testWorkspace.Id, _testUser1.Id, "connection-123");

            // Act
            var result = await _messagingService.StopTypingIndicatorAsync(_testWorkspace.Id, _testUser1.Id, "connection-123");

            // Assert
            result.Should().BeTrue();

            // Verify typing indicator is removed
            var indicators = await _messagingService.GetTypingIndicatorsAsync(_testWorkspace.Id);
            indicators.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchMessagesAsync_Should_Find_Messages_By_Text()
        {
            // Arrange
            // Send messages with different content
            var messages = new[]
            {
                "This is about testing our search functionality",
                "Another message about development",
                "Search should find this message too",
                "Random message without the keyword"
            };

            foreach (var text in messages)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = text,
                    MessageType = MessageType.Text
                };
                await _messagingService.SendMessageAsync(request, _testUser1.Id);
            }

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "search"
            };

            // Act
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(2); // Two messages contain "search"
            result.TotalCount.Should().Be(2);
            result.Query.Should().Be("search");
        }

        [Fact]
        public async Task HasMessagingAccessAsync_Should_Return_True_For_Workspace_Participants()
        {
            // Act & Assert for both workspace participants
            var clientAccess = await _messagingService.HasMessagingAccessAsync(_testWorkspace.Id, _testUser1.Id);
            var providerAccess = await _messagingService.HasMessagingAccessAsync(_testWorkspace.Id, _testUser2.Id);

            clientAccess.Should().BeTrue();
            providerAccess.Should().BeTrue();
        }

        [Fact]
        public async Task HasMessagingAccessAsync_Should_Return_False_For_Non_Participants()
        {
            // Arrange
            var nonParticipantId = Guid.NewGuid();

            // Act
            var access = await _messagingService.HasMessagingAccessAsync(_testWorkspace.Id, nonParticipantId);

            // Assert
            access.Should().BeFalse();
        }

        [Fact]
        public async Task GetUnreadMessageCountAsync_Should_Return_Correct_Count()
        {
            // Arrange
            // Send messages from user1 to user2 (user2 should have unread messages)
            for (int i = 0; i < 3; i++)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Unread message {i + 1}",
                    MessageType = MessageType.Text
                };
                await _messagingService.SendMessageAsync(request, _testUser1.Id);
            }

            // Act
            var unreadCount = await _messagingService.GetUnreadMessageCountAsync(_testUser2.Id);

            // Assert
            unreadCount.Should().Be(3);
        }

        [Fact]
        public async Task GetMessageStatsAsync_Should_Return_Workspace_Statistics()
        {
            // Arrange
            // Send various types of messages
            var requests = new[]
            {
                new SendMessageRequest { WorkspaceId = _testWorkspace.Id, MessageText = "Text 1", MessageType = MessageType.Text },
                new SendMessageRequest { WorkspaceId = _testWorkspace.Id, MessageText = "Text 2", MessageType = MessageType.Text },
                new SendMessageRequest { WorkspaceId = _testWorkspace.Id, AttachmentUrl = "file.pdf", MessageType = MessageType.File }
            };

            foreach (var request in requests)
            {
                await _messagingService.SendMessageAsync(request, _testUser1.Id);
            }

            // Act
            var stats = await _messagingService.GetMessageStatsAsync(_testWorkspace.Id, _testUser1.Id);

            // Assert
            stats.Should().NotBeNull();
            stats.WorkspaceId.Should().Be(_testWorkspace.Id);
            stats.TotalMessages.Should().Be(3);
            stats.MessagesByType[MessageType.Text].Should().Be(2);
            stats.MessagesByType[MessageType.File].Should().Be(1);
            stats.LastMessageAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task SendMessageAsync_Should_Encrypt_Message_Text()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Sensitive message that should be encrypted",
                MessageType = MessageType.Text
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.MessageText.Should().Be("Sensitive message that should be encrypted");

            // Verify in database that the text is encrypted (not plain text)
            var messageInDb = await Context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == result.Id);

            messageInDb.Should().NotBeNull();
            messageInDb!.MessageText.Should().NotBeNull();
            messageInDb.MessageText.Should().NotBe("Sensitive message that should be encrypted"); // Should be encrypted
        }

        [Fact]
        public async Task SendMessageAsync_Should_Handle_Empty_Message_Text()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = null,
                MessageType = MessageType.Text
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.MessageText.Should().BeNull();
        }

        [Fact]
        public async Task SendMessageAsync_Should_Handle_File_Message_With_Attachment()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageType = MessageType.File,
                AttachmentUrl = "https://example.com/test.pdf",
                AttachmentFileName = "test.pdf",
                AttachmentSize = 1024000,
                AttachmentMimeType = "application/pdf"
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.MessageType.Should().Be(MessageType.File);
            result.AttachmentUrl.Should().Be("https://example.com/test.pdf");
            result.AttachmentFileName.Should().Be("test.pdf");
            result.AttachmentSize.Should().Be(1024000);
            result.AttachmentMimeType.Should().Be("application/pdf");
        }

        [Fact]
        public async Task SendMessageAsync_Should_Fail_For_Invalid_Reply_Message()
        {
            // Arrange
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Reply to non-existent message",
                MessageType = MessageType.Text,
                ReplyToMessageId = Guid.NewGuid() // Non-existent message
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _messagingService.SendMessageAsync(request, _testUser1.Id));
        }

        [Fact]
        public async Task EditMessageAsync_Should_Fail_For_Non_Existent_Message()
        {
            // Arrange
            var editRequest = new EditMessageRequest
            {
                MessageText = "Edited text"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _messagingService.EditMessageAsync(Guid.NewGuid(), editRequest, _testUser1.Id));
        }

        [Fact]
        public async Task EditMessageAsync_Should_Encrypt_Updated_Text()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Original message",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var editRequest = new EditMessageRequest
            {
                MessageText = "Sensitive edited message"
            };

            // Act
            var editedMessage = await _messagingService.EditMessageAsync(message.Id, editRequest, _testUser1.Id);

            // Assert
            editedMessage.MessageText.Should().Be("Sensitive edited message");

            // Verify in database that the updated text is encrypted
            var messageInDb = await Context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            messageInDb.Should().NotBeNull();
            messageInDb!.MessageText.Should().NotBe("Sensitive edited message"); // Should be encrypted
        }

        [Fact]
        public async Task DeleteMessageAsync_Should_Remove_Related_Reactions()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message with reactions",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Add multiple reactions
            await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "👍" }, _testUser2.Id);
            await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "❤️" }, _testUser1.Id);

            // Act
            var result = await _messagingService.DeleteMessageAsync(message.Id, _testUser1.Id);

            // Assert
            result.Should().BeTrue();

            // Verify reactions are also deleted
            var remainingReactions = await Context.MessageReactions
                .Where(r => r.MessageId == message.Id)
                .CountAsync();
            remainingReactions.Should().Be(0);
        }

        [Fact]
        public async Task MarkMessageAsReadAsync_Should_Not_Mark_Own_Message_As_Read()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Own message",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Act
            var result = await _messagingService.MarkMessageAsReadAsync(message.Id, _testUser1.Id);

            // Assert
            result.Should().BeTrue(); // Returns true but doesn't change status

            // Verify message status remains as sent (not read)
            var messageAfter = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            messageAfter!.Status.Should().Be(MessageStatus.Sent);
            messageAfter.ReadAt.Should().BeNull();
        }

        [Fact]
        public async Task MarkAllMessagesAsReadAsync_Should_Only_Mark_Others_Messages()
        {
            // Arrange
            // Send messages from both users
            var user1Request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message from user 1",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(user1Request, _testUser1.Id);

            var user2Request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message from user 2",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(user2Request, _testUser2.Id);

            // Act - User 1 marks all as read
            var count = await _messagingService.MarkAllMessagesAsReadAsync(_testWorkspace.Id, _testUser1.Id);

            // Assert
            count.Should().Be(1); // Only user2's message should be marked as read
        }

        [Fact]
        public async Task SearchMessagesAsync_Should_Handle_Special_Characters()
        {
            // Arrange
            var specialCharMessage = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message with special chars: @#$%^&*()",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(specialCharMessage, _testUser1.Id);

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "@#$"
            };

            // Act
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchMessagesAsync_Should_Be_Case_Insensitive()
        {
            // Arrange
            var message = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "CaseSensitive Message Test",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(message, _testUser1.Id);

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "casesensitive"
            };

            // Act
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddReactionAsync_Should_Prevent_Duplicate_Reactions()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message for reaction test",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var reactionRequest = new AddReactionRequest { Emoji = "👍" };

            // Act - Add same reaction twice
            var firstResult = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _testUser2.Id);
            var secondResult = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _testUser2.Id);

            // Assert
            firstResult.Should().BeTrue();
            secondResult.Should().BeTrue(); // Returns true but doesn't duplicate

            // Verify only one reaction exists
            var messageWithReactions = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            messageWithReactions!.Reactions.Should().HaveCount(1);
        }

        [Fact]
        public async Task CleanupInactiveTypingIndicatorsAsync_Should_Remove_Old_Indicators()
        {
            // Arrange
            var initialCount = await Context.TypingIndicators.CountAsync();

            // Create typing indicators with different timestamps
            var activeIndicator = new TypingIndicator
            {
                WorkspaceId = _testWorkspace.Id,
                UserId = _testUser1.Id,
                LastTypingAt = DateTime.UtcNow.AddSeconds(-3) // Active (within 5 seconds)
            };

            var inactiveIndicator = new TypingIndicator
            {
                WorkspaceId = _testWorkspace.Id,
                UserId = _testUser2.Id,
                LastTypingAt = DateTime.UtcNow.AddSeconds(-10) // Inactive (older than 5 seconds)
            };

            Context.TypingIndicators.AddRange(activeIndicator, inactiveIndicator);
            await Context.SaveChangesAsync();

            // Act
            var cleanedCount = await _messagingService.CleanupInactiveTypingIndicatorsAsync();

            // Assert
            cleanedCount.Should().BeGreaterOrEqualTo(1); // Should clean at least our inactive indicator

            // Verify the active indicator we created still remains
            var activeIndicatorExists = await Context.TypingIndicators
                .AnyAsync(ti => ti.UserId == _testUser1.Id && ti.WorkspaceId == _testWorkspace.Id);
            activeIndicatorExists.Should().BeTrue();
        }

        [Fact]
        public async Task GetMessageHistoryAsync_Should_Apply_Date_Filters()
        {
            // Arrange
            var encryptionService = ServiceScope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var baseDate = DateTime.UtcNow.AddDays(-5);

            // Send messages with different dates (encrypt the text properly)
            var message1 = new WorkspaceMessage
            {
                WorkspaceId = _testWorkspace.Id,
                SenderId = _testUser1.Id,
                MessageText = await encryptionService.EncryptAsync("Old message"),
                CreatedAt = baseDate.AddDays(-2),
                MessageType = MessageType.Text
            };

            var message2 = new WorkspaceMessage
            {
                WorkspaceId = _testWorkspace.Id,
                SenderId = _testUser1.Id,
                MessageText = await encryptionService.EncryptAsync("Recent message"),
                CreatedAt = baseDate.AddDays(1),
                MessageType = MessageType.Text
            };

            Context.WorkspaceMessages.AddRange(message1, message2);
            await Context.SaveChangesAsync();

            var historyRequest = new MessageHistoryRequest
            {
                WorkspaceId = _testWorkspace.Id,
                AfterDate = baseDate,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _messagingService.GetMessageHistoryAsync(historyRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1); // Only the recent message
            result.Messages[0].MessageText.Should().Be("Recent message");
        }

        [Fact]
        public async Task GetWorkspaceUnreadCountAsync_Should_Return_Zero_For_Unauthorized_User()
        {
            // Arrange
            var unauthorizedUserId = Guid.NewGuid();

            // Act
            var count = await _messagingService.GetWorkspaceUnreadCountAsync(_testWorkspace.Id, unauthorizedUserId);

            // Assert
            count.Should().Be(0);
        }

        [Fact]
        public async Task SearchMessagesAsync_Should_Respect_Message_Type_Filter()
        {
            // Arrange
            var textMessage = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "search term text",
                MessageType = MessageType.Text
            };
            await _messagingService.SendMessageAsync(textMessage, _testUser1.Id);

            var fileMessage = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                AttachmentFileName = "search term file.pdf",
                MessageType = MessageType.File
            };
            await _messagingService.SendMessageAsync(fileMessage, _testUser1.Id);

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "search",
                MessageType = MessageType.Text // Only search text messages
            };

            // Act
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1);
            result.Messages[0].MessageType.Should().Be(MessageType.Text);
        }

        [Theory]
        [InlineData("👍")]
        [InlineData("❤️")]
        [InlineData("😄")]
        [InlineData("🎉")]
        [InlineData("🔥")]
        public async Task AddReactionAsync_Should_Accept_Various_Emoji_Types(string emoji)
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message for emoji test",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var reactionRequest = new AddReactionRequest { Emoji = emoji };

            // Act
            var result = await _messagingService.AddReactionAsync(message.Id, reactionRequest, _testUser2.Id);

            // Assert
            result.Should().BeTrue();

            var messageWithReaction = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            messageWithReaction!.Reactions.Should().HaveCount(1);
            messageWithReaction.Reactions[0].Emoji.Should().Be(emoji);
        }

        [Fact]
        public async Task GetMessageStatsAsync_Should_Include_Reaction_Statistics()
        {
            // Arrange
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message for reaction stats",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Add multiple reactions
            await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "👍" }, _testUser2.Id);
            await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "👍" }, _testUser1.Id);
            await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "❤️" }, _testUser2.Id);

            // Act
            var stats = await _messagingService.GetMessageStatsAsync(_testWorkspace.Id, _testUser1.Id);

            // Assert
            stats.Should().NotBeNull();
            stats.TopReactions.Should().ContainKey("👍");
            stats.TopReactions["👍"].Should().Be(2);
            stats.TopReactions.Should().ContainKey("❤️");
            stats.TopReactions["❤️"].Should().Be(1);
        }
    }
}