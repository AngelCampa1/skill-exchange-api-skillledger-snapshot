namespace SkillLedger.Core.Interfaces
{
    /// <summary>
    /// Service for scanning files for malware and security threats
    /// </summary>
    public interface IVirusScanService
    {
        /// <summary>
        /// Scans a file stream for viruses and malware
        /// </summary>
        /// <param name="fileStream">File stream to scan</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="contentType">File content type</param>
        /// <returns>Scan result</returns>
        Task<VirusScanResult> ScanFileAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Scans a file by path for viruses and malware
        /// </summary>
        /// <param name="filePath">Path to the file to scan</param>
        /// <returns>Scan result</returns>
        Task<VirusScanResult> ScanFileAsync(string filePath);

        /// <summary>
        /// Performs a quick scan based on file metadata and signatures
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="contentType">Content type</param>
        /// <param name="fileSize">File size</param>
        /// <returns>Quick scan result</returns>
        Task<VirusScanResult> QuickScanAsync(string fileName, string contentType, long fileSize);

        /// <summary>
        /// Gets the scan engine information
        /// </summary>
        /// <returns>Scan engine details</returns>
        Task<ScanEngineInfo> GetScanEngineInfoAsync();

        /// <summary>
        /// Updates virus definitions if supported
        /// </summary>
        /// <returns>Update result</returns>
        Task<bool> UpdateVirusDefinitionsAsync();

        /// <summary>
        /// Validates if a file type is allowed for upload
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="contentType">Content type</param>
        /// <returns>True if file type is allowed</returns>
        Task<bool> IsFileTypeAllowedAsync(string fileName, string contentType);

        /// <summary>
        /// Gets list of blocked file extensions
        /// </summary>
        /// <returns>List of blocked extensions</returns>
        Task<List<string>> GetBlockedFileExtensionsAsync();

        /// <summary>
        /// Gets list of allowed MIME types
        /// </summary>
        /// <returns>List of allowed MIME types</returns>
        Task<List<string>> GetAllowedMimeTypesAsync();
    }

    public class VirusScanResult
    {
        public bool IsClean { get; set; }
        public bool ScanCompleted { get; set; }
        public List<ThreatInfo> Threats { get; set; } = new();
        public string ScanEngine { get; set; } = string.Empty;
        public string EngineVersion { get; set; } = string.Empty;
        public DateTime ScanDate { get; set; } = DateTime.UtcNow;
        public long ScanDurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    public class ThreatInfo
    {
        public string ThreatName { get; set; } = string.Empty;
        public ThreatSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Action { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public enum ThreatSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public class ScanEngineInfo
    {
        public string EngineName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime DefinitionsDate { get; set; }
        public bool IsOperational { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}