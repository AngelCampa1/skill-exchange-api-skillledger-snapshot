'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react';
import {
  Shield,
  User,
  Users,
  Eye,
  Edit3,
  Trash2,
  Plus,
  X,
  Search,
  AlertCircle,
  Crown,
  Lock,
  Unlock
} from 'lucide-react';
import { AUTH_CONFIG } from '../../constants/auth';
import { useToast } from '@/components/ui/toast';  // BUG-017 FIX: Import toast
import { ConfirmDialog } from '../ui/confirm-dialog';  // BUG-005 FIX: Import for proper modal
import {
  DocumentPermission,
  DocumentPermissionLevel,
  WorkspaceDocument,
  DocumentFolder
} from '@/types/document';

interface DocumentPermissionsProps {
  item: WorkspaceDocument | DocumentFolder;
  itemType: 'document' | 'folder';
  isOpen: boolean;
  onClose: () => void;
  onPermissionsUpdated: () => void;
  className?: string;
}

interface PermissionsState {
  permissions: DocumentPermission[];
  availableUsers: Array<{ id: string; name: string; email: string; avatar?: string }>;
  loading: boolean;
  saving: boolean;
  error?: string;
  showAddUser: boolean;
  searchTerm: string;
  selectedUserId: string;
  selectedPermission: DocumentPermissionLevel;
  expirationDate: string;
  inheritedPermissions: DocumentPermission[];
}

export default function DocumentPermissions({
  item,
  itemType,
  isOpen,
  onClose,
  onPermissionsUpdated,
  className = ''
}: DocumentPermissionsProps) {
  const { toast } = useToast();  // BUG-017 FIX: Add toast hook

  const [state, setState] = useState<PermissionsState>({
    permissions: [],
    availableUsers: [],
    loading: true,
    saving: false,
    showAddUser: false,
    searchTerm: '',
    selectedUserId: '',
    selectedPermission: DocumentPermissionLevel.Read,
    expirationDate: '',
    inheritedPermissions: []
  });

  // BUG-005 FIX: State for delete confirmation dialog
  const [deleteConfirm, setDeleteConfirm] = useState<{ open: boolean; permissionId: string | null; userName: string }>({
    open: false,
    permissionId: null,
    userName: ''
  });

  // BUG-026 FIX: Reset state when modal closes
  const handleClose = useCallback(() => {
    setState({
      permissions: [],
      availableUsers: [],
      loading: true,
      saving: false,
      showAddUser: false,
      searchTerm: '',
      selectedUserId: '',
      selectedPermission: DocumentPermissionLevel.Read,
      expirationDate: '',
      inheritedPermissions: []
    });
    setDeleteConfirm({ open: false, permissionId: null, userName: '' });
    onClose();
  }, [onClose]);

  const loadPermissions = async () => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const endpoint = itemType === 'document' 
        ? `/api/documents/${item.id}/permissions`
        : `/api/folders/${item.id}/permissions`;
      
      const response = await fetch(endpoint, {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to load permissions');
      }

      const data = await response.json();
      setState(prev => ({ 
        ...prev, 
        permissions: data.permissions || [],
        inheritedPermissions: data.inheritedPermissions || [],
        loading: false 
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to load permissions',
        loading: false
      }));
    }
  };

  const loadAvailableUsers = async () => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch('/api/workspace/users', {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (response.ok) {
        const users = await response.json();
        setState(prev => ({ ...prev, availableUsers: users }));
      }
    } catch (error) {
      logger.error('Failed to load users:', error);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadPermissions();
      loadAvailableUsers();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, item.id]);

  const addPermission = async () => {
    if (!state.selectedUserId || state.saving) return;

    setState(prev => ({ ...prev, saving: true }));

    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const endpoint = itemType === 'document' 
        ? `/api/documents/${item.id}/permissions`
        : `/api/folders/${item.id}/permissions`;
      
      const response = await fetch(endpoint, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userId: state.selectedUserId,
          permission: state.selectedPermission,
          expiresAt: state.expirationDate || undefined
        })
      });

      if (!response.ok) {
        throw new Error('Failed to add permission');
      }

      // BUG-017 FIX: Show success toast
      toast({
        title: 'Permission added',
        description: 'The user has been granted access successfully.',
        variant: 'default'
      });

      // Reset form
      setState(prev => ({
        ...prev,
        showAddUser: false,
        selectedUserId: '',
        selectedPermission: DocumentPermissionLevel.Read,
        expirationDate: '',
        saving: false
      }));

      // Reload permissions
      await loadPermissions();
      onPermissionsUpdated();
    } catch (error) {
      // BUG-017 FIX: Show error toast
      const errorMessage = error instanceof Error ? error.message : 'Failed to add permission';
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'error'
      });
      setState(prev => ({
        ...prev,
        error: errorMessage,
        saving: false
      }));
    }
  };

  const updatePermission = async (permissionId: string, newLevel: DocumentPermissionLevel) => {
    setState(prev => ({ ...prev, saving: true }));

    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/permissions/${permissionId}`, {
        method: 'PATCH',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          permission: newLevel
        })
      });

      if (!response.ok) {
        throw new Error('Failed to update permission');
      }

      // BUG-017 FIX: Show success toast
      toast({
        title: 'Permission updated',
        description: `Permission level changed to ${newLevel.toLowerCase()}.`,
        variant: 'default'
      });

      await loadPermissions();
      onPermissionsUpdated();
    } catch (error) {
      // BUG-017 FIX: Show error toast
      const errorMessage = error instanceof Error ? error.message : 'Failed to update permission';
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'error'
      });
      setState(prev => ({
        ...prev,
        error: errorMessage
      }));
    } finally {
      setState(prev => ({ ...prev, saving: false }));
    }
  };

  // BUG-005 FIX: Show delete confirmation dialog instead of using native confirm
  const handleDeleteClick = (permissionId: string, userName: string) => {
    setDeleteConfirm({ open: true, permissionId, userName });
  };

  const removePermission = async (permissionId: string) => {
    setState(prev => ({ ...prev, saving: true }));

    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/permissions/${permissionId}`, {
        method: 'DELETE',
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to remove permission');
      }

      // BUG-017 FIX: Show success toast
      toast({
        title: 'Permission removed',
        description: 'The user permission has been removed successfully.',
        variant: 'default'
      });

      await loadPermissions();
      onPermissionsUpdated();
    } catch (error) {
      // BUG-017 FIX: Show error toast
      const errorMessage = error instanceof Error ? error.message : 'Failed to remove permission';
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'error'
      });
      setState(prev => ({
        ...prev,
        error: errorMessage
      }));
    } finally {
      setState(prev => ({ ...prev, saving: false }));
      setDeleteConfirm({ open: false, permissionId: null, userName: '' });
    }
  };

  const getPermissionIcon = (level: DocumentPermissionLevel) => {
    switch (level) {
      case DocumentPermissionLevel.Read:
        return <Eye className="h-4 w-4 text-primary" />;
      case DocumentPermissionLevel.Write:
        return <Edit3 className="h-4 w-4 text-success" />;
      case DocumentPermissionLevel.Admin:
        return <Crown className="h-4 w-4 text-warning" />;
      default:
        return <Shield className="h-4 w-4 text-muted-foreground" />;
    }
  };

  const getPermissionDescription = (level: DocumentPermissionLevel) => {
    switch (level) {
      case DocumentPermissionLevel.Read:
        return 'Can view and download';
      case DocumentPermissionLevel.Write:
        return 'Can view, download, and upload new versions';
      case DocumentPermissionLevel.Admin:
        return 'Full access including permissions management';
      default:
        return '';
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  // BUG-023 FIX: Use UTC comparison to avoid timezone issues
  const isExpired = (expiresAt?: string): boolean => {
    if (!expiresAt) return false;
    return new Date(expiresAt).getTime() < Date.now();
  };

  const filteredUsers = state.availableUsers.filter(user =>
    user.name.toLowerCase().includes(state.searchTerm.toLowerCase()) ||
    user.email.toLowerCase().includes(state.searchTerm.toLowerCase())
  ).filter(user => 
    !state.permissions.some(p => p.userId === user.id)
  );

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50 p-4">
      <div className={`bg-card rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-hidden ${className}`}>

        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-border bg-muted">
          <div className="flex items-center space-x-3">
            <Shield className="h-6 w-6 text-primary" />
            <div>
              <h3 className="text-lg font-semibold text-foreground">Access Permissions</h3>
              <p className="text-sm text-muted-foreground">
                {itemType === 'document' ? 'Document:' : 'Folder:'} {'name' in item ? item.name : (item as WorkspaceDocument).originalFileName}
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            <button
              onClick={() => setState(prev => ({ ...prev, showAddUser: true }))}
              className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground text-sm font-medium rounded-full hover:bg-primary/90"
            >
              <Plus className="h-4 w-4 mr-2" />
              Add User
            </button>
            {/* BUG-026 FIX: Use handleClose to reset state */}
            <button onClick={handleClose} className="text-muted-foreground hover:text-foreground">
              <X className="h-6 w-6" />
            </button>
          </div>
        </div>

        {/* Add User Modal - BUG-005 FIX: Proper z-index layering for nested modals */}
        {state.showAddUser && (
          <div className="fixed inset-0 bg-overlay/90 flex items-center justify-center z-[60]">
            <div className="bg-card rounded-lg p-6 max-w-md w-full mx-4">
              <h3 className="text-lg font-semibold mb-4">Add User Permission</h3>

              <div className="space-y-4">
                {/* BUG-019 FIX: Add required indicator */}
                <div>
                  <label className="block text-sm font-medium text-foreground mb-2">
                    Search Users <span className="text-destructive">*</span>
                  </label>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <input
                      type="text"
                      value={state.searchTerm}
                      onChange={(e) => setState(prev => ({ ...prev, searchTerm: e.target.value }))}
                      placeholder="Search by name or email..."
                      className="w-full pl-10 pr-4 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
                    />
                  </div>

                  {filteredUsers.length > 0 && (
                    <div className="mt-2 max-h-32 overflow-y-auto border border-border rounded-lg">
                      {filteredUsers.map(user => (
                        <button
                          key={user.id}
                          onClick={() => {
                            setState(prev => ({
                              ...prev,
                              selectedUserId: user.id,
                              searchTerm: user.name
                            }));
                          }}
                          className={`w-full text-left px-3 py-2 hover:bg-muted flex items-center space-x-2 ${
                            state.selectedUserId === user.id ? 'bg-primary/10 border-primary/20' : ''
                          }`}
                        >
                          <div className="w-8 h-8 bg-muted rounded-full flex items-center justify-center">
                            <User className="h-4 w-4 text-muted-foreground" />
                          </div>
                          <div>
                            <p className="font-medium text-sm">{user.name}</p>
                            <p className="text-xs text-muted-foreground">{user.email}</p>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-2">
                    Permission Level
                  </label>
                  <select
                    value={state.selectedPermission}
                    onChange={(e) => setState(prev => ({
                      ...prev,
                      selectedPermission: e.target.value as DocumentPermissionLevel
                    }))}
                    className="w-full px-3 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
                  >
                    <option value={DocumentPermissionLevel.Read}>Read - Can view and download</option>
                    <option value={DocumentPermissionLevel.Write}>Write - Can edit and upload</option>
                    <option value={DocumentPermissionLevel.Admin}>Admin - Full access</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-2">
                    Expiration Date (Optional)
                  </label>
                  <input
                    type="date"
                    value={state.expirationDate}
                    onChange={(e) => setState(prev => ({ ...prev, expirationDate: e.target.value }))}
                    className="w-full px-3 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
                    min={new Date().toISOString().split('T')[0]}
                  />
                </div>
              </div>

              <div className="flex justify-end space-x-3 mt-6">
                <button
                  onClick={() => setState(prev => ({
                    ...prev,
                    showAddUser: false,
                    selectedUserId: '',
                    searchTerm: ''
                  }))}
                  className="px-4 py-2 text-sm text-foreground border border-input rounded-full hover:bg-muted"
                  disabled={state.saving}
                >
                  Cancel
                </button>
                <button
                  onClick={addPermission}
                  disabled={!state.selectedUserId || state.saving}
                  className="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-full hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {state.saving ? 'Adding...' : 'Add Permission'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-auto max-h-[60vh]">
          {state.loading ? (
            <div className="flex items-center justify-center h-32">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            </div>
          ) : state.error ? (
            <div className="flex items-center justify-center h-32">
              <div className="text-center">
                <AlertCircle className="h-12 w-12 text-destructive mx-auto mb-4" />
                <p className="text-destructive">{state.error}</p>
              </div>
            </div>
          ) : (
            <div className="p-6">
              {/* Direct Permissions */}
              <div className="mb-6">
                <h4 className="text-sm font-medium text-foreground mb-3 flex items-center">
                  <Lock className="h-4 w-4 mr-2 text-primary" />
                  Direct Permissions
                </h4>

                {state.permissions.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground">
                    <Users className="h-12 w-12 mx-auto mb-4 text-muted" />
                    <p>No direct permissions set</p>
                    <p className="text-sm">Click "Add User" to grant access</p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {state.permissions.map(permission => (
                      <div key={permission.id} className="flex items-center justify-between p-3 border border-border rounded-lg">
                        <div className="flex items-center space-x-3">
                          <div className="w-8 h-8 bg-muted rounded-full flex items-center justify-center">
                            <User className="h-4 w-4 text-muted-foreground" />
                          </div>
                          <div>
                            <p className="font-medium text-sm">{permission.userName}</p>
                            <div className="flex items-center space-x-2 text-xs text-muted-foreground">
                              <span>Granted {formatDate(permission.grantedAt)}</span>
                              {permission.expiresAt && (
                                <span className={`px-1 py-0.5 rounded ${
                                  isExpired(permission.expiresAt)
                                    ? 'bg-destructive/10 text-destructive'
                                    : 'bg-warning/10 text-warning'
                                }`}>
                                  {isExpired(permission.expiresAt) ? 'Expired' : `Expires ${formatDate(permission.expiresAt)}`}
                                </span>
                              )}
                            </div>
                          </div>
                        </div>

                        <div className="flex items-center space-x-2">
                          <select
                            value={permission.permission}
                            onChange={(e) => updatePermission(permission.id, e.target.value as DocumentPermissionLevel)}
                            disabled={state.saving}
                            className="text-sm border border-input rounded px-2 py-1 focus:ring-ring focus:border-ring"
                          >
                            <option value={DocumentPermissionLevel.Read}>Read</option>
                            <option value={DocumentPermissionLevel.Write}>Write</option>
                            <option value={DocumentPermissionLevel.Admin}>Admin</option>
                          </select>

                          <div className="flex items-center space-x-1">
                            {getPermissionIcon(permission.permission)}
                            {/* BUG-005 FIX: Use handleDeleteClick to show confirmation dialog */}
                            <button
                              onClick={() => handleDeleteClick(permission.id, permission.userName)}
                              disabled={state.saving}
                              className="p-1 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Inherited Permissions */}
              {state.inheritedPermissions.length > 0 && (
                <div>
                  <h4 className="text-sm font-medium text-foreground mb-3 flex items-center">
                    <Unlock className="h-4 w-4 mr-2 text-success" />
                    Inherited Permissions
                  </h4>

                  <div className="space-y-3">
                    {state.inheritedPermissions.map(permission => (
                      <div key={permission.id} className="flex items-center justify-between p-3 border border-border/50 rounded-lg bg-muted/50">
                        <div className="flex items-center space-x-3">
                          <div className="w-8 h-8 bg-muted rounded-full flex items-center justify-center">
                            <User className="h-4 w-4 text-muted-foreground" />
                          </div>
                          <div>
                            <p className="font-medium text-sm">{permission.userName}</p>
                            <p className="text-xs text-muted-foreground">Inherited from parent folder</p>
                          </div>
                        </div>

                        <div className="flex items-center space-x-2">
                          <span className="text-sm text-muted-foreground capitalize">
                            {permission.permission.toLowerCase()}
                          </span>
                          {getPermissionIcon(permission.permission)}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Permission Legend */}
              <div className="mt-6 p-4 bg-primary/10 rounded-lg">
                <h5 className="text-sm font-medium text-primary mb-2">Permission Levels</h5>
                <div className="space-y-1 text-sm text-foreground">
                  <div className="flex items-center space-x-2">
                    <Eye className="h-3 w-3" />
                    <span><strong>Read:</strong> Can view and download files</span>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Edit3 className="h-3 w-3" />
                    <span><strong>Write:</strong> Can upload new versions and edit metadata</span>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Crown className="h-3 w-3" />
                    <span><strong>Admin:</strong> Full access including permission management</span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="border-t border-border bg-muted p-4">
          <div className="flex items-center justify-between">
            <div className="text-sm text-muted-foreground">
              {state.permissions.length} direct permission{state.permissions.length !== 1 ? 's' : ''}
              {state.inheritedPermissions.length > 0 &&
                `, ${state.inheritedPermissions.length} inherited`
              }
            </div>
            {/* BUG-026 FIX: Use handleClose to reset state */}
            <button
              onClick={handleClose}
              className="px-4 py-2 text-foreground border border-input rounded-full hover:bg-muted"
            >
              Close
            </button>
          </div>
        </div>
      </div>

      {/* BUG-005 FIX: Delete confirmation dialog */}
      <ConfirmDialog
        open={deleteConfirm.open}
        onOpenChange={(open) => {
          if (!open) setDeleteConfirm({ open: false, permissionId: null, userName: '' });
        }}
        title="Remove Permission"
        description={`Are you sure you want to remove ${deleteConfirm.userName}'s access? This action cannot be undone.`}
        confirmText="Remove"
        cancelText="Cancel"
        variant="destructive"
        onConfirm={() => {
          if (deleteConfirm.permissionId) {
            removePermission(deleteConfirm.permissionId);
          }
        }}
        loading={state.saving}
      />
    </div>
  );
}