using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for provider selection and matching functionality
/// </summary>
public interface IProviderSelectionService
{
    /// <summary>
    /// Create a new provider selection for a project
    /// </summary>
    /// <param name="createDto">Selection creation details</param>
    /// <param name="clientId">ID of the project owner making the selection</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Selection creation result</returns>
    Task<ServiceResponseDto> CreateProviderSelectionAsync(CreateProviderSelectionDto createDto, Guid clientId, string ipAddress);

    /// <summary>
    /// Get provider selection by ID
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="requestingUserId">ID of the user requesting (for authorization)</param>
    /// <returns>Selection details or null</returns>
    Task<ProviderSelectionDto?> GetProviderSelectionByIdAsync(Guid selectionId, Guid requestingUserId);

    /// <summary>
    /// Get provider selection for a specific project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="requestingUserId">ID of the user requesting (for authorization)</param>
    /// <returns>Selection details or null if no selection made</returns>
    Task<ProviderSelectionDto?> GetProjectSelectionAsync(Guid projectId, Guid requestingUserId);

    /// <summary>
    /// Get selection dashboard with ranked applications for a project
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <returns>Selection dashboard with ranked applications</returns>
    Task<SelectionDashboardDto> GetSelectionDashboardAsync(Guid projectId, Guid clientId);

    /// <summary>
    /// Rank and compare applications for a project using automated scoring
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <returns>List of ranked applications with comparison scores</returns>
    Task<List<ApplicationComparisonDto>> RankApplicationsAsync(Guid projectId, Guid clientId);

    /// <summary>
    /// Calculate ranking score for a specific application
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="projectId">Project ID</param>
    /// <returns>Application comparison details with ranking score</returns>
    Task<ApplicationComparisonDto> CalculateApplicationRankingAsync(Guid applicationId, Guid projectId);

    /// <summary>
    /// Calculate ranking score for a specific application after verifying requester access
    /// </summary>
    /// <param name="applicationId">Application ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="requestingUserId">ID of the user requesting the comparison</param>
    /// <param name="isAdmin">Whether the requester has administrative access</param>
    /// <returns>Application comparison details with ranking score</returns>
    Task<ApplicationComparisonDto> CalculateApplicationRankingAsync(Guid applicationId, Guid projectId, Guid requestingUserId, bool isAdmin = false);

    /// <summary>
    /// Get provider work history and reputation summary
    /// </summary>
    /// <param name="providerId">Provider user ID</param>
    /// <returns>Provider history summary</returns>
    Task<ProviderHistorySummaryDto> GetProviderHistorySummaryAsync(Guid providerId);

    /// <summary>
    /// Update provider selection status (e.g., contract signed, work started)
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="newStatus">New status</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Update result</returns>
    Task<ServiceResponseDto> UpdateSelectionStatusAsync(Guid selectionId, ProviderSelectionStatus newStatus, Guid clientId, string ipAddress);

    /// <summary>
    /// Update escrow funding status
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="isFunded">Whether escrow is funded</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Update result</returns>
    Task<ServiceResponseDto> UpdateEscrowStatusAsync(Guid selectionId, bool isFunded, Guid clientId, string ipAddress);

    /// <summary>
    /// Update contract signing status
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="isSigned">Whether contract is signed</param>
    /// <param name="userId">ID of the user (client or provider)</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Update result</returns>
    Task<ServiceResponseDto> UpdateContractStatusAsync(Guid selectionId, bool isSigned, Guid userId, string ipAddress);

    /// <summary>
    /// Cancel a provider selection before work begins
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Cancellation result</returns>
    Task<ServiceResponseDto> CancelSelectionAsync(Guid selectionId, string reason, Guid clientId, string ipAddress);

    /// <summary>
    /// Search and filter provider selections
    /// </summary>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <param name="requestingUserId">ID of the user requesting (for authorization)</param>
    /// <returns>List of matching selections</returns>
    Task<List<ProviderSelectionDto>> SearchSelectionsAsync(ProviderSelectionSearchDto searchDto, Guid requestingUserId);

    /// <summary>
    /// Get selection statistics for a client
    /// </summary>
    /// <param name="clientId">Client user ID</param>
    /// <returns>Selection statistics</returns>
    Task<Dictionary<string, object>> GetClientSelectionStatisticsAsync(Guid clientId);

    /// <summary>
    /// Get selection statistics for a provider
    /// </summary>
    /// <param name="providerId">Provider user ID</param>
    /// <returns>Selection statistics</returns>
    Task<Dictionary<string, object>> GetProviderSelectionStatisticsAsync(Guid providerId);

    /// <summary>
    /// Send selection notification to the chosen provider and rejection notifications to others
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <returns>True if notifications sent successfully</returns>
    Task<bool> SendSelectionNotificationsAsync(Guid selectionId);

    /// <summary>
    /// Initiate escrow setup for a selection
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="clientId">ID of the project owner</param>
    /// <returns>Escrow initiation result</returns>
    Task<ServiceResponseDto> InitiateEscrowAsync(Guid selectionId, Guid clientId);

    /// <summary>
    /// Generate contract terms based on project and application details
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="applicationId">Selected application ID</param>
    /// <returns>Generated contract terms</returns>
    Task<string> GenerateContractTermsAsync(Guid projectId, Guid applicationId);

    /// <summary>
    /// Check if user has permission to access/modify a selection
    /// </summary>
    /// <param name="selectionId">Selection ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>True if user has access</returns>
    Task<bool> HasUserAccessToSelectionAsync(Guid selectionId, Guid userId);

    /// <summary>
    /// Validate selection business rules
    /// </summary>
    /// <param name="selection">Selection to validate</param>
    /// <returns>Validation result</returns>
    Task<ServiceResponseDto> ValidateSelectionRulesAsync(ProviderSelection selection);

    /// <summary>
    /// Check if a project is ready for provider selection
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>True if ready for selection, false otherwise</returns>
    Task<bool> IsProjectReadyForSelectionAsync(Guid projectId);

    /// <summary>
    /// Check if a project is ready for provider selection for an authorized requester.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="requestingUserId">User requesting readiness state</param>
    /// <param name="isAdmin">Whether requester is an administrator</param>
    /// <returns>True if ready for selection and visible to the requester, false otherwise</returns>
    Task<bool> IsProjectReadyForSelectionAsync(Guid projectId, Guid requestingUserId, bool isAdmin = false);

    /// <summary>
    /// Get recommended providers for a project based on skill matching and reputation
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="take">Number of recommendations to return</param>
    /// <returns>List of recommended applications</returns>
    Task<List<ApplicationComparisonDto>> GetRecommendedProvidersAsync(Guid projectId, int take = 5);

    /// <summary>
    /// Get recommended providers for a project after verifying requester access
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="requestingUserId">ID of the user requesting recommendations</param>
    /// <param name="take">Number of recommendations to return</param>
    /// <param name="isAdmin">Whether the requester has administrative access</param>
    /// <returns>List of recommended applications</returns>
    Task<List<ApplicationComparisonDto>> GetRecommendedProvidersAsync(Guid projectId, Guid requestingUserId, int take = 5, bool isAdmin = false);
}
