'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useRouter } from 'next/navigation'
import { Mail, AlertTriangle, ArrowLeft } from 'lucide-react'
import Link from 'next/link'
import { Logo } from './Logo'
import { ThemeToggle } from './ThemeToggle'

const forgotPasswordSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
})

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>

interface ForgotPasswordProps {
  onSuccess?: (email: string) => void
}

export default function ForgotPassword({ onSuccess }: ForgotPasswordProps) {
  const router = useRouter()
  const [submitState, setSubmitState] = useState<'idle' | 'loading' | 'success' | 'error'>('idle')
  const [errorMessage, setErrorMessage] = useState('')
  const [successEmail, setSuccessEmail] = useState('')
  
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  })

  const email = watch('email')

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setSubmitState('loading')
    setErrorMessage('')

    try {
      // Get CSRF token first
      const csrfResponse = await fetch('/api/auth/csrf-token')
      // FE-HIGH-001 FIX: Validate CSRF token response before parsing
      if (!csrfResponse.ok) {
        throw new Error('Failed to get CSRF token')
      }
      const csrfData = await csrfResponse.json()
      if (!csrfData.token) {
        throw new Error('Invalid CSRF token response')
      }

      const response = await fetch('/api/auth/forgot-password', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfData.token,
        },
        body: JSON.stringify(data),
      })

      if (response.ok) {
        setSubmitState('success')
        setSuccessEmail(data.email)
        onSuccess?.(data.email)
      } else if (response.status === 429) {
        setSubmitState('error')
        setErrorMessage('Too many password reset requests. Please wait before trying again.')
      } else {
        const errorResult = await response.json()
        setSubmitState('error')
        setErrorMessage(errorResult.message || 'Failed to send password reset email.')
      }
    } catch (error) {
      logger.error('Forgot password error:', error)
      setSubmitState('error')
      setErrorMessage('An error occurred. Please try again.')
    }
  }

  const renderContent = () => {
    if (submitState === 'success') {
      return (
        <div className="text-center">
          <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-primary/10 mb-4">
            <svg className="h-6 w-6 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <h2 className="text-2xl font-semibold text-foreground mb-2">Check Your Email</h2>
          <p className="text-muted-foreground mb-4">
            If the email address is registered and verified, password reset instructions have been sent to:
          </p>
          <p className="font-medium text-foreground mb-6">{successEmail}</p>
          
          <div className="card-premium bg-muted p-6 space-md">
            <div className="flex items-center text-body text-foreground mb-4">
              <Mail className="w-5 h-5 mr-3" />
              <strong>What's next?</strong>
            </div>
            <ul className="text-caption text-muted-foreground space-y-2">
              <li>• Check your email inbox (and spam folder)</li>
              <li>• Click the password reset link in the email</li>
              <li>• The reset link expires in 1 hour</li>
              <li>• You can request a new reset if needed</li>
            </ul>
          </div>

          <div className="space-y-3">
            <button
              onClick={() => {
                setSubmitState('idle')
                setSuccessEmail('')
                setErrorMessage('')
              }}
              className="w-full flex justify-center py-2 px-4 border border-transparent rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
            >
              Send to Different Email
            </button>
            <button
              onClick={() => router.push('/login')}
              className="w-full flex justify-center py-2 px-4 border border-input rounded-full shadow-sm text-sm font-medium text-foreground bg-background hover:bg-muted focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
            >
              Back to Login
            </button>
          </div>
        </div>
      )
    }

    return (
      <div>
        <div className="text-center mb-6">
          <h2 className="text-2xl font-semibold text-foreground mb-2">Forgot Password?</h2>
          <p className="text-muted-foreground">
            Enter your email address and we'll send you instructions to reset your password.
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-foreground">
              Email Address
            </label>
            <input
              {...register('email')}
              type="email"
              id="email"
              data-testid="email-input"
              className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-input sm:text-sm bg-background text-foreground"
              placeholder="Enter your email address"
              disabled={submitState === 'loading' || isSubmitting}
            />
            {errors.email && (
              <p className="mt-1 text-sm text-destructive" data-testid="email-error">
                {errors.email.message}
              </p>
            )}
          </div>

          {submitState === 'error' && errorMessage && (
            <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="ml-3">
                  <p className="text-sm text-destructive">{errorMessage}</p>
                </div>
              </div>
            </div>
          )}

          <button
            type="submit"
            disabled={submitState === 'loading' || isSubmitting}
            data-testid="submit-button"
            className="w-full flex justify-center py-2 px-4 border border-transparent rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {submitState === 'loading' || isSubmitting ? (
              <div className="flex items-center">
                <div className="animate-spin -ml-1 mr-3 h-4 w-4 border-2 border-primary-foreground border-t-transparent rounded-full"></div>
                Sending Instructions...
              </div>
            ) : (
              'Send Reset Instructions'
            )}
          </button>
        </form>

        <div className="mt-6 text-center">
          <p className="text-sm text-muted-foreground">
            Remember your password?{' '}
            <button
              onClick={() => router.push('/login')}
              className="font-medium text-primary hover:text-primary/80 focus:outline-none focus:underline"
            >
              Sign in
            </button>
          </p>
          <p className="mt-2 text-sm text-muted-foreground">
            Don't have an account?{' '}
            <button
              onClick={() => router.push('/register')}
              className="font-medium text-primary hover:text-primary/80 focus:outline-none focus:underline"
            >
              Create one
            </button>
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
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

        <div className="bg-card rounded-lg shadow-md px-8 py-10 border border-border">
          <div className="text-center mb-6 flex flex-col items-center">
            <Logo size="medium" showText={true} />
            <p className="text-muted-foreground">Professional collaboration platform</p>
          </div>

          {renderContent()}
        </div>

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