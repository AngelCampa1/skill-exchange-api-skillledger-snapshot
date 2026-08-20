'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { Mail, AlertTriangle, CheckCircle, ArrowLeft, Loader2, RefreshCw } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { ThemeToggle } from '@/components/ThemeToggle'
import LogoutButton from '@/components/LogoutButton'
import { logger } from '@/utils/logger'

export default function VerifyEmail() {
  const { user, isAuthenticated, isInitialized, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const [isResending, setIsResending] = useState(false)
  const [resendSuccess, setResendSuccess] = useState(false)
  const [resendError, setResendError] = useState<string | null>(null)

  // Redirect to login if not authenticated
  if (isInitialized && !authLoading && !isAuthenticated) {
    router.push('/login')
    return null
  }

  // Redirect to dashboard if already verified
  if (isInitialized && !authLoading && user?.emailVerified) {
    router.push('/dashboard')
    return null
  }

  const handleResendVerification = async () => {
    try {
      setIsResending(true)
      setResendError(null)
      setResendSuccess(false)

      // Get CSRF token
      const csrfResponse = await fetch('/api/auth/csrf-token')
      const csrfData = await csrfResponse.json()

      const response = await fetch('/api/auth/resend-verification', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfData.token,
        },
        credentials: 'include',
      })

      if (response.ok) {
        setResendSuccess(true)
      } else {
        const data = await response.json()
        setResendError(data.message || 'Failed to resend verification email. Please try again later.')
      }
    } catch (error) {
      logger.error('Resend verification error:', error)
      setResendError('An unexpected error occurred. Please try again later.')
    } finally {
      setIsResending(false)
    }
  }

  // Show loading state
  if (!isInitialized || authLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Navigation Header */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <Link
              href="/"
              className="flex items-center text-foreground hover:text-primary transition-colors duration-300"
            >
              <ArrowLeft className="w-5 h-5 mr-2" />
              <span className="font-medium">SkillLedger</span>
            </Link>

            <div className="flex items-center space-x-4">
              <ThemeToggle />
              <LogoutButton showAllDevicesOption={false} />
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <div className="flex flex-col items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full">
          <div className="bg-card border border-border rounded-xl shadow-sm p-8">
            {/* Icon */}
            <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-warning/10 mb-6">
              <Mail className="h-8 w-8 text-warning" />
            </div>

            {/* Title */}
            <h1 className="text-2xl font-bold text-center text-foreground mb-2">
              Email Verification Required
            </h1>

            {/* Description */}
            <p className="text-center text-muted-foreground mb-6">
              Please verify your email address to access all features of SkillLedger.
            </p>

            {/* User Email Display */}
            {user?.email && (
              <div className="bg-muted rounded-lg p-4 mb-6">
                <p className="text-sm text-muted-foreground mb-1">Verification email sent to:</p>
                <p className="font-medium text-foreground break-all">{user.email}</p>
              </div>
            )}

            {/* Instructions */}
            <div className="bg-info/10 border border-info/20 rounded-lg p-4 mb-6">
              <h3 className="font-medium text-foreground flex items-center mb-2">
                <AlertTriangle className="w-4 h-4 mr-2 text-info" />
                What to do:
              </h3>
              <ol className="text-sm text-muted-foreground space-y-2 list-decimal list-inside">
                <li>Check your email inbox (and spam/junk folder)</li>
                <li>Click the verification link in the email</li>
                <li>Return to SkillLedger to continue</li>
              </ol>
            </div>

            {/* Success Message */}
            {resendSuccess && (
              <div className="bg-success/10 border border-success/20 rounded-lg p-4 mb-6">
                <div className="flex items-center">
                  <CheckCircle className="w-5 h-5 text-success mr-2" />
                  <p className="text-sm text-success">
                    Verification email sent! Please check your inbox.
                  </p>
                </div>
              </div>
            )}

            {/* Error Message */}
            {resendError && (
              <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4 mb-6">
                <div className="flex items-start">
                  <AlertTriangle className="w-5 h-5 text-destructive mr-2 flex-shrink-0 mt-0.5" />
                  <p className="text-sm text-destructive">{resendError}</p>
                </div>
              </div>
            )}

            {/* Action Buttons */}
            <div className="space-y-3">
              <button
                onClick={handleResendVerification}
                disabled={isResending}
                className="btn-primary w-full flex items-center justify-center"
              >
                {isResending ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Sending...
                  </>
                ) : (
                  <>
                    <RefreshCw className="w-4 h-4 mr-2" />
                    Resend Verification Email
                  </>
                )}
              </button>

              <button
                onClick={() => window.location.reload()}
                className="btn-secondary w-full"
              >
                I've verified my email - Refresh
              </button>
            </div>

            {/* Additional Help */}
            <div className="mt-6 pt-6 border-t border-border">
              <p className="text-sm text-muted-foreground text-center">
                Having trouble? Contact our{' '}
                <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:underline">
                  support team
                </a>
                {' '}for assistance.
              </p>
            </div>
          </div>

          {/* Expiration Warning */}
          <div className="mt-4 bg-warning/10 border border-warning/20 rounded-lg p-4">
            <div className="flex items-start">
              <AlertTriangle className="w-5 h-5 text-warning mr-2 flex-shrink-0 mt-0.5" />
              <p className="text-sm text-warning">
                <strong>Note:</strong> Verification links expire after 24 hours.
                If your link has expired, click "Resend Verification Email" above.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
