using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.Entities;

/// <summary>
/// Pre-aggregated financial reporting data for users
/// Provides optimized analytics and reporting capabilities with historical data
/// </summary>
public class UserCreditReport
{
    public UserCreditReport()
    {
        Id = Guid.NewGuid();
        GeneratedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique identifier for the credit report
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID this report belongs to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Report month in YYYYMM format (e.g., 202509)
    /// Allows for efficient querying and grouping by time periods
    /// </summary>
    [Range(190001, 999912, ErrorMessage = "ReportMonth must be in YYYYMM format")]
    public int ReportMonth { get; set; }

    /// <summary>
    /// Total credits earned during this reporting period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalEarned { get; set; } = 0;

    /// <summary>
    /// Total credits spent during this reporting period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TotalSpent { get; set; } = 0;

    /// <summary>
    /// Net change in credits for this period (TotalEarned - TotalSpent)
    /// </summary>
    public int NetChange => TotalEarned - TotalSpent;

    /// <summary>
    /// Number of transactions during this reporting period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int TransactionCount { get; set; } = 0;

    /// <summary>
    /// Average transaction size for this period
    /// Calculated as (TotalEarned + TotalSpent) / TransactionCount
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal AverageTransactionSize { get; set; } = 0;

    /// <summary>
    /// When this report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When this report was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Breakdown of earnings by transaction type (JSON format)
    /// e.g., {"ProjectPayment": 500, "StartingCredit": 100, "Bonus": 50}
    /// </summary>
    [MaxLength(2000)]
    public string? EarningsByType { get; set; }

    /// <summary>
    /// Breakdown of spending by transaction type (JSON format)
    /// e.g., {"ProjectEscrow": 300, "Transfer": 200}
    /// </summary>
    [MaxLength(2000)]
    public string? SpendingByType { get; set; }

    /// <summary>
    /// Breakdown of project-related earnings (JSON format)
    /// e.g., {"project_id_1": 200, "project_id_2": 300}
    /// </summary>
    [MaxLength(2000)]
    public string? ProjectEarnings { get; set; }

    /// <summary>
    /// Peak balance reached during this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PeakBalance { get; set; } = 0;

    /// <summary>
    /// Lowest balance reached during this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int LowestBalance { get; set; } = 0;

    /// <summary>
    /// Starting balance at the beginning of this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int StartingBalance { get; set; } = 0;

    /// <summary>
    /// Ending balance at the end of this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int EndingBalance { get; set; } = 0;

    /// <summary>
    /// Number of unique projects user earned from this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int UniqueProjectsCount { get; set; } = 0;

    /// <summary>
    /// Number of completed projects during this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int CompletedProjectsCount { get; set; } = 0;

    /// <summary>
    /// Largest single transaction (incoming) during this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int LargestIncomingTransaction { get; set; } = 0;

    /// <summary>
    /// Largest single transaction (outgoing) during this period
    /// </summary>
    [Range(0, int.MaxValue)]
    public int LargestOutgoingTransaction { get; set; } = 0;

    /// <summary>
    /// Version for concurrency control
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Whether this report has been finalized (locked from further changes)
    /// </summary>
    public bool IsFinalized { get; set; } = false;

    /// <summary>
    /// When this report was finalized
    /// </summary>
    public DateTime? FinalizedAt { get; set; }

    #region Business Logic Methods

    /// <summary>
    /// Update the UpdatedAt timestamp
    /// </summary>
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Finalize this report to prevent further changes
    /// </summary>
    public void FinalizeReport()
    {
        IsFinalized = true;
        FinalizedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Check if this report can be modified
    /// </summary>
    public bool CanBeModified => !IsFinalized;

    /// <summary>
    /// Get the month and year from ReportMonth
    /// </summary>
    /// <returns>Tuple of (year, month)</returns>
    public (int Year, int Month) GetMonthYear()
    {
        var year = ReportMonth / 100;
        var month = ReportMonth % 100;
        return (year, month);
    }

    /// <summary>
    /// Get the first day of the report month
    /// </summary>
    /// <returns>DateTime representing the first day of the report month</returns>
    public DateTime GetReportMonthStart()
    {
        var (year, month) = GetMonthYear();
        return new DateTime(year, month, 1);
    }

    /// <summary>
    /// Get the last day of the report month
    /// </summary>
    /// <returns>DateTime representing the last day of the report month</returns>
    public DateTime GetReportMonthEnd()
    {
        var start = GetReportMonthStart();
        return start.AddMonths(1).AddDays(-1);
    }

    /// <summary>
    /// Create a report month value from a DateTime
    /// </summary>
    /// <param name="date">Date to create report month from</param>
    /// <returns>Report month in YYYYMM format</returns>
    public static int CreateReportMonth(DateTime date)
    {
        return date.Year * 100 + date.Month;
    }

    /// <summary>
    /// Calculate average transaction size based on current data
    /// </summary>
    /// <returns>Average transaction size</returns>
    public decimal CalculateAverageTransactionSize()
    {
        if (TransactionCount == 0) return 0;
        return (decimal)(TotalEarned + TotalSpent) / TransactionCount;
    }

    /// <summary>
    /// Update calculated fields based on current data
    /// </summary>
    public void RecalculateFields()
    {
        AverageTransactionSize = CalculateAverageTransactionSize();
        UpdateTimestamp();
    }

    /// <summary>
    /// Validate that the report data is consistent
    /// </summary>
    /// <returns>True if data is consistent</returns>
    public bool ValidateDataConsistency()
    {
        // Check that ending balance equals starting balance plus net change
        var expectedEndingBalance = StartingBalance + NetChange;
        if (EndingBalance != expectedEndingBalance)
            return false;

        // Check that average transaction size is calculated correctly
        var expectedAverage = CalculateAverageTransactionSize();
        if (Math.Abs((double)(AverageTransactionSize - expectedAverage)) > 0.01)
            return false;

        // Check that peak balance is at least as high as ending balance
        if (PeakBalance < Math.Max(StartingBalance, EndingBalance))
            return false;

        // Check that lowest balance is at most as low as the starting balance
        if (LowestBalance > Math.Min(StartingBalance, EndingBalance))
            return false;

        return true;
    }

    #endregion

    #region Static Helper Methods

    /// <summary>
    /// Generate a list of report months between two dates
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of report months in YYYYMM format</returns>
    public static List<int> GenerateReportMonths(DateTime startDate, DateTime endDate)
    {
        var months = new List<int>();
        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var end = new DateTime(endDate.Year, endDate.Month, 1);

        while (current <= end)
        {
            months.Add(CreateReportMonth(current));
            current = current.AddMonths(1);
        }

        return months;
    }

    /// <summary>
    /// Parse report month back to DateTime
    /// </summary>
    /// <param name="reportMonth">Report month in YYYYMM format</param>
    /// <returns>DateTime representing the first day of the month</returns>
    public static DateTime ParseReportMonth(int reportMonth)
    {
        var year = reportMonth / 100;
        var month = reportMonth % 100;
        return new DateTime(year, month, 1);
    }

    #endregion
}