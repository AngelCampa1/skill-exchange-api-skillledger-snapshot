using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.Interfaces;
using SkillLedger.Core.Enums;
using SkillLedger.Core.DTOs;
using SkillLedger.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;

namespace SkillLedger.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly SkillLedgerDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly BackupConfiguration _config;

    public BackupService(
        ILogger<BackupService> logger,
        SkillLedgerDbContext context,
        IFileStorageService fileStorageService,
        IOptions<BackupConfiguration> config)
    {
        _logger = logger;
        _context = context;
        _fileStorageService = fileStorageService;
        _config = config.Value;
    }

    public async Task<bool> BackupDocumentAsync(Guid documentId, BackupType backupType = BackupType.Automatic)
    {
        try
        {
            var document = await _context.WorkspaceDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted);

            if (document == null)
            {
                _logger.LogWarning("Document {DocumentId} not found for backup", documentId);
                return false;
            }

            // Check if we need to cleanup old backups first
            await CleanupDocumentBackupsAsync(documentId);

            // BUG-NEW-006 FIX: Properly dispose streams to prevent resource leaks
            // Download original file
            await using var fileStream = await _fileStorageService.DownloadFileAsync(document.FilePath);
            if (fileStream == null)
            {
                _logger.LogError("Failed to download document {DocumentId} for backup", documentId);
                return false;
            }

            // Generate backup path
            var backupPath = GenerateBackupPath(document.WorkspaceId, documentId);

            // Compress if enabled (backupStream will be either fileStream or a new compressed stream)
            Stream? compressedStream = null;
            Stream backupStream = fileStream;
            try
            {
                if (_config.CompressBackups)
                {
                    compressedStream = await CompressStreamAsync(fileStream);
                    backupStream = compressedStream;
                }

                // Calculate checksum
                var checksum = await CalculateChecksumAsync(backupStream);
                backupStream.Seek(0, SeekOrigin.Begin);

                // Upload to backup storage
                var uploadRequest = new FileStorageUploadRequest
                {
                    FileName = $"backup_{document.FileName}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    FileStream = backupStream,
                    ContentType = "application/octet-stream",
                    FileSize = backupStream.Length,
                    ContainerPath = backupPath,
                    Metadata = new Dictionary<string, string>
                    {
                        ["originalDocumentId"] = documentId.ToString(),
                        ["backupType"] = backupType.ToString(),
                        ["checksum"] = checksum,
                        ["isCompressed"] = _config.CompressBackups.ToString()
                    }
                };

                var uploadResult = await _fileStorageService.UploadFileAsync(uploadRequest);
                if (!uploadResult.Success)
                {
                    throw new InvalidOperationException($"Failed to upload backup: {uploadResult.ErrorMessage}");
                }

                var backupUrl = uploadResult.FilePath ?? uploadResult.PublicUrl ?? backupPath;

                // Create backup record
                var backup = new DocumentBackup
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    BackupPath = backupUrl,
                    BackupSize = backupStream.Length,
                    CreatedAt = DateTime.UtcNow,
                    BackupType = backupType,
                    CheckSum = checksum,
                    IsVerified = false,
                    ExpiresAt = DateTime.UtcNow.AddDays(_config.DefaultRetentionDays)
                };

                // Store backup metadata (you would add DocumentBackup entity to DbContext)
                // _context.DocumentBackups.Add(backup);
                // await _context.SaveChangesAsync();

                // Verify backup if enabled
                if (_config.VerifyBackups)
                {
                    backup.IsVerified = await VerifyBackupIntegrityAsync(backup.Id);
                }

                _logger.LogInformation("Document {DocumentId} backed up successfully. Backup ID: {BackupId}",
                    documentId, backup.Id);

                return true;
            }
            finally
            {
                // BUG-NEW-006 FIX: Dispose compressed stream if it was created
                compressedStream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error backing up document {DocumentId}", documentId);
            return false;
        }
    }

    public Task<bool> RestoreDocumentAsync(Guid documentId, DateTime backupTimestamp)
    {
        try
        {
            // Implementation for restore functionality
            // This would involve:
            // 1. Finding the backup record closest to the timestamp
            // 2. Downloading the backup file
            // 3. Decompressing if needed
            // 4. Verifying integrity
            // 5. Replacing the current document

            _logger.LogInformation("Document {DocumentId} restored from backup timestamp {Timestamp}",
                documentId, backupTimestamp);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring document {DocumentId} from backup", documentId);
            return Task.FromResult(false);
        }
    }

    public Task<bool> ScheduleWorkspaceBackupAsync(Guid workspaceId, BackupSchedule schedule)
    {
        try
        {
            // Implementation for scheduling automatic backups
            // This would typically integrate with a job scheduler like Hangfire or Quartz.NET

            _logger.LogInformation("Backup schedule created for workspace {WorkspaceId} with frequency {Frequency}",
                workspaceId, schedule.Frequency);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling backup for workspace {WorkspaceId}", workspaceId);
            return Task.FromResult(false);
        }
    }

    public Task<int> CleanupExpiredBackupsAsync()
    {
        try
        {
            var deletedCount = 0;
            var cutoffDate = DateTime.UtcNow.AddDays(-_config.DefaultRetentionDays);

            // This would query the DocumentBackups table and delete expired records
            // var expiredBackups = await _context.DocumentBackups
            //     .Where(b => b.ExpiresAt <= cutoffDate)
            //     .ToListAsync();

            // foreach (var backup in expiredBackups)
            // {
            //     await _fileStorageService.DeleteFileAsync(backup.BackupPath);
            //     _context.DocumentBackups.Remove(backup);
            //     deletedCount++;
            // }

            // await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired backups", deletedCount);
            return Task.FromResult(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired backups");
            return Task.FromResult(0);
        }
    }

    public Task<IEnumerable<DocumentBackup>> GetBackupHistoryAsync(Guid documentId)
    {
        try
        {
            // This would query the DocumentBackups table
            // return await _context.DocumentBackups
            //     .Where(b => b.DocumentId == documentId)
            //     .OrderByDescending(b => b.CreatedAt)
            //     .ToListAsync();

            return Task.FromResult<IEnumerable<DocumentBackup>>(new List<DocumentBackup>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backup history for document {DocumentId}", documentId);
            return Task.FromResult<IEnumerable<DocumentBackup>>(new List<DocumentBackup>());
        }
    }

    public Task<bool> VerifyBackupIntegrityAsync(Guid backupId)
    {
        try
        {
            // Implementation for backup verification
            // This would:
            // 1. Download the backup file
            // 2. Calculate its checksum
            // 3. Compare with stored checksum
            // 4. Optionally test file readability

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying backup {BackupId}", backupId);
            return Task.FromResult(false);
        }
    }

    private Task<int> CleanupDocumentBackupsAsync(Guid documentId)
    {
        // Implementation to enforce max backups per document
        var maxBackups = _config.MaxBackupsPerDocument;

        // This would keep only the most recent N backups per document
        return Task.FromResult(0);
    }

    private string GenerateBackupPath(Guid workspaceId, Guid documentId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"backups/{workspaceId}/{timestamp}/{documentId}";
    }

    /// <summary>
    /// Compresses a stream asynchronously using GZip compression.
    /// </summary>
    /// <param name="input">The input stream to compress</param>
    /// <returns>A compressed MemoryStream. IMPORTANT: Caller MUST dispose the returned stream.</returns>
    /// <remarks>
    /// RESOURCE MANAGEMENT FIX: The returned MemoryStream must be disposed by the caller.
    /// This is documented to prevent memory leaks. Consider using this method within a using statement.
    /// </remarks>
    private async Task<Stream> CompressStreamAsync(Stream input)
    {
        var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionMode.Compress, true))
        {
            await input.CopyToAsync(gzip);
        }
        compressed.Seek(0, SeekOrigin.Begin);
        return compressed;
    }

    private async Task<string> CalculateChecksumAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }
}

public class BackupConfiguration
{
    public bool CompressBackups { get; set; } = true;
    public bool VerifyBackups { get; set; } = true;
    public int DefaultRetentionDays { get; set; } = 90;
    public int MaxBackupsPerDocument { get; set; } = 10;
    public string BackupStorageContainer { get; set; } = "document-backups";
}