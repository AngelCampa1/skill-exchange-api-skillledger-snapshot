'use client'

import { logger } from '@/utils/logger';

import React, { useState, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { Mail, ArrowLeft, CheckCircle } from 'lucide-react'
import Link from 'next/link'
import RegistrationForm from '../../components/RegistrationForm'
import FormRecoveryBanner from '@/components/FormRecoveryBanner'
import { Logo } from '@/components/Logo'
import { ThemeToggle } from '@/components/ThemeToggle'
import { useAuth, User } from '@/contexts/AuthContext'
import { useFormPersistence } from '@/hooks/useFormPersistence'
import { trackEvent } from '@/utils/analytics'

interface RegistrationData {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  acceptedTerms: boolean
}

interface RegistrationResponse {
  success: boolean
  message: string
  userId?: string
  accessToken?: string
  refreshToken?: string
  expiresIn?: number
  expiresAt?: string
  user?: {
    id: string
    email: string
    userName: string
    emailVerified: boolean
    status: string
    roles: string[]
    permissions: string[]
  }
}

export default function RegisterPage() {
  const router = useRouter()
  // BUG-FE-020 FIX: Get updateUser from AuthContext to sync user state before redirect
  const { updateUser } = useAuth()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const [formKey, setFormKey] = useState(0)

  const { persistedValues, updateField, clearPersistedData, hasPersistedData } =
    useFormPersistence('registration', ['firstName', 'lastName', 'email'])

  const handleFieldChange = useCallback(
    (name: string, value: string) => {
      updateField(name, value)
    },
    [updateField]
  )

  const handleStartFresh = useCallback(() => {
    clearPersistedData()
    setFormKey((k) => k + 1)
    trackEvent({
      name: 'form_started',
      category: 'forms',
      priority: 'medium',
      properties: { form_name: 'registration', action: 'start_fresh' },
    })
  }, [clearPersistedData])

  const handleContinue = useCallback(() => {
    trackEvent({
      name: 'form_started',
      category: 'forms',
      priority: 'medium',
      properties: { form_name: 'registration', action: 'continue_saved' },
    })
  }, [])

  const handleRegistration = async (data: RegistrationData) => {
    setIsLoading(true)
    setError(null)
    
    try {
      // BUG-001 FIX: Fetch CSRF token before registration
      // BUG-FE-020 FIX: Include credentials to properly handle CSRF cookies
      const csrfResponse = await fetch('/api/auth/csrf-token', {
        method: 'GET',
        credentials: 'include',
      })
      
      if (!csrfResponse.ok) {
        throw new Error('Failed to fetch CSRF token')
      }
      
      const csrfData = await csrfResponse.json()
      const csrfToken = csrfData.token
      
      // BUG-FE-020 FIX: Add credentials: 'include' to accept auth cookies from backend
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include', // Required to accept httpOnly cookies from backend
        body: JSON.stringify({
          email: data.email,
          password: data.password,
          confirmPassword: data.confirmPassword,
          firstName: data.firstName,
          lastName: data.lastName,
          acceptedTerms: data.acceptedTerms,
        }),
      })

      const result: RegistrationResponse = await response.json()

      if (response.ok && result.success) {
        // Clear persisted form data on success
        clearPersistedData()
        // Show success message
        setSuccess(true)

        // Wait 2 seconds before redirecting to let user see the success message
        setTimeout(() => {
          // Check if user is immediately logged in (has access token)
          if (result.user) {
            // BUG-FE-020 FIX: Update AuthContext with user data BEFORE redirecting
            // This prevents the race condition where dashboard sees isAuthenticated=false
            // and triggers a logout, clearing the cookie set by registration
            const userForContext: User = {
              id: result.user.id,
              email: result.user.email,
              userName: result.user.userName,
              firstName: data.firstName,
              lastName: data.lastName,
              emailVerified: result.user.emailVerified,
              taxCompliant: false, // Default for new users
              status: result.user.status,
              roles: result.user.roles,
              permissions: result.user.permissions,
            }
            updateUser(userForContext)

            // Now redirect to plan selection — AuthContext will have user data
            router.push('/subscription/choose-plan')
          } else {
            // Fallback: Redirect to login page
            router.push('/login')
          }
        }, 2000)
      } else {
        setError(result.message || 'Registration failed. Please try again.')
      }
    } catch (err) {
      logger.error('Registration error:', err)
      setError('An unexpected error occurred. Please try again.')
    } finally {
      setIsLoading(false)
    }
  }

  
  return (
    <div className="min-h-screen flex items-center justify-center bg-background px-6 py-12">
      <div className="container-centered space-xl">
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

        <div className="card-premium p-8 space-xl">
          <div className="text-center space-md flex flex-col items-center">
            <Logo size="medium" showText={true} />
            <p className="text-body text-muted-foreground">Create your account to start your 30-day free trial.</p>
          </div>

          <div className="text-center space-md">
            <h2 className="text-heading text-foreground">Create Your Account</h2>
            <p className="text-body text-muted-foreground">Join the professional collaboration network</p>
          </div>

          <div className="space-y-2 mb-6">
            {['30-day free trial', 'Credit card required to start', 'Cancel anytime'].map((benefit) => (
              <div key={benefit} className="flex items-center gap-2 text-sm text-muted-foreground">
                <CheckCircle className="w-4 h-4 text-green-500 shrink-0" />
                <span>{benefit}</span>
              </div>
            ))}
          </div>

          {error && (
            <div className="bg-destructive/10 border border-destructive/20 rounded-xl p-4 mb-6">
              <div className="flex items-start space-x-3">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-destructive" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                  </svg>
                </div>
                <p className="text-body text-destructive">{error}</p>
              </div>
            </div>
          )}

          {success && (
            <div className="bg-success/10 border border-success/20 rounded-xl p-4 mb-6">
              <div className="flex items-start space-x-3">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div>
                  <p className="text-body text-success font-medium">Registration successful!</p>
                  <p className="text-caption text-success/80 mt-1">Redirecting you now...</p>
                </div>
              </div>
            </div>
          )}

          {hasPersistedData && !success && (
            <FormRecoveryBanner
              onContinue={handleContinue}
              onStartFresh={handleStartFresh}
            />
          )}

          <RegistrationForm
            key={formKey}
            onSubmit={handleRegistration}
            isLoading={isLoading || success}
            defaultValues={persistedValues}
            onFieldChange={handleFieldChange}
          />

          <div className="space-md">
            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-border" />
              </div>
              <div className="relative flex justify-center">
                <span className="px-4 bg-card text-caption text-muted-foreground">Already have an account?</span>
              </div>
            </div>

            <div className="mt-6">
              <button
                onClick={() => router.push('/login')}
                className="btn-secondary w-full"
              >
                Sign In Instead
              </button>
            </div>
          </div>
        </div>
        
        <div className="text-center space-y-2">
          <p className="text-xs text-muted-foreground">
            By creating an account, you agree to our{' '}
            <a href="/terms" className="text-primary hover:text-primary/80 transition-colors">Terms of Service</a> and{' '}
            <a href="/privacy" className="text-primary hover:text-primary/80 transition-colors">Privacy Policy</a>
          </p>
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
