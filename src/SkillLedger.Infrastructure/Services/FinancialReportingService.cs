using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Comprehensive financial reporting and analytics service
/// Provides pre-aggregated reports, real-time analytics, and business intelligence
/// </summary>
public partial class FinancialReportingService : IFinancialReportingService
{
    private readonly SkillLedgerDbContext _context;
    private readonly ICreditWalletService _creditWalletService;
    private readonly IFinancialExportService _exportService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<FinancialReportingService> _logger;
    private readonly EncryptionConfiguration _encryptionConfig;

    // Constants for report generation
    private const int MAX_TREND_MONTHS = 36;
    private const int DEFAULT_FORECAST_ACCURACY_DAYS = 90;
    private const int BULK_REPORT_BATCH_SIZE = 100;
    private const int REPORT_RETENTION_MONTHS = 36;

    public FinancialReportingService(
        SkillLedgerDbContext context,
        ICreditWalletService creditWalletService,
        IFinancialExportService exportService,
        IAuditLogService auditLogService,
        ILogger<FinancialReportingService> logger,
        IOptions<EncryptionConfiguration> encryptionConfig)
    {
        _context = context;
        _creditWalletService = creditWalletService;
        _exportService = exportService;
        _auditLogService = auditLogService;
        _logger = logger;
        _encryptionConfig = encryptionConfig.Value;
    }

    #region Report Generation

    public async Task<CreditSummaryReport> GenerateCreditSummaryReportAsync(CreditReportRequest request)
    {
        try
        {
            _logger.LogInformation("Generating credit summary report for user {UserId}, period {PeriodType}, {StartDate} to {EndDate}",
                request.UserId, request.PeriodType, request.StartDate, request.EndDate);

            // Validate request
            if (request.EndDate <= request.StartDate)
                throw new ArgumentException("End date must be after start date");

            var userId = request.UserId ?? throw new ArgumentException("User ID is required");
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty");

            // Generate the comprehensive report
            var report = new CreditSummaryReport
            {
                UserId = userId,
                Period = CreateReportPeriodInfo(request),
                GeneratedAt = DateTime.UtcNow,
                Metadata = new ReportMetadata
                {
                    GenerationTime = TimeSpan.Zero, // Will be calculated at the end
                    HasFilters = request.TransactionTypeFilter?.Any() == true
                }
            };

            // Track generation time
            var startTime = DateTime.UtcNow;

            // Get financial summary
            report.Summary = await GenerateFinancialSummaryAsync(userId, request.StartDate, request.EndDate);

            // Get category breakdowns
            report.CategoryBreakdowns = await GetCategorizedTransactionBreakdownAsync(
                userId, request.StartDate, request.EndDate);

            // Get project breakdowns if requested
            if (request.IncludeProjectBreakdown)
            {
                report.ProjectBreakdowns = await GetProjectEarningsBreakdownAsync(
                    userId, request.StartDate, request.EndDate);
            }

            // Get trend data
            var trendMonths = CalculateTrendMonths(request.PeriodType);
            report.TrendData = await GetHistoricalTrendDataAsync(userId, trendMonths);

            // Get transaction details if requested
            if (request.IncludeTransactionDetails)
            {
                report.TransactionDetails = await GetTransactionSummariesAsync(
                    userId, request.StartDate, request.EndDate, request.TransactionTypeFilter);
            }

            // Apply filters to metadata
            if (request.TransactionTypeFilter?.Any() == true)
            {
                report.Metadata.AppliedFilters = request.TransactionTypeFilter
                    .Select(t => $"TransactionType: {t}")
                    .ToList();
            }

            // Calculate generation time
            report.Metadata.GenerationTime = DateTime.UtcNow - startTime;
            report.Metadata.DataPointsAnalyzed = CalculateDataPointsAnalyzed(report);

            _logger.LogInformation("Successfully generated credit summary report for user {UserId} in {Duration}ms",
                userId, report.Metadata.GenerationTime.TotalMilliseconds);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate credit summary report for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<List<UserCreditReport>> GenerateMonthlyReportsAsync(Guid userId, int startMonth, int endMonth)
    {
        try
        {
            _logger.LogInformation("Generating monthly reports for user {UserId}, months {StartMonth} to {EndMonth}",
                userId, startMonth, endMonth);

            var reports = new List<UserCreditReport>();
            var monthsToGenerate = GenerateMonthRange(startMonth, endMonth);

            foreach (var month in monthsToGenerate)
            {
                var report = await GenerateMonthlyReportAsync(userId, month, false);
                reports.Add(report);
            }

            _logger.LogInformation("Generated {Count} monthly reports for user {UserId}", reports.Count, userId);
            return reports;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate monthly reports for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserCreditReport> GenerateMonthlyReportAsync(Guid userId, int reportMonth, bool forceRecalculate = false)
    {
        try
        {
            _logger.LogInformation("Generating monthly report for user {UserId}, month {ReportMonth}, force recalculate: {ForceRecalculate}",
                userId, reportMonth, forceRecalculate);

            // Check if report already exists
            var existingReport = await _context.UserCreditReports
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ReportMonth == reportMonth);

            if (existingReport != null && !forceRecalculate && existingReport.IsFinalized)
            {
                _logger.LogInformation("Returning existing finalized report for user {UserId}, month {ReportMonth}", userId, reportMonth);
                return existingReport;
            }

            // Calculate month boundaries
            var (year, month) = ParseReportMonth(reportMonth);
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Get transactions for the month
            var transactions = await GetUserTransactionsForPeriodAsync(userId, monthStart, monthEnd.AddDays(1));

            // Get wallet state at month boundaries
            var startingBalance = await CalculateBalanceAtDateAsync(userId, monthStart);
            var endingBalance = await CalculateBalanceAtDateAsync(userId, monthEnd.AddDays(1));

            // Create or update report
            var report = existingReport ?? new UserCreditReport
            {
                UserId = userId,
                ReportMonth = reportMonth
            };

            // Calculate aggregated data
            await PopulateReportDataAsync(report, transactions, startingBalance, endingBalance, monthStart, monthEnd);

            // Save or update the report
            if (existingReport == null)
            {
                _context.UserCreditReports.Add(report);
                _logger.LogInformation("Created new monthly report for user {UserId}, month {ReportMonth}", userId, reportMonth);
            }
            else
            {
                existingReport = report;
                _logger.LogInformation("Updated existing monthly report for user {UserId}, month {ReportMonth}", userId, reportMonth);
            }

            await _context.SaveChangesAsync();

            // Audit log
            await _auditLogService.LogEventAsync(
                userId,
                "MonthlyReportGenerated",
                string.Empty,
                null,
                true,
                $"Report month: {reportMonth}, Force recalculate: {forceRecalculate}"
            );

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate monthly report for user {UserId}, month {ReportMonth}", userId, reportMonth);
            throw;
        }
    }

    public async Task<CreditSummaryReport> GenerateQuarterlyReportAsync(Guid userId, int quarter, int year)
    {
        try
        {
            _logger.LogInformation("Generating quarterly report for user {UserId}, Q{Quarter} {Year}", userId, quarter, year);

            if (quarter < 1 || quarter > 4)
                throw new ArgumentException("Quarter must be between 1 and 4", nameof(quarter));

            // Calculate quarter boundaries
            var quarterStart = new DateTime(year, (quarter - 1) * 3 + 1, 1);
            var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);

            var request = new CreditReportRequest
            {
                UserId = userId,
                PeriodType = ReportPeriodType.Quarterly,
                StartDate = quarterStart,
                EndDate = quarterEnd,
                IncludeProjectBreakdown = true,
                IncludeTransactionDetails = false
            };

            return await GenerateCreditSummaryReportAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate quarterly report for user {UserId}, Q{Quarter} {Year}", userId, quarter, year);
            throw;
        }
    }

    public async Task<CreditSummaryReport> GenerateAnnualReportAsync(Guid userId, int year)
    {
        try
        {
            _logger.LogInformation("Generating annual report for user {UserId}, year {Year}", userId, year);

            var yearStart = new DateTime(year, 1, 1);
            var yearEnd = new DateTime(year, 12, 31);

            var request = new CreditReportRequest
            {
                UserId = userId,
                PeriodType = ReportPeriodType.Annual,
                StartDate = yearStart,
                EndDate = yearEnd,
                IncludeProjectBreakdown = true,
                IncludeTransactionDetails = false
            };

            return await GenerateCreditSummaryReportAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate annual report for user {UserId}, year {Year}", userId, year);
            throw;
        }
    }

    #endregion

    #region Real-time Analytics

    public async Task<AnalyticsData> GetRealTimeAnalyticsAsync(AnalyticsRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            _logger.LogInformation("Generating real-time analytics for user {UserId}, time window {TimeWindowDays} days",
                request.UserId, request.TimeWindowDays);

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-request.TimeWindowDays);

            var analytics = new AnalyticsData
            {
                UserId = request.UserId,
                LastUpdated = DateTime.UtcNow
            };

            // Get current balance analytics
            if (request.IncludeSpendingPatterns || request.IncludeEarningTrends)
            {
                analytics.CurrentBalance = await GetBalanceAnalyticsAsync(request.UserId);
            }

            // Get spending analytics
            if (request.IncludeSpendingPatterns)
            {
                analytics.Spending = await GetSpendingAnalyticsAsync(request.UserId, startDate, endDate);
            }

            // Get earning analytics
            if (request.IncludeEarningTrends)
            {
                analytics.Earnings = await GetEarningAnalyticsAsync(request.UserId, startDate, endDate);
            }

            // Get goal tracking
            if (request.IncludeGoalTracking)
            {
                analytics.GoalTracking = await GetGoalTrackingProgressAsync(request.UserId);
            }

            // Generate insights
            analytics.Insights = await GenerateActivityInsightsAsync(request.UserId, request.TimeWindowDays);

            // Calculate performance metrics
            analytics.Performance = await CalculatePerformanceMetricsAsync(request.UserId, request.TimeWindowDays);

            _logger.LogInformation("Successfully generated real-time analytics for user {UserId}", request.UserId);
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate real-time analytics for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<UserDashboardData> GetUserDashboardDataAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Generating dashboard data for user {UserId}", userId);

            var dashboard = new UserDashboardData
            {
                UserId = userId
            };

            // Get wallet summary
            var wallet = await _creditWalletService.GetWalletAsync(userId);
            if (wallet != null)
            {
                dashboard.Wallet = new WalletDashboardSummary
                {
                    CurrentBalance = wallet.Balance,
                    AvailableBalance = wallet.AvailableBalance,
                    PendingBalance = wallet.PendingBalance,
                    BalanceChangeIndicator = await CalculateBalanceChangeIndicatorAsync(userId),
                    BalanceChangePercent = await CalculateBalanceChangePercentAsync(userId)
                };
            }

            // Get recent activity
            dashboard.RecentActivity = await GetRecentActivitySummaryAsync(userId);

            // Get monthly performance
            dashboard.MonthlyStats = await GetMonthlyPerformanceAsync(userId);

            // Get goal progress
            dashboard.Goals = await GetGoalProgressAsync(userId);

            // Get trend indicators
            dashboard.Trends = await GetTrendIndicatorsAsync(userId);

            // Get alerts
            dashboard.Alerts = await GetDashboardAlertsAsync(userId);

            // Get quick actions
            dashboard.QuickActions = GetQuickActionsForUser(userId);

            _logger.LogInformation("Successfully generated dashboard data for user {UserId}", userId);
            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate dashboard data for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(SpendingAnalytics Spending, EarningAnalytics Earnings)> GetSpendingEarningAnalyticsAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Generating spending/earning analytics for user {UserId}, {StartDate} to {EndDate}",
                userId, startDate, endDate);

            var spendingTask = GetSpendingAnalyticsAsync(userId, startDate, endDate);
            var earningTask = GetEarningAnalyticsAsync(userId, startDate, endDate);

            await Task.WhenAll(spendingTask, earningTask);

            return (await spendingTask, await earningTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate spending/earning analytics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<PerformanceMetrics> CalculatePerformanceMetricsAsync(Guid userId, int timeWindowDays = 90)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-timeWindowDays);

            var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
            var projects = await GetUserProjectsForPeriodAsync(userId, startDate, endDate);

            var metrics = new PerformanceMetrics
            {
                EarningEfficiency = CalculateEarningEfficiency(transactions),
                ProjectCompletionRate = CalculateProjectCompletionRate(projects),
                ConsistencyScore = CalculateConsistencyScore(transactions),
                PerformanceRating = "Good" // Will be calculated based on other metrics
            };

            // Calculate performance rating
            metrics.PerformanceRating = CalculatePerformanceRating(metrics);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate performance metrics for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Budget and Goal Tracking

    public async Task<WalletOperationResponse> SetupBudgetTrackingAsync(BudgetTrackingRequest request)
    {
        try
        {
            _logger.LogInformation("Setting up budget tracking for user {UserId}", request.UserId);

            // For now, we'll store budget settings in metadata format
            // In a full implementation, this would be a separate entity
            var budgetSettings = JsonSerializer.Serialize(request);

            // Store in user profile or separate budget entity
            // This is a simplified implementation
            await _auditLogService.LogEventAsync(
                request.UserId,
                "BudgetTrackingSetup",
                string.Empty,
                null,
                true,
                $"Monthly spending budget: {request.MonthlySpendingBudget}, Earning goal: {request.MonthlyEarningGoal}"
            );

            return new WalletOperationResponse
            {
                Success = true,
                Message = "Budget tracking setup successfully",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup budget tracking for user {UserId}", request.UserId);
            return new WalletOperationResponse
            {
                Success = false,
                Message = "Failed to setup budget tracking",
                Errors = new List<string> { ex.Message },
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<GoalTrackingData> GetGoalTrackingProgressAsync(Guid userId)
    {
        try
        {
            // This would typically fetch from a budget/goals entity
            // For now, using default values
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthlyTransactions = await GetUserTransactionsForPeriodAsync(userId, monthStart, monthEnd.AddDays(1));

            var monthlyEarnings = monthlyTransactions
                .Where(t => IsIncomingTransaction(t))
                .Sum(t => t.Amount);

            var monthlySpending = monthlyTransactions
                .Where(t => !IsIncomingTransaction(t))
                .Sum(t => t.Amount);

            var completedProjects = await GetCompletedProjectsCountAsync(userId, monthStart, monthEnd);

            return new GoalTrackingData
            {
                MonthlyEarningGoal = 2000, // Default goal
                MonthlyEarningProgress = monthlyEarnings,
                EarningGoalProgress = monthlyEarnings / 2000m * 100,
                MonthlySpendingBudget = 1500, // Default budget
                MonthlySpendingActual = monthlySpending,
                SpendingBudgetProgress = monthlySpending / 1500m * 100,
                ProjectCompletionGoal = 3, // Default goal
                ProjectCompletionProgress = completedProjects,
                ProjectGoalProgress = completedProjects / 3m * 100,
                GoalAlerts = await CheckBudgetGoalAlertsAsync(userId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get goal tracking progress for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserGoalsAsync(Guid userId, int monthlyEarningGoal, int monthlySpendingBudget, int projectCompletionGoal)
    {
        try
        {
            // In a full implementation, this would update a UserBudgetSettings entity
            await _auditLogService.LogEventAsync(
                userId,
                "UserGoalsUpdated",
                string.Empty,
                null,
                true,
                $"Earning goal: {monthlyEarningGoal}, Spending budget: {monthlySpendingBudget}, Project goal: {projectCompletionGoal}"
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user goals for user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<GoalAlert>> CheckBudgetGoalAlertsAsync(Guid userId)
    {
        try
        {
            var alerts = new List<GoalAlert>();

            // Get current month data directly to avoid circular reference
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthlyTransactions = await GetUserTransactionsForPeriodAsync(userId, monthStart, monthEnd.AddDays(1));

            var monthlyEarnings = monthlyTransactions
                .Where(t => IsIncomingTransaction(t))
                .Sum(t => t.Amount);

            var monthlySpending = monthlyTransactions
                .Where(t => !IsIncomingTransaction(t))
                .Sum(t => t.Amount);

            // Default goals
            const int defaultEarningGoal = 2000;
            const int defaultSpendingBudget = 1500;

            var earningGoalProgress = defaultEarningGoal > 0 ? (decimal)monthlyEarnings / defaultEarningGoal * 100 : 0;
            var spendingBudgetProgress = defaultSpendingBudget > 0 ? (decimal)monthlySpending / defaultSpendingBudget * 100 : 0;

            // Check spending alerts
            if (spendingBudgetProgress >= 90)
            {
                alerts.Add(new GoalAlert
                {
                    Type = "warning",
                    Message = "You've spent 90% of your monthly budget",
                    Percentage = spendingBudgetProgress,
                    IsUrgent = true
                });
            }
            else if (spendingBudgetProgress >= 75)
            {
                alerts.Add(new GoalAlert
                {
                    Type = "info",
                    Message = "You've spent 75% of your monthly budget",
                    Percentage = spendingBudgetProgress,
                    IsUrgent = false
                });
            }

            // Check earning alerts
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var dayProgress = now.Day / (decimal)daysInMonth * 100;

            if (earningGoalProgress < dayProgress - 20) // 20% behind schedule
            {
                alerts.Add(new GoalAlert
                {
                    Type = "warning",
                    Message = "Earnings are behind your monthly goal pace",
                    Percentage = earningGoalProgress,
                    IsUrgent = false
                });
            }

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check budget goal alerts for user {UserId}", userId);
            return new List<GoalAlert>();
        }
    }

    #endregion

    #region Export Services

    public async Task<FinancialExportResult> ExportFinancialDataAsync(FinancialExportRequest request)
    {
        return request.Format switch
        {
            ExportFormat.CSV => await ExportTransactionHistoryAsCsvAsync(request.UserId, request.StartDate, request.EndDate),
            ExportFormat.JSON => await ExportFinancialDataAsJsonAsync(request.UserId, request.StartDate, request.EndDate),
            ExportFormat.PDF => await ExportCreditSummaryAsPdfAsync(request.UserId, new CreditReportRequest
            {
                UserId = request.UserId,
                PeriodType = ReportPeriodType.Custom,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            }),
            _ => throw new NotSupportedException($"Export format {request.Format} is not supported")
        };
    }

    public async Task<FinancialExportResult> ExportTransactionHistoryAsCsvAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var transactions = await GetTransactionSummariesAsync(userId, startDate, endDate, null);
            var csvContent = await _exportService.ExportToCsvAsync(transactions);

            return new FinancialExportResult
            {
                Success = true,
                Content = csvContent,
                ContentType = "text/csv",
                FileName = $"transactions_{userId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv",
                FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(csvContent),
                RecordCount = transactions.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export transaction history as CSV for user {UserId}", userId);
            return new FinancialExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<FinancialExportResult> ExportCreditSummaryAsPdfAsync(Guid userId, CreditReportRequest request)
    {
        try
        {
            var report = await GenerateCreditSummaryReportAsync(request);
            var pdfContent = await _exportService.ExportReportToPdfAsync(report, true);

            return new FinancialExportResult
            {
                Success = true,
                Content = Convert.ToBase64String(pdfContent),
                ContentType = "application/pdf",
                FileName = $"credit_report_{userId}_{DateTime.UtcNow:yyyyMMdd}.pdf",
                FileSizeBytes = pdfContent.Length,
                RecordCount = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export credit summary as PDF for user {UserId}", userId);
            return new FinancialExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<FinancialExportResult> ExportFinancialDataAsJsonAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var transactions = await GetTransactionSummariesAsync(userId, startDate, endDate, null);
            var jsonContent = await _exportService.ExportToJsonAsync(transactions);

            return new FinancialExportResult
            {
                Success = true,
                Content = jsonContent,
                ContentType = "application/json",
                FileName = $"financial_data_{userId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.json",
                FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(jsonContent),
                RecordCount = transactions.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export financial data as JSON for user {UserId}", userId);
            return new FinancialExportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Categorized Reporting

    public async Task<List<TransactionCategoryBreakdown>> GetCategorizedTransactionBreakdownAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // PERFORMANCE FIX: Perform GroupBy aggregation at database level instead of loading all transactions
            var categoryAggregates = await _context.CreditTransactions
                .Where(t => (t.ToUserId == userId || t.FromUserId == userId) &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt < endDate &&
                           t.Status == TransactionStatus.Completed)
                .GroupBy(t => t.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    TotalAmount = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            // Apply display logic to small aggregated result set
            var breakdowns = categoryAggregates
                .Select(agg => new TransactionCategoryBreakdown
                {
                    Category = agg.Type,
                    DisplayName = GetTransactionTypeDisplayName(agg.Type),
                    TotalAmount = agg.TotalAmount,
                    TransactionCount = agg.Count,
                    AverageAmount = agg.Count > 0 ? (decimal)agg.TotalAmount / agg.Count : 0,
                    IsIncoming = IsIncomingTransactionType(agg.Type),
                    PercentageOfTotal = 0 // Will be calculated next
                })
                .ToList();

            // Calculate percentages
            var totalTransactionAmount = breakdowns.Sum(b => b.TotalAmount);
            if (totalTransactionAmount > 0)
            {
                foreach (var breakdown in breakdowns)
                {
                    breakdown.PercentageOfTotal = (decimal)breakdown.TotalAmount / totalTransactionAmount * 100;
                }
            }

            return breakdowns.OrderByDescending(b => b.TotalAmount).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get categorized transaction breakdown for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<ProjectEarningsBreakdown>> GetProjectEarningsBreakdownAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            // PERFORMANCE FIX: Perform GroupBy aggregation at database level with AsNoTracking and AsSplitQuery
            // First get the aggregated transaction data
            var transactionGroups = await _context.CreditTransactions
                .AsNoTracking()
                .Where(t => t.ToUserId == userId &&
                           t.ProjectId != null &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate &&
                           t.Status == TransactionStatus.Completed)
                .GroupBy(t => t.ProjectId!.Value)
                .Select(g => new
                {
                    ProjectId = g.Key,
                    TotalEarned = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .ToListAsync();

            // Get project details separately to avoid null reference issues with navigation properties in GroupBy
            var projectIds = transactionGroups.Select(g => g.ProjectId).ToList();
            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Combine the data
            var breakdowns = transactionGroups
                .Select(g => new ProjectEarningsBreakdown
                {
                    ProjectId = g.ProjectId,
                    ProjectTitle = projects.TryGetValue(g.ProjectId, out var project) ? project.Title : "Unknown Project",
                    TotalEarned = g.TotalEarned,
                    TransactionCount = g.TransactionCount,
                    ProjectCompletedAt = null, // Would need completion date from project status tracking
                    ProjectStatus = projects.TryGetValue(g.ProjectId, out var proj) ? proj.Status.ToString() : "Unknown",
                    HourlyRate = 0, // Would be calculated based on time tracking
                    HoursWorked = 0 // Would come from time tracking data
                })
                .OrderByDescending(b => b.TotalEarned)
                .ToList();

            return breakdowns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project earnings breakdown for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Dictionary<CreditTransactionType, int>> GetEarningsByTypeAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var earnings = await _context.CreditTransactions
                .Where(t => t.ToUserId == userId &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate &&
                           t.Status == TransactionStatus.Completed)
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(x => x.Type, x => x.Total);

            return earnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get earnings by type for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Dictionary<CreditTransactionType, int>> GetSpendingByTypeAsync(
        Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var spending = await _context.CreditTransactions
                .Where(t => t.FromUserId == userId &&
                           t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate &&
                           t.Status == TransactionStatus.Completed)
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(x => x.Type, x => x.Total);

            return spending;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get spending by type for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Trend Analysis

    public async Task<List<PeriodTrendData>> GetHistoricalTrendDataAsync(Guid userId, int months = 12)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var trendData = new List<PeriodTrendData>();

            // Generate monthly trend data
            for (int i = 0; i < months; i++)
            {
                var periodStart = startDate.AddMonths(i);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);
                var reportMonth = periodStart.Year * 100 + periodStart.Month;

                var transactions = await GetUserTransactionsForPeriodAsync(userId, periodStart, periodEnd.AddDays(1));
                var earnings = transactions.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount);
                var spending = transactions.Where(t => !IsIncomingTransaction(t)).Sum(t => t.Amount);
                var projects = await GetCompletedProjectsCountAsync(userId, periodStart, periodEnd);

                // Calculate growth rate compared to previous period
                var previousPeriodData = trendData.LastOrDefault();
                var growthRate = 0m;
                if (previousPeriodData != null && previousPeriodData.Earnings > 0)
                {
                    growthRate = ((decimal)earnings - previousPeriodData.Earnings) / previousPeriodData.Earnings * 100;
                }

                trendData.Add(new PeriodTrendData
                {
                    Period = reportMonth,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    Earnings = earnings,
                    Spending = spending,
                    TransactionCount = transactions.Count,
                    ProjectsCompleted = projects,
                    GrowthRate = growthRate
                });
            }

            return trendData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get historical trend data for user {UserId}", userId);
            throw;
        }
    }

    public async Task<decimal> CalculateEarningGrowthRateAsync(Guid userId, int timeWindowDays = 90)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var midDate = endDate.AddDays(-timeWindowDays / 2);
            var startDate = endDate.AddDays(-timeWindowDays);

            var firstPeriodEarnings = await GetUserEarningsForPeriodAsync(userId, startDate, midDate);
            var secondPeriodEarnings = await GetUserEarningsForPeriodAsync(userId, midDate, endDate);

            if (firstPeriodEarnings == 0) return 0;

            return (secondPeriodEarnings - firstPeriodEarnings) / firstPeriodEarnings * 100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate earning growth rate for user {UserId}", userId);
            throw;
        }
    }

    public async Task<(List<EarningTrend> EarningTrends, List<SpendingTrend> SpendingTrends)> GetDailyTrendsAsync(
        Guid userId, int days = 30)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate.AddDays(1));

            var earningTrends = new List<EarningTrend>();
            var spendingTrends = new List<SpendingTrend>();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var dayTransactions = transactions.Where(t => t.CreatedAt.Date == date).ToList();

                var earnings = dayTransactions.Where(t => IsIncomingTransaction(t));
                var spending = dayTransactions.Where(t => !IsIncomingTransaction(t));

                earningTrends.Add(new EarningTrend
                {
                    Date = date,
                    Amount = earnings.Sum(t => t.Amount),
                    TransactionCount = earnings.Count(),
                    Category = "Daily"
                });

                spendingTrends.Add(new SpendingTrend
                {
                    Date = date,
                    Amount = spending.Sum(t => t.Amount),
                    TransactionCount = spending.Count(),
                    Category = "Daily"
                });
            }

            return (earningTrends, spendingTrends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily trends for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<EarningTrend>> PredictFutureEarningsAsync(Guid userId, int forecastDays = 30)
    {
        try
        {
            // Simple linear regression prediction based on historical data
            var historicalData = await GetDailyTrendsAsync(userId, DEFAULT_FORECAST_ACCURACY_DAYS);
            var earningTrends = historicalData.EarningTrends;

            // Calculate average daily earnings
            var averageDailyEarnings = earningTrends.Average(t => t.Amount);

            // Calculate trend (simple linear slope)
            var days = earningTrends.Count;
            var sumX = days * (days + 1) / 2; // Sum of 1, 2, 3, ..., days
            var sumY = earningTrends.Sum(t => t.Amount);
            var sumXY = earningTrends.Select((t, i) => (i + 1) * t.Amount).Sum();
            var sumX2 = days * (days + 1) * (2 * days + 1) / 6; // Sum of squares

            var slope = 0m;
            if (days > 1)
            {
                slope = ((decimal)days * sumXY - sumX * sumY) / ((decimal)days * sumX2 - sumX * sumX);
            }

            // Generate predictions
            var predictions = new List<EarningTrend>();
            var startDate = DateTime.UtcNow.Date.AddDays(1);

            for (int i = 0; i < forecastDays; i++)
            {
                var predictedAmount = Math.Max(0, (int)((decimal)averageDailyEarnings + slope * (days + i + 1)));

                predictions.Add(new EarningTrend
                {
                    Date = startDate.AddDays(i),
                    Amount = predictedAmount,
                    TransactionCount = 0, // Prediction doesn't include transaction count
                    Category = "Predicted"
                });
            }

            return predictions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict future earnings for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Activity Insights

    public async Task<ActivityInsights> GenerateActivityInsightsAsync(Guid userId, int analysisWindowDays = 90)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-analysisWindowDays);

            var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
            var projects = await GetUserProjectsForPeriodAsync(userId, startDate, endDate);

            var insights = new ActivityInsights();

            // Find most active day
            var dayActivity = transactions
                .GroupBy(t => t.CreatedAt.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            insights.MostActiveDay = dayActivity?.Key.ToString() ?? "No activity";

            // Find most profitable project type - simplified since no Category property
            var projectEarnings = projects
                .Where(p => p.Status == ProjectStatus.Completed)
                .GroupBy(p => "General") // Simplified - would use actual project categories
                .OrderByDescending(g => g.Sum(p => CalculateProjectValue(p)))
                .FirstOrDefault();

            insights.MostProfitableProjectType = projectEarnings?.Key ?? "No projects";

            // Calculate average project value
            var completedProjects = projects.Where(p => p.Status == ProjectStatus.Completed).ToList();
            insights.AverageProjectValue = completedProjects.Any()
                ? completedProjects.Average(p => CalculateProjectValue(p))
                : 0;

            // Calculate transaction frequency
            insights.TransactionFrequency = await CalculateTransactionFrequencyAsync(userId, analysisWindowDays);

            // Generate recommendations
            insights.RecommendedAction = GenerateRecommendedAction(transactions, projects);

            // Generate key insights
            insights.KeyInsights = GenerateKeyInsights(transactions, projects, insights);

            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate activity insights for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> IdentifyPeakActivityPatternsAsync(Guid userId)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-90);

            var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);

            var patterns = new Dictionary<string, object>();

            // Peak hours
            var hourlyActivity = transactions
                .GroupBy(t => t.CreatedAt.Hour)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .ToDictionary(g => $"Hour {g.Key}:00", g => g.Count());

            patterns["PeakHours"] = hourlyActivity;

            // Peak days of week
            var weeklyActivity = transactions
                .GroupBy(t => t.CreatedAt.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            patterns["PeakDaysOfWeek"] = weeklyActivity;

            // Peak transaction types
            var typeActivity = transactions
                .GroupBy(t => t.Type)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            patterns["PeakTransactionTypes"] = typeActivity;

            return patterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify peak activity patterns for user {UserId}", userId);
            throw;
        }
    }

    public async Task<decimal> CalculateTransactionFrequencyAsync(Guid userId, int timeWindowDays = 30)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-timeWindowDays);

            var transactionCount = await _context.CreditTransactions
                .CountAsync(t => (t.ToUserId == userId || t.FromUserId == userId) &&
                               t.CreatedAt >= startDate &&
                               t.CreatedAt <= endDate &&
                               t.Status == TransactionStatus.Completed);

            return (decimal)transactionCount / timeWindowDays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate transaction frequency for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private ReportPeriodInfo CreateReportPeriodInfo(CreditReportRequest request)
    {
        var dayCount = (request.EndDate - request.StartDate).Days + 1;

        return new ReportPeriodInfo
        {
            Type = request.PeriodType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DisplayName = GetPeriodDisplayName(request.PeriodType, request.StartDate, request.EndDate),
            DayCount = dayCount
        };
    }

    private string GetPeriodDisplayName(ReportPeriodType type, DateTime startDate, DateTime endDate)
    {
        return type switch
        {
            ReportPeriodType.Monthly => $"{startDate:MMMM yyyy}",
            ReportPeriodType.Quarterly => $"Q{(startDate.Month - 1) / 3 + 1} {startDate.Year}",
            ReportPeriodType.Annual => $"{startDate.Year}",
            ReportPeriodType.Custom => $"{startDate:MMM d} - {endDate:MMM d, yyyy}",
            _ => "Unknown Period"
        };
    }

    private int CalculateTrendMonths(ReportPeriodType periodType)
    {
        return periodType switch
        {
            ReportPeriodType.Monthly => 12,
            ReportPeriodType.Quarterly => 8, // 2 years of quarters
            ReportPeriodType.Annual => 5,    // 5 years
            ReportPeriodType.Custom => 12,
            _ => 12
        };
    }

    private int CalculateDataPointsAnalyzed(CreditSummaryReport report)
    {
        return report.Summary.TransactionCount +
               report.CategoryBreakdowns.Count +
               report.ProjectBreakdowns.Count +
               report.TrendData.Count +
               (report.TransactionDetails?.Count ?? 0);
    }

    private List<int> GenerateMonthRange(int startMonth, int endMonth)
    {
        var months = new List<int>();
        var current = startMonth;

        while (current <= endMonth)
        {
            months.Add(current);

            // Move to next month
            var year = current / 100;
            var month = current % 100;

            if (month == 12)
            {
                current = (year + 1) * 100 + 1;
            }
            else
            {
                current = year * 100 + month + 1;
            }
        }

        return months;
    }

    private (int year, int month) ParseReportMonth(int reportMonth)
    {
        return (reportMonth / 100, reportMonth % 100);
    }

    private async Task<int> CalculateBalanceAtDateAsync(Guid userId, DateTime date)
    {
        // This would calculate the balance at a specific point in time
        // For now, return current balance as a placeholder
        var balance = await _creditWalletService.GetBalanceAsync(userId);
        return balance ?? 0;
    }

    private async Task<List<CreditTransaction>> GetUserTransactionsForPeriodAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        // PERFORMANCE FIX: Use AsNoTracking for read-only reporting queries
        return await _context.CreditTransactions
            .AsNoTracking()
            .Where(t => (t.ToUserId == userId || t.FromUserId == userId) &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt < endDate &&
                       t.Status == TransactionStatus.Completed)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }

    private async Task<List<Project>> GetUserProjectsForPeriodAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        // PERFORMANCE FIX: Use AsNoTracking for read-only reporting queries
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.ClientId == userId && // Simplified to only client projects for now
                       p.CreatedAt >= startDate &&
                       p.CreatedAt < endDate)
            .ToListAsync();
    }

    private bool IsIncomingTransaction(CreditTransaction transaction)
    {
        // This would need the current user ID to determine direction
        // For now, using transaction type as indicator
        return IsIncomingTransactionType(transaction.Type);
    }

    private bool IsIncomingTransactionType(CreditTransactionType type)
    {
        return type switch
        {
            CreditTransactionType.StartingCredit => true,
            CreditTransactionType.ProjectPayment => true,
            CreditTransactionType.BonusPayment => true,
            CreditTransactionType.Refund => true,
            _ => false
        };
    }

    // Additional helper methods would be implemented here...
    // This is a partial implementation to demonstrate the structure

    #endregion

    // Additional helper methods not already defined in the helpers file
}