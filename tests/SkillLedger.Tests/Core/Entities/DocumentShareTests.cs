using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    /// <summary>
    /// Unit tests for DocumentShare entity following TDD principles
    /// Tests focus on share permissions, expiration logic, and security
    /// </summary>
    [UnitTest]
    [DocumentTest]
    public class DocumentShareTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var share = new DocumentShare();

            // Assert
            Assert.NotEqual(Guid.Empty, share.Id);
            Assert.Equal(SharePermission.View, share.Permission);
            Assert.True(share.IsActive);
            Assert.True(share.CreatedAt <= DateTime.UtcNow);
            Assert.True(share.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.Null(share.ExpiresAt);
            Assert.Null(share.RevokedAt);
            Assert.Null(share.RevokedBy);
        }

        [Fact]
        public void IsActiveAndValid_WithActiveNonExpiredShare_ShouldReturnTrue()
        {
            // Arrange
            var share = new DocumentShare
            {
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1) // Future expiration
            };

            // Act
            var result = share.IsActiveAndValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsActiveAndValid_WithInactiveShare_ShouldReturnFalse()
        {
            // Arrange
            var share = new DocumentShare
            {
                IsActive = false,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = share.IsActiveAndValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsActiveAndValid_WithRevokedShare_ShouldReturnFalse()
        {
            // Arrange
            var share = new DocumentShare
            {
                IsActive = true,
                RevokedAt = DateTime.UtcNow.AddMinutes(-1)
            };

            // Act
            var result = share.IsActiveAndValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsActiveAndValid_WithExpiredShare_ShouldReturnFalse()
        {
            // Arrange
            var share = new DocumentShare
            {
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Past expiration
            };

            // Act
            var result = share.IsActiveAndValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsActiveAndValid_WithNoExpirationDate_ShouldReturnTrue()
        {
            // Arrange
            var share = new DocumentShare
            {
                IsActive = true,
                ExpiresAt = null // No expiration
            };

            // Act
            var result = share.IsActiveAndValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsActiveAndValid_WithExactExpirationTime_ShouldReturnFalse()
        {
            // Arrange - Set expiration to exactly now or slightly past
            var exactNow = DateTime.UtcNow;
            var share = new DocumentShare
            {
                IsActive = true,
                ExpiresAt = exactNow.AddMilliseconds(-1) // Just expired
            };

            // Act
            var result = share.IsActiveAndValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Revoke_ShouldSetInactiveAndMetadata()
        {
            // Arrange
            var share = new DocumentShare { IsActive = true };
            var revokedBy = Guid.NewGuid();
            var beforeRevoke = DateTime.UtcNow;

            // Act
            share.Revoke(revokedBy);

            // Assert
            Assert.False(share.IsActive);
            Assert.NotNull(share.RevokedAt);
            Assert.True(share.RevokedAt >= beforeRevoke);
            Assert.Equal(revokedBy, share.RevokedBy);
        }

        [Fact]
        public void Revoke_WhenAlreadyRevoked_ShouldUpdateMetadata()
        {
            // Arrange
            var share = new DocumentShare { IsActive = true };
            var firstRevoker = Guid.NewGuid();
            var secondRevoker = Guid.NewGuid();

            share.Revoke(firstRevoker);
            var firstRevokedAt = share.RevokedAt;
            Thread.Sleep(1);

            // Act
            share.Revoke(secondRevoker);

            // Assert
            Assert.False(share.IsActive);
            Assert.True(share.RevokedAt > firstRevokedAt);
            Assert.Equal(secondRevoker, share.RevokedBy);
        }

        [Fact]
        public void ExtendExpiration_WithFutureDate_ShouldUpdateExpiration()
        {
            // Arrange
            var share = new DocumentShare
            {
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            var newExpiration = DateTime.UtcNow.AddDays(7);

            // Act
            share.ExtendExpiration(newExpiration);

            // Assert
            Assert.Equal(newExpiration, share.ExpiresAt);
        }

        [Fact]
        public void ExtendExpiration_WithPastDate_ShouldNotUpdateExpiration()
        {
            // Arrange
            var originalExpiration = DateTime.UtcNow.AddDays(1);
            var share = new DocumentShare
            {
                ExpiresAt = originalExpiration
            };
            var pastDate = DateTime.UtcNow.AddDays(-1);

            // Act
            share.ExtendExpiration(pastDate);

            // Assert
            Assert.Equal(originalExpiration, share.ExpiresAt);
        }

        [Fact]
        public void ExtendExpiration_WithCurrentTime_ShouldNotUpdateExpiration()
        {
            // Arrange
            var originalExpiration = DateTime.UtcNow.AddDays(1);
            var share = new DocumentShare
            {
                ExpiresAt = originalExpiration
            };
            var currentTime = DateTime.UtcNow;

            // Act
            share.ExtendExpiration(currentTime);

            // Assert
            Assert.Equal(originalExpiration, share.ExpiresAt);
        }

        [Theory]
        [InlineData(SharePermission.View, SharePermission.View, true)]
        [InlineData(SharePermission.Edit, SharePermission.View, true)]
        [InlineData(SharePermission.Admin, SharePermission.View, true)]
        [InlineData(SharePermission.Admin, SharePermission.Edit, true)]
        [InlineData(SharePermission.View, SharePermission.Edit, false)]
        [InlineData(SharePermission.View, SharePermission.Admin, false)]
        [InlineData(SharePermission.Edit, SharePermission.Admin, false)]
        public void HasPermission_WithVariousPermissionLevels_ShouldReturnExpectedResult(
            SharePermission userPermission,
            SharePermission requiredPermission,
            bool expectedResult)
        {
            // Arrange
            var share = new DocumentShare
            {
                Permission = userPermission,
                IsActive = true
            };

            // Act
            var result = share.HasPermission(requiredPermission);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void HasPermission_WithInactiveShare_ShouldReturnFalse()
        {
            // Arrange
            var share = new DocumentShare
            {
                Permission = SharePermission.Admin,
                IsActive = false
            };

            // Act
            var result = share.HasPermission(SharePermission.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasPermission_WithExpiredShare_ShouldReturnFalse()
        {
            // Arrange
            var share = new DocumentShare
            {
                Permission = SharePermission.Admin,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            };

            // Act
            var result = share.HasPermission(SharePermission.View);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Properties_ShouldAcceptValidValues()
        {
            // Arrange
            var share = new DocumentShare();
            var documentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var sharedBy = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(30);
            var accessToken = "secure-token-123";
            var shareMessage = "Sharing this important document with you";

            // Act
            share.DocumentId = documentId;
            share.UserId = userId;
            share.SharedBy = sharedBy;
            share.Permission = SharePermission.Edit;
            share.ExpiresAt = expiresAt;
            share.AccessToken = accessToken;
            share.ShareMessage = shareMessage;

            // Assert
            Assert.Equal(documentId, share.DocumentId);
            Assert.Equal(userId, share.UserId);
            Assert.Equal(sharedBy, share.SharedBy);
            Assert.Equal(SharePermission.Edit, share.Permission);
            Assert.Equal(expiresAt, share.ExpiresAt);
            Assert.Equal(accessToken, share.AccessToken);
            Assert.Equal(shareMessage, share.ShareMessage);
        }

        [Fact]
        public void ShareMessage_ShouldAcceptLongText()
        {
            // Arrange
            var share = new DocumentShare();
            var longMessage = new string('A', 999); // Close to 1000 char limit

            // Act
            share.ShareMessage = longMessage;

            // Assert
            Assert.Equal(longMessage, share.ShareMessage);
        }

        [Fact]
        public void AccessToken_ShouldAcceptSecureToken()
        {
            // Arrange
            var share = new DocumentShare();
            var secureToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            share.AccessToken = secureToken;

            // Assert
            Assert.Equal(secureToken, share.AccessToken);
        }

        [Fact]
        public void CompleteShareLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var share = new DocumentShare
            {
                DocumentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SharedBy = Guid.NewGuid(),
                Permission = SharePermission.Edit,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            // Act & Assert - Initial state
            Assert.True(share.IsActiveAndValid());
            Assert.True(share.HasPermission(SharePermission.View));
            Assert.True(share.HasPermission(SharePermission.Edit));
            Assert.False(share.HasPermission(SharePermission.Admin));

            // Act & Assert - Extend expiration
            var newExpiration = DateTime.UtcNow.AddDays(14);
            share.ExtendExpiration(newExpiration);
            Assert.Equal(newExpiration, share.ExpiresAt);
            Assert.True(share.IsActiveAndValid());

            // Act & Assert - Revoke share
            var revokedBy = Guid.NewGuid();
            share.Revoke(revokedBy);
            Assert.False(share.IsActiveAndValid());
            Assert.False(share.HasPermission(SharePermission.View));
            Assert.Equal(revokedBy, share.RevokedBy);
            Assert.NotNull(share.RevokedAt);
        }
    }
}