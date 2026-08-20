import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useAuth } from '@/contexts/AuthContext'
import LogoutButton from '../LogoutButton'
import { logger } from '@/utils/logger'

// Mock AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
    warn: jest.fn(),
    info: jest.fn(),
    debug: jest.fn(),
  },
}))

const mockLogout = jest.fn()
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>

const mockUser = {
  id: '1',
  email: 'test@example.com',
  userName: 'test@example.com',
  emailVerified: true,
  phoneVerified: false,
  taxCompliant: false,
  status: 'EmailVerified',
  roles: ['User'],
  permissions: [],
}

describe('LogoutButton', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockUseAuth.mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
      isLoading: false,
      login: jest.fn(),
      logout: mockLogout,
      refreshToken: jest.fn(),
      updateUser: jest.fn(),
    })
  })

  describe('Basic Functionality', () => {
    it('renders nothing when user is not logged in', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: mockLogout,
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      const { container } = render(<LogoutButton />)
      expect(container.firstChild).toBeNull()
    })

    it('renders sign out button when user is logged in', () => {
      render(<LogoutButton />)
      expect(screen.getByRole('button', { name: 'Sign Out' })).toBeInTheDocument()
    })

    it('renders custom children when provided', () => {
      render(<LogoutButton>Custom Logout Text</LogoutButton>)
      expect(screen.getByRole('button', { name: 'Custom Logout Text' })).toBeInTheDocument()
    })

    it('calls logout function when clicked', async () => {
      render(<LogoutButton />)
      
      const button = screen.getByRole('button', { name: 'Sign Out' })
      await userEvent.click(button)

      expect(mockLogout).toHaveBeenCalledWith(false)
    })

    it('shows loading state while logging out', async () => {
      mockLogout.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)))
      
      render(<LogoutButton />)
      
      const button = screen.getByRole('button', { name: 'Sign Out' })
      await userEvent.click(button)

      expect(screen.getByText('Signing Out...')).toBeInTheDocument()
      expect(button).toBeDisabled()

      await waitFor(() => {
        expect(screen.getByText('Sign Out')).toBeInTheDocument()
      })
    })
  })

  describe('Button Variants', () => {
    it('applies button variant styles by default', () => {
      render(<LogoutButton />)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('px-4', 'py-2', 'rounded-full', 'bg-destructive', 'text-destructive-foreground')
    })

    it('applies link variant styles when specified', () => {
      render(<LogoutButton variant="link" />)
      const button = screen.getByRole('button')
      expect(button).not.toHaveClass('px-4', 'py-2', 'rounded-full', 'bg-destructive')
      expect(button).toHaveClass('text-destructive', 'focus:underline')
    })

    it('applies custom className', () => {
      render(<LogoutButton className="custom-class" />)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('custom-class')
    })
  })

  describe('All Devices Option', () => {
    it('shows dropdown when showAllDevicesOption is true', () => {
      render(<LogoutButton showAllDevicesOption={true} />)
      
      const dropdownButton = screen.getByRole('button')
      expect(dropdownButton).toHaveTextContent('Sign Out')
      
      // Check for dropdown arrow SVG
      expect(dropdownButton.querySelector('svg')).toBeInTheDocument()
    })

    it('toggles dropdown menu when clicked', async () => {
      render(<LogoutButton showAllDevicesOption={true} />)
      
      const dropdownButton = screen.getByRole('button')
      await userEvent.click(dropdownButton)

      expect(screen.getByText('Sign out from this device')).toBeInTheDocument()
      expect(screen.getByText('Sign out from all devices')).toBeInTheDocument()

      // Click again to close
      await userEvent.click(dropdownButton)
      expect(screen.queryByText('Sign out from this device')).not.toBeInTheDocument()
    })

    it('calls logout with false when clicking single device option', async () => {
      render(<LogoutButton showAllDevicesOption={true} />)
      
      const dropdownButton = screen.getByRole('button')
      await userEvent.click(dropdownButton)

      const singleDeviceOption = screen.getByText('Sign out from this device')
      await userEvent.click(singleDeviceOption)

      expect(mockLogout).toHaveBeenCalledWith(false)
    })

    it('calls logout with true when clicking all devices option', async () => {
      render(<LogoutButton showAllDevicesOption={true} />)
      
      const dropdownButton = screen.getByRole('button')
      await userEvent.click(dropdownButton)

      const allDevicesOption = screen.getByText('Sign out from all devices')
      await userEvent.click(allDevicesOption)

      expect(mockLogout).toHaveBeenCalledWith(true)
    })

    it('closes dropdown when clicking backdrop', async () => {
      render(<LogoutButton showAllDevicesOption={true} />)
      
      const dropdownButton = screen.getByRole('button')
      await userEvent.click(dropdownButton)

      expect(screen.getByText('Sign out from this device')).toBeInTheDocument()

      // Find and click backdrop
      const backdrop = document.querySelector('.fixed.inset-0')
      expect(backdrop).toBeInTheDocument()
      fireEvent.click(backdrop!)

      expect(screen.queryByText('Sign out from this device')).not.toBeInTheDocument()
    })

    it('closes dropdown after selecting an option', async () => {
      render(<LogoutButton showAllDevicesOption={true} />)
      
      const dropdownButton = screen.getByRole('button')
      await userEvent.click(dropdownButton)

      const singleDeviceOption = screen.getByText('Sign out from this device')
      await userEvent.click(singleDeviceOption)

      expect(screen.queryByText('Sign out from this device')).not.toBeInTheDocument()
    })
  })

  describe('Error Handling', () => {
    beforeEach(() => {
      jest.clearAllMocks()
    })

    it('handles logout errors gracefully', async () => {
      const error = new Error('Logout failed')
      mockLogout.mockRejectedValueOnce(error)

      render(<LogoutButton />)

      const button = screen.getByRole('button', { name: 'Sign Out' })
      await userEvent.click(button)

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith('Logout failed:', error)
        expect(screen.getByText('Sign Out')).toBeInTheDocument()
      })
    })

    it('resets loading state after error', async () => {
      mockLogout.mockImplementation(() => {
        return new Promise((_, reject) => {
          setTimeout(() => reject(new Error('Logout failed')), 50)
        })
      })

      render(<LogoutButton />)

      const button = screen.getByRole('button', { name: 'Sign Out' })
      await userEvent.click(button)

      // The loading state might be very quick, so we check if either state is present
      // After logout completes, button should return to normal state
      await waitFor(() => {
        const buttonElement = screen.getByRole('button')
        expect(buttonElement.textContent).toMatch(/Sign Out|Signing Out.../)
      })

      // Eventually should return to normal state
      await waitFor(() => {
        const buttonElement = screen.getByRole('button')
        expect(buttonElement).toHaveTextContent('Sign Out')
        expect(buttonElement).not.toBeDisabled()
      })
    })

    it('handles logout errors in dropdown mode', async () => {
      mockLogout.mockRejectedValueOnce(new Error('Logout failed'))

      render(<LogoutButton showAllDevicesOption={true} />)

      const dropdownButton = screen.getByRole('button')
      await userEvent.click(dropdownButton)

      const singleDeviceOption = screen.getByText('Sign out from this device')
      await userEvent.click(singleDeviceOption)

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith('Logout failed:', expect.any(Error))
      })
    })
  })

  describe('Disabled State', () => {
    it('disables button while logging out', async () => {
      mockLogout.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)))
      
      render(<LogoutButton />)
      
      const button = screen.getByRole('button')
      await userEvent.click(button)

      expect(button).toBeDisabled()
      expect(button).toHaveClass('disabled:opacity-50', 'disabled:cursor-not-allowed')
    })

    it('shows regular button instead of dropdown when logging out', async () => {
      mockLogout.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)))
      
      render(<LogoutButton showAllDevicesOption={true} />)
      
      // First click opens the dropdown
      const button = screen.getByRole('button')
      await userEvent.click(button)
      expect(screen.getByText('Sign out from this device')).toBeInTheDocument()

      // Click on the single device option to start logout
      const singleDeviceOption = screen.getByText('Sign out from this device')
      await userEvent.click(singleDeviceOption)

      // After clicking, dropdown should be hidden and logout should be in progress
      expect(screen.queryByText('Sign out from this device')).not.toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('has proper focus management', () => {
      render(<LogoutButton />)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('focus:outline-none', 'focus:ring-2', 'focus:ring-offset-2')
    })

    it('maintains focus styles in link variant', () => {
      render(<LogoutButton variant="link" />)
      const button = screen.getByRole('button')
      expect(button).toHaveClass('focus:outline-none', 'focus:underline')
    })
  })
})