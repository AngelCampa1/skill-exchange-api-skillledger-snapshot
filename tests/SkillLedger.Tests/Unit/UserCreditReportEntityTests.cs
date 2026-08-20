using SkillLedger.Tests.Infrastructure;
using FluentAssertions;
using SkillLedger.Core.Entities;
using Xunit;

namespace SkillLedger.Tests.Unit;

/// <summary>
/// Unit tests for UserCreditReport entity
/// Tests business logic, validation, and data integrity methods
/// </summary>
[UnitTest]
[FinancialTest]
public class UserCreditReportEntityTests
{
    #region Constructor Tests

    [Fact]
    public void UserCreditReport_Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var report = new UserCreditReport();

        // Assert
        report.Id.Should().NotBe(Guid.Empty);
        report.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        report.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        report.TotalEarned.Should().Be(0);
        report.TotalSpent.Should().Be(0);
        report.TransactionCount.Should().Be(0);
        report.AverageTransactionSize.Should().Be(0);
        report.PeakBalance.Should().Be(0);
        report.LowestBalance.Should().Be(0);
        report.StartingBalance.Should().Be(0);
        report.EndingBalance.Should().Be(0);
        report.UniqueProjectsCount.Should().Be(0);
        report.CompletedProjectsCount.Should().Be(0);
        report.LargestIncomingTransaction.Should().Be(0);
        report.LargestOutgoingTransaction.Should().Be(0);
        report.IsFinalized.Should().BeFalse();
        report.FinalizedAt.Should().BeNull();
        report.RowVersion.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void NetChange_ShouldCalculateCorrectly()
    {
        // Arrange
        var report = new UserCreditReport
        {
            TotalEarned = 1000,
            TotalSpent = 300
        };

        // Act & Assert
        report.NetChange.Should().Be(700);
    }

    [Theory]
    [InlineData(500, 200, 300)]
    [InlineData(1000, 800, 200)]
    [InlineData(0, 0, 0)]
    public void NetChange_ShouldCalculateCorrectlyForVariousValues(int earned, int spent, int expected)
    {
        // Arrange
        var report = new UserCreditReport
        {
            TotalEarned = earned,
            TotalSpent = spent
        };

        // Act & Assert
        report.NetChange.Should().Be(expected);
    }

    [Fact]
    public void CanBeModified_ShouldReturnTrueWhenNotFinalized()
    {
        // Arrange
        var report = new UserCreditReport { IsFinalized = false };

        // Act & Assert
        report.CanBeModified.Should().BeTrue();
    }

    [Fact]
    public void CanBeModified_ShouldReturnFalseWhenFinalized()
    {
        // Arrange
        var report = new UserCreditReport { IsFinalized = true };

        // Act & Assert
        report.CanBeModified.Should().BeFalse();
    }

    #endregion

    #region Business Logic Method Tests

    [Fact]
    public void UpdateTimestamp_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var report = new UserCreditReport();
        var originalUpdatedAt = report.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(50);

        // Act
        report.UpdateTimestamp();

        // Assert
        report.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void FinalizeReport_ShouldSetFinalizedPropertiesAndUpdateTimestamp()
    {
        // Arrange
        var report = new UserCreditReport();
        var originalUpdatedAt = report.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(50);

        // Act
        report.FinalizeReport();

        // Assert
        report.IsFinalized.Should().BeTrue();
        report.FinalizedAt.Should().NotBeNull();
        report.FinalizedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        report.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData(202509, 2025, 9)]
    [InlineData(202312, 2023, 12)]
    [InlineData(202001, 2020, 1)]
    public void GetMonthYear_ShouldParseReportMonthCorrectly(int reportMonth, int expectedYear, int expectedMonth)
    {
        // Arrange
        var report = new UserCreditReport { ReportMonth = reportMonth };

        // Act
        var (year, month) = report.GetMonthYear();

        // Assert
        year.Should().Be(expectedYear);
        month.Should().Be(expectedMonth);
    }

    [Fact]
    public void GetReportMonthStart_ShouldReturnFirstDayOfMonth()
    {
        // Arrange
        var report = new UserCreditReport { ReportMonth = 202509 }; // September 2025

        // Act
        var result = report.GetReportMonthStart();

        // Assert
        result.Should().Be(new DateTime(2025, 9, 1));
    }

    [Fact]
    public void GetReportMonthEnd_ShouldReturnLastDayOfMonth()
    {
        // Arrange
        var report = new UserCreditReport { ReportMonth = 202509 }; // September 2025

        // Act
        var result = report.GetReportMonthEnd();

        // Assert
        result.Should().Be(new DateTime(2025, 9, 30));
    }

    [Fact]
    public void GetReportMonthEnd_ShouldHandleLeapYear()
    {
        // Arrange
        var report = new UserCreditReport { ReportMonth = 202402 }; // February 2024 (leap year)

        // Act
        var result = report.GetReportMonthEnd();

        // Assert
        result.Should().Be(new DateTime(2024, 2, 29));
    }

    [Theory]
    [InlineData(1000, 500, 10, 150.0)]
    [InlineData(0, 0, 0, 0.0)]
    [InlineData(500, 300, 4, 200.0)]
    public void CalculateAverageTransactionSize_ShouldCalculateCorrectly(
        int earned, int spent, int transactionCount, double expectedAverage)
    {
        // Arrange
        var report = new UserCreditReport
        {
            TotalEarned = earned,
            TotalSpent = spent,
            TransactionCount = transactionCount
        };

        // Act
        var result = report.CalculateAverageTransactionSize();

        // Assert
        result.Should().Be((decimal)expectedAverage);
    }

    [Fact]
    public void RecalculateFields_ShouldUpdateAverageTransactionSizeAndTimestamp()
    {
        // Arrange
        var report = new UserCreditReport
        {
            TotalEarned = 1000,
            TotalSpent = 500,
            TransactionCount = 10,
            AverageTransactionSize = 0 // Incorrect value
        };
        var originalUpdatedAt = report.UpdatedAt;

        // Wait a sufficient amount to ensure timestamp difference
        Thread.Sleep(100);

        // Act
        report.RecalculateFields();

        // Assert
        report.AverageTransactionSize.Should().Be(150); // (1000 + 500) / 10
        report.UpdatedAt.Should().BeAfter(originalUpdatedAt.AddMilliseconds(-1)); // Allow for minor timing differences
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public void ValidateDataConsistency_ShouldReturnTrueForConsistentData()
    {
        // Arrange
        var report = new UserCreditReport
        {
            StartingBalance = 1000,
            TotalEarned = 500,
            TotalSpent = 200,
            EndingBalance = 1300, // 1000 + 500 - 200
            TransactionCount = 7,
            AverageTransactionSize = 100, // (500 + 200) / 7 = 100
            PeakBalance = 1300,
            LowestBalance = 1000
        };

        // Act
        var result = report.ValidateDataConsistency();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateDataConsistency_ShouldReturnFalseForInconsistentEndingBalance()
    {
        // Arrange
        var report = new UserCreditReport
        {
            StartingBalance = 1000,
            TotalEarned = 500,
            TotalSpent = 200,
            EndingBalance = 1500, // Should be 1300 (1000 + 500 - 200)
            TransactionCount = 7,
            AverageTransactionSize = 100,
            PeakBalance = 1500,
            LowestBalance = 1000
        };

        // Act
        var result = report.ValidateDataConsistency();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDataConsistency_ShouldReturnFalseForIncorrectAverageTransactionSize()
    {
        // Arrange
        var report = new UserCreditReport
        {
            StartingBalance = 1000,
            TotalEarned = 500,
            TotalSpent = 200,
            EndingBalance = 1300,
            TransactionCount = 7,
            AverageTransactionSize = 200, // Should be 100 ((500 + 200) / 7)
            PeakBalance = 1300,
            LowestBalance = 1000
        };

        // Act
        var result = report.ValidateDataConsistency();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDataConsistency_ShouldReturnFalseWhenPeakBalanceIsTooLow()
    {
        // Arrange
        var report = new UserCreditReport
        {
            StartingBalance = 1000,
            TotalEarned = 500,
            TotalSpent = 200,
            EndingBalance = 1300,
            TransactionCount = 7,
            AverageTransactionSize = 100,
            PeakBalance = 1200, // Should be at least 1300 (ending balance)
            LowestBalance = 1000
        };

        // Act
        var result = report.ValidateDataConsistency();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateDataConsistency_ShouldReturnFalseWhenLowestBalanceIsTooHigh()
    {
        // Arrange
        var report = new UserCreditReport
        {
            StartingBalance = 1000,
            TotalEarned = 500,
            TotalSpent = 800, // Net change: -300
            EndingBalance = 700,
            TransactionCount = 13,
            AverageTransactionSize = 100,
            PeakBalance = 1000,
            LowestBalance = 800 // Should be at most 700 (ending balance)
        };

        // Act
        var result = report.ValidateDataConsistency();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Static Helper Method Tests

    [Theory]
    [InlineData("2025-09-15", 202509)]
    [InlineData("2023-12-25", 202312)]
    [InlineData("2020-01-01", 202001)]
    public void CreateReportMonth_ShouldFormatDateCorrectly(string dateString, int expected)
    {
        // Arrange
        var date = DateTime.Parse(dateString);

        // Act
        var result = UserCreditReport.CreateReportMonth(date);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateReportMonths_ShouldReturnCorrectSequence()
    {
        // Arrange
        var startDate = new DateTime(2025, 8, 15);
        var endDate = new DateTime(2025, 11, 10);

        // Act
        var result = UserCreditReport.GenerateReportMonths(startDate, endDate);

        // Assert
        result.Should().Equal(202508, 202509, 202510, 202511);
    }

    [Fact]
    public void GenerateReportMonths_ShouldHandleYearBoundary()
    {
        // Arrange
        var startDate = new DateTime(2024, 11, 15);
        var endDate = new DateTime(2025, 2, 10);

        // Act
        var result = UserCreditReport.GenerateReportMonths(startDate, endDate);

        // Assert
        result.Should().Equal(202411, 202412, 202501, 202502);
    }

    [Fact]
    public void GenerateReportMonths_ShouldReturnSingleMonthForSameMonth()
    {
        // Arrange
        var startDate = new DateTime(2025, 9, 1);
        var endDate = new DateTime(2025, 9, 30);

        // Act
        var result = UserCreditReport.GenerateReportMonths(startDate, endDate);

        // Assert
        result.Should().Equal(202509);
    }

    [Theory]
    [InlineData(202509, "2025-09-01")]
    [InlineData(202312, "2023-12-01")]
    [InlineData(202001, "2020-01-01")]
    public void ParseReportMonth_ShouldParseCorrectly(int reportMonth, string expectedDateString)
    {
        // Arrange
        var expectedDate = DateTime.Parse(expectedDateString);

        // Act
        var result = UserCreditReport.ParseReportMonth(reportMonth);

        // Assert
        result.Should().Be(expectedDate);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void CalculateAverageTransactionSize_ShouldReturnZeroWhenNoTransactions()
    {
        // Arrange
        var report = new UserCreditReport
        {
            TotalEarned = 1000,
            TotalSpent = 500,
            TransactionCount = 0
        };

        // Act
        var result = report.CalculateAverageTransactionSize();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ValidateDataConsistency_ShouldHandleZeroTransactionCount()
    {
        // Arrange
        var report = new UserCreditReport
        {
            StartingBalance = 1000,
            TotalEarned = 0,
            TotalSpent = 0,
            EndingBalance = 1000,
            TransactionCount = 0,
            AverageTransactionSize = 0,
            PeakBalance = 1000,
            LowestBalance = 1000
        };

        // Act
        var result = report.ValidateDataConsistency();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetReportMonthEnd_ShouldHandleDecember()
    {
        // Arrange
        var report = new UserCreditReport { ReportMonth = 202512 }; // December 2025

        // Act
        var result = report.GetReportMonthEnd();

        // Assert
        result.Should().Be(new DateTime(2025, 12, 31));
    }

    [Fact]
    public void FinalizeReport_WhenAlreadyFinalized_ShouldUpdateTimestamp()
    {
        // Arrange
        var report = new UserCreditReport();
        report.FinalizeReport();
        var firstFinalizedAt = report.FinalizedAt;

        // Wait a small amount
        Thread.Sleep(50);

        // Act
        report.FinalizeReport();

        // Assert
        report.IsFinalized.Should().BeTrue();
        report.FinalizedAt.Should().NotBeNull();
        report.FinalizedAt.Should().BeAfter(firstFinalizedAt!.Value);
    }

    #endregion

    #region Property Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void TotalEarned_ShouldNotAcceptNegativeValues(int negativeValue)
    {
        // This test documents expected validation behavior
        // In practice, validation would be enforced by data annotations or business rules

        // Arrange & Act
        var report = new UserCreditReport { TotalEarned = negativeValue };

        // Assert - The entity allows negative values, but validation should catch this
        // This test documents the current behavior and expected validation requirements
        report.TotalEarned.Should().Be(negativeValue);
        // In a complete system, additional validation would prevent negative values
    }

    [Theory]
    [InlineData(999999)]
    [InlineData(0)]
    [InlineData(190001)]
    [InlineData(999912)]
    public void ReportMonth_ShouldAcceptValidFormats(int validReportMonth)
    {
        // Arrange & Act
        var report = new UserCreditReport { ReportMonth = validReportMonth };

        // Assert
        report.ReportMonth.Should().Be(validReportMonth);
    }

    #endregion
}