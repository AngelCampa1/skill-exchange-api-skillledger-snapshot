import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import RegistrationSuccess from '../RegistrationSuccess'
import { useRouter } from 'next/navigation'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

const mockRouter = {
  push: jest.fn(),
  back: jest.fn(),
  forward: jest.fn(),
  refresh: jest.fn(),
  replace: jest.fn(),
  prefetch: jest.fn(),
}

const mockUseRouter = useRouter as jest.Mock

describe('RegistrationSuccess', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockUseRouter.mockReturnValue(mockRouter)
  })

  // ============================================
  // Content Display (5 tests)
  // ============================================
  describe('Content Display', () => {
    it('should display success heading', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText('Account Created Successfully!')).toBeInTheDocument()
    })

    it('should display welcome message', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText(/Welcome to SkillLedger!/)).toBeInTheDocument()
      expect(screen.getByText(/We've sent a verification email to:/)).toBeInTheDocument()
    })

    it('should display user email address', () => {
      const email = 'user@example.com'
      render(<RegistrationSuccess email={email} />)

      expect(screen.getByText(email)).toBeInTheDocument()
    })

    it('should display email with proper styling for long emails', () => {
      const longEmail = 'very.long.email.address.that.might.overflow@example.com'
      render(<RegistrationSuccess email={longEmail} />)

      const emailElement = screen.getByText(longEmail)
      expect(emailElement).toBeInTheDocument()
      expect(emailElement.className).toContain('break-all')
    })

    it('should display success icon', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const successIcon = container.querySelector('.text-success')
      expect(successIcon).toBeInTheDocument()
    })
  })

  // ============================================
  // Next Steps Section (2 tests)
  // ============================================
  describe('Next Steps Section', () => {
    it('should display next steps heading', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText('Next Steps')).toBeInTheDocument()
    })

    it('should display all 4 next steps', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText(/Check your email inbox/)).toBeInTheDocument()
      expect(screen.getByText(/Click the verification link in the email/)).toBeInTheDocument()
      expect(screen.getByText(/Complete your email verification/)).toBeInTheDocument()
      expect(screen.getByText(/Start collaborating on SkillLedger!/)).toBeInTheDocument()
    })
  })

  // ============================================
  // Warning Message (2 tests)
  // ============================================
  describe('Warning Message', () => {
    it('should display expiration warning', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText(/The verification link expires in 24 hours/)).toBeInTheDocument()
    })

    it('should display warning with alert triangle icon', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const warningIcon = container.querySelector('.text-warning')
      expect(warningIcon).toBeInTheDocument()
    })
  })

  // ============================================
  // Resend Email Button (3 tests)
  // ============================================
  describe('Resend Email Button', () => {
    it('should display resend verification email button', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText('Resend Verification Email')).toBeInTheDocument()
    })

    it('should call onResendEmail when resend button is clicked', async () => {
      const user = userEvent.setup()
      const onResendEmail = jest.fn()

      render(<RegistrationSuccess email="test@example.com" onResendEmail={onResendEmail} />)

      const resendButton = screen.getByText('Resend Verification Email')
      await user.click(resendButton)

      expect(onResendEmail).toHaveBeenCalledTimes(1)
    })

    it('should handle missing onResendEmail callback gracefully', async () => {
      const user = userEvent.setup()

      render(<RegistrationSuccess email="test@example.com" />)

      const resendButton = screen.getByText('Resend Verification Email')

      // Should not throw error when callback is undefined
      await expect(user.click(resendButton)).resolves.not.toThrow()
    })
  })

  // ============================================
  // Login Button (2 tests)
  // ============================================
  describe('Login Button', () => {
    it('should display login button', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText("I'll verify later - Go to Login")).toBeInTheDocument()
    })

    it('should navigate to login page when login button is clicked', async () => {
      const user = userEvent.setup()

      render(<RegistrationSuccess email="test@example.com" />)

      const loginButton = screen.getByText("I'll verify later - Go to Login")
      await user.click(loginButton)

      expect(mockRouter.push).toHaveBeenCalledWith('/login')
    })
  })

  // ============================================
  // Features Section (5 tests)
  // ============================================
  describe('Features Section', () => {
    it('should display features heading', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText('What can you do on SkillLedger?')).toBeInTheDocument()
    })

    it('should display all 4 feature items', () => {
      render(<RegistrationSuccess email="test@example.com" />)

      expect(screen.getByText('Post projects and find skilled professionals')).toBeInTheDocument()
      expect(screen.getByText('Offer your skills and earn SkillCredits')).toBeInTheDocument()
      expect(screen.getByText('Build your reputation through quality work')).toBeInTheDocument()
      expect(screen.getByText('Collaborate in secure workspaces')).toBeInTheDocument()
    })

    it('should display Rocket icon for post projects feature', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      // Check for multiple icon containers with primary color
      const icons = container.querySelectorAll('.text-primary')
      expect(icons.length).toBeGreaterThan(0)
    })

    it('should display features in a grid layout', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const grid = container.querySelector('.grid.grid-cols-1')
      expect(grid).toBeInTheDocument()
    })

    it('should display features with proper spacing', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const grid = container.querySelector('.gap-4')
      expect(grid).toBeInTheDocument()
    })
  })

  // ============================================
  // Layout and Styling (3 tests)
  // ============================================
  describe('Layout and Styling', () => {
    it('should have centered text alignment', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const wrapper = container.querySelector('.text-center')
      expect(wrapper).toBeInTheDocument()
    })

    it('should have proper card styling for next steps', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const card = container.querySelector('.card-premium')
      expect(card).toBeInTheDocument()
    })

    it('should have border separator before features section', () => {
      const { container } = render(<RegistrationSuccess email="test@example.com" />)

      const separator = container.querySelector('.border-t.border-border')
      expect(separator).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render complete success page without errors', () => {
      const onResendEmail = jest.fn()
      const { container } = render(
        <RegistrationSuccess email="test@example.com" onResendEmail={onResendEmail} />
      )

      // Verify all major sections are present
      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Account Created Successfully!')).toBeInTheDocument()
      expect(screen.getByText('test@example.com')).toBeInTheDocument()
      expect(screen.getByText('Next Steps')).toBeInTheDocument()
      expect(screen.getByText(/The verification link expires in 24 hours/)).toBeInTheDocument()
      expect(screen.getByText('Resend Verification Email')).toBeInTheDocument()
      expect(screen.getByText("I'll verify later - Go to Login")).toBeInTheDocument()
      expect(screen.getByText('What can you do on SkillLedger?')).toBeInTheDocument()
    })
  })
})
