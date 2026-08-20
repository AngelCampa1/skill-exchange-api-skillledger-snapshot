'use client'

import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Eye, EyeOff, Mail, Lock } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Progress } from '@/components/ui/progress'

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
  firstName: z.string().min(1, 'First name is required').max(50, 'First name is too long'),
  lastName: z.string().min(1, 'Last name is required').max(50, 'Last name is too long'),
  acceptedTerms: z.boolean().refine(val => val === true, 'You must accept the terms and conditions'),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
})

type RegistrationFormData = z.infer<typeof registrationSchema>

interface RegistrationFormProps {
  onSubmit: (data: RegistrationFormData) => Promise<void>
  isLoading?: boolean
  defaultValues?: Partial<Record<string, string>>
  onFieldChange?: (name: string, value: string) => void
}

export default function RegistrationForm({ onSubmit, isLoading = false, defaultValues, onFieldChange }: RegistrationFormProps) {
  const [showPassword, setShowPassword] = useState(false)
  const [passwordStrength, setPasswordStrength] = useState(0)
  
  const {
    register: registerField,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegistrationFormData>({
    resolver: zodResolver(registrationSchema),
    mode: 'onChange',
    reValidateMode: 'onChange',
    defaultValues: defaultValues ? {
      firstName: defaultValues.firstName || '',
      lastName: defaultValues.lastName || '',
      email: defaultValues.email || '',
    } : undefined,
  })

  // Watch fields for persistence
  const firstName = watch('firstName')
  const lastName = watch('lastName')
  const email = watch('email')

  React.useEffect(() => {
    if (onFieldChange && firstName !== undefined) onFieldChange('firstName', firstName || '')
  }, [firstName, onFieldChange])

  React.useEffect(() => {
    if (onFieldChange && lastName !== undefined) onFieldChange('lastName', lastName || '')
  }, [lastName, onFieldChange])

  React.useEffect(() => {
    if (onFieldChange && email !== undefined) onFieldChange('email', email || '')
  }, [email, onFieldChange])

  const password = watch('password')

  // BUG-032 FIX: Enhanced password strength calculation with common pattern detection
  // Calculate password strength (0-100)
  const calculatePasswordStrength = (pwd: string): number => {
    if (!pwd) return 0
    
    let score = 0
    
    // Length scoring
    if (pwd.length >= 12) score += 25
    if (pwd.length >= 16) score += 10
    
    // Character variety scoring
    if (/[A-Z]/.test(pwd)) score += 15
    if (/[a-z]/.test(pwd)) score += 15
    if (/[0-9]/.test(pwd)) score += 15
    if (/[^A-Za-z0-9]/.test(pwd)) score += 20
    
    // BUG-032 FIX: Penalize common patterns
    const lowerPwd = pwd.toLowerCase()
    
    // Common words penalty
    const commonWords = ['password', 'admin', 'user', 'login', 'welcome', 'qwerty', 'letmein']
    if (commonWords.some(word => lowerPwd.includes(word))) {
      score -= 30
    }
    
    // BUG-MED-002 FIX: Sequential and reversed sequential characters penalty (123, abc, 321, cba, etc.)
    // Forward sequences: 012, 123, abc, bcd, etc.
    if (/(?:012|123|234|345|456|567|678|789|890|abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)/i.test(pwd)) {
      score -= 20
    }
    // Reverse sequences: 987, 321, zyx, cba, etc.
    if (/(?:987|876|765|654|543|432|321|210|109|098|zyx|yxw|xwv|wvu|vut|uts|tsr|srq|rqp|qpo|pon|onm|nml|mlk|lkj|kji|jih|ihg|hgf|gfe|fed|edc|dcb|cba)/i.test(pwd)) {
      score -= 20
    }
    
    // Repeated characters penalty (aaa, 111, etc.)
    if (/(.)\1{2,}/.test(pwd)) {
      score -= 15
    }
    
    // Keyboard patterns penalty
    if (/(?:qwerty|asdfgh|zxcvbn)/i.test(pwd)) {
      score -= 25
    }
    
    return Math.max(0, Math.min(score, 100))
  }

  // Update password strength when password changes
  React.useEffect(() => {
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
    await onSubmit(data)
  }

  return (
    <div className="container-centered">
      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="firstName" required>
              First Name
            </Label>
            <Input
              {...registerField('firstName')}
              type="text"
              id="firstName"
              name="firstName"
              data-testid="firstName-input"
              placeholder="First name"
              disabled={isLoading || isSubmitting}
              error={!!errors.firstName}
              helperText={errors.firstName?.message}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="lastName" required>
              Last Name
            </Label>
            <Input
              {...registerField('lastName')}
              type="text"
              id="lastName"
              name="lastName"
              data-testid="lastName-input"
              placeholder="Last name"
              disabled={isLoading || isSubmitting}
              error={!!errors.lastName}
              helperText={errors.lastName?.message}
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email" required>
            Email Address
          </Label>
          <Input
            {...registerField('email')}
            type="email"
            id="email"
            data-testid="email-input"
            placeholder="Enter your email"
            autoComplete="email"
            disabled={isLoading || isSubmitting}
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
            {...registerField('password')}
            type={showPassword ? 'text' : 'password'}
            id="password"
            data-testid="password-input"
            placeholder="Create password"
            autoComplete="new-password"
            disabled={isLoading || isSubmitting}
            error={!!errors.password}
            helperText={errors.password?.message}
            startIcon={<Lock className="w-4 h-4" />}
            endIcon={
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="text-muted-foreground hover:text-foreground transition-colors"
                data-testid="toggle-password"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
                aria-pressed={showPassword}
                aria-controls="password"
              >
                {showPassword ? (
                  <EyeOff className="w-4 h-4" aria-hidden="true" />
                ) : (
                  <Eye className="w-4 h-4" aria-hidden="true" />
                )}
              </button>
            }
          />

          <div className="mt-2 text-xs text-muted-foreground space-y-1">
            <p className="font-medium">Password requirements:</p>
            <ul className="list-disc list-inside space-y-0.5 ml-1">
              <li>At least 12 characters long</li>
              <li>Mix of uppercase and lowercase letters</li>
              <li>At least one number and one special character</li>
              <li>Avoid common words, patterns, or sequences (e.g., "Password", "123", "abc")</li>
            </ul>
          </div>

          {password && (
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
          <Label htmlFor="confirm-password" required>
            Confirm Password
          </Label>
          <Input
            {...registerField('confirmPassword')}
            type="password"
            id="confirm-password"
            data-testid="confirm-password-input"
            placeholder="Confirm password"
            autoComplete="new-password"
            disabled={isLoading || isSubmitting}
            error={!!errors.confirmPassword}
            helperText={errors.confirmPassword?.message}
            startIcon={<Lock className="w-4 h-4" />}
          />
        </div>

        <div className="space-y-2">
          <div className="flex items-start space-x-2">
            <input
              {...registerField('acceptedTerms')}
              type="checkbox"
              id="acceptedTerms"
              data-testid="terms-checkbox"
              className="mt-1 h-4 w-4 text-primary border-border rounded focus:ring-ring"
            />
            <Label htmlFor="acceptedTerms" className="text-sm text-muted-foreground">
              I agree to the{' '}
              <a 
                href="/terms" 
                target="_blank" 
                rel="noopener noreferrer"
                className="text-primary hover:text-primary/80 underline"
              >
                Terms of Service
              </a>
              {' '}and{' '}
              <a 
                href="/privacy" 
                target="_blank" 
                rel="noopener noreferrer"
                className="text-primary hover:text-primary/80 underline"
              >
                Privacy Policy
              </a>
            </Label>
          </div>
          {errors.acceptedTerms && (
            <p className="text-sm text-destructive">{errors.acceptedTerms.message}</p>
          )}
        </div>

        <Button
          type="submit"
          disabled={isLoading || isSubmitting}
          loading={isLoading || isSubmitting}
          loadingText="Creating Account..."
          data-testid="submit-button"
          className="w-full"
        >
          Create Account
        </Button>
      </form>
    </div>
  )
}
