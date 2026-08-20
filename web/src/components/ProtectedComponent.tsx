import React from 'react'
import { usePermissions } from '../hooks/usePermissions'

interface ProtectedComponentProps {
  children: React.ReactNode
  /** Required permission to show the component */
  permission?: string
  /** Required permissions (user must have ALL) */
  permissions?: string[]
  /** Required permissions (user must have ANY) */
  anyPermissions?: string[]
  /** Required role to show the component */
  role?: string
  /** Required roles (user must have ANY) */
  anyRoles?: string[]
  /** Component to show when user doesn't have permission */
  fallback?: React.ReactNode
  /** Whether to completely hide the component (vs showing fallback) */
  hideWhenUnauthorized?: boolean
}

/**
 * Component that conditionally renders children based on user permissions/roles
 */
export function ProtectedComponent({
  children,
  permission,
  permissions,
  anyPermissions,
  role,
  anyRoles,
  fallback = null,
  hideWhenUnauthorized = false
}: ProtectedComponentProps) {
  const {
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
    hasRole,
    hasAnyRole
  } = usePermissions()

  // Check permission requirements
  if (permission && !hasPermission(permission)) {
    return hideWhenUnauthorized ? null : <>{fallback}</>
  }

  if (permissions && !hasAllPermissions(permissions)) {
    return hideWhenUnauthorized ? null : <>{fallback}</>
  }

  if (anyPermissions && !hasAnyPermission(anyPermissions)) {
    return hideWhenUnauthorized ? null : <>{fallback}</>
  }

  // Check role requirements
  if (role && !hasRole(role)) {
    return hideWhenUnauthorized ? null : <>{fallback}</>
  }

  if (anyRoles && !hasAnyRole(anyRoles)) {
    return hideWhenUnauthorized ? null : <>{fallback}</>
  }

  return <>{children}</>
}

/**
 * Higher-order component for protecting components with permissions
 */
export function withPermission<P extends object>(
  Component: React.ComponentType<P>,
  requiredPermission: string,
  fallback?: React.ReactNode
) {
  return function ProtectedComponentWrapper(props: P) {
    return (
      <ProtectedComponent permission={requiredPermission} fallback={fallback}>
        <Component {...props} />
      </ProtectedComponent>
    )
  }
}

/**
 * Higher-order component for protecting components with roles
 */
export function withRole<P extends object>(
  Component: React.ComponentType<P>,
  requiredRole: string,
  fallback?: React.ReactNode
) {
  return function ProtectedComponentWrapper(props: P) {
    return (
      <ProtectedComponent role={requiredRole} fallback={fallback}>
        <Component {...props} />
      </ProtectedComponent>
    )
  }
}