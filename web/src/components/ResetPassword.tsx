'use client'

import { logger } from '@/utils/logger';
import { trackEvent } from '@/utils/analytics';

import React, { useState, useEffect, startTransition } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useRouter, useSearchParams } from 'next/navigation'
import { Eye, EyeOff, CheckCircle, Shield, Lock, AlertCircle, X, Clock } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Logo } from './Logo'

const resetPasswordSchema = z.object({
  newPassword: z
    .string()
    .min(12, 'Password must be at least 12 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one number')
    .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character'),
  confirmPassword: z.string(),
}).refine((data) => data.newPassword === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
})

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>

interface ResetPasswordProps {
  onSuccess?: () => void
}

export default function ResetPassword({ onSuccess }: ResetPasswordProps) {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [tokenValidationState, setTokenValidationState] = useState<'loading' | 'valid' | 'invalid' | 'expired'>('loading')
  const [submitState, setSubmitState] = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
  const [errorMessage, setErrorMessage] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [passwordStrength, setPasswordStrength] = useState(0)
  
  const token = searchParams.get('token')
  
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    mode: 'onChange',
    reValidateMode: 'onChange',
  })

  const newPassword = watch('newPassword')

  // Calculate password strength (0-100)
  const calculatePasswordStrength = (pwd: string): number => {
    if (!pwd) return 0
    
    let score = 0
    if (pwd.length >= 12) score += 25
    if (pwd.length >= 16) score += 10
    if (/[A-Z]/.test(pwd)) score += 15
    if (/[a-z]/.test(pwd)) score += 15
    if (/[0-9]/.test(pwd)) score += 15
    if (/[^A-Za-z0-9]/.test(pwd)) score += 20
    
    return Math.min(score, 100)
  }

  // Update password strength when password changes
  useEffect(() => {
    if (newPassword) {
      setPasswordStrength(calculatePasswordStrength(newPassword))
    } else {
      setPasswordStrength(0)
    }
  }, [newPassword])

  // Validate reset token on component mount
  useEffect(() => {
    const validateToken = async () => {
      if (!token) {
        setTokenValidationState('invalid')
        return
      }

      try {
        const response = await fetch(`/api/auth/validate-reset-token?token=${encodeURIComponent(token)}`)
        const result = await response.json()

        if (response.ok && result.valid) {
          setTokenValidationState('valid')
        } else {
          setTokenValidationState('expired')
        }
      } catch (error) {
        logger.error('Token validation error:', error)
        setTokenValidationState('invalid')
      }
    }

    validateToken()
  }, [token])

  const onSubmit = async (data: ResetPasswordFormData) => {
    if (!token) {
      setErrorMessage('Invalid reset token.')
      return
    }

    startTransition(() => {
      setSubmitState('loading')
      setErrorMessage('')
    })

    try {
      // Get CSRF token first
      const csrfResponse = await fetch('/api/auth/csrf-token')
      const csrfData = await csrfResponse.json()

      const response = await fetch('/api/auth/reset-password', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfData.token,
        },
        body: JSON.stringify({
          token: token,
          newPassword: data.newPassword,
          confirmPassword: data.confirmPassword,
        }),
      })

      const result = await response.json()

      if (response.ok && result.success) {
        startTransition(() => {
          setSubmitState('success')
        })

        // Track successful password reset
        trackEvent({
          name: 'password_reset',
          category: 'authentication',
          priority: 'critical',
          properties: {
            method: 'email',
            success: true,
          },
        })

        // Call callback outside transition to avoid act warnings
        onSuccess?.()
      } else {
        startTransition(() => {
          setSubmitState('error')
          if (result.tokenExpired) {
            setTokenValidationState('expired')
            setErrorMessage('Your reset token has expired. Please request a new password reset.')
          } else {
            setErrorMessage(result.message || 'Failed to reset password.')
          }
        })
      }
    } catch (error) {
      // Only log in non-test environments
      if (process.env.NODE_ENV !== 'test') {
        logger.error('Reset password error:', error)
      }
      setSubmitState('error')
      setErrorMessage('An error occurred. Please try again.')
    }
  }

  const getStrengthColor = (strength: number): string => {
    if (strength < 30) return 'bg-destructive'
    if (strength < 60) return 'bg-warning'
    if (strength < 80) return 'bg-primary'
    return 'bg-success'
  }

  const getStrengthText = (strength: number): string => {
    if (strength < 30) return 'Weak'
    if (strength < 60) return 'Fair'
    if (strength < 80) return 'Good'
    return 'Strong'
  }

  const renderContent = () => {
    if (tokenValidationState === 'loading') {
      return (
        <div className="text-center space-y-4">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <div>
            <h2 className="text-heading text-foreground mb-2">Validating Reset Token</h2>
            <p className="text-body text-muted-foreground">Please wait while we verify your reset link...</p>
          </div>
        </div>
      )
    }

    if (tokenValidationState === 'invalid') {
      return (
        <div className="text-center space-y-6">
          <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-destructive/10">
            <X className="h-6 w-6 text-destructive" />
          </div>
          <div>
            <h2 className="text-heading text-foreground mb-2">Invalid Reset Link</h2>
            <p className="text-body text-muted-foreground">
              The password reset link is invalid or malformed. Please check the link in your email or request a new password reset.
            </p>
          </div>
          
          <div className="space-y-3">
            <Button
              onClick={() => router.push('/forgot-password')}
              className="w-full"
            >
              Request New Reset
            </Button>
            <Button
              onClick={() => router.push('/login')}
              variant="outline"
              className="w-full"
            >
              Back to Login
            </Button>
          </div>
        </div>
      )
    }

    if (tokenValidationState === 'expired') {
      return (
        <div className="text-center space-y-6">
          <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-warning/10">
            <Clock className="h-6 w-6 text-warning" />
          </div>
          <div>
            <h2 className="text-heading text-foreground mb-2">Reset Link Expired</h2>
            <p className="text-body text-muted-foreground">
              Your password reset link has expired or has already been used. Password reset links are valid for 1 hour for security reasons.
            </p>
          </div>
          
          <div className="space-y-3">
            <Button
              onClick={() => router.push('/forgot-password')}
              className="w-full"
            >
              Request New Reset
            </Button>
            <Button
              onClick={() => router.push('/login')}
              variant="outline"
              className="w-full"
            >
              Back to Login
            </Button>
          </div>
        </div>
      )
    }

    if (submitState === 'success') {
      return (
        <div className="text-center space-y-6">
          <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-success/10">
            <CheckCircle className="h-6 w-6 text-success" />
          </div>
          <div>
            <h2 className="text-heading text-foreground mb-2">Password Reset Successful!</h2>
            <p className="text-body text-muted-foreground">
              Your password has been successfully reset. You can now log in with your new password.
            </p>
          </div>

          <div className="bg-success/10 border border-success/20 rounded-xl p-4">
            <div className="flex items-center text-success text-sm">
              <Shield className="w-4 h-4 mr-2 flex-shrink-0" />
              <span>For security, you'll need to log in again on all devices with your new password.</span>
            </div>
          </div>

          <Button
            onClick={() => router.push('/login')}
            className="w-full"
            endIcon={
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            }
          >
            Go to Login
          </Button>
        </div>
      )
    }

    return (
      <div className="space-y-6">
        <div className="text-center">
          <h2 className="text-heading text-foreground mb-2">Reset Your Password</h2>
          <p className="text-body text-muted-foreground">
            Choose a strong new password for your account.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="newPassword" required>
              New Password
            </Label>
            <Input
              {...register('newPassword')}
              type={showPassword ? 'text' : 'password'}
              id="newPassword"
              data-testid="new-password-input"
              placeholder="Enter your new password"
              disabled={submitState === 'loading' || isSubmitting}
              error={!!errors.newPassword}
              helperText={errors.newPassword?.message}
              startIcon={<Lock className="w-4 h-4" />}
              endIcon={
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="text-muted-foreground hover:text-foreground transition-colors"
                  data-testid="toggle-password"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </button>
              }
            />
            
            {newPassword && (
              <div className="mt-3">
                <Progress
                  value={passwordStrength}
                  showLabel
                  data-testid="strength-bar"
                />
              </div>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword" required>
              Confirm New Password
            </Label>
            <Input
              {...register('confirmPassword')}
              type={showConfirmPassword ? 'text' : 'password'}
              id="confirmPassword"
              data-testid="confirm-password-input"
              placeholder="Confirm your new password"
              disabled={submitState === 'loading' || isSubmitting}
              error={!!errors.confirmPassword}
              helperText={errors.confirmPassword?.message}
              startIcon={<Lock className="w-4 h-4" />}
              endIcon={
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  className="text-muted-foreground hover:text-foreground transition-colors"
                  data-testid="toggle-confirm-password"
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                >
                  {showConfirmPassword ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </button>
              }
            />
          </div>

          {submitState === 'error' && errorMessage && (
            <div className="bg-destructive/10 border border-destructive/20 rounded-xl p-4">
              <div className="flex items-start space-x-3">
                <AlertCircle className="h-5 w-5 text-destructive flex-shrink-0 mt-0.5" />
                <p className="text-sm text-destructive">{errorMessage}</p>
              </div>
            </div>
          )}

          <Button
            type="submit"
            disabled={submitState === 'loading' || isSubmitting}
            loading={submitState === 'loading' || isSubmitting}
            loadingText="Resetting Password..."
            data-testid="submit-button"
            className="w-full"
          >
            Reset Password
          </Button>
        </form>

        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            Remember your password?{' '}
            <Button
              onClick={() => router.push('/login')}
              variant="link"
              className="p-0 h-auto text-sm"
            >
              Sign in instead
            </Button>
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <Card className="p-8">
          <CardHeader className="text-center">
            <div className="flex flex-col items-center space-md">
              <Logo size="medium" showText={true} />
              <p className="text-body text-muted-foreground">Professional collaboration platform</p>
            </div>
          </CardHeader>
          
          <CardContent>
            {renderContent()}
          </CardContent>
        </Card>
        
        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            Need help? Contact us at{' '}
            <a href="mailto:angel.campa@skillledger.app" className="text-primary hover:text-primary/80 transition-colors">
              angel.campa@skillledger.app
            </a>
          </p>
        </div>
      </div>
    </div>
  )
}