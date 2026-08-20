import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

// Define protected routes that require authentication
const protectedRoutes = [
  '/dashboard',
  '/profile/me',
  '/create-project',
  '/workspace',
  '/wallet',
  '/my-projects',
  '/subscription',
  '/reputation',
  '/reviews',
]

// Define public routes that authenticated users should not access
const authRoutes = [
  '/login',
  '/register',
]

// Define routes that don't require checks
const publicRoutes = [
  '/',
  '/forgot-password',
  '/reset-password',
  '/verify-email',
  '/resend-verification',
  '/projects/search',
  '/api',
]

/**
 * AUTH_COOKIE_NAME: The name of the ASP.NET Identity authentication cookie
 * This cookie is set by the backend and is httpOnly (cannot be read by JS)
 * The cookie is encrypted by ASP.NET Data Protection, so we can only check for existence
 * Actual validation is done server-side when API calls are made
 */
const AUTH_COOKIE_NAME = '.SkillLedger.Auth'

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl

  // E2E-009 FIX: Check for the correct ASP.NET Identity cookie name
  // The cookie is httpOnly and encrypted, so we can only verify it exists
  // Actual validation happens server-side when API calls are made
  const authCookie = request.cookies.get(AUTH_COOKIE_NAME)?.value

  // Cookie presence indicates a potentially valid session
  // The actual validation is done server-side on protected API calls
  const isAuthenticated = !!authCookie

  // Check if the route is protected
  const isProtectedRoute = protectedRoutes.some(route => pathname.startsWith(route))
  const isAuthRoute = authRoutes.some(route => pathname.startsWith(route))
  const isPublicRoute = publicRoutes.some(route => pathname.startsWith(route))

  // Protect routes that require authentication
  if (isProtectedRoute && !isAuthenticated) {
    const url = request.nextUrl.clone()
    url.pathname = '/login'
    // BUG-MW-001 FIX: Include query parameters in redirect URL
    const redirectPath = pathname + (request.nextUrl.search || '')
    url.searchParams.set('redirect', redirectPath)
    return NextResponse.redirect(url)
  }

  // BUG-FE-012 FIX: Don't redirect users away from auth pages even if they have a cookie
  // The cookie might be stale (e.g., after container restart which regenerates Data Protection keys)
  // Instead, let users access /login to get a fresh session
  // The AuthContext will handle the actual authentication state
  // This prevents infinite redirect loops when cookies are invalid but present

  // Allow all other requests to proceed
  return NextResponse.next()
}

// Configure which routes the middleware should run on
export const config = {
  matcher: [
    /*
     * Match all request paths except:
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - public files (public folder)
     */
    '/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)',
  ],
}
