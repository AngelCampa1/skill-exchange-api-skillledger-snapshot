using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    [UnitTest]
    [MessagingTest]
    public class WorkspaceMessageTests
    {
        [Fact]
        public void WorkspaceMessage_Should_Initialize_With_Default_Values()
        {
            // Act
            var message = new WorkspaceMessage();

            // Assert
            message.Id.Should().NotBeEmpty();
            message.MessageType.Should().Be(MessageType.Text);
            message.Status.Should().Be(MessageStatus.Sent);
            message.IsEdited.Should().BeFalse();
            message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void MarkAsEdited_Should_Update_IsEdited_And_EditedAt()
        {
            // Arrange
            var message = new WorkspaceMessage();
            var originalCreatedAt = message.CreatedAt;

            // Act
            message.MarkAsEdited();

            // Assert
            message.IsEdited.Should().BeTrue();
            message.EditedAt.Should().NotBeNull();
            message.EditedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            message.CreatedAt.Should().Be(originalCreatedAt); // Should not change
        }

        [Fact]
        public void MarkAsRead_Should_Update_Status_And_ReadAt()
        {
            // Arrange
            var message = new WorkspaceMessage { Status = MessageStatus.Delivered };

            // Act
            message.MarkAsRead();

            // Assert
            message.Status.Should().Be(MessageStatus.Read);
            message.ReadAt.Should().NotBeNull();
            message.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void MarkAsRead_Should_Not_Change_Already_Read_Message()
        {
            // Arrange
            var initialReadTime = DateTime.UtcNow.AddMinutes(-5);
            var message = new WorkspaceMessage
            {
                Status = MessageStatus.Read,
                ReadAt = initialReadTime
            };

            // Act
            message.MarkAsRead();

            // Assert
            message.Status.Should().Be(MessageStatus.Read);
            message.ReadAt.Should().Be(initialReadTime); // Should not change
        }

        [Fact]
        public void MarkAsDelivered_Should_Update_Status_From_Sent_Only()
        {
            // Arrange
            var message = new WorkspaceMessage { Status = MessageStatus.Sent };

            // Act
            message.MarkAsDelivered();

            // Assert
            message.Status.Should().Be(MessageStatus.Delivered);
        }

        [Fact]
        public void MarkAsDelivered_Should_Not_Change_Read_Status()
        {
            // Arrange
            var message = new WorkspaceMessage { Status = MessageStatus.Read };

            // Act
            message.MarkAsDelivered();

            // Assert
            message.Status.Should().Be(MessageStatus.Read); // Should remain read
        }

        [Fact]
        public void CanBeEditedBy_Should_Return_True_For_Own_Text_Message_Within_24_Hours()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = userId,
                MessageType = MessageType.Text,
                CreatedAt = DateTime.UtcNow.AddHours(-1) // 1 hour ago
            };

            // Act
            var canEdit = message.CanBeEditedBy(userId);

            // Assert
            canEdit.Should().BeTrue();
        }

        [Fact]
        public void CanBeEditedBy_Should_Return_False_For_Other_User()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = senderId,
                MessageType = MessageType.Text,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Act
            var canEdit = message.CanBeEditedBy(otherUserId);

            // Assert
            canEdit.Should().BeFalse();
        }

        [Fact]
        public void CanBeEditedBy_Should_Return_False_For_Non_Text_Message()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = userId,
                MessageType = MessageType.File,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Act
            var canEdit = message.CanBeEditedBy(userId);

            // Assert
            canEdit.Should().BeFalse();
        }

        [Fact]
        public void CanBeEditedBy_Should_Return_False_For_Message_Older_Than_24_Hours()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = userId,
                MessageType = MessageType.Text,
                CreatedAt = DateTime.UtcNow.AddHours(-25) // 25 hours ago
            };

            // Act
            var canEdit = message.CanBeEditedBy(userId);

            // Assert
            canEdit.Should().BeFalse();
        }

        [Fact]
        public void CanBeDeletedBy_Should_Return_True_For_Own_Message_Within_24_Hours()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = userId,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Act
            var canDelete = message.CanBeDeletedBy(userId);

            // Assert
            canDelete.Should().BeTrue();
        }

        [Fact]
        public void CanBeDeletedBy_Should_Return_False_For_Other_User()
        {
            // Arrange
            var senderId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = senderId,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            // Act
            var canDelete = message.CanBeDeletedBy(otherUserId);

            // Assert
            canDelete.Should().BeFalse();
        }

        [Fact]
        public void CanBeDeletedBy_Should_Return_False_For_Message_Older_Than_24_Hours()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var message = new WorkspaceMessage
            {
                SenderId = userId,
                CreatedAt = DateTime.UtcNow.AddHours(-25)
            };

            // Act
            var canDelete = message.CanBeDeletedBy(userId);

            // Assert
            canDelete.Should().BeFalse();
        }

        [Theory]
        [InlineData(MessageType.Text)]
        [InlineData(MessageType.File)]
        [InlineData(MessageType.Image)]
        [InlineData(MessageType.Voice)]
        [InlineData(MessageType.System)]
        [InlineData(MessageType.Milestone)]
        public void MessageType_Should_Accept_All_Valid_Types(MessageType messageType)
        {
            // Arrange & Act
            var message = new WorkspaceMessage { MessageType = messageType };

            // Assert
            message.MessageType.Should().Be(messageType);
        }

        [Theory]
        [InlineData(MessageStatus.Sent)]
        [InlineData(MessageStatus.Delivered)]
        [InlineData(MessageStatus.Read)]
        [InlineData(MessageStatus.Failed)]
        [InlineData(MessageStatus.Deleted)]
        public void MessageStatus_Should_Accept_All_Valid_Statuses(MessageStatus status)
        {
            // Arrange & Act
            var message = new WorkspaceMessage { Status = status };

            // Assert
            message.Status.Should().Be(status);
        }

        [Fact]
        public void MessageText_Should_Support_Long_Content()
        {
            // Arrange
            var longText = new string('A', 3000); // 3000 characters
            var message = new WorkspaceMessage();

            // Act
            message.MessageText = longText;

            // Assert
            message.MessageText.Should().Be(longText);
            message.MessageText.Length.Should().Be(3000);
        }
    }
}