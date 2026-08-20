using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    [UnitTest]
    [MessagingTest]
    public class TypingIndicatorTests
    {
        [Fact]
        public void TypingIndicator_Should_Initialize_With_Default_Values()
        {
            // Act
            var indicator = new TypingIndicator();

            // Assert
            indicator.Id.Should().NotBeEmpty();
            indicator.LastTypingAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void IsActive_Should_Return_True_For_Recent_Typing()
        {
            // Arrange
            var indicator = new TypingIndicator
            {
                LastTypingAt = DateTime.UtcNow.AddSeconds(-2) // 2 seconds ago
            };

            // Act
            var isActive = indicator.IsActive();

            // Assert
            isActive.Should().BeTrue();
        }

        [Fact]
        public void IsActive_Should_Return_False_For_Old_Typing()
        {
            // Arrange
            var indicator = new TypingIndicator
            {
                LastTypingAt = DateTime.UtcNow.AddSeconds(-6) // 6 seconds ago
            };

            // Act
            var isActive = indicator.IsActive();

            // Assert
            isActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_Should_Return_False_For_Exactly_5_Second_Old_Typing()
        {
            // Arrange
            var indicator = new TypingIndicator
            {
                LastTypingAt = DateTime.UtcNow.AddSeconds(-5) // Exactly 5 seconds ago
            };

            // Act
            var isActive = indicator.IsActive();

            // Assert
            isActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateTyping_Should_Refresh_LastTypingAt()
        {
            // Arrange
            var indicator = new TypingIndicator
            {
                LastTypingAt = DateTime.UtcNow.AddSeconds(-10) // 10 seconds ago
            };
            var originalTime = indicator.LastTypingAt;

            // Act
            indicator.UpdateTyping();

            // Assert
            indicator.LastTypingAt.Should().BeAfter(originalTime);
            indicator.LastTypingAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void TypingIndicator_Should_Have_Required_Properties_Set()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var connectionId = "test-connection-123";

            // Act
            var indicator = new TypingIndicator
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                ConnectionId = connectionId
            };

            // Assert
            indicator.WorkspaceId.Should().Be(workspaceId);
            indicator.UserId.Should().Be(userId);
            indicator.ConnectionId.Should().Be(connectionId);
        }

        [Fact]
        public void ConnectionId_Should_Support_Long_SignalR_Connection_Ids()
        {
            // Arrange
            var longConnectionId = new string('A', 90); // 90 characters
            var indicator = new TypingIndicator();

            // Act
            indicator.ConnectionId = longConnectionId;

            // Assert
            indicator.ConnectionId.Should().Be(longConnectionId);
            indicator.ConnectionId.Length.Should().Be(90);
        }
    }
}