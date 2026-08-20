import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import '@testing-library/jest-dom'
import DynamicQuestionnaireForm, {
  QuestionnaireData,
  QuestionType,
  QuestionResponse,
  QuestionnaireQuestion
} from '../DynamicQuestionnaireForm'

describe('DynamicQuestionnaireForm', () => {
  const mockOnSubmit = jest.fn().mockResolvedValue(undefined)
  const mockOnSaveDraft = jest.fn().mockResolvedValue(undefined)

  beforeEach(() => {
    jest.clearAllMocks()
    jest.useFakeTimers()
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  // BUG-LOW-002 FIX: Use proper Partial<QuestionnaireQuestion> type instead of 'any'
  const createTestQuestionnaire = (questions: Partial<QuestionnaireQuestion>[] = []): QuestionnaireData => ({
    id: 'test-questionnaire-1',
    title: 'Test Questionnaire',
    description: 'This is a test questionnaire',
    questions: questions.map((q, index) => ({
      id: `question-${index + 1}`,
      questionText: q.questionText || `Question ${index + 1}`,
      type: q.type ?? QuestionType.Text,
      isRequired: q.isRequired ?? false,
      displayOrder: index + 1,
      options: [],
      ...q
    }))
  })

  it('renders questionnaire title and description', () => {
    const questionnaire = createTestQuestionnaire()
    
    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    expect(screen.getByText('Test Questionnaire')).toBeInTheDocument()
    expect(screen.getByText('This is a test questionnaire')).toBeInTheDocument()
  })

  it('renders text questions correctly', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'What is your name?',
        type: QuestionType.Text,
        isRequired: true,
        placeholderText: 'Enter your name'
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    expect(screen.getByText(/what is your name/i)).toBeInTheDocument()
    expect(screen.getByText('*')).toBeInTheDocument() // Required indicator
    expect(screen.getByPlaceholderText('Enter your name')).toBeInTheDocument()
    expect(screen.getByRole('textbox')).toHaveAttribute('type', 'text')
  })

  it('renders number questions with min/max constraints', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'What is your age?',
        type: QuestionType.Number,
        isRequired: false,
        minValue: 0,
        maxValue: 150
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const numberInput = screen.getByRole('spinbutton')
    expect(numberInput).toHaveAttribute('type', 'number')
    expect(numberInput).toHaveAttribute('min', '0')
    expect(numberInput).toHaveAttribute('max', '150')
  })

  it('renders email questions correctly', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'What is your email?',
        type: QuestionType.Email,
        isRequired: true
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const emailInput = screen.getByRole('textbox')
    expect(emailInput).toHaveAttribute('type', 'email')
    expect(emailInput).toHaveAttribute('placeholder', 'Enter your email')
  })

  it('renders textarea for long text questions', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Tell us about yourself',
        type: QuestionType.LongText,
        isRequired: false,
        placeholderText: 'Write a brief description'
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const textarea = screen.getByRole('textbox')
    expect(textarea.tagName).toBe('TEXTAREA')
    expect(textarea).toHaveAttribute('placeholder', 'Write a brief description')
  })

  it('renders boolean questions as checkboxes', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Do you agree to the terms?',
        type: QuestionType.Boolean,
        isRequired: true
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const checkbox = screen.getByRole('checkbox')
    expect(checkbox).toBeInTheDocument()
    expect(screen.getByText('Yes')).toBeInTheDocument()
  })

  it('renders radio questions with options', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'What is your favorite color?',
        type: QuestionType.Radio,
        isRequired: false,
        options: [
          { id: 'opt1', optionText: 'Red', optionValue: 'red', displayOrder: 1, isDefault: false },
          { id: 'opt2', optionText: 'Blue', optionValue: 'blue', displayOrder: 2, isDefault: false },
          { id: 'opt3', optionText: 'Green', optionValue: 'green', displayOrder: 3, isDefault: false }
        ]
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    expect(screen.getByText('Red')).toBeInTheDocument()
    expect(screen.getByText('Blue')).toBeInTheDocument()
    expect(screen.getByText('Green')).toBeInTheDocument()
    
    const radioButtons = screen.getAllByRole('radio')
    expect(radioButtons).toHaveLength(3)
  })

  it('renders dropdown questions with options', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Select your country',
        type: QuestionType.Dropdown,
        isRequired: true,
        options: [
          { id: 'opt1', optionText: 'United States', optionValue: 'US', displayOrder: 1, isDefault: false },
          { id: 'opt2', optionText: 'Canada', optionValue: 'CA', displayOrder: 2, isDefault: false }
        ]
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const select = screen.getByRole('combobox')
    expect(select).toBeInTheDocument()
    expect(screen.getByText('-- Select an option --')).toBeInTheDocument()
    expect(screen.getByText('United States')).toBeInTheDocument()
    expect(screen.getByText('Canada')).toBeInTheDocument()
  })

  it('renders rating questions with interactive buttons', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Rate our service',
        type: QuestionType.Rating,
        isRequired: false,
        maxValue: 5
      }
    ])

    render(
      <DynamicQuestionnaireForm
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const ratingButtons = screen.getAllByRole('button').filter(btn =>
      btn.textContent && /^[1-5]$/.test(btn.textContent)
    )
    expect(ratingButtons).toHaveLength(5)

    // Test rating selection
    await act(async () => {
      fireEvent.click(ratingButtons[2]) // Click rating 3
    })
    expect(ratingButtons[2]).toHaveClass('bg-primary')
  })

  it('validates required fields and shows errors', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Required field',
        type: QuestionType.Text,
        isRequired: true
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    await waitFor(() => {
      expect(screen.getByText('Required')).toBeInTheDocument()
    })

    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('validates email format', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Email field',
        type: QuestionType.Email,
        isRequired: true
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const emailInput = screen.getByRole('textbox')
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } })
    fireEvent.blur(emailInput)

    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    await waitFor(() => {
      expect(screen.getByText(/valid email/i)).toBeInTheDocument()
    })
  })

  it('validates number constraints', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Age field',
        type: QuestionType.Number,
        isRequired: true,
        minValue: 18,
        maxValue: 65
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const numberInput = screen.getByRole('spinbutton')
    fireEvent.change(numberInput, { target: { value: '10' } })

    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    await waitFor(() => {
      expect(screen.getByText(/between 18 and 65/i)).toBeInTheDocument()
    })
  })

  it('validates regex patterns', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Phone number',
        type: QuestionType.Phone,
        isRequired: true
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const phoneInput = screen.getByRole('textbox')
    fireEvent.change(phoneInput, { target: { value: 'invalid-phone' } })

    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    await waitFor(() => {
      expect(screen.getByText(/valid phone number/i)).toBeInTheDocument()
    })
  })

  it('submits form with valid data', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Your name',
        type: QuestionType.Text,
        isRequired: true
      },
      {
        questionText: 'Your age',
        type: QuestionType.Number,
        isRequired: false
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const nameInput = screen.getByRole('textbox')
    const ageInput = screen.getByRole('spinbutton')
    
    fireEvent.change(nameInput, { target: { value: 'John Doe' } })
    fireEvent.change(ageInput, { target: { value: '30' } })

    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith([
        { questionId: 'question-1', responseValue: 'John Doe' },
        { questionId: 'question-2', responseValue: '30' }
      ])
    })
  })

  it('pre-fills form with initial responses', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Your name',
        type: QuestionType.Text,
        isRequired: true
      }
    ])

    const initialResponses: QuestionResponse[] = [
      { questionId: 'question-1', responseValue: 'Initial Name' }
    ]

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        initialResponses={initialResponses}
        onSubmit={mockOnSubmit}
      />
    )

    const nameInput = screen.getByRole('textbox')
    expect(nameInput).toHaveValue('Initial Name')
  })

  it('saves draft when save draft button is clicked', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Your name',
        type: QuestionType.Text,
        isRequired: false
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
        onSaveDraft={mockOnSaveDraft}
      />
    )

    const nameInput = screen.getByRole('textbox')
    fireEvent.change(nameInput, { target: { value: 'Draft Name' } })

    const saveDraftButton = screen.getByRole('button', { name: /save draft/i })
    fireEvent.click(saveDraftButton)

    await waitFor(() => {
      expect(mockOnSaveDraft).toHaveBeenCalledWith([
        { questionId: 'question-1', responseValue: 'Draft Name' }
      ])
    })
  })

  it('auto-saves draft after 30 seconds', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Your name',
        type: QuestionType.Text,
        isRequired: false
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
        onSaveDraft={mockOnSaveDraft}
      />
    )

    const nameInput = screen.getByRole('textbox')
    fireEvent.change(nameInput, { target: { value: 'Auto Save Name' } })

    // Fast-forward 30 seconds
    jest.advanceTimersByTime(30000)

    await waitFor(() => {
      expect(mockOnSaveDraft).toHaveBeenCalledWith([
        { questionId: 'question-1', responseValue: 'Auto Save Name' }
      ])
    })
  })

  it('disables form when in read-only mode', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Your name',
        type: QuestionType.Text,
        isRequired: true
      },
      {
        questionText: 'Agree to terms',
        type: QuestionType.Boolean,
        isRequired: false
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
        isReadOnly={true}
      />
    )

    const textInput = screen.getByRole('textbox')
    const checkbox = screen.getByRole('checkbox')
    
    expect(textInput).toBeDisabled()
    expect(checkbox).toBeDisabled()
    expect(screen.queryByRole('button', { name: /submit/i })).not.toBeInTheDocument()
  })

  it('sorts questions by display order', () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Third question',
        type: QuestionType.Text,
        displayOrder: 3,
        isRequired: false
      },
      {
        questionText: 'First question',
        type: QuestionType.Text,
        displayOrder: 1,
        isRequired: false
      },
      {
        questionText: 'Second question',
        type: QuestionType.Text,
        displayOrder: 2,
        isRequired: false
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    // Look for labels that contain the numbered questions
    expect(screen.getByText('1. First question')).toBeInTheDocument()
    expect(screen.getByText('2. Second question')).toBeInTheDocument()
    expect(screen.getByText('3. Third question')).toBeInTheDocument()
  })

  it('shows loading state during submission', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Your name',
        type: QuestionType.Text,
        isRequired: true
      }
    ])

    const slowSubmit = jest.fn().mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    )

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={slowSubmit}
      />
    )

    const nameInput = screen.getByRole('textbox')
    fireEvent.change(nameInput, { target: { value: 'Test Name' } })

    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    // Wait for the loading state to appear
    await waitFor(() => {
      expect(screen.getByText(/submitting/i)).toBeInTheDocument()
    })
    
    expect(submitButton).toBeDisabled()

    await waitFor(() => {
      expect(slowSubmit).toHaveBeenCalled()
    })
  })

  it('handles URL validation correctly', async () => {
    const questionnaire = createTestQuestionnaire([
      {
        questionText: 'Website URL',
        type: QuestionType.Url,
        isRequired: true
      }
    ])

    render(
      <DynamicQuestionnaireForm 
        questionnaire={questionnaire}
        onSubmit={mockOnSubmit}
      />
    )

    const urlInput = screen.getByRole('textbox')
    
    // Test invalid URL
    fireEvent.change(urlInput, { target: { value: 'not-a-url' } })
    const submitButton = screen.getByRole('button', { name: /submit/i })

    await act(async () => {
      fireEvent.click(submitButton)
    })

    await waitFor(() => {
      expect(screen.getByText(/valid URL/i)).toBeInTheDocument()
    })

    // Test valid URL
    fireEvent.change(urlInput, { target: { value: 'https://example.com' } })
    fireEvent.click(submitButton)

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalled()
    })
  })
})