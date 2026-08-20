using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Infrastructure.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly SkillLedgerDbContext _context;
        private readonly ILogger<WorkspaceService> _logger;
        private readonly IAuditLogService _auditLogService;

        public WorkspaceService(
            SkillLedgerDbContext context,
            ILogger<WorkspaceService> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        public async Task<ProjectWorkspace> CreateWorkspaceAsync(Guid projectId, Guid providerId, Guid? requestedByUserId = null)
        {
            _logger.LogInformation("Creating workspace for project {ProjectId} with provider {ProviderId}",
                projectId, providerId);

            // Get project with client information
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                throw new ArgumentException($"Project not found with ID: {projectId}");
            }

            if (requestedByUserId.HasValue && project.ClientId != requestedByUserId.Value)
            {
                throw new UnauthorizedAccessException("Only the project client can create a workspace");
            }

            if (project.ProviderId != providerId)
            {
                throw new UnauthorizedAccessException("Workspace provider must match the project's assigned provider");
            }

            // Check if workspace already exists for this project
            var existingWorkspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(pw => pw.ProjectId == projectId);

            if (existingWorkspace != null)
            {
                throw new InvalidOperationException($"Workspace already exists for this project: {projectId}");
            }

            // Create new workspace
            var workspace = new ProjectWorkspace
            {
                ProjectId = projectId,
                ClientId = project.ClientId,
                ProviderId = providerId,
                Status = Core.Enums.WorkspaceStatus.Active,
                IntegrationStatus = "initialized"
            };

            _context.ProjectWorkspaces.Add(workspace);
            await _context.SaveChangesAsync();

            // Log workspace creation
            // LOW-PRIORITY FIX: IP address not available in service layer
            // For complete IP tracking, consider passing from controller or using IHttpContextAccessor
            await _auditLogService.LogEventAsync(
                workspace.ClientId,
                "WorkspaceCreated",
                "0.0.0.0", // Service layer - IP not available (use controller for IP tracking)
                null, // User agent
                true, // Success
                JsonConvert.SerializeObject(new { WorkspaceId = workspace.Id, ProjectId = projectId, ProviderId = providerId }));

            _logger.LogInformation("Workspace {WorkspaceId} created successfully for project {ProjectId}",
                workspace.Id, projectId);

            return workspace;
        }

        public async Task<ProjectWorkspace?> GetWorkspaceAsync(Guid workspaceId, Guid userId)
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var workspace = await _context.ProjectWorkspaces
                .Include(pw => pw.Project)
                .Include(pw => pw.Client)
                .Include(pw => pw.Provider)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            if (workspace == null || !workspace.IsAccessibleBy(userId))
            {
                return null;
            }

            return workspace;
        }

        public async Task<ProjectWorkspace?> GetWorkspaceByProjectAsync(Guid projectId, Guid userId)
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var workspace = await _context.ProjectWorkspaces
                .Include(pw => pw.Project)
                .Include(pw => pw.Client)
                .Include(pw => pw.Provider)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pw => pw.ProjectId == projectId);

            if (workspace == null || !workspace.IsAccessibleBy(userId))
            {
                return null;
            }

            return workspace;
        }

        public async Task<IEnumerable<WorkspaceListDto>> GetUserWorkspacesAsync(Guid userId)
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var workspaces = await _context.ProjectWorkspaces
                .Include(pw => pw.Project)
                .Include(pw => pw.Client)
                .Include(pw => pw.Provider)
                .AsSplitQuery()
                .Where(pw => pw.ClientId == userId || pw.ProviderId == userId)
                .OrderByDescending(pw => pw.LastSyncedAt ?? pw.CreatedAt)
                .ToListAsync();

            return workspaces.Select(pw => new WorkspaceListDto
            {
                Id = pw.Id,
                ProjectTitle = pw.Project.Title,
                OtherParticipantName = pw.ClientId == userId ?
                    pw.Provider.Email ?? "Provider" :
                    pw.Client.Email ?? "Client",
                Status = pw.Status,
                CreatedAt = pw.CreatedAt,
                LastActivity = pw.LastSyncedAt,
                IsClient = pw.ClientId == userId
            });
        }

        public async Task<WorkspaceDashboardDto> GetWorkspaceDashboardAsync(Guid workspaceId, Guid userId)
        {
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var workspace = await _context.ProjectWorkspaces
                .Include(pw => pw.Project)
                .Include(pw => pw.Client)
                .Include(pw => pw.Provider)
                .AsSplitQuery()
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            if (workspace == null || !workspace.IsAccessibleBy(userId))
            {
                throw new UnauthorizedAccessException("Access denied to workspace");
            }

            return new WorkspaceDashboardDto
            {
                WorkspaceId = workspace.Id,
                ProjectTitle = workspace.Project.Title,
                ProjectDescription = workspace.Project.Description,
                ClientName = workspace.Client.Email ?? "Client",
                ProviderName = workspace.Provider.Email ?? "Provider",
                Status = workspace.Status,
                CreatedAt = workspace.CreatedAt,
                ArchivedAt = workspace.ArchivedAt,
                TimelineData = workspace.TimelineData,
                MilestoneData = workspace.MilestoneData,
                IntegrationStatus = workspace.IntegrationStatus,
                LastSyncedAt = workspace.LastSyncedAt
            };
        }

        public async Task<bool> ArchiveWorkspaceAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            if (workspace == null || !workspace.IsAccessibleBy(userId))
            {
                return false;
            }

            workspace.ArchiveWorkspace();
            await _context.SaveChangesAsync();

            // Log workspace archival
            await _auditLogService.LogEventAsync(
                userId,
                "WorkspaceArchived",
                "0.0.0.0", // Service layer - IP not available
                null, // User agent
                true, // Success
                JsonConvert.SerializeObject(new { WorkspaceId = workspaceId }));

            _logger.LogInformation("Workspace {WorkspaceId} archived by user {UserId}",
                workspaceId, userId);

            return true;
        }

        // VULN-017 FIX: Changed parameter from object to TimelineDataDto for type safety
        public async Task<bool> UpdateTimelineAsync(Guid workspaceId, Guid userId, TimelineDataDto timelineData)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            if (workspace == null || !workspace.IsAccessibleBy(userId))
            {
                return false;
            }

            // Now serializing strongly-typed DTO instead of arbitrary object
            workspace.TimelineData = JsonConvert.SerializeObject(timelineData);
            workspace.LastSyncedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log timeline update
            await _auditLogService.LogEventAsync(
                userId,
                "TimelineUpdated",
                "0.0.0.0", // Service layer - IP not available
                null, // User agent
                true, // Success
                JsonConvert.SerializeObject(new { WorkspaceId = workspaceId }));

            return true;
        }

        public async Task<bool> UpdateMilestonesAsync(Guid workspaceId, Guid userId, object milestoneData)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            if (workspace == null || !workspace.IsAccessibleBy(userId))
            {
                return false;
            }

            workspace.MilestoneData = JsonConvert.SerializeObject(milestoneData);
            workspace.LastSyncedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log milestone update
            await _auditLogService.LogEventAsync(
                userId,
                "MilestonesUpdated",
                "0.0.0.0", // Service layer - IP not available
                null, // User agent
                true, // Success
                JsonConvert.SerializeObject(new { WorkspaceId = workspaceId }));

            return true;
        }

        public async Task<bool> UpdateIntegrationStatusAsync(Guid workspaceId, string status)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            if (workspace == null)
            {
                return false;
            }

            workspace.IntegrationStatus = status;
            workspace.LastSyncedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Integration status updated for workspace {WorkspaceId}: {Status}",
                workspaceId, status);

            return true;
        }

        public async Task<bool> HasUserAccessAsync(Guid workspaceId, Guid userId)
        {
            var workspace = await _context.ProjectWorkspaces
                .FirstOrDefaultAsync(pw => pw.Id == workspaceId);

            return workspace?.IsAccessibleBy(userId) == true;
        }
    }
}
