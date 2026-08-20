/**
 * Tests for feedbackApiService.ts
 *
 * This file tests the feedback API service for submitting user feedback
 */

import { feedbackApiService, type SubmitFeedbackRequest, type FeedbackResponse } from '@/services/feedbackApiService'
import { AUTH_CONFIG } from '@/constants/auth'

// Mock fetch
global.fetch = jest.fn()

describe('feedbackApiService', () => {
  let mockFetch: jest.Mock

  beforeEach(() => {
    mockFetch = global.fetch as jest.Mock
    mockFetch.mockClear()
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('submitFeedback', () => {
    it('should successfully submit general feedback', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Great platform!',
        replyToEmail: 'user@example.com',
      }

      const mockResponse: FeedbackResponse = {
        success: true,
        message: 'Thank you for your feedback!',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual(mockResponse)
      expect(mockFetch).toHaveBeenCalledWith('/api/feedback', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      })
    })

    it('should successfully submit bug report', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'Bug',
        message: 'Found a bug in the login form',
      }

      const mockResponse: FeedbackResponse = {
        success: true,
        message: 'Bug report received',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual(mockResponse)
    })

    it('should successfully submit feature request', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'FeatureRequest',
        message: 'Please add Light-Only Mode',
        replyToEmail: 'feature@example.com',
      }

      const mockResponse: FeedbackResponse = {
        success: true,
        message: 'Feature request received',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual(mockResponse)
    })

    it('should successfully submit other category feedback', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'Other',
        message: 'Just wanted to say hello',
      }

      const mockResponse: FeedbackResponse = {
        success: true,
        message: 'Feedback received',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual(mockResponse)
    })

    it('should handle rate limiting (429 status)', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test message',
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 429,
        statusText: 'Too Many Requests',
        json: async () => ({ message: 'Rate limit exceeded' }),
      })

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'Too many feedback submissions. Please try again later.'
      )
    })

    it('should handle 400 Bad Request errors', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: '',
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        json: async () => ({ message: 'Message cannot be empty' }),
      })

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'Message cannot be empty'
      )
    })

    it('should handle 500 Internal Server Error', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        json: async () => ({ message: 'Server error occurred' }),
      })

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'Server error occurred'
      )
    })

    it('should handle errors without JSON body', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 503,
        statusText: 'Service Unavailable',
        json: async () => {
          throw new Error('Not JSON')
        },
      })

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'HTTP 503: Service Unavailable'
      )
    })

    it('should handle error responses without message field', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: async () => ({}),
      })

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'Request failed: 404'
      )
    })

    it('should handle non-JSON successful responses', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'text/plain' }),
        json: async () => ({}),
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual({})
    })

    it('should handle missing content-type header', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({}),
        json: async () => ({}),
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual({})
    })

    it('should handle network errors', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'Network error'
      )
    })

    it('should submit feedback without optional replyToEmail', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'Bug',
        message: 'Anonymous bug report',
      }

      const mockResponse: FeedbackResponse = {
        success: true,
        message: 'Feedback received',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual(mockResponse)
      expect(mockFetch).toHaveBeenCalledWith('/api/feedback', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      })
    })

    it('should handle long feedback messages', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'FeatureRequest',
        message: 'A'.repeat(10000), // Very long message
        replyToEmail: 'user@example.com',
      }

      const mockResponse: FeedbackResponse = {
        success: true,
        message: 'Long feedback received',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => mockResponse,
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual(mockResponse)
    })

    it('should use correct base URL and endpoint', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ success: true, message: 'OK' }),
      })

      await feedbackApiService.submitFeedback(request)

      const callArgs = mockFetch.mock.calls[0]
      expect(callArgs[0]).toBe('/api/feedback')
    })

    it('should include credentials in request', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => ({ success: true, message: 'OK' }),
      })

      await feedbackApiService.submitFeedback(request)

      const callArgs = mockFetch.mock.calls[0]
      expect(callArgs[1].credentials).toBe(AUTH_CONFIG.CREDENTIALS)
    })
  })

  describe('edge cases', () => {
    it('should handle malformed JSON in successful response', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: async () => {
          throw new Error('Malformed JSON')
        },
      })

      await expect(feedbackApiService.submitFeedback(request)).rejects.toThrow(
        'Malformed JSON'
      )
    })

    it('should handle partial content-type headers', async () => {
      const request: SubmitFeedbackRequest = {
        category: 'General',
        message: 'Test',
      }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json; charset=utf-8' }),
        json: async () => ({ success: true, message: 'OK' }),
      })

      const result = await feedbackApiService.submitFeedback(request)

      expect(result).toEqual({ success: true, message: 'OK' })
    })
  })
})
