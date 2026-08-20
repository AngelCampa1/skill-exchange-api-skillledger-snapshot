using SkillLedger.Core.Entities;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Core.Entities
{
    /// <summary>
    /// Unit tests for DocumentAccess entity following TDD principles
    /// Tests focus on audit logging, access tracking, and metadata validation
    /// </summary>
    [UnitTest]
    [DocumentTest]
    public class DocumentAccessTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Act
            var access = new DocumentAccess();

            // Assert
            Assert.NotEqual(Guid.Empty, access.Id);
            Assert.Equal("view", access.AccessType);
            Assert.True(access.AccessedAt <= DateTime.UtcNow);
            Assert.True(access.AccessedAt > DateTime.UtcNow.AddMinutes(-1));
            Assert.Null(access.IpAddress);
            Assert.Null(access.UserAgent);
            Assert.Null(access.Metadata);
        }

        [Fact]
        public void Properties_ShouldAcceptValidValues()
        {
            // Arrange
            var access = new DocumentAccess();
            var documentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var accessedAt = DateTime.UtcNow.AddMinutes(-5);
            var ipAddress = "192.168.1.100";
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
            var metadata = "{\"source\":\"web\",\"feature\":\"document_viewer\"}";

            // Act
            access.DocumentId = documentId;
            access.UserId = userId;
            access.AccessedAt = accessedAt;
            access.AccessType = "download";
            access.IpAddress = ipAddress;
            access.UserAgent = userAgent;
            access.Metadata = metadata;

            // Assert
            Assert.Equal(documentId, access.DocumentId);
            Assert.Equal(userId, access.UserId);
            Assert.Equal(accessedAt, access.AccessedAt);
            Assert.Equal("download", access.AccessType);
            Assert.Equal(ipAddress, access.IpAddress);
            Assert.Equal(userAgent, access.UserAgent);
            Assert.Equal(metadata, access.Metadata);
        }

        [Theory]
        [InlineData("view")]
        [InlineData("download")]
        [InlineData("preview")]
        [InlineData("edit")]
        [InlineData("share")]
        public void AccessType_ShouldAcceptValidAccessTypes(string accessType)
        {
            // Arrange
            var access = new DocumentAccess();

            // Act
            access.AccessType = accessType;

            // Assert
            Assert.Equal(accessType, access.AccessType);
        }

        [Theory]
        [InlineData("127.0.0.1")] // IPv4 localhost
        [InlineData("192.168.1.100")] // IPv4 private
        [InlineData("10.0.0.1")] // IPv4 private
        [InlineData("203.0.113.1")] // IPv4 public
        [InlineData("::1")] // IPv6 localhost
        [InlineData("2001:db8::1")] // IPv6 example
        [InlineData("fe80::1%lo0")] // IPv6 with zone identifier
        public void IpAddress_ShouldAcceptValidIpAddresses(string ipAddress)
        {
            // Arrange
            var access = new DocumentAccess();

            // Act
            access.IpAddress = ipAddress;

            // Assert
            Assert.Equal(ipAddress, access.IpAddress);
        }

        [Fact]
        public void IpAddress_ShouldHandleLongIpv6Address()
        {
            // Arrange
            var access = new DocumentAccess();
            var longIpv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334"; // 39 characters

            // Act
            access.IpAddress = longIpv6;

            // Assert
            Assert.Equal(longIpv6, access.IpAddress);
        }

        [Theory]
        [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")]
        [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")]
        [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36")]
        [InlineData("SkillLedger-Mobile/1.0.0 (iOS 14.6)")]
        [InlineData("SkillLedger-Desktop/2.1.0 (Windows 10)")]
        public void UserAgent_ShouldAcceptValidUserAgents(string userAgent)
        {
            // Arrange
            var access = new DocumentAccess();

            // Act
            access.UserAgent = userAgent;

            // Assert
            Assert.Equal(userAgent, access.UserAgent);
        }

        [Fact]
        public void UserAgent_ShouldHandleLongUserAgent()
        {
            // Arrange
            var access = new DocumentAccess();
            var longUserAgent = new string('A', 499); // Close to 500 char limit

            // Act
            access.UserAgent = longUserAgent;

            // Assert
            Assert.Equal(longUserAgent, access.UserAgent);
        }

        [Theory]
        [InlineData("{\"source\":\"web\"}")]
        [InlineData("{\"source\":\"mobile\",\"version\":\"1.0.0\"}")]
        [InlineData("{\"feature\":\"document_viewer\",\"user_action\":\"download\",\"session_id\":\"abc123\"}")]
        [InlineData("null")] // JSON null
        [InlineData("\"simple string\"")] // JSON string
        public void Metadata_ShouldAcceptValidJsonStrings(string metadata)
        {
            // Arrange
            var access = new DocumentAccess();

            // Act
            access.Metadata = metadata;

            // Assert
            Assert.Equal(metadata, access.Metadata);
        }

        [Fact]
        public void AccessedAt_ShouldAcceptPastDates()
        {
            // Arrange
            var access = new DocumentAccess();
            var pastDate = DateTime.UtcNow.AddDays(-7);

            // Act
            access.AccessedAt = pastDate;

            // Assert
            Assert.Equal(pastDate, access.AccessedAt);
        }

        [Fact]
        public void AccessedAt_ShouldAcceptFutureDates()
        {
            // Arrange
            var access = new DocumentAccess();
            var futureDate = DateTime.UtcNow.AddMinutes(1); // Slight future for timing edge cases

            // Act
            access.AccessedAt = futureDate;

            // Assert
            Assert.Equal(futureDate, access.AccessedAt);
        }

        [Fact]
        public void AllProperties_ShouldBeSettableIndependently()
        {
            // Arrange
            var access = new DocumentAccess();
            var documentId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act - Set properties in different order
            access.AccessType = "preview";
            access.DocumentId = documentId;
            access.Metadata = "{\"test\":\"value\"}";
            access.UserId = userId;
            access.IpAddress = "10.0.0.1";
            access.UserAgent = "Test Agent";
            access.AccessedAt = DateTime.UtcNow.AddMinutes(-10);

            // Assert - All properties should be set correctly
            Assert.Equal("preview", access.AccessType);
            Assert.Equal(documentId, access.DocumentId);
            Assert.Equal("{\"test\":\"value\"}", access.Metadata);
            Assert.Equal(userId, access.UserId);
            Assert.Equal("10.0.0.1", access.IpAddress);
            Assert.Equal("Test Agent", access.UserAgent);
            Assert.True(access.AccessedAt < DateTime.UtcNow);
        }

        [Fact]
        public void NavigationProperties_ShouldBeSettable()
        {
            // Arrange
            var access = new DocumentAccess();
            var document = new WorkspaceDocument { Id = Guid.NewGuid() };
            var user = new User { Id = Guid.NewGuid() };

            // Act
            access.Document = document;
            access.User = user;

            // Assert
            Assert.Equal(document, access.Document);
            Assert.Equal(user, access.User);
        }

        [Fact]
        public void OptionalProperties_ShouldAcceptNullValues()
        {
            // Arrange
            var access = new DocumentAccess();

            // Act & Assert - All optional properties should accept null
            access.IpAddress = null;
            access.UserAgent = null;
            access.Metadata = null;

            Assert.Null(access.IpAddress);
            Assert.Null(access.UserAgent);
            Assert.Null(access.Metadata);
        }

        [Fact]
        public void RequiredProperties_ShouldNotAcceptDefaultValues()
        {
            // Arrange
            var access = new DocumentAccess();

            // Act
            access.DocumentId = Guid.NewGuid();
            access.UserId = Guid.NewGuid();

            // Assert - Required properties should have non-default values
            Assert.NotEqual(Guid.Empty, access.DocumentId);
            Assert.NotEqual(Guid.Empty, access.UserId);
            Assert.NotNull(access.AccessType);
            Assert.NotEmpty(access.AccessType);
        }

        [Fact]
        public void CompleteAccessRecord_ShouldBeValid()
        {
            // Arrange & Act - Create a complete access record
            var access = new DocumentAccess
            {
                DocumentId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AccessedAt = DateTime.UtcNow,
                AccessType = "download",
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                Metadata = "{\"source\":\"web\",\"feature\":\"document_download\",\"file_size\":1048576}"
            };

            // Assert - All properties should be set and valid
            Assert.NotEqual(Guid.Empty, access.Id);
            Assert.NotEqual(Guid.Empty, access.DocumentId);
            Assert.NotEqual(Guid.Empty, access.UserId);
            Assert.Equal("download", access.AccessType);
            Assert.Equal("192.168.1.100", access.IpAddress);
            Assert.Contains("Mozilla", access.UserAgent);
            Assert.Contains("source", access.Metadata);
            Assert.True(access.AccessedAt <= DateTime.UtcNow);
        }
    }
}