/**
 * API Error Handler Utility
 *
 * Provides centralized handling of authentication and authorization errors
 * from API requests, with automatic logout on token expiration.
 */

import { AuthContextType } from '@/contexts/AuthContext'

export class ApiError extends Error {
  constructor(
    public statusCode: number,
    public message: string,
    public details?: any
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

export interface ApiErrorResponse {
  success: false
  message: string
  errors?: Record<string, string[]>
  statusCode?: number
}

/**
 * Handle API response errors with proper authentication handling
 *
 * @param response - Fetch Response object
 * @param auth - Auth context (optional, for automatic logout on 401)
 * @throws ApiError with status code and message
 */
export async function handleApiError(
  response: Response,
  auth?: Pick<AuthContextType, 'logout'>
): Promise<never> {
  let errorData: ApiErrorResponse

  try {
    errorData = await response.json()
  } catch {
    // If response body is not JSON, use generic error
    errorData = {
      success: false,
      message: response.statusText || 'An unexpected error occurred',
      statusCode: response.status,
    }
  }

  // Handle specific status codes
  switch (response.status) {
    case 401:
      // Unauthorized - token expired or invalid
      if (auth) {
        // Automatically logout user
        await auth.logout()
      }
      throw new ApiError(
        401,
        errorData.message || 'Your session has expired. Please log in again.',
        errorData
      )

    case 403:
      // Forbidden - insufficient permissions
      throw new ApiError(
        403,
        errorData.message || 'You do not have permission to perform this action.',
        errorData
      )

    case 404:
      // Not found
      throw new ApiError(
        404,
        errorData.message || 'The requested resource was not found.',
        errorData
      )

    case 422:
      // Validation error
      throw new ApiError(
        422,
        errorData.message || 'Validation failed. Please check your input.',
        errorData
      )

    case 429:
      // Too many requests
      throw new ApiError(
        429,
        errorData.message || 'Too many requests. Please wait a moment and try again.',
        errorData
      )

    case 500:
      // Internal server error
      throw new ApiError(
        500,
        errorData.message || 'An internal server error occurred. Please try again later.',
        errorData
      )

    default:
      // Generic error
      throw new ApiError(
        response.status,
        errorData.message || `Request failed with status ${response.status}`,
        errorData
      )
  }
}

/**
 * Make an authenticated API request with automatic error handling
 *
 * @param url - API endpoint URL
 * @param options - Fetch options
 * @param auth - Auth context (optional, for automatic logout on 401)
 * @returns Response data
 * @throws ApiError on failure
 */
export async function authenticatedFetch<T = any>(
  url: string,
  options: RequestInit = {},
  auth?: Pick<AuthContextType, 'logout'>
): Promise<T> {
  const fetchOptions = { ...options }

  // Include credentials to send httpOnly cookies
  fetchOptions.credentials = 'include'

  // Make the request
  const response = await fetch(url, fetchOptions)

  // Handle errors
  if (!response.ok) {
    await handleApiError(response, auth)
  }

  // Parse and return JSON response
  return await response.json()
}

/**
 * Check if an error is an ApiError with a specific status code
 *
 * @param error - Error to check
 * @param statusCode - Expected status code
 * @returns true if error matches
 */
// BUG-FE-008 FIX: Use 'unknown' instead of 'any' for improved type safety
export function isApiError(error: unknown, statusCode?: number): error is ApiError {
  if (!(error instanceof ApiError)) {
    return false
  }

  if (statusCode !== undefined) {
    return error.statusCode === statusCode
  }

  return true
}

/**
 * Extract user-friendly error message from any error type
 *
 * @param error - Error object
 * @returns User-friendly error message
 */
// BUG-FE-008 FIX: Use 'unknown' instead of 'any' for improved type safety
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  if (typeof error === 'string') {
    return error
  }

  return 'An unexpected error occurred. Please try again.'
}
