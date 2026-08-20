import { renderHook } from '@testing-library/react'
import { usePermissions } from '../usePermissions'
import { useAuth } from '../../contexts/AuthContext'

// Mock the AuthContext
jest.mock('../../contexts/AuthContext')
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>

describe('usePermissions', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('when user is not authenticated', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn()
      })
    })

    it('should return false for all permission checks', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.hasPermission('TEST_PERMISSION')).toBe(false)
      expect(result.current.hasAnyPermission(['TEST_PERMISSION'])).toBe(false)
      expect(result.current.hasAllPermissions(['TEST_PERMISSION'])).toBe(false)
      expect(result.current.hasRole('Admin')).toBe(false)
      expect(result.current.hasAnyRole(['Admin'])).toBe(false)
      expect(result.current.isAdmin()).toBe(false)
      expect(result.current.isModerator()).toBe(false)
    })

    it('should return empty arrays for user permissions and roles', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.userPermissions).toEqual([])
      expect(result.current.userRoles).toEqual([])
    })
  })

  describe('when user is authenticated', () => {
    const mockUser = {
      id: 'user-1',
      email: 'test@example.com',
      userName: 'test',
      emailVerified: true,
      phoneVerified: true,
      taxCompliant: true,
      status: 'Verified',
      roles: ['Admin', 'User'],
      permissions: ['VIEW_USERS', 'CREATE_PROJECTS', 'MANAGE_ROLES']
    }

    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn()
      })
    })

    describe('permission checks', () => {
      it('should return true for permissions user has', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasPermission('VIEW_USERS')).toBe(true)
        expect(result.current.hasPermission('CREATE_PROJECTS')).toBe(true)
        expect(result.current.hasPermission('MANAGE_ROLES')).toBe(true)
      })

      it('should return false for permissions user does not have', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasPermission('DELETE_USERS')).toBe(false)
        expect(result.current.hasPermission('NONEXISTENT_PERMISSION')).toBe(false)
      })

      it('should check multiple permissions with hasAnyPermission', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasAnyPermission(['VIEW_USERS', 'DELETE_USERS'])).toBe(true)
        expect(result.current.hasAnyPermission(['DELETE_USERS', 'MANAGE_CREDITS'])).toBe(false)
        expect(result.current.hasAnyPermission(['VIEW_USERS', 'CREATE_PROJECTS'])).toBe(true)
      })

      it('should check multiple permissions with hasAllPermissions', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasAllPermissions(['VIEW_USERS', 'CREATE_PROJECTS'])).toBe(true)
        expect(result.current.hasAllPermissions(['VIEW_USERS', 'DELETE_USERS'])).toBe(false)
        expect(result.current.hasAllPermissions(['VIEW_USERS', 'CREATE_PROJECTS', 'MANAGE_ROLES'])).toBe(true)
      })
    })

    describe('role checks', () => {
      it('should return true for roles user has', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasRole('Admin')).toBe(true)
        expect(result.current.hasRole('User')).toBe(true)
      })

      it('should return false for roles user does not have', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasRole('Moderator')).toBe(false)
        expect(result.current.hasRole('Support')).toBe(false)
      })

      it('should check multiple roles with hasAnyRole', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.hasAnyRole(['Admin', 'Moderator'])).toBe(true)
        expect(result.current.hasAnyRole(['Moderator', 'Support'])).toBe(false)
        expect(result.current.hasAnyRole(['User', 'Admin'])).toBe(true)
      })

      it('should correctly identify admin users', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.isAdmin()).toBe(true)
      })

      it('should correctly identify moderators', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.isModerator()).toBe(true) // Admin is also considered moderator
      })
    })

    describe('user data', () => {
      it('should return user permissions and roles', () => {
        const { result } = renderHook(() => usePermissions())

        expect(result.current.userPermissions).toEqual(['VIEW_USERS', 'CREATE_PROJECTS', 'MANAGE_ROLES'])
        expect(result.current.userRoles).toEqual(['Admin', 'User'])
      })
    })
  })

  describe('when user has moderator role', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-2',
          email: 'moderator@example.com',
          userName: 'moderator',
          emailVerified: true,
          phoneVerified: true,
          taxCompliant: true,
          status: 'Verified',
          roles: ['Moderator'],
          permissions: ['MODERATE_CONTENT', 'VIEW_REPORTS']
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn()
      })
    })

    it('should identify moderator as having moderation access', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.isModerator()).toBe(true)
      expect(result.current.isAdmin()).toBe(false)
    })
  })

  describe('when user has no roles', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-3',
          email: 'noroles@example.com',
          userName: 'noroles',
          emailVerified: true,
          phoneVerified: true,
          taxCompliant: true,
          status: 'Verified',
          roles: [],
          permissions: []
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn()
      })
    })

    it('should return false for all role and permission checks', () => {
      const { result } = renderHook(() => usePermissions())

      expect(result.current.hasRole('User')).toBe(false)
      expect(result.current.isAdmin()).toBe(false)
      expect(result.current.isModerator()).toBe(false)
      expect(result.current.hasPermission('ANY_PERMISSION')).toBe(false)
    })
  })
})