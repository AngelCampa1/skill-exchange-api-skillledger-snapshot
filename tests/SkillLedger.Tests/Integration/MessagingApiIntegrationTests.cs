using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Api.Hubs;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration
{
    [IntegrationTest]
    [ApiTest]
    [Collection("Integration Other")]
public class MessagingApiIntegrationTests : IntegrationTestBase
    {
        private readonly IHubContext<MessagingHub> _hubContext;
        private User _testUser1 = null!;
        private User _testUser2 = null!;
        private ProjectWorkspace _testWorkspace = null!;

        public MessagingApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
        {
            _hubContext = ServiceScope.ServiceProvider.GetRequiredService<IHubContext<MessagingHub>>();
        }

        protected override async Task OnInitializeAsync()
        {
            await base.OnInitializeAsync();
            await SetupTestDataAsync();
        }

        private async Task SetupTestDataAsync()
        {
            // Create test users
            _testUser1 = await CreateTestUserAsync("testuser1@example.com", "TestPassword123!");
            _testUser2 = await CreateTestUserAsync("testuser2@example.com", "TestPassword123!");

            // Create a test project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = "Test Project for Messaging",
                Description = "Test Description",
                ClientId = _testUser1.Id,
                CreditBudget = 1000,
                Status = ProjectStatus.Published,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Create a test workspace
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

        // REMOVED: GetAuthTokenForUser - now using AuthenticateAs() helper from IntegrationTestBase

        [Fact]
        [FastTest]
        public async Task SendMessage_Should_Return_201_And_MessageDto()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Hello from integration test!",
                MessageType = MessageType.Text
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(content);

            // Act
            var response = await Client.PostAsync("/api/messaging/send", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var messageDto = JsonSerializer.Deserialize<MessageDto>(responseContent, TestJsonOptions.Default);

            messageDto.Should().NotBeNull();
            messageDto!.WorkspaceId.Should().Be(_testWorkspace.Id);
            messageDto.SenderId.Should().Be(_testUser1.Id);
            messageDto.MessageText.Should().Be("Hello from integration test!");
            messageDto.MessageType.Should().Be(MessageType.Text);
        }

        [Fact]
        [SecurityTest]
        public async Task SendMessage_Should_Return_403_For_Unauthorized_Workspace()
        {
            // Arrange
            var unauthorizedUser = await CreateTestUserAsync("unauthorized@example.com", "TestPassword123!");
            AuthenticateAs(unauthorizedUser);

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Unauthorized message",
                MessageType = MessageType.Text
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(content);

            // Act
            var response = await Client.PostAsync("/api/messaging/send", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        [SecurityTest]
        public async Task SendMessage_Should_Return_401_Without_Authentication()
        {
            // Arrange

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Unauthenticated message",
                MessageType = MessageType.Text
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("/api/messaging/send", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        [FastTest]
        public async Task EditMessage_Should_Return_200_And_UpdatedMessage()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // First, send a message
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Original message",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Now edit the message
            var editRequest = new EditMessageRequest
            {
                MessageText = "Edited message text"
            };

            var editJson = JsonSerializer.Serialize(editRequest);
            var editContent = new StringContent(editJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(editContent);

            // Act
            var response = await Client.PutAsync($"/api/messaging/{sentMessage!.Id}/edit", editContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var editedMessage = JsonSerializer.Deserialize<MessageDto>(responseContent, TestJsonOptions.Default);

            editedMessage.Should().NotBeNull();
            editedMessage!.MessageText.Should().Be("Edited message text");
            editedMessage.IsEdited.Should().BeTrue();
            editedMessage.EditedAt.Should().NotBeNull();
        }

        [Fact]
        [SecurityTest]
        public async Task EditMessage_Should_Return_403_For_Other_User_Message()
        {
            // Arrange

            // Send message as user1
            AuthenticateAs(_testUser1);

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "User 1 message",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Try to edit as user2
            AuthenticateAs(_testUser2);

            var editRequest = new EditMessageRequest
            {
                MessageText = "User 2 trying to edit"
            };

            var editJson = JsonSerializer.Serialize(editRequest);
            var editContent = new StringContent(editJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(editContent);

            // Act
            var response = await Client.PutAsync($"/api/messaging/{sentMessage!.Id}/edit", editContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        [FastTest]
        public async Task DeleteMessage_Should_Return_200_And_Success_Response()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // First, send a message
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to delete",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Act
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/messaging/{sentMessage!.Id}");
            await AddCsrfTokenToRequest(deleteRequest);
            var response = await Client.SendAsync(deleteRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);

            result.Should().NotBeNull();
            result!.Should().ContainKey("success");
        }

        [Fact]
        [FastTest]
        public async Task MarkMessageAsRead_Should_Return_200()
        {
            // Arrange

            // Send message as user1
            AuthenticateAs(_testUser1);

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to mark as read",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Mark as read by user2
            AuthenticateAs(_testUser2);

            // Act
            var readRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/messaging/{sentMessage!.Id}/read");
            await AddCsrfTokenToRequest(readRequest);
            var response = await Client.SendAsync(readRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        [FastTest]
        public async Task GetMessageHistory_Should_Return_200_And_Paginated_Messages()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Send multiple messages
            for (int i = 1; i <= 3; i++)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Test message {i}",
                    MessageType = MessageType.Text
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await AddCsrfTokenToRequest(content);
                await Client.PostAsync("/api/messaging/send", content);
            }

            // Act
            var response = await Client.GetAsync($"/api/messaging/workspace/{_testWorkspace.Id}/history?pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var historyResponse = JsonSerializer.Deserialize<MessageHistoryResponse>(responseContent, TestJsonOptions.Default);

            historyResponse.Should().NotBeNull();
            historyResponse!.Messages.Should().HaveCount(2);
            historyResponse.TotalCount.Should().Be(3);
            historyResponse.PageNumber.Should().Be(1);
            historyResponse.PageSize.Should().Be(2);
            historyResponse.HasNextPage.Should().BeTrue();
        }

        [Fact]
        [SlowTest]
        public async Task SearchMessages_Should_Return_200_And_Filtered_Results()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Send messages with different content
            var messages = new[]
            {
                "This message contains the keyword search",
                "Another message about development",
                "Search functionality is important",
                "Random message without the keyword"
            };

            foreach (var messageText in messages)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = messageText,
                    MessageType = MessageType.Text
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await AddCsrfTokenToRequest(content);
                await Client.PostAsync("/api/messaging/send", content);
            }

            // Act
            var response = await Client.GetAsync($"/api/messaging/workspace/{_testWorkspace.Id}/search?query=search");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<SearchMessagesResponse>(responseContent, TestJsonOptions.Default);

            searchResponse.Should().NotBeNull();
            searchResponse!.Messages.Should().HaveCount(2); // Two messages contain "search"
            searchResponse.Query.Should().Be("search");
        }

        [Fact]
        [FastTest]
        public async Task SearchMessages_Should_Return_400_For_Empty_Query()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Act
            var response = await Client.GetAsync($"/api/messaging/workspace/{_testWorkspace.Id}/search?query=");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        [FastTest]
        public async Task GetMessage_Should_Return_200_And_MessageDto()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Send a message first
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to retrieve",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Act
            var response = await Client.GetAsync($"/api/messaging/{sentMessage!.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var retrievedMessage = JsonSerializer.Deserialize<MessageDto>(responseContent, TestJsonOptions.Default);

            retrievedMessage.Should().NotBeNull();
            retrievedMessage!.Id.Should().Be(sentMessage.Id);
            retrievedMessage.MessageText.Should().Be("Message to retrieve");
        }

        [Fact]
        [FastTest]
        public async Task GetMessage_Should_Return_404_For_Nonexistent_Message()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Act
            var response = await Client.GetAsync($"/api/messaging/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        [FastTest]
        public async Task AddReaction_Should_Return_200()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Send a message first
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message to react to",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Add reaction
            var reactionRequest = new AddReactionRequest
            {
                Emoji = "👍"
            };

            var reactionJson = JsonSerializer.Serialize(reactionRequest);
            var reactionContent = new StringContent(reactionJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(reactionContent);

            // Act
            var response = await Client.PostAsync($"/api/messaging/{sentMessage!.Id}/reactions", reactionContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        [FastTest]
        public async Task RemoveReaction_Should_Return_200()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Send a message and add a reaction
            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message with reaction",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            var sendResponse = await Client.PostAsync("/api/messaging/send", sendContent);
            var sentMessage = JsonSerializer.Deserialize<MessageDto>(await sendResponse.Content.ReadAsStringAsync(), TestJsonOptions.Default);

            // Add reaction
            var reactionRequest = new AddReactionRequest { Emoji = "👍" };
            var reactionJson = JsonSerializer.Serialize(reactionRequest);
            var reactionContent = new StringContent(reactionJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(reactionContent);
            await Client.PostAsync($"/api/messaging/{sentMessage!.Id}/reactions", reactionContent);

            // Act - Remove reaction
            var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/messaging/{sentMessage.Id}/reactions/👍");
            await AddCsrfTokenToRequest(deleteRequest);
            var response = await Client.SendAsync(deleteRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        [FastTest]
        public async Task GetUnreadMessageCount_Should_Return_200_And_Count()
        {
            // Arrange

            // Send a message as user1
            AuthenticateAs(_testUser1);

            var sendRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Unread message",
                MessageType = MessageType.Text
            };

            var sendJson = JsonSerializer.Serialize(sendRequest);
            var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(sendContent);
            await Client.PostAsync("/api/messaging/send", sendContent);

            // Check unread count for user2
            AuthenticateAs(_testUser2);

            // Act
            var response = await Client.GetAsync("/api/messaging/unread-count");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var count = JsonSerializer.Deserialize<int>(responseContent);
            count.Should().Be(1);
        }

        [Fact]
        [FastTest]
        public async Task GetWorkspaceUnreadCount_Should_Return_200_And_WorkspaceSpecific_Count()
        {
            // Arrange

            // Send messages as user1
            AuthenticateAs(_testUser1);

            for (int i = 0; i < 3; i++)
            {
                var sendRequest = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Unread message {i + 1}",
                    MessageType = MessageType.Text
                };

                var sendJson = JsonSerializer.Serialize(sendRequest);
                var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
                await AddCsrfTokenToRequest(sendContent);
                await Client.PostAsync("/api/messaging/send", sendContent);
            }

            // Check workspace-specific unread count for user2
            AuthenticateAs(_testUser2);

            // Act
            var response = await Client.GetAsync($"/api/messaging/workspace/{_testWorkspace.Id}/unread-count");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var count = JsonSerializer.Deserialize<int>(responseContent);
            count.Should().Be(3);
        }

        [Fact]
        [FastTest]
        public async Task GetMessageStats_Should_Return_200_And_Statistics()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Send various types of messages
            var textRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Text message",
                MessageType = MessageType.Text
            };

            var fileRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageType = MessageType.File,
                AttachmentFileName = "test.pdf"
            };

            var textJson = JsonSerializer.Serialize(textRequest);
            var textContent = new StringContent(textJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(textContent);
            await Client.PostAsync("/api/messaging/send", textContent);

            var fileJson = JsonSerializer.Serialize(fileRequest);
            var fileContent = new StringContent(fileJson, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(fileContent);
            await Client.PostAsync("/api/messaging/send", fileContent);

            // Act
            var response = await Client.GetAsync($"/api/messaging/workspace/{_testWorkspace.Id}/stats");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var stats = JsonSerializer.Deserialize<MessageStatsDto>(responseContent, TestJsonOptions.Default);

            stats.Should().NotBeNull();
            stats!.WorkspaceId.Should().Be(_testWorkspace.Id);
            stats.TotalMessages.Should().Be(2);
        }

        [Fact]
        [FastTest]
        public async Task SendMessage_With_File_Attachment_Should_Return_201()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageType = MessageType.File,
                AttachmentUrl = "https://example.com/test-document.pdf",
                AttachmentFileName = "test-document.pdf",
                AttachmentSize = 1024000,
                AttachmentMimeType = "application/pdf"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await AddCsrfTokenToRequest(content);

            // Act
            var response = await Client.PostAsync("/api/messaging/send", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var messageDto = JsonSerializer.Deserialize<MessageDto>(responseContent, TestJsonOptions.Default);

            messageDto.Should().NotBeNull();
            messageDto!.MessageType.Should().Be(MessageType.File);
            messageDto.AttachmentUrl.Should().Be("https://example.com/test-document.pdf");
            messageDto.AttachmentFileName.Should().Be("test-document.pdf");
            messageDto.AttachmentSize.Should().Be(1024000);
            messageDto.AttachmentMimeType.Should().Be("application/pdf");
        }

        [Fact]
        [SlowTest]
        public async Task MarkAllMessagesAsRead_Should_Return_200_And_Count()
        {
            // Arrange

            // Send messages as user1
            AuthenticateAs(_testUser1);

            for (int i = 0; i < 5; i++)
            {
                var sendRequest = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Message {i + 1}",
                    MessageType = MessageType.Text
                };

                var sendJson = JsonSerializer.Serialize(sendRequest);
                var sendContent = new StringContent(sendJson, Encoding.UTF8, "application/json");
                await AddCsrfTokenToRequest(sendContent);
                await Client.PostAsync("/api/messaging/send", sendContent);
            }

            // Mark all as read by user2
            AuthenticateAs(_testUser2);

            // Act
            var readAllRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/messaging/workspace/{_testWorkspace.Id}/read-all");
            await AddCsrfTokenToRequest(readAllRequest);
            var response = await Client.SendAsync(readAllRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var count = JsonSerializer.Deserialize<int>(responseContent);
            count.Should().Be(5);
        }

        [Fact]
        [FastTest]
        public async Task GetTypingIndicators_Should_Return_200_And_Empty_List_Initially()
        {
            // Arrange
            AuthenticateAs(_testUser1);

            // Act
            var response = await Client.GetAsync($"/api/messaging/workspace/{_testWorkspace.Id}/typing");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var indicators = JsonSerializer.Deserialize<List<TypingIndicatorDto>>(responseContent, TestJsonOptions.Default);

            indicators.Should().NotBeNull();
            indicators!.Should().BeEmpty();
        }
    }
}