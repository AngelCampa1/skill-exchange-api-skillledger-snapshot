import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import Step1BasicInfo from '../../wizard/Step1BasicInfo'
import { BasicInfo } from '@/types/profile'

describe('Step1BasicInfo', () => {
  const mockData: BasicInfo = {
    firstName: '',
    lastName: '',
    title: '',
  }

  const mockOnUpdate = jest.fn()
  const mockOnNext = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('renders the basic info form', () => {
    render(<Step1BasicInfo data={mockData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    expect(screen.getByText('Basic Information')).toBeInTheDocument()
    expect(screen.getByLabelText(/First Name/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Last Name/)).toBeInTheDocument()
    expect(screen.getByLabelText(/Professional Title/)).toBeInTheDocument()
  })

  it('displays validation errors for required fields', async () => {
    render(<Step1BasicInfo data={mockData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    // Try to submit without filling required fields
    const nextButton = screen.getByText('Next Step')
    expect(nextButton).toBeDisabled()
  })

  it('enables submit button when all required fields are filled', async () => {
    render(<Step1BasicInfo data={mockData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    const firstNameInput = screen.getByLabelText(/First Name/)
    const lastNameInput = screen.getByLabelText(/Last Name/)
    const titleInput = screen.getByLabelText(/Professional Title/)

    fireEvent.change(firstNameInput, { target: { value: 'John' } })
    fireEvent.change(lastNameInput, { target: { value: 'Doe' } })
    fireEvent.change(titleInput, { target: { value: 'Software Engineer' } })

    await waitFor(() => {
      const nextButton = screen.getByText('Next Step')
      expect(nextButton).not.toBeDisabled()
    })
  })

  it('calls onUpdate and onNext when form is submitted', async () => {
    render(<Step1BasicInfo data={mockData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    const firstNameInput = screen.getByLabelText(/First Name/)
    const lastNameInput = screen.getByLabelText(/Last Name/)
    const titleInput = screen.getByLabelText(/Professional Title/)

    fireEvent.change(firstNameInput, { target: { value: 'John' } })
    fireEvent.change(lastNameInput, { target: { value: 'Doe' } })
    fireEvent.change(titleInput, { target: { value: 'Software Engineer' } })

    await waitFor(() => {
      const nextButton = screen.getByText('Next Step')
      expect(nextButton).not.toBeDisabled()
    })

    fireEvent.click(screen.getByText('Next Step'))

    await waitFor(() => {
      expect(mockOnUpdate).toHaveBeenCalled()
      expect(mockOnNext).toHaveBeenCalled()
    })
  })

  it('loads initial data correctly', () => {
    const initialData: BasicInfo = {
      firstName: 'Jane',
      lastName: 'Smith',
      title: 'Product Manager',
      company: 'Tech Corp',
    }

    render(<Step1BasicInfo data={initialData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    expect(screen.getByDisplayValue('Jane')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Smith')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Product Manager')).toBeInTheDocument()
    expect(screen.getByDisplayValue('Tech Corp')).toBeInTheDocument()
  })

  it('validates URL fields correctly', async () => {
    render(<Step1BasicInfo data={mockData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    const websiteInput = screen.getByLabelText(/Website/)

    // Enter invalid URL
    fireEvent.change(websiteInput, { target: { value: 'not-a-url' } })

    await waitFor(() => {
      expect(screen.getByText(/Please enter a valid URL/)).toBeInTheDocument()
    })

    // Enter valid URL
    fireEvent.change(websiteInput, { target: { value: 'https://example.com' } })

    await waitFor(() => {
      expect(screen.queryByText(/Please enter a valid URL/)).not.toBeInTheDocument()
    })
  })

  it('accepts empty string for optional URL fields', async () => {
    render(<Step1BasicInfo data={mockData} onUpdate={mockOnUpdate} onNext={mockOnNext} />)

    const websiteInput = screen.getByLabelText(/Website/)

    // Enter and clear URL field
    fireEvent.change(websiteInput, { target: { value: '' } })

    await waitFor(() => {
      expect(screen.queryByText(/Please enter a valid URL/)).not.toBeInTheDocument()
    })
  })
})
