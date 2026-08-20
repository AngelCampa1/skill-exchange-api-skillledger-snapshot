/**
 * API service for feedback submissions
 * Handles HTTP requests to the feedback endpoint
 */

import { AUTH_CONFIG } from '../constants/auth';

export type FeedbackCategory = 'General' | 'Bug' | 'FeatureRequest' | 'Other';

export interface SubmitFeedbackRequest {
  category: FeedbackCategory;
  message: string;
  replyToEmail?: string;
}

export interface FeedbackResponse {
  success: boolean;
  message: string;
}

class FeedbackApiService {
  private baseUrl = '/api';

  private async makeRequest<T>(
    url: string,
    options: RequestInit = {}
  ): Promise<T> {
    const defaultHeaders: HeadersInit = {
      'Content-Type': 'application/json',
    };

    const response = await fetch(`${this.baseUrl}${url}`, {
      ...options,
      credentials: AUTH_CONFIG.CREDENTIALS,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
    });

    if (!response.ok) {
      if (response.status === 429) {
        throw new Error('Too many feedback submissions. Please try again later.');
      }
      const errorData = await response.json().catch(() => ({
        message: `HTTP ${response.status}: ${response.statusText}`
      }));
      throw new Error(errorData.message || `Request failed: ${response.status}`);
    }

    const contentType = response.headers.get('content-type');
    if (!contentType?.includes('application/json')) {
      return {} as T;
    }

    return response.json();
  }

  /**
   * Submit user feedback
   * Available to both authenticated and anonymous users
   */
  async submitFeedback(request: SubmitFeedbackRequest): Promise<FeedbackResponse> {
    return this.makeRequest<FeedbackResponse>('/feedback', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }
}

export const feedbackApiService = new FeedbackApiService();
