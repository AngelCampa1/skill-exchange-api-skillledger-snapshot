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

namespace SkillLedger.Tests.Security
{
    /// <summary>
    /// Security-focused tests for file sharing system
    /// Tests access control, permissions, and security validation
    /// </summary>
    [SecurityTest]
    [DocumentTest]
    public class FileShareSecurityTests : IDisposable
    {
        private readonly SkillLedgerDbContext _context;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<IMessagingService> _mockMessagingService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IVirusScanService> _mockVirusScanService;
        private readonly Mock<ILogger<FileShareService>> _mockLogger;
        private readonly FileShareService _fileShareService;
        private readonly MediaUploadConfiguration _config;

        public FileShareSecurityTests()
        {
            var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SkillLedgerDbContext(options);

            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockMessagingService = new Mock<IMessagingService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockVirusScanService = new Mock<IVirusScanService>();
            _mockLogger = new Mock<ILogger<FileShareService>>();

            _config = new MediaUploadConfiguration
            {
                MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
                UserQuotaBytes = 100 * 1024 * 1024 // 100MB
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
        public async Task UploadDocumentAsync_WithMaliciousFileName_ShouldSanitizeAndSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var maliciousFileName = "../../etc/passwd"; // Path traversal attempt
            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = maliciousFileName,
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Content")),
                ContentType = "text/plain",
                FileSize = 100
            };

            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "safe/path" });

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

            // Verify the file storage service received a sanitized filename
            _mockFileStorageService.Verify(x => x.UploadFileAsync(It.Is<FileStorageUploadRequest>(
                req => !req.FileName.Contains("../") && !req.FileName.Contains("etc/passwd"))),
                Times.Once);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithExecutableFile_ShouldReject()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var executableTypes = new[]
            {
                ("malware.exe", "application/x-msdownload"),
                ("script.bat", "application/x-bat"),
                ("shell.sh", "application/x-sh"),
                ("virus.com", "application/x-msdos-program")
            };

            foreach (var (fileName, contentType) in executableTypes)
            {
                var request = new UploadDocumentRequest
                {
                    WorkspaceId = workspaceId,
                    FileName = fileName,
                    FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Malicious content")),
                    ContentType = contentType,
                    FileSize = 100
                };

                // Act
                var result = await _fileShareService.UploadDocumentAsync(request, userId);

                // Assert
                Assert.False(result.Success, $"Should reject {fileName} with content type {contentType}");
                Assert.Contains("Unsupported file type", result.ErrorMessage);
            }
        }

        [Fact]
        public async Task UploadDocumentAsync_WithZeroBytesFile_ShouldReject()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "empty.txt",
                FileStream = new MemoryStream(),
                ContentType = "text/plain",
                FileSize = 0
            };

            // Act
            var result = await _fileShareService.UploadDocumentAsync(request, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("File is empty", result.ErrorMessage);
        }

        [Fact]
        public async Task GetDocumentAsync_WithUnauthorizedUser_ShouldReturnNull()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            // Setup workspace with client and provider
            await SeedWorkspaceWithTwoUsersAsync(clientId, providerId, workspaceId);
            await SeedDocumentAsync(documentId, workspaceId, clientId);

            // Act - Unauthorized user tries to access
            var result = await _fileShareService.GetDocumentAsync(documentId, unauthorizedUserId);

            // Assert
            Assert.Null(result);

            // Verify no access was recorded
            var accessCount = await _context.DocumentAccesses.CountAsync();
            Assert.Equal(0, accessCount);
        }

        [Fact]
        public async Task GetDocumentAsync_WithSoftDeletedDocument_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var document = new WorkspaceDocument
            {
                Id = documentId,
                WorkspaceId = workspaceId,
                FileName = "deleted-file.pdf",
                FilePath = "path/to/deleted-file.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = userId,
                SecurityScanPassed = true,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkspaceDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Act
            var result = await _fileShareService.GetDocumentAsync(documentId, userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DownloadDocumentAsync_WithUnauthorizedUser_ShouldReturnNull()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedTestDataAsync(clientId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(documentId, workspaceId, clientId);

            // Act - Unauthorized user tries to download
            var result = await _fileShareService.DownloadDocumentAsync(documentId, unauthorizedUserId);

            // Assert
            Assert.Null(result);

            // Verify file storage service was not called
            _mockFileStorageService.Verify(x => x.DownloadFileAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ShareDocumentAsync_WithUnauthorizedUser_ShouldNotCreateShare()
        {
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var attackerId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedWorkspaceWithTwoUsersAsync(clientId, providerId, workspaceId);
            await SeedUserAsync(attackerId);
            await SeedUserAsync(targetUserId);
            await SeedDocumentAsync(documentId, workspaceId, clientId);

            var request = new ShareDocumentRequest
            {
                DocumentId = documentId,
                UserId = targetUserId,
                Permission = SharePermission.Download
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _fileShareService.ShareDocumentAsync(request, attackerId));

            Assert.Equal(0, await _context.DocumentShares.CountAsync());
        }

        [Fact]
        public async Task ShareDocumentAsync_WithAuthorizedUser_ShouldPersistShareAndAllowTargetDownload()
        {
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedWorkspaceWithTwoUsersAsync(clientId, providerId, workspaceId);
            await SeedUserAsync(targetUserId);
            await SeedDocumentAsync(documentId, workspaceId, clientId);

            _mockFileStorageService
                .Setup(x => x.DownloadFileAsync(It.IsAny<string>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("shared content")));

            var share = await _fileShareService.ShareDocumentAsync(new ShareDocumentRequest
            {
                DocumentId = documentId,
                UserId = targetUserId,
                Permission = SharePermission.Download
            }, clientId);

            var canDownload = await _fileShareService.ValidateDocumentAccessAsync(documentId, targetUserId, SharePermission.Download);
            using var downloaded = await _fileShareService.DownloadDocumentAsync(documentId, targetUserId);

            Assert.True(share.IsActive);
            Assert.Equal(1, await _context.DocumentShares.CountAsync());
            Assert.True(canDownload);
            Assert.NotNull(downloaded);
        }

        [Fact]
        public async Task ValidateDocumentAccessAsync_WithViewOnlyShare_ShouldDenyDownloadPermission()
        {
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedWorkspaceWithTwoUsersAsync(clientId, providerId, workspaceId);
            await SeedUserAsync(targetUserId);
            await SeedDocumentAsync(documentId, workspaceId, clientId);

            await _fileShareService.ShareDocumentAsync(new ShareDocumentRequest
            {
                DocumentId = documentId,
                UserId = targetUserId,
                Permission = SharePermission.View
            }, clientId);

            Assert.True(await _fileShareService.ValidateDocumentAccessAsync(documentId, targetUserId, SharePermission.View));
            Assert.False(await _fileShareService.ValidateDocumentAccessAsync(documentId, targetUserId, SharePermission.Download));
        }

        [Fact]
        public async Task GetWorkspaceDocumentsAsync_WithUnauthorizedWorkspace_ShouldReturnEmpty()
        {
            // Arrange
            var authorizedUserId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(authorizedUserId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(Guid.NewGuid(), workspaceId, authorizedUserId);

            var request = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                PageNumber = 1,
                PageSize = 10
            };

            // Act - Unauthorized user tries to access workspace documents
            var result = await _fileShareService.GetWorkspaceDocumentsAsync(request, unauthorizedUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Documents);
        }

        [Fact]
        public async Task GetWorkspaceStorageStatsAsync_WithUnauthorizedUser_ShouldNotLeakCounts()
        {
            var authorizedUserId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(authorizedUserId, workspaceId, Guid.NewGuid());
            await SeedDocumentAsync(Guid.NewGuid(), workspaceId, authorizedUserId);

            var result = await _fileShareService.GetWorkspaceStorageStatsAsync(workspaceId, unauthorizedUserId);

            Assert.Equal(workspaceId, result.WorkspaceId);
            Assert.Equal(0, result.DocumentCount);
            Assert.Equal(0, result.TotalSizeBytes);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithInvalidFolder_ShouldReject()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var otherWorkspaceId = Guid.NewGuid();
            var folderId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            // Create folder in different workspace
            var folder = new DocumentFolder
            {
                Id = folderId,
                WorkspaceId = otherWorkspaceId,
                FolderName = "Other Workspace Folder",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentFolders.Add(folder);
            await _context.SaveChangesAsync();

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FolderId = folderId, // Folder belongs to different workspace
                FileName = "test.txt",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Content")),
                ContentType = "text/plain",
                FileSize = 100
            };

            // Act
            var result = await _fileShareService.UploadDocumentAsync(request, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Folder not found or access denied", result.ErrorMessage);
        }

        [Fact]
        public async Task WorkspaceDocument_CanBeAccessedBy_ShouldRespectPermissions()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedWorkspaceWithTwoUsersAsync(clientId, providerId, workspaceId);

            var document = new WorkspaceDocument
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                FileName = "test.pdf",
                FilePath = "path/to/test.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = clientId,
                SecurityScanPassed = true,
                CreatedAt = DateTime.UtcNow
            };

            var workspace = new ProjectWorkspace
            {
                Id = workspaceId,
                ProjectId = Guid.NewGuid(),
                ClientId = clientId,
                ProviderId = providerId,
                Status = WorkspaceStatus.Active
            };

            document.Workspace = workspace;

            // Act & Assert
            Assert.True(document.CanBeAccessedBy(clientId));
            Assert.True(document.CanBeAccessedBy(providerId));
            Assert.False(document.CanBeAccessedBy(unauthorizedUserId));
        }

        [Fact]
        public async Task WorkspaceDocument_CanBeDeletedBy_ShouldRestrictToUploader()
        {
            // Arrange
            var uploaderId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            var document = new WorkspaceDocument
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                FileName = "test.pdf",
                FilePath = "path/to/test.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = uploaderId,
                SecurityScanPassed = true,
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            Assert.True(document.CanBeDeletedBy(uploaderId));
            Assert.False(document.CanBeDeletedBy(otherUserId));
        }

        [Fact]
        public async Task DocumentFolder_ShouldPreventUnauthorizedAccess()
        {
            // Arrange
            var authorizedUserId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(authorizedUserId, workspaceId, Guid.NewGuid());

            var folder = new DocumentFolder
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                FolderName = "Confidential Folder",
                CreatedBy = authorizedUserId,
                CreatedAt = DateTime.UtcNow
            };

            var workspace = new ProjectWorkspace
            {
                Id = workspaceId,
                ProjectId = Guid.NewGuid(),
                ClientId = authorizedUserId,
                ProviderId = Guid.NewGuid(),
                Status = WorkspaceStatus.Active
            };

            folder.Workspace = workspace;

            // Act & Assert
            Assert.True(folder.CanBeAccessedBy(authorizedUserId));
            Assert.False(folder.CanBeAccessedBy(unauthorizedUserId));
        }

        [Fact]
        public async Task UploadDocumentAsync_ShouldLogSecurityEvents()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedTestDataAsync(userId, workspaceId, Guid.NewGuid());

            var request = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "important-document.pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Sensitive content")),
                ContentType = "application/pdf",
                FileSize = 1024
            };

            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "path/to/file" });

            _mockVirusScanService
                .Setup(x => x.ScanFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new VirusScanResult
                {
                    IsClean = true,
                    ScanCompleted = true
                });

            // Act
            await _fileShareService.UploadDocumentAsync(request, userId);

            // Assert - Verify audit logging occurred
            _mockAuditLogService.Verify(x => x.LogEventAsync(
                userId,
                "UploadDocument",
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.Is<string>(msg => msg.Contains("important-document.pdf")),
                null),
                Times.Once);
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
                Title = "Security Test Project",
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

        private async Task SeedWorkspaceWithTwoUsersAsync(Guid clientId, Guid providerId, Guid workspaceId)
        {
            var client = new User
            {
                Id = clientId,
                UserName = $"client{clientId}",
                Email = $"client{clientId}@test.com",
                EmailConfirmed = true
            };

            var provider = new User
            {
                Id = providerId,
                UserName = $"provider{providerId}",
                Email = $"provider{providerId}@test.com",
                EmailConfirmed = true
            };

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Title = "Two User Test Project",
                Description = "Test Description",
                ClientId = clientId,
                Status = ProjectStatus.InProgress
            };

            var workspace = new ProjectWorkspace
            {
                Id = workspaceId,
                ProjectId = project.Id,
                ClientId = clientId,
                ProviderId = providerId,
                Status = WorkspaceStatus.Active
            };

            _context.Users.AddRange(client, provider);
            _context.Projects.Add(project);
            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDocumentAsync(Guid documentId, Guid workspaceId, Guid userId)
        {
            var document = new WorkspaceDocument
            {
                Id = documentId,
                WorkspaceId = workspaceId,
                FileName = "security-test.pdf",
                FilePath = $"workspaces/{workspaceId}/documents/security-test.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = userId,
                SecurityScanPassed = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkspaceDocuments.Add(document);
            await _context.SaveChangesAsync();
        }

        private async Task SeedUserAsync(Guid userId)
        {
            if (await _context.Users.AnyAsync(u => u.Id == userId))
                return;

            _context.Users.Add(new User
            {
                Id = userId,
                UserName = $"user{userId}",
                Email = $"user{userId}@test.com",
                EmailConfirmed = true
            });
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
