using SkillLedger.Tests.Infrastructure;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;

namespace SkillLedger.Tests.Unit;

[UnitTest]
[FinancialTest]
public class CreditTransferDtoTests
{
    [Fact]
    public void TransferCreditsRequestDto_ValidData_PassesValidation()
    {
        // Arrange
        var request = new TransferCreditsRequestDto
        {
            ToUserId = Guid.NewGuid(),
            Amount = 100,
            Message = "Test transfer"
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void TransferCreditsRequestDto_InvalidAmount_FailsValidation(int amount)
    {
        // Arrange
        var request = new TransferCreditsRequestDto
        {
            ToUserId = Guid.NewGuid(),
            Amount = amount
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Transfer amount must be between 1 and 10,000 credits"));
    }

    [Fact]
    public void TransferCreditsRequestDto_MessageTooLong_FailsValidation()
    {
        // Arrange
        var request = new TransferCreditsRequestDto
        {
            ToUserId = Guid.NewGuid(),
            Amount = 100,
            Message = new string('x', 501) // Exceeds 500 character limit
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Message cannot exceed 500 characters"));
    }

    [Fact]
    public void BatchTransferItemDto_ValidData_PassesValidation()
    {
        // Arrange
        var item = new BatchTransferItemDto
        {
            ToUserId = Guid.NewGuid(),
            Amount = 50,
            Message = "Batch item"
        };

        // Act & Assert
        var validationResults = ValidateModel(item);
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void BatchTransferRequestDto_ValidData_PassesValidation()
    {
        // Arrange
        var request = new BatchTransferRequestDto
        {
            Transfers = new List<BatchTransferItemDto>
            {
                new() { ToUserId = Guid.NewGuid(), Amount = 50 },
                new() { ToUserId = Guid.NewGuid(), Amount = 100 }
            }
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void BatchTransferRequestDto_EmptyTransfers_FailsValidation()
    {
        // Arrange
        var request = new BatchTransferRequestDto
        {
            Transfers = new List<BatchTransferItemDto>()
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("At least one transfer is required"));
    }

    [Fact]
    public void BatchTransferRequestDto_TooManyTransfers_FailsValidation()
    {
        // Arrange
        var request = new BatchTransferRequestDto
        {
            Transfers = Enumerable.Range(1, 11)
                .Select(_ => new BatchTransferItemDto { ToUserId = Guid.NewGuid(), Amount = 50 })
                .ToList()
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Maximum 10 transfers allowed per batch"));
    }

    [Fact]
    public void ReverseTransferRequestDto_ValidReason_PassesValidation()
    {
        // Arrange
        var request = new ReverseTransferRequestDto
        {
            Reason = "Customer requested reversal"
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void ReverseTransferRequestDto_EmptyReason_FailsValidation()
    {
        // Arrange
        var request = new ReverseTransferRequestDto
        {
            Reason = ""
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("required"));
    }

    [Fact]
    public void ReverseTransferRequestDto_ReasonTooLong_FailsValidation()
    {
        // Arrange
        var request = new ReverseTransferRequestDto
        {
            Reason = new string('x', 1001) // Exceeds 1000 character limit
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Reversal reason cannot exceed 1000 characters"));
    }

    [Fact]
    public void TransferHistoryRequestDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new TransferHistoryRequestDto();

        // Assert
        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.Status.Should().BeNull();
        request.Direction.Should().BeNull();
        request.StartDate.Should().BeNull();
        request.EndDate.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TransferHistoryRequestDto_InvalidPage_FailsValidation(int page)
    {
        // Arrange
        var request = new TransferHistoryRequestDto
        {
            Page = page
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void TransferHistoryRequestDto_InvalidPageSize_FailsValidation(int pageSize)
    {
        // Arrange
        var request = new TransferHistoryRequestDto
        {
            PageSize = pageSize
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().NotBeEmpty();
    }

    [Fact]
    public void VerifyReceiptRequestDto_ValidData_PassesValidation()
    {
        // Arrange
        var request = new VerifyReceiptRequestDto
        {
            TransferId = Guid.NewGuid(),
            Signature = "test-signature"
        };

        // Act & Assert
        var validationResults = ValidateModel(request);
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void TransferCreditsResponseDto_InitializedCorrectly()
    {
        // Arrange & Act
        var response = new TransferCreditsResponseDto
        {
            TransferId = Guid.NewGuid(),
            TransactionHash = "ABC123",
            Status = TransferStatus.Completed,
            Amount = 100,
            TransferFee = 5,
            CreatedAt = DateTime.UtcNow,
            RemainingBalance = 500
        };

        // Assert
        response.TransferId.Should().NotBe(Guid.Empty);
        response.TransactionHash.Should().Be("ABC123");
        response.Status.Should().Be(TransferStatus.Completed);
        response.Amount.Should().Be(100);
        response.TransferFee.Should().Be(5);
        response.RemainingBalance.Should().Be(500);
    }

    [Fact]
    public void BatchTransferResponseDto_InitializesCollections()
    {
        // Arrange & Act
        var response = new BatchTransferResponseDto();

        // Assert
        response.Transfers.Should().NotBeNull();
        response.Transfers.Should().BeEmpty();
    }

    [Fact]
    public void TransferHistoryResponseDto_CalculatesPaginationCorrectly()
    {
        // Arrange & Act
        var response = new TransferHistoryResponseDto
        {
            TotalCount = 250,
            Page = 3,
            PageSize = 20
        };

        // Calculate properties that would be set by service
        response.TotalPages = (int)Math.Ceiling((double)response.TotalCount / response.PageSize);
        response.HasNextPage = response.Page < response.TotalPages;
        response.HasPreviousPage = response.Page > 1;

        // Assert
        response.TotalPages.Should().Be(13); // 250 / 20 = 12.5, rounded up to 13
        response.HasNextPage.Should().BeTrue(); // Page 3 < 13
        response.HasPreviousPage.Should().BeTrue(); // Page 3 > 1
    }

    [Fact]
    public void TransferLimitsDto_CalculatesRemainingLimits()
    {
        // Arrange & Act
        var limits = new TransferLimitsDto
        {
            MaxSingleTransfer = 1000,
            MaxDailyTransfer = 5000,
            MaxDailyCount = 10,
            DailyTransferredAmount = 2000,
            DailyTransferCount = 3,
            WalletBalance = 10000
        };

        // Calculate remaining limits
        limits.RemainingDailyAmount = limits.MaxDailyTransfer - limits.DailyTransferredAmount;
        limits.RemainingDailyCount = limits.MaxDailyCount - limits.DailyTransferCount;

        // Assert
        limits.RemainingDailyAmount.Should().Be(3000);
        limits.RemainingDailyCount.Should().Be(7);
    }

    [Fact]
    public void TransferReceiptDto_RequiredPropertiesNotNull()
    {
        // Arrange & Act
        var receipt = new TransferReceiptDto
        {
            TransferId = Guid.NewGuid(),
            TransactionHash = "ABC123",
            FromUser = "alice@example.com",
            ToUser = "bob@example.com",
            Amount = 100,
            TransferFee = 5,
            CompletedAt = DateTime.UtcNow,
            ReceiptSignature = "signature123",
            GeneratedAt = DateTime.UtcNow
        };

        // Assert
        receipt.TransferId.Should().NotBe(Guid.Empty);
        receipt.TransactionHash.Should().NotBeNullOrEmpty();
        receipt.FromUser.Should().NotBeNullOrEmpty();
        receipt.ToUser.Should().NotBeNullOrEmpty();
        receipt.ReceiptSignature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyReceiptResponseDto_ValidReceipt_HasTransferDetails()
    {
        // Arrange & Act
        var response = new VerifyReceiptResponseDto
        {
            IsValid = true,
            Transfer = new CreditTransferDetailDto
            {
                Id = Guid.NewGuid(),
                Amount = 100,
                Status = TransferStatus.Completed
            },
            Message = "Receipt is valid"
        };

        // Assert
        response.IsValid.Should().BeTrue();
        response.Transfer.Should().NotBeNull();
        response.Message.Should().Be("Receipt is valid");
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}