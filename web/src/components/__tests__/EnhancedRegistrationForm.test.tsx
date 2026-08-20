/**
 * Tests for EnhancedRegistrationForm
 *
 * Comprehensive test suite for the enhanced registration form component
 * Coverage target: 70%+ (404 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import EnhancedRegistrationForm from '../EnhancedRegistrationForm'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

// Mock device fingerprinting
jest.mock('../../utils/deviceFingerprinting', () => ({
  collectDeviceFingerprint: jest.fn(),
  setDeviceFingerprintConsent: jest.fn(),
}))

// Mock geolocation
jest.mock('../../utils/geolocation', () => ({
  getUserGeolocation: jest.fn(),
  isLocationRestricted: jest.fn(),
  getLocationRestrictionMessage: jest.fn(),
  getVPNWarningMessage: jest.fn(),
  getEnhancedVerificationMessage: jest.fn(),
}))

// Mock analytics
jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}))

import { collectDeviceFingerprint, setDeviceFingerprintConsent } from '../../utils/deviceFingerprinting'
import {
  getUserGeolocation,
  isLocationRestricted,
  getLocationRestrictionMessage,
  getVPNWarningMessage,
  getEnhancedVerificationMessage,
} from '../../utils/geolocation'

const mockCollectDeviceFingerprint = collectDeviceFingerprint as jest.MockedFunction<typeof collectDeviceFingerprint>
const mockGetUserGeolocation = getUserGeolocation as jest.MockedFunction<typeof getUserGeolocation>
const mockIsLocationRestricted = isLocationRestricted as jest.MockedFunction<typeof isLocationRestricted>
const mockGetLocationRestrictionMessage = getLocationRestrictionMessage as jest.MockedFunction<typeof getLocationRestrictionMessage>
const mockGetVPNWarningMessage = getVPNWarningMessage as jest.MockedFunction<typeof getVPNWarningMessage>
const mockGetEnhancedVerificationMessage = getEnhancedVerificationMessage as jest.MockedFunction<typeof getEnhancedVerificationMessage>

describe('EnhancedRegistrationForm', () => {
  const mockOnSubmit = jest.fn()
  const mockDeviceFingerprint = {
    userAgent: 'Mozilla/5.0',
    screenResolution: '1920x1080',
    timezone: 'America/New_York',
    acceptLanguage: 'en-US',
    platform: 'Win32',
    colorDepth: 24,
    deviceMemory: 8,
    hardwareConcurrency: 8,
    touchSupport: false,
    cookieEnabled: true,
    doNotTrack: null,
    installedPlugins: [],
    availableFonts: [],
  }

  const mockGeolocation = {
    ip: '192.168.1.1',
    city: 'San Francisco',
    region: 'California',
    country: 'United States',
    countryCode: 'US',
    timezone: 'America/Los_Angeles',
    isVPN: false,
    isProxy: false,
    isTor: false,
    riskScore: 10,
    isRestricted: false,
  }

  beforeEach(() => {
    jest.clearAllMocks()

    // Default mock implementations
    mockCollectDeviceFingerprint.mockResolvedValue(mockDeviceFingerprint)
    mockGetUserGeolocation.mockResolvedValue({
      success: true,
      data: mockGeolocation,
    })
    mockIsLocationRestricted.mockReturnValue({
      isRestricted: false,
      requiresEnhancedVerification: false,
    })
    mockGetLocationRestrictionMessage.mockReturnValue(null)
    mockGetVPNWarningMessage.mockReturnValue(null)
    mockGetEnhancedVerificationMessage.mockReturnValue(null)
  })

  describe('Initial Loading State', () => {
    it('should show loading spinner while security checks are running', async () => {
      mockCollectDeviceFingerprint.mockImplementation(() => new Promise(() => {}))

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('Initializing security checks...')).toBeInTheDocument()
    })

    it('should hide loading spinner after security checks complete', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.queryByText('Initializing security checks...')).not.toBeInTheDocument()
      })
    })
  })

  describe('Basic Rendering', () => {
    it('should render the registration form after security checks', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      expect(screen.getByTestId('password-input')).toBeInTheDocument()
      expect(screen.getByTestId('confirm-password-input')).toBeInTheDocument()
      expect(screen.getByTestId('submit-button')).toBeInTheDocument()
    })

    it('should render device fingerprint consent checkbox', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/Enhanced Fraud Protection/i)).toBeInTheDocument()
      })
    })

    it('should render submit button with correct text', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('submit-button')).toHaveTextContent('Create Account')
      })
    })
  })

  describe('Location Restrictions', () => {
    it('should show restriction message when location is blocked', async () => {
      mockIsLocationRestricted.mockReturnValue({
        isRestricted: true,
        requiresEnhancedVerification: false,
      })
      mockGetLocationRestrictionMessage.mockReturnValue('Registration is not available in your country')

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText('Registration Unavailable')).toBeInTheDocument()
      })

      expect(screen.getByText('Registration is not available in your country')).toBeInTheDocument()
      expect(screen.queryByTestId('email-input')).not.toBeInTheDocument()
    })

    it('should show VPN warning when VPN is detected', async () => {
      mockGetVPNWarningMessage.mockReturnValue('VPN detected. Please disable VPN to continue.')

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText('VPN/Proxy Detected')).toBeInTheDocument()
      })

      expect(screen.getByText('VPN detected. Please disable VPN to continue.')).toBeInTheDocument()
    })

    it('should show enhanced verification message', async () => {
      mockIsLocationRestricted.mockReturnValue({
        isRestricted: false,
        requiresEnhancedVerification: true,
      })
      mockGetEnhancedVerificationMessage.mockReturnValue('Additional verification required')

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText('Enhanced Verification Required')).toBeInTheDocument()
      })

      expect(screen.getByText('Additional verification required')).toBeInTheDocument()
    })

    it('should not show enhanced verification message when VPN warning is present', async () => {
      mockIsLocationRestricted.mockReturnValue({
        isRestricted: false,
        requiresEnhancedVerification: true,
      })
      mockGetVPNWarningMessage.mockReturnValue('VPN detected')
      mockGetEnhancedVerificationMessage.mockReturnValue('Additional verification required')

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText('VPN/Proxy Detected')).toBeInTheDocument()
      })

      expect(screen.queryByText('Enhanced Verification Required')).not.toBeInTheDocument()
    })
  })

  describe('Security Information Display', () => {
    it('should display geolocation information', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText('Security Information')).toBeInTheDocument()
      })

      expect(screen.getByText(/San Francisco, United States/)).toBeInTheDocument()
      expect(screen.getByText(/192\.168\.1\.1/)).toBeInTheDocument()
    })

    it('should show risk level for medium-risk locations', async () => {
      mockGetUserGeolocation.mockResolvedValue({
        success: true,
        data: { ...mockGeolocation, riskScore: 50 },
      })

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText(/Risk Level: Medium/)).toBeInTheDocument()
      })
    })

    it('should show risk level for high-risk locations', async () => {
      mockGetUserGeolocation.mockResolvedValue({
        success: true,
        data: { ...mockGeolocation, riskScore: 80 },
      })

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByText(/Risk Level: High/)).toBeInTheDocument()
      })
    })

    it('should not show risk level for low-risk locations', async () => {
      mockGetUserGeolocation.mockResolvedValue({
        success: true,
        data: { ...mockGeolocation, riskScore: 20 },
      })

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.queryByText(/Risk Level/)).not.toBeInTheDocument()
      })
    })
  })

  describe('Email Validation', () => {
    it('should validate email format', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      await user.type(emailInput, 'invalid-email')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('email-error')).toHaveTextContent('Please enter a valid email address')
      })
    })

    it('should accept valid email', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      await user.type(emailInput, 'test@example.com')
      await user.tab()

      expect(screen.queryByTestId('email-error')).not.toBeInTheDocument()
    })
  })

  describe('Password Validation', () => {
    it('should require minimum password length', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'Short1!')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('password-error')).toHaveTextContent('Password must be at least 12 characters')
      })
    })

    it('should require uppercase letter', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'alllowercase1!')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('password-error')).toHaveTextContent('Password must contain at least one uppercase letter')
      })
    })

    it('should require lowercase letter', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'ALLUPPERCASE1!')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('password-error')).toHaveTextContent('Password must contain at least one lowercase letter')
      })
    })

    it('should require number', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'NoNumbersHere!')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('password-error')).toHaveTextContent('Password must contain at least one number')
      })
    })

    it('should require special character', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'NoSpecialChar1')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('password-error')).toHaveTextContent('Password must contain at least one special character')
      })
    })

    it('should accept valid password', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'ValidPassword123!')
      await user.tab()

      expect(screen.queryByTestId('password-error')).not.toBeInTheDocument()
    })
  })

  describe('Password Strength Indicator', () => {
    it('should show password strength indicator when password is entered', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'Test123!')

      await waitFor(() => {
        expect(screen.getByTestId('strength-text')).toBeInTheDocument()
        expect(screen.getByTestId('strength-bar')).toBeInTheDocument()
      })
    })

    it('should show "Weak" for weak passwords', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'weak')

      await waitFor(() => {
        expect(screen.getByTestId('strength-text')).toHaveTextContent('Weak')
      })
    })

    it('should show "Fair" for fair passwords', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'Fair123')

      await waitFor(() => {
        expect(screen.getByTestId('strength-text')).toHaveTextContent('Fair')
      })
    })

    it('should show "Good" for good passwords', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'GoodPassword1')

      await waitFor(() => {
        expect(screen.getByTestId('strength-text')).toHaveTextContent('Good')
      })
    })

    it('should show "Strong" for strong passwords', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      await user.type(passwordInput, 'VeryStrongPassword123!')

      await waitFor(() => {
        expect(screen.getByTestId('strength-text')).toHaveTextContent('Strong')
      })
    })
  })

  describe('Password Visibility Toggle', () => {
    it('should toggle password visibility when toggle button is clicked', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input') as HTMLInputElement
      const toggleButton = screen.getByTestId('toggle-password')

      expect(passwordInput.type).toBe('password')

      await user.click(toggleButton)
      expect(passwordInput.type).toBe('text')

      await user.click(toggleButton)
      expect(passwordInput.type).toBe('password')
    })
  })

  describe('Confirm Password Validation', () => {
    it('should validate passwords match', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      const confirmPasswordInput = screen.getByTestId('confirm-password-input')

      await user.type(passwordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'DifferentPassword123!')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('confirm-password-error')).toHaveTextContent("Passwords don't match")
      })
    })

    it('should not show error when passwords match', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('password-input')).toBeInTheDocument()
      })

      const passwordInput = screen.getByTestId('password-input')
      const confirmPasswordInput = screen.getByTestId('confirm-password-input')

      await user.type(passwordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'ValidPassword123!')
      await user.tab()

      expect(screen.queryByTestId('confirm-password-error')).not.toBeInTheDocument()
    })
  })

  describe('Form Submission', () => {
    it('should call onSubmit with form data and device fingerprint', async () => {
      const user = userEvent.setup()
      mockOnSubmit.mockResolvedValue(undefined)

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      const passwordInput = screen.getByTestId('password-input')
      const confirmPasswordInput = screen.getByTestId('confirm-password-input')
      const submitButton = screen.getByTestId('submit-button')

      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'ValidPassword123!')

      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            email: 'test@example.com',
            password: 'ValidPassword123!',
            confirmPassword: 'ValidPassword123!',
            deviceFingerprint: mockDeviceFingerprint,
            geolocation: mockGeolocation,
          })
        )
      })
    })

    it('should store device fingerprint consent when checkbox is checked', async () => {
      const user = userEvent.setup()
      mockOnSubmit.mockResolvedValue(undefined)

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      const passwordInput = screen.getByTestId('password-input')
      const confirmPasswordInput = screen.getByTestId('confirm-password-input')
      const consentCheckbox = screen.getByLabelText(/Enhanced Fraud Protection/i)
      const submitButton = screen.getByTestId('submit-button')

      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'ValidPassword123!')
      await user.click(consentCheckbox)

      await user.click(submitButton)

      await waitFor(() => {
        expect(setDeviceFingerprintConsent).toHaveBeenCalledWith(true)
      })
    })

    it('should show loading state during submission', async () => {
      const user = userEvent.setup()
      let resolveSubmit: () => void
      mockOnSubmit.mockImplementation(() => new Promise<void>((resolve) => {
        resolveSubmit = resolve
      }))

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      const passwordInput = screen.getByTestId('password-input')
      const confirmPasswordInput = screen.getByTestId('confirm-password-input')
      const submitButton = screen.getByTestId('submit-button')

      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'ValidPassword123!')

      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Creating Account...')).toBeInTheDocument()
      })

      resolveSubmit!()
    })

    it('should not submit if device fingerprint is not available', async () => {
      const user = userEvent.setup()
      mockCollectDeviceFingerprint.mockResolvedValue(null as any)

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      const passwordInput = screen.getByTestId('password-input')
      const confirmPasswordInput = screen.getByTestId('confirm-password-input')
      const submitButton = screen.getByTestId('submit-button')

      await user.type(emailInput, 'test@example.com')
      await user.type(passwordInput, 'ValidPassword123!')
      await user.type(confirmPasswordInput, 'ValidPassword123!')

      await waitFor(() => {
        expect(submitButton).toBeDisabled()
      })
    })

    it('should disable form during loading', () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} isLoading={true} />)

      waitFor(() => {
        const submitButton = screen.getByTestId('submit-button')
        expect(submitButton).toBeDisabled()
      })
    })
  })

  describe('Error Handling', () => {
    it('should handle security check failures gracefully', async () => {
      mockCollectDeviceFingerprint.mockRejectedValue(new Error('Fingerprint failed'))
      mockGetUserGeolocation.mockRejectedValue(new Error('Geolocation failed'))

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      // Form should still render after errors
      await waitFor(() => {
        expect(screen.queryByText('Initializing security checks...')).not.toBeInTheDocument()
      })
    })

    it('should handle geolocation failure without crashing', async () => {
      mockGetUserGeolocation.mockResolvedValue({
        success: false,
        data: undefined,
      })

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.queryByText('Security Information')).not.toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('should have proper labels for all inputs', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText('Email Address')).toBeInTheDocument()
      })

      expect(screen.getByLabelText('Password')).toBeInTheDocument()
      expect(screen.getByLabelText('Confirm Password')).toBeInTheDocument()
    })

    it('should have accessible submit button', async () => {
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-button')
        expect(submitButton).toBeInTheDocument()
      })
    })

    it('should show error messages with proper test IDs', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      const emailInput = screen.getByTestId('email-input')
      await user.type(emailInput, 'invalid')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByTestId('email-error')).toBeInTheDocument()
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle empty form submission attempt', async () => {
      const user = userEvent.setup()
      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('submit-button')).toBeInTheDocument()
      })

      const submitButton = screen.getByTestId('submit-button')
      await user.click(submitButton)

      // Should not call onSubmit with empty fields
      expect(mockOnSubmit).not.toHaveBeenCalled()
    })

    it('should handle device fingerprint without geolocation', async () => {
      mockGetUserGeolocation.mockResolvedValue({
        success: true,
        data: undefined,
      })

      render(<EnhancedRegistrationForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByTestId('email-input')).toBeInTheDocument()
      })

      expect(screen.queryByText('Security Information')).not.toBeInTheDocument()
    })
  })
})
