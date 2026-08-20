namespace SkillLedger.Core.Enums;

public enum ProjectStatus
{
    /// <summary>
    /// Project is being created but not yet published
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Project is published and accepting applications
    /// </summary>
    Published = 1,

    /// <summary>
    /// Project has been assigned and work is in progress
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Project has been completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Project was cancelled before completion
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Project was suspended (e.g., for moderation)
    /// </summary>
    Suspended = 5,

    /// <summary>
    /// Project is in dispute between client and provider
    /// </summary>
    Disputed = 6
}

/// <summary>
/// Project visibility levels for discovery
/// </summary>
public enum ProjectVisibility
{
    /// <summary>
    /// Visible to all users in search results
    /// </summary>
    Public = 0,

    /// <summary>
    /// Visible only to users matching specific criteria
    /// </summary>
    Restricted = 1,

    /// <summary>
    /// Only visible to invited users
    /// </summary>
    Private = 2,

    /// <summary>
    /// Hidden from all search results
    /// </summary>
    Hidden = 3
}