using Bogus;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;

namespace SkillLedger.Infrastructure.Services.TestData;

/// <summary>
/// Factory for creating test financial data (wallets, transactions, escrow, transfers)
/// </summary>
public class CreditTestDataFactory
{
    private readonly IEncryptionService _encryptionService;
    private readonly Faker _faker;

    public CreditTestDataFactory(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
        _faker = new Faker();
    }

    /// <summary>
    /// Creates credit wallets for all users with encrypted balances
    /// </summary>
    public async Task<List<CreditWallet>> CreateWalletsForUsersAsync(List<User> users)
    {
        var wallets = new List<CreditWallet>();

        foreach (var user in users)
        {
            var balance = GetBalanceForUser(user);
            var wallet = await CreateWalletAsync(user.Id, balance);
            wallets.Add(wallet);
        }

        return wallets;
    }

    private int GetBalanceForUser(User user)
    {
        return user.Email switch
        {
            "thomas.anderson@testmail.com" => 50000, // Enterprise user
            "robert.chen@testmail.com" => 12000, // Business user
            "jennifer.lee@testmail.com" => 8500,
            "rachel.goldstein@testmail.com" => 5000, // Alice
            "david.kumar@testmail.com" => 2500, // Bob
            "patricia.williams@testmail.com" => 5000,
            "alex.kim@testmail.com" => 1200,
            "marcus.thompson@testmail.com" => 300,
            "maria.santos@testmail.com" => 450,
            "sophia.martinez@testmail.com" => 180,
            "emily.rodriguez@testmail.com" => 15,
            "sarah.chen@testmail.com" => 100, // Starting credits
            "mike.johnson@testmail.com" => 85,
            "james.park@testmail.com" => 45, // Suspended, blocked wallet
            "zero.balance@testmail.com" => 0,
            "high.risk@testmail.com" => 150,
            "banned.user@testmail.com" => 0,
            "lisa.wong@testmail.com" => 100,
            _ => 1000
        };
    }

    private async Task<CreditWallet> CreateWalletAsync(Guid userId, int balance, int pendingBalance = 0)
    {
        var keyIdentifier = _encryptionService.GenerateSecureToken(16); // 16 byte token for key identifier

        var wallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            KeyIdentifier = keyIdentifier,
            EncryptedBalance = await _encryptionService.EncryptAsync(balance.ToString()),
            EncryptedPendingBalance = await _encryptionService.EncryptAsync(pendingBalance.ToString()),
            EncryptedTotalEarned = await _encryptionService.EncryptAsync(balance.ToString()),
            EncryptedTotalSpent = await _encryptionService.EncryptAsync("0"),
            LastTransactionAt = DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 30)),
            CreatedAt = DateTime.UtcNow.AddDays(-_faker.Random.Int(30, 365)),
            UpdatedAt = DateTime.UtcNow,
            IsBlocked = false,
            RowVersion = new byte[8]
        };

        // Block suspended user's wallet
        if (balance == 45) // James Park
        {
            wallet.IsBlocked = true;
            wallet.BlockedReason = "Policy violation - spam";
            wallet.BlockedAt = DateTime.UtcNow.AddDays(-5);
        }

        return wallet;
    }

    /// <summary>
    /// Creates starting credit transactions for all users
    /// </summary>
    public List<CreditTransaction> CreateStartingCreditTransactions(List<User> users)
    {
        var transactions = new List<CreditTransaction>();

        foreach (var user in users)
        {
            transactions.Add(CreateTransaction(
                CreditTransactionType.StartingCredit,
                100,
                null, // System
                user.Id,
                null,
                TransactionStatus.Completed,
                "Welcome bonus - starting credits",
                user.CreatedAt
            ));
        }

        return transactions;
    }

    /// <summary>
    /// Creates credit purchase transactions
    /// </summary>
    public List<CreditTransaction> CreatePurchaseTransactions(List<User> users)
    {
        var transactions = new List<CreditTransaction>();
        var wealthyUsers = users.Where(u =>
            u.Email.Contains("anderson") ||
            u.Email.Contains("chen") ||
            u.Email.Contains("goldstein") ||
            u.Email.Contains("lee")).ToList();

        foreach (var user in wealthyUsers)
        {
            var purchaseAmount = _faker.Random.Int(1000, 10000);
            transactions.Add(CreateTransaction(
                CreditTransactionType.Purchase,
                purchaseAmount,
                null, // External/Stripe
                user.Id,
                null,
                TransactionStatus.Completed,
                $"Credit purchase via Stripe - {purchaseAmount} credits",
                DateTime.UtcNow.AddDays(-_faker.Random.Int(10, 90))
            ));
        }

        return transactions;
    }

    /// <summary>
    /// Creates escrow accounts for in-progress and completed projects
    /// </summary>
    public List<ProjectEscrow> CreateEscrowForProjects(List<Project> projects)
    {
        var escrows = new List<ProjectEscrow>();

        foreach (var project in projects)
        {
            // Only create escrow for projects with providers
            if (project.ProviderId == null)
                continue;

            // Only for InProgress, Completed, or Disputed projects
            if (project.Status != ProjectStatus.InProgress &&
                project.Status != ProjectStatus.Completed &&
                project.Status != ProjectStatus.Disputed)
                continue;

            var releasedAmount = GetReleasedAmountForProject(project);

            var escrow = new ProjectEscrow
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ClientId = project.ClientId,
                ProviderId = project.ProviderId.Value,
                TotalAmount = project.CreditBudget,
                ReleasedAmount = releasedAmount,
                Status = GetEscrowStatusForProject(project, releasedAmount),
                RequiresMultiSignature = project.CreditBudget > 1000,
                CreatedAt = project.StartDate ?? project.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                CreatedFromIP = "TEST_DATA_SEEDER"
            };

            if (project.Status == ProjectStatus.Disputed)
            {
                escrow.Status = EscrowStatus.Disputed;
                escrow.DisputeReason = "Quality concerns - work does not meet requirements";
                escrow.DisputedAt = DateTime.UtcNow.AddDays(-3);
            }

            if (project.Status == ProjectStatus.Completed)
            {
                escrow.CompletedAt = project.CompletedAt;
            }

            escrows.Add(escrow);
        }

        return escrows;
    }

    private int GetReleasedAmountForProject(Project project)
    {
        return project.Status switch
        {
            ProjectStatus.Completed => project.CreditBudget, // Fully released
            ProjectStatus.InProgress when project.Id.ToString().Contains("15") => 800, // Mid-progress
            ProjectStatus.InProgress when project.Id.ToString().Contains("16") => 1200, // Near completion
            ProjectStatus.Disputed when project.Id.ToString().Contains("29") => 600, // Partial release before dispute
            _ => 0 // Fresh escrow
        };
    }

    private EscrowStatus GetEscrowStatusForProject(Project project, int releasedAmount)
    {
        if (project.Status == ProjectStatus.Disputed)
            return EscrowStatus.Disputed;

        if (project.Status == ProjectStatus.Completed)
            return EscrowStatus.Completed;

        if (releasedAmount > 0 && releasedAmount < project.CreditBudget)
            return EscrowStatus.PartiallyReleased;

        return EscrowStatus.Active;
    }

    /// <summary>
    /// Creates escrow deposit transactions for funded escrows
    /// </summary>
    public List<CreditTransaction> CreateEscrowTransactions(List<ProjectEscrow> escrows, List<Project> projects)
    {
        var transactions = new List<CreditTransaction>();

        foreach (var escrow in escrows)
        {
            var project = projects.FirstOrDefault(p => p.Id == escrow.ProjectId);
            if (project == null) continue;

            // Escrow deposit transaction
            transactions.Add(CreateTransaction(
                CreditTransactionType.EscrowDeposit,
                escrow.TotalAmount,
                escrow.ClientId,
                null, // To escrow (system)
                project.Id,
                TransactionStatus.Completed,
                $"Escrow deposit for project: {project.Title}",
                escrow.CreatedAt
            ));

            // Escrow release transactions
            if (escrow.ReleasedAmount > 0)
            {
                transactions.Add(CreateTransaction(
                    CreditTransactionType.EscrowRelease,
                    escrow.ReleasedAmount,
                    null, // From escrow (system)
                    escrow.ProviderId,
                    project.Id,
                    TransactionStatus.Completed,
                    $"Milestone release for project: {project.Title}",
                    DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 20))
                ));
            }

            // Platform fee (5% of released amount)
            if (escrow.ReleasedAmount > 0)
            {
                var platformFee = (int)(escrow.ReleasedAmount * 0.05m);
                transactions.Add(CreateTransaction(
                    CreditTransactionType.PlatformFee,
                    platformFee,
                    escrow.ProviderId,
                    null, // To platform (system)
                    project.Id,
                    TransactionStatus.Completed,
                    $"Platform fee (5%) for project: {project.Title}",
                    DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 20))
                ));
            }
        }

        return transactions;
    }

    /// <summary>
    /// Creates bonus payment transaction for exceptional work
    /// </summary>
    public List<CreditTransaction> CreateBonusTransactions(List<Project> projects)
    {
        var transactions = new List<CreditTransaction>();

        // Project 24 has bonus payment
        var bonusProject = projects.FirstOrDefault(p => p.Id.ToString().Contains("24"));
        if (bonusProject != null && bonusProject.ProviderId.HasValue)
        {
            transactions.Add(CreateTransaction(
                CreditTransactionType.BonusPayment,
                500,
                bonusProject.ClientId,
                bonusProject.ProviderId.Value,
                bonusProject.Id,
                TransactionStatus.Completed,
                "Exceptional work bonus - exceeded expectations",
                bonusProject.CompletedAt?.AddDays(1) ?? DateTime.UtcNow
            ));
        }

        return transactions;
    }

    /// <summary>
    /// Creates P2P credit transfers
    /// </summary>
    public List<CreditTransfer> CreateCreditTransfers(List<User> users)
    {
        var transfers = new List<CreditTransfer>();

        // Create a few P2P transfers between users
        if (users.Count >= 10)
        {
            transfers.Add(CreateTransfer(
                users[6].Id, // Rachel Goldstein
                users[5].Id, // David Kumar
                500,
                "Payment for consultation services",
                TransferStatus.Completed,
                DateTime.UtcNow.AddDays(-15)
            ));

            transfers.Add(CreateTransfer(
                users[10].Id, // Jennifer Lee
                users[7].Id, // Marcus Thompson
                300,
                "Partial payment for design work",
                TransferStatus.Completed,
                DateTime.UtcNow.AddDays(-25)
            ));

            transfers.Add(CreateTransfer(
                users[13].Id, // Thomas Anderson
                users[6].Id, // Rachel Goldstein
                1000,
                "Investment funds transfer",
                TransferStatus.Completed,
                DateTime.UtcNow.AddDays(-40)
            ));
        }

        return transfers;
    }

    private CreditTransfer CreateTransfer(
        Guid fromUserId,
        Guid toUserId,
        int amount,
        string message,
        TransferStatus status,
        DateTime createdAt)
    {
        return new CreditTransfer
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            TransferFee = (int)(amount * 0.02m), // 2% transfer fee
            Message = message,
            Status = status,
            TransactionHash = GenerateTransactionHash(),
            IdempotencyKey = Guid.NewGuid().ToString(),
            InitiatedFromIP = "TEST_DATA_SEEDER",
            CreatedAt = createdAt,
            CompletedAt = status == TransferStatus.Completed ? createdAt.AddMinutes(1) : null,
            RowVersion = new byte[8]
        };
    }

    private CreditTransaction CreateTransaction(
        CreditTransactionType type,
        int amount,
        Guid? fromUserId,
        Guid? toUserId,
        Guid? projectId,
        TransactionStatus status,
        string description,
        DateTime createdAt)
    {
        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            ProjectId = projectId,
            Status = status,
            Description = description,
            TransactionHash = GenerateTransactionHash(),
            PreviousTransactionHash = null, // Would be set by service in production
            CreatedAt = createdAt,
            CompletedAt = status == TransactionStatus.Completed ? createdAt.AddSeconds(30) : null,
            InitiatedFromIP = "TEST_DATA_SEEDER",
            IsFlagged = false
        };

        return transaction;
    }

    private string GenerateTransactionHash()
    {
        // Generate a realistic-looking hash (in production, this would be HMAC-SHA256)
        return _faker.Random.Hash(64);
    }
}
