import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import Step2SkillSelection from '../../wizard/Step2SkillSelection'
import { Skill } from '@/types/profile'

describe('Step2SkillSelection', () => {
  const mockSkills: Skill[] = []
  const mockOnUpdate = jest.fn()
  const mockOnNext = jest.fn()
  const mockOnBack = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('renders the skill selection form', () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    expect(screen.getByText('Your Skills')).toBeInTheDocument()
    expect(screen.getByLabelText(/Skill Name/)).toBeInTheDocument()
    expect(screen.getByText('Add Skill')).toBeInTheDocument()
  })

  it('adds a skill to the list', async () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const skillNameInput = screen.getByLabelText(/Skill Name/)
    fireEvent.change(skillNameInput, { target: { value: 'JavaScript' } })

    const addButton = screen.getByText('Add Skill')
    fireEvent.click(addButton)

    await waitFor(() => {
      expect(screen.getByText('JavaScript')).toBeInTheDocument()
    })
  })

  it('shows error when trying to add skill without name', async () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const addButton = screen.getByText('Add Skill')
    fireEvent.click(addButton)

    await waitFor(() => {
      expect(screen.getByText('Skill name is required')).toBeInTheDocument()
    })
  })

  it('allows setting proficiency level and years of experience', async () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const skillNameInput = screen.getByLabelText(/Skill Name/)
    fireEvent.change(skillNameInput, { target: { value: 'Python' } })

    const proficiencySelect = screen.getByLabelText(/Proficiency Level/)
    fireEvent.change(proficiencySelect, { target: { value: 'Expert' } })

    const yearsInput = screen.getByLabelText(/Years of Experience/)
    fireEvent.change(yearsInput, { target: { value: '5' } })

    const addButton = screen.getByText('Add Skill')
    fireEvent.click(addButton)

    await waitFor(() => {
      expect(screen.getByText('Python')).toBeInTheDocument()
      const expertElements = screen.getAllByText('Expert')
      expect(expertElements.length).toBeGreaterThan(0)
      expect(screen.getByText('5 years')).toBeInTheDocument()
    })
  })

  it('removes a skill from the list', async () => {
    const initialSkills: Skill[] = [
      {
        id: 'skill-1',
        name: 'React',
        proficiencyLevel: 'Advanced',
        yearsOfExperience: 3,
      },
    ]

    render(
      <Step2SkillSelection
        skills={initialSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    expect(screen.getByText('React')).toBeInTheDocument()

    const removeButton = screen.getByText('Remove')
    fireEvent.click(removeButton)

    await waitFor(() => {
      expect(screen.queryByText('React')).not.toBeInTheDocument()
    })
  })

  it('clears form after adding a skill', async () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const skillNameInput = screen.getByLabelText(/Skill Name/) as HTMLInputElement
    fireEvent.change(skillNameInput, { target: { value: 'TypeScript' } })

    const addButton = screen.getByText('Add Skill')
    fireEvent.click(addButton)

    await waitFor(() => {
      expect(skillNameInput.value).toBe('')
    })
  })

  it('calls onBack when Back button is clicked', () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const backButton = screen.getByText('Back')
    fireEvent.click(backButton)

    expect(mockOnBack).toHaveBeenCalled()
  })

  // BUG-005 FIX: Updated test to match new dynamic error message format
  it('shows error when trying to proceed without any skills', () => {
    render(
      <Step2SkillSelection
        skills={mockSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const nextButton = screen.getByText('Next Step')
    fireEvent.click(nextButton)

    // Default minSkills is 1, so error message uses number format
    expect(screen.getByText('Please add at least 1 skill')).toBeInTheDocument()
    expect(mockOnNext).not.toHaveBeenCalled()
  })

  it('calls onUpdate and onNext when Next button is clicked with skills', async () => {
    const initialSkills: Skill[] = [
      {
        id: 'skill-1',
        name: 'React',
        proficiencyLevel: 'Advanced',
        yearsOfExperience: 3,
      },
    ]

    render(
      <Step2SkillSelection
        skills={initialSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
      />
    )

    const nextButton = screen.getByText('Next Step')
    fireEvent.click(nextButton)

    await waitFor(() => {
      expect(mockOnUpdate).toHaveBeenCalledWith(initialSkills)
      expect(mockOnNext).toHaveBeenCalled()
    })
  })

  // BUG-005 FIX: Test for custom minSkills validation
  it('enforces custom minSkills requirement', () => {
    const initialSkills: Skill[] = [
      {
        id: 'skill-1',
        name: 'React',
        proficiencyLevel: 'Advanced',
        yearsOfExperience: 3,
      },
    ]

    render(
      <Step2SkillSelection
        skills={initialSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
        minSkills={3}
      />
    )

    const nextButton = screen.getByText('Next Step')
    fireEvent.click(nextButton)

    // Should show error because only 1 skill but 3 required
    expect(screen.getByText('Please add at least 3 skills')).toBeInTheDocument()
    expect(mockOnNext).not.toHaveBeenCalled()
  })

  it('shows minimum indicator when minSkills is set', () => {
    const initialSkills: Skill[] = [
      {
        id: 'skill-1',
        name: 'React',
        proficiencyLevel: 'Advanced',
        yearsOfExperience: 3,
      },
    ]

    render(
      <Step2SkillSelection
        skills={initialSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
        minSkills={3}
      />
    )

    // Should show the requirement indicator
    expect(screen.getByText('(3 required)')).toBeInTheDocument()
  })

  it('shows success indicator when minimum skills are met', async () => {
    const initialSkills: Skill[] = [
      { id: 'skill-1', name: 'React', proficiencyLevel: 'Advanced', yearsOfExperience: 3 },
      { id: 'skill-2', name: 'TypeScript', proficiencyLevel: 'Intermediate', yearsOfExperience: 2 },
      { id: 'skill-3', name: 'Node.js', proficiencyLevel: 'Beginner', yearsOfExperience: 1 },
    ]

    render(
      <Step2SkillSelection
        skills={initialSkills}
        onUpdate={mockOnUpdate}
        onNext={mockOnNext}
        onBack={mockOnBack}
        minSkills={3}
      />
    )

    expect(screen.getByText('✓ Minimum met')).toBeInTheDocument()
  })
})
