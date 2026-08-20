namespace SkillLedger.Core.Models;

public class VirusThreat
{
    public string ThreatName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
}