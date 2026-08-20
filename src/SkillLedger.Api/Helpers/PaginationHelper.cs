namespace SkillLedger.Api.Helpers;

/// <summary>
/// BUG-MEDIUM-002 FIX: Centralized pagination validation to prevent DoS and excessive queries
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Maximum allowed page size to prevent excessive database queries
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Default page size when not specified
    /// </summary>
    public const int DefaultPageSize = 20;

    /// <summary>
    /// Validates and normalizes pagination parameters
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Tuple of (skip, take) values for database queries</returns>
    public static (int skip, int take) ValidatePagination(int page, int pageSize)
    {
        // Ensure page is at least 1
        if (page < 1)
            page = 1;

        // Ensure pageSize is positive and use default if invalid
        if (pageSize < 1)
            pageSize = DefaultPageSize;

        // Cap pageSize at maximum to prevent excessive queries
        if (pageSize > MaxPageSize)
            pageSize = MaxPageSize;

        // Calculate skip and take values
        int skip = (page - 1) * pageSize;
        int take = pageSize;

        return (skip, take);
    }

    /// <summary>
    /// Validates and normalizes skip/take parameters (direct pagination)
    /// </summary>
    /// <param name="skip">Number of items to skip</param>
    /// <param name="take">Number of items to take</param>
    /// <returns>Validated (skip, take) tuple</returns>
    public static (int skip, int take) ValidateSkipTake(int skip, int take)
    {
        // Ensure skip is non-negative
        if (skip < 0)
            skip = 0;

        // Ensure take is positive and use default if invalid
        if (take < 1)
            take = DefaultPageSize;

        // Cap take at maximum
        if (take > MaxPageSize)
            take = MaxPageSize;

        return (skip, take);
    }
}
