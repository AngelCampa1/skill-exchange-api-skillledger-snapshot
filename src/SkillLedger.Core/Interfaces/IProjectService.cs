using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

public interface IProjectService
{
    /// <summary>
    /// Creates a new project with full validation and moderation queue
    /// </summary>
    /// <param name="createDto">Project creation details</param>
    /// <param name="clientId">ID of the user creating the project</param>
    /// <param name="ipAddress">IP address of the creation request</param>
    /// <returns>Project creation result</returns>
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto createDto, Guid clientId, string ipAddress);

    /// <summary>
    /// Updates an existing project (only editable projects)
    /// </summary>
    /// <param name="projectId">Project ID to update</param>
    /// <param name="updateDto">Update details</param>
    /// <param name="clientId">ID of the user requesting the update</param>
    /// <param name="ipAddress">IP address of the update request</param>
    /// <returns>Project update result</returns>
    Task<ProjectResponseDto> UpdateProjectAsync(Guid projectId, UpdateProjectDto updateDto, Guid clientId, string ipAddress);

    /// <summary>
    /// Saves a project as a draft (allows partial information)
    /// </summary>
    /// <param name="saveDraftDto">Draft project details</param>
    /// <param name="clientId">ID of the user creating the draft</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Draft save result</returns>
    Task<ProjectResponseDto> SaveProjectDraftAsync(SaveDraftProjectDto saveDraftDto, Guid clientId, string ipAddress);

    /// <summary>
    /// Updates an existing draft project
    /// </summary>
    /// <param name="projectId">Project ID to update</param>
    /// <param name="saveDraftDto">Updated draft details</param>
    /// <param name="clientId">ID of the user requesting the update</param>
    /// <param name="ipAddress">IP address of the update request</param>
    /// <returns>Draft update result</returns>
    Task<ProjectResponseDto> UpdateProjectDraftAsync(Guid projectId, SaveDraftProjectDto saveDraftDto, Guid clientId, string ipAddress);

    /// <summary>
    /// Publishes a draft project (submits for moderation)
    /// </summary>
    /// <param name="projectId">Project ID to publish</param>
    /// <param name="clientId">ID of the user requesting to publish</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Publication result</returns>
    Task<ServiceResponseDto> PublishProjectAsync(Guid projectId, Guid clientId, string ipAddress);

    /// <summary>
    /// Gets a project by ID with full details
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="requestingUserId">Optional user ID to check access permissions</param>
    /// <returns>Project details or null</returns>
    Task<ProjectDto?> GetProjectByIdAsync(Guid projectId, Guid? requestingUserId = null);

    /// <summary>
    /// Gets projects for a specific client
    /// </summary>
    /// <param name="clientId">Client user ID</param>
    /// <param name="includeNonPublic">Whether to include draft/non-public projects</param>
    /// <param name="skip">Number of projects to skip (pagination)</param>
    /// <param name="take">Number of projects to take (pagination)</param>
    /// <returns>List of client's projects</returns>
    Task<List<ProjectDto>> GetProjectsByClientAsync(Guid clientId, bool includeNonPublic = false, int skip = 0, int take = 20);

    /// <summary>
    /// Searches for projects with filtering and pagination
    /// </summary>
    /// <param name="searchDto">Search criteria and filters</param>
    /// <returns>List of matching projects</returns>
    Task<List<ProjectSummaryDto>> SearchProjectsAsync(ProjectSearchDto searchDto);

    /// <summary>
    /// Gets the total count of projects matching search criteria
    /// </summary>
    /// <param name="searchDto">Search criteria and filters</param>
    /// <returns>Total count of matching projects</returns>
    Task<int> CountProjectsAsync(ProjectSearchDto searchDto);

    /// <summary>
    /// Deletes a project (only by owner or admin)
    /// </summary>
    /// <param name="projectId">Project ID to delete</param>
    /// <param name="clientId">ID of the user requesting deletion</param>
    /// <param name="ipAddress">IP address of the delete request</param>
    /// <returns>Deletion result</returns>
    Task<ServiceResponseDto> DeleteProjectAsync(Guid projectId, Guid clientId, string ipAddress);

    /// <summary>
    /// Validates project business rules (timeline, budget, etc.)
    /// </summary>
    /// <param name="project">Project to validate</param>
    /// <returns>Validation result with error messages if any</returns>
    Task<ServiceResponseDto> ValidateProjectRulesAsync(Project project);

    /// <summary>
    /// Checks if a user has permission to modify a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="userId">User ID requesting access</param>
    /// <returns>True if the user can modify the project</returns>
    Task<bool> CanUserModifyProjectAsync(Guid projectId, Guid userId);

    /// <summary>
    /// Gets project statistics for analytics
    /// </summary>
    /// <param name="clientId">Optional client ID filter</param>
    /// <returns>Project statistics</returns>
    Task<object> GetProjectStatisticsAsync(Guid? clientId = null);

    /// <summary>
    /// Moderates a project (approve/reject/flag)
    /// </summary>
    /// <param name="projectId">Project ID to moderate</param>
    /// <param name="moderationStatus">New moderation status</param>
    /// <param name="moderatorId">ID of the moderator</param>
    /// <param name="notes">Optional moderation notes</param>
    /// <param name="ipAddress">IP address of the moderation request</param>
    /// <returns>Moderation result</returns>
    Task<ServiceResponseDto> ModerateProjectAsync(Guid projectId, string moderationStatus, Guid moderatorId, string? notes, string ipAddress);
}