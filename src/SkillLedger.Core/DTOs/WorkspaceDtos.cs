using SkillLedger.Core.Enums;

namespace SkillLedger.Core.DTOs
{
    public class WorkspaceDashboardDto
    {
        public Guid WorkspaceId { get; set; }
        public string ProjectTitle { get; set; } = null!;
        public string ProjectDescription { get; set; } = null!;
        public string ClientName { get; set; } = null!;
        public string ProviderName { get; set; } = null!;
        public WorkspaceStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? TimelineData { get; set; }
        public string? MilestoneData { get; set; }
        public string? IntegrationStatus { get; set; }
        public DateTime? LastSyncedAt { get; set; }
    }

    public class CreateWorkspaceRequest
    {
        public Guid ProjectId { get; set; }
        public Guid ProviderId { get; set; }
    }

    // VULN-017 FIX: Replaced object with strongly-typed DTO to prevent JSON deserialization attacks
    public class UpdateTimelineRequest
    {
        public TimelineDataDto TimelineData { get; set; } = null!;
    }

    /// <summary>
    /// Strongly-typed timeline data to prevent deserialization attacks
    /// </summary>
    public class TimelineDataDto
    {
        public List<TimelineEventDto> Events { get; set; } = new();
        public DateTime? LastUpdated { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Individual timeline event
    /// </summary>
    public class TimelineEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = null!; // e.g., "milestone", "meeting", "deliverable"
        public string Status { get; set; } = null!; // e.g., "planned", "completed", "cancelled"
    }

    public class WorkspaceListDto
    {
        public Guid Id { get; set; }
        public string ProjectTitle { get; set; } = null!;
        public string OtherParticipantName { get; set; } = null!;
        public WorkspaceStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivity { get; set; }
        public bool IsClient { get; set; }
    }
}