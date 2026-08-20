/**
 * TypeScript types for questionnaire system
 * Mirrors backend DTOs for consistent data handling
 */

export enum QuestionnaireType {
  General = 0,
  ProjectIntake = 1,
  ClientOnboarding = 2,
  ProviderVetting = 3,
  ProjectFeedback = 4,
  SkillAssessment = 5,
  MarketResearch = 6
}

export enum QuestionType {
  Text = 0,
  LongText = 1,
  Number = 2,
  Email = 3,
  Phone = 4,
  Date = 5,
  Time = 6,
  DateTime = 7,
  Boolean = 8,
  Radio = 9,
  Checkbox = 10,
  Dropdown = 11,
  MultipleChoice = 12,
  Rating = 13,
  FileUpload = 14,
  Url = 15
}

export enum ResponseStatus {
  Draft = 0,
  Submitted = 1,
  UnderReview = 2,
  Approved = 3,
  Rejected = 4,
  NeedsRevision = 5
}

export interface QuestionOption {
  id: string;
  questionId: string;
  optionText: string;
  optionValue?: string;
  displayOrder: number;
  isActive: boolean;
  isDefault: boolean;
  metadata?: string;
  createdAt: string;
  updatedAt: string;
}

export interface QuestionnaireQuestion {
  id: string;
  questionnaireId: string;
  questionText: string;
  description?: string;
  type: QuestionType;
  isRequired: boolean;
  displayOrder: number;
  configuration?: string;
  defaultValue?: string;
  placeholderText?: string;
  validationRegex?: string;
  validationMessage?: string;
  minValue?: number;
  maxValue?: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  options: QuestionOption[];
}

export interface QuestionnaireData {
  id: string;
  title: string;
  description?: string;
  createdByUserId: string;
  createdByUserName?: string;
  type: QuestionnaireType;
  isActive: boolean;
  isTemplate: boolean;
  requiresReview: boolean;
  maxResponses?: number;
  startDate?: string;
  endDate?: string;
  version: number;
  metadata?: string;
  createdAt: string;
  updatedAt: string;
  questionCount: number;
  responseCount: number;
  isAvailable: boolean;
  questions: QuestionnaireQuestion[];
}

export interface QuestionResponse {
  id: string;
  questionnaireResponseId: string;
  questionId: string;
  questionText: string;
  questionType: QuestionType;
  responseValue?: string;
  selectedOptionIds?: string[];
  fileAttachments?: string[];
  metadata?: string;
  isValid: boolean;
  validationError?: string;
  createdAt: string;
  updatedAt: string;
}

export interface QuestionnaireResponseData {
  id: string;
  questionnaireId: string;
  questionnaireTitle: string;
  respondentUserId: string;
  respondentUserName?: string;
  status: ResponseStatus;
  isSubmitted: boolean;
  isComplete: boolean;
  startedAt: string;
  submittedAt?: string;
  updatedAt: string;
  submittedFromIP?: string;
  userAgent?: string;
  metadata?: string;
  reviewNotes?: string;
  reviewedByUserId?: string;
  reviewedByUserName?: string;
  reviewedAt?: string;
  completionPercentage: number;
  questionResponses: QuestionResponse[];
}

// Request DTOs

export interface CreateQuestionnaireRequest {
  title: string;
  description?: string;
  type: QuestionnaireType;
  isTemplate: boolean;
  requiresReview: boolean;
  maxResponses?: number;
  startDate?: string;
  endDate?: string;
  metadata?: string;
  questions: CreateQuestionRequest[];
}

export interface UpdateQuestionnaireRequest {
  id: string;
  title: string;
  description?: string;
  type: QuestionnaireType;
  isActive: boolean;
  isTemplate: boolean;
  requiresReview: boolean;
  maxResponses?: number;
  startDate?: string;
  endDate?: string;
  metadata?: string;
}

export interface CreateQuestionRequest {
  questionText: string;
  description?: string;
  type: QuestionType;
  isRequired: boolean;
  displayOrder: number;
  configuration?: string;
  defaultValue?: string;
  placeholderText?: string;
  validationRegex?: string;
  validationMessage?: string;
  minValue?: number;
  maxValue?: number;
  options: CreateQuestionOptionRequest[];
}

export interface UpdateQuestionRequest {
  id: string;
  questionText: string;
  description?: string;
  type: QuestionType;
  isRequired: boolean;
  isActive: boolean;
  displayOrder: number;
  configuration?: string;
  defaultValue?: string;
  placeholderText?: string;
  validationRegex?: string;
  validationMessage?: string;
  minValue?: number;
  maxValue?: number;
}

export interface CreateQuestionOptionRequest {
  optionText: string;
  optionValue?: string;
  displayOrder: number;
  isDefault: boolean;
  metadata?: string;
}

export interface SubmitQuestionResponseRequest {
  questionId: string;
  responseValue?: string;
  selectedOptionIds?: string[];
  fileAttachments?: string[];
  metadata?: string;
}

export interface SubmitQuestionnaireResponseRequest {
  questionnaireId: string;
  questionResponses: SubmitQuestionResponseRequest[];
  metadata?: string;
}

export interface UpdateResponseStatusRequest {
  responseId: string;
  status: ResponseStatus;
  reviewNotes?: string;
}

export interface QuestionnaireSearchRequest {
  searchTerm?: string;
  type?: QuestionnaireType;
  isActive?: boolean;
  isTemplate?: boolean;
  createdByUserId?: string;
  startDateFrom?: string;
  startDateTo?: string;
  endDateFrom?: string;
  endDateTo?: string;
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDescending: boolean;
}

export interface QuestionnaireSearchResult {
  questionnaires: QuestionnaireData[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Analytics and Statistics

export interface QuestionnaireAnalytics {
  totalResponses: number;
  completedResponses: number;
  incompletedResponses: number;
  avgCompletionPercentage: number;
  statusBreakdown: Record<string, number>;
  responsesOverTime: Record<string, number>;
}

export interface QuestionStatistics {
  questionId: string;
  questionText: string;
  type: string;
  isRequired: boolean;
  responseCount: number;
  skipRate: number;
}

export interface QuestionnaireStatistics {
  overview: {
    totalQuestions: number;
    requiredQuestions: number;
    totalResponses: number;
    completionRate: number;
    avgTimeToComplete: number;
  };
  questionStatistics: QuestionStatistics[];
}

// UI specific interfaces

export interface QuestionnaireFormState {
  questionnaire: QuestionnaireData | null;
  currentResponse: QuestionnaireResponseData | null;
  isLoading: boolean;
  isSubmitting: boolean;
  isSavingDraft: boolean;
  errors: Record<string, string>;
  isDirty: boolean;
}

export interface QuestionnaireListState {
  questionnaires: QuestionnaireData[];
  loading: boolean;
  error: string | null;
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
  filters: QuestionnaireSearchRequest;
}

export interface QuestionnaireBuilderState {
  questionnaire: Partial<QuestionnaireData>;
  questions: Partial<QuestionnaireQuestion>[];
  currentStep: number;
  isValid: boolean;
  isDirty: boolean;
  isSaving: boolean;
  errors: Record<string, string[]>;
}

// Constants

export const QUESTIONNAIRE_TYPE_LABELS: Record<QuestionnaireType, string> = {
  [QuestionnaireType.General]: 'General',
  [QuestionnaireType.ProjectIntake]: 'Project Intake',
  [QuestionnaireType.ClientOnboarding]: 'Client Onboarding',
  [QuestionnaireType.ProviderVetting]: 'Provider Vetting',
  [QuestionnaireType.ProjectFeedback]: 'Project Feedback',
  [QuestionnaireType.SkillAssessment]: 'Skill Assessment',
  [QuestionnaireType.MarketResearch]: 'Market Research'
};

export const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
  [QuestionType.Text]: 'Short Text',
  [QuestionType.LongText]: 'Long Text',
  [QuestionType.Number]: 'Number',
  [QuestionType.Email]: 'Email',
  [QuestionType.Phone]: 'Phone',
  [QuestionType.Date]: 'Date',
  [QuestionType.Time]: 'Time',
  [QuestionType.DateTime]: 'Date & Time',
  [QuestionType.Boolean]: 'Yes/No',
  [QuestionType.Radio]: 'Radio Button',
  [QuestionType.Checkbox]: 'Checkbox',
  [QuestionType.Dropdown]: 'Dropdown',
  [QuestionType.MultipleChoice]: 'Multiple Choice',
  [QuestionType.Rating]: 'Rating',
  [QuestionType.FileUpload]: 'File Upload',
  [QuestionType.Url]: 'URL'
};

export const RESPONSE_STATUS_LABELS: Record<ResponseStatus, string> = {
  [ResponseStatus.Draft]: 'Draft',
  [ResponseStatus.Submitted]: 'Submitted',
  [ResponseStatus.UnderReview]: 'Under Review',
  [ResponseStatus.Approved]: 'Approved',
  [ResponseStatus.Rejected]: 'Rejected',
  [ResponseStatus.NeedsRevision]: 'Needs Revision'
};

// Helper functions

export const getQuestionnaireTypeLabel = (type: QuestionnaireType): string => {
  return QUESTIONNAIRE_TYPE_LABELS[type] || 'Unknown';
};

export const getQuestionTypeLabel = (type: QuestionType): string => {
  return QUESTION_TYPE_LABELS[type] || 'Unknown';
};

export const getResponseStatusLabel = (status: ResponseStatus): string => {
  return RESPONSE_STATUS_LABELS[status] || 'Unknown';
};

export const isQuestionTypeWithOptions = (type: QuestionType): boolean => {
  return [
    QuestionType.Radio,
    QuestionType.Checkbox,
    QuestionType.Dropdown,
    QuestionType.MultipleChoice
  ].includes(type);
};

export const isQuestionTypeWithMultipleValues = (type: QuestionType): boolean => {
  return type === QuestionType.Checkbox;
};

export const getQuestionValidationRules = (question: QuestionnaireQuestion) => {
  const rules: any = {};

  if (question.isRequired) {
    rules.required = 'This field is required';
  }

  if (question.minValue !== undefined && question.type === QuestionType.Text) {
    rules.minLength = {
      value: question.minValue,
      message: `Minimum length is ${question.minValue} characters`
    };
  }

  if (question.maxValue !== undefined && question.type === QuestionType.Text) {
    rules.maxLength = {
      value: question.maxValue,
      message: `Maximum length is ${question.maxValue} characters`
    };
  }

  if (question.validationRegex) {
    rules.pattern = {
      value: new RegExp(question.validationRegex),
      message: question.validationMessage || 'Invalid format'
    };
  }

  return rules;
};