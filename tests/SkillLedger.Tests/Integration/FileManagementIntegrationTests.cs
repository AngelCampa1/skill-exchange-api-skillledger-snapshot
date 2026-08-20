using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
using SkillLedger.Tests.Infrastructure;
using System.Text;
using Xunit;

namespace SkillLedger.Tests.Integration
{
    /// <summary>
    /// Integration tests for file management workflows following TDD principles
    /// Tests the complete file upload/download flow with real database and mocked external services
    /// </summary>
    [IntegrationTest]
    [FileManagementTest]
    [Collection("Integration Other")]
public class FileManagementIntegrationTests : IntegrationTestBase
    {
        private readonly IFileShareService _fileShareService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IMessagingService> _mockMessagingService;
        private readonly Mock<IVirusScanService> _mockVirusScanService;

        public FileManagementIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
        {
            // Setup mocked external services
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockMessagingService = new Mock<IMessagingService>();
            _mockVirusScanService = new Mock<IVirusScanService>();

            // Configure media upload settings
            var mediaConfig = Options.Create(new MediaUploadConfiguration
            {
                MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
                UserQuotaBytes = 100 * 1024 * 1024, // 100MB
                AllowedFileTypes = new[] { "pdf", "docx", "txt", "jpg", "png" }
            });

            // Create FileShareService with mocked dependencies
            _fileShareService = new FileShareService(
                Factory.Services.GetRequiredService<ILogger<FileShareService>>(),
                Context,
                _mockFileStorageService.Object,
                _mockMessagingService.Object,
                _mockAuditLogService.Object,
                _mockVirusScanService.Object,
                mediaConfig
            );
        }

        [Fact]
        [SlowTest]
        public async Task FullDocumentLifecycle_ShouldWorkEndToEnd()
        {
            // Arrange - Create test users and workspace
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedWorkspaceDataAsync(clientId, providerId, projectId, workspaceId);

            var fileContent = Encoding.UTF8.GetBytes("Test document content for integration testing");
            var uploadRequest = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "integration-test-doc.pdf",
                FileStream = new MemoryStream(fileContent),
                ContentType = "application/pdf",
                FileSize = fileContent.Length,
                Description = "Integration test document"
            };

            // Mock successful file storage
            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult
                {
                    Success = true,
                    FilePath = $"workspaces/{workspaceId}/documents/integration-test-doc.pdf"
                });

            // Act - Upload document
            var uploadResult = await _fileShareService.UploadDocumentAsync(uploadRequest, clientId);

            // Assert - Upload success
            Assert.True(uploadResult.Success);
            Assert.NotNull(uploadResult.DocumentId);
            Assert.NotNull(uploadResult.Document);

            // Verify document in database
            var savedDocument = await Context.WorkspaceDocuments
                .FirstOrDefaultAsync(d => d.Id == uploadResult.DocumentId);
            Assert.NotNull(savedDocument);
            Assert.Equal(uploadRequest.FileName, savedDocument.FileName);
            Assert.Equal(uploadRequest.FileSize, savedDocument.FileSize);
            Assert.Equal(clientId, savedDocument.UploadedBy);
            Assert.Equal(workspaceId, savedDocument.WorkspaceId);

            // Act - Retrieve document by client
            var retrievedDoc = await _fileShareService.GetDocumentAsync(uploadResult.DocumentId.Value, clientId);

            // Assert - Document retrieval success
            Assert.NotNull(retrievedDoc);
            Assert.Equal(uploadRequest.FileName, retrievedDoc.FileName);
            Assert.True(retrievedDoc.CanEdit);
            Assert.True(retrievedDoc.CanDelete);

            // Act - Provider should also be able to access
            var providerDoc = await _fileShareService.GetDocumentAsync(uploadResult.DocumentId.Value, providerId);

            // Assert - Provider access
            Assert.NotNull(providerDoc);
            Assert.Equal(uploadRequest.FileName, providerDoc.FileName);
            Assert.False(providerDoc.CanDelete); // Only uploader can delete

            // Verify access tracking
            var accessCount = await Context.DocumentAccesses
                .CountAsync(a => a.DocumentId == uploadResult.DocumentId);
            Assert.Equal(2, accessCount); // Client and provider access

            // Setup download mock
            var downloadStream = new MemoryStream(fileContent);
            _mockFileStorageService
                .Setup(x => x.DownloadFileAsync(It.IsAny<string>()))
                .ReturnsAsync(downloadStream);

            // Act - Download document
            var downloadedStream = await _fileShareService.DownloadDocumentAsync(uploadResult.DocumentId.Value, clientId);

            // Assert - Download success
            Assert.NotNull(downloadedStream);
            Assert.Equal(downloadStream, downloadedStream);

            // Verify download tracking
            var downloadAccess = await Context.DocumentAccesses
                .FirstOrDefaultAsync(a => a.DocumentId == uploadResult.DocumentId && a.AccessType == "download");
            Assert.NotNull(downloadAccess);
            Assert.Equal(clientId, downloadAccess.UserId);

            // Act - Get workspace documents
            var documentsRequest = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                PageNumber = 1,
                PageSize = 10
            };
            var documentsResult = await _fileShareService.GetWorkspaceDocumentsAsync(documentsRequest, clientId);

            // Assert - Workspace document listing
            Assert.NotNull(documentsResult);
            Assert.Equal(1, documentsResult.TotalCount);
            Assert.Single(documentsResult.Documents);
            Assert.Equal(uploadRequest.FileName, documentsResult.Documents[0].FileName);

            // Verify audit logging occurred
            _mockAuditLogService.Verify(x => x.LogEventAsync(
                clientId,
                "UploadDocument",
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.Is<string>(msg => msg.Contains(uploadRequest.FileName)),
                null),
                Times.Once);
        }

        [Fact]
        [SecurityTest]
        public async Task DocumentUpload_WithUnauthorizedUser_ShouldFail()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedWorkspaceDataAsync(clientId, providerId, projectId, workspaceId);

            var uploadRequest = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FileName = "unauthorized-test.pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test content")),
                ContentType = "application/pdf",
                FileSize = 100
            };

            // Act
            var result = await _fileShareService.UploadDocumentAsync(uploadRequest, unauthorizedUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Access denied to workspace", result.ErrorMessage);

            // Verify no document was saved
            var documentCount = await Context.WorkspaceDocuments
                .CountAsync(d => d.WorkspaceId == workspaceId);
            Assert.Equal(0, documentCount);

            // Verify file storage was not called
            _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()),
                Times.Never);
        }

        [Fact]
        [SecurityTest]
        public async Task DocumentAccess_WithUnauthorizedUser_ShouldReturnNull()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var unauthorizedUserId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var documentId = Guid.NewGuid();

            await SeedWorkspaceDataAsync(clientId, providerId, projectId, workspaceId);
            await SeedDocumentAsync(documentId, workspaceId, clientId);

            // Act
            var result = await _fileShareService.GetDocumentAsync(documentId, unauthorizedUserId);

            // Assert
            Assert.Null(result);

            // Verify no access was recorded
            var accessCount = await Context.DocumentAccesses
                .CountAsync(a => a.DocumentId == documentId);
            Assert.Equal(0, accessCount);
        }

        [Fact]
        [FastTest]
        public async Task DocumentUpload_WithFolderStructure_ShouldOrganizeCorrectly()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var parentFolderId = Guid.NewGuid();
            var childFolderId = Guid.NewGuid();

            await SeedWorkspaceDataAsync(clientId, Guid.NewGuid(), projectId, workspaceId);

            // Create folder structure
            var parentFolder = new DocumentFolder
            {
                Id = parentFolderId,
                WorkspaceId = workspaceId,
                FolderName = "Parent Folder",
                CreatedBy = clientId,
                CreatedAt = DateTime.UtcNow
            };

            var childFolder = new DocumentFolder
            {
                Id = childFolderId,
                WorkspaceId = workspaceId,
                FolderName = "Child Folder",
                ParentFolderId = parentFolderId,
                CreatedBy = clientId,
                CreatedAt = DateTime.UtcNow
            };

            Context.DocumentFolders.AddRange(parentFolder, childFolder);
            await Context.SaveChangesAsync();

            var uploadRequest = new UploadDocumentRequest
            {
                WorkspaceId = workspaceId,
                FolderId = childFolderId,
                FileName = "folder-test.pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Folder test content")),
                ContentType = "application/pdf",
                FileSize = 100
            };

            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "test/path" });

            // Act
            var result = await _fileShareService.UploadDocumentAsync(uploadRequest, clientId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Document);
            Assert.Equal(childFolderId, result.Document.FolderId);

            // Verify document is in correct folder
            var savedDocument = await Context.WorkspaceDocuments
                .Include(d => d.Folder)
                .ThenInclude(f => f!.ParentFolder)
                .FirstOrDefaultAsync(d => d.Id == result.DocumentId);

            Assert.NotNull(savedDocument);
            Assert.Equal(childFolderId, savedDocument.FolderId);
            Assert.Equal("Child Folder", savedDocument.Folder!.FolderName);
            Assert.Equal("Parent Folder", savedDocument.Folder.ParentFolder!.FolderName);
        }

        [Fact]
        [SlowTest]
        public async Task MultipleDocumentUploads_ShouldMaintainPaginationCorrectly()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedWorkspaceDataAsync(clientId, Guid.NewGuid(), projectId, workspaceId);

            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "test/path" });

            // Upload 5 documents
            for (int i = 1; i <= 5; i++)
            {
                var uploadRequest = new UploadDocumentRequest
                {
                    WorkspaceId = workspaceId,
                    FileName = $"document-{i:D2}.pdf",
                    FileStream = new MemoryStream(Encoding.UTF8.GetBytes($"Content {i}")),
                    ContentType = "application/pdf",
                    FileSize = 100 + i
                };

                var result = await _fileShareService.UploadDocumentAsync(uploadRequest, clientId);
                Assert.True(result.Success);

                // Small delay to ensure different creation times
                await Task.Delay(10);
            }

            // Ensure all database operations are complete before querying
            await Context.SaveChangesAsync();

            // Act - Get first page (3 items)
            var page1Request = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                PageNumber = 1,
                PageSize = 3
            };
            var page1Result = await _fileShareService.GetWorkspaceDocumentsAsync(page1Request, clientId);

            // Assert - Page 1
            Assert.NotNull(page1Result);
            Assert.Equal(5, page1Result.TotalCount);
            Assert.Equal(3, page1Result.Documents.Count);
            Assert.True(page1Result.HasNextPage);
            Assert.False(page1Result.HasPreviousPage);

            // Act - Get second page (2 remaining items)
            var page2Request = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                PageNumber = 2,
                PageSize = 3
            };
            var page2Result = await _fileShareService.GetWorkspaceDocumentsAsync(page2Request, clientId);

            // Assert - Page 2
            Assert.NotNull(page2Result);
            Assert.Equal(5, page2Result.TotalCount);
            Assert.Equal(2, page2Result.Documents.Count);
            Assert.False(page2Result.HasNextPage);
            Assert.True(page2Result.HasPreviousPage);

            // Verify all document names are present across both pages
            var allDocuments = page1Result.Documents.Concat(page2Result.Documents).ToList();
            var expectedNames = new[] { "document-01.pdf", "document-02.pdf", "document-03.pdf", "document-04.pdf", "document-05.pdf" };
            var actualNames = allDocuments.Select(d => d.FileName).OrderBy(n => n).ToArray();
            Assert.Equal(expectedNames, actualNames);
        }

        [Fact]
        [FastTest]
        public async Task DocumentSearch_ShouldFilterCorrectly()
        {
            // Arrange
            var clientId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            await SeedWorkspaceDataAsync(clientId, Guid.NewGuid(), projectId, workspaceId);

            _mockFileStorageService
                .Setup(x => x.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
                .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "test/path" });

            // Upload documents with different names and appropriate content types
            var documentData = new[]
            {
                new { FileName = "important-contract.pdf", ContentType = "application/pdf" },
                new { FileName = "meeting-notes.txt", ContentType = "text/plain" },
                new { FileName = "project-specs.docx", ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                new { FileName = "important-design.jpg", ContentType = "image/jpeg" },
                new { FileName = "regular-file.pdf", ContentType = "application/pdf" }
            };

            foreach (var docData in documentData)
            {
                var uploadRequest = new UploadDocumentRequest
                {
                    WorkspaceId = workspaceId,
                    FileName = docData.FileName,
                    FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Content")),
                    ContentType = docData.ContentType,
                    FileSize = 100
                };

                var result = await _fileShareService.UploadDocumentAsync(uploadRequest, clientId);
                Assert.True(result.Success);
            }

            // Act - Search for "important" documents
            var searchRequest = new WorkspaceDocumentsRequest
            {
                WorkspaceId = workspaceId,
                SearchQuery = "important",
                PageNumber = 1,
                PageSize = 10
            };
            var searchResult = await _fileShareService.GetWorkspaceDocumentsAsync(searchRequest, clientId);

            // Assert
            Assert.NotNull(searchResult);
            Assert.Equal(2, searchResult.TotalCount);
            Assert.Equal(2, searchResult.Documents.Count);

            var foundNames = searchResult.Documents.Select(d => d.FileName).OrderBy(n => n).ToArray();
            Assert.Equal(new[] { "important-contract.pdf", "important-design.jpg" }, foundNames);
        }

        private async Task SeedWorkspaceDataAsync(Guid clientId, Guid providerId, Guid projectId, Guid workspaceId)
        {
            // Use a new context to avoid tracking conflicts
            using var seedScope = Factory.Services.CreateScope();
            var seedContext = seedScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

            // Clear tracker to avoid conflicts
            seedContext.ChangeTracker.Clear();

            // Check if users already exist to avoid conflicts
            var existingClient = await seedContext.Users.FindAsync(clientId);
            if (existingClient == null)
            {
                var client = new User
                {
                    Id = clientId,
                    UserName = $"client{clientId}",
                    Email = $"client{clientId}@test.com",
                    EmailConfirmed = true
                };
                seedContext.Users.Add(client);
            }

            var existingProvider = await seedContext.Users.FindAsync(providerId);
            if (existingProvider == null)
            {
                var provider = new User
                {
                    Id = providerId,
                    UserName = $"provider{providerId}",
                    Email = $"provider{providerId}@test.com",
                    EmailConfirmed = true
                };
                seedContext.Users.Add(provider);
            }

            await seedContext.SaveChangesAsync(); // Save users first to avoid FK conflicts

            var project = new Project
            {
                Id = projectId,
                Title = "Integration Test Project",
                Description = "Project for testing file management",
                ClientId = clientId,
                Status = ProjectStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            };

            var workspace = new ProjectWorkspace
            {
                Id = workspaceId,
                ProjectId = projectId,
                ClientId = clientId,
                ProviderId = providerId,
                Status = WorkspaceStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            seedContext.Projects.Add(project);
            seedContext.ProjectWorkspaces.Add(workspace);
            await seedContext.SaveChangesAsync();
        }

        private async Task SeedDocumentAsync(Guid documentId, Guid workspaceId, Guid uploadedBy)
        {
            // Use a new context to avoid tracking conflicts
            using var seedScope = Factory.Services.CreateScope();
            var seedContext = seedScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

            // Clear tracker to avoid conflicts
            seedContext.ChangeTracker.Clear();

            // Check if document already exists to avoid conflicts
            var existingDocument = await seedContext.WorkspaceDocuments.FindAsync(documentId);
            if (existingDocument == null)
            {
                var document = new WorkspaceDocument
                {
                    Id = documentId,
                    WorkspaceId = workspaceId,
                    FileName = "seeded-document.pdf",
                    FilePath = $"workspaces/{workspaceId}/documents/seeded-document.pdf",
                    FileSize = 1024,
                    MimeType = "application/pdf",
                    UploadedBy = uploadedBy,
                    SecurityScanPassed = true,
                    CreatedAt = DateTime.UtcNow
                };

                seedContext.WorkspaceDocuments.Add(document);
                await seedContext.SaveChangesAsync();
            }
        }
    }
}