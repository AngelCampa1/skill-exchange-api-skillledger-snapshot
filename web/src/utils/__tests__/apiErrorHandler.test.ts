import {
  ApiError,
  handleApiError,
  authenticatedFetch,
  isApiError,
  getErrorMessage,
} from '../apiErrorHandler'

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

describe('API Error Handler', () => {
  beforeEach(() => {
    mockFetch.mockClear()
    jest.clearAllMocks()
  })

  describe('ApiError class', () => {
    it('should create an ApiError instance with all properties', () => {
      const error = new ApiError(404, 'Not found', { details: 'test' })

      expect(error).toBeInstanceOf(Error)
      expect(error).toBeInstanceOf(ApiError)
      expect(error.statusCode).toBe(404)
      expect(error.message).toBe('Not found')
      expect(error.details).toEqual({ details: 'test' })
      expect(error.name).toBe('ApiError')
    })
  })

  describe('handleApiError', () => {
    it('should handle 401 Unauthorized and trigger logout', async () => {
      const mockLogout = jest.fn()
      const auth = { logout: mockLogout }

      const response = new Response(
        JSON.stringify({ success: false, message: 'Token expired' }),
        { status: 401 }
      )

      try {
        await handleApiError(response, auth)
        fail('Expected error to be thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(401)
        expect((error as ApiError).message).toBe('Token expired')
      }

      expect(mockLogout).toHaveBeenCalled()
    })

    it('should handle 401 without auth context', async () => {
      const response = new Response(
        JSON.stringify({ success: false, message: 'Unauthorized' }),
        { status: 401 }
      )

      await expect(handleApiError(response)).rejects.toThrow(ApiError)

      try {
        await handleApiError(response)
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(401)
      }
    })

    it('should handle 403 Forbidden errors', async () => {
      const response = new Response(
        JSON.stringify({ success: false, message: 'Insufficient permissions' }),
        { status: 403 }
      )

      try {
        await handleApiError(response)
        fail('Expected error to be thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(403)
        expect((error as ApiError).message).toBe('Insufficient permissions')
      }
    })

    it('should handle 404 Not Found errors', async () => {
      const response = new Response(
        JSON.stringify({ success: false, message: 'Resource not found' }),
        { status: 404 }
      )

      await expect(handleApiError(response)).rejects.toThrow(ApiError)

      try {
        await handleApiError(response)
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(404)
      }
    })

    it('should handle 422 Validation errors', async () => {
      const response = new Response(
        JSON.stringify({
          success: false,
          message: 'Validation failed',
          errors: { email: ['Invalid email format'] },
        }),
        { status: 422 }
      )

      try {
        await handleApiError(response)
        fail('Expected error to be thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(422)
        expect((error as ApiError).details.errors).toBeDefined()
      }
    })

    it('should handle 429 Rate Limit errors', async () => {
      const response = new Response(
        JSON.stringify({ success: false, message: 'Too many requests' }),
        { status: 429 }
      )

      await expect(handleApiError(response)).rejects.toThrow(ApiError)

      try {
        await handleApiError(response)
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(429)
      }
    })

    it('should handle 500 Internal Server Error', async () => {
      const response = new Response(
        JSON.stringify({ success: false, message: 'Internal server error' }),
        { status: 500 }
      )

      await expect(handleApiError(response)).rejects.toThrow(ApiError)

      try {
        await handleApiError(response)
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(500)
      }
    })

    it('should handle non-JSON error responses', async () => {
      const response = new Response('Internal Server Error', {
        status: 500,
        statusText: 'Internal Server Error',
      })

      await expect(handleApiError(response)).rejects.toThrow(ApiError)

      try {
        await handleApiError(response)
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).statusCode).toBe(500)
        expect((error as ApiError).message).toContain('Internal Server Error')
      }
    })

    it('should use default messages when none provided', async () => {
      const response = new Response(JSON.stringify({ success: false }), { status: 403 })

      try {
        await handleApiError(response)
        fail('Expected error to be thrown')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError)
        expect((error as ApiError).message).toBe(
          'You do not have permission to perform this action.'
        )
      }
    })
  })

  describe('authenticatedFetch', () => {
    it('should make authenticated request with cookies', async () => {
      const mockData = { success: true, data: 'test' }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      const result = await authenticatedFetch('/api/test')

      expect(result).toEqual(mockData)
      expect(mockFetch).toHaveBeenCalledWith('/api/test', {
        credentials: 'include',
      })
    })

    it('should include credentials in all requests', async () => {
      const mockData = { success: true, data: 'test' }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      const result = await authenticatedFetch('/api/test', {})

      expect(result).toEqual(mockData)
      expect(mockFetch).toHaveBeenCalledWith('/api/test', {
        credentials: 'include',
      })
    })

    it('should handle 401 errors and trigger logout', async () => {
      const auth = { logout: jest.fn() }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: async () => ({ success: false, message: 'Unauthorized' }),
      })

      await expect(authenticatedFetch('/api/test', {}, auth)).rejects.toThrow(ApiError)

      expect(auth.logout).toHaveBeenCalled()
    })

    it('should handle 403 errors without logout', async () => {
      const auth = { logout: jest.fn() }

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 403,
        json: async () => ({ success: false, message: 'Forbidden' }),
      })

      await expect(authenticatedFetch('/api/test', {}, auth)).rejects.toThrow(ApiError)

      expect(auth.logout).not.toHaveBeenCalled()
    })

    it('should pass through fetch options', async () => {
      const mockData = { success: true }

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockData,
      })

      await authenticatedFetch('/api/test', {
        method: 'POST',
        body: JSON.stringify({ test: 'data' }),
        headers: {
          'Content-Type': 'application/json',
        },
      })

      expect(mockFetch).toHaveBeenCalledWith('/api/test', {
        method: 'POST',
        body: JSON.stringify({ test: 'data' }),
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
        },
      })
    })
  })

  describe('isApiError', () => {
    it('should return true for ApiError instances', () => {
      const error = new ApiError(404, 'Not found')
      expect(isApiError(error)).toBe(true)
    })

    it('should return true for ApiError with matching status code', () => {
      const error = new ApiError(404, 'Not found')
      expect(isApiError(error, 404)).toBe(true)
    })

    it('should return false for ApiError with non-matching status code', () => {
      const error = new ApiError(404, 'Not found')
      expect(isApiError(error, 401)).toBe(false)
    })

    it('should return false for non-ApiError instances', () => {
      const error = new Error('Regular error')
      expect(isApiError(error)).toBe(false)
    })

    it('should return false for null/undefined', () => {
      expect(isApiError(null)).toBe(false)
      expect(isApiError(undefined)).toBe(false)
    })
  })

  describe('getErrorMessage', () => {
    it('should extract message from ApiError', () => {
      const error = new ApiError(404, 'Resource not found')
      expect(getErrorMessage(error)).toBe('Resource not found')
    })

    it('should extract message from regular Error', () => {
      const error = new Error('Something went wrong')
      expect(getErrorMessage(error)).toBe('Something went wrong')
    })

    it('should return string errors as-is', () => {
      expect(getErrorMessage('Error message')).toBe('Error message')
    })

    it('should return default message for unknown error types', () => {
      expect(getErrorMessage({})).toBe('An unexpected error occurred. Please try again.')
      expect(getErrorMessage(123)).toBe('An unexpected error occurred. Please try again.')
      expect(getErrorMessage(null)).toBe('An unexpected error occurred. Please try again.')
    })
  })
})
