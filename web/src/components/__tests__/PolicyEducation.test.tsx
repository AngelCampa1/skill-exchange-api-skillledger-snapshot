/**
 * Tests for PolicyEducation
 *
 * Comprehensive test suite for the policy education component
 * Coverage target: 80%+ (406 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PolicyEducation from '../PolicyEducation'

describe('PolicyEducation', () => {
  describe('Initial Rendering', () => {
    it('should render the component with title', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('Platform Policy Education')).toBeInTheDocument()
    })

    it('should render all policy sections in navigation', () => {
      render(<PolicyEducation />)

      expect(screen.getAllByText('Fake Reviews & Gaming').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Content Originality').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Identity & Authenticity').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Network Integrity').length).toBeGreaterThan(0)
    })

    it('should show progress bar with 0 of 4 completed initially', () => {
      render(<PolicyEducation />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('0 of 4 completed')
    })

    it('should select first policy by default', () => {
      render(<PolicyEducation />)

      // First policy should be highlighted
      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Fake Reviews & Gaming')
      expect(textContent).toContain('Understanding what constitutes review manipulation and gaming behavior')
    })

    it('should render Quick Reference section', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('Quick Reference')).toBeInTheDocument()
      expect(screen.getByText('Be Authentic')).toBeInTheDocument()
      expect(screen.getByText('Earn Trust')).toBeInTheDocument()
      expect(screen.getByText('Create Original')).toBeInTheDocument()
      expect(screen.getByText('Follow Rules')).toBeInTheDocument()
    })
  })

  describe('Policy Navigation', () => {
    it('should switch to selected policy when clicked', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const contentOriginalityButton = screen.getByText('Content Originality')
      await user.click(contentOriginalityButton)

      await waitFor(() => {
        expect(screen.getByText('Guidelines for original content and proper attribution')).toBeInTheDocument()
      })
    })

    it('should highlight selected policy in navigation', async () => {
      const user = userEvent.setup()
      const { container } = render(<PolicyEducation />)

      const identityButton = screen.getByText('Identity & Authenticity')
      await user.click(identityButton)

      await waitFor(() => {
        const buttons = container.querySelectorAll('button')
        const selectedButton = Array.from(buttons).find(
          (btn) => btn.textContent?.includes('Identity & Authenticity') && btn.className.includes('bg-primary')
        )
        expect(selectedButton).toBeTruthy()
      })
    })

    it('should render all policy navigation buttons', () => {
      render(<PolicyEducation />)

      expect(screen.getAllByText('Fake Reviews & Gaming').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Content Originality').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Identity & Authenticity').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Network Integrity').length).toBeGreaterThan(0)
    })
  })

  describe('Policy Content Display', () => {
    it('should display policy overview', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('Overview')).toBeInTheDocument()
    })

    it('should display Good Practices section', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('✅ Good Practices')).toBeInTheDocument()
    })

    it('should display Violations section', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('❌ Violations')).toBeInTheDocument()
    })

    it('should display Enforcement & Penalties section', () => {
      render(<PolicyEducation />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Enforcement & Penalties')
    })

    it('should render good practices examples', () => {
      render(<PolicyEducation />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Leaving honest feedback based on actual project experience')
    })

    it('should render violation examples', () => {
      render(<PolicyEducation />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Asking friends to leave positive reviews without working with you')
    })

    it('should render penalties', () => {
      render(<PolicyEducation />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('First offense: Warning and review removal')
    })
  })

  describe('Mark as Completed', () => {
    it('should show Mark Complete button initially', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('Mark Complete')).toBeInTheDocument()
    })

    it('should mark section as completed when button clicked', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const markCompleteButton = screen.getByText('Mark Complete')
      await user.click(markCompleteButton)

      await waitFor(() => {
        expect(screen.getByText('Completed ✓')).toBeInTheDocument()
      })
    })

    it('should update progress count when section completed', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const markCompleteButton = screen.getByText('Mark Complete')
      await user.click(markCompleteButton)

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('1 of 4 completed')
      })
    })

    it('should update progress bar width when section completed', async () => {
      const user = userEvent.setup()
      const { container } = render(<PolicyEducation />)

      const markCompleteButton = screen.getByText('Mark Complete')
      await user.click(markCompleteButton)

      await waitFor(() => {
        const progressBar = container.querySelector('.bg-primary.h-2')
        const width = progressBar?.getAttribute('style')
        expect(width).toContain('25%') // 1 of 4 is 25%
      })
    })

    it('should disable Mark Complete button after completion', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const markCompleteButton = screen.getByText('Mark Complete')
      await user.click(markCompleteButton)

      await waitFor(() => {
        const completedButton = screen.getByText('Completed ✓')
        expect(completedButton).toBeDisabled()
      })
    })

    it('should show checkmark in navigation for completed sections', async () => {
      const user = userEvent.setup()
      const { container } = render(<PolicyEducation />)

      const markCompleteButton = screen.getByText('Mark Complete')
      await user.click(markCompleteButton)

      await waitFor(() => {
        const textContent = container.textContent || ''
        expect(textContent).toContain('✓')
      })
    })

    it('should show completion message when all sections completed', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      // Mark all 4 sections as completed
      const policies = [
        'Fake Reviews & Gaming',
        'Content Originality',
        'Identity & Authenticity',
        'Network Integrity',
      ]

      for (const policy of policies) {
        const policyButtons = screen.getAllByText(policy)
        // Click the navigation button (first occurrence)
        await user.click(policyButtons[0])
        const markButton = screen.queryByText('Mark Complete')
        if (markButton) {
          await user.click(markButton)
        }
      }

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('Congratulations!')
        expect(textContent).toContain('4 of 4 completed')
      })
    })
  })

  describe('Progress Tracking', () => {
    it('should show 0% progress initially', () => {
      const { container } = render(<PolicyEducation />)

      const progressBar = container.querySelector('.bg-primary.h-2')
      expect(progressBar).toHaveStyle({ width: '0%' })
    })

    it('should calculate correct progress percentage', async () => {
      const user = userEvent.setup()
      const { container } = render(<PolicyEducation />)

      // Complete 2 out of 4 sections
      const policies = ['Fake Reviews & Gaming', 'Content Originality']

      for (const policy of policies) {
        const policyButtons = screen.getAllByText(policy)
        await user.click(policyButtons[0])
        const markButton = screen.getByText('Mark Complete')
        await user.click(markButton)
      }

      await waitFor(() => {
        const progressBar = container.querySelector('.bg-primary.h-2')
        const width = progressBar?.getAttribute('style')
        expect(width).toContain('50%')
      })
    })

    it('should persist completed state when navigating between sections', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      // Complete first section
      const markCompleteButton = screen.getByText('Mark Complete')
      await user.click(markCompleteButton)

      // Navigate to second section
      const contentOriginalityButton = screen.getByText('Content Originality')
      await user.click(contentOriginalityButton)

      // Navigate back to first section
      const fakeReviewsButton = screen.getByText('Fake Reviews & Gaming')
      await user.click(fakeReviewsButton)

      // Should still show as completed
      await waitFor(() => {
        expect(screen.getByText('Completed ✓')).toBeInTheDocument()
      })
    })
  })

  describe('Content for Each Policy', () => {
    it('should render Content Originality policy details', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const contentOriginalityButton = screen.getByText('Content Originality')
      await user.click(contentOriginalityButton)

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('Guidelines for original content and proper attribution')
      })
    })

    it('should render Identity & Authenticity policy details', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const identityButton = screen.getByText('Identity & Authenticity')
      await user.click(identityButton)

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('Requirements for truthful identity and credential representation')
      })
    })

    it('should render Network Integrity policy details', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const networkButton = screen.getByText('Network Integrity')
      await user.click(networkButton)

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('Preventing coordinated manipulation and network abuse')
      })
    })
  })

  describe('UI Elements', () => {
    it('should render policy icons', () => {
      const { container } = render(<PolicyEducation />)

      // Check for emoji icons in the navigation
      const textContent = container.textContent || ''
      expect(textContent).toContain('🎭')
      expect(textContent).toContain('📝')
      expect(textContent).toContain('🆔')
    })

    it('should render quick reference cards with icons', () => {
      render(<PolicyEducation />)

      expect(screen.getByText('Use real identity and honest skills')).toBeInTheDocument()
      expect(screen.getByText('Build genuine professional relationships')).toBeInTheDocument()
      expect(screen.getByText('Write unique content and reviews')).toBeInTheDocument()
      expect(screen.getByText('Respect platform policies and community')).toBeInTheDocument()
    })

    it('should show completion message styling', async () => {
      const user = userEvent.setup()
      const { container } = render(<PolicyEducation />)

      // Complete all sections
      const policies = [
        'Fake Reviews & Gaming',
        'Content Originality',
        'Identity & Authenticity',
        'Network Integrity',
      ]

      for (const policy of policies) {
        const policyButtons = screen.getAllByText(policy)
        await user.click(policyButtons[0])
        const markButton = screen.queryByText('Mark Complete')
        if (markButton) {
          await user.click(markButton)
        }
      }

      await waitFor(() => {
        const successDiv = container.querySelector('.bg-success\\/10')
        expect(successDiv).toBeTruthy()
      })
    })
  })

  describe('Accessibility', () => {
    it('should have accessible navigation buttons', () => {
      render(<PolicyEducation />)

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('should have proper button states', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const markCompleteButton = screen.getByText('Mark Complete')
      expect(markCompleteButton).not.toBeDisabled()

      await user.click(markCompleteButton)

      await waitFor(() => {
        const completedButton = screen.getByText('Completed ✓')
        expect(completedButton).toBeDisabled()
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle rapid policy switching', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      const policies = ['Content Originality', 'Identity & Authenticity', 'Fake Reviews & Gaming']

      for (const policy of policies) {
        const policyButton = screen.getByText(policy)
        await user.click(policyButton)
      }

      // Should end on the last clicked policy
      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('Understanding what constitutes review manipulation and gaming behavior')
      })
    })

    it('should maintain state consistency after multiple completions', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      // Complete first section
      const markButton1 = screen.getByText('Mark Complete')
      await user.click(markButton1)

      // Switch to another section
      const contentButton = screen.getByText('Content Originality')
      await user.click(contentButton)

      // Complete second section
      const markButton2 = screen.getByText('Mark Complete')
      await user.click(markButton2)

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('2 of 4 completed')
      })
    })
  })

  describe('Integration', () => {
    it('should render complete component without errors', () => {
      const { container } = render(<PolicyEducation />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Platform Policy Education')).toBeInTheDocument()
      expect(screen.getAllByText('Fake Reviews & Gaming').length).toBeGreaterThan(0)
      expect(screen.getByText('Quick Reference')).toBeInTheDocument()
    })

    it('should handle full user flow', async () => {
      const user = userEvent.setup()
      render(<PolicyEducation />)

      // Navigate to second policy
      const contentButton = screen.getByText('Content Originality')
      await user.click(contentButton)

      // Complete it
      await waitFor(async () => {
        const markButton = screen.getByText('Mark Complete')
        await user.click(markButton)
      })

      // Navigate to third policy
      const identityButton = screen.getByText('Identity & Authenticity')
      await user.click(identityButton)

      // Complete it
      await waitFor(async () => {
        const markButton = screen.getByText('Mark Complete')
        await user.click(markButton)
      })

      // Check progress
      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('2 of 4 completed')
      })
    })
  })
})
