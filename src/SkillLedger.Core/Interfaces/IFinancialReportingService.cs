using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Core.Interfaces;

/// <summary>
/// Service interface for financial reporting and analytics
/// Provides comprehensive credit reporting, analytics, and export capabilities
/// </summary>
public interface IFinancialReportingService
{
    #region Report Generation

    /// <summary>
    /// Generate a comprehensive credit summary report for a user
    /// </summary>
    /// <param name="request">Report request parameters</param>
    /// <returns>Detailed credit summary report</returns>
    Task<CreditSummaryReport> GenerateCreditSummaryReportAsync(CreditReportRequest request);

    /// <summary>
    /// Generate monthly credit reports for a user (pre-aggregated data)
    /// </summary>
    /// <param name="userId">User ID to generate reports for</param>
    /// <param name="startMonth">Start month in YYYYMM format</param>
    /// <param name="endMonth">End month in YYYYMM format</param>
    /// <returns>List of monthly credit reports</returns>
    Task<List<UserCreditReport>> GenerateMonthlyReportsAsync(Guid userId, int startMonth, int endMonth);

    /// <summary>
    /// Generate or update a single monthly credit report for a user
    /// </summary>
    /// <param name="userId">User ID to generate report for</param>
    /// <param name="reportMonth">Report month in YYYYMM format</param>
    /// <param name="forceRecalculate">Whether to force recalculation even if report exists</param>
    /// <returns>Generated or updated credit report</returns>
    Task<UserCreditReport> GenerateMonthlyReportAsync(Guid userId, int reportMonth, bool forceRecalculate = false);

    /// <summary>
    /// Generate quarterly credit summary
    /// </summary>
    /// <param name="userId">User ID to generate report for</param>
    /// <param name="quarter">Quarter (1-4)</param>
    /// <param name="year">Year</param>
    /// <returns>Quarterly credit summary</returns>
    Task<CreditSummaryReport> GenerateQuarterlyReportAsync(Guid userId, int quarter, int year);

    /// <summary>
    /// Generate annual credit summary
    /// </summary>
    /// <param name="userId">User ID to generate report for</param>
    /// <param name="year">Year</param>
    /// <returns>Annual credit summary</returns>
    Task<CreditSummaryReport> GenerateAnnualReportAsync(Guid userId, int year);

    #endregion

    #region Real-time Analytics

    /// <summary>
    /// Get real-time analytics data for a user
    /// </summary>
    /// <param name="request">Analytics request parameters</param>
    /// <returns>Real-time analytics data</returns>
    Task<AnalyticsData> GetRealTimeAnalyticsAsync(AnalyticsRequest request);

    /// <summary>
    /// Get user dashboard data with activity insights
    /// </summary>
    /// <param name="userId">User ID to get dashboard for</param>
    /// <returns>Comprehensive dashboard data</returns>
    Task<UserDashboardData> GetUserDashboardDataAsync(Guid userId);

    /// <summary>
    /// Get spending and earning analytics for a specific time period
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>Spending and earning analytics</returns>
    Task<(SpendingAnalytics Spending, EarningAnalytics Earnings)> GetSpendingEarningAnalyticsAsync(
        Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Calculate performance metrics for a user
    /// </summary>
    /// <param name="userId">User ID to calculate metrics for</param>
    /// <param name="timeWindowDays">Time window for analysis in days</param>
    /// <returns>Performance metrics</returns>
    Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(Guid userId, int timeWindowDays = 90);

    #endregion

    #region Budget and Goal Tracking

    /// <summary>
    /// Set up or update budget tracking for a user
    /// </summary>
    /// <param name="request">Budget tracking request</param>
    /// <returns>Success result with updated settings</returns>
    Task<WalletOperationResponse> SetupBudgetTrackingAsync(BudgetTrackingRequest request);

    /// <summary>
    /// Get current goal tracking progress for a user
    /// </summary>
    /// <param name="userId">User ID to get progress for</param>
    /// <returns>Goal tracking data with current progress</returns>
    Task<GoalTrackingData> GetGoalTrackingProgressAsync(Guid userId);

    /// <summary>
    /// Update earning and spending goals for a user
    /// </summary>
    /// <param name="userId">User ID to update goals for</param>
    /// <param name="monthlyEarningGoal">New monthly earning goal</param>
    /// <param name="monthlySpendingBudget">New monthly spending budget</param>
    /// <param name="projectCompletionGoal">New project completion goal</param>
    /// <returns>Success result</returns>
    Task<bool> UpdateUserGoalsAsync(Guid userId, int monthlyEarningGoal, int monthlySpendingBudget, int projectCompletionGoal);

    /// <summary>
    /// Check for budget and goal alerts for a user
    /// </summary>
    /// <param name="userId">User ID to check alerts for</param>
    /// <returns>List of current alerts</returns>
    Task<List<GoalAlert>> CheckBudgetGoalAlertsAsync(Guid userId);

    #endregion

    #region Export Services

    /// <summary>
    /// Export financial data in the specified format
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <returns>Export result with file content</returns>
    Task<FinancialExportResult> ExportFinancialDataAsync(FinancialExportRequest request);

    /// <summary>
    /// Export transaction history as CSV
    /// </summary>
    /// <param name="userId">User ID to export data for</param>
    /// <param name="startDate">Start date for export</param>
    /// <param name="endDate">End date for export</param>
    /// <returns>CSV export result</returns>
    Task<FinancialExportResult> ExportTransactionHistoryAsCsvAsync(Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Export credit summary as PDF report
    /// </summary>
    /// <param name="userId">User ID to export report for</param>
    /// <param name="request">Report request parameters</param>
    /// <returns>PDF export result</returns>
    Task<FinancialExportResult> ExportCreditSummaryAsPdfAsync(Guid userId, CreditReportRequest request);

    /// <summary>
    /// Export financial data as JSON for API consumption
    /// </summary>
    /// <param name="userId">User ID to export data for</param>
    /// <param name="startDate">Start date for export</param>
    /// <param name="endDate">End date for export</param>
    /// <returns>JSON export result</returns>
    Task<FinancialExportResult> ExportFinancialDataAsJsonAsync(Guid userId, DateTime startDate, DateTime endDate);

    #endregion

    #region Categorized Reporting

    /// <summary>
    /// Get detailed transaction breakdown by category
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>List of category breakdowns</returns>
    Task<List<TransactionCategoryBreakdown>> GetCategorizedTransactionBreakdownAsync(
        Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get project earnings breakdown for a user
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>List of project earnings breakdowns</returns>
    Task<List<ProjectEarningsBreakdown>> GetProjectEarningsBreakdownAsync(
        Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get earnings breakdown by transaction type (project earnings, transfers, bonuses)
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>Dictionary of earnings by type</returns>
    Task<Dictionary<CreditTransactionType, int>> GetEarningsByTypeAsync(
        Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get spending breakdown by transaction type
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>Dictionary of spending by type</returns>
    Task<Dictionary<CreditTransactionType, int>> GetSpendingByTypeAsync(
        Guid userId, DateTime startDate, DateTime endDate);

    #endregion

    #region Trend Analysis

    /// <summary>
    /// Get historical trend data for earnings and spending
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="months">Number of months to include in trend</param>
    /// <returns>List of period trend data</returns>
    Task<List<PeriodTrendData>> GetHistoricalTrendDataAsync(Guid userId, int months = 12);

    /// <summary>
    /// Calculate earning growth rate over time
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="timeWindowDays">Time window for calculation</param>
    /// <returns>Growth rate as percentage</returns>
    Task<decimal> CalculateEarningGrowthRateAsync(Guid userId, int timeWindowDays = 90);

    /// <summary>
    /// Get daily earning and spending trends
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="days">Number of days to include</param>
    /// <returns>Tuple of daily earning and spending trends</returns>
    Task<(List<EarningTrend> EarningTrends, List<SpendingTrend> SpendingTrends)> GetDailyTrendsAsync(
        Guid userId, int days = 30);

    /// <summary>
    /// Predict future earnings based on historical patterns
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="forecastDays">Number of days to forecast</param>
    /// <returns>Predicted earnings for the forecast period</returns>
    Task<List<EarningTrend>> PredictFutureEarningsAsync(Guid userId, int forecastDays = 30);

    #endregion

    #region Activity Insights

    /// <summary>
    /// Generate activity insights and recommendations for a user
    /// </summary>
    /// <param name="userId">User ID to generate insights for</param>
    /// <param name="analysisWindowDays">Analysis window in days</param>
    /// <returns>Activity insights with recommendations</returns>
    Task<ActivityInsights> GenerateActivityInsightsAsync(Guid userId, int analysisWindowDays = 90);

    /// <summary>
    /// Identify peak activity patterns for a user
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <returns>Dictionary of peak activity patterns</returns>
    Task<Dictionary<string, object>> IdentifyPeakActivityPatternsAsync(Guid userId);

    /// <summary>
    /// Get transaction frequency analysis
    /// </summary>
    /// <param name="userId">User ID to analyze</param>
    /// <param name="timeWindowDays">Time window for analysis</param>
    /// <returns>Transaction frequency metrics</returns>
    Task<decimal> CalculateTransactionFrequencyAsync(Guid userId, int timeWindowDays = 30);

    #endregion

    #region Report Management

    /// <summary>
    /// Get existing monthly credit reports for a user
    /// </summary>
    /// <param name="userId">User ID to get reports for</param>
    /// <param name="startMonth">Start month in YYYYMM format (optional)</param>
    /// <param name="endMonth">End month in YYYYMM format (optional)</param>
    /// <returns>List of existing credit reports</returns>
    Task<List<UserCreditReport>> GetExistingMonthlyReportsAsync(Guid userId, int? startMonth = null, int? endMonth = null);

    /// <summary>
    /// Delete old credit reports beyond retention period
    /// </summary>
    /// <param name="retentionMonths">Number of months to retain</param>
    /// <returns>Number of reports deleted</returns>
    Task<int> CleanupOldReportsAsync(int retentionMonths = 36);

    /// <summary>
    /// Recalculate all reports for a user (data correction scenarios)
    /// </summary>
    /// <param name="userId">User ID to recalculate reports for</param>
    /// <param name="startMonth">Start month for recalculation (optional)</param>
    /// <param name="endMonth">End month for recalculation (optional)</param>
    /// <returns>Number of reports recalculated</returns>
    Task<int> RecalculateUserReportsAsync(Guid userId, int? startMonth = null, int? endMonth = null);

    /// <summary>
    /// Finalize a monthly report to prevent further changes
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reportMonth">Report month to finalize</param>
    /// <returns>Success result</returns>
    Task<bool> FinalizeMonthlyReportAsync(Guid userId, int reportMonth);

    #endregion

    #region System Analytics

    /// <summary>
    /// Generate system-wide financial analytics (admin function)
    /// </summary>
    /// <param name="startDate">Analysis start date</param>
    /// <param name="endDate">Analysis end date</param>
    /// <returns>System-wide analytics</returns>
    Task<SystemFinancialAnalytics> GenerateSystemAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get top earning users for a period (admin function)
    /// </summary>
    /// <param name="startDate">Period start date</param>
    /// <param name="endDate">Period end date</param>
    /// <param name="limit">Number of top users to return</param>
    /// <returns>List of top earning users</returns>
    Task<List<TopUserEarnings>> GetTopEarningUsersAsync(DateTime startDate, DateTime endDate, int limit = 10);

    /// <summary>
    /// Calculate platform health metrics
    /// </summary>
    /// <returns>Platform health metrics</returns>
    Task<PlatformHealthMetrics> CalculatePlatformHealthMetricsAsync();

    #endregion

    #region Data Validation and Integrity

    /// <summary>
    /// Validate report data integrity for a user
    /// </summary>
    /// <param name="userId">User ID to validate</param>
    /// <param name="reportMonth">Specific month to validate (optional)</param>
    /// <returns>Validation result with any issues found</returns>
    Task<DataIntegrityReport> ValidateReportIntegrityAsync(Guid userId, int? reportMonth = null);

    /// <summary>
    /// Reconcile pre-aggregated report data with transaction history
    /// </summary>
    /// <param name="userId">User ID to reconcile</param>
    /// <param name="reportMonth">Report month to reconcile</param>
    /// <returns>Reconciliation result</returns>
    Task<ReportReconciliationResult> ReconcileReportDataAsync(Guid userId, int reportMonth);

    /// <summary>
    /// Check for missing monthly reports and generate them
    /// </summary>
    /// <param name="userId">User ID to check (optional, checks all users if null)</param>
    /// <returns>Number of missing reports generated</returns>
    Task<int> GenerateMissingReportsAsync(Guid? userId = null);

    #endregion
}