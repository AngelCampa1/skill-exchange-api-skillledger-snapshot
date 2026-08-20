using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using System.Text.Json;

namespace SkillLedger.Infrastructure.Services;

/// <summary>
/// Helper methods for FinancialReportingService
/// Contains utility and calculation methods
/// </summary>
public partial class FinancialReportingService
{
    #region Report Data Population

    private async Task PopulateReportDataAsync(
        UserCreditReport report,
        List<CreditTransaction> transactions,
        int startingBalance,
        int endingBalance,
        DateTime monthStart,
        DateTime monthEnd)
    {
        // Basic aggregations
        var incomingTransactions = transactions.Where(t => IsIncomingTransaction(t)).ToList();
        var outgoingTransactions = transactions.Where(t => !IsIncomingTransaction(t)).ToList();

        report.TotalEarned = incomingTransactions.Sum(t => t.Amount);
        report.TotalSpent = outgoingTransactions.Sum(t => t.Amount);
        report.TransactionCount = transactions.Count;
        report.StartingBalance = startingBalance;
        report.EndingBalance = endingBalance;

        // Calculate peak and lowest balances (simplified - would need daily calculations)
        report.PeakBalance = Math.Max(startingBalance, endingBalance);
        report.LowestBalance = Math.Min(startingBalance, endingBalance);

        // Transaction size calculations
        if (transactions.Any())
        {
            report.LargestIncomingTransaction = incomingTransactions.Any() ? incomingTransactions.Max(t => t.Amount) : 0;
            report.LargestOutgoingTransaction = outgoingTransactions.Any() ? outgoingTransactions.Max(t => t.Amount) : 0;
        }

        // Project-related calculations
        var projectTransactions = transactions.Where(t => t.ProjectId != null).ToList();
        report.UniqueProjectsCount = projectTransactions.Select(t => t.ProjectId).Distinct().Count();

        // Completed projects (simplified - would need project status)
        report.CompletedProjectsCount = await GetCompletedProjectsCountAsync(report.UserId, monthStart, monthEnd);

        // Categorized data (JSON format)
        report.EarningsByType = JsonSerializer.Serialize(
            incomingTransactions.GroupBy(t => t.Type).ToDictionary(g => g.Key.ToString(), g => g.Sum(t => t.Amount))
        );

        report.SpendingByType = JsonSerializer.Serialize(
            outgoingTransactions.GroupBy(t => t.Type).ToDictionary(g => g.Key.ToString(), g => g.Sum(t => t.Amount))
        );

        report.ProjectEarnings = JsonSerializer.Serialize(
            projectTransactions.Where(t => IsIncomingTransaction(t))
                .GroupBy(t => t.ProjectId!.Value)
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(t => t.Amount))
        );

        // Recalculate derived fields
        report.RecalculateFields();
        report.UpdateTimestamp();
    }

    #endregion

    #region Financial Summary Generation

    private async Task<FinancialSummary> GenerateFinancialSummaryAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
        var incomingTransactions = transactions.Where(t => IsIncomingTransaction(t)).ToList();
        var outgoingTransactions = transactions.Where(t => !IsIncomingTransaction(t)).ToList();

        var summary = new FinancialSummary
        {
            StartingBalance = await CalculateBalanceAtDateAsync(userId, startDate),
            EndingBalance = await CalculateBalanceAtDateAsync(userId, endDate),
            TotalEarned = incomingTransactions.Sum(t => t.Amount),
            TotalSpent = outgoingTransactions.Sum(t => t.Amount),
            TransactionCount = transactions.Count,
            PeakBalance = 0, // Would need daily balance calculations
            LowestBalance = 0, // Would need daily balance calculations
        };


        if (transactions.Any())
        {
            summary.AverageTransactionSize = (decimal)(summary.TotalEarned + summary.TotalSpent) / transactions.Count;
            summary.LargestSingleEarning = incomingTransactions.Any() ? incomingTransactions.Max(t => t.Amount) : 0;
            summary.LargestSingleExpense = outgoingTransactions.Any() ? outgoingTransactions.Max(t => t.Amount) : 0;
        }

        return summary;
    }

    #endregion

    #region Transaction Summaries

    private async Task<List<TransactionSummary>> GetTransactionSummariesAsync(
        Guid userId,
        DateTime startDate,
        DateTime endDate,
        List<CreditTransactionType>? typeFilter)
    {
        var query = _context.CreditTransactions
            .Where(t => (t.ToUserId == userId || t.FromUserId == userId) &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate &&
                       t.Status == TransactionStatus.Completed);

        if (typeFilter?.Any() == true)
        {
            query = query.Where(t => typeFilter.Contains(t.Type));
        }

        // PERFORMANCE FIX: Add AsNoTracking for read-only query and AsSplitQuery to prevent future cartesian explosion
        var transactions = await query
            .Include(t => t.Project)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(t => new TransactionSummary
        {
            TransactionId = t.Id,
            Type = t.Type,
            Amount = t.Amount,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            Status = t.Status.ToString(),
            IsIncoming = t.ToUserId == userId,
            ProjectId = t.ProjectId,
            ProjectTitle = t.Project?.Title
        }).ToList();
    }

    #endregion

    #region Balance and Analytics

    private async Task<BalanceAnalytics> GetBalanceAnalyticsAsync(Guid userId)
    {
        var wallet = await _creditWalletService.GetWalletAsync(userId);
        if (wallet == null)
        {
            return new BalanceAnalytics();
        }

        // Calculate balance change over last 30 days
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var oldBalance = await CalculateBalanceAtDateAsync(userId, thirtyDaysAgo);

        var balanceChange = wallet.Balance - oldBalance;
        var changePercentage = oldBalance > 0 ? (decimal)balanceChange / oldBalance * 100 : 0;

        // Estimate days of spending remaining
        var avgDailySpending = await CalculateAverageDailySpendingAsync(userId, 30);
        var daysRemaining = avgDailySpending > 0 ? wallet.AvailableBalance / (int)avgDailySpending : int.MaxValue;

        return new BalanceAnalytics
        {
            CurrentBalance = wallet.Balance,
            AvailableBalance = wallet.AvailableBalance,
            PendingBalance = wallet.PendingBalance,
            BalanceChangePercentage = changePercentage,
            TrendDirection = balanceChange > 0 ? "up" : balanceChange < 0 ? "down" : "stable",
            DaysOfSpendingRemaining = Math.Min(daysRemaining, 999) // Cap at 999 days
        };
    }

    private async Task<SpendingAnalytics> GetSpendingAnalyticsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
        var spendingTransactions = transactions.Where(t => !IsIncomingTransaction(t)).ToList();

        var totalSpent = spendingTransactions.Sum(t => t.Amount);
        var days = (endDate - startDate).Days;
        var dailyAverage = days > 0 ? (decimal)totalSpent / days : 0;

        var spendingByCategory = spendingTransactions
            .GroupBy(t => GetTransactionTypeDisplayName(t.Type))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var dailyTrends = await GetDailySpendingTrendsAsync(userId, startDate, endDate);

        return new SpendingAnalytics
        {
            TotalSpent = totalSpent,
            DailyAverageSpending = dailyAverage,
            SpendingByCategory = spendingByCategory,
            DailySpendingTrend = dailyTrends,
            LargestExpense = spendingTransactions.Any() ? spendingTransactions.Max(t => t.Amount) : 0,
            BudgetRemaining = 1500 - totalSpent, // Default budget
            BudgetUtilizationPercent = totalSpent / 1500m * 100
        };
    }

    private async Task<EarningAnalytics> GetEarningAnalyticsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
        var earningTransactions = transactions.Where(t => IsIncomingTransaction(t)).ToList();

        var totalEarned = earningTransactions.Sum(t => t.Amount);
        var days = (endDate - startDate).Days;
        var dailyAverage = days > 0 ? (decimal)totalEarned / days : 0;

        var earningsByCategory = earningTransactions
            .GroupBy(t => GetTransactionTypeDisplayName(t.Type))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var dailyTrends = await GetDailyEarningTrendsAsync(userId, startDate, endDate);
        var growthRate = await CalculateEarningGrowthRateAsync(userId, 90);

        return new EarningAnalytics
        {
            TotalEarned = totalEarned,
            DailyAverageEarnings = dailyAverage,
            EarningsByCategory = earningsByCategory,
            DailyEarningTrend = dailyTrends,
            LargestEarning = earningTransactions.Any() ? earningTransactions.Max(t => t.Amount) : 0,
            EarningGrowthRate = growthRate,
            GoalProgress = totalEarned // Assuming monthly goal of current earnings
        };
    }

    #endregion

    #region Dashboard Helpers

    private async Task<string> CalculateBalanceChangeIndicatorAsync(Guid userId)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var oldBalance = await CalculateBalanceAtDateAsync(userId, sevenDaysAgo);
        var currentBalance = await _creditWalletService.GetBalanceAsync(userId) ?? 0;

        return currentBalance > oldBalance ? "up" : currentBalance < oldBalance ? "down" : "stable";
    }

    private async Task<decimal> CalculateBalanceChangePercentAsync(Guid userId)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var oldBalance = await CalculateBalanceAtDateAsync(userId, sevenDaysAgo);
        var currentBalance = await _creditWalletService.GetBalanceAsync(userId) ?? 0;

        if (oldBalance == 0) return 0;
        return ((decimal)currentBalance - oldBalance) / oldBalance * 100;
    }

    private async Task<RecentActivitySummary> GetRecentActivitySummaryAsync(Guid userId)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var now = DateTime.UtcNow;

        var last7DaysTransactions = await GetUserTransactionsForPeriodAsync(userId, sevenDaysAgo, now);
        var last30DaysProjects = await GetCompletedProjectsCountAsync(userId, thirtyDaysAgo, now);

        var recentTransactions = last7DaysTransactions
            .Take(5)
            .Select(t => new RecentTransaction
            {
                Id = t.Id,
                Type = GetTransactionTypeDisplayName(t.Type),
                Amount = t.Amount,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                IsIncoming = IsIncomingTransaction(t),
                Status = t.Status.ToString()
            }).ToList();

        return new RecentActivitySummary
        {
            TransactionsLast7Days = last7DaysTransactions.Count,
            EarningsLast7Days = last7DaysTransactions.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount),
            SpendingLast7Days = last7DaysTransactions.Where(t => !IsIncomingTransaction(t)).Sum(t => t.Amount),
            ProjectsCompletedLast30Days = last30DaysProjects,
            RecentTransactions = recentTransactions
        };
    }

    private async Task<MonthlyPerformance> GetMonthlyPerformanceAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var previousMonthEnd = currentMonthStart.AddDays(-1);

        var currentMonthTransactions = await GetUserTransactionsForPeriodAsync(userId, currentMonthStart, now);
        var previousMonthTransactions = await GetUserTransactionsForPeriodAsync(userId, previousMonthStart, previousMonthEnd.AddDays(1));

        var currentEarnings = currentMonthTransactions.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount);
        var currentSpending = currentMonthTransactions.Where(t => !IsIncomingTransaction(t)).Sum(t => t.Amount);
        var previousEarnings = previousMonthTransactions.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount);
        var previousSpending = previousMonthTransactions.Where(t => !IsIncomingTransaction(t)).Sum(t => t.Amount);

        var earningsGrowth = previousEarnings > 0 ? ((decimal)currentEarnings - previousEarnings) / previousEarnings * 100 : 0;
        var spendingGrowth = previousSpending > 0 ? ((decimal)currentSpending - previousSpending) / previousSpending * 100 : 0;

        return new MonthlyPerformance
        {
            CurrentMonthEarnings = currentEarnings,
            CurrentMonthSpending = currentSpending,
            PreviousMonthEarnings = previousEarnings,
            PreviousMonthSpending = previousSpending,
            EarningsGrowth = earningsGrowth,
            SpendingGrowth = spendingGrowth,
            ProjectsCompleted = await GetCompletedProjectsCountAsync(userId, currentMonthStart, now)
        };
    }

    private async Task<GoalProgress> GetGoalProgressAsync(Guid userId)
    {
        // Calculate goal progress directly without calling GetGoalTrackingProgressAsync to avoid circular reference
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

        // Default goals
        const int defaultEarningGoal = 2000;
        const int defaultSpendingBudget = 1500;
        const int defaultProjectGoal = 3;

        var earningProgress = defaultEarningGoal > 0 ? (decimal)monthlyEarnings / defaultEarningGoal * 100 : 0;
        var spendingProgress = defaultSpendingBudget > 0 ? (decimal)monthlySpending / defaultSpendingBudget * 100 : 0;
        var projectProgress = defaultProjectGoal > 0 ? (decimal)completedProjects / defaultProjectGoal * 100 : 0;

        return new GoalProgress
        {
            EarningGoalProgress = earningProgress,
            SpendingBudgetProgress = spendingProgress,
            ProjectGoalProgress = projectProgress,
            OnTrackForGoals = earningProgress >= 25 && spendingProgress <= 75,
            GoalStatus = earningProgress >= 25 && spendingProgress <= 75 ? "On Track" : "Needs Attention"
        };
    }

    private async Task<TrendIndicators> GetTrendIndicatorsAsync(Guid userId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var fifteenDaysAgo = DateTime.UtcNow.AddDays(-15);
        var now = DateTime.UtcNow;

        var firstHalf = await GetUserTransactionsForPeriodAsync(userId, thirtyDaysAgo, fifteenDaysAgo);
        var secondHalf = await GetUserTransactionsForPeriodAsync(userId, fifteenDaysAgo, now);

        var firstHalfEarnings = firstHalf.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount);
        var secondHalfEarnings = secondHalf.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount);
        var firstHalfSpending = firstHalf.Where(t => !IsIncomingTransaction(t)).Sum(t => t.Amount);
        var secondHalfSpending = secondHalf.Where(t => !IsIncomingTransaction(t)).Sum(t => t.Amount);

        var earningTrend = secondHalfEarnings > firstHalfEarnings ? "up" : secondHalfEarnings < firstHalfEarnings ? "down" : "stable";
        var spendingTrend = secondHalfSpending > firstHalfSpending ? "up" : secondHalfSpending < firstHalfSpending ? "down" : "stable";
        var activityTrend = secondHalf.Count > firstHalf.Count ? "up" : secondHalf.Count < firstHalf.Count ? "down" : "stable";

        var healthScore = CalculateHealthScore(earningTrend, spendingTrend, activityTrend);

        return new TrendIndicators
        {
            EarningTrend = earningTrend,
            SpendingTrend = spendingTrend,
            ActivityTrend = activityTrend,
            OverallHealthScore = healthScore
        };
    }

    private async Task<List<DashboardAlert>> GetDashboardAlertsAsync(Guid userId)
    {
        var alerts = new List<DashboardAlert>();

        // Get budget alerts
        var goalAlerts = await CheckBudgetGoalAlertsAsync(userId);
        alerts.AddRange(goalAlerts.Select(ga => new DashboardAlert
        {
            Type = ga.Type,
            Title = "Budget Alert",
            Message = ga.Message,
            CreatedAt = DateTime.UtcNow,
            IsUrgent = ga.IsUrgent
        }));

        // Check for low balance
        var balance = await _creditWalletService.GetBalanceAsync(userId) ?? 0;
        if (balance < 100)
        {
            alerts.Add(new DashboardAlert
            {
                Type = "warning",
                Title = "Low Balance",
                Message = "Your credit balance is running low. Consider completing more projects.",
                CreatedAt = DateTime.UtcNow,
                IsUrgent = balance < 50
            });
        }

        return alerts;
    }

    private List<QuickAction> GetQuickActionsForUser(Guid userId)
    {
        return new List<QuickAction>
        {
            new QuickAction
            {
                Id = "view_projects",
                Title = "View Available Projects",
                Description = "Browse projects to earn credits",
                Icon = "projects",
                ActionUrl = "/projects",
                IsEnabled = true
            },
            new QuickAction
            {
                Id = "transfer_credits",
                Title = "Transfer Credits",
                Description = "Send credits to another user",
                Icon = "transfer",
                ActionUrl = "/transfer",
                IsEnabled = true
            },
            new QuickAction
            {
                Id = "export_report",
                Title = "Export Financial Report",
                Description = "Download your transaction history",
                Icon = "download",
                ActionUrl = "/reports/export",
                IsEnabled = true
            }
        };
    }

    #endregion

    #region Utility Methods

    private async Task<int> GetCompletedProjectsCountAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Projects
            .CountAsync(p => (p.ClientId == userId) && // Using ClientId as available
                           p.Status == ProjectStatus.Completed &&
                           p.UpdatedAt >= startDate &&
                           p.UpdatedAt <= endDate);
    }

    private async Task<int> GetUserEarningsForPeriodAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.CreditTransactions
            .Where(t => t.ToUserId == userId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt < endDate &&
                       t.Status == TransactionStatus.Completed)
            .SumAsync(t => t.Amount);
    }

    private async Task<decimal> CalculateAverageDailySpendingAsync(Guid userId, int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var spending = await _context.CreditTransactions
            .Where(t => t.FromUserId == userId &&
                       t.CreatedAt >= startDate &&
                       t.Status == TransactionStatus.Completed)
            .SumAsync(t => t.Amount);

        return (decimal)spending / days;
    }

    private async Task<List<SpendingTrend>> GetDailySpendingTrendsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
        var spendingTransactions = transactions.Where(t => !IsIncomingTransaction(t));

        return spendingTransactions
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new SpendingTrend
            {
                Date = g.Key,
                Amount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Category = "Daily"
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private async Task<List<EarningTrend>> GetDailyEarningTrendsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var transactions = await GetUserTransactionsForPeriodAsync(userId, startDate, endDate);
        var earningTransactions = transactions.Where(t => IsIncomingTransaction(t));

        return earningTransactions
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new EarningTrend
            {
                Date = g.Key,
                Amount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Category = "Daily"
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private string GetTransactionTypeDisplayName(CreditTransactionType type)
    {
        return type switch
        {
            CreditTransactionType.StartingCredit => "Starting Credit",
            CreditTransactionType.ProjectPayment => "Project Payment",
            CreditTransactionType.EscrowDeposit => "Project Escrow",
            CreditTransactionType.EscrowRelease => "Escrow Release",
            CreditTransactionType.DirectPayment => "Direct Payment",
            CreditTransactionType.BonusPayment => "Bonus",
            CreditTransactionType.Penalty => "Penalty",
            CreditTransactionType.Refund => "Refund",
            CreditTransactionType.PlatformFee => "Platform Fee",
            CreditTransactionType.Adjustment => "Adjustment",
            _ => type.ToString()
        };
    }

    private decimal CalculateEarningEfficiency(List<CreditTransaction> transactions)
    {
        var totalEarnings = transactions.Where(t => IsIncomingTransaction(t)).Sum(t => t.Amount);
        var totalTime = transactions.Count; // Simplified - would use actual time data

        return totalTime > 0 ? (decimal)totalEarnings / totalTime : 0;
    }

    private decimal CalculateProjectCompletionRate(List<Project> projects)
    {
        if (!projects.Any()) return 0;

        var completedCount = projects.Count(p => p.Status == ProjectStatus.Completed);
        return (decimal)completedCount / projects.Count * 100;
    }

    private int CalculateConsistencyScore(List<CreditTransaction> transactions)
    {
        // Simplified consistency calculation based on transaction frequency
        if (transactions.Count < 2) return 0;

        var days = transactions.Max(t => t.CreatedAt).Subtract(transactions.Min(t => t.CreatedAt)).Days;
        if (days == 0) return 100;

        var averageTransactionsPerDay = (decimal)transactions.Count / days;

        // Score based on consistent daily activity
        return averageTransactionsPerDay switch
        {
            >= 2.0m => 100,
            >= 1.0m => 80,
            >= 0.5m => 60,
            >= 0.2m => 40,
            _ => 20
        };
    }

    private string CalculatePerformanceRating(PerformanceMetrics metrics)
    {
        var score = (metrics.EarningEfficiency * 0.3m) +
                    (metrics.ProjectCompletionRate * 0.3m) +
                    (metrics.ConsistencyScore * 0.4m);

        return score switch
        {
            >= 90 => "Excellent",
            >= 75 => "Good",
            >= 60 => "Fair",
            >= 40 => "Needs Improvement",
            _ => "Poor"
        };
    }

    private decimal CalculateProjectValue(Project project)
    {
        // Simplified - would calculate based on payments received
        return project.CreditBudget;
    }

    private string GenerateRecommendedAction(List<CreditTransaction> transactions, List<Project> projects)
    {
        if (!transactions.Any())
            return "Start participating in projects to earn credits";

        var recentEarnings = transactions
            .Where(t => IsIncomingTransaction(t) && t.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .Sum(t => t.Amount);

        if (recentEarnings == 0)
            return "Consider applying to active projects to boost your earnings";

        return "Great activity! Keep up the consistent project participation";
    }

    private List<string> GenerateKeyInsights(List<CreditTransaction> transactions, List<Project> projects, ActivityInsights insights)
    {
        var keyInsights = new List<string>();

        if (insights.MostActiveDay != "No activity")
        {
            keyInsights.Add($"You're most active on {insights.MostActiveDay}s");
        }

        if (insights.AverageProjectValue > 0)
        {
            keyInsights.Add($"Your average project value is {insights.AverageProjectValue:C}");
        }

        if (insights.TransactionFrequency > 1)
        {
            keyInsights.Add("You maintain high transaction activity");
        }
        else if (insights.TransactionFrequency < 0.5m)
        {
            keyInsights.Add("Consider increasing your platform activity");
        }

        return keyInsights;
    }

    private string CalculateHealthScore(string earningTrend, string spendingTrend, string activityTrend)
    {
        var score = 0;

        // Earnings trend scoring
        if (earningTrend == "up") score += 40;
        else if (earningTrend == "stable") score += 20;

        // Spending trend scoring (inverse - lower is better)
        if (spendingTrend == "down") score += 30;
        else if (spendingTrend == "stable") score += 20;

        // Activity trend scoring
        if (activityTrend == "up") score += 30;
        else if (activityTrend == "stable") score += 15;

        return score switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Needs Attention"
        };
    }

    #endregion

    #region Report Management (Stub implementations)

    public async Task<List<UserCreditReport>> GetExistingMonthlyReportsAsync(Guid userId, int? startMonth = null, int? endMonth = null)
    {
        var query = _context.UserCreditReports.Where(r => r.UserId == userId);

        if (startMonth.HasValue)
            query = query.Where(r => r.ReportMonth >= startMonth.Value);

        if (endMonth.HasValue)
            query = query.Where(r => r.ReportMonth <= endMonth.Value);

        return await query.OrderBy(r => r.ReportMonth).ToListAsync();
    }

    public async Task<int> CleanupOldReportsAsync(int retentionMonths = 36)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-retentionMonths);
        var cutoffMonth = cutoffDate.Year * 100 + cutoffDate.Month;

        // PERFORMANCE FIX: Use batch deletion to avoid loading all old reports into memory
        const int batchSize = 500;
        int totalDeleted = 0;

        while (true)
        {
            var oldReports = await _context.UserCreditReports
                .Where(r => r.ReportMonth < cutoffMonth)
                .Take(batchSize)
                .ToListAsync();

            if (oldReports.Count == 0)
                break;

            _context.UserCreditReports.RemoveRange(oldReports);
            await _context.SaveChangesAsync();
            totalDeleted += oldReports.Count;

            // Exit if we deleted fewer than the batch size (indicates we're done)
            if (oldReports.Count < batchSize)
                break;
        }

        return totalDeleted;
    }

    public async Task<int> RecalculateUserReportsAsync(Guid userId, int? startMonth = null, int? endMonth = null)
    {
        var reports = await GetExistingMonthlyReportsAsync(userId, startMonth, endMonth);
        var recalculatedCount = 0;

        foreach (var report in reports)
        {
            if (!report.IsFinalized)
            {
                await GenerateMonthlyReportAsync(userId, report.ReportMonth, true);
                recalculatedCount++;
            }
        }

        return recalculatedCount;
    }

    public async Task<bool> FinalizeMonthlyReportAsync(Guid userId, int reportMonth)
    {
        var report = await _context.UserCreditReports
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReportMonth == reportMonth);

        if (report == null) return false;

        report.FinalizeReport();
        await _context.SaveChangesAsync();

        return true;
    }

    // CS1998 FIX: Removed async keyword - stub method is synchronous, returns completed Task
    public Task<SystemFinancialAnalytics> GenerateSystemAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        // System-wide analytics implementation would go here
        // This is a stub for the interface requirement
        return Task.FromResult(new SystemFinancialAnalytics
        {
            GeneratedAt = DateTime.UtcNow
        });
    }

    // CS1998 FIX: Removed async keyword - stub method is synchronous, returns completed Task
    public Task<List<TopUserEarnings>> GetTopEarningUsersAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        // Top earners implementation would go here
        return Task.FromResult(new List<TopUserEarnings>());
    }

    // CS1998 FIX: Removed async keyword - stub method is synchronous, returns completed Task
    public Task<PlatformHealthMetrics> CalculatePlatformHealthMetricsAsync()
    {
        // Platform health metrics implementation would go here
        return Task.FromResult(new PlatformHealthMetrics
        {
            LastCalculated = DateTime.UtcNow
        });
    }

    // CS1998 FIX: Removed async keyword - stub method is synchronous, returns completed Task
    public Task<DataIntegrityReport> ValidateReportIntegrityAsync(Guid userId, int? reportMonth = null)
    {
        // Data integrity validation implementation would go here
        return Task.FromResult(new DataIntegrityReport
        {
            UserId = userId,
            IsValid = true,
            ValidatedAt = DateTime.UtcNow
        });
    }

    // CS1998 FIX: Removed async keyword - stub method is synchronous, returns completed Task
    public Task<ReportReconciliationResult> ReconcileReportDataAsync(Guid userId, int reportMonth)
    {
        // Report reconciliation implementation would go here
        return Task.FromResult(new ReportReconciliationResult
        {
            UserId = userId,
            ReportMonth = reportMonth,
            IsReconciled = true,
            ReconciledAt = DateTime.UtcNow
        });
    }

    // CS1998 FIX: Removed async keyword - stub method is synchronous, returns completed Task
    public Task<int> GenerateMissingReportsAsync(Guid? userId = null)
    {
        // Missing reports generation implementation would go here
        return Task.FromResult(0);
    }

    #endregion
}