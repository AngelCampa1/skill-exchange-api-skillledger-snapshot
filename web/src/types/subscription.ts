/**
 * Subscription System API Types for SkillLedger Frontend
 * Based on the backend Subscription DTOs and API responses
 */

export interface SubscriptionTier {
  id: string;
  name: string;
  description?: string;
  price: number;
  annualPrice?: number;
  creditBonus: number;
  maxActiveProjects: number;
  maxTeamMembers: number;
  prioritySupport: boolean;
  apiAccess: boolean;
  advancedAnalytics: boolean;
  advancedFraudDetection: boolean;
  multiSignature: boolean;
  customIntegrations: boolean;
  maxMonthlyEarnings: number;
  features: string[];
  sortOrder: number;
}

export interface UserSubscription {
  id: string;
  userId: string;
  subscriptionTierId: string;
  tier: SubscriptionTier;
  status: SubscriptionStatus;
  startDate: string;
  endDate?: string;
  nextBillingDate?: string;
  cancelAtPeriodEnd: boolean;
  isTrial: boolean;
  trialEndDate?: string;
  externalSubscriptionId?: string;
  externalCustomerId?: string;
  paymentMethodId?: string;
  createdAt: string;
  updatedAt: string;
}

export enum SubscriptionStatus {
  Active = 'Active',
  Trial = 'Trial',
  Cancelled = 'Cancelled',  // Note: Backend uses double 'l'
  Expired = 'Expired',
  Suspended = 'Suspended',
  PastDue = 'PastDue'
}

export enum BillingCycle {
  Monthly = 'Monthly',
  Annual = 'Annual'
}

export interface CheckoutSessionResult {
  success: boolean;
  sessionId?: string;
  sessionUrl?: string;
  customerId?: string;
  tierId?: string;
  tierName?: string;
  amount: number;
  currency: string;
  billingCycle: BillingCycle;
  isPaymentMethodSetup?: boolean;
  errorMessage?: string;
}

export interface CreateSubscriptionRequest {
  tierId: string;
  billingCycle: BillingCycle;
  successUrl?: string;
  cancelUrl?: string;
  promoCode?: string;
}

export interface PaymentMethodSetupRequest {
  successUrl: string;
  cancelUrl: string;
  setAsDefault?: boolean;
}

export interface SubscriptionLimits {
  maxActiveProjects: number;
  maxTeamMembers: number;
  maxMonthlyEarnings: number;
  prioritySupport: boolean;
  apiAccess: boolean;
  advancedAnalytics: boolean;
  advancedFraudDetection: boolean;
  multiSignature: boolean;
  customIntegrations: boolean;
  features: string[];
}

export interface PaymentMethod {
  id: string;
  type: string;
  brand: string;
  last4: string;
  expiryMonth?: number;
  expiryYear?: number;
  isDefault: boolean;
  createdAt: string;
}

export interface BillingHistory {
  id: string;
  subscriptionId: string;
  type: BillingType;
  amount: number;
  currency: string;
  status: BillingStatus;
  date: string;
  description: string;
  invoiceUrl?: string;
  receiptUrl?: string;
}

export enum BillingType {
  Subscription = 'Subscription',
  Invoice = 'Invoice',
  OneTime = 'OneTime'
}

export enum BillingStatus {
  Succeeded = 'Succeeded',
  Pending = 'Pending',
  Failed = 'Failed',
  Void = 'Void'
}

export interface SubscriptionStatistics {
  totalSubscriptions: number;
  activeSubscriptions: number;
  trialSubscriptions: number;
  cancelledSubscriptions: number;
  expiredSubscriptions: number;
  monthlyRecurringRevenue: number;
  annualRecurringRevenue: number;
  newSubscriptionsThisPeriod: number;
  churnedSubscriptionsThisPeriod: number;
  subscriptionsByTier: Record<string, number>;
  subscriptionsByStatus: Record<string, number>;
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

// Subscription upgrade/downgrade types
export interface ChangeTierRequest {
  newTierId: string;
  immediateCharge?: boolean;
  effectiveDate?: string;
}

export interface CancelSubscriptionRequest {
  reason?: string;
  immediate?: boolean;
}

// Usage tracking
export interface SubscriptionUsage {
  activeProjects: number;
  teamMembers: number;
  monthlyEarnings: number;
  apiCalls: number;
  storageUsed: number;
  periodStart: string;
  periodEnd: string;
}