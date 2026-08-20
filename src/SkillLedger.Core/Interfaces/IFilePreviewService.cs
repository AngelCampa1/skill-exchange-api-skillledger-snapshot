using SkillLedger.Core.Models;

namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for generating file previews and thumbnails
    /// </summary>
    public interface IFilePreviewService
    {
        /// <summary>
        /// Generates a preview for the specified file
        /// </summary>
        Task<FilePreviewResult> GeneratePreviewAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Gets a cached preview if available
        /// </summary>
        Task<FilePreviewResult?> GetCachedPreviewAsync(Guid documentId);

        /// <summary>
        /// Generates a thumbnail image for the file
        /// </summary>
        Task<byte[]?> GenerateThumbnailAsync(Stream fileStream, string fileName, string contentType, int maxWidth = 200, int maxHeight = 200);

        /// <summary>
        /// Checks if preview generation is supported for the file type
        /// </summary>
        bool IsPreviewSupported(string fileName, string contentType);

        /// <summary>
        /// Gets metadata about the file without downloading
        /// </summary>
        Task<FileMetadata> ExtractMetadataAsync(Stream fileStream, string fileName, string contentType);
    }

    /// <summary>
    /// Result of file preview generation
    /// </summary>
    public class FilePreviewResult
    {
        public Guid DocumentId { get; set; }
        public FilePreviewType PreviewType { get; set; }
        public string? PreviewContent { get; set; }
        public string? PreviewUrl { get; set; }
        public byte[]? ThumbnailData { get; set; }
        public string? ThumbnailUrl { get; set; }
        public FileMetadata Metadata { get; set; } = new FileMetadata();
        public bool IsGenerated { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Types of file previews
    /// </summary>
    public enum FilePreviewType
    {
        None,
        Text,
        Image,
        Pdf,
        Office,
        Code,
        Video,
        Audio,
        Archive
    }

    /// <summary>
    /// File metadata information
    /// </summary>
    public class FileMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Image-specific metadata
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? ColorDepth { get; set; }

        // Document-specific metadata
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Subject { get; set; }
        public string? Keywords { get; set; }
        public int? PageCount { get; set; }

        // Audio/Video metadata
        public TimeSpan? Duration { get; set; }
        public string? Codec { get; set; }
        public int? Bitrate { get; set; }
    }
}