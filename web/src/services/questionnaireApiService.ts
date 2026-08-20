/**
 * API service for questionnaire operations
 * Handles HTTP requests to the questionnaire endpoints
 */

import {
  QuestionnaireData,
  QuestionResponse,
  CreateQuestionnaireRequest,
  UpdateQuestionnaireRequest,
  QuestionnaireSearchRequest,
  QuestionnaireSearchResult,
  QuestionnaireResponseData,
  SubmitQuestionnaireResponseRequest,
  UpdateResponseStatusRequest,
  QuestionnaireAnalytics,
  QuestionnaireStatistics
} from '../types/questionnaire';
import { AUTH_CONFIG } from '../constants/auth';
import { fetchWithAuth } from '../utils/apiClient';

class QuestionnaireApiService {
  private baseUrl = '/api/questionnaire';

  private async makeRequest<T>(
    url: string,
    options: RequestInit = {}
  ): Promise<T> {
    return fetchWithAuth<T>(`${this.baseUrl}${url}`, options);
  }

  // Questionnaire Management

  /**
   * Create a new questionnaire with questions
   */
  async createQuestionnaire(data: CreateQuestionnaireRequest): Promise<QuestionnaireData> {
    return this.makeRequest<QuestionnaireData>('', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  /**
   * Update an existing questionnaire
   */
  async updateQuestionnaire(id: string, data: UpdateQuestionnaireRequest): Promise<QuestionnaireData> {
    return this.makeRequest<QuestionnaireData>(`/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  /**
   * Get questionnaire by ID with full details
   */
  async getQuestionnaire(id: string, includeQuestions: boolean = true): Promise<QuestionnaireData> {
    const params = new URLSearchParams();
    if (!includeQuestions) {
      params.set('includeQuestions', 'false');
    }
    
    const url = `/${id}${params.toString() ? `?${params.toString()}` : ''}`;
    return this.makeRequest<QuestionnaireData>(url);
  }

  /**
   * Search questionnaires with filtering and pagination
   */
  async searchQuestionnaires(searchParams: QuestionnaireSearchRequest): Promise<QuestionnaireSearchResult> {
    return this.makeRequest<QuestionnaireSearchResult>('/search', {
      method: 'POST',
      body: JSON.stringify(searchParams),
    });
  }

  /**
   * Get questionnaires created by current user
   */
  async getMyQuestionnaires(page: number = 1, pageSize: number = 20): Promise<QuestionnaireSearchResult> {
    const params = new URLSearchParams();
    params.set('page', page.toString());
    params.set('pageSize', pageSize.toString());
    
    return this.makeRequest<QuestionnaireSearchResult>(`/my-questionnaires?${params.toString()}`);
  }

  /**
   * Get questionnaires created by specific user
   */
  async getUserQuestionnaires(
    userId: string, 
    page: number = 1, 
    pageSize: number = 20
  ): Promise<QuestionnaireSearchResult> {
    const params = new URLSearchParams();
    params.set('page', page.toString());
    params.set('pageSize', pageSize.toString());
    
    return this.makeRequest<QuestionnaireSearchResult>(`/by-user/${userId}?${params.toString()}`);
  }

  /**
   * Delete a questionnaire (soft delete)
   */
  async deleteQuestionnaire(id: string): Promise<void> {
    return this.makeRequest<void>(`/${id}`, {
      method: 'DELETE',
    });
  }

  /**
   * Clone an existing questionnaire or template
   */
  async cloneQuestionnaire(id: string, newTitle: string): Promise<QuestionnaireData> {
    return this.makeRequest<QuestionnaireData>(`/${id}/clone`, {
      method: 'POST',
      body: JSON.stringify(newTitle),
    });
  }

  /**
   * Activate or deactivate a questionnaire
   */
  async setQuestionnaireStatus(id: string, isActive: boolean): Promise<QuestionnaireData> {
    return this.makeRequest<QuestionnaireData>(`/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify(isActive),
    });
  }

  // Question Management

  /**
   * Add a new question to a questionnaire
   * BUG-FE-008 FIX: Use 'unknown' instead of 'any' for improved type safety
   */
  async addQuestion(questionnaireId: string, questionData: unknown): Promise<unknown> {
    return this.makeRequest<unknown>(`/${questionnaireId}/questions`, {
      method: 'POST',
      body: JSON.stringify(questionData),
    });
  }

  /**
   * Update an existing question
   * BUG-FE-008 FIX: Use 'unknown' instead of 'any' for improved type safety
   */
  async updateQuestion(questionId: string, questionData: unknown): Promise<unknown> {
    return this.makeRequest<unknown>(`/questions/${questionId}`, {
      method: 'PUT',
      body: JSON.stringify(questionData),
    });
  }

  /**
   * Delete a question from a questionnaire
   */
  async deleteQuestion(questionId: string): Promise<void> {
    return this.makeRequest<void>(`/questions/${questionId}`, {
      method: 'DELETE',
    });
  }

  /**
   * Reorder questions within a questionnaire
   */
  async reorderQuestions(questionnaireId: string, questionOrders: Record<string, number>): Promise<void> {
    return this.makeRequest<void>(`/${questionnaireId}/questions/reorder`, {
      method: 'PATCH',
      body: JSON.stringify(questionOrders),
    });
  }

  /**
   * Add options to a multiple choice question
   * BUG-FE-008 FIX: Use 'unknown' instead of 'any' for improved type safety
   */
  async addQuestionOptions(questionId: string, options: unknown[]): Promise<unknown> {
    return this.makeRequest<unknown>(`/questions/${questionId}/options`, {
      method: 'POST',
      body: JSON.stringify(options),
    });
  }

  // Response Management

  /**
   * Start a new questionnaire response (draft)
   */
  async startResponse(questionnaireId: string, metadata?: string): Promise<QuestionnaireResponseData> {
    return this.makeRequest<QuestionnaireResponseData>(`/${questionnaireId}/responses/start`, {
      method: 'POST',
      body: JSON.stringify(metadata),
    });
  }

  /**
   * Save a draft response (partial completion)
   */
  async saveDraftResponse(responseId: string, questionResponses: QuestionResponse[]): Promise<QuestionnaireResponseData> {
    return this.makeRequest<QuestionnaireResponseData>(`/responses/${responseId}/draft`, {
      method: 'PUT',
      body: JSON.stringify(questionResponses),
    });
  }

  /**
   * Submit a completed questionnaire response
   */
  async submitResponse(data: SubmitQuestionnaireResponseRequest): Promise<QuestionnaireResponseData> {
    return this.makeRequest<QuestionnaireResponseData>('/responses/submit', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  /**
   * Get a questionnaire response by ID
   */
  async getResponse(id: string): Promise<QuestionnaireResponseData> {
    return this.makeRequest<QuestionnaireResponseData>(`/responses/${id}`);
  }

  /**
   * Get all responses for a questionnaire (for questionnaire owners)
   */
  async getQuestionnaireResponses(
    questionnaireId: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<{ responses: QuestionnaireResponseData[], totalCount: number, page: number, pageSize: number, totalPages: number, hasNextPage: boolean, hasPreviousPage: boolean }> {
    const params = new URLSearchParams();
    params.set('page', page.toString());
    params.set('pageSize', pageSize.toString());
    
    return this.makeRequest(`/${questionnaireId}/responses?${params.toString()}`);
  }

  /**
   * Get user's own responses to questionnaires
   */
  async getMyResponses(
    page: number = 1,
    pageSize: number = 20
  ): Promise<{ responses: QuestionnaireResponseData[], totalCount: number, page: number, pageSize: number, totalPages: number, hasNextPage: boolean, hasPreviousPage: boolean }> {
    const params = new URLSearchParams();
    params.set('page', page.toString());
    params.set('pageSize', pageSize.toString());
    
    return this.makeRequest(`/responses/my-responses?${params.toString()}`);
  }

  /**
   * Update response status (for review workflows)
   */
  async updateResponseStatus(responseId: string, data: UpdateResponseStatusRequest): Promise<QuestionnaireResponseData> {
    return this.makeRequest<QuestionnaireResponseData>(`/responses/${responseId}/status`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    });
  }

  /**
   * Delete a response (only allowed for drafts)
   */
  async deleteResponse(responseId: string): Promise<void> {
    return this.makeRequest<void>(`/responses/${responseId}`, {
      method: 'DELETE',
    });
  }

  // Analytics and Reporting

  /**
   * Get response analytics for a questionnaire
   */
  async getQuestionnaireAnalytics(questionnaireId: string): Promise<QuestionnaireAnalytics> {
    return this.makeRequest<QuestionnaireAnalytics>(`/${questionnaireId}/analytics`);
  }

  /**
   * Export questionnaire responses to CSV
   */
  async exportResponsesToCsv(questionnaireId: string): Promise<Blob> {
    // BUG-FE-002 FIX: Remove localStorage token, use httpOnly cookies
    const response = await fetch(`${this.baseUrl}/${questionnaireId}/export/csv`, {
      credentials: AUTH_CONFIG.CREDENTIALS,
    });

    if (!response.ok) {
      throw new Error(`Export failed: ${response.status}`);
    }

    return response.blob();
  }

  /**
   * Get aggregate statistics for a questionnaire
   */
  async getQuestionnaireStatistics(questionnaireId: string): Promise<QuestionnaireStatistics> {
    return this.makeRequest<QuestionnaireStatistics>(`/${questionnaireId}/statistics`);
  }

  // Validation and Utilities

  /**
   * Validate a questionnaire response before submission
   */
  async validateResponse(
    questionnaireId: string,
    questionResponses: QuestionResponse[]
  ): Promise<{ isValid: boolean, validationErrors: Record<string, string> }> {
    return this.makeRequest(`/${questionnaireId}/validate`, {
      method: 'POST',
      body: JSON.stringify(questionResponses),
    });
  }

  /**
   * Check if user can access a specific questionnaire
   */
  async checkAccess(questionnaireId: string): Promise<{ canAccess: boolean }> {
    return this.makeRequest(`/${questionnaireId}/access`);
  }

  /**
   * Check if user can submit a response to a questionnaire
   */
  async checkCanSubmit(questionnaireId: string): Promise<{ canSubmit: boolean }> {
    return this.makeRequest(`/${questionnaireId}/can-submit`);
  }

  /**
   * Get available questionnaire templates
   */
  async getAvailableTemplates(): Promise<QuestionnaireData[]> {
    return this.makeRequest<QuestionnaireData[]>('/templates');
  }
}

// Export singleton instance
export const questionnaireApiService = new QuestionnaireApiService();
export default questionnaireApiService;