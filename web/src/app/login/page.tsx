'use client'

import { logger } from '@/utils/logger';
import React, { useState, useEffect, Suspense } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Mail, Lock, AlertCircle, ArrowLeft } from 'lucide-react'
import Link from 'next/link'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Logo } from '@/components/Logo'
import { ThemeToggle } from '@/components/ThemeToggle'
import { Checkbox } from '@/components/ui/checkbox'  // BUG-047 FIX: Import Checkbox component

const loginSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean().optional(),
})

type LoginFormData = z.infer<typeof loginSchema>

// BUG-010 FIX: Validate returnUrl to prevent open redirect vulnerabilities
function validateReturnUrl(url: string | null): string | null {
  if (!url) return null

  try {
    // Only allow relative URLs (starting with /)
    if (!url.startsWith('/')) return null

    // Prevent protocol-relative URLs (//example.com)
    if (url.startsWith('//')) return null

    // Prevent URLs with protocol indicators
    if (url.includes(':')) return null

    // URL looks safe - return it
    return url
  } catch {
    return null
  }
}

function LoginPageContent() {
  const router = useRouter()
  // BUG-036 FIX: Use useSearchParams hook instead of window.location.search
  const searchParams = useSearchParams()
  const { login, isAuthenticated, isLoading: authLoading } = useAuth()
  const [error, setError] = useState<string | null>(null)

  // BUG-010 FIX: Get and validate returnUrl from query params
  const returnUrl = searchParams.get('returnUrl')

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      rememberMe: false,
    },
  })

  // Track if we initiated a login to prevent useEffect redirect conflict
  const [loginInitiated, setLoginInitiated] = useState(false)

  // BUG-017 FIX: Only redirect if user lands on login page while already authenticated
  // E2E-002 FIX: Don't redirect if login was just initiated (let handleLogin do the redirect)
  useEffect(() => {
    logger.debug('Login useEffect', { isAuthenticated, authLoading, isSubmitting, loginInitiated })
    // Only redirect if:
    // 1. User is authenticated
    // 2. Auth is not loading
    // 3. Not currently submitting
    // 4. Login was NOT just initiated (to prevent race condition with handleLogin redirect)
    if (isAuthenticated && !authLoading && !isSubmitting && !loginInitiated) {
      logger.debug('User already authenticated on page load, redirecting to dashboard', { page: 'login' })
      const redirectTo = validateReturnUrl(returnUrl) || '/dashboard'
      router.replace(redirectTo)
    }
  }, [isAuthenticated, authLoading, router, isSubmitting, loginInitiated, returnUrl])

  // Only disable form during active login attempt, not initial auth check
  // If there's no token and auth is loading, it's just checking localStorage, don't disable form
  const isFormDisabled = isSubmitting || authLoading

  const handleLogin = async (data: LoginFormData) => {
    setError(null)
    setLoginInitiated(true)  // E2E-002 FIX: Prevent useEffect from competing with this redirect

    try {
      const result = await login(data.email, data.password, data.rememberMe)

      if (result.success) {
        logger.info('Login successful, redirecting to dashboard', { page: 'login' })

        // BUG-010 FIX: Validate and use returnUrl if provided, otherwise default to dashboard
        const redirectTo = validateReturnUrl(returnUrl) || '/dashboard'

        // E2E-002 FIX: Use router.replace for immediate redirect without waiting
        // This prevents back button from returning to login page
        router.replace(redirectTo)
      } else {
        setLoginInitiated(false)  // Reset on failure so user can try again
        // BUG-AUTH-001 FIX: Clear password field on failed login for security
        setValue('password', '')
        setError(result.message || 'Login failed. Please check your credentials.')
      }
    } catch (err) {
      setLoginInitiated(false)  // Reset on error so user can try again
      // BUG-AUTH-001 FIX: Clear password field on error for security
      setValue('password', '')
      logger.error('Login error', err, { page: 'login' })
      setError('An unexpected error occurred. Please try again.')
    }
  }

  // BUG-UI-010 FIX: Show loading indicator during initial auth check
  if (authLoading && !isSubmitting) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-primary border-t-transparent rounded-full animate-spin"></div>
          <p className="text-sm text-muted-foreground">Checking authentication...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background px-6 py-12">
      <div className="container-centered space-y-8">
        {/* Navigation Header */}
        <div className="flex items-center justify-between">
          <Link
            href="/"
            className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Home
          </Link>
          <ThemeToggle />
        </div>

        <Card className="p-8">
          <CardHeader className="text-center space-y-4">
            <div>
              <Logo size="medium" showText={true} />
              <p className="text-body text-muted-foreground">Professional collaboration platform</p>
            </div>
            
            <div>
              <h2 className="text-heading text-foreground">Sign In</h2>
              <p className="text-body text-muted-foreground">Access your professional network</p>
            </div>
          </CardHeader>

          <CardContent className="space-y-6">
            {error && (
              <div role="alert" aria-live="assertive" className="bg-destructive/10 border border-destructive/20 rounded-xl p-4">
                <div className="flex items-start space-x-3">
                  <AlertCircle className="h-5 w-5 text-destructive flex-shrink-0 mt-0.5" aria-hidden="true" />
                  <p className="text-sm text-destructive">{error}</p>
                </div>
              </div>
            )}

            <form
              onSubmit={handleSubmit(handleLogin)}
              className="space-y-6"
              aria-label="Login form"
              noValidate
            >
              <div className="space-y-2">
                <Label htmlFor="email" required>
                  Email Address
                </Label>
                <Input
                  {...register('email')}
                  type="email"
                  id="email"
                  placeholder="Enter your email"
                  autoComplete="email"
                  disabled={isFormDisabled}
                  error={!!errors.email}
                  helperText={errors.email?.message}
                  startIcon={<Mail className="w-4 h-4" />}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="password" required>
                  Password
                </Label>
                <Input
                  {...register('password')}
                  type="password"
                  id="password"
                  placeholder="Password"
                  autoComplete="current-password"
                  disabled={isFormDisabled}
                  error={!!errors.password}
                  helperText={errors.password?.message}
                  startIcon={<Lock className="w-4 h-4" />}
                />
              </div>

              {/* BUG-007 FIX: Stack vertically on mobile to prevent wrapping */}
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                {/* BUG-047 FIX: Use Checkbox component instead of native input */}
                <Checkbox
                  {...register('rememberMe')}
                  id="remember-me"
                  label="Remember me"
                  disabled={isFormDisabled}
                />

                <Button
                  type="button"
                  variant="link"
                  onClick={() => router.push('/forgot-password')}
                  className="p-0 h-auto text-sm self-start sm:self-auto"
                >
                  Forgot password?
                </Button>
              </div>

              <Button
                type="submit"
                disabled={isFormDisabled}
                loading={isSubmitting}
                loadingText="Signing In..."
                className="w-full"
              >
                Sign In
              </Button>
            </form>

            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-border" />
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="px-2 bg-background text-muted-foreground">Don't have an account?</span>
              </div>
            </div>

            <Button
              onClick={() => router.push('/register')}
              variant="outline"
              className="w-full"
            >
              Create New Account
            </Button>
          </CardContent>
        </Card>
        
        <div className="text-center space-y-2">
          <p className="text-xs text-muted-foreground">
            Need help?{' '}
            <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:text-primary/80 transition-colors">
              Contact support
            </a>
          </p>
        </div>
      </div>
    </div>
  )
}

// Loading fallback component
function LoginPageLoading() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-4">
        <div className="w-12 h-12 border-4 border-primary border-t-transparent rounded-full animate-spin"></div>
        <p className="text-sm text-muted-foreground">Loading...</p>
      </div>
    </div>
  )
}

// Next.js 14 requires useSearchParams to be wrapped in Suspense
export default function LoginPage() {
  return (
    <Suspense fallback={<LoginPageLoading />}>
      <LoginPageContent />
    </Suspense>
  )
}