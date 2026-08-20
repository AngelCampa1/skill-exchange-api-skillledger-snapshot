/**
 * Badge system types for frontend components
 */

export enum BadgeCategory {
  Performance = 'Performance',
  Volume = 'Volume',
  Expertise = 'Expertise',
  Trust = 'Trust',
  Community = 'Community',
  Achievement = 'Achievement'
}

export enum VerificationLevel {
  Automatic = 'Automatic',
  Manual = 'Manual',
  External = 'External'
}

export interface UserBadge {
  id: string;
  userId: string;
  badgeType: string;
  badgeName: string;
  badgeDescription: string;
  category: BadgeCategory;
  iconUrl?: string;
  earnedAt: string; // ISO date string
  expiresAt?: string; // ISO date string
  isActive: boolean;
  verificationLevel: VerificationLevel;
  verificationEvidence?: string;
  verifiedBy?: string;
  verifiedAt?: string; // ISO date string
  integrityHash?: string;
}

export interface BadgeProgress {
  badgeType: string;
  badgeName: string;
  category: BadgeCategory;
  description: string;
  iconUrl?: string;
  currentProgress: number;
  maxProgress: number;
  progressPercentage: number;
  requirements: string[];
  isEligible: boolean;
  nextMilestone?: string;
}

export interface VerificationRequest {
  id: string;
  userId: string;
  badgeType: string;
  evidence: Record<string, any>;
  status: 'Pending' | 'Approved' | 'Rejected';
  reviewNotes?: string;
  submittedAt: string; // ISO date string
  reviewedAt?: string; // ISO date string
  reviewedBy?: string;
}

export interface BadgeDefinition {
  badgeType: string;
  name: string;
  description: string;
  category: BadgeCategory;
  iconUrl?: string;
  verificationLevel: VerificationLevel;
  isActive: boolean;
  criteria: BadgeCriteria[];
}

export interface BadgeCriteria {
  id: string;
  criteriaType: string;
  targetValue: number;
  description: string;
  isRequired: boolean;
}

export interface ExternalVerificationResult {
  platform: string;
  isVerified: boolean;
  profileData?: any;
  verificationDate: string;
  expiresAt?: string;
}

// API Request/Response types
export interface SubmitVerificationRequestDto {
  badgeType: string;
  evidence?: Record<string, any>;
}

export interface ProcessVerificationRequestDto {
  approved: boolean;
  reviewNotes?: string;
}

export interface AwardBadgeRequestDto {
  userId: string;
  badgeType: string;
  evidence?: Record<string, any>;
}

export interface RevokeBadgeRequestDto {
  reason: string;
}

export interface LinkedInVerificationRequest {
  linkedInUrl: string;
}

export interface GitHubVerificationRequest {
  gitHubUsername: string;
}

// Component prop types
export interface BadgeDisplayProps {
  badge: UserBadge;
  size?: 'small' | 'medium' | 'large';
  showDetails?: boolean;
  showVerificationCode?: boolean;
  onClick?: () => void;
}

export interface BadgeListProps {
  badges: UserBadge[];
  category?: BadgeCategory;
  showExpired?: boolean;
  groupByCategory?: boolean;
}

export interface BadgeProgressProps {
  progress: BadgeProgress[];
  userId: string;
}

export interface VerificationRequestFormProps {
  badgeType: string;
  onSubmit: (evidence: Record<string, any>) => Promise<void>;
  onCancel: () => void;
}