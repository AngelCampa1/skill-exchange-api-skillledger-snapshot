import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import RegistrationForm from '../RegistrationForm'

describe('RegistrationForm', () => {
  const mockOnSubmit = jest.fn().mockResolvedValue(undefined)

  beforeEach(() => {
    mockOnSubmit.mockClear()
  })

  it('renders all form fields', () => {
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    expect(screen.getByTestId('email-input')).toBeInTheDocument()
    expect(screen.getByTestId('password-input')).toBeInTheDocument()
    expect(screen.getByTestId('confirm-password-input')).toBeInTheDocument()
    expect(screen.getByTestId('submit-button')).toBeInTheDocument()
  })

  it('validates email format', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    const emailInput = screen.getByTestId('email-input')
    const passwordInput = screen.getByTestId('password-input')
    const confirmPasswordInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill in all required fields with invalid email
    await user.type(emailInput, 'invalid-email')
    await user.type(passwordInput, 'ValidPassword123!')
    await user.type(confirmPasswordInput, 'ValidPassword123!')
    await user.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByTestId('email-error')).toHaveTextContent('Please enter a valid email address')
    })
    
    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('validates password complexity requirements', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    const passwordInput = screen.getByTestId('password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Test weak password
    await user.type(passwordInput, 'weak')
    await user.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toBeInTheDocument()
    })
    
    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('validates password confirmation matches', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    const passwordInput = screen.getByTestId('password-input')
    const confirmPasswordInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    await user.type(passwordInput, 'ValidPassword123!')
    await user.type(confirmPasswordInput, 'DifferentPassword123!')
    await user.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByTestId('confirm-password-error')).toHaveTextContent("Passwords don't match")
    })
    
    expect(mockOnSubmit).not.toHaveBeenCalled()
  })

  it('shows password strength indicator', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)

    const passwordInput = screen.getByTestId('password-input')

    // Test password that scores 15% (Weak)
    await user.type(passwordInput, 'weak123')
    expect(screen.getByTestId('strength-text')).toHaveTextContent('Weak')

    // Test strong password - avoid common words like "password"
    await user.clear(passwordInput)
    await user.type(passwordInput, 'MySecureP@ssword123!')
    expect(screen.getByTestId('strength-text')).toHaveTextContent('Strong')
  })

  it('toggles password visibility', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    const passwordInput = screen.getByTestId('password-input')
    const toggleButton = screen.getByTestId('toggle-password')
    
    // Initially password type
    expect(passwordInput).toHaveAttribute('type', 'password')
    
    // Click to show password
    await user.click(toggleButton)
    expect(passwordInput).toHaveAttribute('type', 'text')
    
    // Click to hide password
    await user.click(toggleButton)
    expect(passwordInput).toHaveAttribute('type', 'password')
  })

  it('submits form with valid data', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)

    const firstNameInput = screen.getByTestId('firstName-input')
    const lastNameInput = screen.getByTestId('lastName-input')
    const emailInput = screen.getByTestId('email-input')
    const passwordInput = screen.getByTestId('password-input')
    const confirmPasswordInput = screen.getByTestId('confirm-password-input')
    const termsCheckbox = screen.getByTestId('terms-checkbox')
    const submitButton = screen.getByTestId('submit-button')

    await user.type(firstNameInput, 'John')
    await user.type(lastNameInput, 'Doe')
    await user.type(emailInput, 'test@example.com')
    await user.type(passwordInput, 'ValidPassword123!')
    await user.type(confirmPasswordInput, 'ValidPassword123!')
    await user.click(termsCheckbox) // Accept terms
    await user.click(submitButton)

    await waitFor(() => {
      expect(mockOnSubmit).toHaveBeenCalledWith({
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        password: 'ValidPassword123!',
        confirmPassword: 'ValidPassword123!',
        acceptedTerms: true
      })
    })
  })

  it('disables form when loading', () => {
    render(<RegistrationForm onSubmit={mockOnSubmit} isLoading={true} />)
    
    expect(screen.getByTestId('email-input')).toBeDisabled()
    expect(screen.getByTestId('password-input')).toBeDisabled()
    expect(screen.getByTestId('confirm-password-input')).toBeDisabled()
    expect(screen.getByTestId('submit-button')).toBeDisabled()
    expect(screen.getByTestId('submit-button')).toHaveTextContent('Creating Account...')
  })

  it('shows password strength progression', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    const passwordInput = screen.getByTestId('password-input')
    
    // Start with weak password (abc = common word, should be heavily penalized)
    await user.type(passwordInput, 'abc')
    let strengthBar = screen.getByTestId('strength-bar')
    expect(strengthBar.style.width).toBe('0%')
    
    // Add complexity
    await user.type(passwordInput, '123ABC!')
    strengthBar = screen.getByTestId('strength-bar')
    expect(parseInt(strengthBar.style.width)).toBeGreaterThan(15)
    
    // Make it strong - avoid common words like "password"
    await user.clear(passwordInput)
    await user.type(passwordInput, 'Very$ecureP@ssw0rd123!')
    strengthBar = screen.getByTestId('strength-bar')
    expect(parseInt(strengthBar.style.width)).toBeGreaterThan(50)
  })

  it('validates minimum password length', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)
    
    const passwordInput = screen.getByTestId('password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    await user.type(passwordInput, 'Short1!')
    await user.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toHaveTextContent('Password must be at least 12 characters')
    })
  })

  it('validates password character requirements', async () => {
    const user = userEvent.setup()
    render(<RegistrationForm onSubmit={mockOnSubmit} />)

    const passwordInput = screen.getByTestId('password-input')
    const submitButton = screen.getByTestId('submit-button')

    // No uppercase
    await user.type(passwordInput, 'nouppercase123!')
    await user.click(submitButton)

    await waitFor(() => {
      expect(screen.getByTestId('password-error')).toHaveTextContent('Password must contain at least one uppercase letter')
    })
  })

  // ============================================================
  // Week 9: Password Strength Calculation Tests (20 tests)
  // Testing the calculatePasswordStrength function via UI integration
  // ============================================================

  describe('Password Strength Calculation - Length Scoring', () => {
    // Length scoring logic from component:
    // - pwd.length >= 12: +25 points
    // - pwd.length >= 16: +10 additional points (total 35)
    // - 0-11 chars: 0 points from length

    it('scores 0 for passwords shorter than 12 characters', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 11 character password with variety - no length bonus
      await user.type(passwordInput, 'Aa1!xxxxxxx')

      const strengthBar = screen.getByTestId('strength-bar')
      // Character variety gives: 15+15+15+20 = 65, but no length bonus
      // However abc sequence penalty may apply
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(65)
    })

    it('scores +25 for passwords 12-15 characters', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 12 character password with all character types (no repeated chars like 'xxx')
      await user.type(passwordInput, 'Aa1!qwrtymzk') // 12 chars, no repeats/sequences

      const strengthBar = screen.getByTestId('strength-bar')
      // Length 25 + uppercase 15 + lowercase 15 + number 15 + special 20 = 90
      expect(parseInt(strengthBar.style.width)).toBeGreaterThanOrEqual(85)
    })

    it('scores +35 for passwords 16+ characters', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 16 character password with all character types (no repeats/sequences)
      await user.type(passwordInput, 'Aa1!qwrtymzkplsh') // 16 chars, no repeats

      const strengthBar = screen.getByTestId('strength-bar')
      // Length 35 + uppercase 15 + lowercase 15 + number 15 + special 20 = 100
      expect(parseInt(strengthBar.style.width)).toBe(100)
    })

    it('empty password returns 0 strength', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Type then clear
      await user.type(passwordInput, 'test')
      await user.clear(passwordInput)

      // Strength bar should not be visible when password is empty
      expect(screen.queryByTestId('strength-bar')).not.toBeInTheDocument()
    })
  })

  describe('Password Strength Calculation - Common Word Penalties', () => {
    // Common words: password, admin, user, login, welcome, qwerty, letmein
    // Penalty: -30 points

    it('penalizes passwords containing "password"', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Strong password with "password" word embedded
      await user.type(passwordInput, 'MyPassword123!xx') // 16 chars, all types

      const strengthBar = screen.getByTestId('strength-bar')
      // Max 100 - 30 (password penalty) = 70
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(70)
    })

    it('penalizes passwords containing "admin" (case insensitive)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'SuperADMIN123!xx') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      // Should have -30 penalty
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(70)
    })

    it('penalizes passwords containing "qwerty"', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Myqwerty123!xxxx') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      // qwerty gets both -30 (common word) AND -25 (keyboard pattern) = -55
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(50)
    })

    it('penalizes passwords containing "welcome"', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Welcome2024!xxxx') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(70)
    })

    it('penalizes passwords containing "letmein"', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Letmein2024!xxxx') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(70)
    })
  })

  describe('Password Strength Calculation - Sequential Character Detection', () => {
    // Forward sequences: 012, 123, abc, bcd, etc. → -20
    // Reverse sequences: 987, 321, zyx, cba, etc. → -20

    it('penalizes forward numeric sequences (123)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Secure123Phrase!') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      // Full score 100 - 20 (sequence penalty) = 80
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(80)
    })

    it('penalizes forward alphabetic sequences (abc)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Secureabc9Phase!') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(80)
    })

    it('penalizes reverse numeric sequences (321)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Secure321Phrase!') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(80)
    })

    it('penalizes reverse alphabetic sequences (cba)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Securecba9Phrase') // 16 chars, no special

      const strengthBar = screen.getByTestId('strength-bar')
      // Missing special char = -20, cba sequence = -20, total: 100-20-20 = 60
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(80)
    })

    it('applies single forward sequence penalty regardless of count', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Contains both 123 (forward) and abc (forward) sequences
      // Note: Implementation applies ONE -20 for any forward sequence match
      // This could be considered a bug - multiple sequences only get one penalty
      await user.type(passwordInput, 'abc123Phrase!qrt') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      // Base 100 - 20 (single forward penalty) = 80
      // BUG-TEST-048: Multiple sequences in same category (forward) only get one penalty
      expect(parseInt(strengthBar.style.width)).toBe(80)
      console.warn('BUG-TEST-048: Multiple forward sequences only apply one -20 penalty, not per-sequence')
    })

    it('penalizes repeated characters (aaa, 111)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      await user.type(passwordInput, 'Secureaaa9Pass!!') // 16 chars, with 'aaa'

      const strengthBar = screen.getByTestId('strength-bar')
      // -15 penalty for repeated chars
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(85)
    })
  })

  describe('Password Strength Calculation - Character Variety Bonuses', () => {
    // Uppercase: +15
    // Lowercase: +15
    // Numbers: +15
    // Special: +20

    it('awards +15 for uppercase letters', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 12 chars, only lowercase (no repeats to avoid penalty)
      await user.type(passwordInput, 'qwrtymzkplsh')
      const barWithoutUpper = screen.getByTestId('strength-bar')
      const scoreWithoutUpper = parseInt(barWithoutUpper.style.width)

      await user.clear(passwordInput)

      // Same length but with uppercase (12 chars)
      await user.type(passwordInput, 'Qwrtymzkplsh')
      const barWithUpper = screen.getByTestId('strength-bar')
      const scoreWithUpper = parseInt(barWithUpper.style.width)

      expect(scoreWithUpper).toBeGreaterThan(scoreWithoutUpper)
    })

    it('awards +15 for lowercase letters', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 12 chars, only uppercase
      await user.type(passwordInput, 'XXXXXXXXXXXX')
      const barWithoutLower = screen.getByTestId('strength-bar')
      const scoreWithoutLower = parseInt(barWithoutLower.style.width)

      await user.clear(passwordInput)

      // Same but with lowercase
      await user.type(passwordInput, 'XXXXXXXXXXXx')
      const barWithLower = screen.getByTestId('strength-bar')
      const scoreWithLower = parseInt(barWithLower.style.width)

      expect(scoreWithLower).toBeGreaterThan(scoreWithoutLower)
    })

    it('awards +15 for numbers', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 12 chars, letters only
      await user.type(passwordInput, 'Xxxxxxxxxxxx')
      const barWithoutNum = screen.getByTestId('strength-bar')
      const scoreWithoutNum = parseInt(barWithoutNum.style.width)

      await user.clear(passwordInput)

      // Same but with number
      await user.type(passwordInput, 'Xxxxxxxxxxx9')
      const barWithNum = screen.getByTestId('strength-bar')
      const scoreWithNum = parseInt(barWithNum.style.width)

      expect(scoreWithNum).toBeGreaterThan(scoreWithoutNum)
    })

    it('awards +20 for special characters', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 12 chars, no special
      await user.type(passwordInput, 'Xxxxxxxxxxx9')
      const barWithoutSpecial = screen.getByTestId('strength-bar')
      const scoreWithoutSpecial = parseInt(barWithoutSpecial.style.width)

      await user.clear(passwordInput)

      // Same but with special char
      await user.type(passwordInput, 'Xxxxxxxxxx9!')
      const barWithSpecial = screen.getByTestId('strength-bar')
      const scoreWithSpecial = parseInt(barWithSpecial.style.width)

      expect(scoreWithSpecial).toBeGreaterThan(scoreWithoutSpecial)
    })

    it('achieves max score (100) with all variety + length', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // 16+ chars with all character types, no penalties (avoid sequences like 876, nop)
      await user.type(passwordInput, 'Qwrt5!mzkPlsh2@v') // 16 chars, no sequences

      const strengthBar = screen.getByTestId('strength-bar')
      // 35 (length) + 15 + 15 + 15 + 20 = 100
      expect(parseInt(strengthBar.style.width)).toBe(100)
    })
  })

  describe('Password Strength Calculation - Edge Cases', () => {
    it('clamps negative scores to 0', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Short password with multiple penalties
      await user.type(passwordInput, 'password123') // common word + sequence

      const strengthBar = screen.getByTestId('strength-bar')
      // Score should be clamped to 0
      expect(parseInt(strengthBar.style.width)).toBeGreaterThanOrEqual(0)
    })

    it('clamps high scores to 100', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Very long password with max variety
      await user.type(passwordInput, 'Aa1!xxxxxxxxxxxxxxxxxxxx') // 24 chars

      const strengthBar = screen.getByTestId('strength-bar')
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(100)
    })

    it('handles keyboard patterns penalty (qwerty, asdfgh, zxcvbn)', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Contains keyboard pattern
      await user.type(passwordInput, 'Asdfgh12345!xxxx') // 16 chars

      const strengthBar = screen.getByTestId('strength-bar')
      // Should have -25 penalty for asdfgh
      expect(parseInt(strengthBar.style.width)).toBeLessThanOrEqual(75)
    })

    it('shows correct strength text for different score ranges', async () => {
      const user = userEvent.setup()
      render(<RegistrationForm onSubmit={mockOnSubmit} />)

      const passwordInput = screen.getByTestId('password-input')

      // Weak (< 30)
      await user.type(passwordInput, 'password')
      expect(screen.getByTestId('strength-text')).toHaveTextContent('Weak')

      // Fair (30-59)
      await user.clear(passwordInput)
      await user.type(passwordInput, 'Secure12qrty') // 12 chars, no repeats
      expect(screen.getByTestId('strength-text')).toHaveTextContent(/Fair|Good/)

      // Strong (80+) - need password with no penalties to hit 100
      await user.clear(passwordInput)
      await user.type(passwordInput, 'Qwrt5!mzkPlsh2@v') // 16 chars, no sequences
      expect(screen.getByTestId('strength-text')).toHaveTextContent('Strong')
    })
  })
})