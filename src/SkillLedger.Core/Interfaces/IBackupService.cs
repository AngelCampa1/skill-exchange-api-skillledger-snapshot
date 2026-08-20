using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

public interface IBackupService
{
    /// <summary>
    /// Creates a backup of a workspace document
    /// </summary>
    Task<bool> BackupDocumentAsync(Guid documentId, BackupType backupType = BackupType.Automatic);

    /// <summary>
    /// Restores a document from backup
    /// </summary>
    Task<bool> RestoreDocumentAsync(Guid documentId, DateTime backupTimestamp);

    /// <summary>
    /// Schedules automatic backup for a workspace
    /// </summary>
    Task<bool> ScheduleWorkspaceBackupAsync(Guid workspaceId, BackupSchedule schedule);

    /// <summary>
    /// Cleans up expired backups based on retention policy
    /// </summary>
    Task<int> CleanupExpiredBackupsAsync();

    /// <summary>
    /// Gets backup history for a document
    /// </summary>
    Task<IEnumerable<DocumentBackup>> GetBackupHistoryAsync(Guid documentId);

    /// <summary>
    /// Verifies backup integrity
    /// </summary>
    Task<bool> VerifyBackupIntegrityAsync(Guid backupId);
}

public class DocumentBackup
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string BackupPath { get; set; } = string.Empty;
    public long BackupSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public BackupType BackupType { get; set; }
    public string CheckSum { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class BackupSchedule
{
    public BackupFrequency Frequency { get; set; }
    public int RetentionDays { get; set; } = 90;
    public int MaxBackupsPerDocument { get; set; } = 10;
    public bool CompressBackups { get; set; } = true;
    public bool VerifyIntegrity { get; set; } = true;
}