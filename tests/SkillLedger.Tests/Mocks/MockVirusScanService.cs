using SkillLedger.Core.Interfaces;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock virus scan service for testing.
/// This mocks the EXTERNAL virus scanning API (OK to mock per TDD_GUIDE.md).
/// Allows configuring clean/infected scan results and tracking scanned files.
/// </summary>
public class MockVirusScanService : IVirusScanService
{
    private bool _shouldReturnClean = true;
    private bool _fileTypeAllowed = true;
    private string _threatName = "TestVirus";
    private ThreatSeverity _threatSeverity = ThreatSeverity.High;
    private readonly List<ScannedFile> _scannedFiles = new();

    /// <summary>
    /// List of all files scanned through this mock
    /// </summary>
    public IReadOnlyList<ScannedFile> ScannedFiles => _scannedFiles.AsReadOnly();

    /// <summary>
    /// Configure the mock to return clean scan results
    /// </summary>
    public void SetupCleanScan()
    {
        _shouldReturnClean = true;
    }

    /// <summary>
    /// Configure the mock to return infected scan results
    /// </summary>
    public void SetupInfectedScan(string threatName = "TestVirus", ThreatSeverity severity = ThreatSeverity.High)
    {
        _shouldReturnClean = false;
        _threatName = threatName;
        _threatSeverity = severity;
    }

    /// <summary>
    /// Configure whether file types are allowed
    /// </summary>
    public void SetupFileTypeAllowed(bool allowed)
    {
        _fileTypeAllowed = allowed;
    }

    /// <summary>
    /// Clear all state
    /// </summary>
    public void Reset()
    {
        _shouldReturnClean = true;
        _fileTypeAllowed = true;
        _threatName = "TestVirus";
        _threatSeverity = ThreatSeverity.High;
        _scannedFiles.Clear();
    }

    public Task<VirusScanResult> ScanFileAsync(Stream fileStream, string fileName, string contentType)
    {
        _scannedFiles.Add(new ScannedFile
        {
            FileName = fileName,
            ContentType = contentType,
            ScannedAt = DateTime.UtcNow,
            WasClean = _shouldReturnClean
        });

        return Task.FromResult(CreateScanResult());
    }

    public Task<VirusScanResult> ScanFileAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        _scannedFiles.Add(new ScannedFile
        {
            FileName = fileName,
            FilePath = filePath,
            ScannedAt = DateTime.UtcNow,
            WasClean = _shouldReturnClean
        });

        return Task.FromResult(CreateScanResult());
    }

    public Task<VirusScanResult> QuickScanAsync(string fileName, string contentType, long fileSize)
    {
        _scannedFiles.Add(new ScannedFile
        {
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            ScannedAt = DateTime.UtcNow,
            WasClean = _shouldReturnClean,
            WasQuickScan = true
        });

        return Task.FromResult(CreateScanResult());
    }

    public Task<ScanEngineInfo> GetScanEngineInfoAsync()
    {
        return Task.FromResult(new ScanEngineInfo
        {
            EngineName = "Mock Scanner",
            Version = "1.0.0",
            DefinitionsDate = DateTime.UtcNow,
            IsOperational = true,
            Properties = new Dictionary<string, object>
            {
                ["IsMock"] = true
            }
        });
    }

    public Task<bool> UpdateVirusDefinitionsAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsFileTypeAllowedAsync(string fileName, string contentType)
    {
        return Task.FromResult(_fileTypeAllowed);
    }

    public Task<List<string>> GetBlockedFileExtensionsAsync()
    {
        return Task.FromResult(new List<string>
        {
            ".exe", ".bat", ".cmd", ".msi", ".scr", ".pif", ".com",
            ".js", ".vbs", ".wsf", ".wsh", ".ps1", ".psm1"
        });
    }

    public Task<List<string>> GetAllowedMimeTypesAsync()
    {
        return Task.FromResult(new List<string>
        {
            "text/plain", "text/html", "text/css", "text/javascript",
            "application/pdf", "application/json", "application/xml",
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        });
    }

    private VirusScanResult CreateScanResult()
    {
        if (_shouldReturnClean)
        {
            return new VirusScanResult
            {
                IsClean = true,
                ScanCompleted = true,
                ScanEngine = "Mock Scanner",
                EngineVersion = "1.0.0",
                ScanDate = DateTime.UtcNow,
                ScanDurationMs = 50,
                Threats = new List<ThreatInfo>()
            };
        }

        return new VirusScanResult
        {
            IsClean = false,
            ScanCompleted = true,
            ScanEngine = "Mock Scanner",
            EngineVersion = "1.0.0",
            ScanDate = DateTime.UtcNow,
            ScanDurationMs = 100,
            Threats = new List<ThreatInfo>
            {
                new ThreatInfo
                {
                    ThreatName = _threatName,
                    Severity = _threatSeverity,
                    Description = $"Detected {_threatName} in file",
                    Action = "Blocked"
                }
            }
        };
    }
}

/// <summary>
/// Record of a scanned file for test assertions
/// </summary>
public class ScannedFile
{
    public required string FileName { get; set; }
    public string? FilePath { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool WasClean { get; set; }
    public bool WasQuickScan { get; set; }
}
