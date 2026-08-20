using SkillLedger.Tests.Infrastructure;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Tests.Unit;

[UnitTest]
[FinancialTest]
public class CreditTransferEntityTests
{
    private readonly Guid _fromUserId = Guid.NewGuid();
    private readonly Guid _toUserId = Guid.NewGuid();

    [Fact]
    public void CreditTransfer_Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var transfer = new CreditTransfer();

        // Assert
        transfer.Id.Should().NotBe(Guid.Empty);
        transfer.Status.Should().Be(TransferStatus.Pending);
        transfer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        transfer.TransferFee.Should().Be(0);
        transfer.RowVersion.Should().BeEquivalentTo(Array.Empty<byte>());
    }

    [Fact]
    public void CanBeReversed_CompletedTransferWithin24Hours_ReturnsTrue()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddHours(-12)
        };

        // Act
        var canBeReversed = transfer.CanBeReversed();

        // Assert
        canBeReversed.Should().BeTrue();
    }

    [Fact]
    public void CanBeReversed_CompletedTransferAfter24Hours_ReturnsFalse()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddHours(-25)
        };

        // Act
        var canBeReversed = transfer.CanBeReversed();

        // Assert
        canBeReversed.Should().BeFalse();
    }

    [Fact]
    public void CanBeReversed_AlreadyReversedTransfer_ReturnsFalse()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddHours(-12),
            ReversedAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var canBeReversed = transfer.CanBeReversed();

        // Assert
        canBeReversed.Should().BeFalse();
    }

    [Theory]
    [InlineData(TransferStatus.Pending)]
    [InlineData(TransferStatus.Failed)]
    [InlineData(TransferStatus.Processing)]
    [InlineData(TransferStatus.Cancelled)]
    public void CanBeReversed_NonCompletedTransfer_ReturnsFalse(TransferStatus status)
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = status,
            CompletedAt = DateTime.UtcNow.AddHours(-12)
        };

        // Act
        var canBeReversed = transfer.CanBeReversed();

        // Assert
        canBeReversed.Should().BeFalse();
    }

    [Fact]
    public void Complete_PendingTransfer_SetsCompletedStatusAndTimestamp()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Pending
        };

        // Act
        transfer.Complete();

        // Assert
        transfer.Status.Should().Be(TransferStatus.Completed);
        transfer.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(TransferStatus.Completed)]
    [InlineData(TransferStatus.Failed)]
    [InlineData(TransferStatus.Reversed)]
    public void Complete_NonPendingTransfer_ThrowsInvalidOperationException(TransferStatus status)
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = status
        };

        // Act & Assert
        var action = () => transfer.Complete();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot complete transfer in {status} status");
    }

    [Fact]
    public void Fail_PendingTransfer_SetsFailedStatusAndReason()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Pending
        };
        var reason = "Insufficient funds";

        // Act
        transfer.Fail(reason);

        // Assert
        transfer.Status.Should().Be(TransferStatus.Failed);
        transfer.ReversalReason.Should().Be(reason);
    }

    [Theory]
    [InlineData(TransferStatus.Completed)]
    [InlineData(TransferStatus.Failed)]
    [InlineData(TransferStatus.Reversed)]
    public void Fail_NonPendingTransfer_ThrowsInvalidOperationException(TransferStatus status)
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = status
        };

        // Act & Assert
        var action = () => transfer.Fail("Test reason");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot fail transfer in {status} status");
    }

    [Fact]
    public void Reverse_ReversibleTransfer_SetsReversedStatusAndDetails()
    {
        // Arrange
        var reversedByUserId = Guid.NewGuid();
        var reason = "Customer request";
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddHours(-12)
        };

        // Act
        transfer.Reverse(reversedByUserId, reason);

        // Assert
        transfer.Status.Should().Be(TransferStatus.Reversed);
        transfer.ReversedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        transfer.ReversedByUserId.Should().Be(reversedByUserId);
        transfer.ReversalReason.Should().Be(reason);
    }

    [Fact]
    public void Reverse_NonReversibleTransfer_ThrowsInvalidOperationException()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = TransferStatus.Completed,
            CompletedAt = DateTime.UtcNow.AddHours(-25) // Beyond reversal window
        };

        // Act & Assert
        var action = () => transfer.Reverse(Guid.NewGuid(), "Test");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Transfer cannot be reversed at this time");
    }

    [Fact]
    public void GenerateTransactionHash_ValidTransfer_ReturnsConsistentHash()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            FromUserId = _fromUserId,
            ToUserId = _toUserId,
            Amount = 100,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var hash1 = transfer.GenerateTransactionHash();
        var hash2 = transfer.GenerateTransactionHash();

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().HaveLength(64); // SHA-256 hex string length
        hash2.Should().Be(hash1); // Should be deterministic
    }

    [Fact]
    public void GenerateTransactionHash_DifferentTransfers_ReturnsDifferentHashes()
    {
        // Arrange
        var transfer1 = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            FromUserId = _fromUserId,
            ToUserId = _toUserId,
            Amount = 100,
            CreatedAt = DateTime.UtcNow
        };
        var transfer2 = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            FromUserId = _fromUserId,
            ToUserId = _toUserId,
            Amount = 200,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var hash1 = transfer1.GenerateTransactionHash();
        var hash2 = transfer2.GenerateTransactionHash();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateReceiptSignature_ValidTransfer_ReturnsConsistentSignature()
    {
        // Arrange
        var secretKey = "test-secret-key";
        var transfer = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            TransactionHash = "ABC123",
            Amount = 100,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        var signature1 = transfer.GenerateReceiptSignature(secretKey);
        var signature2 = transfer.GenerateReceiptSignature(secretKey);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature2.Should().Be(signature1); // Should be deterministic
    }

    [Fact]
    public void GenerateReceiptSignature_DifferentSecretKeys_ReturnsDifferentSignatures()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            TransactionHash = "ABC123",
            Amount = 100,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        var signature1 = transfer.GenerateReceiptSignature("secret1");
        var signature2 = transfer.GenerateReceiptSignature("secret2");

        // Assert
        signature1.Should().NotBe(signature2);
    }

    [Fact]
    public void VerifyReceiptSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var secretKey = "test-secret-key";
        var transfer = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            TransactionHash = "ABC123",
            Amount = 100,
            CompletedAt = DateTime.UtcNow
        };
        var signature = transfer.GenerateReceiptSignature(secretKey);

        // Act
        var isValid = transfer.VerifyReceiptSignature(signature, secretKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyReceiptSignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Id = Guid.NewGuid(),
            TransactionHash = "ABC123",
            Amount = 100,
            CompletedAt = DateTime.UtcNow
        };

        // Act
        var isValid = transfer.VerifyReceiptSignature("invalid-signature", "test-secret-key");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void TotalAmount_IncludesTransferFee()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Amount = 100,
            TransferFee = 5
        };

        // Act
        var totalAmount = transfer.TotalAmount;

        // Assert
        totalAmount.Should().Be(105);
    }

    [Theory]
    [InlineData(TransferStatus.Completed, true)]
    [InlineData(TransferStatus.Failed, true)]
    [InlineData(TransferStatus.Reversed, true)]
    [InlineData(TransferStatus.Pending, false)]
    [InlineData(TransferStatus.Processing, false)]
    [InlineData(TransferStatus.Cancelled, false)]
    public void IsTerminal_ReturnsCorrectValue(TransferStatus status, bool expectedIsTerminal)
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            Status = status
        };

        // Act
        var isTerminal = transfer.IsTerminal;

        // Assert
        isTerminal.Should().Be(expectedIsTerminal);
    }

    [Fact]
    public void IsBatchTransfer_WithBatchId_ReturnsTrue()
    {
        // Arrange
        var transfer = new CreditTransfer
        {
            BatchId = Guid.NewGuid()
        };

        // Act
        var isBatchTransfer = transfer.IsBatchTransfer;

        // Assert
        isBatchTransfer.Should().BeTrue();
    }

    [Fact]
    public void IsBatchTransfer_WithoutBatchId_ReturnsFalse()
    {
        // Arrange
        var transfer = new CreditTransfer();

        // Act
        var isBatchTransfer = transfer.IsBatchTransfer;

        // Assert
        isBatchTransfer.Should().BeFalse();
    }
}