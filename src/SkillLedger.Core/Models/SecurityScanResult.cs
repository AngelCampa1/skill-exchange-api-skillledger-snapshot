using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Models;

public class SecurityScanResult
{
    public bool ScanPassed { get; set; }
    public bool ThreatDetected { get; set; }
    public string? ThreatType { get; set; }
    public string? ScanDetails { get; set; }
    public DateTime ScanTimestamp { get; set; } = DateTime.UtcNow;
    public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.Low;

    // Additional properties for compatibility
    public bool IsSafe { get; set; } = true;
    public IEnumerable<string> ThreatTypes { get; set; } = Enumerable.Empty<string>();
    public string ScanEngine { get; set; } = "DefaultScanner";
}