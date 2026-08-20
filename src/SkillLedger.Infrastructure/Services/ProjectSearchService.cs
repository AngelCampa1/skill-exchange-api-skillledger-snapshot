using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Advanced project search and discovery service implementation
/// </summary>
public class ProjectSearchService : IProjectSearchService
{
    private readonly SkillLedgerDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ProjectSearchService> _logger;

    public ProjectSearchService(
        SkillLedgerDbContext context,
        IDistributedCache cache,
        ILogger<ProjectSearchService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AdvancedProjectSearchResultDto> AdvancedSearchAsync(AdvancedProjectSearchDto searchDto)
    {
        try
        {
            var startTime = DateTime.UtcNow;


            _logger.LogInformation("Executing advanced project search with criteria: {SearchCriteria}",
                JsonSerializer.Serialize(searchDto, new JsonSerializerOptions { WriteIndented = false }));

            // Start with base query for published projects
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            // (10 projects × 5 deliverables × 3 skills = 150 rows instead of 10)
            var query = _context.Projects
                .Include(p => p.Client)
                .ThenInclude(c => c.Profile)
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                .ThenInclude(ps => ps.Skill)
                .AsSplitQuery()
                .AsQueryable();

            // Apply published filter
            if (searchDto.PublishedOnly)
            {
                query = query.Where(p => p.Status == ProjectStatus.Published &&
                                        p.ModerationStatus == ModerationStatus.Approved &&
                                        p.Visibility == ProjectVisibility.Public);
            }

            // Apply text search
            if (!string.IsNullOrWhiteSpace(searchDto.Query))
            {
                var searchTerms = searchDto.Query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    query = query.Where(p =>
                        (p.SearchText != null && p.SearchText.ToLower().Contains(term)) ||
                        p.Title.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term));
                }
            }

            // Apply status filter
            if (searchDto.Status != null && searchDto.Status.Any())
            {
                // Safely parse status strings, ignoring invalid values
                var statusEnums = new List<ProjectStatus>();
                foreach (var statusStr in searchDto.Status)
                {
                    if (Enum.TryParse<ProjectStatus>(statusStr, ignoreCase: true, out var status))
                    {
                        statusEnums.Add(status);
                    }
                }

                if (statusEnums.Any())
                {
                    query = query.Where(p => statusEnums.Contains(p.Status));
                }
            }

            // Apply budget range filter
            if (searchDto.MinBudget.HasValue)
            {
                query = query.Where(p => p.CreditBudget >= searchDto.MinBudget.Value);
            }
            if (searchDto.MaxBudget.HasValue)
            {
                query = query.Where(p => p.CreditBudget <= searchDto.MaxBudget.Value);
            }

            // Apply skill filter
            if (searchDto.SkillIds != null && searchDto.SkillIds.Any())
            {
                if (searchDto.SkillMatch == SkillMatchStrategy.All)
                {
                    // Project must have ALL specified skills
                    foreach (var skillId in searchDto.SkillIds)
                    {
                        query = query.Where(p => p.ProjectSkills.Any(ps => ps.SkillId == skillId));
                    }
                }
                else
                {
                    // Project must have ANY of the specified skills
                    query = query.Where(p => p.ProjectSkills.Any(ps => searchDto.SkillIds.Contains(ps.SkillId)));
                }
            }

            // Apply geolocation filter
            if (searchDto.Latitude.HasValue && searchDto.Longitude.HasValue && searchDto.RadiusKm.HasValue)
            {
                // Use simplified distance calculation for now
                var lat = searchDto.Latitude.Value;
                var lon = searchDto.Longitude.Value;
                var radiusKm = searchDto.RadiusKm.Value;

                query = query.Where(p => p.LocationLatitude.HasValue && p.LocationLongitude.HasValue &&
                    (6371 * Math.Acos(Math.Cos(Math.PI * lat / 180) *
                     Math.Cos(Math.PI * p.LocationLatitude.Value / 180) *
                     Math.Cos(Math.PI * p.LocationLongitude.Value / 180 - Math.PI * lon / 180) +
                     Math.Sin(Math.PI * lat / 180) *
                     Math.Sin(Math.PI * p.LocationLatitude.Value / 180))) <= radiusKm);
            }

            // Apply client location filter
            if (!string.IsNullOrWhiteSpace(searchDto.ClientLocation))
            {
                var location = searchDto.ClientLocation.ToLower();
                query = query.Where(p =>
                    (!string.IsNullOrWhiteSpace(p.LocationCity) && p.LocationCity.ToLower().Contains(location)) ||
                    (!string.IsNullOrWhiteSpace(p.LocationState) && p.LocationState.ToLower().Contains(location)) ||
                    (!string.IsNullOrWhiteSpace(p.LocationCountry) && p.LocationCountry.ToLower().Contains(location)));
            }

            // Apply duration filter
            if (searchDto.MinDurationDays.HasValue || searchDto.MaxDurationDays.HasValue)
            {
                query = query.Where(p => p.StartDate.HasValue && p.EndDate.HasValue);

                // Check if we're using in-memory provider for testing
                var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

                if (searchDto.MinDurationDays.HasValue)
                {
                    if (isInMemory)
                    {
                        // For in-memory testing, use TimeSpan calculation
                        query = query.Where(p => p.StartDate.HasValue && p.EndDate.HasValue &&
                                                 (p.EndDate.Value - p.StartDate.Value).Days >= searchDto.MinDurationDays.Value);
                    }
                    else
                    {
                        // For PostgreSQL, use date subtraction
                        query = query.Where(p => p.StartDate.HasValue && p.EndDate.HasValue &&
                                                 (p.EndDate.Value - p.StartDate.Value).Days >= searchDto.MinDurationDays.Value);
                    }
                }
                if (searchDto.MaxDurationDays.HasValue)
                {
                    if (isInMemory)
                    {
                        // For in-memory testing, use TimeSpan calculation
                        query = query.Where(p => p.StartDate.HasValue && p.EndDate.HasValue &&
                                                 (p.EndDate.Value - p.StartDate.Value).Days <= searchDto.MaxDurationDays.Value);
                    }
                    else
                    {
                        // For PostgreSQL, use date subtraction
                        query = query.Where(p => p.StartDate.HasValue && p.EndDate.HasValue &&
                                                 (p.EndDate.Value - p.StartDate.Value).Days <= searchDto.MaxDurationDays.Value);
                    }
                }
            }

            // Apply date range filters
            if (searchDto.CreatedFrom.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= searchDto.CreatedFrom.Value);
            }
            if (searchDto.CreatedTo.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= searchDto.CreatedTo.Value);
            }

            // Apply remote work filter
            if (searchDto.RemoteWorkOnly == true)
            {
                query = query.Where(p => p.IsRemoteWork);
            }

            // Apply client exclusion filter
            if (searchDto.ExcludeClients != null && searchDto.ExcludeClients.Any())
            {
                query = query.Where(p => !searchDto.ExcludeClients.Contains(p.ClientId));
            }

            // Apply deliverable count filter
            if (searchDto.MinDeliverables.HasValue)
            {
                query = query.Where(p => p.Deliverables.Count >= searchDto.MinDeliverables.Value);
            }
            if (searchDto.MaxDeliverables.HasValue)
            {
                query = query.Where(p => p.Deliverables.Count <= searchDto.MaxDeliverables.Value);
            }

            // Apply sorting
            query = ApplySorting(query, searchDto);

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var projects = await query
                .Skip(searchDto.Skip)
                .Take(searchDto.Take)
                .ToListAsync();

            // Map to DTOs - using only valid ProjectSummaryDto properties
            var projectSummaries = projects.Select(p => new ProjectSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                ShortDescription = p.Description.Length > 100 ? p.Description.Substring(0, 100) + "..." : p.Description,
                CreditBudget = p.CreditBudget,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                EndDate = p.EndDate,
                // BUG-NEW-004 FIX: Ensure DisplayName is never null
                Client = new UserSummaryDto
                {
                    Id = p.Client.Id,
                    DisplayName = !string.IsNullOrEmpty(p.Client.Profile?.FirstName) || !string.IsNullOrEmpty(p.Client.Profile?.LastName)
                        ? $"{p.Client.Profile?.FirstName} {p.Client.Profile?.LastName}".Trim()
                        : p.Client.Email ?? "Unknown User",
                    Title = p.Client.Profile?.Title,
                    Company = p.Client.Profile?.Company,
                    AvatarUrl = p.Client.Profile?.AvatarUrl
                },
                RequiredSkillNames = p.ProjectSkills?.Select(ps => ps.Skill?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
                DeliverableCount = p.Deliverables?.Count ?? 0,
                DurationDisplay = CalculateDurationDisplay(p.StartDate, p.EndDate)
            }).ToList();

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.Take);
            var currentPage = (searchDto.Skip / searchDto.Take) + 1;

            var result = new AdvancedProjectSearchResultDto
            {
                Projects = projectSummaries,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = searchDto.Take,
                HasNextPage = currentPage < totalPages,
                HasPreviousPage = currentPage > 1,
                Metadata = new SearchMetadataDto
                {
                    ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    FromCache = false,
                    AppliedFilters = BuildAppliedFiltersList(searchDto)
                }
            };

            // Add aggregations if requested
            if (searchDto.IncludeAggregations)
            {
                result.Aggregations = await GetSearchAggregationsAsync(searchDto);
            }

            _logger.LogInformation("Advanced search completed in {ElapsedMs}ms, returned {ProjectCount} projects",
                result.Metadata.ExecutionTimeMs, result.Projects.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing advanced project search");
            throw;
        }
    }

    public async Task<List<ProjectSummaryDto>> GetRecommendedProjectsAsync(Guid userId, int limit = 10, List<Guid>? excludeProjectIds = null)
    {
        try
        {
            _logger.LogInformation("Getting recommended projects for user {UserId}", userId);

            // Get user's skills and recent activity
            var userSkills = await _context.UserSkills
                .Where(us => us.UserId == userId && us.IsVisible)
                .Select(us => us.SkillId)
                .ToListAsync();

            // Base query for recommended projects
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var query = _context.Projects
                .Include(p => p.Client)
                .ThenInclude(c => c.Profile)
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                .ThenInclude(ps => ps.Skill)
                .AsSplitQuery()
                .Where(p => p.Status == ProjectStatus.Published &&
                           p.ModerationStatus == ModerationStatus.Approved &&
                           p.Visibility == ProjectVisibility.Public)
                .AsQueryable();

            // Exclude specified projects
            if (excludeProjectIds != null && excludeProjectIds.Any())
            {
                query = query.Where(p => !excludeProjectIds.Contains(p.Id));
            }

            // Filter projects that match user skills
            if (userSkills.Any())
            {
                query = query.Where(p => p.ProjectSkills.Any(ps => userSkills.Contains(ps.SkillId)));
            }

            // Order by relevance (featured first, then by creation date)
            var projects = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return projects.Select(MapToProjectSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommended projects for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ProjectSummaryDto>> GetSimilarProjectsAsync(Guid projectId, int limit = 10)
    {
        try
        {
            _logger.LogInformation("Getting similar projects to {ProjectId}", projectId);

            // Get reference project
            var referenceProject = await _context.Projects
                .Include(p => p.ProjectSkills)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (referenceProject == null)
            {
                return new List<ProjectSummaryDto>();
            }

            var referenceSkillIds = referenceProject.ProjectSkills.Select(ps => ps.SkillId).ToList();

            // Find projects with similar skills
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var similarProjects = await _context.Projects
                .Include(p => p.Client)
                .ThenInclude(c => c.Profile)
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                .ThenInclude(ps => ps.Skill)
                .AsSplitQuery()
                .Where(p => p.Id != projectId &&
                           p.Status == ProjectStatus.Published &&
                           p.ModerationStatus == ModerationStatus.Approved &&
                           p.Visibility == ProjectVisibility.Public &&
                           p.ProjectSkills.Any(ps => referenceSkillIds.Contains(ps.SkillId)))
                .OrderByDescending(p => p.ProjectSkills.Count(ps => referenceSkillIds.Contains(ps.SkillId)))
                .ThenByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return similarProjects.Select(MapToProjectSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar projects to {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<List<ProjectSummaryDto>> SearchByLocationAsync(double latitude, double longitude, int radiusKm, AdvancedProjectSearchDto? additionalFilters = null)
    {
        try
        {
            _logger.LogInformation("Searching projects by location ({Lat}, {Lon}) within {Radius}km",
                latitude, longitude, radiusKm);

            var searchDto = additionalFilters ?? new AdvancedProjectSearchDto();
            searchDto.Latitude = latitude;
            searchDto.Longitude = longitude;
            searchDto.RadiusKm = radiusKm;

            var result = await AdvancedSearchAsync(searchDto);
            return result.Projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching projects by location");
            throw;
        }
    }

    public async Task<SearchAggregationsDto> GetSearchAggregationsAsync(AdvancedProjectSearchDto searchDto)
    {
        try
        {
            // PERFORMANCE FIX: Build base query with AsNoTracking for read-only aggregations
            var query = _context.Projects
                .AsNoTracking()
                .Include(p => p.ProjectSkills)
                .ThenInclude(ps => ps.Skill)
                .AsQueryable();

            if (searchDto.PublishedOnly)
            {
                query = query.Where(p => p.Status == ProjectStatus.Published &&
                                        p.ModerationStatus == ModerationStatus.Approved);
            }

            // Apply same filters as main search (simplified for aggregations)
            if (!string.IsNullOrWhiteSpace(searchDto.Query))
            {
                query = query.Where(p => p.SearchText != null && p.SearchText.Contains(searchDto.Query) ||
                                        p.Title.Contains(searchDto.Query) ||
                                        p.Description.Contains(searchDto.Query));
            }

            var aggregations = new SearchAggregationsDto();

            // PERFORMANCE FIX: Skills aggregation - execute at database level instead of loading all projects
            var skillCounts = await _context.ProjectSkills
                .AsNoTracking()
                .Where(ps => query.Select(p => p.Id).Contains(ps.ProjectId))
                .GroupBy(ps => new { ps.Skill.Id, ps.Skill.Name })
                .Select(g => new FacetDto
                {
                    Key = g.Key.Id.ToString(),
                    DisplayValue = g.Key.Name,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(20)
                .ToListAsync();
            aggregations.Skills = skillCounts;

            // PERFORMANCE FIX: Budget ranges aggregation - execute counts at database level instead of in-memory
            var budgetRanges = new List<FacetDto>
            {
                new() { Key = "50-500", DisplayValue = "$50 - $500", Count = await query.CountAsync(p => p.CreditBudget >= 50 && p.CreditBudget < 500) },
                new() { Key = "500-1000", DisplayValue = "$500 - $1000", Count = await query.CountAsync(p => p.CreditBudget >= 500 && p.CreditBudget < 1000) },
                new() { Key = "1000-2500", DisplayValue = "$1000 - $2500", Count = await query.CountAsync(p => p.CreditBudget >= 1000 && p.CreditBudget < 2500) },
                new() { Key = "2500-5000", DisplayValue = "$2500 - $5000", Count = await query.CountAsync(p => p.CreditBudget >= 2500 && p.CreditBudget <= 5000) }
            };
            aggregations.BudgetRanges = budgetRanges.Where(b => b.Count > 0).ToList();

            // PERFORMANCE FIX: Status aggregation - execute at database level
            var statusCounts = await query
                .GroupBy(p => p.Status)
                .Select(g => new FacetDto
                {
                    Key = g.Key.ToString(),
                    DisplayValue = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();
            aggregations.Status = statusCounts;

            return aggregations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating search aggregations");
            throw;
        }
    }

    public async Task<SavedSearchDto> CreateSavedSearchAsync(Guid userId, CreateSavedSearchDto createDto)
    {
        try
        {
            _logger.LogInformation("Creating saved search for user {UserId}: {SearchName}", userId, createDto.Name);

            var savedSearch = new SavedSearch
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = createDto.Name,
                Description = createDto.Description,
                SearchCriteria = JsonSerializer.Serialize(createDto.SearchCriteria),
                NotificationsEnabled = createDto.NotificationsEnabled,
                NotificationFrequency = createDto.NotificationFrequency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.SavedSearches.Add(savedSearch);
            await _context.SaveChangesAsync();

            return MapToSavedSearchDto(savedSearch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating saved search for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<SavedSearchDto>> GetSavedSearchesAsync(Guid userId, bool activeOnly = true)
    {
        try
        {
            var query = _context.SavedSearches
                .Where(s => s.UserId == userId);

            if (activeOnly)
            {
                query = query.Where(s => s.IsActive);
            }

            var savedSearches = await query
                .OrderByDescending(s => s.LastUsedAt ?? s.CreatedAt)
                .ToListAsync();

            return savedSearches.Select(MapToSavedSearchDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saved searches for user {UserId}", userId);
            throw;
        }
    }

    public async Task<AdvancedProjectSearchResultDto> ExecuteSavedSearchAsync(Guid savedSearchId, Guid userId)
    {
        try
        {
            var savedSearch = await _context.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == savedSearchId && s.UserId == userId);

            if (savedSearch == null)
            {
                throw new ArgumentException("Saved search not found or access denied");
            }

            // Update usage statistics
            savedSearch.UsageCount++;
            savedSearch.LastUsedAt = DateTime.UtcNow;
            savedSearch.UpdatedAt = DateTime.UtcNow;

            // Deserialize and execute the search
            var searchCriteria = JsonSerializer.Deserialize<AdvancedProjectSearchDto>(savedSearch.SearchCriteria);
            if (searchCriteria == null)
            {
                throw new InvalidOperationException("Invalid saved search criteria");
            }

            await _context.SaveChangesAsync();

            return await AdvancedSearchAsync(searchCriteria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing saved search {SavedSearchId} for user {UserId}", savedSearchId, userId);
            throw;
        }
    }

    public async Task<SavedSearchDto> UpdateSavedSearchAsync(Guid savedSearchId, Guid userId, CreateSavedSearchDto updateDto)
    {
        try
        {
            var savedSearch = await _context.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == savedSearchId && s.UserId == userId);

            if (savedSearch == null)
            {
                throw new ArgumentException("Saved search not found or access denied");
            }

            savedSearch.Name = updateDto.Name;
            savedSearch.Description = updateDto.Description;
            savedSearch.SearchCriteria = JsonSerializer.Serialize(updateDto.SearchCriteria);
            savedSearch.NotificationsEnabled = updateDto.NotificationsEnabled;
            savedSearch.NotificationFrequency = updateDto.NotificationFrequency;
            savedSearch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToSavedSearchDto(savedSearch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating saved search {SavedSearchId} for user {UserId}", savedSearchId, userId);
            throw;
        }
    }

    public async Task<ServiceResponseDto> DeleteSavedSearchAsync(Guid savedSearchId, Guid userId)
    {
        try
        {
            var savedSearch = await _context.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == savedSearchId && s.UserId == userId);

            if (savedSearch == null)
            {
                return new ServiceResponseDto
                {
                    Success = false,
                    Message = "Saved search not found or access denied"
                };
            }

            _context.SavedSearches.Remove(savedSearch);
            await _context.SaveChangesAsync();

            return new ServiceResponseDto
            {
                Success = true,
                Message = "Saved search deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved search {SavedSearchId} for user {UserId}", savedSearchId, userId);
            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while deleting the saved search"
            };
        }
    }

    public async Task UpdateSearchIndexAsync(Guid projectId)
    {
        try
        {
            var project = await _context.Projects
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                .ThenInclude(ps => ps.Skill)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return;

            // Build search text
            var searchText = $"{project.Title} {project.Description}";

            if (project.Deliverables.Any())
            {
                searchText += " " + string.Join(" ", project.Deliverables.Select(d => d.Description));
            }

            if (project.ProjectSkills.Any())
            {
                searchText += " " + string.Join(" ", project.ProjectSkills.Select(ps => ps.Skill.Name));
            }

            project.SearchText = searchText;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated search index for project {ProjectId}", projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating search index for project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<ServiceResponseDto> RebuildSearchIndexAsync(int batchSize = 100)
    {
        try
        {
            _logger.LogInformation("Starting search index rebuild with batch size {BatchSize}", batchSize);

            var totalProjects = await _context.Projects.CountAsync();
            var processedCount = 0;

            for (int skip = 0; skip < totalProjects; skip += batchSize)
            {
                var projectIds = await _context.Projects
                    .Skip(skip)
                    .Take(batchSize)
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var projectId in projectIds)
                {
                    await UpdateSearchIndexAsync(projectId);
                }

                processedCount += projectIds.Count;
                _logger.LogInformation("Processed {ProcessedCount}/{TotalProjects} projects", processedCount, totalProjects);
            }

            return new ServiceResponseDto
            {
                Success = true,
                Message = $"Successfully rebuilt search index for {processedCount} projects"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding search index");
            return new ServiceResponseDto
            {
                Success = false,
                Message = "An error occurred while rebuilding the search index"
            };
        }
    }

    public async Task<object> GetSearchAnalyticsAsync(DateTime fromDate, DateTime toDate)
    {
        // Placeholder implementation - would integrate with actual analytics system
        return new
        {
            TotalSearches = await _context.SavedSearches.CountAsync(s => s.LastUsedAt >= fromDate && s.LastUsedAt <= toDate),
            PopularTerms = new[] { "Development", "Design", "API" },
            AverageResultsPerSearch = 15.6
        };
    }

    public async Task<List<ProjectSummaryDto>> GetTrendingProjectsAsync(int timeRange = 24, int limit = 10)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-timeRange);

            // For now, return recently created featured projects
            // In a real implementation, this would consider search activity, views, applications, etc.
            // PERFORMANCE FIX: Use AsSplitQuery to prevent cartesian explosion
            var trendingProjects = await _context.Projects
                .Include(p => p.Client)
                .ThenInclude(c => c.Profile)
                .Include(p => p.Deliverables)
                .Include(p => p.ProjectSkills)
                .ThenInclude(ps => ps.Skill)
                .AsSplitQuery()
                .Where(p => p.Status == ProjectStatus.Published &&
                           p.ModerationStatus == ModerationStatus.Approved &&
                           p.Visibility == ProjectVisibility.Public &&
                           p.CreatedAt >= cutoffTime)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return trendingProjects.Select(MapToProjectSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending projects");
            throw;
        }
    }

    public async Task<ServiceResponseDto> ValidateSearchCriteriaAsync(AdvancedProjectSearchDto searchDto)
    {
        var errors = new List<string>();

        // Validate geolocation
        if (searchDto.Latitude.HasValue && (searchDto.Latitude < -90 || searchDto.Latitude > 90))
        {
            errors.Add("Latitude must be between -90 and 90 degrees");
        }

        if (searchDto.Longitude.HasValue && (searchDto.Longitude < -180 || searchDto.Longitude > 180))
        {
            errors.Add("Longitude must be between -180 and 180 degrees");
        }

        if (searchDto.RadiusKm.HasValue && (searchDto.RadiusKm < 1 || searchDto.RadiusKm > 10000))
        {
            errors.Add("Search radius must be between 1 and 10000 kilometers");
        }

        // Validate budget range
        if (searchDto.MinBudget.HasValue && searchDto.MaxBudget.HasValue &&
            searchDto.MinBudget > searchDto.MaxBudget)
        {
            errors.Add("Minimum budget cannot be greater than maximum budget");
        }

        // Validate date ranges
        if (searchDto.CreatedFrom.HasValue && searchDto.CreatedTo.HasValue &&
            searchDto.CreatedFrom > searchDto.CreatedTo)
        {
            errors.Add("Created from date cannot be later than created to date");
        }

        return new ServiceResponseDto
        {
            Success = errors.Count == 0,
            Message = errors.Count > 0 ? string.Join(", ", errors) : "Validation passed"
        };
    }

    #region Private Helper Methods

    private IQueryable<Project> ApplySorting(IQueryable<Project> query, AdvancedProjectSearchDto searchDto)
    {
        // Handle multiple sort criteria
        if (searchDto.SortBy != null && searchDto.SortBy.Any())
        {
            IOrderedQueryable<Project>? orderedQuery = null;

            foreach (var sortCriteria in searchDto.SortBy.OrderByDescending(s => s.Weight))
            {
                var isAsc = sortCriteria.Direction.ToLower() == "asc";

                if (orderedQuery == null)
                {
                    orderedQuery = sortCriteria.Field.ToLower() switch
                    {
                        "budget" => isAsc ? query.OrderBy(p => p.CreditBudget) : query.OrderByDescending(p => p.CreditBudget),
                        "created" => isAsc ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                        "enddate" => isAsc ? query.OrderBy(p => p.EndDate) : query.OrderByDescending(p => p.EndDate),
                        "title" => isAsc ? query.OrderBy(p => p.Title) : query.OrderByDescending(p => p.Title),
                        _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
                    };
                }
                else
                {
                    orderedQuery = sortCriteria.Field.ToLower() switch
                    {
                        "budget" => isAsc ? orderedQuery.ThenBy(p => p.CreditBudget) : orderedQuery.ThenByDescending(p => p.CreditBudget),
                        "created" => isAsc ? orderedQuery.ThenBy(p => p.CreatedAt) : orderedQuery.ThenByDescending(p => p.CreatedAt),
                        "enddate" => isAsc ? orderedQuery.ThenBy(p => p.EndDate) : orderedQuery.ThenByDescending(p => p.EndDate),
                        "title" => isAsc ? orderedQuery.ThenBy(p => p.Title) : orderedQuery.ThenByDescending(p => p.Title),
                        _ => orderedQuery
                    };
                }
            }

            return orderedQuery ?? query;
        }

        // Default sorting
        return searchDto.DefaultSort.ToLower() switch
        {
            "budget" => query.OrderByDescending(p => p.CreditBudget),
            "created" => query.OrderByDescending(p => p.CreatedAt),
            "title" => query.OrderBy(p => p.Title),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt) // relevance
        };
    }

    private List<string> BuildAppliedFiltersList(AdvancedProjectSearchDto searchDto)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            // Sanitize sensitive information from search query before including in response
            var sanitizedQuery = SanitizeSensitiveInfo(searchDto.Query);
            filters.Add($"Text search: {sanitizedQuery}");
        }

        if (searchDto.MinBudget.HasValue || searchDto.MaxBudget.HasValue)
            filters.Add($"Budget: ${searchDto.MinBudget ?? 0} - ${searchDto.MaxBudget ?? int.MaxValue}");

        if (searchDto.SkillIds != null && searchDto.SkillIds.Any())
            filters.Add($"Skills: {searchDto.SkillIds.Count} skills ({searchDto.SkillMatch})");

        if (searchDto.Latitude.HasValue && searchDto.Longitude.HasValue)
            filters.Add($"Location: {searchDto.RadiusKm}km radius");

        if (!string.IsNullOrWhiteSpace(searchDto.ClientLocation))
        {
            var sanitizedLocation = SanitizeSensitiveInfo(searchDto.ClientLocation);
            filters.Add($"Client location: {sanitizedLocation}");
        }

        if (searchDto.RemoteWorkOnly == true)
            filters.Add("Remote work only");

        return filters;
    }

    private ProjectSummaryDto MapToProjectSummaryDto(Project project)
    {
        return new ProjectSummaryDto
        {
            Id = project.Id,
            Title = project.Title,
            ShortDescription = project.Description.Length > 200
                ? project.Description.Substring(0, 200) + "..."
                : project.Description,
            Client = new UserSummaryDto
            {
                Id = project.Client.Id,
                DisplayName = GetDisplayName(project.Client),
                Title = project.Client.Profile?.Title,
                Company = project.Client.Profile?.Company,
                AvatarUrl = project.Client.Profile?.AvatarUrl
            },
            Status = project.Status.ToString(),
            CreditBudget = project.CreditBudget,
            CreatedAt = project.CreatedAt,
            EndDate = project.EndDate,
            DeliverableCount = project.Deliverables.Count,
            RequiredSkillNames = project.ProjectSkills.Select(ps => ps.Skill.Name).ToList(),
            DurationDisplay = CalculateDurationDisplay(project.StartDate, project.EndDate)
        };
    }

    private SavedSearchDto MapToSavedSearchDto(SavedSearch savedSearch)
    {
        return new SavedSearchDto
        {
            Id = savedSearch.Id,
            UserId = savedSearch.UserId,
            Name = savedSearch.Name,
            Description = savedSearch.Description,
            SearchCriteria = savedSearch.SearchCriteria,
            NotificationsEnabled = savedSearch.NotificationsEnabled,
            NotificationFrequency = savedSearch.NotificationFrequency,
            CreatedAt = savedSearch.CreatedAt,
            LastUsedAt = savedSearch.LastUsedAt,
            UsageCount = savedSearch.UsageCount,
            IsActive = savedSearch.IsActive
        };
    }

    private static string GetDisplayName(User user)
    {
        if (user.Profile != null && !string.IsNullOrWhiteSpace(user.Profile.FirstName) && !string.IsNullOrWhiteSpace(user.Profile.LastName))
        {
            return $"{user.Profile.FirstName.Trim()} {user.Profile.LastName.Trim()}";
        }

        // BUG-NEW-004 FIX: Ensure non-null return value
        return user.UserName ?? user.Email ?? "Unknown User";
    }

    private static string? CalculateDurationDisplay(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue) return null;

        var days = (endDate.Value - startDate.Value).Days;

        if (days <= 7) return $"{days} day{(days == 1 ? "" : "s")}";
        if (days <= 30) return $"{days / 7} week{(days / 7 == 1 ? "" : "s")}";
        if (days <= 365) return $"{days / 30} month{(days / 30 == 1 ? "" : "s")}";

        return $"{days / 365} year{(days / 365 == 1 ? "" : "s")}";
    }

    /// <summary>
    /// Sanitizes sensitive information from search queries before including in response metadata
    /// </summary>
    /// <param name="query">The search query to sanitize</param>
    /// <returns>Sanitized query string</returns>
    private static string SanitizeSensitiveInfo(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        // High-risk patterns that should redact the entire query
        var highRiskPatterns = new[]
        {
            @"passwd",
            @"shadow",
            @"etc/",
            @"\.\./",
            @"\.\.\\",
            @"system32",
            @"windows/",
        };

        // Check for high-risk patterns first - redact entire query if found
        foreach (var pattern in highRiskPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(query, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return "[QUERY_BLOCKED_FOR_SECURITY]";
            }
        }

        var sensitivePatterns = new[]
        {
            @"\bSSN\b", // Social Security Number
            @"\d{3}-\d{2}-\d{4}", // SSN format 123-45-6789
            @"\b[A-Z]{2}\d{6}[A-Z]\b", // UK National Insurance format
            @"\bpassword\b",
            @"\bsecret\b",
            @"\btoken\b",
            @"\bapi[_-]?key\b",
            @"<script[^>]*>.*?</script>", // Script injection
            @"<[^>]*script[^>]*>", // Script tag variations
            @"javascript:", // JavaScript protocol
            @"on\w+\s*=", // HTML event handlers (onclick, onload, etc.)
            @"%2e%2e%2f", // URL encoded path traversal
            @"%2e%2e\\", // URL encoded Windows path traversal
        };

        var sanitized = query;
        foreach (var pattern in sensitivePatterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, pattern, "[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    #endregion
}