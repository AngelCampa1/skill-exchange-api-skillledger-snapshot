import React from 'react'
import { render, screen } from '@testing-library/react'
import { ProtectedComponent, withPermission, withRole } from '../ProtectedComponent'
import { usePermissions } from '../../hooks/usePermissions'

// Mock the usePermissions hook
jest.mock('../../hooks/usePermissions')
const mockUsePermissions = usePermissions as jest.MockedFunction<typeof usePermissions>

describe('ProtectedComponent', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('permission-based protection', () => {
    it('should render children when user has required permission', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn().mockReturnValue(true),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: ['VIEW_USERS'],
        userRoles: ['User']
      })

      render(
        <ProtectedComponent permission="VIEW_USERS">
          <div>Protected Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should show fallback when user lacks required permission', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn().mockReturnValue(false),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: []
      })

      render(
        <ProtectedComponent 
          permission="VIEW_USERS" 
          fallback={<div>Access Denied</div>}
        >
          <div>Protected Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Access Denied')).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should hide component when user lacks permission and hideWhenUnauthorized is true', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn().mockReturnValue(false),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: []
      })

      render(
        <ProtectedComponent 
          permission="VIEW_USERS" 
          fallback={<div>Access Denied</div>}
          hideWhenUnauthorized={true}
        >
          <div>Protected Content</div>
        </ProtectedComponent>
      )

      expect(screen.queryByText('Access Denied')).not.toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })
  })

  describe('multiple permissions', () => {
    it('should render when user has all required permissions', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn(),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn().mockReturnValue(true),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: ['VIEW_USERS', 'EDIT_USERS'],
        userRoles: []
      })

      render(
        <ProtectedComponent permissions={['VIEW_USERS', 'EDIT_USERS']}>
          <div>Protected Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should render when user has any of the required permissions', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn(),
        hasAnyPermission: jest.fn().mockReturnValue(true),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: ['VIEW_USERS'],
        userRoles: []
      })

      render(
        <ProtectedComponent anyPermissions={['VIEW_USERS', 'EDIT_USERS']}>
          <div>Protected Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })
  })

  describe('role-based protection', () => {
    it('should render when user has required role', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn(),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn().mockReturnValue(true),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: ['Admin']
      })

      render(
        <ProtectedComponent role="Admin">
          <div>Admin Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Admin Content')).toBeInTheDocument()
    })

    it('should render when user has any of the required roles', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn(),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn().mockReturnValue(true),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: ['Moderator']
      })

      render(
        <ProtectedComponent anyRoles={['Admin', 'Moderator']}>
          <div>Privileged Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Privileged Content')).toBeInTheDocument()
    })
  })

  describe('Higher-order components', () => {
    const TestComponent = () => <div>Test Component</div>

    it('withPermission should protect component with permission', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn().mockReturnValue(true),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: ['TEST_PERMISSION'],
        userRoles: []
      })

      const ProtectedTestComponent = withPermission(TestComponent, 'TEST_PERMISSION')
      
      render(<ProtectedTestComponent />)
      
      expect(screen.getByText('Test Component')).toBeInTheDocument()
    })

    it('withRole should protect component with role', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn(),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn().mockReturnValue(true),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: ['Admin']
      })

      const ProtectedTestComponent = withRole(TestComponent, 'Admin')
      
      render(<ProtectedTestComponent />)
      
      expect(screen.getByText('Test Component')).toBeInTheDocument()
    })

    it('should show fallback when protection fails', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn().mockReturnValue(false),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: []
      })

      const ProtectedTestComponent = withPermission(
        TestComponent, 
        'TEST_PERMISSION', 
        <div>No Permission</div>
      )
      
      render(<ProtectedTestComponent />)
      
      expect(screen.getByText('No Permission')).toBeInTheDocument()
      expect(screen.queryByText('Test Component')).not.toBeInTheDocument()
    })
  })

  describe('edge cases', () => {
    it('should render children when no protection rules are specified', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn(),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: []
      })

      render(
        <ProtectedComponent>
          <div>Unprotected Content</div>
        </ProtectedComponent>
      )

      expect(screen.getByText('Unprotected Content')).toBeInTheDocument()
    })

    it('should show null fallback when no fallback is provided', () => {
      mockUsePermissions.mockReturnValue({
        hasPermission: jest.fn().mockReturnValue(false),
        hasAnyPermission: jest.fn(),
        hasAllPermissions: jest.fn(),
        hasRole: jest.fn(),
        hasAnyRole: jest.fn(),
        isAdmin: jest.fn(),
        isModerator: jest.fn(),
        userPermissions: [],
        userRoles: []
      })

      const { container } = render(
        <ProtectedComponent permission="VIEW_USERS">
          <div>Protected Content</div>
        </ProtectedComponent>
      )

      expect(container).toBeEmptyDOMElement()
    })
  })
})