using SkillLedger.Core.DTOs;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Advanced project search and discovery service
/// </summary>
public interface IProjectSearchService
{
    /// <summary>
    /// Performs advanced project search with filtering, geolocation, and recommendations
    /// </summary>
    /// <param name="searchDto">Advanced search criteria</param>
    /// <returns>Comprehensive search results with aggregations and metadata</returns>
    Task<AdvancedProjectSearchResultDto> AdvancedSearchAsync(AdvancedProjectSearchDto searchDto);

    /// <summary>
    /// Gets recommended projects for a specific user based on their profile and skills
    /// </summary>
    /// <param name="userId">User ID to get recommendations for</param>
    /// <param name="limit">Maximum number of recommendations to return</param>
    /// <param name="excludeProjectIds">Project IDs to exclude from recommendations</param>
    /// <returns>List of recommended projects</returns>
    Task<List<ProjectSummaryDto>> GetRecommendedProjectsAsync(Guid userId, int limit = 10, List<Guid>? excludeProjectIds = null);

    /// <summary>
    /// Gets projects similar to a specified project
    /// </summary>
    /// <param name="projectId">Reference project ID</param>
    /// <param name="limit">Maximum number of similar projects to return</param>
    /// <returns>List of similar projects</returns>
    Task<List<ProjectSummaryDto>> GetSimilarProjectsAsync(Guid projectId, int limit = 10);

    /// <summary>
    /// Performs geolocation-based project search
    /// </summary>
    /// <param name="latitude">Search center latitude</param>
    /// <param name="longitude">Search center longitude</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="additionalFilters">Additional search filters</param>
    /// <returns>Location-based search results</returns>
    Task<List<ProjectSummaryDto>> SearchByLocationAsync(double latitude, double longitude, int radiusKm, AdvancedProjectSearchDto? additionalFilters = null);

    /// <summary>
    /// Gets search aggregations (faceted search data) for the current search context
    /// </summary>
    /// <param name="searchDto">Search criteria to generate aggregations for</param>
    /// <returns>Search aggregations for faceted navigation</returns>
    Task<SearchAggregationsDto> GetSearchAggregationsAsync(AdvancedProjectSearchDto searchDto);

    /// <summary>
    /// Saves a search configuration for a user
    /// </summary>
    /// <param name="userId">User ID who owns the saved search</param>
    /// <param name="createDto">Saved search details</param>
    /// <returns>Saved search result</returns>
    Task<SavedSearchDto> CreateSavedSearchAsync(Guid userId, CreateSavedSearchDto createDto);

    /// <summary>
    /// Gets all saved searches for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="activeOnly">Whether to return only active saved searches</param>
    /// <returns>List of user's saved searches</returns>
    Task<List<SavedSearchDto>> GetSavedSearchesAsync(Guid userId, bool activeOnly = true);

    /// <summary>
    /// Executes a saved search by its ID
    /// </summary>
    /// <param name="savedSearchId">Saved search ID</param>
    /// <param name="userId">User executing the search (must be the owner)</param>
    /// <returns>Search results from the saved search</returns>
    Task<AdvancedProjectSearchResultDto> ExecuteSavedSearchAsync(Guid savedSearchId, Guid userId);

    /// <summary>
    /// Updates a saved search configuration
    /// </summary>
    /// <param name="savedSearchId">Saved search ID to update</param>
    /// <param name="userId">User ID (must be the owner)</param>
    /// <param name="updateDto">Updated search details</param>
    /// <returns>Updated saved search</returns>
    Task<SavedSearchDto> UpdateSavedSearchAsync(Guid savedSearchId, Guid userId, CreateSavedSearchDto updateDto);

    /// <summary>
    /// Deletes a saved search
    /// </summary>
    /// <param name="savedSearchId">Saved search ID to delete</param>
    /// <param name="userId">User ID (must be the owner)</param>
    /// <returns>Deletion result</returns>
    Task<ServiceResponseDto> DeleteSavedSearchAsync(Guid savedSearchId, Guid userId);

    /// <summary>
    /// Updates the full-text search index for a project
    /// </summary>
    /// <param name="projectId">Project ID to update in search index</param>
    /// <returns>Index update result</returns>
    Task UpdateSearchIndexAsync(Guid projectId);

    /// <summary>
    /// Rebuilds the entire project search index (admin operation)
    /// </summary>
    /// <param name="batchSize">Number of projects to process per batch</param>
    /// <returns>Index rebuild result</returns>
    Task<ServiceResponseDto> RebuildSearchIndexAsync(int batchSize = 100);

    /// <summary>
    /// Gets search performance analytics
    /// </summary>
    /// <param name="fromDate">Analytics start date</param>
    /// <param name="toDate">Analytics end date</param>
    /// <returns>Search analytics data</returns>
    Task<object> GetSearchAnalyticsAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Gets trending/popular projects based on search activity
    /// </summary>
    /// <param name="timeRange">Time range for trending analysis (hours)</param>
    /// <param name="limit">Maximum number of trending projects</param>
    /// <returns>List of trending projects</returns>
    Task<List<ProjectSummaryDto>> GetTrendingProjectsAsync(int timeRange = 24, int limit = 10);

    /// <summary>
    /// Validates advanced search criteria and provides optimization suggestions
    /// </summary>
    /// <param name="searchDto">Search criteria to validate</param>
    /// <returns>Validation result with suggestions</returns>
    Task<ServiceResponseDto> ValidateSearchCriteriaAsync(AdvancedProjectSearchDto searchDto);
}