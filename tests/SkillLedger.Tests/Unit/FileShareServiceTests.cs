using SkillLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using System.Text;
using Xunit;

namespace SkillLedger.Tests.Unit
{
    /// <summary>
    /// Unit tests for FileShareService following TDD principles
    /// Tests focus on critical business logic and security aspects
    /// </summary>
    [UnitTest]
    [DocumentTest]
    public class FileShareServiceTests : IDisposable
    {
        private readonly SkillLedgerDbContext _context;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<IMessagingService> _mockMessagingService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IVirusScanService> _mockVirusScanService;
        private readonly Mock<ILogger<FileShareService>> _mockLogger;
        private readonly FileShareService _fileShareService;
        private readonly MediaUploadConfiguration _config;

        public FileShareServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SkillLedgerDbContext(options);

            // Setup mocks
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockMessagingService = new Mock<IMessagingService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockVirusScanService = new Mock<IVirusScanService>();
            _mockLogger = new Mock<ILogger<FileShareService>>();

            // Configuration
            _config = new MediaUploadConfiguration
            {
                MaxFileSizeBytes = 50 * 1024 * 1024, // 50MB
                UserQuotaBytes = 1024 * 1024 * 1024 // 1GB
            };

            var configOptions = Options.Create(_config);

            _fileShareService = new FileShareService(
                _mockLogger.Object,
                _context,
                _mockFileStorageService.Object,
                _mockMessagingService.Object,
                _mockAuditLogService.Object,
                _mockVirusScanService.Object,
                configOptions);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithValidDocument_ShouldSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, projectId);

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "test-document.pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test document content")),
                ContentType = "application/pdf",
                FileSize = 1024,
                Description = "Test document"
            };

            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult
                {
                    Success = true,
                    FilePath = "workspaces/test/documents/test-document.pdf"
                });

            _mockVirusScanService
                .Setup(x => x.ScanFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new VirusScanResult
                {
                    IsClean = true,
                    ScanCompleted = true
                });

            // Act
            var result = await _fileShareService.UploadDocumentAsync(request, userId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.DocumentId);
            Assert.NotNull(result.Document);
            Assert.Equal(request.FileName, result.Document.FileName);
            Assert.Equal(request.FileSize, result.Document.FileSize);

            // Verify document was saved to database
            var savedDocument = await _context.WorkspaceDocuments
                .FirstOrDefaultAsync(d => d.Id == result.DocumentId);
            Assert.NotNull(savedDocument);
            Assert.Equal(userId, savedDocument.UploadedBy);
            Assert.Equal(workspaceId, savedDocument.WorkspaceId);

            // Verify audit log was called
            _mockAuditLogService.Verify(x => x.LogEventAsync(
                userId, "UploadDocument", "", "", true, It.IsAny<string>(), null),
                Times.Once);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithInvalidWorkspaceAccess_ShouldFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "test-document.pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test document content")),
                ContentType = "application/pdf",
                FileSize = 1024
            };

            // Act
            var result = await _fileShareService.UploadDocumentAsync(request, unauthorizedUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Access denied to workspace", result.ErrorMessage);

            // Verify no document was saved
            var documentCount = await _context.WorkspaceDocuments.CountAsync();
            Assert.Equal(0, documentCount);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithUnsupportedFileType_ShouldFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "malicious.exe",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Malicious content")),
                ContentType = "application/x-executable",
                FileSize = 1024
            };

            // Act
            var result = await _fileShareService.UploadDocumentAsync(request, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Unsupported file type", result.ErrorMessage);

            // Verify no storage upload was attempted
            _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithFileSizeExceeded_ShouldFail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "large-document.pdf",
                FileStream = new MemoryStream(new byte[1024]),
                ContentType = "application/pdf",
                FileSize = _config.MaxFileSizeBytes + 1 // Exceed limit
            };

            // Act
            var result = await _fileShareService.UploadDocumentAsync(request, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("exceeds maximum allowed size", result.ErrorMessage);
        }

        [Fact]
        public async Task GetDocumentAsync_WithValidAccess_ShouldReturnDocument()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(documentId, workspaceId, userId);

            // Act
            var result = await _fileShareService.GetDocumentAsync(documentId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(documentId, result.Id);
            Assert.Equal("test-document.pdf", result.FileName);

            // Verify access was recorded
            var accessCount = await _context.DocumentAccesses.CountAsync();
            Assert.Equal(1, accessCount);
        }

        [Fact]
        public async Task GetDocumentAsync_WithUnauthorizedAccess_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(documentId, workspaceId, userId);

            // Act
            var result = await _fileShareService.GetDocumentAsync(documentId, unauthorizedUserId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDocumentAsync_WithDeletedDocument_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(documentId, workspaceId, userId, isDeleted: true);

            // Act
            var result = await _fileShareService.GetDocumentAsync(documentId, userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DownloadDocumentAsync_WithValidAccess_ShouldReturnStream()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(documentId, workspaceId, userId);

            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("File content"));
            _mockFileStorageService
                .Setup(x => x.DownloadFileAsync(It.IsAny<string>()))
                .ReturnsAsync(fileStream);

            // Act
            var result = await _fileShareService.DownloadDocumentAsync(documentId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileStream, result);

            // Verify download access was recorded
            var downloadAccess = await _context.DocumentAccesses
                .FirstOrDefaultAsync(a => a.AccessType == "download");
            Assert.NotNull(downloadAccess);
            Assert.Equal(userId, downloadAccess.UserId);
        }

        [Fact]
        public async Task GetWorkspaceDocumentsAsync_WithValidWorkspace_ShouldReturnDocuments()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(Guid.NewGuid(), workspaceId, userId, fileName: "document1.pdf");
            await SeedDocumentAsync(Guid.NewGuid(), workspaceId, userId, fileName: "document2.pdf");

            var request = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _fileShareService.GetWorkspaceDocumentsAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Documents.Count);
            Assert.False(result.HasNextPage);
            Assert.False(result.HasPreviousPage);
        }

        [Fact]
        public async Task GetWorkspaceDocumentsAsync_WithSearchQuery_ShouldFilterResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(Guid.NewGuid(), workspaceId, userId, fileName: "important-document.pdf");
            await SeedDocumentAsync(Guid.NewGuid(), workspaceId, userId, fileName: "regular-file.txt");

            var request = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                SearchQuery = "important",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _fileShareService.GetWorkspaceDocumentsAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Documents);
            Assert.Equal("important-document.pdf", result.Documents[0].FileName);
        }

        private async Task SeedTestDataAsync(Guid userId, Guid workspaceId, Guid projectId)
        {
            var user = new User
            {
                Id = userId,
                UserName = $"user{userId}",
                Email = $"user{userId}@test.com",
                EmailConfirmed = true
            };

            var project = new Project
            {
                Id = projectId,
                Title = "Test Project",
                Description = "Test Description",
                ClientId = userId,
                Status = ProjectStatus.InProgress
            };

            var workspace = new ProjectWorkspace
            {
                Id = workspaceId,
                ProjectId = projectId,
                ClientId = userId,
                ProviderId = Guid.NewGuid(),
                Status = WorkspaceStatus.Active
            };

            _context.Users.Add(user);
            _context.Projects.Add(project);
            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDocumentAsync(Guid documentId, Guid workspaceId, Guid userId,
            string fileName = "test-document.pdf", bool isDeleted = false)
        {
            var document = new WorkspaceDocument
            {
                Id = documentId,
                WorkspaceId = workspaceId,
                FileName = fileName,
                FilePath = $"workspaces/{workspaceId}/documents/{fileName}",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = userId,
                SecurityScanPassed = true,
                IsDeleted = isDeleted,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkspaceDocuments.Add(document);
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}