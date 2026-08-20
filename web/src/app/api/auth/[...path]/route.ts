import { NextRequest, NextResponse } from 'next/server'
import { logger } from '@/utils/logger';

// BUG-FE-012 FIX: Use environment variable for API base URL
// This proxy route forwards auth requests to the backend and handles cookie forwarding
// In production, NEXT_PUBLIC_API_URL MUST be set by the deployment environment
const BACKEND_URL = process.env.NEXT_PUBLIC_API_URL || (
  process.env.NODE_ENV === 'production'
    ? (() => { throw new Error('NEXT_PUBLIC_API_URL must be set in production'); })()
    : 'http://localhost:8030'
)

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params
  const path = resolvedParams.path.join('/')
  const url = `${BACKEND_URL}/api/auth/${path}`

  // BUG-LOW-021 FIX: Add correlation ID for request tracing
  const correlationId = request.headers.get('x-correlation-id') ||
    `auth-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`

  // Forward cookies from the request
  const cookieHeader = request.headers.get('cookie')

  logger.debug('Auth proxy GET request', { path, correlationId })
  logger.debug('Auth proxy GET incoming cookies', { cookies: cookieHeader || 'none', correlationId })

  const headers: HeadersInit = {
    'X-Correlation-ID': correlationId
  }
  if (cookieHeader) {
    headers['Cookie'] = cookieHeader
  }

  const response = await fetch(url, {
    method: 'GET',
    headers,
  })

  // E2E-003 FIX: Handle non-JSON responses gracefully to prevent 500 errors
  let data: unknown = null
  const contentType = response.headers.get('content-type')

  if (contentType?.includes('application/json')) {
    try {
      data = await response.json()
    } catch {
      // JSON parsing failed, return empty object with original status
      data = { message: response.statusText || 'Request failed' }
    }
  } else {
    // Non-JSON response, try to get text
    try {
      const text = await response.text()
      data = { message: text || response.statusText || 'Request failed' }
    } catch {
      data = { message: response.statusText || 'Request failed' }
    }
  }

  // Create response with proper headers
  const nextResponse = NextResponse.json(data, { status: response.status })

  // BUG-LOW-021 FIX: Add correlation ID to response for tracing
  nextResponse.headers.set('X-Correlation-ID', correlationId)

  // Forward ALL Set-Cookie headers from backend to frontend
  // IMPORTANT: response.headers.get() only returns the first header
  // We need to use getSetCookie() to get all Set-Cookie headers
  const setCookieHeaders = response.headers.getSetCookie?.() || []
  logger.debug('Auth proxy GET outgoing cookies', { count: setCookieHeaders.length, correlationId })
  if (setCookieHeaders.length > 0) {
    setCookieHeaders.forEach((cookie) => {
      logger.debug('Auth proxy GET setting cookie', { cookie, correlationId })
      nextResponse.headers.append('set-cookie', cookie)
    })
  }

  return nextResponse
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  const resolvedParams = await params
  const path = resolvedParams.path.join('/')
  const url = `${BACKEND_URL}/api/auth/${path}`

  // BUG-LOW-021 FIX: Add correlation ID for request tracing
  const correlationId = request.headers.get('x-correlation-id') ||
    `auth-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`

  // Get request body
  const body = await request.text()

  // Forward cookies and headers from the request
  const cookieHeader = request.headers.get('cookie')
  const csrfToken = request.headers.get('x-csrf-token')

  logger.debug('Auth proxy POST request', { path, correlationId })
  logger.debug('Auth proxy POST incoming cookies', { cookies: cookieHeader || 'none', correlationId })
  logger.debug('Auth proxy POST CSRF token', { csrfToken: csrfToken || 'none', correlationId })

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'X-Correlation-ID': correlationId
  }

  if (cookieHeader) {
    headers['Cookie'] = cookieHeader
  }

  if (csrfToken) {
    headers['X-CSRF-TOKEN'] = csrfToken
  }

  const response = await fetch(url, {
    method: 'POST',
    headers,
    body,
  })

  // E2E-003 FIX: Handle non-JSON responses gracefully to prevent 500 errors
  let data: unknown = null
  const contentType = response.headers.get('content-type')

  if (contentType?.includes('application/json')) {
    try {
      data = await response.json()
    } catch {
      // JSON parsing failed, return empty object with original status
      data = { message: response.statusText || 'Request failed' }
    }
  } else {
    // Non-JSON response, try to get text
    try {
      const text = await response.text()
      data = { message: text || response.statusText || 'Request failed' }
    } catch {
      data = { message: response.statusText || 'Request failed' }
    }
  }

  // Create response with proper headers
  const nextResponse = NextResponse.json(data, { status: response.status })

  // BUG-LOW-021 FIX: Add correlation ID to response for tracing
  nextResponse.headers.set('X-Correlation-ID', correlationId)

  // Forward ALL Set-Cookie headers from backend to frontend
  // IMPORTANT: response.headers.get() only returns the first header
  // We need to use getSetCookie() to get all Set-Cookie headers
  const setCookieHeaders = response.headers.getSetCookie?.() || []
  logger.debug('Auth proxy POST outgoing cookies', { count: setCookieHeaders.length, correlationId })
  if (setCookieHeaders.length > 0) {
    setCookieHeaders.forEach((cookie) => {
      logger.debug('Auth proxy POST setting cookie', { cookie, correlationId })
      nextResponse.headers.append('set-cookie', cookie)
    })
  }

  return nextResponse
}
