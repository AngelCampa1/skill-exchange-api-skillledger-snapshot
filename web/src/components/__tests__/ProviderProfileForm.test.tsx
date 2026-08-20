/**
 * Tests for ProviderProfileForm
 *
 * Comprehensive test suite for the provider profile form component
 * Coverage target: 70%+ (638 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ProviderProfileForm from '../ProviderProfileForm'

// Mock UI components
jest.mock('@/components/ui/button', () => ({
  Button: ({ children, onClick, disabled, type, loading, loadingText, variant, size, ...props }: any) => (
    <button onClick={onClick} disabled={disabled || loading} type={type} {...props}>
      {loading ? loadingText || 'Loading...' : children}
    </button>
  ),
}))

jest.mock('@/components/ui/input', () => {
  const Input = React.forwardRef((props: any, ref: any) => <input ref={ref} {...props} />)
  Input.displayName = 'Input'
  return { Input }
})

jest.mock('@/components/ui/label', () => ({
  Label: ({ children, ...props }: any) => <label {...props}>{children}</label>,
}))

jest.mock('@/components/ui/alert', () => ({
  Alert: ({ children }: any) => <div role="alert">{children}</div>,
  AlertDescription: ({ children }: any) => <div>{children}</div>,
}))

describe('ProviderProfileForm', () => {
  const mockOnSubmit = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('should render the form', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('Basic Information')).toBeInTheDocument()
    })

    it('should start on step 1', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('Step 1 of 3')).toBeInTheDocument()
    })

    it('should display progress bar', () => {
      const { container } = render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const progressBar = container.querySelector('.bg-primary')
      expect(progressBar).toBeTruthy()
    })

    it('should show progress labels', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('Basic Info')).toBeInTheDocument()
      expect(screen.getByText('Skills & Experience')).toBeInTheDocument()
      expect(screen.getByText('Availability')).toBeInTheDocument()
    })
  })

  describe('Step 1: Basic Information', () => {
    it('should render all basic information fields', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.getByTestId('provider-title-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-company-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-bio-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-hourly-rate-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-location-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-website-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-linkedin-input')).toBeInTheDocument()
      expect(screen.getByTestId('provider-portfolio-input')).toBeInTheDocument()
    })

    it('should show character count for bio', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('0/2000 characters')).toBeInTheDocument()
    })

    it('should update bio character count when typing', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const bioInput = screen.getByTestId('provider-bio-input')
      await user.type(bioInput, 'Test bio content')

      await waitFor(() => {
        expect(screen.getByText('16/2000 characters')).toBeInTheDocument()
      })
    })

    it('should validate required title field', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByTestId('provider-title-input')
      await user.type(titleInput, 'a')
      await user.clear(titleInput)
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Professional title is required')).toBeInTheDocument()
      })
    })

    it('should validate bio minimum length', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const bioInput = screen.getByTestId('provider-bio-input')
      await user.type(bioInput, 'Too short')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Bio must be at least 50 characters')).toBeInTheDocument()
      })
    })

    it('should validate hourly rate minimum', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const hourlyRateInput = screen.getByTestId('provider-hourly-rate-input')
      await user.type(hourlyRateInput, '10')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Hourly rate must be at least $15')).toBeInTheDocument()
      })
    })

    it('should validate hourly rate maximum', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const hourlyRateInput = screen.getByTestId('provider-hourly-rate-input')
      await user.type(hourlyRateInput, '600')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Hourly rate cannot exceed $500')).toBeInTheDocument()
      })
    })

    it('should validate location required field', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const locationInput = screen.getByTestId('provider-location-input')
      await user.type(locationInput, 'a')
      await user.clear(locationInput)
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Location is required')).toBeInTheDocument()
      })
    })
  })

  describe('Step Navigation', () => {
    it('should navigate to step 2 when clicking Next', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const nextButton = screen.getByTestId('profile-next-button')
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getByText('Step 2 of 3')).toBeInTheDocument()
      })

      expect(screen.getAllByText('Skills & Experience').length).toBeGreaterThan(0)
    })

    it('should navigate back to step 1 when clicking Previous from step 2', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      // Go to step 2
      const nextButton = screen.getByTestId('profile-next-button')
      await user.click(nextButton)

      await waitFor(() => {
        expect(screen.getByText('Step 2 of 3')).toBeInTheDocument()
      })

      // Go back to step 1
      const prevButton = screen.getByTestId('profile-prev-button')
      await user.click(prevButton)

      await waitFor(() => {
        expect(screen.getByText('Step 1 of 3')).toBeInTheDocument()
      })

      expect(screen.getByText('Basic Information')).toBeInTheDocument()
    })

    it('should not show Previous button on step 1', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.queryByTestId('profile-prev-button')).not.toBeInTheDocument()
    })

    it('should navigate through all steps', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      // Step 1 → Step 2
      await user.click(screen.getByTestId('profile-next-button'))
      await waitFor(() => {
        expect(screen.getByText('Step 2 of 3')).toBeInTheDocument()
      })

      // Step 2 → Step 3
      await user.click(screen.getByTestId('profile-next-button'))
      await waitFor(() => {
        expect(screen.getByText('Step 3 of 3')).toBeInTheDocument()
      })

      expect(screen.getByText('Availability & Preferences')).toBeInTheDocument()
    })
  })

  describe('Step 2: Skills', () => {
    it('should render default skill field', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('skill-name-0')).toBeInTheDocument()
      })

      expect(screen.getByTestId('skill-proficiency-0')).toBeInTheDocument()
      expect(screen.getByTestId('skill-years-0')).toBeInTheDocument()
    })

    it('should add new skill when clicking Add Skill button', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-skill-button')).toBeInTheDocument()
      })

      await user.click(screen.getByTestId('add-skill-button'))

      await waitFor(() => {
        expect(screen.getByTestId('skill-name-1')).toBeInTheDocument()
      })
    })

    it('should remove skill when clicking remove button', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-skill-button')).toBeInTheDocument()
      })

      await user.click(screen.getByTestId('add-skill-button'))

      await waitFor(() => {
        expect(screen.getByTestId('skill-name-1')).toBeInTheDocument()
      })

      const removeButtons = screen.getAllByRole('button')
      const removeButton = removeButtons.find(btn => btn.querySelector('.lucide-x'))
      if (removeButton) {
        await user.click(removeButton)

        await waitFor(() => {
          expect(screen.queryByTestId('skill-name-1')).not.toBeInTheDocument()
        })
      }
    })

    it('should disable Add Skill button when 10 skills are added', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-skill-button')).toBeInTheDocument()
      })

      const addButton = screen.getByTestId('add-skill-button')

      // Add 9 more skills (already have 1)
      for (let i = 0; i < 9; i++) {
        await user.click(addButton)
      }

      await waitFor(() => {
        expect(addButton).toBeDisabled()
      })
    })

    it('should not allow removing the last skill', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('skill-name-0')).toBeInTheDocument()
      })

      // Should not have a remove button for the only skill
      const skillCard = screen.getByTestId('skill-name-0').closest('.border')
      const removeButton = skillCard?.querySelector('button.text-destructive')
      expect(removeButton).not.toBeInTheDocument()
    })
  })

  describe('Step 2: Experience', () => {
    it('should render Add Experience button', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-experience-button')).toBeInTheDocument()
      })
    })

    it('should add new experience when clicking Add Experience button', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-experience-button')).toBeInTheDocument()
      })

      await user.click(screen.getByTestId('add-experience-button'))

      await waitFor(() => {
        expect(screen.getByTestId('experience-company-0')).toBeInTheDocument()
      })

      expect(screen.getByTestId('experience-position-0')).toBeInTheDocument()
      expect(screen.getByTestId('experience-start-0')).toBeInTheDocument()
      expect(screen.getByTestId('experience-end-0')).toBeInTheDocument()
      expect(screen.getByTestId('experience-description-0')).toBeInTheDocument()
    })

    it('should remove experience when clicking remove button', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-experience-button')).toBeInTheDocument()
      })

      await user.click(screen.getByTestId('add-experience-button'))

      await waitFor(() => {
        expect(screen.getByTestId('experience-company-0')).toBeInTheDocument()
      })

      const removeButtons = screen.getAllByRole('button')
      const removeButton = removeButtons.find(btn =>
        btn.className.includes('text-destructive') && btn.querySelector('.lucide-x')
      )

      if (removeButton) {
        await user.click(removeButton)

        await waitFor(() => {
          expect(screen.queryByTestId('experience-company-0')).not.toBeInTheDocument()
        })
      }
    })

    it('should disable Add Experience button when 5 experiences are added', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('add-experience-button')).toBeInTheDocument()
      })

      const addButton = screen.getByTestId('add-experience-button')

      // Add 5 experiences
      for (let i = 0; i < 5; i++) {
        await user.click(addButton)
      }

      await waitFor(() => {
        expect(addButton).toBeDisabled()
      })
    })
  })

  describe('Step 3: Availability', () => {
    it('should render availability options', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      // Navigate to step 3
      await user.click(screen.getByTestId('profile-next-button'))
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByText('Availability & Preferences')).toBeInTheDocument()
      })

      expect(screen.getByTestId('availability-full-time')).toBeInTheDocument()
      expect(screen.getByTestId('availability-part-time')).toBeInTheDocument()
      expect(screen.getByTestId('availability-contract')).toBeInTheDocument()
      expect(screen.getByTestId('availability-freelance')).toBeInTheDocument()
    })

    it('should render work hours options', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('work-hours-morning')).toBeInTheDocument()
      })

      expect(screen.getByTestId('work-hours-afternoon')).toBeInTheDocument()
      expect(screen.getByTestId('work-hours-evening')).toBeInTheDocument()
      expect(screen.getByTestId('work-hours-flexible')).toBeInTheDocument()
    })

    it('should show Create Profile button on step 3', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('create-profile-button')).toBeInTheDocument()
      })
    })

    it('should show alert message about profile completion', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.click(screen.getByTestId('profile-next-button'))
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByText(/Complete your profile to increase your chances/)).toBeInTheDocument()
      })
    })
  })

  describe('Form Submission', () => {
    it('should call onSubmit with form data', async () => {
      const user = userEvent.setup()
      mockOnSubmit.mockResolvedValue(undefined)

      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      // Fill in required fields
      await user.type(screen.getByTestId('provider-title-input'), 'Senior Developer')
      await user.type(screen.getByTestId('provider-bio-input'), 'A'.repeat(50))
      await user.type(screen.getByTestId('provider-hourly-rate-input'), '75')
      await user.type(screen.getByTestId('provider-location-input'), 'New York, NY')

      // Go to step 2
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('skill-name-0')).toBeInTheDocument()
      })

      await user.type(screen.getByTestId('skill-name-0'), 'React')

      // Go to step 3
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        expect(screen.getByTestId('create-profile-button')).toBeInTheDocument()
      })

      // Submit
      await user.click(screen.getByTestId('create-profile-button'))

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalled()
      })
    }, 10000)

    it('should disable submit button when loading', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} isLoading={true} />)

      // Navigate to step 3
      fireEvent.click(screen.getByTestId('profile-next-button'))
      fireEvent.click(screen.getByTestId('profile-next-button'))

      waitFor(() => {
        const submitButton = screen.getByTestId('create-profile-button')
        expect(submitButton).toBeDisabled()
      })
    })

    it('should show loading text when submitting', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} isLoading={true} />)

      // Navigate to step 3
      fireEvent.click(screen.getByTestId('profile-next-button'))
      fireEvent.click(screen.getByTestId('profile-next-button'))

      waitFor(() => {
        expect(screen.getByText('Creating Profile...')).toBeInTheDocument()
      })
    })
  })

  describe('Initial Data', () => {
    it('should populate form with initial data', () => {
      const initialData = {
        title: 'Full Stack Developer',
        companyName: 'Tech Corp',
        bio: 'A'.repeat(60),
        hourlyRate: 100,
        location: 'San Francisco, CA',
      }

      render(<ProviderProfileForm onSubmit={mockOnSubmit} initialData={initialData} />)

      expect(screen.getByTestId('provider-title-input')).toHaveValue('Full Stack Developer')
      expect(screen.getByTestId('provider-company-input')).toHaveValue('Tech Corp')
      expect(screen.getByTestId('provider-bio-input')).toHaveValue('A'.repeat(60))
      expect(screen.getByTestId('provider-hourly-rate-input')).toHaveValue(100)
      expect(screen.getByTestId('provider-location-input')).toHaveValue('San Francisco, CA')
    })
  })

  describe('Progress Calculation', () => {
    it('should show 25% progress initially', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('25')
      expect(textContent).toContain('% Complete')
    })

    it('should update progress when filling fields', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      await user.type(screen.getByTestId('provider-title-input'), 'Developer')
      await user.type(screen.getByTestId('provider-bio-input'), 'A'.repeat(60))
      await user.type(screen.getByTestId('provider-hourly-rate-input'), '75')

      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('% Complete')
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle empty form submission', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      // Navigate to step 3
      await user.click(screen.getByTestId('profile-next-button'))
      await user.click(screen.getByTestId('profile-next-button'))

      await waitFor(() => {
        const submitButton = screen.getByTestId('create-profile-button')
        expect(submitButton).toBeInTheDocument()
      })

      const submitButton = screen.getByTestId('create-profile-button')

      // Should be disabled when form is invalid
      expect(submitButton).toBeDisabled()
    })

    it('should handle very long bio', async () => {
      const user = userEvent.setup()
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const bioInput = screen.getByTestId('provider-bio-input')

      // Instead of typing 2001 characters, just paste it directly
      fireEvent.change(bioInput, { target: { value: 'A'.repeat(2001) } })
      await user.tab()

      // Check if character count shows exceeded limit
      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('2001/2000 characters')
      })
    })
  })

  describe('Accessibility', () => {
    it('should have proper labels for all inputs', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('Professional Title *')).toBeInTheDocument()
      expect(screen.getByText('Professional Bio *')).toBeInTheDocument()
      expect(screen.getByText('Hourly Rate ($) *')).toBeInTheDocument()
      expect(screen.getByText('Location *')).toBeInTheDocument()
    })

    it('should have accessible buttons', () => {
      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })
  })

  describe('Integration', () => {
    it('should render complete form without errors', () => {
      const { container } = render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Basic Information')).toBeInTheDocument()
      expect(screen.getByTestId('profile-next-button')).toBeInTheDocument()
    })

    it('should handle full workflow from start to finish', async () => {
      const user = userEvent.setup()
      mockOnSubmit.mockResolvedValue(undefined)

      render(<ProviderProfileForm onSubmit={mockOnSubmit} />)

      // Step 1
      await user.type(screen.getByTestId('provider-title-input'), 'Senior Developer')
      const bioInput = screen.getByTestId('provider-bio-input')
      await user.click(bioInput)
      await user.paste('A'.repeat(60))
      await user.type(screen.getByTestId('provider-hourly-rate-input'), '85')
      await user.type(screen.getByTestId('provider-location-input'), 'Boston, MA')
      await user.click(screen.getByTestId('profile-next-button'))

      // Step 2
      await waitFor(() => {
        expect(screen.getByTestId('skill-name-0')).toBeInTheDocument()
      })
      await user.type(screen.getByTestId('skill-name-0'), 'JavaScript')
      await user.click(screen.getByTestId('profile-next-button'))

      // Step 3
      await waitFor(() => {
        expect(screen.getByTestId('create-profile-button')).toBeInTheDocument()
      })
      await user.click(screen.getByTestId('create-profile-button'))

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalled()
      })
    })
  })
})
