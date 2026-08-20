using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces
{
    public interface IWorkspaceService
    {
        Task<ProjectWorkspace> CreateWorkspaceAsync(Guid projectId, Guid providerId, Guid? requestedByUserId = null);
        Task<ProjectWorkspace?> GetWorkspaceAsync(Guid workspaceId, Guid userId);
        Task<ProjectWorkspace?> GetWorkspaceByProjectAsync(Guid projectId, Guid userId);
        Task<IEnumerable<WorkspaceListDto>> GetUserWorkspacesAsync(Guid userId);
        Task<WorkspaceDashboardDto> GetWorkspaceDashboardAsync(Guid workspaceId, Guid userId);
        Task<bool> ArchiveWorkspaceAsync(Guid workspaceId, Guid userId);
        // VULN-017 FIX: Changed parameter type from object to TimelineDataDto
        Task<bool> UpdateTimelineAsync(Guid workspaceId, Guid userId, TimelineDataDto timelineData);
        Task<bool> UpdateMilestonesAsync(Guid workspaceId, Guid userId, object milestoneData);
        Task<bool> UpdateIntegrationStatusAsync(Guid workspaceId, string status);
        Task<bool> HasUserAccessAsync(Guid workspaceId, Guid userId);
    }
}
