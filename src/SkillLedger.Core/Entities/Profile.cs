using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

public class Profile
{
    public Profile()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Unique identifier for the profile
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Associated user ID (foreign key)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    [MaxLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [MaxLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// SEO-friendly URL slug for public profile
    /// </summary>
    [MaxLength(100)]
    public string? ProfileSlug { get; set; }

    /// <summary>
    /// Professional title/headline
    /// </summary>
    [MaxLength(150)]
    public string? Title { get; set; }

    /// <summary>
    /// Company or organization
    /// </summary>
    [MaxLength(100)]
    public string? Company { get; set; }

    /// <summary>
    /// Location (city, state/country)
    /// </summary>
    [MaxLength(100)]
    public string? Location { get; set; }

    /// <summary>
    /// Professional bio/summary
    /// </summary>
    [MaxLength(2000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Short professional summary (legacy field for compatibility)
    /// </summary>
    [MaxLength(500)]
    public string? Summary { get; set; }

    /// <summary>
    /// Professional website URL
    /// </summary>
    [MaxLength(200)]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// LinkedIn profile URL
    /// </summary>
    [MaxLength(200)]
    public string? LinkedInUrl { get; set; }

    /// <summary>
    /// GitHub profile URL
    /// </summary>
    [MaxLength(200)]
    public string? GitHubUrl { get; set; }

    /// <summary>
    /// Twitter profile URL
    /// </summary>
    [MaxLength(200)]
    public string? TwitterUrl { get; set; }

    /// <summary>
    /// Profile avatar URL
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Time zone identifier
    /// </summary>
    [MaxLength(50)]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Profile visibility setting
    /// </summary>
    public ProfileVisibility Visibility { get; set; } = ProfileVisibility.Public;

    /// <summary>
    /// Whether the profile is visible to other users (legacy field for compatibility)
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Whether profile is complete enough to be featured
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Number of profile views
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// When profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Generate SEO-friendly slug from name and title
    /// </summary>
    public string GenerateSlug()
    {
        var baseName = $"{FirstName} {LastName} {Title}".Trim();
        if (string.IsNullOrEmpty(baseName))
            return UserId.ToString("N")[..8];

        // Convert to SEO-friendly slug
        var slug = baseName.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Replace("\"", "");

        // Remove consecutive hyphens and limit length
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        slug = slug.Trim('-');
        return slug.Length > 80 ? slug[..80].TrimEnd('-') : slug;
    }

    /// <summary>
    /// Check if profile has minimum required fields for completeness
    /// </summary>
    public bool CheckCompleteness()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(Title);
    }
}

/// <summary>
/// Profile visibility levels
/// </summary>
public enum ProfileVisibility
{
    /// <summary>
    /// Profile is completely private
    /// </summary>
    Private = 0,

    /// <summary>
    /// Profile visible only to verified users
    /// </summary>
    VerifiedUsersOnly = 1,

    /// <summary>
    /// Profile visible to all platform users
    /// </summary>
    Internal = 2,

    /// <summary>
    /// Profile visible publicly and indexed by search engines
    /// </summary>
    Public = 3
}