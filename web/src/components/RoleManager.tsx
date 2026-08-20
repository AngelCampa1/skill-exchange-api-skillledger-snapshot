'use client'

import React, { useState, useEffect, useCallback, useMemo } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { ConfirmDialog } from './ui/confirm-dialog'  // BUG-041 FIX: Import ConfirmDialog
import { Button } from './ui/button'  // BUG-044 FIX: Import Button component

interface Permission {
  id: string
  name: string
  description?: string
  category?: string
  isActive: boolean
}

interface Role {
  id: string
  name: string
  description?: string
  isSystemRole: boolean
  isActive: boolean
  priority: number
  permissions: Permission[]
  userCount: number
  createdAt: string
  updatedAt: string
}

/**
 * Admin component for managing roles and permissions
 */
export function RoleManager() {
  const [roles, setRoles] = useState<Role[]>([])
  const [permissions, setPermissions] = useState<Record<string, Permission[]>>({})
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedRole, setSelectedRole] = useState<Role | null>(null)
  const [showCreateRole, setShowCreateRole] = useState(false)

  // Form states
  const [roleName, setRoleName] = useState('')
  const [roleDescription, setRoleDescription] = useState('')
  const [rolePriority, setRolePriority] = useState(0)
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([])

  // BUG-041 FIX: State for delete confirmation dialog
  const [deleteConfirm, setDeleteConfirm] = useState<{ open: boolean; roleId: string | null; roleName: string }>({
    open: false,
    roleId: null,
    roleName: ''
  })

  // BUG-018 FIX: Validation state
  const [validationError, setValidationError] = useState<string | null>(null)

  // BUG-027 FIX: Pagination state
  const [currentPage, setCurrentPage] = useState(1)
  const ROLES_PER_PAGE = 9

  const fetchRoles = useCallback(async () => {
    try {
      const response = await fetch('/api/role', {
        headers: {
          'Content-Type': 'application/json'
        },
        credentials: 'include'
      })

      if (response.ok) {
        const data = await response.json()
        setRoles(data)
      } else {
        throw new Error('Failed to fetch roles')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch roles')
    }
  }, [])

  const fetchPermissions = useCallback(async () => {
    try {
      const response = await fetch('/api/role/permissions', {
        headers: {
          'Content-Type': 'application/json'
        },
        credentials: 'include'
      })

      if (response.ok) {
        const data = await response.json()
        setPermissions(data)
      } else {
        throw new Error('Failed to fetch permissions')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch permissions')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchRoles()
    fetchPermissions()
  }, [fetchRoles, fetchPermissions])

  // BUG-027 FIX: Paginated roles
  const paginatedRoles = useMemo(() => {
    const startIndex = (currentPage - 1) * ROLES_PER_PAGE
    return roles.slice(startIndex, startIndex + ROLES_PER_PAGE)
  }, [roles, currentPage])

  const totalPages = Math.ceil(roles.length / ROLES_PER_PAGE)

  // BUG-018 FIX: Validate role name
  const validateRoleName = useCallback((name: string): string | null => {
    if (!name.trim()) {
      return 'Role name is required'
    }
    if (name.length > 50) {
      return 'Role name must be 50 characters or less'
    }
    if (!/^[a-zA-Z0-9\s-_]+$/.test(name)) {
      return 'Role name can only contain letters, numbers, spaces, hyphens, and underscores'
    }
    // Check uniqueness
    const existingRole = roles.find(r => r.name.toLowerCase() === name.toLowerCase().trim())
    if (existingRole) {
      return 'A role with this name already exists'
    }
    return null
  }, [roles])

  const createRole = async () => {
    // BUG-018 FIX: Validate before submitting
    const error = validateRoleName(roleName)
    if (error) {
      setValidationError(error)
      return
    }

    try {
      const response = await fetch('/api/role', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        credentials: 'include',
        body: JSON.stringify({
          name: roleName.trim(),
          description: roleDescription.trim(),
          priority: rolePriority,
          permissionIds: selectedPermissions
        })
      })

      if (response.ok) {
        setShowCreateRole(false)
        setRoleName('')
        setRoleDescription('')
        setRolePriority(0)
        setSelectedPermissions([])
        setValidationError(null)
        await fetchRoles()
      } else {
        const errorData = await response.json()
        throw new Error(errorData.message || 'Failed to create role')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create role')
    }
  }

  // BUG-041 FIX: Show delete confirmation dialog instead of native confirm
  const handleDeleteClick = (roleId: string, roleName: string) => {
    setDeleteConfirm({ open: true, roleId, roleName })
  }

  const deleteRole = async (roleId: string) => {
    try {
      const response = await fetch(`/api/role/${roleId}`, {
        method: 'DELETE',
        credentials: 'include'
      })

      if (response.ok) {
        await fetchRoles()
      } else {
        const errorData = await response.json()
        throw new Error(errorData.message || 'Failed to delete role')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete role')
    } finally {
      setDeleteConfirm({ open: false, roleId: null, roleName: '' })
    }
  }

  if (loading) {
    return (
      <div className="flex justify-center items-center p-8">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold text-foreground">Role Management</h1>
        {/* BUG-044 FIX: Use Button component */}
        <Button onClick={() => setShowCreateRole(true)}>
          Create Role
        </Button>
      </div>

      {error && (
        <div className="bg-destructive/10 border border-destructive/20 text-destructive px-4 py-3 rounded mb-6">
          {error}
        </div>
      )}

      {/* Create Role Modal - BUG-007 FIX: Add backdrop click dismiss */}
      {showCreateRole && (
        <div
          className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50"
          onClick={() => setShowCreateRole(false)}
        >
          <div
            className="bg-card rounded-lg p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="text-xl font-semibold text-foreground mb-4">Create New Role</h2>

            <div className="space-y-4">
              {/* BUG-018 FIX: Add maxLength and validation feedback */}
              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-1">
                  Role Name <span className="text-destructive">*</span>
                </label>
                <input
                  type="text"
                  value={roleName}
                  onChange={(e) => {
                    setRoleName(e.target.value)
                    setValidationError(null)
                  }}
                  maxLength={50}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring ${
                    validationError ? 'border-destructive' : 'border-input'
                  }`}
                  placeholder="Enter role name"
                />
                {validationError && (
                  <p className="text-destructive text-sm mt-1">{validationError}</p>
                )}
                <p className="text-muted-foreground text-xs mt-1">
                  {roleName.length}/50 characters
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-1">
                  Description
                </label>
                <textarea
                  value={roleDescription}
                  onChange={(e) => setRoleDescription(e.target.value)}
                  className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder="Enter role description"
                  rows={3}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-1">
                  Priority (0-100)
                </label>
                <input
                  type="number"
                  value={rolePriority}
                  onChange={(e) => setRolePriority(parseInt(e.target.value) || 0)}
                  className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  min="0"
                  max="100"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-muted-foreground mb-2">
                  Permissions
                </label>
                <div className="space-y-3 max-h-60 overflow-y-auto">
                  {Object.entries(permissions).map(([category, categoryPermissions]) => (
                    <div key={category}>
                      <h4 className="font-medium text-foreground mb-2">{category}</h4>
                      <div className="space-y-1 ml-4">
                        {categoryPermissions.map((permission) => (
                          <label key={permission.id} className="flex items-center">
                            <input
                              type="checkbox"
                              checked={selectedPermissions.includes(permission.id)}
                              onChange={(e) => {
                                if (e.target.checked) {
                                  setSelectedPermissions([...selectedPermissions, permission.id])
                                } else {
                                  setSelectedPermissions(selectedPermissions.filter(id => id !== permission.id))
                                }
                              }}
                              className="mr-2"
                            />
                            <span className="text-sm text-foreground">
                              {permission.name}
                              {permission.description && (
                                <span className="text-muted-foreground"> - {permission.description}</span>
                              )}
                            </span>
                          </label>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* BUG-044 FIX: Use Button components */}
            <div className="flex justify-end space-x-3 mt-6">
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateRole(false)
                  setValidationError(null)
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={createRole}
                disabled={!roleName.trim()}
              >
                Create Role
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Roles List - BUG-027 FIX: Use paginated roles */}
      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {paginatedRoles.map((role) => (
          <div key={role.id} className="bg-card rounded-lg shadow-md p-6 border border-border">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="text-lg font-semibold text-foreground">{role.name}</h3>
                {role.isSystemRole && (
                  <span className="inline-block bg-warning/10 text-warning text-xs px-2 py-1 rounded-full mt-1">
                    System Role
                  </span>
                )}
              </div>
              {/* BUG-041 FIX: Use handleDeleteClick instead of deleteRole directly */}
              {/* BUG-044 FIX: Use Button component */}
              {!role.isSystemRole && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleDeleteClick(role.id, role.name)}
                  className="text-destructive hover:text-destructive hover:bg-destructive/10"
                >
                  Delete
                </Button>
              )}
            </div>

            {role.description && (
              <p className="text-muted-foreground text-sm mb-4">{role.description}</p>
            )}

            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Priority:</span>
                <span className="text-foreground">{role.priority}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Users:</span>
                <span className="text-foreground">{role.userCount}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Permissions:</span>
                <span className="text-foreground">{role.permissions.length}</span>
              </div>
            </div>

            {/* BUG-044 FIX: Use Button component */}
            <div className="mt-4">
              <Button
                variant="outline"
                onClick={() => setSelectedRole(role)}
                className="w-full"
              >
                View Permissions
              </Button>
            </div>
          </div>
        ))}
      </div>

      {/* BUG-027 FIX: Pagination controls */}
      {totalPages > 1 && (
        <div className="flex justify-center items-center space-x-4 mt-8">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            disabled={currentPage === 1}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {currentPage} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
            disabled={currentPage === totalPages}
          >
            Next
          </Button>
        </div>
      )}

      {/* Role Details Modal - BUG-007 FIX: Add backdrop click dismiss */}
      {selectedRole && (
        <div
          className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50"
          onClick={() => setSelectedRole(null)}
        >
          <div
            className="bg-card rounded-lg p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-semibold text-foreground">{selectedRole.name} Permissions</h2>
              {/* BUG-044 FIX: Use Button component */}
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedRole(null)}
              >
                ✕
              </Button>
            </div>

            <div className="space-y-4">
              {selectedRole.permissions.length > 0 ? (
                Object.entries(
                  selectedRole.permissions.reduce((acc, permission) => {
                    const category = permission.category || 'Other'
                    if (!acc[category]) acc[category] = []
                    acc[category].push(permission)
                    return acc
                  }, {} as Record<string, Permission[]>)
                ).map(([category, categoryPermissions]) => (
                  <div key={category}>
                    <h4 className="font-medium text-foreground mb-2">{category}</h4>
                    <div className="space-y-1 ml-4">
                      {categoryPermissions.map((permission) => (
                        <div key={permission.id} className="text-sm">
                          <span className="font-medium text-foreground">{permission.name}</span>
                          {permission.description && (
                            <span className="text-muted-foreground"> - {permission.description}</span>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-muted-foreground">This role has no permissions assigned.</p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* BUG-041 FIX: Delete confirmation dialog */}
      <ConfirmDialog
        open={deleteConfirm.open}
        onOpenChange={(open) => {
          if (!open) setDeleteConfirm({ open: false, roleId: null, roleName: '' })
        }}
        title="Delete Role"
        description={`Are you sure you want to delete the role "${deleteConfirm.roleName}"? This action cannot be undone.`}
        confirmText="Delete"
        cancelText="Cancel"
        variant="destructive"
        onConfirm={() => {
          if (deleteConfirm.roleId) {
            deleteRole(deleteConfirm.roleId)
          }
        }}
      />
    </div>
  )
}