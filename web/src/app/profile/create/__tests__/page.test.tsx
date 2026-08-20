import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import CreateProfilePage from '../page'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'

// Mock dependencies
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

jest.mock('@/components/ProfileOnboardingWizard', () => ({
  __esModule: true,
  default: ({ onComplete, isLoading }: { onComplete: (data: any) => void; isLoading: boolean }) => (
    <div data-testid="profile-onboarding-wizard">
      <button
        data-testid="wizard-submit"
        onClick={() => onComplete(mockProfileData)}
        disabled={isLoading}
      >
        {isLoading ? 'Creating...' : 'Create Profile'}
      </button>
    </div>
  ),
}))

const mockUseAuth = useAuth as jest.Mock
const mockUseRouter = useRouter as jest.Mock
const mockPush = jest.fn()
const mockFetch = jest.fn()

const mockProfileData = {
  basicInfo: {
    firstName: 'John',
    lastName: 'Doe',
    bio: 'Software developer with 5 years of experience',
    location: 'San Francisco, CA',
  },
  isPublic: true,
  skills: [
    { name: 'JavaScript', proficiencyLevel: 'Advanced', yearsOfExperience: 5 },
    { name: 'React', proficiencyLevel: 'Expert', yearsOfExperience: 4 },
    { name: 'NewSkill', proficiencyLevel: 'Beginner', yearsOfExperience: 1 },
  ],
  experiences: [
    {
      type: 'Work',
      title: 'Senior Developer',
      organization: 'Tech Corp',
      location: 'SF, CA',
      startDate: '2020-01-01',
      endDate: '2023-12-31',
      isCurrent: false,
      description: 'Developed web applications',
    },
  ],
  photo: {
    file: new File(['photo content'], 'photo.jpg', { type: 'image/jpeg' }),
  },
}

describe('CreateProfilePage', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    global.fetch = mockFetch
    mockUseRouter.mockReturnValue({ push: mockPush })
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: 'user-123', email: 'test@example.com' },
    })
    jest.useFakeTimers()
  })

  afterEach(() => {
    jest.runOnlyPendingTimers()
    jest.useRealTimers()
  })

  // ============================================
  // Initial Render (3 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render ProfileOnboardingWizard', () => {
      render(<CreateProfilePage />)

      expect(screen.getByTestId('profile-onboarding-wizard')).toBeInTheDocument()
    })

    it('should render create profile button', () => {
      render(<CreateProfilePage />)

      expect(screen.getByTestId('wizard-submit')).toBeInTheDocument()
      expect(screen.getByText('Create Profile')).toBeInTheDocument()
    })

    it('should not display error or success message initially', () => {
      render(<CreateProfilePage />)

      expect(screen.queryByText(/Profile Created!/)).not.toBeInTheDocument()
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Authentication State (3 tests)
  // ============================================
  describe('Authentication State', () => {
    it('should show error when user is not authenticated', async () => {
      const user = userEvent.setup({ delay: null })
      mockUseAuth.mockReturnValue({
        isAuthenticated: false,
        user: null,
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('You must be logged in to create a profile')).toBeInTheDocument()
      })
    })

    it('should not call API when unauthenticated', async () => {
      const user = userEvent.setup({ delay: null })
      mockUseAuth.mockReturnValue({
        isAuthenticated: false,
        user: null,
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('You must be logged in to create a profile')).toBeInTheDocument()
      })

      expect(mockFetch).not.toHaveBeenCalled()
    })

    it('should proceed with profile creation when authenticated', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true, data: { id: 'profile-123' } }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token')
      })
    })
  })

  // ============================================
  // Profile Creation - Basic Info (5 tests)
  // ============================================
  describe('Profile Creation - Basic Info', () => {
    it('should fetch CSRF token before creating profile', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token')
      })
    })

    it('should create profile with basic info', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        // BUG-PROFILE-002: Implementation checks for existing profile first
        if (url === '/api/profile/me') {
          return Promise.resolve({
            ok: false, // No existing profile - triggers POST
            status: 404,
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true, data: { id: 'profile-123' } }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/profile',
          expect.objectContaining({
            method: 'POST',
            headers: expect.objectContaining({
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': 'csrf-123',
            }),
            credentials: 'include',
            body: expect.stringContaining('John'),
          })
        )
      })
    })

    it('should include isPublic flag in profile creation', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const profileCall = mockFetch.mock.calls.find((call) => call[0] === '/api/profile')
        expect(profileCall).toBeTruthy()
        const body = JSON.parse(profileCall![1].body)
        expect(body.isPublic).toBe(true)
      })
    })

    it('should show error when profile creation fails', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: false,
            json: async () => ({ success: false, message: 'Profile already exists' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({}),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Profile already exists')).toBeInTheDocument()
      })
    })

    it('should show button loading state during creation', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        return new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: async () => ({ success: true }),
            } as Response)
          }, 100)
        })
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Button should show loading state
      expect(screen.getByText('Creating...')).toBeInTheDocument()
      expect(submitButton).toBeDisabled()
    })
  })

  // ============================================
  // Skills Addition Flow (8 tests)
  // ============================================
  describe('Skills Addition Flow', () => {
    it('should search for skills before adding them', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [{ id: 'skill-123', name: 'JavaScript' }] }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/skill?searchTerm=JavaScript'),
          expect.objectContaining({
            credentials: 'include',
          })
        )
      })
    })

    it('should use exact match when skill is found', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=JavaScript')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({
              skills: [
                { id: 'skill-exact', name: 'JavaScript' },
                { id: 'skill-other', name: 'JavaScriptMVC' },
              ],
            }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const mySkillsCalls = mockFetch.mock.calls.filter((call) => call[0] === '/api/skill/my-skills')
        expect(mySkillsCalls.length).toBeGreaterThan(0)
        const firstCall = mySkillsCalls[0]
        const body = JSON.parse(firstCall[1].body)
        expect(body.skillId).toBe('skill-exact')
      })
    })

    it('should create skill if not found', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url, options?: RequestInit) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=NewSkill')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [] }),
          } as Response)
        }
        if (url === '/api/skill' && options?.method === 'POST') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true, data: { id: 'new-skill-123' } }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const createSkillCalls = mockFetch.mock.calls.filter(
          (call) => call[0] === '/api/skill' && call[1]?.method === 'POST'
        )
        expect(createSkillCalls.length).toBeGreaterThan(0)
        // Find the NewSkill creation call
        const newSkillCall = createSkillCalls.find((call) => {
          const body = JSON.parse(call[1].body)
          return body.name === 'NewSkill'
        })
        expect(newSkillCall).toBeTruthy()
      })
    })

    it('should add user skill with correct proficiency mapping', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [{ id: 'skill-123', name: 'JavaScript' }] }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const mySkillsCall = mockFetch.mock.calls.find((call) => call[0] === '/api/skill/my-skills')
        expect(mySkillsCall).toBeTruthy()
        const body = JSON.parse(mySkillsCall![1].body)
        expect(body.proficiency).toBe(3) // Advanced = 3
        expect(body.yearsOfExperience).toBe(5)
      })
    })

    it('should map Expert proficiency level correctly', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=React')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [{ id: 'skill-react', name: 'React' }] }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const reactSkillCall = mockFetch.mock.calls.find(
          (call) =>
            call[0] === '/api/skill/my-skills' &&
            call[1]?.body &&
            JSON.parse(call[1].body).skillId === 'skill-react'
        )
        expect(reactSkillCall).toBeTruthy()
        const body = JSON.parse(reactSkillCall![1].body)
        expect(body.proficiency).toBe(4) // Expert = 4
      })
    })

    it('should continue with other skills if one fails', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=JavaScript')) {
          return Promise.resolve({
            ok: false,
            json: async () => ({ error: 'Skill search failed' }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=React')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [{ id: 'skill-react', name: 'React' }] }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Should still add React skill even though JavaScript failed
      await waitFor(() => {
        const reactSkillCall = mockFetch.mock.calls.find(
          (call) =>
            call[0] === '/api/skill/my-skills' &&
            call[1]?.body &&
            JSON.parse(call[1].body).skillId === 'skill-react'
        )
        expect(reactSkillCall).toBeTruthy()
      })
    })

    it('should include CSRF token in skill creation request', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url, options?: RequestInit) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'test-csrf-token' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [] }),
          } as Response)
        }
        if (url === '/api/skill' && options?.method === 'POST') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ data: { id: 'new-skill-123' } }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const createSkillCall = mockFetch.mock.calls.find(
          (call) => call[0] === '/api/skill' && call[1]?.method === 'POST'
        )
        expect(createSkillCall).toBeTruthy()
        expect(createSkillCall![1].headers['X-CSRF-TOKEN']).toBe('test-csrf-token')
      })
    })

    it('should process all three skills from profile data', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [{ id: 'skill-123', name: 'Test' }] }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Should search for all three skills (may search multiple times per skill)
      await waitFor(() => {
        const skillSearchCalls = mockFetch.mock.calls.filter((call) =>
          call[0].includes('/api/skill?searchTerm=')
        )
        // Should have at least 3 skill searches (one per skill minimum)
        expect(skillSearchCalls.length).toBeGreaterThanOrEqual(3)
      })
    })
  })

  // ============================================
  // Experiences Addition (4 tests)
  // ============================================
  describe('Experiences Addition', () => {
    it('should add experiences after profile creation', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/experience') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/experience',
          expect.objectContaining({
            method: 'POST',
            headers: expect.objectContaining({
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': 'csrf-123',
            }),
          })
        )
      })
    })

    it('should include all experience fields', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/experience') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const experienceCall = mockFetch.mock.calls.find((call) => call[0] === '/api/experience')
        expect(experienceCall).toBeTruthy()
        const body = JSON.parse(experienceCall![1].body)
        expect(body.type).toBe('Work')
        expect(body.title).toBe('Senior Developer')
        expect(body.organization).toBe('Tech Corp')
        expect(body.isCurrent).toBe(false)
      })
    })

    it('should add experience with CSRF token', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'test-csrf-456' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/experience') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const experienceCall = mockFetch.mock.calls.find((call) => call[0] === '/api/experience')
        expect(experienceCall).toBeTruthy()
        expect(experienceCall![1].headers['X-CSRF-TOKEN']).toBe('test-csrf-456')
      })
    })

    it('should add experience from profile data', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/experience') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Should add the one experience from mockProfileData
      await waitFor(() => {
        const experienceCalls = mockFetch.mock.calls.filter((call) => call[0] === '/api/experience')
        expect(experienceCalls.length).toBe(1)
      })
    })
  })

  // ============================================
  // Photo Upload (4 tests)
  // ============================================
  describe('Photo Upload', () => {
    it('should upload avatar after profile creation', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/profile/avatar',
          expect.objectContaining({
            method: 'PUT',
            headers: expect.objectContaining({
              'X-CSRF-TOKEN': 'csrf-123',
            }),
          })
        )
      })
    })

    it('should send avatar as FormData', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const avatarCall = mockFetch.mock.calls.find((call) => call[0] === '/api/profile/avatar')
        expect(avatarCall).toBeTruthy()
        expect(avatarCall![1].body).toBeInstanceOf(FormData)
      })
    })

    it('should include credentials in avatar upload', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        const avatarCall = mockFetch.mock.calls.find((call) => call[0] === '/api/profile/avatar')
        expect(avatarCall).toBeTruthy()
        expect(avatarCall![1].credentials).toBe('include')
      })
    })

    it('should upload photo from profile data', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Should call avatar upload since mockProfileData has photo
      await waitFor(() => {
        const avatarCalls = mockFetch.mock.calls.filter((call) => call[0] === '/api/profile/avatar')
        expect(avatarCalls.length).toBe(1)
      })
    })
  })

  // ============================================
  // Success State (4 tests)
  // ============================================
  describe('Success State', () => {
    it('should show success message after profile creation', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Profile Created!')).toBeInTheDocument()
      })
    })

    it('should show success checkmark icon', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('✓')).toBeInTheDocument()
      })
    })

    it('should show redirect message', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(
          screen.getByText('Your profile has been created successfully. Redirecting to your profile page...')
        ).toBeInTheDocument()
      })
    })

    it('should redirect to profile page after 2 seconds', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Profile Created!')).toBeInTheDocument()
      })

      // Fast-forward 2 seconds
      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/profile/me')
      })
    })
  })

  // ============================================
  // Error Handling (6 tests)
  // ============================================
  describe('Error Handling', () => {
    it('should display error message when shown', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: false,
            json: async () => ({ success: false, message: 'Database error' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({}),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Database error')).toBeInTheDocument()
      })
    })

    it('should show error with X icon', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: false,
            json: async () => ({ success: false, message: 'Error occurred' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({}),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('✗')).toBeInTheDocument()
      })
    })

    it('should show generic error on unexpected exception', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation(() => {
        throw new Error('Network failure')
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('An unexpected error occurred')).toBeInTheDocument()
      })
    })

    it('should clear error when retrying submission', async () => {
      const user = userEvent.setup({ delay: null })

      let attemptCount = 0
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          attemptCount++
          if (attemptCount === 1) {
            return Promise.resolve({
              ok: false,
              json: async () => ({ success: false, message: 'First attempt failed' }),
            } as Response)
          }
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')

      // First attempt - should fail
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('First attempt failed')).toBeInTheDocument()
      })

      // Second attempt - should succeed and clear error
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.queryByText('First attempt failed')).not.toBeInTheDocument()
        expect(screen.getByText('Profile Created!')).toBeInTheDocument()
      })
    })

    it('should stop processing if profile creation fails', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: false,
            json: async () => ({ success: false, message: 'Profile creation failed' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Profile creation failed')).toBeInTheDocument()
      })

      // Should not call skills or experience endpoints
      const skillCalls = mockFetch.mock.calls.filter((call) => call[0].includes('/api/skill'))
      const experienceCalls = mockFetch.mock.calls.filter((call) => call[0] === '/api/experience')

      expect(skillCalls.length).toBe(0)
      expect(experienceCalls.length).toBe(0)
    })

    it('should re-enable button after error', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: false,
            json: async () => ({ success: false, message: 'Error' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({}),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Error')).toBeInTheDocument()
      })

      // Button should be enabled again
      expect(submitButton).not.toBeDisabled()
      expect(screen.getByText('Create Profile')).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (3 tests)
  // ============================================
  describe('Integration', () => {
    it('should handle complete profile creation flow', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url, options?: RequestInit) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-token-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true, data: { id: 'profile-123' } }),
          } as Response)
        }
        if (url.includes('/api/skill?searchTerm=')) {
          const searchTerm = url.split('searchTerm=')[1].split('&')[0]
          const decodedTerm = decodeURIComponent(searchTerm)
          if (decodedTerm === 'JavaScript' || decodedTerm === 'React') {
            return Promise.resolve({
              ok: true,
              json: async () => ({
                skills: [{ id: `skill-${decodedTerm.toLowerCase()}`, name: decodedTerm }],
              }),
            } as Response)
          }
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [] }),
          } as Response)
        }
        if (url === '/api/skill' && options?.method === 'POST') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ data: { id: 'new-skill-123' } }),
          } as Response)
        }
        if (url === '/api/skill/my-skills') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/experience') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Should show loading state
      expect(screen.getByText('Creating...')).toBeInTheDocument()

      // Wait for all API calls to complete
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token')
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/profile', expect.any(Object))
      })

      await waitFor(() => {
        const skillCalls = mockFetch.mock.calls.filter((call) => call[0] === '/api/skill/my-skills')
        // Should add skills from mockProfileData (at least 1)
        expect(skillCalls.length).toBeGreaterThan(0)
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/experience', expect.any(Object))
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/profile/avatar', expect.any(Object))
      })

      // Should show success message
      await waitFor(() => {
        expect(screen.getByText('Profile Created!')).toBeInTheDocument()
      })

      // Should redirect after 2 seconds
      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/profile/me')
      })
    })

    it('should call all API endpoints in correct sequence', async () => {
      const user = userEvent.setup({ delay: null })
      const callOrder: string[] = []

      mockFetch.mockImplementation((url) => {
        callOrder.push(url.toString())

        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        // BUG-PROFILE-002: Implementation checks for existing profile first
        if (url === '/api/profile/me') {
          return Promise.resolve({
            ok: false, // No existing profile
            status: 404,
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url.includes('/api/skill')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ skills: [{ id: 'skill-123', name: 'Test' }], success: true }),
          } as Response)
        }
        if (url === '/api/experience') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Profile Created!')).toBeInTheDocument()
      })

      // Verify CSRF was called first
      expect(callOrder[0]).toBe('/api/auth/csrf-token')

      // BUG-PROFILE-002: Profile existence check is second call
      expect(callOrder[1]).toBe('/api/profile/me')

      // Verify profile was created (should be third call)
      expect(callOrder[2]).toBe('/api/profile')

      // Verify all endpoints were called
      expect(callOrder.some((url) => url.includes('/api/skill'))).toBe(true)
      expect(callOrder.some((url) => url === '/api/experience')).toBe(true)
      expect(callOrder.some((url) => url === '/api/profile/avatar')).toBe(true)
    })

    it('should handle graceful failures in skill and experience addition', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'csrf-123' }),
          } as Response)
        }
        if (url === '/api/profile') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        // Skills and experiences may fail, but profile creation succeeds
        if (url.includes('/api/skill') || url === '/api/experience') {
          return Promise.resolve({
            ok: false,
            json: async () => ({ error: 'Service unavailable' }),
          } as Response)
        }
        if (url === '/api/profile/avatar') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<CreateProfilePage />)

      const submitButton = screen.getByTestId('wizard-submit')
      await user.click(submitButton)

      // Should still show success (profile was created)
      await waitFor(() => {
        expect(screen.getByText('Profile Created!')).toBeInTheDocument()
      })

      // Should still redirect
      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/profile/me')
      })
    })
  })
})
