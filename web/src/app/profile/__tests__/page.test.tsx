import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import ProfileRedirect from '../page'
import { useRouter } from 'next/navigation'

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>

describe('ProfileRedirect', () => {
  let mockRouter: { replace: jest.Mock }

  beforeEach(() => {
    jest.clearAllMocks()
    mockRouter = {
      replace: jest.fn(),
    }
    mockUseRouter.mockReturnValue(mockRouter as any)
  })

  // ========================================
  // Redirect Tests
  // ========================================
  describe('Redirect Behavior', () => {
    it('should redirect to /profile/me on mount', () => {
      render(<ProfileRedirect />)

      expect(mockRouter.replace).toHaveBeenCalledWith('/profile/me')
      expect(mockRouter.replace).toHaveBeenCalledTimes(1)
    })

    it('should call router.replace only once even with multiple renders', () => {
      const { rerender } = render(<ProfileRedirect />)

      expect(mockRouter.replace).toHaveBeenCalledTimes(1)

      rerender(<ProfileRedirect />)

      // Should still be 1 because useEffect deps don't change
      expect(mockRouter.replace).toHaveBeenCalledTimes(1)
    })

    it('should use the correct redirect path', () => {
      render(<ProfileRedirect />)

      expect(mockRouter.replace).toHaveBeenCalledWith('/profile/me')
      expect(mockRouter.replace).not.toHaveBeenCalledWith('/profile')
      expect(mockRouter.replace).not.toHaveBeenCalledWith('/me')
    })
  })

  // ========================================
  // Loading UI Tests
  // ========================================
  describe('Loading UI', () => {
    it('should render loading message', () => {
      render(<ProfileRedirect />)

      expect(screen.getByText('Redirecting to your profile...')).toBeInTheDocument()
    })

    it('should render loading spinner', () => {
      const { container } = render(<ProfileRedirect />)

      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('should apply correct classes to container', () => {
      const { container } = render(<ProfileRedirect />)

      const mainDiv = container.querySelector('.min-h-screen')
      expect(mainDiv).toBeInTheDocument()
      expect(mainDiv).toHaveClass('bg-background', 'flex', 'items-center', 'justify-center')
    })

    it('should apply correct classes to content wrapper', () => {
      const { container } = render(<ProfileRedirect />)

      const contentWrapper = container.querySelector('.text-center')
      expect(contentWrapper).toBeInTheDocument()
    })

    it('should apply correct classes to spinner', () => {
      const { container } = render(<ProfileRedirect />)

      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toHaveClass('rounded-full', 'h-12', 'w-12', 'border-b-2', 'border-primary', 'mx-auto')
    })

    it('should apply correct classes to loading text', () => {
      render(<ProfileRedirect />)

      const loadingText = screen.getByText('Redirecting to your profile...')
      expect(loadingText).toHaveClass('mt-4', 'text-muted-foreground')
      expect(loadingText.tagName).toBe('P')
    })
  })

  // ========================================
  // Structure Tests
  // ========================================
  describe('Component Structure', () => {
    it('should render all expected elements', () => {
      const { container } = render(<ProfileRedirect />)

      expect(container.querySelector('.min-h-screen')).toBeInTheDocument()
      expect(container.querySelector('.text-center')).toBeInTheDocument()
      expect(container.querySelector('.animate-spin')).toBeInTheDocument()
      expect(screen.getByText('Redirecting to your profile...')).toBeInTheDocument()
    })

    it('should have proper DOM hierarchy', () => {
      const { container } = render(<ProfileRedirect />)

      const mainDiv = container.querySelector('.min-h-screen')
      const contentWrapper = container.querySelector('.text-center')
      const spinner = container.querySelector('.animate-spin')
      const text = screen.getByText('Redirecting to your profile...')

      expect(mainDiv as HTMLElement).toContainElement(contentWrapper as HTMLElement)
      expect(contentWrapper as HTMLElement).toContainElement(spinner as HTMLElement)
      expect(contentWrapper).toContainElement(text)
    })

    it('should render spinner before text', () => {
      const { container } = render(<ProfileRedirect />)

      const contentWrapper = container.querySelector('.text-center')
      const children = Array.from(contentWrapper?.children || [])

      expect(children[0]).toHaveClass('animate-spin')
      expect(children[1].textContent).toBe('Redirecting to your profile...')
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should call router.replace even if router changes', () => {
      const { rerender } = render(<ProfileRedirect />)

      expect(mockRouter.replace).toHaveBeenCalledTimes(1)

      // Create a new router instance
      const newMockRouter = { replace: jest.fn() }
      mockUseRouter.mockReturnValue(newMockRouter as any)

      rerender(<ProfileRedirect />)

      // New router should be called
      expect(newMockRouter.replace).toHaveBeenCalledWith('/profile/me')
    })

    it('should work with different router implementations', () => {
      const alternateRouter = {
        replace: jest.fn(),
        push: jest.fn(),
        back: jest.fn(),
      }
      mockUseRouter.mockReturnValue(alternateRouter as any)

      render(<ProfileRedirect />)

      expect(alternateRouter.replace).toHaveBeenCalledWith('/profile/me')
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should immediately start redirect process on mount', async () => {
      render(<ProfileRedirect />)

      // Router replace should be called without waiting
      expect(mockRouter.replace).toHaveBeenCalled()

      await waitFor(() => {
        expect(mockRouter.replace).toHaveBeenCalledWith('/profile/me')
      })
    })

    it('should display loading UI while redirect is in progress', () => {
      render(<ProfileRedirect />)

      // Both loading UI and redirect should be present
      expect(screen.getByText('Redirecting to your profile...')).toBeInTheDocument()
      expect(mockRouter.replace).toHaveBeenCalledWith('/profile/me')
    })

    it('should maintain loading UI after redirect is called', () => {
      const { container } = render(<ProfileRedirect />)

      expect(mockRouter.replace).toHaveBeenCalled()

      // UI should still be present
      expect(screen.getByText('Redirecting to your profile...')).toBeInTheDocument()
      expect(container.querySelector('.animate-spin')).toBeInTheDocument()
    })
  })
})
