// Milestone and deliverable tracking types for US-4.3.1

export enum MilestoneStatus {
  NotStarted = 'NotStarted',
  InProgress = 'InProgress',
  PendingReview = 'PendingReview',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export enum MilestonePriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum DeliverableType {
  FileUpload = 'FileUpload',
  Text = 'Text',
  Link = 'Link',
  CodeRepository = 'CodeRepository'
}

export interface ProjectMilestone {
  id: string;
  projectId: string;
  escrowMilestoneId?: string;
  title: string;
  description: string;
  status: MilestoneStatus;
  priority: MilestonePriority;
  dueDate?: string;
  completedAt?: string;
  sequenceOrder: number;
  weightPercentage: number;
  acceptanceCriteria?: string;
  reviewNotes?: string;
  createdByUserId: string;
  createdByUserName: string;
  assignedToUserId?: string;
  assignedToUserName?: string;
  createdAt: string;
  updatedAt: string;
  
  // Calculated properties
  isOverdue: boolean;
  canBeStarted: boolean;
  canBeSubmitted: boolean;
  canBeApproved: boolean;
  daysUntilDue?: number;
  
  // Related data
  submissions: SubmissionSummary[];
}

export interface SubmissionSummary {
  id: string;
  title: string;
  type: DeliverableType;
  submittedAt: string;
  isReviewed: boolean;
  isApproved: boolean;
  attachmentCount: number;
  totalFileSize: number;
}

export interface DeliverableSubmission {
  id: string;
  milestoneId: string;
  submittedByUserId: string;
  submittedByUserName: string;
  type: DeliverableType;
  title: string;
  description?: string;
  submissionUrl?: string;
  textContent?: string;
  submittedAt: string;
  submissionNotes?: string;
  isReviewed: boolean;
  isApproved: boolean;
  reviewedAt?: string;
  reviewedByUserId?: string;
  reviewedByUserName?: string;
  reviewFeedback?: string;
  attachedFiles: AttachedFile[];
  
  // Calculated properties
  canBeReviewed: boolean;
  totalFileSize: number;
  attachmentCount: number;
}

export interface AttachedFile {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  fileUrl: string;
}

export interface ProjectProgress {
  projectId: string;
  totalMilestones: number;
  completedMilestones: number;
  inProgressMilestones: number;
  overdueMilestones: number;
  overallProgressPercentage: number;
  nextMilestoneDue?: string;
  upcomingMilestones: ProjectMilestone[];
  overdueMilestonesList: ProjectMilestone[];
}

export interface MilestoneFilter {
  projectId?: string;
  status?: MilestoneStatus;
  priority?: MilestonePriority;
  assignedToUserId?: string;
  createdByUserId?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
  overdueOnly?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface PaginatedMilestones {
  items: ProjectMilestone[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// Request DTOs
export interface CreateMilestoneRequest {
  projectId: string;
  title: string;
  description: string;
  priority: MilestonePriority;
  dueDate?: string;
  sequenceOrder: number;
  weightPercentage: number;
  acceptanceCriteria?: string;
  assignedToUserId?: string;
  escrowMilestoneId?: string;
}

export interface UpdateMilestoneRequest {
  title?: string;
  description?: string;
  priority?: MilestonePriority;
  dueDate?: string;
  sequenceOrder?: number;
  weightPercentage?: number;
  acceptanceCriteria?: string;
  assignedToUserId?: string;
}

export interface CreateSubmissionRequest {
  milestoneId: string;
  type: DeliverableType;
  title: string;
  description?: string;
  submissionUrl?: string;
  textContent?: string;
  submissionNotes?: string;
  attachedFileIds?: string[];
}

export interface ReviewSubmissionRequest {
  isApproved: boolean;
  reviewFeedback: string;
}

// Real-time update types
export interface MilestoneUpdate {
  milestoneId: string;
  projectId: string;
  type: 'created' | 'updated' | 'started' | 'submitted' | 'approved' | 'rejected' | 'cancelled' | 'deleted';
  data: Partial<ProjectMilestone>;
  updatedBy: string;
  timestamp: string;
}

export interface ProgressUpdate {
  milestoneId: string;
  progressPercentage: number;
  updatedBy: string;
  updatedAt: string;
}

// UI State types
export interface MilestoneUIState {
  selectedMilestone?: ProjectMilestone;
  isCreating: boolean;
  isEditing: boolean;
  isSubmitting: boolean;
  showSubmissions: boolean;
  filter: MilestoneFilter;
  sortBy: string;
  sortDirection: 'asc' | 'desc';
}

// Error handling
export interface MilestoneError {
  code: string;
  message: string;
  field?: string;
}

export interface ApiResponse<T> {
  data?: T;
  error?: MilestoneError;
  success: boolean;
}