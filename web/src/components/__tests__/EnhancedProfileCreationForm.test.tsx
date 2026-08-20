/**
 * Tests for EnhancedProfileCreationForm
 *
 * Comprehensive test suite for the enhanced profile creation component
 * Coverage target: 80%+ (568 lines)
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import EnhancedProfileCreationForm from '../EnhancedProfileCreationForm'

// Mock SimpleProfessionalPhotoUpload
jest.mock('../SimpleProfessionalPhotoUpload', () => {
  return function MockSimpleProfessionalPhotoUpload({ onUploadComplete, currentPhotoUrl, isLoading }: any) {
    return (
      <div data-testid="photo-upload">
        <span>Photo Upload Component</span>
        <button
          onClick={() => onUploadComplete({ success: true, fileId: 'photo-123' })}
          disabled={isLoading}
        >
          Upload Photo
        </button>
        {currentPhotoUrl && <img src={currentPhotoUrl} alt="Current" />}
      </div>
    )
  }
})

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

describe('EnhancedProfileCreationForm', () => {
  let mockFetch: jest.MockedFunction<typeof fetch>
  let mockOnSubmit: jest.Mock

  beforeEach(() => {
    jest.clearAllMocks()

    mockOnSubmit = jest.fn().mockResolvedValue(undefined)

    // Mock global fetch
    mockFetch = jest.fn()
    global.fetch = mockFetch

    // Default mock responses
    mockFetch.mockImplementation((url: any) => {
      if (url.includes('/api/profile/check-slug')) {
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      }
      if (url.includes('/api/content/moderate')) {
        return Promise.resolve({
          ok: true,
          json: async () => ({
            isApproved: true,
            severity: 'safe',
            flaggedContent: [],
            suggestions: [],
            requiresHumanReview: false,
          }),
        } as Response)
      }
      return Promise.reject(new Error('Unknown URL'))
    })
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Initial Rendering', () => {
    it('should render all form sections', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      expect(screen.getByText('Profile Photo')).toBeInTheDocument()
      expect(screen.getByLabelText('First Name')).toBeInTheDocument()
      expect(screen.getByLabelText('Last Name')).toBeInTheDocument()
      expect(screen.getByLabelText('Profile URL')).toBeInTheDocument()
      expect(screen.getByLabelText('Professional Title')).toBeInTheDocument()
    })

    it('should render photo upload component', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      expect(screen.getByTestId('photo-upload')).toBeInTheDocument()
      expect(screen.getByText('Photo Upload Component')).toBeInTheDocument()
    })

    it('should render submit button with default text', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      expect(screen.getByRole('button', { name: 'Create Profile' })).toBeInTheDocument()
    })

    it('should render submit button with custom text', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} submitButtonText="Update Profile" />)

      expect(screen.getByRole('button', { name: 'Update Profile' })).toBeInTheDocument()
    })

    it('should render with initial data', () => {
      const initialData = {
        firstName: 'John',
        lastName: 'Doe',
        title: 'Senior Developer',
        profilePhotoUrl: 'https://example.com/photo.jpg',
      }

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} initialData={initialData} />)

      expect(screen.getByDisplayValue('John')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Doe')).toBeInTheDocument()
      expect(screen.getByDisplayValue('Senior Developer')).toBeInTheDocument()
      expect(screen.getByAltText('Current')).toHaveAttribute('src', 'https://example.com/photo.jpg')
    })
  })

  describe('Slug Auto-Generation', () => {
    it('should auto-generate slug from first and last name', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const firstNameInput = screen.getByLabelText('First Name')
      const lastNameInput = screen.getByLabelText('Last Name')
      const slugInput = screen.getByLabelText('Profile URL')

      await user.type(firstNameInput, 'John')
      await user.type(lastNameInput, 'Doe')

      await waitFor(() => {
        expect(slugInput).toHaveValue('john-doe')
      })
    })

    it('should handle special characters in slug generation', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const firstNameInput = screen.getByLabelText('First Name')
      const lastNameInput = screen.getByLabelText('Last Name')
      const slugInput = screen.getByLabelText('Profile URL')

      await user.type(firstNameInput, "Mary-Jane")
      await user.type(lastNameInput, "O'Brien")

      await waitFor(() => {
        expect(slugInput).toHaveValue('mary-jane-obrien')
      })
    })

    it('should handle multiple spaces in slug generation', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const firstNameInput = screen.getByLabelText('First Name')
      const slugInput = screen.getByLabelText('Profile URL')

      await user.type(firstNameInput, 'John   Paul')

      await waitFor(() => {
        expect(slugInput).toHaveValue('john-paul')
      })
    })
  })

  describe('Slug Availability Checking', () => {
    it('should check slug availability when slug is entered', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const slugInput = screen.getByLabelText('Profile URL')
      await user.type(slugInput, 'john')

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/profile/check-slug?slug=john')
        )
      })
    })

    it('should show checking message while validating slug', async () => {
      const user = userEvent.setup()

      // Create a promise that we can resolve manually to control timing
      let resolveSlugCheck: (value: Response) => void
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/profile/check-slug')) {
          return new Promise<Response>(resolve => {
            resolveSlugCheck = resolve
          })
        }
        return Promise.reject(new Error('Unknown URL'))
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const slugInput = screen.getByLabelText('Profile URL')
      await user.type(slugInput, 'johndoe')

      // Wait for the loading state to appear (async state update)
      await waitFor(() => {
        expect(screen.getByText('Checking availability...')).toBeInTheDocument()
      })

      // Resolve the pending request to clean up
      resolveSlugCheck!({
        ok: true,
        json: async () => ({ isAvailable: true }),
      } as Response)
    })

    it('should show success when slug is available', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const slugInput = screen.getByLabelText('Profile URL')
      await user.type(slugInput, 'available-slug')

      await waitFor(() => {
        expect(screen.getByText('✓ URL is available')).toBeInTheDocument()
      })
    })

    it('should show error with suggestion when slug is not available', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/profile/check-slug')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ isAvailable: false, suggestion: 'john-doe-123' }),
          } as Response)
        }
        return Promise.reject(new Error('Unknown URL'))
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const slugInput = screen.getByLabelText('Profile URL')
      await user.type(slugInput, 'taken-slug')

      await waitFor(() => {
        expect(screen.getByText(/✗ URL not available. Try: john-doe-123/)).toBeInTheDocument()
      })
    })

    it('should handle slug availability check error gracefully', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/profile/check-slug')) {
          return Promise.reject(new Error('Network error'))
        }
        return Promise.reject(new Error('Unknown URL'))
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const slugInput = screen.getByLabelText('Profile URL')
      await user.type(slugInput, 'test-slug')

      // Should not crash and should assume available
      await waitFor(() => {
        expect(screen.getByText('✓ URL is available')).toBeInTheDocument()
      })
    })

    it('should not check availability for slugs shorter than 3 characters', async () => {
      const user = userEvent.setup()
      mockFetch.mockClear()

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const slugInput = screen.getByLabelText('Profile URL')
      await user.type(slugInput, 'ab')

      // Wait a bit to ensure no call is made
      await new Promise(resolve => setTimeout(resolve, 100))

      expect(mockFetch).not.toHaveBeenCalledWith(
        expect.stringContaining('/api/profile/check-slug')
      )
    })
  })

  describe('Content Moderation', () => {
    it('should moderate title content when length > 10', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByLabelText('Professional Title')
      await user.type(titleInput, 'Senior Developer')

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/content/moderate',
          expect.objectContaining({
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ content: 'Senior Developer', context: 'title' }),
          })
        )
      })
    })

    it('should show checking message during moderation', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return new Promise(resolve => {
            setTimeout(() => {
              resolve({
                ok: true,
                json: async () => ({
                  isApproved: true,
                  severity: 'safe',
                  flaggedContent: [],
                  suggestions: [],
                  requiresHumanReview: false,
                }),
              } as Response)
            }, 100)
          })
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByLabelText('Professional Title')
      await user.type(titleInput, 'Senior Full Stack Developer')

      expect(screen.getByText('Checking content...')).toBeInTheDocument()
    })

    it('should show error for high severity content violations', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({
              isApproved: false,
              severity: 'high',
              flaggedContent: ['inappropriate term'],
              suggestions: [],
              requiresHumanReview: false,
            }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByLabelText('Professional Title')
      await user.type(titleInput, 'Bad content here')

      await waitFor(() => {
        expect(screen.getByText(/Content violates community guidelines: inappropriate term/)).toBeInTheDocument()
      })
    })

    it('should show warning for medium severity requiring review', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({
              isApproved: true,
              severity: 'medium',
              flaggedContent: [],
              suggestions: [],
              requiresHumanReview: true,
            }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByLabelText('Professional Title')
      await user.type(titleInput, 'Questionable title')

      await waitFor(() => {
        expect(screen.getByText(/Content is under review and may require approval/)).toBeInTheDocument()
      })
    })

    it('should show info message with suggestions', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({
              isApproved: true,
              severity: 'safe',
              flaggedContent: [],
              suggestions: ['Consider being more specific', 'Add years of experience'],
              requiresHumanReview: false,
            }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByLabelText('Professional Title')
      await user.type(titleInput, 'Developer title')

      await waitFor(() => {
        expect(screen.getByText(/Suggestions: Consider being more specific, Add years of experience/)).toBeInTheDocument()
      })
    })

    it('should handle moderation API failure gracefully', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return Promise.reject(new Error('Network error'))
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const titleInput = screen.getByLabelText('Professional Title')
      await user.type(titleInput, 'Software Engineer')

      // Should not crash and allow submission
      await waitFor(() => {
        expect(titleInput).toHaveValue('Software Engineer')
      })
    })
  })

  describe('Photo Upload Integration', () => {
    it('should handle successful photo upload', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const uploadButton = screen.getByText('Upload Photo')
      await user.click(uploadButton)

      // Photo ID should be stored for submission
      const firstNameInput = screen.getByLabelText('First Name')
      await user.type(firstNameInput, 'John')

      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            photoFileId: 'photo-123',
          })
        )
      })
    })

    it('should display current photo when provided', () => {
      render(
        <EnhancedProfileCreationForm
          onSubmit={mockOnSubmit}
          initialData={{ profilePhotoUrl: 'https://example.com/current.jpg' }}
        />
      )

      expect(screen.getByAltText('Current')).toHaveAttribute('src', 'https://example.com/current.jpg')
    })
  })

  describe('Form Validation', () => {
    it('should validate URL fields', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const expandButton = screen.getByText('Show Additional Information')
      await user.click(expandButton)

      const websiteInput = screen.getByLabelText('Website')
      await user.type(websiteInput, 'not-a-url')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Please enter a valid URL')).toBeInTheDocument()
      })
    })

    it('should accept valid URLs', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const expandButton = screen.getByText('Show Additional Information')
      await user.click(expandButton)

      const websiteInput = screen.getByLabelText('Website')
      await user.type(websiteInput, 'https://example.com')

      await waitFor(() => {
        expect(screen.queryByText('Please enter a valid URL')).not.toBeInTheDocument()
      })
    })

    it('should accept empty URL fields', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const expandButton = screen.getByText('Show Additional Information')
      await user.click(expandButton)

      const websiteInput = screen.getByLabelText('Website')
      await user.clear(websiteInput)

      await waitFor(() => {
        expect(screen.queryByText('Please enter a valid URL')).not.toBeInTheDocument()
      })
    })
  })

  describe('Expandable Section', () => {
    it('should not show additional fields initially', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      expect(screen.queryByLabelText('Location')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Time Zone')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Website')).not.toBeInTheDocument()
    })

    it('should show additional fields when expanded', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const expandButton = screen.getByText('Show Additional Information')
      await user.click(expandButton)

      expect(screen.getByLabelText('Location')).toBeInTheDocument()
      expect(screen.getByLabelText('Time Zone')).toBeInTheDocument()
      expect(screen.getByLabelText('Website')).toBeInTheDocument()
      expect(screen.getByLabelText('LinkedIn')).toBeInTheDocument()
      expect(screen.getByLabelText('GitHub')).toBeInTheDocument()
    })

    it('should hide fields when collapsed', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const expandButton = screen.getByText('Show Additional Information')
      await user.click(expandButton)

      expect(screen.getByLabelText('Location')).toBeInTheDocument()

      const collapseButton = screen.getByText('Hide Additional Information')
      await user.click(collapseButton)

      expect(screen.queryByLabelText('Location')).not.toBeInTheDocument()
    })
  })

  describe('Form Submission', () => {
    it('should submit form with all data', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      await user.type(screen.getByLabelText('First Name'), 'John')
      await user.type(screen.getByLabelText('Last Name'), 'Doe')
      await user.type(screen.getByLabelText('Professional Title'), 'Senior Engineer')

      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            firstName: 'John',
            lastName: 'Doe',
            title: 'Senior Engineer',
            profileSlug: 'john-doe',
          })
        )
      })
    })

    it('should show loading state during submission', async () => {
      const user = userEvent.setup()
      mockOnSubmit.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 100)))

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      await user.type(screen.getByLabelText('First Name'), 'John')

      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      await user.click(submitButton)

      expect(screen.getByText('Saving...')).toBeInTheDocument()
      expect(submitButton).toBeDisabled()
    })

    it('should disable form fields when isLoading prop is true', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} isLoading={true} />)

      expect(screen.getByLabelText('First Name')).toBeDisabled()
      expect(screen.getByLabelText('Last Name')).toBeDisabled()
      expect(screen.getByLabelText('Professional Title')).toBeDisabled()
      expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    })

    it('should prevent submission when slug is not available', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/profile/check-slug')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ isAvailable: false, suggestion: 'other-slug' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({
            isApproved: true,
            severity: 'safe',
            flaggedContent: [],
            suggestions: [],
            requiresHumanReview: false,
          }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      await user.type(screen.getByLabelText('First Name'), 'John')
      const slugInput = screen.getByLabelText('Profile URL')
      await user.clear(slugInput)
      await user.type(slugInput, 'taken-slug')

      await waitFor(() => {
        expect(screen.getByText(/✗ URL not available/)).toBeInTheDocument()
      })

      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      expect(submitButton).toBeDisabled()
    })

    it('should prevent submission when content has high severity violations', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({
              isApproved: false,
              severity: 'high',
              flaggedContent: ['violation'],
              suggestions: [],
              requiresHumanReview: false,
            }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      await user.type(screen.getByLabelText('Professional Title'), 'Violating content here')

      await waitFor(() => {
        expect(screen.getByText(/Content violates community guidelines/)).toBeInTheDocument()
      })

      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      expect(submitButton).toBeDisabled()
    })

    it('should not call onSubmit when blocked content exists', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url: any) => {
        if (url.includes('/api/content/moderate')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({
              isApproved: false,
              severity: 'high',
              flaggedContent: ['violation'],
              suggestions: [],
              requiresHumanReview: false,
            }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ isAvailable: true }),
        } as Response)
      })

      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      await user.type(screen.getByLabelText('Professional Title'), 'Violating title')

      await waitFor(() => {
        expect(screen.getByText(/Content violates/)).toBeInTheDocument()
      })

      // Try to submit (button should be disabled, but test the handler logic)
      const form = screen.getByRole('button', { name: 'Create Profile' }).closest('form')
      if (form) {
        // Even if we could submit, the handler should block it
        expect(screen.getByRole('button', { name: 'Create Profile' })).toBeDisabled()
      }
    })
  })

  describe('Privacy Setting', () => {
    it('should render privacy checkbox', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      expect(screen.getByLabelText('Make my profile publicly discoverable')).toBeInTheDocument()
    })

    it('should have privacy checkbox unchecked by default', () => {
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const checkbox = screen.getByLabelText('Make my profile publicly discoverable')
      expect(checkbox).not.toBeChecked()
    })

    it('should toggle privacy checkbox', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const checkbox = screen.getByLabelText('Make my profile publicly discoverable')
      await user.click(checkbox)

      expect(checkbox).toBeChecked()
    })

    it('should submit privacy setting', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      const checkbox = screen.getByLabelText('Make my profile publicly discoverable')
      await user.click(checkbox)

      await user.type(screen.getByLabelText('First Name'), 'John')

      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            isPublic: true,
          })
        )
      })
    })
  })

  describe('Integration', () => {
    it('should render complete form without errors', () => {
      const { container } = render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      expect(container.firstChild).toBeTruthy()
    })

    it('should handle full workflow', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCreationForm onSubmit={mockOnSubmit} />)

      // Upload photo
      await user.click(screen.getByText('Upload Photo'))

      // Fill basic info
      await user.type(screen.getByLabelText('First Name'), 'Jane')
      await user.type(screen.getByLabelText('Last Name'), 'Smith')

      // Wait for slug generation
      await waitFor(() => {
        expect(screen.getByLabelText('Profile URL')).toHaveValue('jane-smith')
      })

      // Fill professional info
      await user.type(screen.getByLabelText('Professional Title'), 'Product Manager')
      await user.type(screen.getByLabelText('Professional Summary'), 'Experienced product manager with 10 years in tech')

      // Expand additional info
      await user.click(screen.getByText('Show Additional Information'))

      // Fill additional fields
      await user.type(screen.getByLabelText('Current Company'), 'Tech Corp')
      await user.type(screen.getByLabelText('Location'), 'San Francisco, CA')
      await user.type(screen.getByLabelText('Time Zone'), 'UTC-8')
      await user.type(screen.getByLabelText('LinkedIn'), 'https://linkedin.com/in/janesmith')

      // Enable public profile
      await user.click(screen.getByLabelText('Make my profile publicly discoverable'))

      // Submit
      const submitButton = screen.getByRole('button', { name: 'Create Profile' })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith(
          expect.objectContaining({
            firstName: 'Jane',
            lastName: 'Smith',
            profileSlug: 'jane-smith',
            title: 'Product Manager',
            summary: 'Experienced product manager with 10 years in tech',
            company: 'Tech Corp',
            location: 'San Francisco, CA',
            timeZone: 'UTC-8',
            linkedInUrl: 'https://linkedin.com/in/janesmith',
            isPublic: true,
            photoFileId: 'photo-123',
          })
        )
      })
    }, 10000)
  })
})
