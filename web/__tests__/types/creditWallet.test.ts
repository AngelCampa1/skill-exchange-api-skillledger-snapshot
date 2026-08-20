/**
 * Tests for creditWallet.ts type definitions
 *
 * This file validates that credit wallet types and enums are correctly defined
 */

import {
  CreditTransactionType,
  TransactionStatus,
  type CreditWallet,
  type CreditTransaction,
  type WalletDashboardData,
  type TransferCreditsRequest,
  type CreateEscrowRequest,
  type TransactionHistoryRequest,
  type FraudAnalysisReport,
  type BalanceReconciliationReport,
  type WalletExportData,
  type ApiResponse,
  type ApiError,
} from '@/types/creditWallet'

describe('creditWallet types', () => {
  describe('CreditTransactionType enum', () => {
    it('should have all transaction types defined', () => {
      expect(CreditTransactionType.StartingCredit).toBe('StartingCredit')
      expect(CreditTransactionType.ProjectPayment).toBe('ProjectPayment')
      expect(CreditTransactionType.EscrowDeposit).toBe('EscrowDeposit')
      expect(CreditTransactionType.EscrowRelease).toBe('EscrowRelease')
      expect(CreditTransactionType.EscrowRefund).toBe('EscrowRefund')
      expect(CreditTransactionType.Purchase).toBe('Purchase')
      expect(CreditTransactionType.Refund).toBe('Refund')
      expect(CreditTransactionType.AdminAdjustment).toBe('AdminAdjustment')
    })

    it('should have exactly 8 transaction types', () => {
      const types = Object.values(CreditTransactionType)
      expect(types).toHaveLength(8)
    })
  })

  describe('TransactionStatus enum', () => {
    it('should have all status values defined', () => {
      expect(TransactionStatus.Pending).toBe('Pending')
      expect(TransactionStatus.Completed).toBe('Completed')
      expect(TransactionStatus.Failed).toBe('Failed')
      expect(TransactionStatus.Cancelled).toBe('Cancelled')
    })

    it('should have exactly 4 status values', () => {
      const statuses = Object.values(TransactionStatus)
      expect(statuses).toHaveLength(4)
    })
  })

  describe('Type structure validation', () => {
    it('should allow valid CreditWallet object', () => {
      const wallet: CreditWallet = {
        id: 'wallet-123',
        userId: 'user-456',
        balance: 1000,
        pendingBalance: 100,
        totalEarned: 5000,
        totalSpent: 4000,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-15T12:00:00Z',
      }

      expect(wallet.id).toBe('wallet-123')
      expect(wallet.balance).toBe(1000)
    })

    it('should allow valid CreditTransaction object', () => {
      const transaction: CreditTransaction = {
        id: 'tx-123',
        fromUserId: 'user-1',
        toUserId: 'user-2',
        amount: 500,
        type: CreditTransactionType.ProjectPayment,
        description: 'Payment for services',
        status: TransactionStatus.Completed,
        projectId: 'project-789',
        createdAt: '2024-01-01T00:00:00Z',
        completedAt: '2024-01-01T00:05:00Z',
      }

      expect(transaction.amount).toBe(500)
      expect(transaction.type).toBe(CreditTransactionType.ProjectPayment)
      expect(transaction.status).toBe(TransactionStatus.Completed)
    })

    it('should allow valid WalletDashboardData object', () => {
      const dashboard: WalletDashboardData = {
        wallet: {
          id: 'wallet-1',
          userId: 'user-1',
          balance: 1000,
          pendingBalance: 0,
          totalEarned: 2000,
          totalSpent: 1000,
          createdAt: '2024-01-01',
          updatedAt: '2024-01-15',
        },
        recentTransactions: [],
        activeEscrows: [],
        totalActiveEscrowAmount: 0,
        availableBalance: 1000,
      }

      expect(dashboard.availableBalance).toBe(1000)
    })

    it('should allow valid TransferCreditsRequest object', () => {
      const request: TransferCreditsRequest = {
        toUserId: 'user-2',
        amount: 100,
        description: 'Transfer',
        transactionType: CreditTransactionType.ProjectPayment,
        projectId: 'project-1',
      }

      expect(request.amount).toBe(100)
      expect(request.transactionType).toBe(CreditTransactionType.ProjectPayment)
    })

    it('should allow valid CreateEscrowRequest object', () => {
      const request: CreateEscrowRequest = {
        projectId: 'project-1',
        amount: 500,
        description: 'Escrow for project',
      }

      expect(request.amount).toBe(500)
    })

    it('should allow valid TransactionHistoryRequest object', () => {
      const request: TransactionHistoryRequest = {
        limit: 50,
        offset: 0,
        fromDate: '2024-01-01',
        toDate: '2024-12-31',
        transactionType: CreditTransactionType.ProjectPayment,
      }

      expect(request.limit).toBe(50)
      expect(request.transactionType).toBe(CreditTransactionType.ProjectPayment)
    })

    it('should allow valid FraudAnalysisReport object', () => {
      const report: FraudAnalysisReport = {
        isHighRisk: false,
        riskScore: 15,
        riskFactors: ['Multiple rapid transactions'],
        recommendedActions: ['Monitor for 24 hours'],
        analysisTimestamp: '2024-01-15T12:00:00Z',
      }

      expect(report.isHighRisk).toBe(false)
      expect(report.riskScore).toBe(15)
    })

    it('should allow valid BalanceReconciliationReport object', () => {
      const report: BalanceReconciliationReport = {
        isBalanced: true,
        currentBalance: 1000,
        calculatedBalance: 1000,
        discrepancy: 0,
        transactionCount: 25,
        lastReconciliationDate: '2024-01-15',
      }

      expect(report.isBalanced).toBe(true)
      expect(report.discrepancy).toBe(0)
    })

    it('should allow valid WalletExportData object', () => {
      const exportData: WalletExportData = {
        wallet: {
          id: 'wallet-1',
          userId: 'user-1',
          balance: 1000,
          pendingBalance: 0,
          totalEarned: 2000,
          totalSpent: 1000,
          createdAt: '2024-01-01',
          updatedAt: '2024-01-15',
        },
        transactions: [],
        exportedAt: '2024-01-15T12:00:00Z',
        totalTransactions: 25,
        exportedBy: 'admin-1',
      }

      expect(exportData.totalTransactions).toBe(25)
    })

    it('should allow valid ApiResponse object', () => {
      const response: ApiResponse<CreditWallet> = {
        data: {
          id: 'wallet-1',
          userId: 'user-1',
          balance: 1000,
          pendingBalance: 0,
          totalEarned: 2000,
          totalSpent: 1000,
          createdAt: '2024-01-01',
          updatedAt: '2024-01-15',
        },
        message: 'Success',
        success: true,
      }

      expect(response.success).toBe(true)
      expect(response.data?.balance).toBe(1000)
    })

    it('should allow valid ApiError object', () => {
      const error: ApiError = {
        message: 'Insufficient balance',
        code: 'INSUFFICIENT_BALANCE',
        details: ['Required: 100', 'Available: 50'],
      }

      expect(error.code).toBe('INSUFFICIENT_BALANCE')
      expect(error.details).toHaveLength(2)
    })
  })
})
