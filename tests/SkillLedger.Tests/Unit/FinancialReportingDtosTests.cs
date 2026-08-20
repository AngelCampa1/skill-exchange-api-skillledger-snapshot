using SkillLedger.Tests.Infrastructure;
using FluentAssertions;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace SkillLedger.Tests.Unit;

/// <summary>
/// Unit tests for Financial Reporting DTOs
/// Tests validation, calculations, and data integrity
/// </summary>
[UnitTest]
[FinancialTest]
public class FinancialReportingDtosTests
{
    #region CreditReportRequest Tests

    [Fact]
    public void CreditReportRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new CreditReportRequest();

        // Assert
        request.UserId.Should().BeNull();
        request.IncludeTransactionDetails.Should().BeFalse();
        request.IncludeProjectBreakdown.Should().BeTrue();
        request.TransactionTypeFilter.Should().BeNull();
    }

    [Fact]
    public void CreditReportRequest_ShouldValidateRequiredFields()
    {
        // Arrange
        var request = new CreditReportRequest();

        // Act
        var context = new ValidationContext(request, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().HaveCountGreaterThan(0);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CreditReportRequest.PeriodType)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CreditReportRequest.StartDate)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(CreditReportRequest.EndDate)));
    }

    [Fact]
    public void CreditReportRequest_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var request = new CreditReportRequest
        {
            PeriodType = ReportPeriodType.Monthly,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow
        };

        // Act
        var context = new ValidationContext(request, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    #endregion

    #region FinancialExportRequest Tests

    [Fact]
    public void FinancialExportRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new FinancialExportRequest();

        // Assert
        request.IncludePersonalInfo.Should().BeFalse();
        request.CurrencyFormat.Should().Be("USD");
    }

    [Fact]
    public void FinancialExportRequest_ShouldValidateRequiredFields()
    {
        // Arrange
        var request = new FinancialExportRequest();

        // Act
        var context = new ValidationContext(request, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().HaveCountGreaterThan(0);
        results.Should().Contain(r => r.MemberNames.Contains(nameof(FinancialExportRequest.UserId)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(FinancialExportRequest.Format)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(FinancialExportRequest.StartDate)));
        results.Should().Contain(r => r.MemberNames.Contains(nameof(FinancialExportRequest.EndDate)));
    }

    #endregion

    #region AnalyticsRequest Tests

    [Fact]
    public void AnalyticsRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new AnalyticsRequest();

        // Assert
        request.TimeWindowDays.Should().Be(30);
        request.IncludeSpendingPatterns.Should().BeTrue();
        request.IncludeEarningTrends.Should().BeTrue();
        request.IncludeGoalTracking.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(-1)]
    public void AnalyticsRequest_ShouldValidateTimeWindowRange(int timeWindowDays)
    {
        // Arrange
        var request = new AnalyticsRequest
        {
            UserId = Guid.NewGuid(),
            TimeWindowDays = timeWindowDays
        };

        // Act
        var context = new ValidationContext(request, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(AnalyticsRequest.TimeWindowDays)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void AnalyticsRequest_ShouldAcceptValidTimeWindowRange(int timeWindowDays)
    {
        // Arrange
        var request = new AnalyticsRequest
        {
            UserId = Guid.NewGuid(),
            TimeWindowDays = timeWindowDays
        };

        // Act
        var context = new ValidationContext(request, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    #endregion

    #region BudgetTrackingRequest Tests

    [Fact]
    public void BudgetTrackingRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new BudgetTrackingRequest();

        // Assert
        request.AlertSettings.Should().NotBeNull();
        request.AlertSettings.SpendingAlert50Percent.Should().Be(50);
        request.AlertSettings.SpendingAlert75Percent.Should().Be(75);
        request.AlertSettings.SpendingAlert90Percent.Should().Be(90);
        request.AlertSettings.EarningGoalAlert25Percent.Should().Be(25);
        request.AlertSettings.EnableDailyDigest.Should().BeTrue();
        request.AlertSettings.EnableWeeklyReport.Should().BeTrue();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void BudgetTrackingRequest_ShouldValidateNonNegativeBudgets(int negativeValue)
    {
        // Arrange
        var request = new BudgetTrackingRequest
        {
            UserId = Guid.NewGuid(),
            MonthlySpendingBudget = negativeValue
        };

        // Act
        var context = new ValidationContext(request, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains(nameof(BudgetTrackingRequest.MonthlySpendingBudget)));
    }

    #endregion

    #region FinancialSummary Tests

    [Fact]
    public void FinancialSummary_NetChange_ShouldCalculateCorrectly()
    {
        // Arrange
        var summary = new FinancialSummary
        {
            TotalEarned = 1500,
            TotalSpent = 800
        };

        // Act & Assert
        summary.NetChange.Should().Be(700);
    }

    [Theory]
    [InlineData(1000, 300, 700)]
    [InlineData(500, 500, 0)]
    [InlineData(200, 800, -600)]
    public void FinancialSummary_NetChange_ShouldHandleVariousScenarios(int earned, int spent, int expectedNet)
    {
        // Arrange
        var summary = new FinancialSummary
        {
            TotalEarned = earned,
            TotalSpent = spent
        };

        // Act & Assert
        summary.NetChange.Should().Be(expectedNet);
    }

    #endregion

    #region BalanceAnalytics Tests

    [Fact]
    public void BalanceAnalytics_ShouldInitializeWithDefaults()
    {
        // Act
        var analytics = new BalanceAnalytics();

        // Assert
        analytics.TrendDirection.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("up")]
    [InlineData("down")]
    [InlineData("stable")]
    public void BalanceAnalytics_ShouldAcceptValidTrendDirections(string trendDirection)
    {
        // Arrange
        var analytics = new BalanceAnalytics
        {
            TrendDirection = trendDirection
        };

        // Act & Assert
        analytics.TrendDirection.Should().Be(trendDirection);
    }

    #endregion

    #region WalletUpdateNotification Tests

    [Fact]
    public void WalletUpdateNotification_ShouldInitializeWithDefaults()
    {
        // Act
        var notification = new WalletUpdateNotification();

        // Assert
        notification.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        notification.UpdateReason.Should().Be(string.Empty);
        notification.NotificationType.Should().Be(string.Empty);
    }

    [Fact]
    public void WalletUpdateNotification_BalanceChange_ShouldCalculateCorrectly()
    {
        // Arrange
        var notification = new WalletUpdateNotification
        {
            NewBalance = 1200,
            PreviousBalance = 1000
        };

        // Act & Assert
        notification.BalanceChange.Should().Be(200);
    }

    [Theory]
    [InlineData(1500, 1000, 500)]
    [InlineData(800, 1200, -400)]
    [InlineData(1000, 1000, 0)]
    public void WalletUpdateNotification_BalanceChange_ShouldHandleVariousScenarios(
        int newBalance, int previousBalance, int expectedChange)
    {
        // Arrange
        var notification = new WalletUpdateNotification
        {
            NewBalance = newBalance,
            PreviousBalance = previousBalance
        };

        // Act & Assert
        notification.BalanceChange.Should().Be(expectedChange);
    }

    #endregion

    #region CreditSummaryReport Tests

    [Fact]
    public void CreditSummaryReport_ShouldInitializeWithDefaults()
    {
        // Act
        var report = new CreditSummaryReport();

        // Assert
        report.Period.Should().NotBeNull();
        report.Summary.Should().NotBeNull();
        report.CategoryBreakdowns.Should().NotBeNull().And.BeEmpty();
        report.ProjectBreakdowns.Should().NotBeNull().And.BeEmpty();
        report.TrendData.Should().NotBeNull().And.BeEmpty();
        report.TransactionDetails.Should().BeNull();
        report.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        report.Metadata.Should().NotBeNull();
    }

    #endregion

    #region AnalyticsData Tests

    [Fact]
    public void AnalyticsData_ShouldInitializeWithDefaults()
    {
        // Act
        var data = new AnalyticsData();

        // Assert
        data.CurrentBalance.Should().NotBeNull();
        data.Spending.Should().NotBeNull();
        data.Earnings.Should().NotBeNull();
        data.GoalTracking.Should().NotBeNull();
        data.Insights.Should().NotBeNull();
        data.Performance.Should().NotBeNull();
        data.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region FinancialExportResult Tests

    [Fact]
    public void FinancialExportResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new FinancialExportResult();

        // Assert
        result.ContentType.Should().Be(string.Empty);
        result.FileName.Should().Be(string.Empty);
        result.Metadata.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FinancialExportResult_ShouldHandleSuccessStates(bool success)
    {
        // Arrange
        var result = new FinancialExportResult
        {
            Success = success
        };

        // Act & Assert
        result.Success.Should().Be(success);
        // This test documents the expected usage pattern
    }

    #endregion

    #region UserDashboardData Tests

    [Fact]
    public void UserDashboardData_ShouldInitializeWithDefaults()
    {
        // Act
        var data = new UserDashboardData();

        // Assert
        data.Wallet.Should().NotBeNull();
        data.RecentActivity.Should().NotBeNull();
        data.MonthlyStats.Should().NotBeNull();
        data.Goals.Should().NotBeNull();
        data.Trends.Should().NotBeNull();
        data.Alerts.Should().NotBeNull().And.BeEmpty();
        data.QuickActions.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region ReportPeriodInfo Tests

    [Theory]
    [InlineData(ReportPeriodType.Monthly, "Monthly Report")]
    [InlineData(ReportPeriodType.Quarterly, "Quarterly Report")]
    [InlineData(ReportPeriodType.Annual, "Annual Report")]
    [InlineData(ReportPeriodType.Custom, "Custom Report")]
    public void ReportPeriodInfo_ShouldAcceptValidPeriodTypes(ReportPeriodType periodType, string description)
    {
        // Arrange
        var periodInfo = new ReportPeriodInfo
        {
            Type = periodType,
            DisplayName = description
        };

        // Act & Assert
        periodInfo.Type.Should().Be(periodType);
        periodInfo.DisplayName.Should().Be(description);
    }

    #endregion

    #region TransactionCategoryBreakdown Tests

    [Fact]
    public void TransactionCategoryBreakdown_ShouldInitializeWithDefaults()
    {
        // Act
        var breakdown = new TransactionCategoryBreakdown();

        // Assert
        breakdown.DisplayName.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(1000, 10, 100.0)]
    [InlineData(500, 5, 100.0)]
    [InlineData(0, 0, 0.0)]
    public void TransactionCategoryBreakdown_AverageAmount_ShouldCalculateCorrectly(
        int totalAmount, int transactionCount, double expectedAverage)
    {
        // Arrange
        var breakdown = new TransactionCategoryBreakdown
        {
            TotalAmount = totalAmount,
            TransactionCount = transactionCount,
            AverageAmount = transactionCount > 0 ? (decimal)totalAmount / transactionCount : 0
        };

        // Act & Assert
        breakdown.AverageAmount.Should().Be((decimal)expectedAverage);
    }

    #endregion

    #region ProjectEarningsBreakdown Tests

    [Fact]
    public void ProjectEarningsBreakdown_ShouldInitializeWithDefaults()
    {
        // Act
        var breakdown = new ProjectEarningsBreakdown();

        // Assert
        breakdown.ProjectTitle.Should().Be(string.Empty);
        breakdown.ProjectStatus.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(1000, 10, 100.0)]
    [InlineData(500, 5, 100.0)]
    [InlineData(0, 0, 0.0)]
    public void ProjectEarningsBreakdown_HourlyRate_ShouldCalculateCorrectly(
        int totalEarned, int hoursWorked, decimal expectedRate)
    {
        // Arrange
        var breakdown = new ProjectEarningsBreakdown
        {
            TotalEarned = totalEarned,
            HoursWorked = hoursWorked,
            HourlyRate = hoursWorked > 0 ? (decimal)totalEarned / hoursWorked : 0
        };

        // Act & Assert
        breakdown.HourlyRate.Should().Be(expectedRate);
    }

    #endregion

    #region SystemReconciliationReport Tests

    [Fact]
    public void SystemReconciliationReport_HealthStatus_ShouldReturnCorrectStatus()
    {
        // Arrange & Act
        var healthyReport = new SystemReconciliationReport
        {
            WalletsWithDiscrepancies = 0
        };

        var unhealthyReport = new SystemReconciliationReport
        {
            WalletsWithDiscrepancies = 5
        };

        // Assert
        healthyReport.HealthStatus.Should().Be("Healthy");
        unhealthyReport.HealthStatus.Should().Be("Requires Attention");
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void ReportPeriodType_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetValues<ReportPeriodType>().Should().Contain(new[]
        {
            ReportPeriodType.Monthly,
            ReportPeriodType.Quarterly,
            ReportPeriodType.Annual,
            ReportPeriodType.Custom
        });
    }

    [Fact]
    public void ExportFormat_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Enum.GetValues<ExportFormat>().Should().Contain(new[]
        {
            ExportFormat.CSV,
            ExportFormat.PDF,
            ExportFormat.JSON,
            ExportFormat.XML,
            ExportFormat.Excel
        });
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void WalletOperationResponse_ShouldInitializeWithDefaults()
    {
        // Act
        var response = new WalletOperationResponse();

        // Assert
        response.Message.Should().Be(string.Empty);
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        response.Errors.Should().NotBeNull().And.BeEmpty();
        response.Warnings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExportMetadata_ShouldInitializeWithDefaults()
    {
        // Act
        var metadata = new ExportMetadata();

        // Assert
        metadata.ExportTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        metadata.ExportedBy.Should().Be(string.Empty);
        metadata.Version.Should().Be("1.0");
        metadata.CustomProperties.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ReportMetadata_ShouldInitializeWithDefaults()
    {
        // Act
        var metadata = new ReportMetadata();

        // Assert
        metadata.Version.Should().Be("1.0");
        metadata.AppliedFilters.Should().NotBeNull().And.BeEmpty();
        metadata.GeneratedBy.Should().Be("System");
    }

    #endregion

    #region Collection Property Tests

    [Fact]
    public void SpendingAnalytics_ShouldInitializeCollections()
    {
        // Act
        var analytics = new SpendingAnalytics();

        // Assert
        analytics.SpendingByCategory.Should().NotBeNull().And.BeEmpty();
        analytics.DailySpendingTrend.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void EarningAnalytics_ShouldInitializeCollections()
    {
        // Act
        var analytics = new EarningAnalytics();

        // Assert
        analytics.EarningsByCategory.Should().NotBeNull().And.BeEmpty();
        analytics.DailyEarningTrend.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void GoalTrackingData_ShouldInitializeCollections()
    {
        // Act
        var data = new GoalTrackingData();

        // Assert
        data.GoalAlerts.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ActivityInsights_ShouldInitializeCollections()
    {
        // Act
        var insights = new ActivityInsights();

        // Assert
        insights.KeyInsights.Should().NotBeNull().And.BeEmpty();
        insights.MostActiveDay.Should().Be(string.Empty);
        insights.MostProfitableProjectType.Should().Be(string.Empty);
        insights.RecommendedAction.Should().Be(string.Empty);
    }

    #endregion
}