import { useAuth } from '../contexts/AuthContext'

/**
 * Custom hook for permission-based access control
 */
export function usePermissions() {
  const { user, isAuthenticated } = useAuth()

  // BUG-SEC-014 FIX: Add runtime validation for permissions and roles arrays
  // This prevents crashes if server returns malformed data
  const userPermissions = Array.isArray(user?.permissions) ? user.permissions : []
  const userRoles = Array.isArray(user?.roles) ? user.roles : []

  /**
   * Check if the current user has a specific permission
   */
  const hasPermission = (permission: string): boolean => {
    if (!isAuthenticated || userPermissions.length === 0) {
      return false
    }
    return userPermissions.includes(permission)
  }

  /**
   * Check if the current user has any of the specified permissions
   */
  const hasAnyPermission = (permissions: string[]): boolean => {
    if (!isAuthenticated || userPermissions.length === 0) {
      return false
    }
    return permissions.some(permission => userPermissions.includes(permission))
  }

  /**
   * Check if the current user has all of the specified permissions
   */
  const hasAllPermissions = (permissions: string[]): boolean => {
    if (!isAuthenticated || userPermissions.length === 0) {
      return false
    }
    return permissions.every(permission => userPermissions.includes(permission))
  }

  /**
   * Check if the current user has a specific role
   */
  const hasRole = (role: string): boolean => {
    if (!isAuthenticated || userRoles.length === 0) {
      return false
    }
    return userRoles.includes(role)
  }

  /**
   * Check if the current user has any of the specified roles
   */
  const hasAnyRole = (roles: string[]): boolean => {
    if (!isAuthenticated || userRoles.length === 0) {
      return false
    }
    return roles.some(role => userRoles.includes(role))
  }

  /**
   * Check if the current user is an admin
   */
  const isAdmin = (): boolean => {
    return hasRole('Admin')
  }

  /**
   * Check if the current user is a moderator or admin
   */
  const isModerator = (): boolean => {
    return hasAnyRole(['Admin', 'Moderator'])
  }

  return {
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    hasRole,
    hasAnyRole,
    isAdmin,
    isModerator,
    userPermissions,
    userRoles
  }
}