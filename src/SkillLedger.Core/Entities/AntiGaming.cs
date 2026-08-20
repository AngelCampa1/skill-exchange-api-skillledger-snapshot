using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Anti-gaming alert for suspicious review or reputation activity
/// </summary>
public class AntiGamingAlert
{
    public AntiGamingAlert()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who triggered the alert
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of gaming activity detected
    /// </summary>
    [MaxLength(100)]
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the alert
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Human-readable description of the issue
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON evidence supporting the alert
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Current status of the alert
    /// </summary>
    public AlertStatus Status { get; set; } = AlertStatus.Open;

    /// <summary>
    /// When the alert was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the alert was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Admin who resolved the alert
    /// </summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution notes from admin
    /// </summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to resolving admin
    /// </summary>
    public virtual User? ResolvedByUser { get; set; }
}

/// <summary>
/// User behavior metrics for gaming detection
/// </summary>
public class UserBehaviorMetric
{
    public UserBehaviorMetric()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User being measured
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Name of the metric
    /// </summary>
    [MaxLength(100)]
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Calculated metric value
    /// </summary>
    public decimal MetricValue { get; set; }

    /// <summary>
    /// Time window for calculation
    /// </summary>
    [MaxLength(50)]
    public string? CalculationWindow { get; set; }

    /// <summary>
    /// When the metric was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this metric indicates anomalous behavior
    /// </summary>
    public bool IsAnomaly { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Network connections between users for graph analysis
/// </summary>
public class UserNetworkConnection
{
    public UserNetworkConnection()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// First user in the connection
    /// </summary>
    public Guid User1Id { get; set; }

    /// <summary>
    /// Second user in the connection
    /// </summary>
    public Guid User2Id { get; set; }

    /// <summary>
    /// Type of connection detected
    /// </summary>
    [MaxLength(100)]
    public string ConnectionType { get; set; } = string.Empty;

    /// <summary>
    /// Strength of the connection (0-1 scale)
    /// </summary>
    public decimal ConnectionStrength { get; set; }

    /// <summary>
    /// Number of interactions between users
    /// </summary>
    public int InteractionCount { get; set; }

    /// <summary>
    /// When the last interaction occurred
    /// </summary>
    public DateTime? LastInteractionAt { get; set; }

    /// <summary>
    /// JSON metadata for additional connection properties
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the connection was first detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the connection has been validated by admin
    /// </summary>
    public bool IsValidated { get; set; }

    /// <summary>
    /// Navigation property to first user
    /// </summary>
    public virtual User User1 { get; set; } = null!;

    /// <summary>
    /// Navigation property to second user
    /// </summary>
    public virtual User User2 { get; set; } = null!;
}

/// <summary>
/// Sanctions applied to users for gaming violations
/// </summary>
public class UserSanction
{
    public UserSanction()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User receiving the sanction
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of sanction applied
    /// </summary>
    [MaxLength(100)]
    public string SanctionType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the sanction
    /// </summary>
    public SanctionSeverity Severity { get; set; }

    /// <summary>
    /// Description of the violation and sanction
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON evidence supporting the sanction
    /// </summary>
    public string? Evidence { get; set; }

    /// <summary>
    /// Admin who issued the sanction
    /// </summary>
    public Guid? IssuedBy { get; set; }

    /// <summary>
    /// When the sanction was issued
    /// </summary>
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the sanction expires (if temporary)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Current status of the sanction
    /// </summary>
    public SanctionStatus Status { get; set; } = SanctionStatus.Active;

    /// <summary>
    /// Appeal notes from the user
    /// </summary>
    [MaxLength(2000)]
    public string? AppealNotes { get; set; }

    /// <summary>
    /// When the appeal was submitted
    /// </summary>
    public DateTime? AppealSubmittedAt { get; set; }

    /// <summary>
    /// Navigation property to sanctioned user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to issuing admin
    /// </summary>
    public virtual User? IssuedByUser { get; set; }
}

/// <summary>
/// Gaming risk assessment for a user
/// </summary>
public class GamingRiskAssessment
{
    public GamingRiskAssessment()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User being assessed
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Overall risk score (0-1 scale)
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// JSON array of detected risk factors
    /// </summary>
    public string? RiskFactors { get; set; }

    /// <summary>
    /// JSON array of detected gaming patterns
    /// </summary>
    public string? DetectedPatterns { get; set; }

    /// <summary>
    /// When the assessment was performed
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Version of the analysis model used
    /// </summary>
    [MaxLength(20)]
    public string ModelVersion { get; set; } = "1.0";

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Alert status values
/// </summary>
public enum AlertStatus
{
    Open = 1,
    Investigating = 2,
    Resolved = 3,
    FalsePositive = 4
}

/// <summary>
/// Sanction severity levels
/// </summary>
public enum SanctionSeverity
{
    Warning = 1,
    Temporary = 2,
    Permanent = 3,
    AccountSuspension = 4
}

/// <summary>
/// Sanction status values
/// </summary>
public enum SanctionStatus
{
    Active = 1,
    Appealed = 2,
    Overturned = 3,
    Expired = 4
}