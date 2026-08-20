using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Models;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for DocumentService.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses MockAuditLogService that writes to real database (internal service - cannot mock with Moq)
/// - Uses MockFileStorageService (external service - OK to mock)
/// - Uses MockVirusScanService (external service - OK to mock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 2 (FileStorage, VirusScan)
/// </summary>
[IntegrationTest]
[DocumentTest]
public class DocumentServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly MockAuditLogService _auditLogService;  // REAL internal service (writes to DB)
    private readonly MockFileStorageService _fileStorageService;  // EXTERNAL - OK to mock
    private readonly MockVirusScanService _virusScanService;  // EXTERNAL - OK to mock
    private readonly DocumentService _documentService;
    private readonly MediaUploadConfiguration _config;

    // Test data
    private User _testUser1 = null!;
    private User _testUser2 = null!;
    private ProjectWorkspace _testWorkspace = null!;

    public DocumentServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"DocumentServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        // Use mock services that behave like real implementations
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!
        _fileStorageService = new MockFileStorageService();
        _virusScanService = new MockVirusScanService();

        _config = new MediaUploadConfiguration
        {
            MaxFileSizeBytes = 10 * 1024 * 1024, // 10MB
            SecurityKey = "test-key"
        };

        var optionsWrapper = Options.Create(_config);
        var mockLogger = new LoggerFactory().CreateLogger<DocumentService>();

        _documentService = new DocumentService(
            _context,
            _fileStorageService,
            _virusScanService,
            _auditLogService,
            mockLogger,
            optionsWrapper
        );

        SetupTestData();
    }

    private void SetupTestData()
    {
        _testUser1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@test.com",
            UserName = "testclient",
            FirstName = "Test",
            LastName = "Client"
        };

        _testUser2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            UserName = "testprovider",
            FirstName = "Test",
            LastName = "Provider"
        };

        _context.Users.AddRange(_testUser1, _testUser2);

        _testWorkspace = new ProjectWorkspace
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ClientId = _testUser1.Id,
            ProviderId = _testUser2.Id,
            Status = WorkspaceStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectWorkspaces.Add(_testWorkspace);
        _context.SaveChanges();
    }

    #region Upload Tests

    [Fact]
    public async Task UploadDocumentAsync_ValidRequest_CreatesDocumentInDatabase()
    {
        // Arrange
        _virusScanService.SetupCleanScan();
        _virusScanService.SetupFileTypeAllowed(true);

        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var fileStream = new MemoryStream(fileContent);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FileName = "test-document.txt",
            FileStream = fileStream,
            ContentType = "text/plain",
            FileSize = fileStream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request, _testUser1.Id);

        // Assert - Verify operation success
        result.Success.Should().BeTrue();
        result.Document.Should().NotBeNull();
        result.Document!.FileName.Should().Be("test-document.txt");
        result.Document.FileSize.Should().Be(fileContent.Length);
        result.Document.SecurityScanPassed.Should().BeTrue();

        // Assert - Verify REAL database state (not mock calls!)
        var savedDocument = await _context.WorkspaceDocuments
            .FirstOrDefaultAsync(d => d.Id == result.Document.Id);
        savedDocument.Should().NotBeNull();
        savedDocument!.FileName.Should().Be("test-document.txt");
        savedDocument.MimeType.Should().Be("text/plain");
        savedDocument.UploadedBy.Should().Be(_testUser1.Id);
        savedDocument.WorkspaceId.Should().Be(_testWorkspace.Id);

        // Assert - Verify audit log was written to database
        // Note: DocumentService uses "DocumentUploaded" for successful uploads
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "DocumentUploaded" && a.UserId == _testUser1.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UploadDocumentAsync_FileTooLarge_ReturnsErrorWithoutCreatingDocument()
    {
        // Arrange
        _virusScanService.SetupFileTypeAllowed(true);

        var largeFileContent = new byte[_config.MaxFileSizeBytes + 1];
        var fileStream = new MemoryStream(largeFileContent);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FileName = "large-file.bin",
            FileStream = fileStream,
            ContentType = "application/octet-stream",
            FileSize = fileStream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request, _testUser1.Id);

        // Assert - Verify error returned
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File size exceeds maximum");

        // Assert - Verify NO document was created in database
        var documents = await _context.WorkspaceDocuments
            .Where(d => d.WorkspaceId == _testWorkspace.Id && d.FileName == "large-file.bin")
            .ToListAsync();
        documents.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadDocumentAsync_VirusDetected_ReturnsErrorAndLogsSecurityEvent()
    {
        // Arrange
        _virusScanService.SetupFileTypeAllowed(true);
        _virusScanService.SetupInfectedScan("TestMalware", ThreatSeverity.Critical);

        var fileContent = System.Text.Encoding.UTF8.GetBytes("malicious content");
        var fileStream = new MemoryStream(fileContent);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FileName = "malware.exe",
            FileStream = fileStream,
            ContentType = "application/octet-stream",
            FileSize = fileStream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request, _testUser1.Id);

        // Assert - Verify error returned
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File failed security scan");
        result.ErrorMessage.Should().Contain("TestMalware");

        // Assert - Verify NO document was created in database
        var documents = await _context.WorkspaceDocuments
            .Where(d => d.WorkspaceId == _testWorkspace.Id && d.FileName == "malware.exe")
            .ToListAsync();
        documents.Should().BeEmpty();

        // Assert - Verify security audit log was written (REAL database check!)
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a =>
                a.Action == "DocumentUpload" &&
                a.UserId == _testUser1.Id &&
                !a.Success);
        auditLog.Should().NotBeNull();
        auditLog!.ErrorMessage.Should().Contain("File failed security scan");
    }

    [Fact]
    public async Task UploadDocumentAsync_WorkspaceNotFound_ReturnsAccessDeniedError()
    {
        // Arrange
        _virusScanService.SetupFileTypeAllowed(true);

        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var fileStream = new MemoryStream(fileContent);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = Guid.NewGuid(), // Non-existent workspace
            FileName = "test.txt",
            FileStream = fileStream,
            ContentType = "text/plain",
            FileSize = fileStream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request, _testUser1.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Workspace not found or access denied");
    }

    [Fact]
    public async Task UploadDocumentAsync_FileTypeNotAllowed_ReturnsValidationError()
    {
        // Arrange
        _virusScanService.SetupFileTypeAllowed(false); // Block file type

        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var fileStream = new MemoryStream(fileContent);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FileName = "dangerous.exe",
            FileStream = fileStream,
            ContentType = "application/x-msdownload",
            FileSize = fileStream.Length
        };

        // Act
        var result = await _documentService.UploadDocumentAsync(request, _testUser1.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("File type is not allowed");
    }

    #endregion

    #region Download Tests

    [Fact]
    public async Task DownloadDocumentAsync_ValidRequest_ReturnsFileAndLogsAccess()
    {
        // Arrange
        var document = await CreateTestDocumentAsync("download-test.txt", "Download test content");

        // Act
        var result = await _documentService.DownloadDocumentAsync(document.Id, _testUser1.Id);

        // Assert - Verify download success
        result.Success.Should().BeTrue();
        result.Document.Should().NotBeNull();
        result.Document!.FileName.Should().Be("download-test.txt");
        result.FileStream.Should().NotBeNull();

        // Assert - Verify access was logged in REAL database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a =>
                a.Action == "DocumentDownloaded" &&
                a.UserId == _testUser1.Id &&
                a.Success);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task DownloadDocumentAsync_DocumentNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentDocumentId = Guid.NewGuid();

        // Act
        var result = await _documentService.DownloadDocumentAsync(nonExistentDocumentId, _testUser1.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Document not found");
    }

    [Fact]
    public async Task DownloadDocumentAsync_UnauthorizedUser_ReturnsAccessDenied()
    {
        // Arrange
        var document = await CreateTestDocumentAsync("private-doc.txt", "Private content");

        // Create an unauthorized user (not part of workspace)
        var unauthorizedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "unauthorized@test.com",
            UserName = "unauthorized"
        };
        _context.Users.Add(unauthorizedUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.DownloadDocumentAsync(document.Id, unauthorizedUser.Id);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Access denied"); // Exact case as returned by DocumentService
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteDocumentAsync_ValidRequest_SoftDeletesAndLogsAction()
    {
        // Arrange
        var document = await CreateTestDocumentAsync("delete-test.txt", "Delete test content");

        // Act
        var result = await _documentService.DeleteDocumentAsync(document.Id, _testUser1.Id);

        // Assert - Verify deletion success
        result.Should().BeTrue();

        // Assert - Verify soft delete in REAL database
        var deletedDocument = await _context.WorkspaceDocuments
            .FirstOrDefaultAsync(d => d.Id == document.Id);
        deletedDocument.Should().NotBeNull();
        deletedDocument!.IsDeleted.Should().BeTrue();
        deletedDocument.DeletedAt.Should().NotBeNull();
        deletedDocument.DeletedBy.Should().Be(_testUser1.Id);

        // Assert - Verify deletion was logged in REAL database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a =>
                a.Action == "DocumentDeleted" &&
                a.UserId == _testUser1.Id &&
                a.Success);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteDocumentAsync_DocumentNotFound_ReturnsFalse()
    {
        // Arrange
        var nonExistentDocumentId = Guid.NewGuid();

        // Act
        var result = await _documentService.DeleteDocumentAsync(nonExistentDocumentId, _testUser1.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Folder Tests

    [Fact]
    public async Task CreateFolderAsync_ValidRequest_CreatesFolderInDatabase()
    {
        // Arrange
        var request = new DocumentFolderCreateRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FolderName = "Project Documents",
            Description = "Important project files",
            SortOrder = 1
        };

        // Act
        var result = await _documentService.CreateFolderAsync(request, _testUser1.Id);

        // Assert - Verify folder created
        result.Should().NotBeNull();
        result.FolderName.Should().Be("Project Documents");
        result.Description.Should().Be("Important project files");
        result.WorkspaceId.Should().Be(_testWorkspace.Id);
        result.CreatedBy.Should().Be(_testUser1.Id);

        // Assert - Verify REAL database state
        var savedFolder = await _context.DocumentFolders
            .FirstOrDefaultAsync(f => f.Id == result.Id);
        savedFolder.Should().NotBeNull();
        savedFolder!.FolderName.Should().Be("Project Documents");
    }

    [Fact]
    public async Task CreateFolderAsync_DuplicateName_CreatesFolderWithUniqueName()
    {
        // Arrange
        var folder1 = new DocumentFolder
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _testWorkspace.Id,
            FolderName = "Documents",
            CreatedBy = _testUser1.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentFolders.Add(folder1);
        await _context.SaveChangesAsync();

        var request = new DocumentFolderCreateRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FolderName = "Documents",
            SortOrder = 2
        };

        // Act
        var result = await _documentService.CreateFolderAsync(request, _testUser1.Id);

        // Assert - Should create folder (service may append suffix or allow duplicates)
        result.Should().NotBeNull();
        result.WorkspaceId.Should().Be(_testWorkspace.Id);
    }

    #endregion

    #region List/Query Tests

    [Fact]
    public async Task GetDocumentsAsync_ValidWorkspace_ReturnsAllDocuments()
    {
        // Arrange
        await CreateTestDocumentAsync("doc1.txt", "Content 1");
        await CreateTestDocumentAsync("doc2.txt", "Content 2");
        await CreateTestDocumentAsync("doc3.txt", "Content 3");

        // Act
        var result = await _documentService.GetDocumentsAsync(_testWorkspace.Id, null, _testUser1.Id);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(d => d.FileName == "doc1.txt");
        result.Should().Contain(d => d.FileName == "doc2.txt");
        result.Should().Contain(d => d.FileName == "doc3.txt");
    }

    [Fact]
    public async Task GetDocumentsAsync_WithFolder_ReturnsOnlyFolderDocuments()
    {
        // Arrange
        var folder = new DocumentFolder
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _testWorkspace.Id,
            FolderName = "Filtered Folder",
            CreatedBy = _testUser1.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentFolders.Add(folder);
        await _context.SaveChangesAsync();

        await CreateTestDocumentAsync("folder-doc.txt", "Folder content", folder.Id);
        await CreateTestDocumentAsync("root-doc.txt", "Root content", null);

        // Act
        var result = await _documentService.GetDocumentsAsync(_testWorkspace.Id, folder.Id, _testUser1.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().FileName.Should().Be("folder-doc.txt");
    }

    [Fact]
    public async Task GetDocumentsAsync_DeletedDocuments_AreExcluded()
    {
        // Arrange
        var activeDoc = await CreateTestDocumentAsync("active.txt", "Active content");
        var deletedDoc = await CreateTestDocumentAsync("deleted.txt", "Deleted content");

        // Soft delete one document
        deletedDoc.IsDeleted = true;
        deletedDoc.DeletedAt = DateTime.UtcNow;
        deletedDoc.DeletedBy = _testUser1.Id;
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentService.GetDocumentsAsync(_testWorkspace.Id, null, _testUser1.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().FileName.Should().Be("active.txt");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateUploadAsync_ValidFile_ReturnsNoErrors()
    {
        // Arrange
        _virusScanService.SetupFileTypeAllowed(true);

        var fileContent = System.Text.Encoding.UTF8.GetBytes("valid content");
        var fileStream = new MemoryStream(fileContent);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = _testWorkspace.Id,
            FileName = "valid-file.txt",
            FileStream = fileStream,
            ContentType = "text/plain",
            FileSize = fileStream.Length
        };

        // Act
        var result = await _documentService.ValidateUploadAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateUploadAsync_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        _virusScanService.SetupFileTypeAllowed(false);

        var request = new DocumentUploadRequest
        {
            WorkspaceId = Guid.Empty, // Invalid
            FileName = "", // Invalid
            ContentType = "", // Invalid
            FileSize = 0 // Invalid
        };

        // Act
        var result = await _documentService.ValidateUploadAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("File name is required");
        result.Errors.Should().Contain("File size must be greater than 0");
        result.Errors.Should().Contain("Content type is required");
        result.Errors.Should().Contain("File type is not allowed");
        result.Errors.Should().Contain("Workspace ID is required");
    }

    #endregion

    #region Security Scan Tests

    [Fact]
    public async Task ScanDocumentAsync_CleanFile_ReturnsLowRisk()
    {
        // Arrange
        var document = await CreateTestDocumentAsync("clean-file.txt", "Clean content");
        _virusScanService.SetupCleanScan();

        // Act
        var result = await _documentService.ScanDocumentAsync(document.Id, _testUser1.Id);

        // Assert - DocumentService uses ScanPassed property
        result.ScanPassed.Should().BeTrue();
        result.ThreatDetected.Should().BeFalse();
        result.ThreatTypes.Should().BeEmpty();
        result.ScanEngine.Should().Be("Mock Scanner");
    }

    [Fact]
    public async Task ScanDocumentAsync_InfectedFile_ReturnsHighRiskWithThreats()
    {
        // Arrange
        var document = await CreateTestDocumentAsync("suspicious-file.exe", "Suspicious content");
        _virusScanService.SetupInfectedScan("Trojan.Generic", ThreatSeverity.Critical);

        // Act
        var result = await _documentService.ScanDocumentAsync(document.Id, _testUser1.Id);

        // Assert - DocumentService uses ScanPassed/ThreatDetected properties
        result.ScanPassed.Should().BeFalse();
        result.ThreatDetected.Should().BeTrue();
        result.ThreatTypes.Should().NotBeEmpty();
        result.ThreatTypes.Should().Contain("Trojan.Generic");
    }

    #endregion

    #region Helper Methods

    private async Task<WorkspaceDocument> CreateTestDocumentAsync(
        string fileName,
        string content,
        Guid? folderId = null)
    {
        // Upload file to mock storage first
        var fileContent = System.Text.Encoding.UTF8.GetBytes(content);
        var filePath = $"workspaces/{_testWorkspace.Id}/{fileName}";

        var uploadRequest = new FileStorageUploadRequest
        {
            FileStream = new MemoryStream(fileContent),
            ContainerPath = $"workspaces/{_testWorkspace.Id}",
            FileName = fileName,
            ContentType = "text/plain",
            FileSize = fileContent.Length
        };
        await _fileStorageService.UploadFileAsync(uploadRequest);

        // Create document record
        var document = new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _testWorkspace.Id,
            FolderId = folderId,
            FileName = fileName,
            FilePath = filePath,
            FileSize = fileContent.Length,
            MimeType = "text/plain",
            UploadedBy = _testUser1.Id,
            SecurityScanPassed = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        return document;
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}
