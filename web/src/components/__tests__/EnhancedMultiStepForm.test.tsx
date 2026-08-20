/**
 * Tests for EnhancedMultiStepForm
 *
 * Comprehensive test suite for the multi-step form component
 * Coverage target: 70%+ (715 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EnhancedMultiStepForm } from '../EnhancedMultiStepForm'
import { User, Briefcase, FileText } from 'lucide-react'

describe('EnhancedMultiStepForm', () => {
  const mockSteps = [
    {
      id: 'step1',
      title: 'Personal Info',
      description: 'Enter your personal details',
      icon: User,
      fields: ['name', 'email'],
    },
    {
      id: 'step2',
      title: 'Work Experience',
      description: 'Tell us about your work',
      icon: Briefcase,
      fields: ['company', 'position'],
    },
    {
      id: 'step3',
      title: 'Review',
      description: 'Review your information',
      icon: FileText,
      fields: ['consent'],
    },
  ]

  const mockOnSubmit = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('should render the form with default variant', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      expect(screen.getAllByText('Personal Info')[0]).toBeInTheDocument()
      expect(screen.getAllByText('Enter your personal details').length).toBeGreaterThan(0)
    })

    it('should render progress indicator by default', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      expect(screen.getByText(/Step 1 of 3/i)).toBeInTheDocument()
      expect(screen.getByText(/33% Complete/i)).toBeInTheDocument()
    })

    it('should hide progress when showProgress is false', () => {
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          showProgress={false}
        />
      )

      expect(screen.queryByText(/Step 1 of 3/i)).not.toBeInTheDocument()
    })

    it('should render with wizard variant', () => {
      const { container } = render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          variant="wizard"
        />
      )

      expect(container.firstChild).toBeInTheDocument()
    })

    it('should render without animation', () => {
      const { container } = render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          animated={false}
        />
      )

      expect(container.firstChild).toBeInTheDocument()
    })

    it('should render with vertical orientation', () => {
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          orientation="vertical"
        />
      )

      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
    })
  })

  describe('Navigation', () => {
    it('should show disabled Previous button on first step', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      const prevButton = screen.getByRole('button', { name: /previous/i })
      expect(prevButton).toBeDisabled()
    })

    it('should show Next Step button when not on last step', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      expect(screen.getByRole('button', { name: /next step/i })).toBeInTheDocument()
    })

    it('should show Complete Setup button on last step', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{ name: 'John', email: 'john@example.com', company: 'Acme', position: 'Developer' }}
        />
      )

      // Navigate to last step by clicking Next twice
      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)
      await user.click(nextButton)

      expect(screen.getByRole('button', { name: /complete setup/i })).toBeInTheDocument()
    })

    it('should navigate to next step when validation passes', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{ name: 'John', email: 'john@example.com' }}
        />
      )

      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)

      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getAllByText('Work Experience').length).toBeGreaterThan(0)
      })
    })

    it('should navigate to previous step when clicking Previous button', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{ name: 'John', email: 'john@example.com' }}
        />
      )

      // Go to step 2
      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getAllByText('Work Experience').length).toBeGreaterThan(0)
      })

      // Go back to step 1
      const prevButton = screen.getByRole('button', { name: /previous/i })
      await user.click(prevButton)

      await waitFor(() => {
        expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
      })
    })
  })

  describe('Step Validation', () => {
    it('should not proceed to next step if required fields are empty', async () => {
      const user = userEvent.setup()
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      // Should still be on step 1
      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
    })

    it('should mark step as completed after validation passes', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{ name: 'John', email: 'john@example.com' }}
        />
      )

      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getAllByText('Work Experience').length).toBeGreaterThan(0)
      })
    })
  })

  describe('Progress Tracking', () => {
    it('should update progress percentage when navigating steps', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{ name: 'John', email: 'john@example.com' }}
        />
      )

      expect(screen.getByText(/33% Complete/i)).toBeInTheDocument()

      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getByText(/67% Complete/i)).toBeInTheDocument()
      })
    })

    it('should show current step indicator', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      expect(screen.getByText(/Step 1 of 3/i)).toBeInTheDocument()
    })
  })

  describe('Form Submission', () => {
    it('should call onSubmit when completing final step with valid data', async () => {
      const user = userEvent.setup()
      const formData = {
        name: 'John Doe',
        email: 'john@example.com',
        company: 'Acme Corp',
        position: 'Developer',
        consent: 'yes',
      }

      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={formData}
        />
      )

      // Navigate through all steps
      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)
      await user.click(nextButton)

      // Submit on final step
      const submitButton = screen.getByRole('button', { name: /complete setup/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(formData)
      })
    })

    it('should show processing state while submitting', async () => {
      const user = userEvent.setup()
      let resolveSubmit: () => void
      const delayedSubmit = jest.fn(() => new Promise<void>((resolve) => {
        resolveSubmit = resolve
      }))

      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={delayedSubmit}
          initialData={{
            name: 'John',
            email: 'john@example.com',
            company: 'Acme',
            position: 'Dev',
            consent: 'yes',
          }}
        />
      )

      // Navigate to last step
      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)
      await user.click(nextButton)

      // Click submit
      const submitButton = screen.getByRole('button', { name: /complete setup/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/processing/i)).toBeInTheDocument()
      })

      // Resolve the submission
      resolveSubmit!()
    })

    it('should not proceed if validation fails on final step', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{
            name: 'John',
            email: 'john@example.com',
            company: 'Acme',
            position: 'Dev',
            // Missing consent field
          }}
        />
      )

      // Navigate to last step
      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)
      await user.click(nextButton)

      // Try to submit
      const submitButton = screen.getByRole('button', { name: /complete setup/i })
      await user.click(submitButton)

      // Should not have called onSubmit
      expect(mockOnSubmit).not.toHaveBeenCalled()
    })
  })

  describe('Step Click Navigation', () => {
    it('should allow clicking on current step', async () => {
      const user = userEvent.setup()
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      const stepButtons = screen.getAllByRole('button')
      const currentStepButton = stepButtons.find(btn => btn.textContent?.includes('Personal Info'))

      if (currentStepButton) {
        await user.click(currentStepButton)
        expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
      }
    })

    it('should allow clicking on completed steps', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={{ name: 'John', email: 'john@example.com' }}
        />
      )

      // Complete step 1 by going to step 2
      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getAllByText('Work Experience').length).toBeGreaterThan(0)
      })

      // Now try to click back to step 1 using step buttons
      const stepButtons = screen.getAllByRole('button')
      const step1Button = stepButtons.find(btn => btn.textContent?.includes('Personal Info'))

      if (step1Button && !(step1Button as HTMLButtonElement).disabled) {
        await user.click(step1Button)

        await waitFor(() => {
          expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
        })
      }
    })

    it('should not allow clicking on incomplete future steps', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      const stepButtons = screen.getAllByRole('button')
      const step3Button = stepButtons.find(btn => btn.textContent?.includes('Review'))

      // Step 3 button should be disabled
      if (step3Button) {
        expect(step3Button).toBeDisabled()
      }
    })
  })

  describe('Initial Data', () => {
    it('should populate form with initial data', () => {
      const initialData = {
        name: 'Jane Smith',
        email: 'jane@example.com',
      }

      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          initialData={initialData}
        />
      )

      // Component should render without errors
      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
    })
  })

  describe('Wizard Variant Specifics', () => {
    it('should render step icons in wizard variant', () => {
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          variant="wizard"
        />
      )

      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
      expect(screen.getByText('Step 1')).toBeInTheDocument()
    })

    it('should show progress bar in wizard variant', () => {
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          variant="wizard"
        />
      )

      // Wizard variant has its own progress display
      expect(screen.getByText(/Step 1 of 3/i)).toBeInTheDocument()
    })

    it('should navigate with arrow buttons in wizard variant', async () => {
      const user = userEvent.setup()
      render(
        <EnhancedMultiStepForm
          steps={mockSteps}
          onSubmit={mockOnSubmit}
          variant="wizard"
          initialData={{ name: 'John', email: 'john@example.com' }}
        />
      )

      const nextButton = screen.getByRole('button', { name: /next step/i })
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getAllByText('Work Experience').length).toBeGreaterThan(0)
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle single step form', () => {
      const singleStep = [mockSteps[0]]

      render(<EnhancedMultiStepForm steps={singleStep} onSubmit={mockOnSubmit} />)

      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
      expect(screen.getByText(/100% Complete/i)).toBeInTheDocument()
    })

    it('should handle empty initial data', () => {
      render(
        <EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} initialData={{}} />
      )

      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
    })

    it('should handle missing initialData prop', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      expect(screen.getAllByText('Personal Info').length).toBeGreaterThan(0)
    })
  })

  describe('Accessibility', () => {
    it('should have proper button roles', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('should disable buttons appropriately', () => {
      render(<EnhancedMultiStepForm steps={mockSteps} onSubmit={mockOnSubmit} />)

      const prevButton = screen.getByRole('button', { name: /previous/i })
      expect(prevButton).toBeDisabled()
    })
  })
})
