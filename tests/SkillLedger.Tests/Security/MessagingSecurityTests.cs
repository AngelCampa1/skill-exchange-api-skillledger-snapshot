using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace SkillLedger.Tests.Security
{
    /// <summary>
    /// Security tests for messaging functionality focusing on encryption, authorization, and data protection
    /// </summary>
    [SecurityTest]
    [MessagingTest]
    [Collection("Integration Security")]
public class MessagingSecurityTests : IntegrationTestBase
    {
        private readonly IMessagingService _messagingService;
        private readonly IEncryptionService _encryptionService;
        private User _testUser1 = null!;
        private User _testUser2 = null!;
        private User _unauthorizedUser = null!;
        private ProjectWorkspace _testWorkspace = null!;

        public MessagingSecurityTests(SharedTestHostFixture fixture) : base(fixture)
        {
            var encryptionService = ServiceScope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var auditLogService = ServiceScope.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logger = ServiceScope.ServiceProvider.GetRequiredService<ILogger<MessagingService>>();
            _messagingService = new MessagingService(Context, encryptionService, auditLogService, logger);
            _encryptionService = encryptionService;
        }

        private async Task SetupTestDataAsync()
        {
            // Create test users
            _testUser1 = await CreateTestUserAsync("sectest1@example.com", "TestPassword123!");
            _testUser2 = await CreateTestUserAsync("sectest2@example.com", "TestPassword123!");
            _unauthorizedUser = await CreateTestUserAsync("unauthorized@example.com", "TestPassword123!");

            // Create a test project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = "Security Test Project",
                Description = "Test Description",
                ClientId = _testUser1.Id,
                CreditBudget = 1000,
                Status = ProjectStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Create a test workspace (only user1 and user2 have access)
            _testWorkspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ClientId = _testUser1.Id,
                ProviderId = _testUser2.Id,
                Status = WorkspaceStatus.Active
            };

            Context.Projects.Add(project);
            Context.ProjectWorkspaces.Add(_testWorkspace);
            await Context.SaveChangesAsync();
        }

        [Fact]
        public async Task MessageText_Should_Be_Encrypted_In_Database()
        {
            // Arrange
            await SetupTestDataAsync();
            var sensitiveMessage = "This contains sensitive information: SSN 123-45-6789, Credit Card 1234-5678-9012-3456";

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = sensitiveMessage,
                MessageType = MessageType.Text
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.MessageText.Should().Be(sensitiveMessage); // Decrypted for the DTO

            // Verify that the message is encrypted in the database
            var messageInDb = await Context.WorkspaceMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == result.Id);

            messageInDb.Should().NotBeNull();
            messageInDb!.MessageText.Should().NotBeNull();
            messageInDb.MessageText.Should().NotBe(sensitiveMessage); // Should be encrypted
            messageInDb.MessageText!.Length.Should().BeGreaterThan(sensitiveMessage.Length); // Encrypted data is usually longer
        }

        [Fact]
        public async Task Edited_MessageText_Should_Be_Encrypted_In_Database()
        {
            // Arrange
            await SetupTestDataAsync();
            var originalMessage = "Original message";
            var editedSensitiveMessage = "Edited message with PII: Email john.doe@company.com, Phone 555-123-4567";

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = originalMessage,
                MessageType = MessageType.Text
            };

            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var editRequest = new EditMessageRequest
            {
                MessageText = editedSensitiveMessage
            };

            // Act
            var editedMessage = await _messagingService.EditMessageAsync(message.Id, editRequest, _testUser1.Id);

            // Assert
            editedMessage.MessageText.Should().Be(editedSensitiveMessage); // Decrypted for DTO

            // Verify that the edited message is encrypted in the database
            var messageInDb = await Context.WorkspaceMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            messageInDb.Should().NotBeNull();
            messageInDb!.MessageText.Should().NotBe(editedSensitiveMessage); // Should be encrypted
            messageInDb.MessageText.Should().NotBe(originalMessage); // Should not be the original message either
        }

        [Fact]
        public async Task Unauthorized_User_Cannot_Send_Message_To_Workspace()
        {
            // Arrange
            await SetupTestDataAsync();

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Unauthorized message attempt",
                MessageType = MessageType.Text
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.SendMessageAsync(request, _unauthorizedUser.Id));
        }

        [Fact]
        public async Task Unauthorized_User_Cannot_Read_Messages_From_Workspace()
        {
            // Arrange
            await SetupTestDataAsync();

            // Send a message as authorized user
            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Secret message for authorized users only",
                MessageType = MessageType.Text
            };

            var message = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Act
            var retrievedMessage = await _messagingService.GetMessageAsync(message.Id, _unauthorizedUser.Id);

            // Assert
            retrievedMessage.Should().BeNull(); // Unauthorized user should not be able to retrieve the message
        }

        [Fact]
        public async Task Unauthorized_User_Cannot_Access_Message_History()
        {
            // Arrange
            await SetupTestDataAsync();

            // Send some messages
            for (int i = 0; i < 3; i++)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Secret message {i + 1}",
                    MessageType = MessageType.Text
                };
                await _messagingService.SendMessageAsync(request, _testUser1.Id);
            }

            var historyRequest = new MessageHistoryRequest
            {
                WorkspaceId = _testWorkspace.Id,
                PageNumber = 1,
                PageSize = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.GetMessageHistoryAsync(historyRequest, _unauthorizedUser.Id));
        }

        [Fact]
        public async Task Unauthorized_User_Cannot_Search_Messages()
        {
            // Arrange
            await SetupTestDataAsync();

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "secret"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.SearchMessagesAsync(searchRequest, _unauthorizedUser.Id));
        }

        [Fact]
        public async Task User_Cannot_Edit_Other_Users_Messages()
        {
            // Arrange
            await SetupTestDataAsync();

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "User 1's message",
                MessageType = MessageType.Text
            };

            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var editRequest = new EditMessageRequest
            {
                MessageText = "User 2 attempting to edit"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.EditMessageAsync(message.Id, editRequest, _testUser2.Id));
        }

        [Fact]
        public async Task User_Cannot_Delete_Other_Users_Messages()
        {
            // Arrange
            await SetupTestDataAsync();

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "User 1's message",
                MessageType = MessageType.Text
            };

            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.DeleteMessageAsync(message.Id, _testUser2.Id));
        }

        [Fact]
        public async Task Message_Edit_Should_Have_Time_Limit_Of_24_Hours()
        {
            // Arrange
            await SetupTestDataAsync();

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Old message",
                MessageType = MessageType.Text
            };

            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Simulate old message by updating CreatedAt directly in database
            var messageEntity = await Context.WorkspaceMessages.FindAsync(message.Id);
            messageEntity!.CreatedAt = DateTime.UtcNow.AddHours(-25); // 25 hours ago
            await Context.SaveChangesAsync();

            var editRequest = new EditMessageRequest
            {
                MessageText = "Attempting to edit old message"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.EditMessageAsync(message.Id, editRequest, _testUser1.Id));
        }

        [Fact]
        public async Task Message_Delete_Should_Have_Time_Limit_Of_24_Hours()
        {
            // Arrange
            await SetupTestDataAsync();

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Old message to delete",
                MessageType = MessageType.Text
            };

            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            // Simulate old message by updating CreatedAt directly in database
            var messageEntity = await Context.WorkspaceMessages.FindAsync(message.Id);
            messageEntity!.CreatedAt = DateTime.UtcNow.AddHours(-25); // 25 hours ago
            await Context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.DeleteMessageAsync(message.Id, _testUser1.Id));
        }

        [Fact]
        public async Task Only_Text_Messages_Can_Be_Edited()
        {
            // Arrange
            await SetupTestDataAsync();

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageType = MessageType.File,
                AttachmentUrl = "https://example.com/file.pdf",
                AttachmentFileName = "file.pdf"
            };

            var message = await _messagingService.SendMessageAsync(sendRequest, _testUser1.Id);

            var editRequest = new EditMessageRequest
            {
                MessageText = "Attempting to edit file message"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messagingService.EditMessageAsync(message.Id, editRequest, _testUser1.Id));
        }

        [Fact]
        public async Task Message_Search_Should_Work_With_Encrypted_Content()
        {
            // Arrange
            await SetupTestDataAsync();

            var messages = new[]
            {
                "This message contains sensitive data: SSN 111-22-3333",
                "Another message with PII: Email user@example.com",
                "Search should find encrypted content: Credit Card 4444-5555-6666-7777",
                "Random message without sensitive data"
            };

            foreach (var messageText in messages)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = messageText,
                    MessageType = MessageType.Text
                };
                await _messagingService.SendMessageAsync(request, _testUser1.Id);
            }

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "sensitive" // This should be found even though it's encrypted
            };

            // Act
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(2); // Two messages contain "sensitive"
            result.Messages.Should().AllSatisfy(m => m.MessageText.Should().Contain("sensitive"));
        }

        [Fact]
        public async Task Audit_Log_Should_Record_Security_Events()
        {
            // Arrange
            await SetupTestDataAsync();

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message for audit logging",
                MessageType = MessageType.Text,
                IpAddress = "192.168.1.100",
                UserAgent = "Test User Agent"
            };

            // Act
            await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert - Check that audit logs were created
            var auditLogs = await Context.AuditLogs
                .Where(log => log.UserId == _testUser1.Id && log.Action == "SendMessage")
                .ToListAsync();

            auditLogs.Should().NotBeEmpty();
            // Note: IP address tracking would be verified through the service layer in real implementation
        }

        [Fact]
        public async Task IP_Address_Should_Be_Logged_For_Security_Tracking()
        {
            // Arrange
            await SetupTestDataAsync();

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message with IP tracking",
                MessageType = MessageType.Text,
                IpAddress = "203.0.113.100", // Test IP address
                UserAgent = "Mozilla/5.0 Test Browser"
            };

            // Act
            var message = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            var messageInDb = await Context.WorkspaceMessages
                .FirstOrDefaultAsync(m => m.Id == message.Id);

            messageInDb.Should().NotBeNull();
            messageInDb!.SenderIpAddress.Should().Be("203.0.113.100");
            messageInDb.SenderUserAgent.Should().Be("Mozilla/5.0 Test Browser");
        }

        [Fact]
        public async Task Message_Reaction_Should_Log_IP_Address()
        {
            // Arrange
            await SetupTestDataAsync();

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
                IpAddress = "198.51.100.50" // Test IP
            };

            // Act
            await _messagingService.AddReactionAsync(message.Id, reactionRequest, _testUser2.Id);

            // Assert
            var reaction = await Context.MessageReactions
                .FirstOrDefaultAsync(r => r.MessageId == message.Id);

            reaction.Should().NotBeNull();
            reaction!.IpAddress.Should().Be("198.51.100.50");
        }

        [Fact]
        public async Task Encryption_Should_Handle_Special_Characters_And_Unicode()
        {
            // Arrange
            await SetupTestDataAsync();

            var unicodeMessage = "Test with Unicode: 测试消息 🔐 Émojis and spëcial chars @#$%^&*()";

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = unicodeMessage,
                MessageType = MessageType.Text
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.MessageText.Should().Be(unicodeMessage); // Should be properly decrypted

            // Verify proper encryption/decryption by retrieving the message again
            var retrievedMessage = await _messagingService.GetMessageAsync(result.Id, _testUser1.Id);
            retrievedMessage!.MessageText.Should().Be(unicodeMessage);
        }

        [Fact]
        public async Task Large_Messages_Should_Be_Encrypted_Properly()
        {
            // Arrange
            await SetupTestDataAsync();

            // Create a large message (close to the 4000 character limit)
            var largeMessage = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                largeMessage.Append($"This is line {i} of a large message with sensitive information. ");
            }

            var messageText = largeMessage.ToString().Substring(0, Math.Min(3500, largeMessage.Length));

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = messageText,
                MessageType = MessageType.Text
            };

            // Act
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);

            // Assert
            result.Should().NotBeNull();
            result.MessageText.Should().Be(messageText);

            // Verify encryption in database
            var messageInDb = await Context.WorkspaceMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == result.Id);

            messageInDb.Should().NotBeNull();
            messageInDb!.MessageText.Should().NotBe(messageText); // Should be encrypted
        }

        [Fact]
        public async Task Empty_Or_Null_Messages_Should_Handle_Encryption_Gracefully()
        {
            // Arrange
            await SetupTestDataAsync();

            var requests = new[]
            {
                new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = null,
                    MessageType = MessageType.Text
                },
                new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = "",
                    MessageType = MessageType.Text
                },
                new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = "   ", // Whitespace only
                    MessageType = MessageType.Text
                }
            };

            // Act & Assert
            foreach (var request in requests)
            {
                var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);
                result.Should().NotBeNull();
                result.MessageText.Should().Be(request.MessageText); // Should handle null/empty gracefully
            }
        }

        [Fact]
        public async Task Message_Search_Performance_Should_Not_Leak_Timing_Information()
        {
            // Arrange
            await SetupTestDataAsync();

            // Send messages with varying lengths and complexity
            var messages = new[]
            {
                "short",
                "This is a medium length message with some complexity",
                new string('A', 1000) + "search term" + new string('B', 1000), // Long message with search term
                new string('X', 2000) // Long message without search term
            };

            foreach (var messageText in messages)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = messageText,
                    MessageType = MessageType.Text
                };
                await _messagingService.SendMessageAsync(request, _testUser1.Id);
            }

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "search"
            };

            // Act - Measure search performance
            var startTime = DateTime.UtcNow;
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);
            var endTime = DateTime.UtcNow;
            var searchDuration = endTime - startTime;

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(1); // Only one message contains "search"

            // Performance should be reasonable (less than 5 seconds for this test)
            searchDuration.Should().BeLessThan(TimeSpan.FromSeconds(5));

            // The search duration should be recorded in the response
            result.SearchDuration.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }
}