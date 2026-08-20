import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import ProfileCreationForm from '../ProfileCreationForm'

// Mock useRouter
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      back: jest.fn(),
    }
  },
}))

// Store photo upload callback for testing
let capturedPhotoUploadCallback: ((result: { success: boolean; fileUrl?: string; error?: string }) => void) | null = null

// Mock SimpleProfessionalPhotoUpload to capture the callback
jest.mock('../SimpleProfessionalPhotoUpload', () => ({
  __esModule: true,
  default: ({ onUploadComplete, currentPhotoUrl }: any) => {
    capturedPhotoUploadCallback = onUploadComplete
    return (
      <div data-testid="mock-photo-upload">
        <span>Photo Upload Mock</span>
        {currentPhotoUrl && <img src={currentPhotoUrl} alt="Current" data-testid="current-photo" />}
        <button
          type="button"
          onClick={() => onUploadComplete({ success: true, fileUrl: 'https://example.com/avatar.jpg' })}
          data-testid="upload-success-btn"
        >
          Simulate Upload Success
        </button>
        <button
          type="button"
          onClick={() => onUploadComplete({ success: false, error: 'Upload failed' })}
          data-testid="upload-error-btn"
        >
          Simulate Upload Error
        </button>
      </div>
    )
  }
}))

// Store skill change callback for testing
let capturedSkillsChangeCallback: ((skills: string[]) => void) | null = null

// Mock SkillSelector to capture the callback
jest.mock('../SkillSelector', () => ({
  __esModule: true,
  default: ({ selectedSkills, onSkillsChange, minSkills }: any) => {
    capturedSkillsChangeCallback = onSkillsChange
    return (
      <div data-testid="mock-skill-selector">
        <span>{selectedSkills.length} / {minSkills} skills selected</span>
        <button
          type="button"
          onClick={() => onSkillsChange(['skill1', 'skill2', 'skill3'])}
          data-testid="add-skills-btn"
        >
          Add 3 Skills
        </button>
        <button
          type="button"
          onClick={() => onSkillsChange(['skill1'])}
          data-testid="add-one-skill-btn"
        >
          Add 1 Skill
        </button>
        <button
          type="button"
          onClick={() => onSkillsChange([])}
          data-testid="clear-skills-btn"
        >
          Clear Skills
        </button>
      </div>
    )
  }
}))

describe('ProfileCreationForm', () => {
  const mockOnSubmit = jest.fn().mockResolvedValue(undefined)

  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('renders basic form fields', () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} />)
    
    expect(screen.getByLabelText(/first name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/last name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/professional title/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/company/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /create profile/i })).toBeInTheDocument()
  })

  it('shows additional fields when expanded', async () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} />)
    
    // Click expand button
    const expandButton = screen.getByText(/additional details/i)
    fireEvent.click(expandButton)
    
    await waitFor(() => {
      expect(screen.getByLabelText(/professional summary/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/location/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/time zone/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/website/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/linkedin profile/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/github profile/i)).toBeInTheDocument()
    })
  })

  it('shows profile completion indicator when required fields are filled', async () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} />)
    
    // Fill required fields
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })
    fireEvent.change(screen.getByLabelText(/last name/i), {
      target: { value: 'Doe' }
    })
    fireEvent.change(screen.getByLabelText(/professional title/i), {
      target: { value: 'Software Engineer' }
    })
    
    await waitFor(() => {
      expect(screen.getByText(/great! your profile will be marked as complete/i)).toBeInTheDocument()
    })
  })

  it('validates URL fields correctly', async () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} />)
    
    // Expand to show URL fields
    fireEvent.click(screen.getByText(/additional details/i))
    
    await waitFor(() => {
      expect(screen.getByLabelText(/website/i)).toBeInTheDocument()
    })
    
    // Enter invalid URL
    const websiteInput = screen.getByLabelText(/website/i)
    fireEvent.change(websiteInput, { target: { value: 'invalid-url' } })
    fireEvent.blur(websiteInput)
    
    await waitFor(() => {
      expect(screen.getByText(/please enter a valid url/i)).toBeInTheDocument()
    })
  })

  it('submits form with valid data', async () => {
    // BUG-005 FIX: Use showSkillSelection={false} to test form submission without skills requirement
    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={false} />)

    // Fill form
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })
    fireEvent.change(screen.getByLabelText(/last name/i), {
      target: { value: 'Doe' }
    })
    fireEvent.change(screen.getByLabelText(/professional title/i), {
      target: { value: 'Software Engineer' }
    })

    // Submit form
    const submitButton = screen.getByRole('button', { name: /create profile/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith(expect.objectContaining({
        firstName: 'John',
        lastName: 'Doe',
        title: 'Software Engineer',
        isPublic: false
      }))
    })
  })

  it('shows loading state during submission', async () => {
    const slowSubmit = jest.fn().mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    )

    // BUG-005 FIX: Use showSkillSelection={false} to test loading state without skills requirement
    render(<ProfileCreationForm onSubmit={slowSubmit} showSkillSelection={false} />)

    // Fill minimal required fields
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })

    // Submit form
    const submitButton = screen.getByRole('button', { name: /create profile/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText(/creating profile/i)).toBeInTheDocument()
    })
  })

  it('pre-fills form with initial data', () => {
    const initialData = {
      firstName: 'Jane',
      lastName: 'Smith',
      title: 'Product Manager',
      isPublic: true
    }
    
    render(
      <ProfileCreationForm 
        onSubmit={mockOnSubmit} 
        initialData={initialData}
        submitButtonText="Update Profile"
      />
    )
    
    expect(screen.getByDisplayValue('Jane')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Smith')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Product Manager')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /update profile/i })).toBeInTheDocument()
  })

  it('handles public profile checkbox correctly', () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} />)

    const checkbox = screen.getByLabelText(/make my profile visible to other users/i)
    expect(checkbox).not.toBeChecked()

    fireEvent.click(checkbox)
    expect(checkbox).toBeChecked()
  })

  // BUG-005 FIX: Test minimum skills validation
  it('disables submit button when minimum skills are not met', () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={true} />)

    // Fill required fields
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })
    fireEvent.change(screen.getByLabelText(/last name/i), {
      target: { value: 'Doe' }
    })
    fireEvent.change(screen.getByLabelText(/professional title/i), {
      target: { value: 'Software Engineer' }
    })

    // Submit button should be disabled without 3 skills
    const submitButton = screen.getByRole('button', { name: /create profile/i })
    expect(submitButton).toBeDisabled()
  })

  it('shows skill count indicator when showSkillSelection is true', () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={true} />)

    // Should show skills selection requirement message
    expect(screen.getByText(/0.*3.*skills selected/i)).toBeInTheDocument()
  })

  // Coverage: Lines 80-83 - Skills validation error on submit
  it('shows skills validation error when submitting with insufficient skills', async () => {
    // Mock scrollIntoView
    const scrollIntoViewMock = jest.fn()
    Element.prototype.scrollIntoView = scrollIntoViewMock

    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={true} />)

    // Fill required fields
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })
    fireEvent.change(screen.getByLabelText(/last name/i), {
      target: { value: 'Doe' }
    })
    fireEvent.change(screen.getByLabelText(/professional title/i), {
      target: { value: 'Software Engineer' }
    })

    // Add only 1 skill (less than minimum 3)
    fireEvent.click(screen.getByTestId('add-one-skill-btn'))

    // Try to submit by simulating form submission
    const form = document.querySelector('form')
    expect(form).toBeTruthy()
    fireEvent.submit(form!)

    await waitFor(() => {
      expect(screen.getByText(/please select at least 3 skills/i)).toBeInTheDocument()
    })
  })

  // Coverage: Lines 97-100 - Photo upload success handler
  it('handles successful photo upload', async () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={false} />)

    // Trigger photo upload success
    fireEvent.click(screen.getByTestId('upload-success-btn'))

    // The avatar URL should be stored (component should update internal state)
    // We can verify by checking if form submission includes avatarUrl
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })

    // Submit form
    const submitButton = screen.getByRole('button', { name: /create profile/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith(expect.objectContaining({
        avatarUrl: 'https://example.com/avatar.jpg'
      }))
    })
  })

  // Coverage: Lines 97-100 - Photo upload error handler
  it('handles failed photo upload', async () => {
    // Suppress console.error for expected error logging
    const originalError = console.error
    console.error = jest.fn()

    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={false} />)

    // Trigger photo upload error
    fireEvent.click(screen.getByTestId('upload-error-btn'))

    // The component should log the error (coverage for line 100)
    // Form should still be usable without avatar
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })

    const submitButton = screen.getByRole('button', { name: /create profile/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled()
    })

    // Restore console.error
    console.error = originalError
  })

  // Coverage: Lines 328-332 - Skill change clears error
  it('clears skills error when minimum skills are met', async () => {
    // Mock scrollIntoView
    Element.prototype.scrollIntoView = jest.fn()

    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={true} />)

    // Fill required fields
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })
    fireEvent.change(screen.getByLabelText(/last name/i), {
      target: { value: 'Doe' }
    })
    fireEvent.change(screen.getByLabelText(/professional title/i), {
      target: { value: 'Software Engineer' }
    })

    // Add only 1 skill to trigger error state
    fireEvent.click(screen.getByTestId('add-one-skill-btn'))

    // Try to submit to generate error
    const form = document.querySelector('form')
    if (form) {
      fireEvent.submit(form)
    }

    await waitFor(() => {
      expect(screen.getByText(/please select at least 3 skills/i)).toBeInTheDocument()
    })

    // Now add enough skills - this should clear the error (coverage for lines 328-332)
    fireEvent.click(screen.getByTestId('add-skills-btn'))

    await waitFor(() => {
      expect(screen.queryByText(/please select at least 3 skills/i)).not.toBeInTheDocument()
    })
  })

  // Coverage: Line 414 - Skip button click handler
  it('navigates back when skip button is clicked', () => {
    // Mock window.history.back
    const backMock = jest.fn()
    const originalBack = window.history.back
    window.history.back = backMock

    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={false} />)

    // Click skip button
    const skipButton = screen.getByText(/skip for now/i)
    fireEvent.click(skipButton)

    expect(backMock).toHaveBeenCalled()

    // Restore
    window.history.back = originalBack
  })

  // Coverage: Skill selector display when skills are added
  it('displays correct skill count when skills are selected', async () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={true} />)

    // Initially 0 skills
    expect(screen.getByText(/0 \/ 3 skills selected/i)).toBeInTheDocument()

    // Add 3 skills
    fireEvent.click(screen.getByTestId('add-skills-btn'))

    await waitFor(() => {
      expect(screen.getByText(/3 \/ 3 skills selected/i)).toBeInTheDocument()
    })
  })

  // Coverage: Form submission includes skills data
  it('includes selected skills in form submission', async () => {
    render(<ProfileCreationForm onSubmit={mockOnSubmit} showSkillSelection={true} />)

    // Fill required fields
    fireEvent.change(screen.getByLabelText(/first name/i), {
      target: { value: 'John' }
    })
    fireEvent.change(screen.getByLabelText(/last name/i), {
      target: { value: 'Doe' }
    })
    fireEvent.change(screen.getByLabelText(/professional title/i), {
      target: { value: 'Software Engineer' }
    })

    // Add 3 skills
    fireEvent.click(screen.getByTestId('add-skills-btn'))

    await waitFor(() => {
      expect(screen.getByText(/3 \/ 3 skills selected/i)).toBeInTheDocument()
    })

    // Submit form
    const submitButton = screen.getByRole('button', { name: /create profile/i })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith(expect.objectContaining({
        skills: ['skill1', 'skill2', 'skill3']
      }))
    })
  })
})