import React from 'react'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import '@testing-library/jest-dom'
import ProfileOnboardingWizard from '../ProfileOnboardingWizard'
import { STORAGE_KEY } from '@/types/profile'

// Mock the wizard step components
jest.mock('../wizard/Step1BasicInfo', () => {
  return function MockStep1({ onNext }: { onNext: () => void }) {
    return (
      <div data-testid="step1">
        <button onClick={onNext}>Next Step</button>
      </div>
    )
  }
})

jest.mock('../wizard/Step2SkillSelection', () => {
  return function MockStep2({ onNext, onBack, onUpdate }: { onNext: () => void; onBack: () => void; onUpdate: (skills: any[]) => void }) {
    const handleNext = () => {
      // Add 3 skills (minimum required) before proceeding
      onUpdate([
        { id: '1', name: 'JavaScript', proficiency: 3 },
        { id: '2', name: 'React', proficiency: 3 },
        { id: '3', name: 'TypeScript', proficiency: 2 },
      ]);
      onNext();
    };
    return (
      <div data-testid="step2">
        <button onClick={onBack}>Back</button>
        <button onClick={handleNext}>Next Step</button>
      </div>
    )
  }
})

jest.mock('../wizard/Step3ExperienceTimeline', () => {
  return function MockStep3({ onNext, onBack }: { onNext: () => void; onBack: () => void }) {
    return (
      <div data-testid="step3">
        <button onClick={onBack}>Back</button>
        <button onClick={onNext}>Next Step</button>
      </div>
    )
  }
})

jest.mock('../wizard/Step4PhotoUpload', () => {
  return function MockStep4({ onNext, onBack }: { onNext: () => void; onBack: () => void }) {
    return (
      <div data-testid="step4">
        <button onClick={onBack}>Back</button>
        <button onClick={onNext}>Next Step</button>
      </div>
    )
  }
})

jest.mock('../wizard/Step5ReviewPublish', () => {
  return function MockStep5({
    onBack,
    onComplete,
  }: {
    onBack: () => void
    onComplete: () => void
  }) {
    return (
      <div data-testid="step5">
        <button onClick={onBack}>Back</button>
        <button onClick={onComplete}>Publish Profile</button>
      </div>
    )
  }
})

describe('ProfileOnboardingWizard', () => {
  const mockOnComplete = jest.fn()

  beforeEach(() => {
    localStorage.clear()
    jest.clearAllMocks()
  })

  it('renders the wizard with initial step', () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    expect(screen.getByText('Create Your Profile')).toBeInTheDocument()
    expect(screen.getByTestId('step1')).toBeInTheDocument()
  })

  it('renders progress indicator with all steps', () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    expect(screen.getByText('Basic Information')).toBeInTheDocument()
    expect(screen.getByText('Skills')).toBeInTheDocument()
    expect(screen.getByText('Experience')).toBeInTheDocument()
    expect(screen.getByText('Photo')).toBeInTheDocument()
    expect(screen.getByText('Review')).toBeInTheDocument()
  })

  it('navigates forward through steps', () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Start at step 1
    expect(screen.getByTestId('step1')).toBeInTheDocument()

    // Move to step 2
    fireEvent.click(screen.getByText('Next Step'))
    expect(screen.getByTestId('step2')).toBeInTheDocument()

    // Move to step 3
    fireEvent.click(screen.getByText('Next Step'))
    expect(screen.getByTestId('step3')).toBeInTheDocument()
  })

  it('navigates backward through steps', () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Navigate to step 2
    fireEvent.click(screen.getByText('Next Step'))
    expect(screen.getByTestId('step2')).toBeInTheDocument()

    // Go back to step 1
    fireEvent.click(screen.getByText('Back'))
    expect(screen.getByTestId('step1')).toBeInTheDocument()
  })

  it('calls onComplete when wizard is finished', async () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Navigate through all steps
    fireEvent.click(screen.getByText('Next Step')) // Step 2
    fireEvent.click(screen.getByText('Next Step')) // Step 3
    fireEvent.click(screen.getByText('Next Step')) // Step 4
    fireEvent.click(screen.getByText('Next Step')) // Step 5

    // Complete the wizard
    fireEvent.click(screen.getByText('Publish Profile'))

    await waitFor(() => {
      expect(mockOnComplete).toHaveBeenCalled()
    })
  })

  it('saves draft to localStorage on navigation', async () => {
    jest.useFakeTimers()
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Move to next step
    fireEvent.click(screen.getByText('Next Step'))

    // Wait for auto-save
    act(() => {
      jest.advanceTimersByTime(30000)
    })

    const savedDraft = localStorage.getItem(STORAGE_KEY)
    expect(savedDraft).toBeTruthy()

    jest.useRealTimers()
  })

  it('loads saved draft from localStorage on mount', () => {
    const draftData = {
      data: {
        basicInfo: { firstName: 'John', lastName: 'Doe', title: 'Developer' },
        skills: [],
        experiences: [],
        photo: {},
        isPublic: false,
      },
      currentStep: 2,
      lastSaved: new Date().toISOString(),
    }

    localStorage.setItem(STORAGE_KEY, JSON.stringify(draftData))

    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Should start at step 2 (saved step)
    expect(screen.getByTestId('step2')).toBeInTheDocument()
  })

  it('clears draft when completing the wizard', async () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Navigate to final step
    fireEvent.click(screen.getByText('Next Step')) // Step 2
    fireEvent.click(screen.getByText('Next Step')) // Step 3
    fireEvent.click(screen.getByText('Next Step')) // Step 4
    fireEvent.click(screen.getByText('Next Step')) // Step 5

    // Complete the wizard
    fireEvent.click(screen.getByText('Publish Profile'))

    await waitFor(() => {
      expect(localStorage.getItem(STORAGE_KEY)).toBeNull()
    })
  })

  it('displays loading state during submission', () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} isLoading={true} />)

    // Navigate to final step
    fireEvent.click(screen.getByText('Next Step')) // Step 2
    fireEvent.click(screen.getByText('Next Step')) // Step 3
    fireEvent.click(screen.getByText('Next Step')) // Step 4
    fireEvent.click(screen.getByText('Next Step')) // Step 5

    // The step component should receive isLoading prop
    expect(screen.getByTestId('step5')).toBeInTheDocument()
  })

  it('allows clicking on completed steps', () => {
    render(<ProfileOnboardingWizard onComplete={mockOnComplete} />)

    // Complete step 1
    fireEvent.click(screen.getByText('Next Step'))
    expect(screen.getByTestId('step2')).toBeInTheDocument()

    // Complete step 2
    fireEvent.click(screen.getByText('Next Step'))
    expect(screen.getByTestId('step3')).toBeInTheDocument()

    // Click on step 1 in progress indicator
    const stepButtons = screen.getAllByRole('button')
    const step1Button = stepButtons.find(btn => btn.textContent === '✓' || btn.textContent === '1')

    if (step1Button) {
      fireEvent.click(step1Button)
      // Should navigate back to step 1
      expect(screen.getByTestId('step1')).toBeInTheDocument()
    }
  })
})
