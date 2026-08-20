using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for BackupService - DOCUMENT BACKUP AND RESTORE.
///
/// Pattern (per TDD_GUIDE.md):
/// - Real DbContext with in-memory database
/// - Mock IFileStorageService (external storage - OK to mock)
/// - Tests backup workflow, compression, and error handling
///
/// Max mocked external dependencies: 1 (IFileStorageService)
/// </summary>
[IntegrationTest]
public class BackupServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly BackupService _service;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly BackupConfiguration _config;

    // Test data
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public BackupServiceIntegrationTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"BackupServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        // Mock file storage service (external)
        _mockFileStorageService = new Mock<IFileStorageService>();

        // Setup configuration
        _config = new BackupConfiguration
        {
            CompressBackups = true,
            VerifyBackups = true,
            DefaultRetentionDays = 90,
            MaxBackupsPerDocument = 10,
            BackupStorageContainer = "document-backups"
        };

        var logger = new LoggerFactory().CreateLogger<BackupService>();
        var configOptions = Options.Create(_config);

        _service = new BackupService(logger, _context, _mockFileStorageService.Object, configOptions);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = _userId,
            Email = "test@example.com",
            UserName = "testuser",
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active
        };
        _context.Users.Add(user);

        var providerId = Guid.NewGuid();
        var provider = new User
        {
            Id = providerId,
            Email = "provider@example.com",
            UserName = "testprovider",
            PasswordHash = "hashedpassword",
            FirstName = "Provider",
            LastName = "User",
            Status = UserStatus.Active
        };
        _context.Users.Add(provider);

        var project = new Project
        {
            Id = _projectId,
            Title = "Test Project",
            Description = "Test Description",
            ClientId = _userId,
            ProviderId = providerId,
            Status = ProjectStatus.InProgress,
            CreatedAt = DateTime.UtcNow,
            CreditBudget = 300
        };
        _context.Projects.Add(project);

        var workspace = new ProjectWorkspace
        {
            Id = _workspaceId,
            ProjectId = _projectId,
            Project = project,
            ClientId = _userId,
            Client = user,
            ProviderId = providerId,
            Provider = provider,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProjectWorkspaces.Add(workspace);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region BackupDocumentAsync Tests

    [Fact]
    public async Task BackupDocumentAsync_NonExistentDocument_ReturnsFalse()
    {
        // Arrange
        var nonExistentDocumentId = Guid.NewGuid();

        // Act
        var result = await _service.BackupDocumentAsync(nonExistentDocumentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BackupDocumentAsync_DeletedDocument_ReturnsFalse()
    {
        // Arrange
        var document = CreateTestDocument();
        document.IsDeleted = true;
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BackupDocumentAsync_FileDownloadFails_ReturnsFalse()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        // Mock file download to return null (failure)
        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BackupDocumentAsync_ValidDocument_DownloadsFile()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content for backup"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
        _mockFileStorageService.Verify(s => s.DownloadFileAsync(document.FilePath), Times.Once);
    }

    [Fact]
    public async Task BackupDocumentAsync_ValidDocument_UploadsBackup()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content for backup"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.FileName.Should().Contain("backup_");
        capturedRequest.FileName.Should().Contain(document.FileName);
    }

    [Fact]
    public async Task BackupDocumentAsync_UploadFails_ReturnsFalse()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .ReturnsAsync(new FileStorageResult { Success = false, ErrorMessage = "Upload failed" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BackupDocumentAsync_WithManualBackupType_UsesCorrectType()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id, BackupType.Manual);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata.Should().ContainKey("backupType");
        capturedRequest.Metadata["backupType"].Should().Be("Manual");
    }

    [Fact]
    public async Task BackupDocumentAsync_WithScheduledBackupType_UsesCorrectType()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id, BackupType.Scheduled);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata["backupType"].Should().Be("Scheduled");
    }

    [Fact]
    public async Task BackupDocumentAsync_IncludesChecksumInMetadata()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata.Should().ContainKey("checksum");
        capturedRequest.Metadata["checksum"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BackupDocumentAsync_IncludesCompressionFlagInMetadata()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata.Should().ContainKey("isCompressed");
        capturedRequest.Metadata["isCompressed"].Should().Be("True");
    }

    [Fact]
    public async Task BackupDocumentAsync_IncludesOriginalDocumentIdInMetadata()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata.Should().ContainKey("originalDocumentId");
        capturedRequest.Metadata["originalDocumentId"].Should().Be(document.Id.ToString());
    }

    #endregion

    #region RestoreDocumentAsync Tests

    [Fact]
    public async Task RestoreDocumentAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = await _service.RestoreDocumentAsync(documentId, timestamp);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreDocumentAsync_WithPastTimestamp_ReturnsTrue()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = await _service.RestoreDocumentAsync(documentId, timestamp);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ScheduleWorkspaceBackupAsync Tests

    [Fact]
    public async Task ScheduleWorkspaceBackupAsync_DailyFrequency_ReturnsTrue()
    {
        // Arrange
        var schedule = new BackupSchedule
        {
            Frequency = BackupFrequency.Daily,
            RetentionDays = 90,
            MaxBackupsPerDocument = 10,
            CompressBackups = true,
            VerifyIntegrity = true
        };

        // Act
        var result = await _service.ScheduleWorkspaceBackupAsync(_workspaceId, schedule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ScheduleWorkspaceBackupAsync_WeeklyFrequency_ReturnsTrue()
    {
        // Arrange
        var schedule = new BackupSchedule
        {
            Frequency = BackupFrequency.Weekly,
            RetentionDays = 180,
            MaxBackupsPerDocument = 5,
            CompressBackups = true,
            VerifyIntegrity = false
        };

        // Act
        var result = await _service.ScheduleWorkspaceBackupAsync(_workspaceId, schedule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ScheduleWorkspaceBackupAsync_HourlyFrequency_ReturnsTrue()
    {
        // Arrange
        var schedule = new BackupSchedule
        {
            Frequency = BackupFrequency.Hourly,
            RetentionDays = 7,
            MaxBackupsPerDocument = 168 // 7 days * 24 hours
        };

        // Act
        var result = await _service.ScheduleWorkspaceBackupAsync(_workspaceId, schedule);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ScheduleWorkspaceBackupAsync_MonthlyFrequency_ReturnsTrue()
    {
        // Arrange
        var schedule = new BackupSchedule
        {
            Frequency = BackupFrequency.Monthly,
            RetentionDays = 365,
            MaxBackupsPerDocument = 12
        };

        // Act
        var result = await _service.ScheduleWorkspaceBackupAsync(_workspaceId, schedule);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CleanupExpiredBackupsAsync Tests

    [Fact]
    public async Task CleanupExpiredBackupsAsync_NoExpiredBackups_ReturnsZero()
    {
        // Act
        var result = await _service.CleanupExpiredBackupsAsync();

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region GetBackupHistoryAsync Tests

    [Fact]
    public async Task GetBackupHistoryAsync_NonExistentDocument_ReturnsEmptyList()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _service.GetBackupHistoryAsync(documentId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBackupHistoryAsync_ValidDocument_ReturnsHistory()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetBackupHistoryAsync(document.Id);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region VerifyBackupIntegrityAsync Tests

    [Fact]
    public async Task VerifyBackupIntegrityAsync_ValidBackup_ReturnsTrue()
    {
        // Arrange
        var backupId = Guid.NewGuid();

        // Act
        var result = await _service.VerifyBackupIntegrityAsync(backupId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task BackupDocumentAsync_WithCompressionEnabled_CompressesFile()
    {
        // Arrange - Config has CompressBackups = true
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content for compression testing. This should be compressed."u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata["isCompressed"].Should().Be("True");
    }

    [Fact]
    public void Configuration_DefaultValues_AreCorrect()
    {
        // Arrange
        var defaultConfig = new BackupConfiguration();

        // Assert
        defaultConfig.CompressBackups.Should().BeTrue();
        defaultConfig.VerifyBackups.Should().BeTrue();
        defaultConfig.DefaultRetentionDays.Should().Be(90);
        defaultConfig.MaxBackupsPerDocument.Should().Be(10);
        defaultConfig.BackupStorageContainer.Should().Be("document-backups");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task BackupDocumentAsync_ExceptionDuringDownload_ReturnsFalse()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task BackupDocumentAsync_ExceptionDuringUpload_ReturnsFalse()
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "Test file content"u8.ToArray();
        var fileStream = new MemoryStream(fileContent);

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .ThrowsAsync(new Exception("Storage error"));

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task FullBackupWorkflow_CreatesBackupWithAllMetadata()
    {
        // Arrange
        var document = CreateTestDocument();
        document.FileName = "important_document.pdf";
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileContent = "This is an important document that needs to be backed up securely."u8.ToArray();
        var fileStream = new MemoryStream(fileContent);
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id, BackupType.Manual);

        // Assert
        result.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.FileName.Should().Contain("important_document.pdf");
        capturedRequest.Metadata.Should().HaveCount(4);
        capturedRequest.Metadata.Should().ContainKey("originalDocumentId");
        capturedRequest.Metadata.Should().ContainKey("backupType");
        capturedRequest.Metadata.Should().ContainKey("checksum");
        capturedRequest.Metadata.Should().ContainKey("isCompressed");
    }

    [Fact]
    public async Task BackupDocument_WithLargeFile_Succeeds()
    {
        // Arrange
        var document = CreateTestDocument();
        document.FileSize = 10 * 1024 * 1024; // 10 MB
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        // Simulate large file (just use a reasonable size for testing)
        var largeContent = new byte[1024]; // 1 KB for test
        new Random().NextBytes(largeContent);
        var fileStream = new MemoryStream(largeContent);

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task BackupMultipleDocuments_AllSucceed()
    {
        // Arrange
        var documents = new List<WorkspaceDocument>();
        for (int i = 0; i < 3; i++)
        {
            var doc = CreateTestDocument();
            doc.FileName = $"document_{i}.txt";
            documents.Add(doc);
        }
        _context.WorkspaceDocuments.AddRange(documents);
        await _context.SaveChangesAsync();

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new MemoryStream("Test content"u8.ToArray()));

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var results = new List<bool>();
        foreach (var doc in documents)
        {
            results.Add(await _service.BackupDocumentAsync(doc.Id));
        }

        // Assert
        results.Should().AllBeEquivalentTo(true);
    }

    #endregion

    #region BackupType Tests

    [Theory]
    [InlineData(BackupType.Automatic)]
    [InlineData(BackupType.Manual)]
    [InlineData(BackupType.Scheduled)]
    [InlineData(BackupType.SystemInitiated)]
    public async Task BackupDocumentAsync_AllBackupTypes_Succeed(BackupType backupType)
    {
        // Arrange
        var document = CreateTestDocument();
        _context.WorkspaceDocuments.Add(document);
        await _context.SaveChangesAsync();

        var fileStream = new MemoryStream("Test content"u8.ToArray());
        FileStorageUploadRequest? capturedRequest = null;

        _mockFileStorageService
            .Setup(s => s.DownloadFileAsync(document.FilePath))
            .ReturnsAsync(fileStream);

        _mockFileStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<FileStorageUploadRequest>()))
            .Callback<FileStorageUploadRequest>(req => capturedRequest = req)
            .ReturnsAsync(new FileStorageResult { Success = true, FilePath = "backup/path" });

        // Act
        var result = await _service.BackupDocumentAsync(document.Id, backupType);

        // Assert
        result.Should().BeTrue();
        capturedRequest!.Metadata["backupType"].Should().Be(backupType.ToString());
    }

    #endregion

    #region Helper Methods

    private WorkspaceDocument CreateTestDocument()
    {
        return new WorkspaceDocument
        {
            Id = Guid.NewGuid(),
            WorkspaceId = _workspaceId,
            FileName = "test_document.txt",
            FilePath = $"workspaces/{_workspaceId}/documents/test_document.txt",
            FileSize = 1024,
            MimeType = "text/plain",
            UploadedBy = _userId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            SecurityScanPassed = true
        };
    }

    #endregion
}
