/**
 * CreditWallet API Types for SkillLedger Frontend
 * Based on the backend CreditWallet DTOs and API responses
 */

export interface CreditWallet {
  id: string;
  userId: string;
  balance: number;
  pendingBalance: number;
  totalEarned: number;
  totalSpent: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreditTransaction {
  id: string;
  fromUserId?: string;
  toUserId?: string;
  amount: number;
  type: CreditTransactionType;
  description: string;
  status: TransactionStatus;
  projectId?: string;
  createdAt: string;
  completedAt?: string;
}

export enum CreditTransactionType {
  StartingCredit = 'StartingCredit',
  ProjectPayment = 'ProjectPayment',
  EscrowDeposit = 'EscrowDeposit',
  EscrowRelease = 'EscrowRelease',
  EscrowRefund = 'EscrowRefund',
  Purchase = 'Purchase',
  Refund = 'Refund',
  AdminAdjustment = 'AdminAdjustment'
}

export enum TransactionStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled'
}

export interface WalletDashboardData {
  wallet: CreditWallet;
  recentTransactions: CreditTransaction[];
  activeEscrows: CreditTransaction[];
  totalActiveEscrowAmount: number;
  availableBalance: number;
}

export interface TransferCreditsRequest {
  toUserId: string;
  amount: number;
  description: string;
  transactionType: CreditTransactionType;
  projectId?: string;
}

export interface CreateEscrowRequest {
  projectId: string;
  amount: number;
  description: string;
}

export interface TransactionHistoryRequest {
  limit?: number;
  offset?: number;
  fromDate?: string;
  toDate?: string;
  transactionType?: CreditTransactionType;
}

export interface FraudAnalysisReport {
  isHighRisk: boolean;
  riskScore: number;
  riskFactors: string[];
  recommendedActions: string[];
  analysisTimestamp: string;
}

export interface BalanceReconciliationReport {
  isBalanced: boolean;
  currentBalance: number;
  calculatedBalance: number;
  discrepancy: number;
  transactionCount: number;
  lastReconciliationDate: string;
}

export interface WalletExportData {
  wallet: CreditWallet;
  transactions: CreditTransaction[];
  exportedAt: string;
  totalTransactions: number;
  exportedBy: string;
}

// API Response wrappers
export interface ApiResponse<T> {
  data?: T;
  message: string;
  success: boolean;
}

export interface ApiError {
  message: string;
  code?: string;
  details?: string[];
}