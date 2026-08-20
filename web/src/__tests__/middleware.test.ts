/**
 * Tests for middleware.ts
 *
 * Comprehensive test suite for authentication middleware
 * Coverage target: 95%+ (93 lines)
 *
 * Known issues to test:
 * - BUG-FE-012: Stale cookies after container restart (fixed - no redirect from auth pages)
 * - Potential infinite redirect loops
 */

import { NextRequest, NextResponse } from 'next/server'
import { middleware } from '../middleware'

// Mock NextResponse methods
jest.mock('next/server', () => {
  const actual = jest.requireActual('next/server')
  return {
    ...actual,
    NextResponse: {
      next: jest.fn(() => ({ type: 'next' })),
      redirect: jest.fn((url) => ({ type: 'redirect', url: url.toString() })),
    },
  }
})

describe('middleware', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  const createMockRequest = (pathname: string, hasCookie: boolean = false): NextRequest => {
    const url = new URL(pathname, 'http://localhost:3000')

    // Create a mock cookies object
    const cookies = new Map()
    if (hasCookie) {
      cookies.set('.SkillLedger.Auth', { name: '.SkillLedger.Auth', value: 'mock-auth-token-value' })
    }

    // Create mock request object
    const mockRequest = {
      nextUrl: {
        pathname: url.pathname,
        search: url.search,
        searchParams: url.searchParams,
        clone: () => {
          // Return a real URL object that can be mutated
          const clonedUrl = new URL(url.href)
          return clonedUrl
        },
        toString: () => url.toString(),
      },
      cookies: {
        get: (name: string) => cookies.get(name),
        set: (name: string, value: string) => {
          cookies.set(name, { name, value })
        },
      },
    } as unknown as NextRequest

    return mockRequest
  }

  describe('Protected Routes', () => {
    const protectedRoutes = [
      '/dashboard',
      '/profile/me',
      '/create-project',
      '/workspace',
      '/wallet',
      '/my-projects',
      '/subscription',
    ]

    describe('WITHOUT authentication cookie', () => {
      it.each(protectedRoutes)('redirects %s to /login', async (route) => {
        const request = createMockRequest(route, false)

        const response = await middleware(request)

        expect(NextResponse.redirect).toHaveBeenCalledWith(
          expect.objectContaining({
            pathname: '/login',
            search: expect.stringContaining(`redirect=${encodeURIComponent(route)}`),
          })
        )
        expect(response).toEqual({ type: 'redirect', url: expect.stringContaining('/login') })
      })

      it('redirects /dashboard/analytics (sub-path) to /login', async () => {
        const request = createMockRequest('/dashboard/analytics', false)

        const response = await middleware(request)

        expect(NextResponse.redirect).toHaveBeenCalled()
        expect(response).toEqual({ type: 'redirect', url: expect.stringContaining('/login') })
      })

      it('sets redirect query parameter with original pathname', async () => {
        const request = createMockRequest('/my-projects', false)

        await middleware(request)

        expect(NextResponse.redirect).toHaveBeenCalledWith(
          expect.objectContaining({
            search: expect.stringContaining('redirect=%2Fmy-projects'),
          })
        )
      })

      it('redirects nested protected route /workspace/project-123', async () => {
        const request = createMockRequest('/workspace/project-123', false)

        const response = await middleware(request)

        expect(NextResponse.redirect).toHaveBeenCalled()
        expect(response).toEqual({ type: 'redirect', url: expect.stringContaining('/login') })
      })
    })

    describe('WITH authentication cookie', () => {
      it.each(protectedRoutes)('allows access to %s', async (route) => {
        const request = createMockRequest(route, true)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
        expect(response).toEqual({ type: 'next' })
      })

      it('allows access to /dashboard/settings (sub-path)', async () => {
        const request = createMockRequest('/dashboard/settings', true)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
      })

      it('allows access to /workspace/123/details (nested path)', async () => {
        const request = createMockRequest('/workspace/123/details', true)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
      })
    })
  })

  describe('Auth Routes (/login, /register)', () => {
    const authRoutes = ['/login', '/register']

    describe('WITHOUT authentication cookie', () => {
      it.each(authRoutes)('allows access to %s', async (route) => {
        const request = createMockRequest(route, false)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
        expect(response).toEqual({ type: 'next' })
      })
    })

    describe('WITH authentication cookie (BUG-FE-012 fix)', () => {
      it.each(authRoutes)('allows access to %s (prevents infinite redirect loop)', async (route) => {
        const request = createMockRequest(route, true)

        const response = await middleware(request)

        // BUG-FE-012 FIX: Should NOT redirect away from auth pages
        // This prevents infinite loops when cookie is present but stale
        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
        expect(response).toEqual({ type: 'next' })
      })

      it('allows /login with stale cookie (prevents infinite redirect)', async () => {
        // Simulates scenario: User has cookie from restarted container (invalid Data Protection keys)
        const request = createMockRequest('/login', true)

        const response = await middleware(request)

        // Should allow through to /login to get fresh session
        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
      })
    })
  })

  describe('Public Routes', () => {
    const publicRoutes = [
      '/',
      '/forgot-password',
      '/reset-password',
      '/verify-email',
      '/resend-verification',
      '/projects/search',
    ]

    describe('WITHOUT authentication cookie', () => {
      it.each(publicRoutes)('allows access to %s', async (route) => {
        const request = createMockRequest(route, false)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
        expect(response).toEqual({ type: 'next' })
      })

      it('allows access to /api/health (sub-path of /api)', async () => {
        const request = createMockRequest('/api/health', false)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
      })
    })

    describe('WITH authentication cookie', () => {
      it.each(publicRoutes)('allows access to %s', async (route) => {
        const request = createMockRequest(route, true)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
      })

      it('allows authenticated user to access home page /', async () => {
        const request = createMockRequest('/', true)

        const response = await middleware(request)

        expect(NextResponse.next).toHaveBeenCalled()
        expect(NextResponse.redirect).not.toHaveBeenCalled()
      })
    })
  })

  describe('Edge Cases', () => {
    it('handles route not in any category (falls through to public)', async () => {
      const request = createMockRequest('/some-random-page', false)

      const response = await middleware(request)

      expect(NextResponse.next).toHaveBeenCalled()
      expect(NextResponse.redirect).not.toHaveBeenCalled()
    })

    it('handles empty cookie value', async () => {
      const request = createMockRequest('/dashboard', false)
      // Cookie exists but with empty value
      request.cookies.set('.SkillLedger.Auth', '')

      const response = await middleware(request)

      // Empty string is falsy, should redirect
      expect(NextResponse.redirect).toHaveBeenCalled()
    })

    it('handles missing cookie entirely', async () => {
      const request = createMockRequest('/dashboard', false)

      const response = await middleware(request)

      expect(NextResponse.redirect).toHaveBeenCalledWith(
        expect.objectContaining({
          pathname: '/login',
        })
      )
    })

    it('preserves query parameters in redirect URL', async () => {
      const request = createMockRequest('/dashboard?tab=settings', false)

      await middleware(request)

      expect(NextResponse.redirect).toHaveBeenCalledWith(
        expect.objectContaining({
          search: expect.stringContaining('redirect=%2Fdashboard%3Ftab%3Dsettings'),
        })
      )
    })

    it('handles malformed pathname gracefully', async () => {
      const request = createMockRequest('//dashboard//extra//slashes', false)

      const response = await middleware(request)

      // Should still work (might not match routes, but should not crash)
      expect(response).toBeDefined()
    })

    it('treats routes starting with protected paths as protected', async () => {
      const request = createMockRequest('/dashboard-settings', false)

      const response = await middleware(request)

      // /dashboard-settings starts with /dashboard, so should be protected
      expect(NextResponse.redirect).toHaveBeenCalled()
    })

    it('allows /projects but protects /my-projects', async () => {
      const requestPublic = createMockRequest('/projects/123', false)
      const requestProtected = createMockRequest('/my-projects', false)

      const publicResponse = await middleware(requestPublic)
      const protectedResponse = await middleware(requestProtected)

      // /projects is not protected (falls through to public)
      expect(publicResponse).toEqual({ type: 'next' })

      // /my-projects is protected
      expect(protectedResponse).toEqual({
        type: 'redirect',
        url: expect.stringContaining('/login')
      })
    })
  })

  describe('Cookie Security', () => {
    it('only checks for cookie existence (httpOnly cookie)', async () => {
      const request = createMockRequest('/dashboard', true)

      const response = await middleware(request)

      // Middleware should only check existence, not validate content
      // (Actual validation done server-side)
      expect(NextResponse.next).toHaveBeenCalled()
    })

    it('treats any non-empty cookie value as authenticated', async () => {
      const request = createMockRequest('/dashboard', false)
      request.cookies.set('.SkillLedger.Auth', 'invalid-token-format-but-non-empty')

      const response = await middleware(request)

      // Middleware trusts cookie existence, server validates later
      expect(NextResponse.next).toHaveBeenCalled()
      expect(NextResponse.redirect).not.toHaveBeenCalled()
    })
  })

  describe('Route Matching Logic', () => {
    it('uses startsWith for route matching (allows sub-paths)', async () => {
      const request = createMockRequest('/dashboard/analytics/reports', true)

      const response = await middleware(request)

      expect(NextResponse.next).toHaveBeenCalled()
    })

    it('matches /profile/me but not /profile-settings', async () => {
      const requestMatches = createMockRequest('/profile/me', false)
      const requestDoesNotMatch = createMockRequest('/profile-settings', false)

      const matchResponse = await middleware(requestMatches)
      const noMatchResponse = await middleware(requestDoesNotMatch)

      expect(matchResponse).toEqual({ type: 'redirect', url: expect.stringContaining('/login') })
      expect(noMatchResponse).toEqual({ type: 'next' })
    })

    it('API routes are always public (/api/*)', async () => {
      const request = createMockRequest('/api/projects', false)

      const response = await middleware(request)

      expect(NextResponse.next).toHaveBeenCalled()
      expect(NextResponse.redirect).not.toHaveBeenCalled()
    })
  })
})
