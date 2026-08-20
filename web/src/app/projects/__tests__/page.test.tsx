import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import ProjectsPage from '../page'
import { useRouter } from 'next/navigation'

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>

describe('ProjectsPage', () => {
  let mockRouter: { push: jest.Mock }

  beforeEach(() => {
    jest.clearAllMocks()
    mockRouter = {
      push: jest.fn(),
    }
    mockUseRouter.mockReturnValue(mockRouter as any)
  })

  // ========================================
  // Redirect Tests
  // ========================================
  describe('Redirect Behavior', () => {
    it('should redirect to /projects/search on mount', () => {
      render(<ProjectsPage />)

      expect(mockRouter.push).toHaveBeenCalledWith('/projects/search')
      expect(mockRouter.push).toHaveBeenCalledTimes(1)
    })

    it('should call router.push only once even with multiple renders', () => {
      const { rerender } = render(<ProjectsPage />)

      expect(mockRouter.push).toHaveBeenCalledTimes(1)

      rerender(<ProjectsPage />)

      // Should still be 1 because useEffect deps don't change
      expect(mockRouter.push).toHaveBeenCalledTimes(1)
    })

    it('should use the correct redirect path', () => {
      render(<ProjectsPage />)

      expect(mockRouter.push).toHaveBeenCalledWith('/projects/search')
      expect(mockRouter.push).not.toHaveBeenCalledWith('/projects')
      expect(mockRouter.push).not.toHaveBeenCalledWith('/search')
    })
  })

  // ========================================
  // Loading UI Tests
  // ========================================
  describe('Loading UI', () => {
    it('should render loading message', () => {
      render(<ProjectsPage />)

      expect(screen.getByText('Loading projects...')).toBeInTheDocument()
    })

    it('should render loading spinner', () => {
      const { container } = render(<ProjectsPage />)

      const spinner = container.querySelector('.loading-spinner')
      expect(spinner).toBeInTheDocument()
    })

    it('should apply correct classes to container', () => {
      const { container } = render(<ProjectsPage />)

      const mainDiv = container.querySelector('.min-h-screen')
      expect(mainDiv).toBeInTheDocument()
      expect(mainDiv).toHaveClass('flex', 'items-center', 'justify-center', 'bg-background')
    })

    it('should apply correct classes to content wrapper', () => {
      const { container } = render(<ProjectsPage />)

      const contentWrapper = container.querySelector('.text-center')
      expect(contentWrapper).toBeInTheDocument()
      expect(contentWrapper).toHaveClass('space-md', 'animate-fade-in')
    })

    it('should apply correct classes to spinner', () => {
      const { container } = render(<ProjectsPage />)

      const spinner = container.querySelector('.loading-spinner')
      expect(spinner).toHaveClass('mx-auto', 'animate-glow')
    })

    it('should apply correct classes to loading text', () => {
      render(<ProjectsPage />)

      const loadingText = screen.getByText('Loading projects...')
      expect(loadingText).toHaveClass('text-body', 'text-muted-foreground')
      expect(loadingText.tagName).toBe('P')
    })
  })

  // ========================================
  // Structure Tests
  // ========================================
  describe('Component Structure', () => {
    it('should render all expected elements', () => {
      const { container } = render(<ProjectsPage />)

      expect(container.querySelector('.min-h-screen')).toBeInTheDocument()
      expect(container.querySelector('.text-center')).toBeInTheDocument()
      expect(container.querySelector('.loading-spinner')).toBeInTheDocument()
      expect(screen.getByText('Loading projects...')).toBeInTheDocument()
    })

    it('should have proper DOM hierarchy', () => {
      const { container } = render(<ProjectsPage />)

      const mainDiv = container.querySelector('.min-h-screen')
      const contentWrapper = container.querySelector('.text-center')
      const spinner = container.querySelector('.loading-spinner')
      const text = screen.getByText('Loading projects...')

      expect(mainDiv as HTMLElement).toContainElement(contentWrapper as HTMLElement)
      expect(contentWrapper as HTMLElement).toContainElement(spinner as HTMLElement)
      expect(contentWrapper).toContainElement(text)
    })

    it('should render spinner before text', () => {
      const { container } = render(<ProjectsPage />)

      const contentWrapper = container.querySelector('.text-center')
      const children = Array.from(contentWrapper?.children || [])

      expect(children[0]).toHaveClass('loading-spinner')
      expect(children[1].textContent).toBe('Loading projects...')
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should call router.push even if router changes', () => {
      const { rerender } = render(<ProjectsPage />)

      expect(mockRouter.push).toHaveBeenCalledTimes(1)

      // Create a new router instance
      const newMockRouter = { push: jest.fn() }
      mockUseRouter.mockReturnValue(newMockRouter as any)

      rerender(<ProjectsPage />)

      // New router should be called
      expect(newMockRouter.push).toHaveBeenCalledWith('/projects/search')
    })

    it('should work with different router implementations', () => {
      const alternateRouter = {
        push: jest.fn(),
        replace: jest.fn(),
        back: jest.fn(),
      }
      mockUseRouter.mockReturnValue(alternateRouter as any)

      render(<ProjectsPage />)

      expect(alternateRouter.push).toHaveBeenCalledWith('/projects/search')
    })
  })

  // ========================================
  // Integration Tests
  // ========================================
  describe('Integration', () => {
    it('should immediately start redirect process on mount', async () => {
      render(<ProjectsPage />)

      // Router push should be called without waiting
      expect(mockRouter.push).toHaveBeenCalled()

      await waitFor(() => {
        expect(mockRouter.push).toHaveBeenCalledWith('/projects/search')
      })
    })

    it('should display loading UI while redirect is in progress', () => {
      render(<ProjectsPage />)

      // Both loading UI and redirect should be present
      expect(screen.getByText('Loading projects...')).toBeInTheDocument()
      expect(mockRouter.push).toHaveBeenCalledWith('/projects/search')
    })

    it('should maintain loading UI after redirect is called', () => {
      const { container } = render(<ProjectsPage />)

      expect(mockRouter.push).toHaveBeenCalled()

      // UI should still be present
      expect(screen.getByText('Loading projects...')).toBeInTheDocument()
      expect(container.querySelector('.loading-spinner')).toBeInTheDocument()
    })
  })
})
