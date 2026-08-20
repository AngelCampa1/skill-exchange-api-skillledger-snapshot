using SkillLedger.Core.Enums;
using SkillLedger.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SkillLedger.Core.DTOs;

#region Report Request DTOs

/// <summary>
/// Request for generating credit summary reports
/// </summary>
public class CreditReportRequest
{
    /// <summary>
    /// User ID to generate report for (optional for admin users)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Report period type (monthly, quarterly, annual)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PeriodType must be specified")]
    public ReportPeriodType PeriodType { get; set; }

    /// <summary>
    /// Start date for the report
    /// </summary>
    [Required]
    [DateTimeRange("1900-01-01", "2100-12-31", ErrorMessage = "StartDate must be specified")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for the report
    /// </summary>
    [Required]
    [DateTimeRange("1900-01-01", "2100-12-31", ErrorMessage = "EndDate must be specified")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether to include transaction details
    /// </summary>
    public bool IncludeTransactionDetails { get; set; } = false;

    /// <summary>
    /// Whether to include project breakdown
    /// </summary>
    public bool IncludeProjectBreakdown { get; set; } = true;

    /// <summary>
    /// Filter by specific transaction types
    /// </summary>
    public List<CreditTransactionType>? TransactionTypeFilter { get; set; }
}

/// <summary>
/// Request for exporting financial data
/// </summary>
public class FinancialExportRequest
{
    /// <summary>
    /// User ID to export data for
    /// </summary>
    [Required]
    [Range(typeof(Guid), "00000001-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff", ErrorMessage = "UserId must be specified")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Export format (CSV, PDF, JSON, XML)
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Format must be specified")]
    public ExportFormat Format { get; set; }

    /// <summary>
    /// Start date for export
    /// </summary>
    [Required]
    [DateTimeRange("1900-01-01", "2100-12-31", ErrorMessage = "StartDate must be specified")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for export
    /// </summary>
    [Required]
    [DateTimeRange("1900-01-01", "2100-12-31", ErrorMessage = "EndDate must be specified")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether to include personal information (for privacy control)
    /// </summary>
    public bool IncludePersonalInfo { get; set; } = false;

    /// <summary>
    /// Currency format for display
    /// </summary>
    public string CurrencyFormat { get; set; } = "USD";
}

/// <summary>
/// Request for real-time analytics data
/// </summary>
public class AnalyticsRequest
{
    /// <summary>
    /// User ID to get analytics for
    /// </summary>
    [Required]
    [Range(typeof(Guid), "00000001-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff", ErrorMessage = "UserId must be specified")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Time window for analytics (days)
    /// </summary>
    [Range(1, 365)]
    public int TimeWindowDays { get; set; } = 30;

    /// <summary>
    /// Whether to include spending patterns
    /// </summary>
    public bool IncludeSpendingPatterns { get; set; } = true;

    /// <summary>
    /// Whether to include earning trends
    /// </summary>
    public bool IncludeEarningTrends { get; set; } = true;

    /// <summary>
    /// Whether to include goal tracking
    /// </summary>
    public bool IncludeGoalTracking { get; set; } = true;
}

/// <summary>
/// Request for budget tracking setup
/// </summary>
public class BudgetTrackingRequest
{
    /// <summary>
    /// User ID to set budget for
    /// </summary>
    [Required]
    [Range(typeof(Guid), "00000001-0000-0000-0000-000000000000", "ffffffff-ffff-ffff-ffff-ffffffffffff", ErrorMessage = "UserId must be specified")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Monthly spending budget
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MonthlySpendingBudget { get; set; }

    /// <summary>
    /// Monthly earning goal
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MonthlyEarningGoal { get; set; }

    /// <summary>
    /// Project completion goal per month
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ProjectCompletionGoal { get; set; }

    /// <summary>
    /// Alert thresholds (percentages)
    /// </summary>
    public BudgetAlertSettings AlertSettings { get; set; } = new();
}

#endregion

#region Report Response DTOs

/// <summary>
/// Credit summary report response
/// </summary>
public class CreditSummaryReport
{
    /// <summary>
    /// User ID the report is for
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Report period information
    /// </summary>
    public ReportPeriodInfo Period { get; set; } = new();

    /// <summary>
    /// Overall financial summary
    /// </summary>
    public FinancialSummary Summary { get; set; } = new();

    /// <summary>
    /// Breakdown by transaction category
    /// </summary>
    public List<TransactionCategoryBreakdown> CategoryBreakdowns { get; set; } = new();

    /// <summary>
    /// Project-related earnings breakdown
    /// </summary>
    public List<ProjectEarningsBreakdown> ProjectBreakdowns { get; set; } = new();

    /// <summary>
    /// Monthly/period trend data
    /// </summary>
    public List<PeriodTrendData> TrendData { get; set; } = new();

    /// <summary>
    /// Transaction details (if requested)
    /// </summary>
    public List<TransactionSummary>? TransactionDetails { get; set; }

    /// <summary>
    /// When the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Report generation metadata
    /// </summary>
    public ReportMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Real-time analytics data
/// </summary>
public class AnalyticsData
{
    /// <summary>
    /// User ID the analytics are for
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current balance information
    /// </summary>
    public BalanceAnalytics CurrentBalance { get; set; } = new();

    /// <summary>
    /// Spending analytics
    /// </summary>
    public SpendingAnalytics Spending { get; set; } = new();

    /// <summary>
    /// Earning analytics
    /// </summary>
    public EarningAnalytics Earnings { get; set; } = new();

    /// <summary>
    /// Goal tracking data
    /// </summary>
    public GoalTrackingData GoalTracking { get; set; } = new();

    /// <summary>
    /// Activity insights
    /// </summary>
    public ActivityInsights Insights { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = new();

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Financial export result
/// </summary>
public class FinancialExportResult
{
    /// <summary>
    /// Success status
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Export file content (base64 encoded for binary formats)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Content type/MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Suggested filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Number of records included
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Error message if export failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Export metadata
    /// </summary>
    public ExportMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Dashboard data for user insights
/// </summary>
public class UserDashboardData
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current wallet summary
    /// </summary>
    public WalletDashboardSummary Wallet { get; set; } = new();

    /// <summary>
    /// Recent activity summary
    /// </summary>
    public RecentActivitySummary RecentActivity { get; set; } = new();

    /// <summary>
    /// Monthly performance
    /// </summary>
    public MonthlyPerformance MonthlyStats { get; set; } = new();

    /// <summary>
    /// Goal progress
    /// </summary>
    public GoalProgress Goals { get; set; } = new();

    /// <summary>
    /// Trend indicators
    /// </summary>
    public TrendIndicators Trends { get; set; } = new();

    /// <summary>
    /// Alerts and notifications
    /// </summary>
    public List<DashboardAlert> Alerts { get; set; } = new();

    /// <summary>
    /// Quick actions available
    /// </summary>
    public List<QuickAction> QuickActions { get; set; } = new();
}

#endregion

#region Supporting Data Classes

/// <summary>
/// Report period information
/// </summary>
public class ReportPeriodInfo
{
    public ReportPeriodType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int DayCount { get; set; }
}

/// <summary>
/// Financial summary data
/// </summary>
public class FinancialSummary
{
    public int StartingBalance { get; set; }
    public int EndingBalance { get; set; }
    public int TotalEarned { get; set; }
    public int TotalSpent { get; set; }
    public int NetChange => TotalEarned - TotalSpent;
    public int TransactionCount { get; set; }
    public decimal AverageTransactionSize { get; set; }
    public int PeakBalance { get; set; }
    public int LowestBalance { get; set; }
    public int LargestSingleEarning { get; set; }
    public int LargestSingleExpense { get; set; }
}

/// <summary>
/// Transaction category breakdown
/// </summary>
public class TransactionCategoryBreakdown
{
    public CreditTransactionType Category { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public bool IsIncoming { get; set; }
}

/// <summary>
/// Project earnings breakdown
/// </summary>
public class ProjectEarningsBreakdown
{
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public int TotalEarned { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? ProjectCompletedAt { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public int HoursWorked { get; set; }
}

/// <summary>
/// Period trend data
/// </summary>
public class PeriodTrendData
{
    public int Period { get; set; } // YYYYMM format
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int Earnings { get; set; }
    public int Spending { get; set; }
    public int NetChange => Earnings - Spending;
    public int TransactionCount { get; set; }
    public int ProjectsCompleted { get; set; }
    public decimal GrowthRate { get; set; }
}

/// <summary>
/// Transaction summary for reports
/// </summary>
public class TransactionSummary
{
    public Guid TransactionId { get; set; }
    public CreditTransactionType Type { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsIncoming { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
}

/// <summary>
/// Balance analytics
/// </summary>
public class BalanceAnalytics
{
    public int CurrentBalance { get; set; }
    public int AvailableBalance { get; set; }
    public int PendingBalance { get; set; }
    public decimal BalanceChangePercentage { get; set; }
    public string TrendDirection { get; set; } = string.Empty; // "up", "down", "stable"
    public int DaysOfSpendingRemaining { get; set; }
}

/// <summary>
/// Spending analytics
/// </summary>
public class SpendingAnalytics
{
    public int TotalSpent { get; set; }
    public decimal DailyAverageSpending { get; set; }
    public Dictionary<string, int> SpendingByCategory { get; set; } = new();
    public List<SpendingTrend> DailySpendingTrend { get; set; } = new();
    public int LargestExpense { get; set; }
    public int BudgetRemaining { get; set; }
    public decimal BudgetUtilizationPercent { get; set; }
}

/// <summary>
/// Earning analytics
/// </summary>
public class EarningAnalytics
{
    public int TotalEarned { get; set; }
    public decimal DailyAverageEarnings { get; set; }
    public Dictionary<string, int> EarningsByCategory { get; set; } = new();
    public List<EarningTrend> DailyEarningTrend { get; set; } = new();
    public int LargestEarning { get; set; }
    public decimal EarningGrowthRate { get; set; }
    public int GoalProgress { get; set; }
}

/// <summary>
/// Goal tracking data
/// </summary>
public class GoalTrackingData
{
    public int MonthlyEarningGoal { get; set; }
    public int MonthlyEarningProgress { get; set; }
    public decimal EarningGoalProgress { get; set; }
    public int MonthlySpendingBudget { get; set; }
    public int MonthlySpendingActual { get; set; }
    public decimal SpendingBudgetProgress { get; set; }
    public int ProjectCompletionGoal { get; set; }
    public int ProjectCompletionProgress { get; set; }
    public decimal ProjectGoalProgress { get; set; }
    public List<GoalAlert> GoalAlerts { get; set; } = new();
}

/// <summary>
/// Activity insights
/// </summary>
public class ActivityInsights
{
    public string MostActiveDay { get; set; } = string.Empty;
    public string MostProfitableProjectType { get; set; } = string.Empty;
    public decimal AverageProjectValue { get; set; }
    public decimal TransactionFrequency { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public List<string> KeyInsights { get; set; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public decimal EarningEfficiency { get; set; }
    public decimal ProjectCompletionRate { get; set; }
    public decimal ClientSatisfactionScore { get; set; }
    public int ConsistencyScore { get; set; }
    public string PerformanceRating { get; set; } = string.Empty; // "Excellent", "Good", etc.
}

/// <summary>
/// Budget alert settings
/// </summary>
public class BudgetAlertSettings
{
    public int SpendingAlert50Percent { get; set; } = 50;
    public int SpendingAlert75Percent { get; set; } = 75;
    public int SpendingAlert90Percent { get; set; } = 90;
    public int EarningGoalAlert25Percent { get; set; } = 25;
    public bool EnableDailyDigest { get; set; } = true;
    public bool EnableWeeklyReport { get; set; } = true;
}

/// <summary>
/// Report metadata
/// </summary>
public class ReportMetadata
{
    public string Version { get; set; } = "1.0";
    public TimeSpan GenerationTime { get; set; }
    public int DataPointsAnalyzed { get; set; }
    public bool HasFilters { get; set; }
    public List<string> AppliedFilters { get; set; } = new();
    public string GeneratedBy { get; set; } = "System";
}

/// <summary>
/// Export metadata
/// </summary>
public class ExportMetadata
{
    public DateTime ExportTimestamp { get; set; } = DateTime.UtcNow;
    public string ExportedBy { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public Dictionary<string, object> CustomProperties { get; set; } = new();
}

/// <summary>
/// Spending trend data point
/// </summary>
public class SpendingTrend
{
    public DateTime Date { get; set; }
    public int Amount { get; set; }
    public int TransactionCount { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Earning trend data point
/// </summary>
public class EarningTrend
{
    public DateTime Date { get; set; }
    public int Amount { get; set; }
    public int TransactionCount { get; set; }
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Goal alert
/// </summary>
public class GoalAlert
{
    public string Type { get; set; } = string.Empty; // "warning", "info", "success"
    public string Message { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public bool IsUrgent { get; set; }
}

/// <summary>
/// Wallet dashboard summary
/// </summary>
public class WalletDashboardSummary
{
    public int CurrentBalance { get; set; }
    public int AvailableBalance { get; set; }
    public int PendingBalance { get; set; }
    public string BalanceChangeIndicator { get; set; } = string.Empty;
    public decimal BalanceChangePercent { get; set; }
}

/// <summary>
/// Recent activity summary
/// </summary>
public class RecentActivitySummary
{
    public int TransactionsLast7Days { get; set; }
    public int EarningsLast7Days { get; set; }
    public int SpendingLast7Days { get; set; }
    public int ProjectsCompletedLast30Days { get; set; }
    public List<RecentTransaction> RecentTransactions { get; set; } = new();
}

/// <summary>
/// Recent transaction
/// </summary>
public class RecentTransaction
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsIncoming { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Monthly performance
/// </summary>
public class MonthlyPerformance
{
    public int CurrentMonthEarnings { get; set; }
    public int CurrentMonthSpending { get; set; }
    public int PreviousMonthEarnings { get; set; }
    public int PreviousMonthSpending { get; set; }
    public decimal EarningsGrowth { get; set; }
    public decimal SpendingGrowth { get; set; }
    public int ProjectsCompleted { get; set; }
}

/// <summary>
/// Goal progress
/// </summary>
public class GoalProgress
{
    public decimal EarningGoalProgress { get; set; }
    public decimal SpendingBudgetProgress { get; set; }
    public decimal ProjectGoalProgress { get; set; }
    public bool OnTrackForGoals { get; set; }
    public string GoalStatus { get; set; } = string.Empty;
}

/// <summary>
/// Trend indicators
/// </summary>
public class TrendIndicators
{
    public string EarningTrend { get; set; } = string.Empty; // "up", "down", "stable"
    public string SpendingTrend { get; set; } = string.Empty;
    public string ActivityTrend { get; set; } = string.Empty;
    public string OverallHealthScore { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard alert
/// </summary>
public class DashboardAlert
{
    public string Type { get; set; } = string.Empty; // "info", "warning", "success", "error"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsUrgent { get; set; }
    public string? ActionUrl { get; set; }
}

/// <summary>
/// Quick action
/// </summary>
public class QuickAction
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

#endregion

#region System Analytics DTOs

/// <summary>
/// System-wide financial analytics
/// </summary>
public class SystemFinancialAnalytics
{
    public int TotalActiveUsers { get; set; }
    public long TotalCreditsInCirculation { get; set; }
    public long TotalTransactionVolume { get; set; }
    public int TotalTransactionCount { get; set; }
    public decimal AverageUserBalance { get; set; }
    public decimal PlatformGrowthRate { get; set; }
    public Dictionary<CreditTransactionType, long> VolumeByTransactionType { get; set; } = new();
    public List<PeriodTrendData> PlatformTrends { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Top user earnings
/// </summary>
public class TopUserEarnings
{
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public int TotalEarnings { get; set; }
    public int ProjectsCompleted { get; set; }
    public decimal AverageProjectValue { get; set; }
    public string PrimarySkillArea { get; set; } = string.Empty;
}

/// <summary>
/// Platform health metrics
/// </summary>
public class PlatformHealthMetrics
{
    public decimal UserEngagementScore { get; set; }
    public decimal TransactionSuccessRate { get; set; }
    public decimal AverageResponseTime { get; set; }
    public int ActiveUsersLast30Days { get; set; }
    public decimal PlatformRevenueGrowth { get; set; }
    public string OverallHealthStatus { get; set; } = string.Empty;
    public List<string> HealthAlerts { get; set; } = new();
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Data integrity report
/// </summary>
public class DataIntegrityReport
{
    public Guid UserId { get; set; }
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int ReportsValidated { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Report reconciliation result
/// </summary>
public class ReportReconciliationResult
{
    public Guid UserId { get; set; }
    public int ReportMonth { get; set; }
    public bool IsReconciled { get; set; }
    public List<string> Discrepancies { get; set; } = new();
    public bool AutoCorrected { get; set; }
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Enums

/// <summary>
/// Report period types
/// </summary>
public enum ReportPeriodType
{
    None = 0,
    Monthly,
    Quarterly,
    Annual,
    Custom
}


#endregion