using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    [UnitTest]
    [MessagingTest]
    public class MessageReactionTests
    {
        [Fact]
        public void MessageReaction_Should_Initialize_With_Default_Values()
        {
            // Act
            var reaction = new MessageReaction();

            // Assert
            reaction.Id.Should().NotBeEmpty();
            reaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("👍")]
        [InlineData("❤️")]
        [InlineData("😄")]
        [InlineData("🎉")]
        [InlineData("🔥")]
        public void Emoji_Should_Accept_Valid_Emoji_Characters(string emoji)
        {
            // Arrange
            var reaction = new MessageReaction();

            // Act
            reaction.Emoji = emoji;

            // Assert
            reaction.Emoji.Should().Be(emoji);
        }

        [Fact]
        public void MessageReaction_Should_Have_Required_Properties_Set()
        {
            // Arrange
            var messageId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var emoji = "👍";

            // Act
            var reaction = new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji
            };

            // Assert
            reaction.MessageId.Should().Be(messageId);
            reaction.UserId.Should().Be(userId);
            reaction.Emoji.Should().Be(emoji);
        }

        [Fact]
        public void MessageReaction_Should_Track_IP_Address_For_Auditing()
        {
            // Arrange
            var ipAddress = "192.168.1.1";
            var reaction = new MessageReaction();

            // Act
            reaction.IpAddress = ipAddress;

            // Assert
            reaction.IpAddress.Should().Be(ipAddress);
        }

        [Fact]
        public void MessageReaction_Should_Support_IPv6_Address()
        {
            // Arrange
            var ipv6Address = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
            var reaction = new MessageReaction();

            // Act
            reaction.IpAddress = ipv6Address;

            // Assert
            reaction.IpAddress.Should().Be(ipv6Address);
        }
    }
}