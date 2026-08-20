using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SkillLedger.Api.Controllers;

/// <summary>
/// Controller for project search functionality
/// </summary>
[ApiController]
[Route("api/project-search")]
public class ProjectSearchController : ControllerBase
{
    private readonly IProjectSearchService _projectSearchService;
    private readonly ILogger<ProjectSearchController> _logger;
    private readonly SkillLedgerDbContext _context;

    public ProjectSearchController(
        IProjectSearchService projectSearchService,
        ILogger<ProjectSearchController> logger,
        SkillLedgerDbContext context)
    {
        _projectSearchService = projectSearchService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Advanced project search with filters and sorting
    /// </summary>
    [HttpPost("advanced")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("ProjectSearchPolicy")]
    public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedProjectSearchDto searchDto)
    {
        try
        {
            // Check for model validation errors
            if (!ModelState.IsValid)
            {
                // Filter out null ModelState values to avoid NullReferenceException
                var errors = ModelState
                    .Where(x => x.Value != null)
                    .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage)); // Safe after null check
                return BadRequest(new { message = "Validation failed", errors = errors });
            }
            // Validate budget range
            if (searchDto.MinBudget.HasValue && searchDto.MaxBudget.HasValue &&
                searchDto.MinBudget > searchDto.MaxBudget)
            {
                return BadRequest(new { message = "MinBudget cannot be greater than MaxBudget" });
            }

            // Validate skill IDs exist
            if (searchDto.SkillIds != null && searchDto.SkillIds.Any())
            {
                var existingSkillIds = await _context.Skills
                    .Where(s => searchDto.SkillIds.Contains(s.Id))
                    .Select(s => s.Id)
                    .ToListAsync();

                var invalidSkillIds = searchDto.SkillIds.Except(existingSkillIds).ToList();
                if (invalidSkillIds.Any())
                {
                    return BadRequest(new { message = $"Invalid skill IDs: {string.Join(", ", invalidSkillIds)}" });
                }
            }

            // Security: Anonymous users can only see published projects
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                searchDto.PublishedOnly = true;
            }

            var result = await _projectSearchService.AdvancedSearchAsync(searchDto);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing advanced project search: {ErrorMessage}", ex.Message);

            // Include error details for debugging (production logs will capture this)
            var errorResponse = new
            {
                message = "An error occurred while searching projects",
                error = ex.Message,
                type = ex.GetType().Name
            };

            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Search projects by location
    /// </summary>
    [HttpPost("location")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchByLocation(
        [FromBody] LocationSearchDto locationSearchDto)
    {
        try
        {
            var result = await _projectSearchService.SearchByLocationAsync(
                locationSearchDto.Latitude,
                locationSearchDto.Longitude,
                (int)locationSearchDto.RadiusKm);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing location-based project search");
            return StatusCode(500, new { message = "An error occurred while searching projects by location" });
        }
    }

    /// <summary>
    /// Get saved searches for authenticated user
    /// </summary>
    [HttpGet("saved")]
    [Authorize]
    public async Task<IActionResult> GetSavedSearches()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var savedSearches = await _projectSearchService.GetSavedSearchesAsync(userId);
            return Ok(savedSearches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saved searches for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving saved searches" });
        }
    }

    /// <summary>
    /// Create a saved search for authenticated user
    /// </summary>
    [HttpPost("saved")]
    [Authorize]
    public async Task<IActionResult> CreateSavedSearch([FromBody] CreateSavedSearchDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var result = await _projectSearchService.CreateSavedSearchAsync(userId, createDto);
            return Created($"/api/project-search/saved/{result.Id}", result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating saved search for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while creating the saved search" });
        }
    }

    /// <summary>
    /// Execute a saved search
    /// </summary>
    [HttpPost("saved/{id}/execute")]
    [Authorize]
    public async Task<IActionResult> ExecuteSavedSearch(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var result = await _projectSearchService.ExecuteSavedSearchAsync(userId, id);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You don't have access to this saved search");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing saved search {SearchId} for user {UserId}", id, userId);
            return StatusCode(500, new { message = "An error occurred while executing the saved search" });
        }
    }

    /// <summary>
    /// Update a saved search
    /// </summary>
    [HttpPut("saved/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateSavedSearch(Guid id, [FromBody] CreateSavedSearchDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            var result = await _projectSearchService.UpdateSavedSearchAsync(id, userId, updateDto);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You don't have access to this saved search");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating saved search {SearchId} for user {UserId}", id, userId);
            return StatusCode(500, new { message = "An error occurred while updating the saved search" });
        }
    }

    /// <summary>
    /// Delete a saved search
    /// </summary>
    [HttpDelete("saved/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteSavedSearch(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User authentication required" });
        }

        try
        {
            await _projectSearchService.DeleteSavedSearchAsync(userId, id);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You don't have access to this saved search");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved search {SearchId} for user {UserId}", id, userId);
            return StatusCode(500, new { message = "An error occurred while deleting the saved search" });
        }
    }
}

/// <summary>
/// DTO for location-based search
/// </summary>
public class LocationSearchDto
{
    [Required]
    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Required]
    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    [Required]
    [Range(1, 10000)]
    public double RadiusKm { get; set; }
}
