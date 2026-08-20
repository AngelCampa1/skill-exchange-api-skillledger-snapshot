using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for FileShareService - WORKSPACE FILE SHARING.
///
/// Pattern (per TDD_GUIDE.md):
/// - Uses MockFileStorageService (external - OK to mock)
/// - Uses MockVirusScanService (external - OK to mock)
/// - Real AuditLogService with real database
/// - Tests file upload, download, sharing, and access control
///
/// Max mocked external dependencies: 2 (FileStorage, VirusScan)
/// </summary>
[IntegrationTest]
public class FileShareServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly FileShareService _service;
    private readonly MockFileStorageService _fileStorageService;
    private readonly MockVirusScanService _virusScanService;
    private readonly AuditLogService _auditLogService;

    // Test entity IDs
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _providerId = Guid.NewGuid();
    private readonly Guid _unauthorizedUserId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();
    private readonly Guid _folderId = Guid.NewGuid();

    public FileShareServiceIntegrationTests()
    {
        // Create InMemory database
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new SkillLedgerDbContext(options);

        // External services (OK to mock)
        _fileStorageService = new MockFileStorageService();
        _virusScanService = new MockVirusScanService();
        _virusScanService.SetupCleanScan(); // Default to clean

        // Internal services (real implementations)
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var auditLogger = new LoggerFactory().CreateLogger<AuditLogService>();
        _auditLogService = new AuditLogService(_context, auditLogger, memoryCache);

        // Mock messaging service (external service - OK to mock)
        var messagingService = new Mock<IMessagingService>().Object;

        // Configuration
        var config = Options.Create(new MediaUploadConfiguration
        {
            MaxFileSizeBytes = 10_000_000, // 10MB
            AllowedFileTypes = new[] { "pdf", "jpeg", "jpg", "png", "txt" }
        });

        var logger = new LoggerFactory().CreateLogger<FileShareService>();

        _service = new FileShareService(
            logger,
            _context,
            _fileStorageService,
            messagingService,
            _auditLogService,
            _virusScanService,
            config
        );

        SeedTestData().GetAwaiter().GetResult();
    }

    private async Task SeedTestData()
    {
        // Create test users
        var client = new User
        {
            Id = _clientId,
            Email = "client@test.com",
            UserName = "client@test.com",
            FirstName = "Test",
            LastName = "Client",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            Profile = new Profile
            {
                UserId = _clientId,
                FirstName = "Test",
                LastName = "Client"
            }
        };

        var provider = new User
        {
            Id = _providerId,
            Email = "provider@test.com",
            UserName = "provider@test.com",
            FirstName = "Test",
            LastName = "Provider",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            Profile = new Profile
            {
                UserId = _providerId,
                FirstName = "Test",
                LastName = "Provider"
            }
        };

        var unauthorized = new User
        {
            Id = _unauthorizedUserId,
            Email = "unauthorized@test.com",
            UserName = "unauthorized@test.com",
            FirstName = "Unauthorized",
            LastName = "User",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            Profile = new Profile
            {
                UserId = _unauthorizedUserId,
                FirstName = "Unauthorized",
                LastName = "User"
            }
        };

        _context.Users.AddRange(client, provider, unauthorized);

        // Create test project
        var project = new Project
        {
            Id = _projectId,
            ClientId = _clientId,
            Title = "Test Project",
            Description = "Test project for file sharing tests",
            Status = ProjectStatus.Published,
            CreditBudget = 1000,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);

        // Create workspace
        var workspace = new ProjectWorkspace
        {
            Id = _workspaceId,
            ProjectId = _projectId,
            ClientId = _clientId,
            ProviderId = _providerId,
            Status = WorkspaceStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectWorkspaces.Add(workspace);

        // Create test folder
        var folder = new DocumentFolder
        {
            Id = _folderId,
            WorkspaceId = _workspaceId,
            FolderName = "Test Folder",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _clientId
        };
        _context.DocumentFolders.Add(folder);

        // Create test document
        var document = new WorkspaceDocument
        {
            Id = _documentId,
            WorkspaceId = _workspaceId,
            FileName = "test-document.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/{_documentId}_test-document.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            SecurityScanPassed = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkspaceDocuments.Add(document);

        await _context.SaveChangesAsync();
    }

    #region UploadDocumentAsync Tests

    [Fact]
    public async Task UploadDocumentAsync_ValidFile_ReturnsSuccessAndCreatesDocument()
    {
        // Arrange
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "new-document.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = stream
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeTrue();
        result.DocumentId.Should().NotBeEmpty();
        result.Document.Should().NotBeNull();
        result.Document!.FileName.Should().Be("new-document.pdf");
        result.Document.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task UploadDocumentAsync_NoWorkspaceAccess_ReturnsFailed()
    {
        // Arrange
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = stream
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _unauthorizedUserId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Access denied");
    }

    [Fact]
    public async Task UploadDocumentAsync_UnsupportedFileType_ReturnsFailed()
    {
        // Arrange
        var fileContent = new byte[] { 0x4D, 0x5A }; // EXE header
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "malicious.exe",
            ContentType = "application/x-msdownload",
            FileSize = fileContent.Length,
            FileStream = stream
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("Unsupported file type"));
    }

    [Fact]
    public async Task UploadDocumentAsync_FileTooLarge_ReturnsFailed()
    {
        // Arrange
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "large-file.pdf",
            ContentType = "application/pdf",
            FileSize = 20_000_000, // 20MB, exceeds 10MB limit
            FileStream = new MemoryStream()
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("exceeds maximum"));
    }

    [Fact]
    public async Task UploadDocumentAsync_EmptyFile_ReturnsFailed()
    {
        // Arrange
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "empty.pdf",
            ContentType = "application/pdf",
            FileSize = 0,
            FileStream = new MemoryStream()
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("empty"));
    }

    [Fact]
    public async Task UploadDocumentAsync_VirusDetected_ReturnsFailed()
    {
        // Arrange
        _virusScanService.SetupInfectedScan("Trojan.Test", ThreatSeverity.High);

        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "infected.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = stream
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("security scan failed");
    }

    [Fact]
    public async Task UploadDocumentAsync_MaliciousFileName_SanitizesAndSucceeds()
    {
        // Arrange
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "../../../etc/passwd.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = stream
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeTrue();
        result.Document!.FileName.Should().NotContain("..");
        result.Document.FileName.Should().NotContain("/");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithFolder_AssociatesDocumentToFolder()
    {
        // Arrange
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "folder-doc.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = stream,
            FolderId = _folderId
        };

        // Act
        var result = await _service.UploadDocumentAsync(request, _clientId);

        // Assert
        result.Success.Should().BeTrue();
        result.Document!.FolderId.Should().Be(_folderId);
    }

    [Fact]
    public async Task UploadDocumentAsync_CreatesAuditLog()
    {
        // Arrange
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        using var stream = new MemoryStream(fileContent);
        var request = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "audit-test.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = stream
        };

        // Act
        await _service.UploadDocumentAsync(request, _clientId);
        await Task.Delay(200); // Allow for fire-and-forget audit logging

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().Contain(log => log.Action == "UploadDocument" && log.UserId == _clientId);
    }

    #endregion

    #region GetDocumentAsync Tests

    [Fact]
    public async Task GetDocumentAsync_ClientAccess_ReturnsDocument()
    {
        // Act
        var result = await _service.GetDocumentAsync(_documentId, _clientId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_documentId);
        result.FileName.Should().Be("test-document.pdf");
    }

    [Fact]
    public async Task GetDocumentAsync_ProviderAccess_ReturnsDocument()
    {
        // Act
        var result = await _service.GetDocumentAsync(_documentId, _providerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(_documentId);
    }

    [Fact]
    public async Task GetDocumentAsync_UnauthorizedUser_ReturnsNull()
    {
        // Act
        var result = await _service.GetDocumentAsync(_documentId, _unauthorizedUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetDocumentAsync(Guid.NewGuid(), _clientId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentAsync_DeletedDocument_ReturnsNull()
    {
        // Arrange
        var deletedDocId = Guid.NewGuid();
        var deletedDoc = new WorkspaceDocument
        {
            Id = deletedDocId,
            WorkspaceId = _workspaceId,
            FileName = "deleted.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/{deletedDocId}_deleted.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkspaceDocuments.Add(deletedDoc);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDocumentAsync(deletedDocId, _clientId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DownloadDocumentAsync Tests

    [Fact]
    public async Task DownloadDocumentAsync_ValidAccess_ReturnsStream()
    {
        // Arrange - Upload a file first
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // PDF header
        using var uploadStream = new MemoryStream(fileContent);
        var uploadRequest = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "download-test.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = uploadStream
        };
        var uploadResult = await _service.UploadDocumentAsync(uploadRequest, _clientId);

        // Act
        var result = await _service.DownloadDocumentAsync(uploadResult.DocumentId!.Value, _clientId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DownloadDocumentAsync_UnauthorizedUser_ReturnsNull()
    {
        // Act
        var result = await _service.DownloadDocumentAsync(_documentId, _unauthorizedUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadDocumentAsync_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.DownloadDocumentAsync(Guid.NewGuid(), _clientId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadDocumentAsync_RecordsAccessLog()
    {
        // Arrange - Upload file first
        var fileContent = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        using var uploadStream = new MemoryStream(fileContent);
        var uploadRequest = new UploadDocumentRequest
        {
            WorkspaceId = _workspaceId,
            FileName = "access-log-test.pdf",
            ContentType = "application/pdf",
            FileSize = fileContent.Length,
            FileStream = uploadStream
        };
        var uploadResult = await _service.UploadDocumentAsync(uploadRequest, _clientId);

        // Act
        await _service.DownloadDocumentAsync(uploadResult.DocumentId!.Value, _clientId);
        await Task.Delay(200);

        // Assert
        var accessLogs = await _context.DocumentAccesses.ToListAsync();
        accessLogs.Should().Contain(a => a.DocumentId == uploadResult.DocumentId && a.AccessType == "download");
    }

    #endregion

    #region GetWorkspaceDocumentsAsync Tests

    [Fact]
    public async Task GetWorkspaceDocumentsAsync_ValidAccess_ReturnsDocuments()
    {
        // Arrange
        var request = new WorkspaceDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetWorkspaceDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetWorkspaceDocumentsAsync_UnauthorizedUser_ReturnsEmpty()
    {
        // Arrange
        var request = new WorkspaceDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetWorkspaceDocumentsAsync(request, _unauthorizedUserId);

        // Assert
        result.Documents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkspaceDocumentsAsync_WithSearchQuery_FiltersResults()
    {
        // Arrange
        var request = new WorkspaceDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            SearchQuery = "test-document",
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetWorkspaceDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().Contain(d => d.FileName.Contains("test-document"));
    }

    [Fact]
    public async Task GetWorkspaceDocumentsAsync_WithFileTypeFilter_FiltersResults()
    {
        // Arrange
        var request = new WorkspaceDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            FileTypes = new List<string> { "application/pdf" },
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetWorkspaceDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().OnlyContain(d => d.MimeType == "application/pdf");
    }

    [Fact]
    public async Task GetWorkspaceDocumentsAsync_Pagination_RespectsPageSize()
    {
        // Arrange - Add more documents
        for (int i = 0; i < 5; i++)
        {
            var doc = new WorkspaceDocument
            {
                Id = Guid.NewGuid(),
                WorkspaceId = _workspaceId,
                FileName = $"pagination-test-{i}.pdf",
                FilePath = $"workspaces/{_workspaceId}/documents/pagination-{i}.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = _clientId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.WorkspaceDocuments.Add(doc);
        }
        await _context.SaveChangesAsync();

        var request = new WorkspaceDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            PageNumber = 1,
            PageSize = 3
        };

        // Act
        var result = await _service.GetWorkspaceDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().HaveCount(3);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkspaceDocumentsAsync_ExcludesDeletedByDefault()
    {
        // Arrange
        var deletedDoc = new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            FileName = "deleted-exclude-test.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/deleted.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkspaceDocuments.Add(deletedDoc);
        await _context.SaveChangesAsync();

        var request = new WorkspaceDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            PageNumber = 1,
            PageSize = 100,
            IncludeDeleted = false
        };

        // Act
        var result = await _service.GetWorkspaceDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().NotContain(d => d.FileName == "deleted-exclude-test.pdf");
    }

    #endregion

    #region DeleteDocumentAsync Tests

    [Fact]
    public async Task DeleteDocumentAsync_ValidOwner_SoftDeletesDocument()
    {
        // Arrange
        var docToDelete = new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            FileName = "to-delete.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/to-delete.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkspaceDocuments.Add(docToDelete);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteDocumentAsync(docToDelete.Id, _clientId);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.WorkspaceDocuments.FindAsync(docToDelete.Id);
        deleted!.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteDocumentAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteDocumentAsync(_documentId, _unauthorizedUserId);

        // Assert
        result.Should().BeFalse();
        var doc = await _context.WorkspaceDocuments.FindAsync(_documentId);
        doc!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDocumentAsync_NotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteDocumentAsync(Guid.NewGuid(), _clientId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDocumentAsync_CreatesAuditLog()
    {
        // Arrange
        var docToDelete = new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            FileName = "audit-delete.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/audit-delete.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkspaceDocuments.Add(docToDelete);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteDocumentAsync(docToDelete.Id, _clientId);
        await Task.Delay(200);

        // Assert
        var auditLogs = await _context.AuditLogs.ToListAsync();
        auditLogs.Should().Contain(log => log.Action == "DocumentDeleted" && log.UserId == _clientId);
    }

    #endregion

    #region RestoreDocumentAsync Tests

    [Fact]
    public async Task RestoreDocumentAsync_ValidDeletedDocument_RestoresDocument()
    {
        // Arrange
        var docToRestore = new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            FileName = "to-restore.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/to-restore.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.WorkspaceDocuments.Add(docToRestore);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RestoreDocumentAsync(docToRestore.Id, _clientId);

        // Assert
        result.Should().BeTrue();
        var restored = await _context.WorkspaceDocuments.FindAsync(docToRestore.Id);
        restored!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreDocumentAsync_NotDeleted_ReturnsFalse()
    {
        // Act
        var result = await _service.RestoreDocumentAsync(_documentId, _clientId);

        // Assert
        result.Should().BeFalse(); // Document is not deleted
    }

    #endregion

    #region SearchDocumentsAsync Tests

    [Fact]
    public async Task SearchDocumentsAsync_ByFileName_ReturnsMatches()
    {
        // Arrange
        var request = new SearchDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            SearchQuery = "test-document",
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.SearchDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().Contain(d => d.FileName.Contains("test-document"));
    }

    [Fact]
    public async Task SearchDocumentsAsync_ByFileType_FiltersCorrectly()
    {
        // Arrange
        var request = new SearchDocumentsRequest
        {
            WorkspaceId = _workspaceId,
            FileTypes = new List<string> { "application/pdf" },
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.SearchDocumentsAsync(request, _clientId);

        // Assert
        result.Documents.Should().OnlyContain(d => d.MimeType == "application/pdf");
    }

    #endregion

    #region GetRecentDocumentsAsync Tests

    [Fact]
    public async Task GetRecentDocumentsAsync_ValidWorkspace_ReturnsRecentDocuments()
    {
        // Act
        var result = await _service.GetRecentDocumentsAsync(_workspaceId, _clientId, 10);

        // Assert
        result.Documents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRecentDocumentsAsync_UnauthorizedUser_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetRecentDocumentsAsync(_workspaceId, _unauthorizedUserId, 10);

        // Assert
        result.Documents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentDocumentsAsync_RespectsCount()
    {
        // Arrange - Add more documents
        for (int i = 0; i < 5; i++)
        {
            var doc = new WorkspaceDocument
            {
                Id = Guid.NewGuid(),
                WorkspaceId = _workspaceId,
                FileName = $"recent-test-{i}.pdf",
                FilePath = $"workspaces/{_workspaceId}/documents/recent-{i}.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = _clientId,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            _context.WorkspaceDocuments.Add(doc);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentDocumentsAsync(_workspaceId, _clientId, 3);

        // Assert
        result.Documents.Should().HaveCountLessOrEqualTo(3);
    }

    #endregion

    #region GetWorkspaceStorageStatsAsync Tests

    [Fact]
    public async Task GetWorkspaceStorageStatsAsync_ValidWorkspace_ReturnsStats()
    {
        // Act
        var result = await _service.GetWorkspaceStorageStatsAsync(_workspaceId, _clientId);

        // Assert
        result.Should().NotBeNull();
        result.WorkspaceId.Should().Be(_workspaceId);
        result.DocumentCount.Should().BeGreaterThan(0);
        result.TotalSizeBytes.Should().BeGreaterThan(0);
    }

    #endregion

    #region ValidateDocumentAccessAsync Tests

    [Fact]
    public async Task ValidateDocumentAccessAsync_ValidClientAccess_ReturnsTrue()
    {
        // Act
        var result = await _service.ValidateDocumentAccessAsync(_documentId, _clientId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDocumentAccessAsync_ValidProviderAccess_ReturnsTrue()
    {
        // Act
        var result = await _service.ValidateDocumentAccessAsync(_documentId, _providerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDocumentAccessAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateDocumentAccessAsync(_documentId, _unauthorizedUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateDocumentAccessAsync_NotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateDocumentAccessAsync(Guid.NewGuid(), _clientId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region BulkDeleteDocumentsAsync Tests

    [Fact]
    public async Task BulkDeleteDocumentsAsync_ValidDocuments_DeletesAll()
    {
        // Arrange
        var doc1Id = Guid.NewGuid();
        var doc2Id = Guid.NewGuid();

        _context.WorkspaceDocuments.AddRange(
            new WorkspaceDocument
            {
                Id = doc1Id,
                WorkspaceId = _workspaceId,
                FileName = "bulk-1.pdf",
                FilePath = $"workspaces/{_workspaceId}/documents/bulk-1.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = _clientId,
                CreatedAt = DateTime.UtcNow
            },
            new WorkspaceDocument
            {
                Id = doc2Id,
                WorkspaceId = _workspaceId,
                FileName = "bulk-2.pdf",
                FilePath = $"workspaces/{_workspaceId}/documents/bulk-2.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                UploadedBy = _clientId,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.BulkDeleteDocumentsAsync(new List<Guid> { doc1Id, doc2Id }, _clientId);

        // Assert
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteDocumentsAsync_MixedPermissions_ReportsPartialSuccess()
    {
        // Arrange - One valid, one unauthorized
        var validDocId = Guid.NewGuid();
        _context.WorkspaceDocuments.Add(new WorkspaceDocument
        {
            Id = validDocId,
            WorkspaceId = _workspaceId,
            FileName = "bulk-valid.pdf",
            FilePath = $"workspaces/{_workspaceId}/documents/bulk-valid.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            UploadedBy = _clientId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.BulkDeleteDocumentsAsync(new List<Guid> { validDocId, nonExistentId }, _clientId);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
    }

    #endregion

    #region SendDocumentNotificationAsync Tests

    [Fact]
    public async Task SendDocumentNotificationAsync_ValidDocument_SendsNotification()
    {
        // Act
        var result = await _service.SendDocumentNotificationAsync(
            _documentId,
            DocumentNotificationType.DocumentShared,
            _providerId,
            _clientId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendDocumentNotificationAsync_DocumentNotFound_ReturnsFalse()
    {
        // Act
        var result = await _service.SendDocumentNotificationAsync(
            Guid.NewGuid(),
            DocumentNotificationType.DocumentShared,
            _providerId,
            _clientId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetSecureDownloadUrlAsync Tests

    [Fact]
    public async Task GetSecureDownloadUrlAsync_ValidAccess_ReturnsUrl()
    {
        // Act
        var result = await _service.GetSecureDownloadUrlAsync(_documentId, _clientId);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("token=");
    }

    [Fact]
    public async Task GetSecureDownloadUrlAsync_UnauthorizedUser_ReturnsNull()
    {
        // Act
        var result = await _service.GetSecureDownloadUrlAsync(_documentId, _unauthorizedUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
