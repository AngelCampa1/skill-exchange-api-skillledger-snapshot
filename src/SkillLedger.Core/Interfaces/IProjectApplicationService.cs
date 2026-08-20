using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;

namespace SkillLedger.Core.Interfaces;

public interface IProjectApplicationService
{
    /// <summary>
    /// Submit a new project application
    /// </summary>
    /// <param name="createDto">Application creation details</param>
    /// <param name="providerId">ID of the service provider applying</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Application creation result</returns>
    Task<ServiceResponseDto> SubmitApplicationAsync(CreateProjectApplicationDto createDto, Guid providerId, string ipAddress);

    /// <summary>
    /// Get a project application by ID
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="requestingUserId">ID of the user requesting (for authorization)</param>
    /// <returns>Application details or null</returns>
    Task<ProjectApplicationDto?> GetApplicationByIdAsync(Guid applicationId, Guid requestingUserId);

    /// <summary>
    /// Get applications for a specific project (client view)
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <returns>List of applications for the project</returns>
    Task<ApplicationSearchResultDto> GetProjectApplicationsAsync(Guid projectId, Guid clientId, ApplicationSearchDto searchDto);

    /// <summary>
    /// Get applications submitted by a provider (provider view)
    /// </summary>
    /// <param name="providerId">Provider user ID</param>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <returns>List of provider's applications</returns>
    Task<ApplicationSearchResultDto> GetProviderApplicationsAsync(Guid providerId, ApplicationSearchDto searchDto);

    /// <summary>
    /// Update application status (by client - accept, reject, etc.)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="updateDto">Status update details</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Update result</returns>
    Task<ServiceResponseDto> UpdateApplicationStatusAsync(Guid applicationId, UpdateApplicationStatusDto updateDto, Guid clientId, string ipAddress);

    /// <summary>
    /// Withdraw an application (by provider)
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="providerId">ID of the service provider</param>
    /// <param name="reason">Optional withdrawal reason</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Withdrawal result</returns>
    Task<ServiceResponseDto> WithdrawApplicationAsync(Guid applicationId, Guid providerId, string? reason, string ipAddress);

    /// <summary>
    /// Calculate automatic skill matching score for an application
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="providerId">Provider user ID</param>
    /// <returns>Skill match score (0.0 to 1.0)</returns>
    Task<decimal> CalculateSkillMatchScoreAsync(Guid projectId, Guid providerId);

    /// <summary>
    /// Get application statistics for a provider
    /// </summary>
    /// <param name="providerId">Provider user ID</param>
    /// <returns>Application statistics</returns>
    Task<ApplicationStatisticsDto> GetProviderApplicationStatisticsAsync(Guid providerId);

    /// <summary>
    /// Get application statistics for a client's projects
    /// </summary>
    /// <param name="clientId">Client user ID</param>
    /// <returns>Application statistics for client's projects</returns>
    Task<ApplicationStatisticsDto> GetClientApplicationStatisticsAsync(Guid clientId);

    /// <summary>
    /// Check if a provider can apply to a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="providerId">Provider user ID</param>
    /// <returns>True if provider can apply, false otherwise</returns>
    Task<bool> CanProviderApplyToProjectAsync(Guid projectId, Guid providerId);

    /// <summary>
    /// Get recommended projects for a provider based on skills and preferences
    /// </summary>
    /// <param name="providerId">Provider user ID</param>
    /// <param name="take">Number of recommendations to return</param>
    /// <returns>List of recommended projects</returns>
    Task<List<ProjectSummaryDto>> GetRecommendedProjectsForProviderAsync(Guid providerId, int take = 10);

    /// <summary>
    /// Expire old applications that haven't been reviewed
    /// </summary>
    /// <param name="expiredAfterDays">Applications older than this many days will be expired</param>
    /// <returns>Number of applications expired</returns>
    Task<int> ExpireOldApplicationsAsync(int expiredAfterDays = 30);

    /// <summary>
    /// Send application status update notifications
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="newStatus">New status</param>
    /// <param name="feedback">Optional feedback</param>
    /// <returns>True if notification sent successfully</returns>
    Task<bool> SendApplicationStatusNotificationAsync(Guid applicationId, string newStatus, string? feedback);

    /// <summary>
    /// Validate application business rules
    /// </summary>
    /// <param name="application">Application to validate</param>
    /// <returns>Validation result</returns>
    Task<ServiceResponseDto> ValidateApplicationRulesAsync(ProjectApplication application);

    /// <summary>
    /// Check if user has permission to access/modify an application
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>True if user has access</returns>
    Task<bool> HasUserAccessToApplicationAsync(Guid applicationId, Guid userId);
}