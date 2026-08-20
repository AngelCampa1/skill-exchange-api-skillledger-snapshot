namespace SkillLedger.Core.DTOs.TestData;

/// <summary>
/// Results of a database seeding operation
/// </summary>
public class SeedResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    // Entity counts
    public int UsersCreated { get; set; }
    public int ProfilesCreated { get; set; }
    public int SkillsCreated { get; set; }
    public int ProjectsCreated { get; set; }
    public int WalletsCreated { get; set; }
    public int TransactionsCreated { get; set; }
    public int EscrowsCreated { get; set; }
    public int EscrowAccountsCreated { get; set; } // Alias for EscrowsCreated
    public int ApplicationsCreated { get; set; }
    public int WorkspacesCreated { get; set; }
    public int MessagesCreated { get; set; }
    public int DocumentsCreated { get; set; }
    public int ReviewsCreated { get; set; }
    public int ReputationScoresCreated { get; set; }
    public int AuditLogsCreated { get; set; }

    // Performance metrics
    public long ExecutionTimeMs { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public static SeedResult CreateSuccess(string message)
    {
        return new SeedResult
        {
            Success = true,
            Message = message,
            CompletedAt = DateTime.UtcNow
        };
    }

    public static SeedResult CreateFailure(string message, string? errorDetails = null)
    {
        return new SeedResult
        {
            Success = false,
            Message = message,
            ErrorMessage = errorDetails,
            CompletedAt = DateTime.UtcNow
        };
    }
}
