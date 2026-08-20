'use client'

import React from 'react'
import { usePermissions } from '../hooks/usePermissions'

/**
 * Component that displays the current user's roles and permissions
 */
export function UserPermissions() {
  const { userRoles, userPermissions, isAdmin, isModerator } = usePermissions()

  // Group permissions by category (extract category from permission name)
  const permissionsByCategory = userPermissions.reduce((acc, permission) => {
    // Try to extract category from permission name (e.g., "VIEW_USERS" -> "User Management")
    const category = getCategoryFromPermission(permission)
    if (!acc[category]) acc[category] = []
    acc[category].push(permission)
    return acc
  }, {} as Record<string, string[]>)

  function getCategoryFromPermission(permission: string): string {
    if (permission.includes('USER')) return 'User Management'
    if (permission.includes('PROJECT')) return 'Project Management'
    if (permission.includes('CREDIT')) return 'Credit System'
    if (permission.includes('SYSTEM') || permission.includes('ROLE') || permission.includes('PERMISSION')) return 'System Administration'
    if (permission.includes('MODERATE') || permission.includes('REPORT')) return 'Content Moderation'
    if (permission.includes('SUPPORT') || permission.includes('TICKET')) return 'Support'
    return 'Other'
  }

  if (userRoles.length === 0) {
    return (
      <div className="bg-muted rounded-lg p-6">
        <h3 className="text-lg font-semibold text-foreground mb-2">User Permissions</h3>
        <p className="text-muted-foreground">No roles assigned</p>
      </div>
    )
  }

  return (
    <div className="bg-card rounded-lg shadow-sm border border-border p-6">
      <h3 className="text-lg font-semibold text-foreground mb-4">Your Access Level</h3>

      {/* Role Badges */}
      <div className="mb-6">
        <h4 className="text-sm font-medium text-muted-foreground mb-2">Roles:</h4>
        <div className="flex flex-wrap gap-2">
          {userRoles.map((role) => (
            <span
              key={role}
              className={`px-3 py-1 rounded-full text-sm font-medium ${
                role === 'Admin'
                  ? 'bg-destructive/10 text-destructive'
                  : role === 'Moderator'
                  ? 'bg-warning/10 text-warning'
                  : role === 'Support'
                  ? 'bg-info/10 text-info'
                  : role === 'Analyst'
                  ? 'bg-primary/10 text-primary'
                  : 'bg-muted text-foreground'
              }`}
            >
              {role}
              {role === 'Admin' && ' 👑'}
              {role === 'Moderator' && ' 🛡️'}
            </span>
          ))}
        </div>
      </div>

      {/* Access Level Summary */}
      <div className="mb-6">
        <div className="flex items-center space-x-4 text-sm">
          <div className={`flex items-center ${isAdmin() ? 'text-success' : 'text-muted-foreground'}`}>
            <span className="mr-1">{isAdmin() ? '✓' : '✗'}</span>
            Administrative Access
          </div>
          <div className={`flex items-center ${isModerator() ? 'text-success' : 'text-muted-foreground'}`}>
            <span className="mr-1">{isModerator() ? '✓' : '✗'}</span>
            Moderation Access
          </div>
        </div>
      </div>

      {/* Detailed Permissions */}
      {userPermissions.length > 0 && (
        <div>
          <h4 className="text-sm font-medium text-muted-foreground mb-3">
            Permissions ({userPermissions.length}):
          </h4>
          <div className="space-y-3">
            {Object.entries(permissionsByCategory).map(([category, permissions]) => (
              <div key={category}>
                <h5 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1">
                  {category}
                </h5>
                <div className="flex flex-wrap gap-1">
                  {permissions.map((permission) => (
                    <span
                      key={permission}
                      className="px-2 py-1 bg-success/10 text-success text-xs rounded border border-success/20"
                    >
                      {formatPermissionName(permission)}
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function formatPermissionName(permission: string): string {
  return permission
    .toLowerCase()
    .split('_')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
}