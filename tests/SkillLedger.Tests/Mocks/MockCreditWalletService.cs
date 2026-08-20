using Microsoft.EntityFrameworkCore;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock implementation of ICreditWalletService for integration testing.
/// INTERNAL SERVICE - uses real database operations, not mock behavior.
/// </summary>
public class MockCreditWalletService : ICreditWalletService
{
    private readonly SkillLedgerDbContext _context;

    public MockCreditWalletService(SkillLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<CreditWallet> CreateWalletAsync(Guid userId)
    {
        var wallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 100, // Starting credits
            PendingBalance = 0,
            TotalEarned = 100,
            TotalSpent = 0,
            EncryptedBalance = "encrypted",
            EncryptedPendingBalance = "encrypted",
            EncryptedTotalEarned = "encrypted",
            EncryptedTotalSpent = "encrypted",
            KeyIdentifier = "test-key",
            LastTransactionAt = DateTime.UtcNow
        };

        _context.CreditWallets.Add(wallet);
        await _context.SaveChangesAsync();
        return wallet;
    }

    public async Task<CreditWallet?> GetWalletAsync(Guid userId)
    {
        return await _context.CreditWallets
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<int?> GetBalanceAsync(Guid userId)
    {
        var wallet = await GetWalletAsync(userId);
        return wallet?.Balance;
    }

    public async Task<int?> GetAvailableBalanceAsync(Guid userId)
    {
        var wallet = await GetWalletAsync(userId);
        return wallet?.AvailableBalance;
    }

    public async Task<CreditTransaction> TransferCreditsAsync(
        Guid fromUserId,
        Guid toUserId,
        int amount,
        string description,
        CreditTransactionType transactionType,
        Guid? projectId = null,
        string? initiatedFromIP = null,
        string? userAgent = null)
    {
        var fromWallet = await GetWalletAsync(fromUserId);
        var toWallet = await GetWalletAsync(toUserId);

        if (fromWallet == null || toWallet == null)
            throw new InvalidOperationException("Wallet not found");

        if (fromWallet.Balance < amount)
            throw new InvalidOperationException("Insufficient credits");

        fromWallet.Balance -= amount;
        fromWallet.TotalSpent += amount;
        toWallet.Balance += amount;
        toWallet.TotalEarned += amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            Description = description,
            Type = transactionType,
            ProjectId = projectId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash",
            InitiatedFromIP = initiatedFromIP,
            UserAgent = userAgent
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<CreditTransaction> AddCreditsAsync(
        Guid userId,
        int amount,
        string description,
        CreditTransactionType transactionType)
    {
        var wallet = await GetWalletAsync(userId);
        if (wallet == null)
            throw new InvalidOperationException("Wallet not found");

        wallet.Balance += amount;
        wallet.TotalEarned += amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            ToUserId = userId,
            Amount = amount,
            Description = description,
            Type = transactionType,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash"
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<CreditTransaction> DeductCreditsAsync(
        Guid userId,
        int amount,
        string description,
        CreditTransactionType transactionType)
    {
        var wallet = await GetWalletAsync(userId);
        if (wallet == null)
            throw new InvalidOperationException("Wallet not found");

        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient credits");

        wallet.Balance -= amount;
        wallet.TotalSpent += amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            FromUserId = userId,
            Amount = amount,
            Description = description,
            Type = transactionType,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash"
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<CreditTransaction> CreateEscrowAsync(Guid clientUserId, Guid projectId, int amount)
    {
        var wallet = await GetWalletAsync(clientUserId);
        if (wallet == null)
            throw new InvalidOperationException("Wallet not found");

        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient credits");

        // Move from balance to pending (escrowed)
        wallet.Balance -= amount;
        wallet.PendingBalance += amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            FromUserId = clientUserId,
            Amount = amount,
            Description = "Escrow created for project",
            Type = CreditTransactionType.EscrowDeposit,
            ProjectId = projectId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash"
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<CreditTransaction> ReleaseEscrowAsync(Guid projectId, Guid providerUserId)
    {
        // Find the escrow transaction
        var escrowTx = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.ProjectId == projectId &&
                                     t.Type == CreditTransactionType.EscrowDeposit);

        if (escrowTx == null || escrowTx.FromUserId == null)
            throw new InvalidOperationException("Escrow not found");

        var clientWallet = await GetWalletAsync(escrowTx.FromUserId.Value);
        var providerWallet = await GetWalletAsync(providerUserId);

        if (clientWallet == null || providerWallet == null)
            throw new InvalidOperationException("Wallet not found");

        // Release from escrow to provider
        clientWallet.PendingBalance -= escrowTx.Amount;
        providerWallet.Balance += escrowTx.Amount;
        providerWallet.TotalEarned += escrowTx.Amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            FromUserId = escrowTx.FromUserId,
            ToUserId = providerUserId,
            Amount = escrowTx.Amount,
            Description = "Escrow released",
            Type = CreditTransactionType.EscrowRelease,
            ProjectId = projectId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash"
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<CreditTransaction> RefundEscrowAsync(Guid projectId, int remainingAmount)
    {
        var escrowTx = await _context.CreditTransactions
            .FirstOrDefaultAsync(t => t.ProjectId == projectId &&
                                     t.Type == CreditTransactionType.EscrowDeposit);

        if (escrowTx == null || escrowTx.FromUserId == null)
            throw new InvalidOperationException("Escrow not found");

        var wallet = await GetWalletAsync(escrowTx.FromUserId.Value);
        if (wallet == null)
            throw new InvalidOperationException("Wallet not found");

        // Refund from escrow to available
        wallet.PendingBalance -= remainingAmount;
        wallet.Balance += remainingAmount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            FromUserId = escrowTx.FromUserId,
            ToUserId = escrowTx.FromUserId,
            Amount = remainingAmount,
            Description = "Escrow refunded",
            Type = CreditTransactionType.EscrowRefund,
            ProjectId = projectId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash"
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<CreditTransaction> ReleaseMilestoneFromEscrowAsync(
        Guid clientUserId,
        Guid providerUserId,
        Guid projectId,
        int amount)
    {
        var clientWallet = await GetWalletAsync(clientUserId);
        var providerWallet = await GetWalletAsync(providerUserId);

        if (clientWallet == null || providerWallet == null)
            throw new InvalidOperationException("Wallet not found");

        // Release partial from escrow to provider
        clientWallet.PendingBalance -= amount;
        providerWallet.Balance += amount;
        providerWallet.TotalEarned += amount;

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            FromUserId = clientUserId,
            ToUserId = providerUserId,
            Amount = amount,
            Description = "Milestone released from escrow",
            Type = CreditTransactionType.EscrowRelease,
            ProjectId = projectId,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TransactionHash = "test-hash"
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IList<CreditTransaction>> GetTransactionHistoryAsync(Guid userId, int limit = 50, int offset = 0)
    {
        return await _context.CreditTransactions
            .Where(t => t.FromUserId == userId || t.ToUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IList<CreditTransaction>> GetProjectTransactionsAsync(Guid projectId)
    {
        return await _context.CreditTransactions
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public Task<bool> ValidateTransactionIntegrity(Guid transactionId)
    {
        // Simplified for testing
        return Task.FromResult(true);
    }

    public Task<FraudAnalysisReport> AnalyzeFraudPatterns(Guid userId)
    {
        // Simplified for testing
        return Task.FromResult(new FraudAnalysisReport
        {
            RiskScore = 0,
            IsHighRisk = false,
            TransactionsAnalyzed = 0,
            AnalysisStartDate = DateTime.UtcNow.AddDays(-30),
            AnalysisEndDate = DateTime.UtcNow,
            RequiresAdditionalVerification = false
        });
    }

    public async Task<bool> BlockWalletAsync(Guid userId, string reason)
    {
        // Simplified for testing - just return true
        return true;
    }

    public async Task<bool> UnblockWalletAsync(Guid userId)
    {
        // Simplified for testing - just return true
        return true;
    }

    public Task<BalanceReconciliationReport> ReconcileWalletBalance(Guid userId)
    {
        return Task.FromResult(new BalanceReconciliationReport());
    }

    public Task<SystemReconciliationReport> ReconcileAllWallets()
    {
        return Task.FromResult(new SystemReconciliationReport());
    }

    public Task<bool> RotateEncryptionKeysAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> VerifyEncryptionIntegrityAsync(Guid userId)
    {
        return Task.FromResult(true);
    }

    public Task<WalletExportData> ExportWalletDataAsync(Guid userId)
    {
        return Task.FromResult(new WalletExportData());
    }

    public Task<FinancialSummaryReport> GenerateFinancialReportAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return Task.FromResult(new FinancialSummaryReport());
    }

    public Task<WalletUpdateNotification> GetWalletUpdateNotificationAsync(Guid userId)
    {
        return Task.FromResult(new WalletUpdateNotification());
    }
}
