'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { collectDeviceFingerprint, DeviceFingerprint, setDeviceFingerprintConsent } from '../utils/deviceFingerprinting'
import { getUserGeolocation, isLocationRestricted, getLocationRestrictionMessage, getVPNWarningMessage, getEnhancedVerificationMessage, GeolocationInfo } from '../utils/geolocation'

const registrationSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z
    .string()
    .min(12, 'Password must be at least 12 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one number')
    .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character'),
  confirmPassword: z.string(),
  // BUG-HIGH-009 FIX: Add consent checkbox for device fingerprinting
  deviceFingerprintConsent: z.boolean().default(false),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
})

type RegistrationFormData = z.infer<typeof registrationSchema>

interface EnhancedRegistrationFormProps {
  onSubmit: (data: RegistrationFormData & { deviceFingerprint: DeviceFingerprint; geolocation?: GeolocationInfo }) => Promise<void>
  isLoading?: boolean
}

export default function EnhancedRegistrationForm({ onSubmit, isLoading = false }: EnhancedRegistrationFormProps) {
  const [showPassword, setShowPassword] = useState(false)
  const [passwordStrength, setPasswordStrength] = useState(0)
  const [deviceFingerprint, setDeviceFingerprint] = useState<DeviceFingerprint | null>(null)
  const [geolocation, setGeolocation] = useState<GeolocationInfo | null>(null)
  const [locationRestricted, setLocationRestricted] = useState(false)
  const [locationMessage, setLocationMessage] = useState<string | null>(null)
  const [vpnWarning, setVPNWarning] = useState<string | null>(null)
  const [enhancedVerificationRequired, setEnhancedVerificationRequired] = useState(false)
  const [securityChecksLoading, setSecurityChecksLoading] = useState(true)
  
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegistrationFormData>({
    resolver: zodResolver(registrationSchema),
    mode: 'onChange',
    reValidateMode: 'onChange',
  })

  const password = watch('password')

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

  // Initialize security checks on component mount
  useEffect(() => {
    async function initializeSecurityChecks() {
      try {
        // Collect device fingerprint
        const fingerprint = await collectDeviceFingerprint()
        setDeviceFingerprint(fingerprint)
        
        // Get user geolocation
        const geoResponse = await getUserGeolocation()
        
        if (geoResponse.success && geoResponse.data) {
          const geoData = geoResponse.data
          setGeolocation(geoData)
          
          // Check location restrictions
          const restriction = isLocationRestricted(geoData.countryCode)
          setLocationRestricted(restriction.isRestricted)
          setEnhancedVerificationRequired(restriction.requiresEnhancedVerification)
          
          if (restriction.isRestricted) {
            setLocationMessage(getLocationRestrictionMessage(geoData))
          }
          
          // Check for VPN/Proxy warnings
          const vpnMsg = getVPNWarningMessage(geoData)
          if (vpnMsg) {
            setVPNWarning(vpnMsg)
          }
          
          // Enhanced verification message
          if (restriction.requiresEnhancedVerification) {
            const enhancedMsg = getEnhancedVerificationMessage(geoData.countryCode)
            if (enhancedMsg && !vpnMsg && !restriction.isRestricted) {
              setLocationMessage(enhancedMsg)
            }
          }
        }
      } catch (error) {
        logger.error('Security checks failed:', error)
        // Continue with registration if security checks fail
      } finally {
        setSecurityChecksLoading(false)
      }
    }
    
    initializeSecurityChecks()
  }, [])
  
  // Update password strength when password changes
  useEffect(() => {
    if (password) {
      setPasswordStrength(calculatePasswordStrength(password))
    } else {
      setPasswordStrength(0)
    }
  }, [password])

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

  const handleFormSubmit = async (data: RegistrationFormData) => {
    if (!deviceFingerprint) {
      logger.error('Device fingerprint not available')
      return
    }

    // BUG-HIGH-009 FIX: Store device fingerprint consent before submitting
    if (data.deviceFingerprintConsent) {
      setDeviceFingerprintConsent(true)
    }

    try {
      await onSubmit({
        ...data,
        deviceFingerprint,
        geolocation: geolocation || undefined
      })

      // Track successful registration
      const { trackEvent } = await import('@/utils/analytics')
      trackEvent({
        name: 'sign_up',
        category: 'authentication',
        priority: 'critical',
        properties: {
          method: 'email',
          enhanced_verification_required: enhancedVerificationRequired,
          has_geolocation: !!geolocation,
        },
      })
    } catch (error) {
      // Error is handled by the parent component
      throw error
    }
  }

  // Show loading state while security checks are running
  if (securityChecksLoading) {
    return (
      <div className="w-full max-w-md mx-auto">
        <div className="flex items-center justify-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          <span className="ml-3 text-muted-foreground">Initializing security checks...</span>
        </div>
      </div>
    )
  }
  
  // Show restriction message if location is blocked
  if (locationRestricted && locationMessage) {
    return (
      <div className="w-full max-w-md mx-auto">
        <div className="bg-destructive/10 border border-destructive/20 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-destructive">
                Registration Unavailable
              </h3>
              <div className="mt-2 text-sm text-destructive">
                <p>{locationMessage}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    )
  }
  
  return (
    <div className="w-full max-w-md mx-auto">
      {/* Warning messages */}
      {vpnWarning && (
        <div className="mb-4 bg-warning/10 border border-warning/20 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-warning" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-warning">
                VPN/Proxy Detected
              </h3>
              <div className="mt-2 text-sm text-warning">
                <p>{vpnWarning}</p>
              </div>
            </div>
          </div>
        </div>
      )}
      
      {/* Enhanced verification notice */}
      {enhancedVerificationRequired && locationMessage && !vpnWarning && (
        <div className="mb-4 bg-primary/10 border border-primary/20 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-primary" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-primary">
                Enhanced Verification Required
              </h3>
              <div className="mt-2 text-sm text-primary">
                <p>{locationMessage}</p>
              </div>
            </div>
          </div>
        </div>
      )}
      
      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-6">
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-foreground">
            Email Address
          </label>
          <input
            {...register('email')}
            type="email"
            id="email"
            data-testid="email-input"
            className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-input bg-background text-foreground"
            placeholder="Enter your email"
            disabled={isLoading || isSubmitting}
          />
          {errors.email && (
            <p className="mt-1 text-sm text-destructive" data-testid="email-error">
              {errors.email.message}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="password" className="block text-sm font-medium text-foreground">
            Password
          </label>
          <div className="relative">
            <input
              {...register('password')}
              type={showPassword ? 'text' : 'password'}
              id="password"
              data-testid="password-input"
              className="mt-1 block w-full px-3 py-2 pr-10 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-input bg-background text-foreground"
              placeholder="Create a secure password"
              disabled={isLoading || isSubmitting}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute inset-y-0 right-0 pr-3 flex items-center"
              data-testid="toggle-password"
            >
              {showPassword ? '🙈' : '👁️'}
            </button>
          </div>

          {password && (
            <div className="mt-2">
              <div className="flex justify-between text-sm text-muted-foreground mb-1">
                <span>Password strength:</span>
                <span data-testid="strength-text">{getStrengthText(passwordStrength)}</span>
              </div>
              <div className="w-full bg-muted rounded-full h-2">
                <div
                  className={`h-2 rounded-full transition-all ${getStrengthColor(passwordStrength)}`}
                  style={{ width: `${passwordStrength}%` }}
                  data-testid="strength-bar"
                />
              </div>
            </div>
          )}

          {errors.password && (
            <p className="mt-1 text-sm text-destructive" data-testid="password-error">
              {errors.password.message}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="confirmPassword" className="block text-sm font-medium text-foreground">
            Confirm Password
          </label>
          <input
            {...register('confirmPassword')}
            type="password"
            id="confirmPassword"
            data-testid="confirm-password-input"
            className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-input bg-background text-foreground"
            placeholder="Confirm your password"
            disabled={isLoading || isSubmitting}
          />
          {errors.confirmPassword && (
            <p className="mt-1 text-sm text-destructive" data-testid="confirm-password-error">
              {errors.confirmPassword.message}
            </p>
          )}
        </div>

        {/* Security information display */}
        {geolocation && (
          <div className="bg-muted border border-border rounded-md p-3">
            <h4 className="text-sm font-medium text-foreground mb-2">Security Information</h4>
            <div className="text-xs text-muted-foreground space-y-1">
              <div>Location: {geolocation.city}, {geolocation.country}</div>
              <div>IP Address: {geolocation.ip}</div>
              {geolocation.riskScore > 30 && (
                <div className="text-warning">
                  Risk Level: {geolocation.riskScore > 70 ? 'High' : 'Medium'}
                </div>
              )}
            </div>
          </div>
        )}

        {/* BUG-HIGH-009 FIX: Device fingerprinting consent checkbox for GDPR/CCPA compliance */}
        <div className="space-y-4">
          <div className="flex items-start">
            <input
              type="checkbox"
              {...register('deviceFingerprintConsent')}
              id="deviceFingerprintConsent"
              className="h-4 w-4 text-primary focus:ring-ring border-input rounded mt-1"
            />
            <label htmlFor="deviceFingerprintConsent" className="ml-2 block text-sm text-foreground">
              <span className="font-medium">Enhanced Fraud Protection (Optional)</span>
              <p className="text-xs text-muted-foreground mt-1">
                Allow SkillLedger to collect advanced device characteristics (canvas fingerprint, audio fingerprint, installed fonts)
                to enhance security and prevent fraud. This helps us detect suspicious activity and protect your account.
                Basic security information is always collected. You can change this preference in your privacy settings.
              </p>
            </label>
          </div>
        </div>

        <button
          type="submit"
          disabled={isLoading || isSubmitting || !deviceFingerprint}
          data-testid="submit-button"
          className="w-full flex justify-center py-2 px-4 border border-transparent rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading || isSubmitting ? 'Creating Account...' : 'Create Account'}
        </button>

        {!deviceFingerprint && (
          <p className="text-xs text-muted-foreground text-center">
            Preparing security verification...
          </p>
        )}
      </form>
    </div>
  )
}