namespace SkillLedger.Core.DTOs;

/// <summary>
/// Request DTO for milestone approval
/// </summary>
public class ApproveMilestoneRequestDto
{
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Request DTO for milestone revision requests
/// </summary>
public class RequestRevisionsDto
{
    public string ReviewNotes { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for milestone cancellation
/// </summary>
public class CancelMilestoneRequestDto
{
    public string? Reason { get; set; }
}
