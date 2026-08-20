using SkillLedger.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

/// <summary>
/// DTO for user risk score information
/// </summary>
public class UserRiskScoreDto
{
    public Guid UserId { get; set; }

    [Range(0, 100)] // VULN-019 FIX: Validate risk score range (0-100)
    public decimal RiskScore { get; set; }

    public DateTime AssessedAt { get; set; }
}

/// <summary>
/// DTO for gaming risk assessment
/// </summary>
public class GamingRiskAssessmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [Range(0, 100)] // VULN-019 FIX: Validate risk score range (0-100)
    public decimal RiskScore { get; set; }

    public string? RiskFactors { get; set; }
    public string? DetectedPatterns { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string ModelVersion { get; set; } = "1.0";
}

/// <summary>
/// DTO for user behavior metrics
/// </summary>
public class UserBehaviorMetricDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string MetricName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)] // VULN-019 FIX: Prevent negative metric values
    public decimal MetricValue { get; set; }

    public string? CalculationWindow { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool IsAnomaly { get; set; }
}

/// <summary>
/// DTO for user network connections
/// </summary>
public class UserNetworkConnectionDto
{
    public Guid Id { get; set; }
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }
    public string ConnectionType { get; set; } = string.Empty;

    [Range(0, 1)] // VULN-019 FIX: Connection strength typically 0-1
    public decimal ConnectionStrength { get; set; }

    public DateTime DetectedAt { get; set; }
    public bool IsValidated { get; set; }
}

/// <summary>
/// DTO for anti-gaming alerts
/// </summary>
public class AntiGamingAlertDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public AlertStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// DTO for user sanctions
/// </summary>
public class UserSanctionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SanctionType { get; set; } = string.Empty;
    public SanctionSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public Guid? IssuedBy { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public SanctionStatus Status { get; set; }
    public string? AppealNotes { get; set; }
    public DateTime? AppealSubmittedAt { get; set; }
}

/// <summary>
/// DTO for review validation results
/// </summary>
public class ReviewValidationResultDto
{
    public Guid ReviewId { get; set; }
    public bool IsAuthentic { get; set; }
    public DateTime ValidatedAt { get; set; }

    [Range(0, 100)] // VULN-019 FIX: Validate risk score range (0-100)
    public decimal? RiskScore { get; set; }

    public List<string>? RiskFactors { get; set; }
}

/// <summary>
/// DTO for monitoring results
/// </summary>
public class MonitoringResultDto
{
    public bool AllowAction { get; set; }

    [Range(0, 100)] // VULN-019 FIX: Validate risk score range (0-100)
    public decimal RiskScore { get; set; }

    public List<string> DetectedRiskFactors { get; set; } = new();
    public string? BlockReason { get; set; }
    public bool RequiresHumanReview { get; set; }
}

/// <summary>
/// Response for gaming activity report submission
/// </summary>
public class GamingReportResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? ReportId { get; set; }
}