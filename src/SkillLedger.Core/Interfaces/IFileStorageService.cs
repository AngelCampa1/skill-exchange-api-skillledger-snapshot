using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Abstraction layer for file storage operations
    /// Supports both local file storage and Azure Blob Storage
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Uploads a file to storage and returns the storage path
        /// </summary>
        /// <param name="request">Upload request with file data</param>
        /// <returns>Storage result with path and metadata</returns>
        Task<FileStorageResult> UploadFileAsync(FileStorageUploadRequest request);

        /// <summary>
        /// Downloads a file from storage
        /// </summary>
        /// <param name="filePath">Storage path of the file</param>
        /// <returns>File stream or null if not found</returns>
        Task<Stream?> DownloadFileAsync(string filePath);

        /// <summary>
        /// Generates a secure, time-limited URL for file access
        /// </summary>
        /// <param name="filePath">Storage path of the file</param>
        /// <param name="expirationMinutes">URL expiration time in minutes</param>
        /// <param name="permission">Access permission level</param>
        /// <returns>Secure URL or null if not supported</returns>
        Task<string?> GetSecureUrlAsync(string filePath, int expirationMinutes = 60, FileAccessPermission permission = FileAccessPermission.Read);

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        /// <param name="filePath">Storage path of the file</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Checks if a file exists in storage
        /// </summary>
        /// <param name="filePath">Storage path to check</param>
        /// <returns>True if file exists</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Gets file metadata without downloading the file
        /// </summary>
        /// <param name="filePath">Storage path of the file</param>
        /// <returns>File metadata or null if not found</returns>
        Task<FileStorageMetadata?> GetFileMetadataAsync(string filePath);

        /// <summary>
        /// Copies a file within storage (for version management)
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <returns>True if copied successfully</returns>
        Task<bool> CopyFileAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Moves a file within storage
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <returns>True if moved successfully</returns>
        Task<bool> MoveFileAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Lists files in a storage container/directory
        /// </summary>
        /// <param name="containerPath">Container or directory path</param>
        /// <param name="prefix">File name prefix filter</param>
        /// <returns>List of file paths</returns>
        Task<List<string>> ListFilesAsync(string containerPath, string? prefix = null);

        /// <summary>
        /// Gets storage statistics for a container/directory
        /// </summary>
        /// <param name="containerPath">Container or directory path</param>
        /// <returns>Storage statistics</returns>
        Task<FileStorageStats> GetStorageStatsAsync(string containerPath);

        /// <summary>
        /// Generates file preview (thumbnails, etc.)
        /// </summary>
        /// <param name="filePath">Source file path</param>
        /// <param name="previewOptions">Preview generation options</param>
        /// <returns>Preview result with generated preview paths</returns>
        Task<FileStoragePreviewResult> GeneratePreviewAsync(string filePath, FilePreviewOptions previewOptions);
    }

    // Supporting DTOs and Enums
    public class FileStorageUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContainerPath { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public bool OverwriteIfExists { get; set; } = false;
    }

    public class FileStorageResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? ErrorMessage { get; set; }
        public FileStorageMetadata? Metadata { get; set; }
        public string? PublicUrl { get; set; }
    }

    public class FileStorageMetadata
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public string ETag { get; set; } = string.Empty;
        public Dictionary<string, string> CustomMetadata { get; set; } = new Dictionary<string, string>();
    }

    public class FileStorageStats
    {
        public string ContainerPath { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long TotalSizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public Dictionary<string, int> FileTypeDistribution { get; set; } = new Dictionary<string, int>();
    }

    public class FileStoragePreviewResult
    {
        public bool Success { get; set; }
        public Dictionary<string, string> PreviewPaths { get; set; } = new Dictionary<string, string>();
        public string? ErrorMessage { get; set; }
    }

    public class FilePreviewOptions
    {
        public bool GenerateThumbnail { get; set; } = true;
        public int ThumbnailWidth { get; set; } = 300;
        public int ThumbnailHeight { get; set; } = 300;
        public bool GeneratePreview { get; set; } = true;
        public int PreviewWidth { get; set; } = 800;
        public int PreviewHeight { get; set; } = 600;
        public List<string> PreviewFormats { get; set; } = new List<string> { "jpg", "webp" };
    }

    public enum FileAccessPermission
    {
        Read = 1,
        Write = 2,
        Delete = 4,
        All = Read | Write | Delete
    }
}