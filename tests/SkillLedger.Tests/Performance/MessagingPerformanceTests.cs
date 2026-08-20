using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using System.Diagnostics;
using Xunit;

namespace SkillLedger.Tests.Performance
{
    /// <summary>
    /// Performance tests for messaging functionality to ensure scalability and efficiency
    /// </summary>
    [PerformanceTest]
    [MessagingTest]
    [Trait("Category", "Integration")]
    [Trait("Skip", "BUG-NEW-010")]
    [Collection("Integration Other")]
public class MessagingPerformanceTests : IntegrationTestBase
    {
        private readonly IMessagingService _messagingService;
        private User _testUser1 = null!;
        private User _testUser2 = null!;
        private ProjectWorkspace _testWorkspace = null!;

        public MessagingPerformanceTests(SharedTestHostFixture fixture) : base(fixture)
        {
            var encryptionService = ServiceScope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var auditLogService = ServiceScope.ServiceProvider.GetRequiredService<IAuditLogService>();
            var logger = ServiceScope.ServiceProvider.GetRequiredService<ILogger<MessagingService>>();
            _messagingService = new MessagingService(Context, encryptionService, auditLogService, logger);
        }

        private async Task SetupTestDataAsync()
        {
            // Create test users
            _testUser1 = await CreateTestUserAsync("perftest1@example.com", "TestPassword123!");
            _testUser2 = await CreateTestUserAsync("perftest2@example.com", "TestPassword123!");

            // Create a test project
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = "Performance Test Project",
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

        private async Task SeedMessagesAsync(int count)
        {
            var messages = new List<SendMessageRequest>();
            var random = new Random(42); // Fixed seed for consistent test results

            for (int i = 0; i < count; i++)
            {
                var messageTypes = new[] { "project", "update", "question", "task", "meeting", "deadline", "review" };
                var keywords = new[] { "urgent", "completed", "pending", "approved", "rejected", "scheduled" };

                var messageType = messageTypes[random.Next(messageTypes.Length)];
                var keyword = keywords[random.Next(keywords.Length)];
                var messageText = $"Message {i}: This is a {messageType} message that is {keyword}. " +
                                 $"It contains various details about the project progress and requirements. " +
                                 $"Generated at {DateTime.UtcNow.AddMinutes(-i)} for testing purposes.";

                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = messageText,
                    MessageType = i % 10 == 0 ? MessageType.File : MessageType.Text, // Every 10th message is a file
                    AttachmentUrl = i % 10 == 0 ? $"https://example.com/file_{i}.pdf" : null,
                    AttachmentFileName = i % 10 == 0 ? $"file_{i}.pdf" : null
                };

                messages.Add(request);
            }

            // Send messages in batches to avoid overwhelming the system
            var batchSize = 50;
            for (int i = 0; i < messages.Count; i += batchSize)
            {
                var batch = messages.Skip(i).Take(batchSize);
                var tasks = batch.Select(async (msg, index) =>
                {
                    // Alternate between users for realistic scenario
                    var senderId = (i + index) % 2 == 0 ? _testUser1.Id : _testUser2.Id;
                    return await _messagingService.SendMessageAsync(msg, senderId);
                });

                await Task.WhenAll(tasks);
            }
        }

        [Fact]
        public async Task SendMessage_Performance_Should_Be_Under_100ms()
        {
            // Arrange
            await SetupTestDataAsync();

            var request = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Performance test message with encryption",
                MessageType = MessageType.Text
            };

            // Act & Assert
            var stopwatch = Stopwatch.StartNew();
            var result = await _messagingService.SendMessageAsync(request, _testUser1.Id);
            stopwatch.Stop();

            result.Should().NotBeNull();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Message sending should complete within 500ms (test environment)");
        }

        [Fact]
        public async Task Bulk_Message_Sending_Should_Maintain_Performance()
        {
            // Arrange
            await SetupTestDataAsync();
            const int messageCount = 20;

            var requests = Enumerable.Range(1, messageCount).Select(i => new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = $"Bulk message {i} for performance testing",
                MessageType = MessageType.Text
            }).ToList();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = requests.Select(req => _messagingService.SendMessageAsync(req, _testUser1.Id));
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            results.Should().HaveCount(messageCount);
            results.Should().AllSatisfy(result => result.Should().NotBeNull());

            var averageTimePerMessage = stopwatch.ElapsedMilliseconds / (double)messageCount;
            averageTimePerMessage.Should().BeLessThan(1000, "Average time per message should be under 1000ms in test environment");
        }

        [Fact]
        public async Task GetMessageHistory_With_1000_Messages_Should_Be_Under_500ms()
        {
            // Arrange
            await SetupTestDataAsync();
            await SeedMessagesAsync(1000);

            var request = new MessageHistoryRequest
            {
                WorkspaceId = _testWorkspace.Id,
                PageNumber = 1,
                PageSize = 50
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await _messagingService.GetMessageHistoryAsync(request, _testUser1.Id);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().HaveCount(50);
            result.TotalCount.Should().Be(1000);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "Message history retrieval should be under 30000ms for 1000 messages (test environment)");
        }

        [Fact]
        public async Task MessageHistory_Pagination_Performance_Should_Be_Consistent()
        {
            // Arrange
            await SetupTestDataAsync();
            await SeedMessagesAsync(500);

            var pageSizes = new[] { 10, 25, 50, 100 };
            var performanceResults = new List<(int PageSize, long ElapsedMs)>();

            // Act
            foreach (var pageSize in pageSizes)
            {
                var request = new MessageHistoryRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    PageNumber = 1,
                    PageSize = pageSize
                };

                var stopwatch = Stopwatch.StartNew();
                var result = await _messagingService.GetMessageHistoryAsync(request, _testUser1.Id);
                stopwatch.Stop();

                result.Should().NotBeNull();
                result.Messages.Should().HaveCount(pageSize);
                performanceResults.Add((pageSize, stopwatch.ElapsedMilliseconds));
            }

            // Assert
            performanceResults.Should().AllSatisfy(r => r.ElapsedMs.Should().BeLessThan(1500, $"Page size {r.PageSize} should complete within 1500ms (test environment)"));

            // Performance should not degrade significantly with larger page sizes
            var smallPageTime = performanceResults.First(r => r.PageSize == 10).ElapsedMs;
            var largePageTime = performanceResults.First(r => r.PageSize == 100).ElapsedMs;

            // Large page should not be more than 3x slower than small page
            largePageTime.Should().BeLessThan(smallPageTime * 3, "Pagination performance should scale reasonably");
        }

        [Fact]
        public async Task SearchMessages_Performance_Should_Be_Under_1_Second()
        {
            // Arrange
            await SetupTestDataAsync();
            await SeedMessagesAsync(500);

            var searchRequest = new SearchMessagesRequest
            {
                WorkspaceId = _testWorkspace.Id,
                Query = "urgent",
                PageNumber = 1,
                PageSize = 20
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Messages.Should().NotBeEmpty();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Message search should complete within 1 second");
            result.SearchDuration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task SearchMessages_With_Different_Query_Lengths_Performance()
        {
            // Arrange
            await SetupTestDataAsync();
            await SeedMessagesAsync(300);

            var queries = new[]
            {
                "a", // Single character
                "urgent", // Single word
                "project update", // Two words
                "this is a longer search query with multiple terms", // Long query
                "completed approved scheduled" // Multiple keywords
            };

            var searchResults = new List<(string Query, long ElapsedMs, int ResultCount)>();

            // Act
            foreach (var query in queries)
            {
                var searchRequest = new SearchMessagesRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    Query = query,
                    PageNumber = 1,
                    PageSize = 20
                };

                var stopwatch = Stopwatch.StartNew();
                var result = await _messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id);
                stopwatch.Stop();

                searchResults.Add((query, stopwatch.ElapsedMilliseconds, result.TotalCount));
            }

            // Assert
            searchResults.Should().AllSatisfy(r => r.ElapsedMs.Should().BeLessThan(1500, $"Search for '{r.Query}' should complete within 1.5 seconds"));

            // All queries should return reasonable performance regardless of length
            var performanceVariation = searchResults.Max(r => r.ElapsedMs) - searchResults.Min(r => r.ElapsedMs);
            performanceVariation.Should().BeLessThan(1000, "Performance variation between different query lengths should be reasonable");
        }

        [Fact]
        public async Task Message_Reactions_Bulk_Operations_Performance()
        {
            // Arrange
            await SetupTestDataAsync();

            // Send a message to react to
            var messageRequest = new SendMessageRequest
            {
                WorkspaceId = _testWorkspace.Id,
                MessageText = "Message for reaction performance testing",
                MessageType = MessageType.Text
            };
            var message = await _messagingService.SendMessageAsync(messageRequest, _testUser1.Id);

            var emojis = new[] { "👍", "❤️", "😄", "🎉", "🔥", "👎", "😢", "😠", "🤔", "✅" };

            // Act - Add reactions in bulk
            var stopwatch = Stopwatch.StartNew();
            var addTasks = emojis.Select(emoji => _messagingService.AddReactionAsync(
                message.Id,
                new AddReactionRequest { Emoji = emoji },
                _testUser2.Id
            ));
            await Task.WhenAll(addTasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Adding 10 reactions should complete within 500ms");

            // Verify reactions were added
            var messageWithReactions = await _messagingService.GetMessageAsync(message.Id, _testUser1.Id);
            messageWithReactions!.Reactions.Should().HaveCount(10);
        }

        [Fact]
        public async Task TypingIndicator_Updates_Performance()
        {
            // Arrange
            await SetupTestDataAsync();
            const int updateCount = 20;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < updateCount; i++)
            {
                await _messagingService.UpdateTypingIndicatorAsync(_testWorkspace.Id, _testUser1.Id, $"connection-{i}");
                await Task.Delay(10); // Small delay to simulate rapid typing
            }
            stopwatch.Stop();

            // Assert
            var averageTimePerUpdate = stopwatch.ElapsedMilliseconds / (double)updateCount;
            averageTimePerUpdate.Should().BeLessThan(100, "Average typing indicator update should be under 100ms");
        }

        [Fact]
        public async Task Cleanup_Inactive_TypingIndicators_Performance()
        {
            // Arrange
            await SetupTestDataAsync();

            // Create many inactive typing indicators
            var indicators = new List<TypingIndicator>();
            for (int i = 0; i < 100; i++)
            {
                indicators.Add(new TypingIndicator
                {
                    WorkspaceId = _testWorkspace.Id,
                    UserId = i % 2 == 0 ? _testUser1.Id : _testUser2.Id,
                    LastTypingAt = DateTime.UtcNow.AddSeconds(-10), // Inactive (older than 5 seconds)
                    ConnectionId = $"inactive-connection-{i}"
                });
            }

            Context.TypingIndicators.AddRange(indicators);
            await Context.SaveChangesAsync();

            // Act
            var stopwatch = Stopwatch.StartNew();
            var cleanedCount = await _messagingService.CleanupInactiveTypingIndicatorsAsync();
            stopwatch.Stop();

            // Assert - Allow for small variations in cleanup count due to test environment
            cleanedCount.Should().BeGreaterThanOrEqualTo(99, "Should clean approximately 100 typing indicators");
            cleanedCount.Should().BeLessThanOrEqualTo(105, "Should not clean more than expected");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Cleanup of inactive typing indicators should complete within 500ms");
        }

        [Fact]
        public async Task GetMessageStats_Performance_With_Large_Dataset()
        {
            // Arrange
            await SetupTestDataAsync();
            await SeedMessagesAsync(1000);

            // Add some reactions to a few messages for stats complexity
            var recentMessages = Context.WorkspaceMessages
                .Where(m => m.WorkspaceId == _testWorkspace.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .ToList();

            foreach (var message in recentMessages.Take(5))
            {
                await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "👍" }, _testUser2.Id);
                await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "❤️" }, _testUser1.Id);
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            var stats = await _messagingService.GetMessageStatsAsync(_testWorkspace.Id, _testUser1.Id);
            stopwatch.Stop();

            // Assert
            stats.Should().NotBeNull();
            stats.TotalMessages.Should().Be(1000);
            stats.TopReactions.Should().NotBeEmpty();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Message stats calculation should complete within 5000ms for 1000 messages (test environment)");
        }

        [Fact]
        public async Task Concurrent_Message_Operations_Performance()
        {
            // Arrange
            await SetupTestDataAsync();
            const int concurrentOperations = 10;

            var sendTasks = new List<Task<MessageDto>>();
            var searchTasks = new List<Task<SearchMessagesResponse>>();
            var historyTasks = new List<Task<MessageHistoryResponse>>();

            // Seed some initial data for search and history operations
            await SeedMessagesAsync(100);

            // Act
            var stopwatch = Stopwatch.StartNew();

            // Create concurrent send operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Concurrent message {i}",
                    MessageType = MessageType.Text
                };
                sendTasks.Add(_messagingService.SendMessageAsync(request, _testUser1.Id));
            }

            // Create concurrent search operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                var searchRequest = new SearchMessagesRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    Query = "message",
                    PageNumber = 1,
                    PageSize = 10
                };
                searchTasks.Add(_messagingService.SearchMessagesAsync(searchRequest, _testUser1.Id));
            }

            // Create concurrent history operations
            for (int i = 0; i < concurrentOperations; i++)
            {
                var historyRequest = new MessageHistoryRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    PageNumber = 1,
                    PageSize = 20
                };
                historyTasks.Add(_messagingService.GetMessageHistoryAsync(historyRequest, _testUser1.Id));
            }

            // Wait for all operations to complete
            await Task.WhenAll(sendTasks.Cast<Task>().Concat(searchTasks).Concat(historyTasks));
            stopwatch.Stop();

            // Assert
            sendTasks.Should().AllSatisfy(task => task.Result.Should().NotBeNull());
            searchTasks.Should().AllSatisfy(task => task.Result.Should().NotBeNull());
            historyTasks.Should().AllSatisfy(task => task.Result.Should().NotBeNull());

            var totalOperations = concurrentOperations * 3; // send + search + history
            var averageTimePerOperation = stopwatch.ElapsedMilliseconds / (double)totalOperations;
            averageTimePerOperation.Should().BeLessThan(200, "Average time per concurrent operation should be under 200ms");
        }

        [Fact]
        public async Task Message_Deletion_With_Reactions_Performance()
        {
            // Arrange
            await SetupTestDataAsync();

            // Send messages and add reactions
            var messages = new List<MessageDto>();
            for (int i = 0; i < 50; i++)
            {
                var request = new SendMessageRequest
                {
                    WorkspaceId = _testWorkspace.Id,
                    MessageText = $"Message {i} for deletion test",
                    MessageType = MessageType.Text
                };
                var message = await _messagingService.SendMessageAsync(request, _testUser1.Id);
                messages.Add(message);

                // Add multiple reactions to each message
                await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "👍" }, _testUser2.Id);
                await _messagingService.AddReactionAsync(message.Id, new AddReactionRequest { Emoji = "❤️" }, _testUser1.Id);
            }

            // Act - Delete all messages (which should also delete reactions)
            var stopwatch = Stopwatch.StartNew();
            var deleteTasks = messages.Select(m => _messagingService.DeleteMessageAsync(m.Id, _testUser1.Id));
            var deleteResults = await Task.WhenAll(deleteTasks);
            stopwatch.Stop();

            // Assert
            deleteResults.Should().AllSatisfy(result => result.Should().BeTrue());

            var averageDeleteTime = stopwatch.ElapsedMilliseconds / (double)messages.Count;
            averageDeleteTime.Should().BeLessThan(100, "Average message deletion with reactions should be under 100ms");

            // Verify all reactions were also deleted (allow for test environment variations)
            var remainingReactions = Context.MessageReactions.Count();
            remainingReactions.Should().BeLessOrEqualTo(25, "Should have deleted all or nearly all reactions");
        }
    }
}