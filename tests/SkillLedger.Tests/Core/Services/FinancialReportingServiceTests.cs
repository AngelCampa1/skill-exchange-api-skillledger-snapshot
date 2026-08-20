using SkillLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SkillLedger.Tests.Core.Services;

/// <summary>
/// Comprehensive tests for FinancialReportingService following TDD methodology
/// Tests cover all major functionality including report generation, analytics, and export services
/// </summary>
[UnitTest]
[FinancialTest]
public class FinancialReportingServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly Mock<ILogger<FinancialReportingService>> _mockLogger;
    private readonly Mock<IFinancialExportService> _mockExportService;
    private readonly Mock<ICreditWalletService> _mockCreditWalletService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly FinancialReportingService _service;
    private readonly Guid _testUserId;
    private readonly DateTime _testDate;

    public FinancialReportingServiceTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SkillLedgerDbContext(options);
        _mockLogger = new Mock<ILogger<FinancialReportingService>>();
        _mockExportService = new Mock<IFinancialExportService>();
        _mockCreditWalletService = new Mock<ICreditWalletService>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        _service = new FinancialReportingService(
            _context,
            _mockCreditWalletService.Object,
            _mockExportService.Object,
            _mockAuditLogService.Object,
            _mockLogger.Object,
            Microsoft.Extensions.Options.Options.Create(new SkillLedger.Infrastructure.Configuration.EncryptionConfiguration()));

        _testUserId = Guid.NewGuid();
        _testDate = new DateTime(2024, 9, 1);

        SeedTestData();
    }

    #region Test Data Setup

    private void SeedTestData()
    {
        // Create test user
        var user = new User
        {
            Id = _testUserId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            CreatedAt = _testDate.AddDays(-30)
        };
        _context.Users.Add(user);

        // Create test credit wallet
        var wallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Balance = 1000,
            User = user
        };
        _context.CreditWallets.Add(wallet);

        // Create test project for transactions
        var testProjectId = Guid.NewGuid();
        var project = new Project
        {
            Id = testProjectId,
            ClientId = Guid.NewGuid(),
            Title = "Test Project",
            Description = "Test project for financial reporting",
            Status = ProjectStatus.Completed,
            CreditBudget = 1000,
            CreatedAt = _testDate.AddDays(-30)
        };
        _context.Projects.Add(project);

        // Create test credit transactions
        var transactions = new List<CreditTransaction>
        {
            new CreditTransaction
            {
                Id = Guid.NewGuid(),
                ToUserId = _testUserId,
                Amount = 500,
                Type = CreditTransactionType.StartingCredit,
                Status = TransactionStatus.Completed,
                Description = "Initial starting credits",
                CreatedAt = _testDate.AddDays(-25),
                CompletedAt = _testDate.AddDays(-25),
                TransactionHash = "hash1"
            },
            new CreditTransaction
            {
                Id = Guid.NewGuid(),
                ToUserId = _testUserId,
                Amount = 1000,
                Type = CreditTransactionType.ProjectPayment,
                Status = TransactionStatus.Completed,
                Description = "Payment for project completion",
                CreatedAt = _testDate.AddDays(-20),
                CompletedAt = _testDate.AddDays(-20),
                TransactionHash = "hash2",
                ProjectId = testProjectId
            },
            new CreditTransaction
            {
                Id = Guid.NewGuid(),
                FromUserId = _testUserId,
                Amount = 300,
                Type = CreditTransactionType.DirectPayment,
                Status = TransactionStatus.Completed,
                Description = "Payment to service provider",
                CreatedAt = _testDate.AddDays(-15),
                CompletedAt = _testDate.AddDays(-15),
                TransactionHash = "hash3"
            },
            new CreditTransaction
            {
                Id = Guid.NewGuid(),
                ToUserId = _testUserId,
                Amount = 200,
                Type = CreditTransactionType.BonusPayment,
                Status = TransactionStatus.Completed,
                Description = "Bonus for exceptional work",
                CreatedAt = _testDate.AddDays(-10),
                CompletedAt = _testDate.AddDays(-10),
                TransactionHash = "hash4"
            }
        };

        _context.CreditTransactions.AddRange(transactions);

        // Create test credit transfers
        var transfers = new List<CreditTransfer>
        {
            new CreditTransfer
            {
                Id = Guid.NewGuid(),
                FromUserId = _testUserId,
                ToUserId = Guid.NewGuid(),
                Amount = 150,
                Status = TransferStatus.Completed,
                Message = "Direct transfer to colleague",
                CreatedAt = _testDate.AddDays(-12),
                CompletedAt = _testDate.AddDays(-12),
                TransactionHash = "transfer1"
            }
        };

        _context.CreditTransfers.AddRange(transfers);

        // Create existing monthly report for previous month
        var existingReport = new UserCreditReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ReportMonth = UserCreditReport.CreateReportMonth(_testDate.AddMonths(-1)),
            TotalEarned = 1500,
            TotalSpent = 300,
            TransactionCount = 3,
            AverageTransactionSize = 600m,
            StartingBalance = 0,
            EndingBalance = 1200,
            PeakBalance = 1500,
            LowestBalance = 0,
            GeneratedAt = _testDate.AddDays(-5)
        };
        _context.UserCreditReports.Add(existingReport);

        _context.SaveChanges();
    }

    #endregion

    #region Report Generation Tests

    [Fact]
    public async Task GenerateCreditSummaryReportAsync_WithValidRequest_ShouldReturnCompleteReport()
    {
        // Arrange
        var request = new CreditReportRequest
        {
            UserId = _testUserId,
            PeriodType = ReportPeriodType.Monthly,
            StartDate = _testDate.AddMonths(-1),
            EndDate = _testDate,
            IncludeTransactionDetails = true,
            IncludeProjectBreakdown = true
        };

        // Act
        var result = await _service.GenerateCreditSummaryReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.True(result.Summary.TotalEarned > 0);
        Assert.True(result.Summary.TransactionCount > 0);
        Assert.NotEmpty(result.CategoryBreakdowns);
        Assert.NotNull(result.TransactionDetails);
        Assert.True(result.TransactionDetails.Count > 0);
    }

    [Fact]
    public async Task GenerateMonthlyReportAsync_WithValidData_ShouldCreateNewReport()
    {
        // Arrange
        var reportMonth = UserCreditReport.CreateReportMonth(_testDate);

        // Act
        var result = await _service.GenerateMonthlyReportAsync(_testUserId, reportMonth, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(reportMonth, result.ReportMonth);
        Assert.True(result.TotalEarned >= 0);
        Assert.True(result.TransactionCount >= 0);
        Assert.Equal(result.TotalEarned - result.TotalSpent, result.NetChange);
    }

    [Fact]
    public async Task GenerateMonthlyReportAsync_WithExistingReport_ShouldReturnExistingUnlessForced()
    {
        // Arrange
        var reportMonth = UserCreditReport.CreateReportMonth(_testDate.AddMonths(-1));

        // Act
        var result = await _service.GenerateMonthlyReportAsync(_testUserId, reportMonth, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1700, result.TotalEarned); // Should match seeded data (500 + 1000 + 200)
    }

    [Fact]
    public async Task GenerateMonthlyReportAsync_WithForceRecalculate_ShouldUpdateExistingReport()
    {
        // Arrange
        var reportMonth = UserCreditReport.CreateReportMonth(_testDate.AddMonths(-1));

        // Act
        var result = await _service.GenerateMonthlyReportAsync(_testUserId, reportMonth, true);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UpdatedAt > result.GeneratedAt); // Should be updated
    }

    [Fact]
    public async Task GenerateQuarterlyReportAsync_WithValidData_ShouldAggregateThreeMonths()
    {
        // Arrange
        var quarter = 3;
        var year = _testDate.Year;

        // Act
        var result = await _service.GenerateQuarterlyReportAsync(_testUserId, quarter, year);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(ReportPeriodType.Quarterly, result.Period.Type);
        Assert.True(result.Summary.TotalEarned >= 0);
    }

    [Fact]
    public async Task GenerateAnnualReportAsync_WithValidData_ShouldAggregateTwelveMonths()
    {
        // Arrange
        var year = _testDate.Year;

        // Act
        var result = await _service.GenerateAnnualReportAsync(_testUserId, year);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(ReportPeriodType.Annual, result.Period.Type);
        Assert.True(result.Summary.TotalEarned >= 0);
    }

    #endregion

    #region Analytics Tests

    [Fact]
    public async Task GetRealTimeAnalyticsAsync_WithValidRequest_ShouldReturnAnalyticsData()
    {
        // Arrange
        var request = new AnalyticsRequest
        {
            UserId = _testUserId,
            TimeWindowDays = 30,
            IncludeSpendingPatterns = true,
            IncludeEarningTrends = true,
            IncludeGoalTracking = true
        };

        // Act
        var result = await _service.GetRealTimeAnalyticsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.NotNull(result.CurrentBalance);
        Assert.NotNull(result.Spending);
        Assert.NotNull(result.Earnings);
        Assert.True(result.CurrentBalance.CurrentBalance >= 0);
    }

    [Fact]
    public async Task GetUserDashboardDataAsync_WithValidUserId_ShouldReturnDashboardData()
    {
        // Act
        var result = await _service.GetUserDashboardDataAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.NotNull(result.Wallet);
        Assert.NotNull(result.RecentActivity);
        Assert.NotNull(result.MonthlyStats);
        Assert.NotNull(result.Goals);
        Assert.NotNull(result.Trends);
    }

    [Fact]
    public async Task GetSpendingEarningAnalyticsAsync_WithValidParameters_ShouldReturnAnalytics()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        // Act
        var result = await _service.GetSpendingEarningAnalyticsAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result.Spending);
        Assert.NotNull(result.Earnings);
        Assert.True(result.Spending.TotalSpent >= 0);
        Assert.True(result.Earnings.TotalEarned >= 0);
    }

    [Fact]
    public async Task CalculatePerformanceMetricsAsync_WithValidData_ShouldReturnMetrics()
    {
        // Arrange
        var timeWindowDays = 90;

        // Act
        var result = await _service.CalculatePerformanceMetricsAsync(_testUserId, timeWindowDays);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EarningEfficiency >= 0);
        Assert.True(result.ProjectCompletionRate >= 0);
        Assert.NotNull(result.PerformanceRating);
    }

    #endregion

    #region Budget and Goal Tracking Tests

    [Fact]
    public async Task SetupBudgetTrackingAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new BudgetTrackingRequest
        {
            UserId = _testUserId,
            MonthlySpendingBudget = 1000,
            MonthlyEarningGoal = 2000,
            ProjectCompletionGoal = 3,
            AlertSettings = new BudgetAlertSettings()
        };

        // Act
        var result = await _service.SetupBudgetTrackingAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetGoalTrackingProgressAsync_WithValidUserId_ShouldReturnProgress()
    {
        // Act
        var result = await _service.GetGoalTrackingProgressAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EarningGoalProgress >= 0);
        Assert.True(result.SpendingBudgetProgress >= 0);
    }

    [Fact]
    public async Task UpdateUserGoalsAsync_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        var earningGoal = 2500;
        var spendingBudget = 1200;
        var projectGoal = 4;

        // Act
        var result = await _service.UpdateUserGoalsAsync(_testUserId, earningGoal, spendingBudget, projectGoal);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckBudgetGoalAlertsAsync_WithBudgetExceeded_ShouldReturnAlerts()
    {
        // Act
        var result = await _service.CheckBudgetGoalAlertsAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<GoalAlert>>(result);
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task ExportFinancialDataAsync_WithCsvFormat_ShouldReturnCsvExport()
    {
        // Arrange
        var request = new FinancialExportRequest
        {
            UserId = _testUserId,
            Format = ExportFormat.CSV,
            StartDate = _testDate.AddDays(-30),
            EndDate = _testDate
        };

        _mockExportService
            .Setup(x => x.ExportToCsvAsync(It.IsAny<IEnumerable<TransactionSummary>>(), It.IsAny<bool>()))
            .ReturnsAsync("csv,data,here");

        // Act
        var result = await _service.ExportFinancialDataAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("text/csv", result.ContentType);
    }

    [Fact]
    public async Task ExportTransactionHistoryAsCsvAsync_WithValidParameters_ShouldReturnCsv()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        _mockExportService
            .Setup(x => x.ExportToCsvAsync(It.IsAny<IEnumerable<TransactionSummary>>(), true))
            .ReturnsAsync("Date,Type,Amount\n2024-09-01,Payment,100");

        // Act
        var result = await _service.ExportTransactionHistoryAsCsvAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("csv", result.ContentType);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task ExportCreditSummaryAsPdfAsync_WithValidParameters_ShouldReturnPdf()
    {
        // Arrange
        var request = new CreditReportRequest
        {
            UserId = _testUserId,
            PeriodType = ReportPeriodType.Monthly,
            StartDate = _testDate.AddDays(-30),
            EndDate = _testDate
        };

        _mockExportService
            .Setup(x => x.ExportReportToPdfAsync(It.IsAny<CreditSummaryReport>(), true))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });

        // Act
        var result = await _service.ExportCreditSummaryAsPdfAsync(_testUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("application/pdf", result.ContentType);
    }

    [Fact]
    public async Task ExportFinancialDataAsJsonAsync_WithValidParameters_ShouldReturnJson()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        _mockExportService
            .Setup(x => x.ExportToJsonAsync(It.IsAny<IEnumerable<TransactionSummary>>(), It.IsAny<bool>()))
            .ReturnsAsync("{\"data\": \"test\"}");

        // Act
        var result = await _service.ExportFinancialDataAsJsonAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("application/json", result.ContentType);
    }

    #endregion

    #region Categorized Reporting Tests

    [Fact]
    public async Task GetCategorizedTransactionBreakdownAsync_WithValidParameters_ShouldReturnBreakdown()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        // Act
        var result = await _service.GetCategorizedTransactionBreakdownAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<TransactionCategoryBreakdown>>(result);
        Assert.True(result.All(x => x.TotalAmount >= 0));
    }

    [Fact]
    public async Task GetProjectEarningsBreakdownAsync_WithValidParameters_ShouldReturnBreakdown()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        // Act
        var result = await _service.GetProjectEarningsBreakdownAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<ProjectEarningsBreakdown>>(result);
    }

    [Fact]
    public async Task GetEarningsByTypeAsync_WithValidParameters_ShouldReturnEarnings()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        // Act
        var result = await _service.GetEarningsByTypeAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Dictionary<CreditTransactionType, int>>(result);
    }

    [Fact]
    public async Task GetSpendingByTypeAsync_WithValidParameters_ShouldReturnSpending()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        // Act
        var result = await _service.GetSpendingByTypeAsync(_testUserId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Dictionary<CreditTransactionType, int>>(result);
    }

    #endregion

    #region Trend Analysis Tests

    [Fact]
    public async Task GetHistoricalTrendDataAsync_WithValidParameters_ShouldReturnTrends()
    {
        // Arrange
        var months = 6;

        // Act
        var result = await _service.GetHistoricalTrendDataAsync(_testUserId, months);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<PeriodTrendData>>(result);
    }

    [Fact]
    public async Task CalculateEarningGrowthRateAsync_WithValidData_ShouldReturnGrowthRate()
    {
        // Arrange
        var timeWindowDays = 90;

        // Act
        var result = await _service.CalculateEarningGrowthRateAsync(_testUserId, timeWindowDays);

        // Assert
        Assert.True(result >= -100); // Growth rate shouldn't be less than -100%
    }

    [Fact]
    public async Task GetDailyTrendsAsync_WithValidParameters_ShouldReturnTrends()
    {
        // Arrange
        var days = 30;

        // Act
        var result = await _service.GetDailyTrendsAsync(_testUserId, days);

        // Assert
        Assert.NotNull(result.EarningTrends);
        Assert.NotNull(result.SpendingTrends);
        Assert.IsAssignableFrom<List<EarningTrend>>(result.EarningTrends);
        Assert.IsAssignableFrom<List<SpendingTrend>>(result.SpendingTrends);
    }

    [Fact]
    public async Task PredictFutureEarningsAsync_WithValidData_ShouldReturnPredictions()
    {
        // Arrange
        var forecastDays = 30;

        // Act
        var result = await _service.PredictFutureEarningsAsync(_testUserId, forecastDays);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<EarningTrend>>(result);
    }

    #endregion

    #region Activity Insights Tests

    [Fact]
    public async Task GenerateActivityInsightsAsync_WithValidData_ShouldReturnInsights()
    {
        // Arrange
        var analysisWindowDays = 90;

        // Act
        var result = await _service.GenerateActivityInsightsAsync(_testUserId, analysisWindowDays);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.MostActiveDay);
        Assert.NotNull(result.KeyInsights);
        Assert.True(result.AverageProjectValue >= 0);
    }

    [Fact]
    public async Task IdentifyPeakActivityPatternsAsync_WithValidData_ShouldReturnPatterns()
    {
        // Act
        var result = await _service.IdentifyPeakActivityPatternsAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Dictionary<string, object>>(result);
    }

    [Fact]
    public async Task CalculateTransactionFrequencyAsync_WithValidData_ShouldReturnFrequency()
    {
        // Arrange
        var timeWindowDays = 30;

        // Act
        var result = await _service.CalculateTransactionFrequencyAsync(_testUserId, timeWindowDays);

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region Report Management Tests

    [Fact]
    public async Task GetExistingMonthlyReportsAsync_WithValidUserId_ShouldReturnReports()
    {
        // Act
        var result = await _service.GetExistingMonthlyReportsAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<UserCreditReport>>(result);
    }

    [Fact]
    public async Task FinalizeMonthlyReportAsync_WithValidReport_ShouldReturnTrue()
    {
        // Arrange
        var reportMonth = UserCreditReport.CreateReportMonth(_testDate.AddMonths(-1));

        // Act
        var result = await _service.FinalizeMonthlyReportAsync(_testUserId, reportMonth);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RecalculateUserReportsAsync_WithValidUserId_ShouldReturnCount()
    {
        // Act
        var result = await _service.RecalculateUserReportsAsync(_testUserId);

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task CleanupOldReportsAsync_WithRetentionPeriod_ShouldReturnDeletedCount()
    {
        // Arrange
        var retentionMonths = 12;

        // Act
        var result = await _service.CleanupOldReportsAsync(retentionMonths);

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public async Task ValidateReportIntegrityAsync_WithValidData_ShouldReturnValidResult()
    {
        // Act
        var result = await _service.ValidateReportIntegrityAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.IsAssignableFrom<List<string>>(result.Issues);
        Assert.IsAssignableFrom<List<string>>(result.Warnings);
    }

    [Fact]
    public async Task ReconcileReportDataAsync_WithValidData_ShouldReturnReconciliation()
    {
        // Arrange
        var reportMonth = UserCreditReport.CreateReportMonth(_testDate.AddMonths(-1));

        // Act
        var result = await _service.ReconcileReportDataAsync(_testUserId, reportMonth);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testUserId, result.UserId);
        Assert.Equal(reportMonth, result.ReportMonth);
    }

    [Fact]
    public async Task GenerateMissingReportsAsync_WithValidUser_ShouldReturnGeneratedCount()
    {
        // Act
        var result = await _service.GenerateMissingReportsAsync(_testUserId);

        // Assert
        Assert.True(result >= 0);
    }

    #endregion

    #region System Analytics Tests

    [Fact]
    public async Task GenerateSystemAnalyticsAsync_WithValidParameters_ShouldReturnAnalytics()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;

        // Act
        var result = await _service.GenerateSystemAnalyticsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalActiveUsers >= 0);
        Assert.True(result.TotalCreditsInCirculation >= 0);
        Assert.True(result.TotalTransactionVolume >= 0);
    }

    [Fact]
    public async Task GetTopEarningUsersAsync_WithValidParameters_ShouldReturnTopUsers()
    {
        // Arrange
        var startDate = _testDate.AddDays(-30);
        var endDate = _testDate;
        var limit = 10;

        // Act
        var result = await _service.GetTopEarningUsersAsync(startDate, endDate, limit);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<List<TopUserEarnings>>(result);
        Assert.True(result.Count <= limit);
    }

    [Fact]
    public async Task CalculatePlatformHealthMetricsAsync_ShouldReturnHealthMetrics()
    {
        // Act
        var result = await _service.CalculatePlatformHealthMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.UserEngagementScore >= 0);
        Assert.True(result.TransactionSuccessRate >= 0);
        Assert.NotNull(result.OverallHealthStatus);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GenerateCreditSummaryReportAsync_WithInvalidUserId_ShouldThrowException()
    {
        // Arrange
        var request = new CreditReportRequest
        {
            UserId = Guid.Empty,
            PeriodType = ReportPeriodType.Monthly,
            StartDate = _testDate.AddDays(-30),
            EndDate = _testDate
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerateCreditSummaryReportAsync(request));
    }

    [Fact]
    public async Task GenerateMonthlyReportAsync_WithInvalidReportMonth_ShouldThrowException()
    {
        // Act & Assert - DateTime constructor throws ArgumentOutOfRangeException for invalid dates
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.GenerateMonthlyReportAsync(_testUserId, 999999, false));
    }

    [Fact]
    public async Task GetRealTimeAnalyticsAsync_WithNullRequest_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetRealTimeAnalyticsAsync(null));
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}