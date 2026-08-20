'use client';

import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Folder,
  File,
  Download,
  Trash2,
  Search,
  MoreHorizontal,
  Upload,
  FolderPlus,
  X  // BUG-037 FIX: Import X from lucide-react instead of custom component
} from 'lucide-react';
import FileUpload from './FileUpload';
import { ConfirmDialog } from '../ui/confirm-dialog';  // BUG-003 FIX: Import ConfirmDialog
import { AUTH_CONFIG } from '../../constants/auth';

interface FileManagerProps {
  workspaceId: string;
  isClient: boolean;
}

interface UploadedFile {
  id: string;
  name: string;
  size: number;
  url: string;
  mimeType: string;
}

interface WorkspaceDocument {
  id: string;
  fileName: string;
  originalFileName: string;
  filePath: string;
  mimeType: string;
  fileSize: number;
  uploadedAt: string;
  uploadedById: string;
  uploaderName: string;
  folderId?: string;
  folderName?: string;
  description?: string;
  version: number;
  isDeleted: boolean;
  downloadCount: number;
}

interface DocumentFolder {
  id: string;
  name: string;
  description?: string;
  parentFolderId?: string;
  createdAt: string;
  createdById: string;
  documentCount: number;
}

interface FileManagerState {
  documents: WorkspaceDocument[];
  folders: DocumentFolder[];
  currentFolderId?: string;
  currentFolderPath: { id: string; name: string }[];
  loading: boolean;
  error?: string;
  searchTerm: string;
  selectedItems: Set<string>;
  showUpload: boolean;
  showNewFolder: boolean;
  viewMode: 'grid' | 'list';
}

export default function FileManager({ workspaceId, isClient }: FileManagerProps) {
  const [state, setState] = useState<FileManagerState>({
    documents: [],
    folders: [],
    currentFolderPath: [{ id: '', name: 'Root' }],
    loading: true,
    selectedItems: new Set(),
    showUpload: false,
    showNewFolder: false,
    searchTerm: '',
    viewMode: 'list'
  });

  const [newFolderName, setNewFolderName] = useState('');

  // BUG-003 FIX: State for confirm dialog
  const [deleteConfirm, setDeleteConfirm] = useState<{ open: boolean; documentId: string | null; fileName: string }>({
    open: false,
    documentId: null,
    fileName: ''
  });

  // BUG-029 FIX: Debounce search input
  const [searchInput, setSearchInput] = useState('');
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // BUG-029 FIX: Debounce search term updates
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    searchTimeoutRef.current = setTimeout(() => {
      setState(prev => ({ ...prev, searchTerm: searchInput }));
    }, 300);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchInput]);

  // BUG-FE-002 FIX: Remove state from dependencies to prevent infinite re-render
  // Pass parameters instead of relying on closure over state
  const loadFiles = useCallback(async (folderId?: string, searchTerm?: string) => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));

    try {
      const params = new URLSearchParams();
      if (folderId) {
        params.append('folderId', folderId);
      }
      if (searchTerm) {
        params.append('search', searchTerm);
      }

      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/fileshare/workspace/${workspaceId}/documents?${params}`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to load files');
      }

      const data = await response.json();
      setState(prev => ({
        ...prev,
        documents: data.documents || [],
        folders: data.folders || [],
        loading: false
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to load files',
        loading: false
      }));
    }
  }, [workspaceId]); // Only workspaceId as dependency

  // BUG-FE-002 FIX: Separate effect for triggering reloads when state changes
  useEffect(() => {
    loadFiles(state.currentFolderId, state.searchTerm);
  }, [workspaceId, state.currentFolderId, state.searchTerm, loadFiles]);

  const handleUploadComplete = (files: UploadedFile[]) => {
    setState(prev => ({ ...prev, showUpload: false }));
    loadFiles(state.currentFolderId, state.searchTerm); // Refresh file list
  };

  const handleUploadError = (error: string) => {
    setState(prev => ({ ...prev, error }));
  };

  const createFolder = async () => {
    if (!newFolderName.trim()) return;

    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/fileshare/folder`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          name: newFolderName,
          workspaceId,
          parentFolderId: state.currentFolderId
        })
      });

      if (!response.ok) {
        throw new Error('Failed to create folder');
      }

      setNewFolderName('');
      setState(prev => ({ ...prev, showNewFolder: false }));
      loadFiles(state.currentFolderId, state.searchTerm);
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to create folder'
      }));
    }
  };

  const downloadFile = async (file: WorkspaceDocument) => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/fileshare/download/${file.id}`, {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to download file');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = file.originalFileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to download file'
      }));
    }
  };

  // BUG-003 FIX: Open confirm dialog instead of native confirm()
  const handleDeleteClick = (documentId: string, fileName: string) => {
    setDeleteConfirm({ open: true, documentId, fileName });
  };

  // BUG-003 FIX: Actual delete function called after confirmation
  const deleteFile = async () => {
    if (!deleteConfirm.documentId) return;

    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/fileshare/document/${deleteConfirm.documentId}`, {
        method: 'DELETE',
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to delete file');
      }

      setDeleteConfirm({ open: false, documentId: null, fileName: '' });
      loadFiles(state.currentFolderId, state.searchTerm);
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to delete file'
      }));
      setDeleteConfirm({ open: false, documentId: null, fileName: '' });
    }
  };

  const navigateToFolder = (folderId?: string, folderName?: string) => {
    if (!folderId) {
      // Navigate to root
      setState(prev => ({
        ...prev,
        currentFolderId: undefined,
        currentFolderPath: [{ id: '', name: 'Root' }]
      }));
    } else {
      setState(prev => ({
        ...prev,
        currentFolderId: folderId,
        currentFolderPath: [...prev.currentFolderPath, { id: folderId, name: folderName || 'Folder' }]
      }));
    }
  };

  const navigateToBreadcrumb = (index: number) => {
    const newPath = state.currentFolderPath.slice(0, index + 1);
    const targetFolder = newPath[newPath.length - 1];
    
    setState(prev => ({
      ...prev,
      currentFolderId: targetFolder.id || undefined,
      currentFolderPath: newPath
    }));
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getFileIcon = (mimeType: string) => {
    if (mimeType.startsWith('image/')) {
      return <File className="h-8 w-8 text-success" />;
    }
    if (mimeType.includes('pdf')) {
      return <File className="h-8 w-8 text-destructive" />;
    }
    if (mimeType.includes('word') || mimeType.includes('document')) {
      return <File className="h-8 w-8 text-primary" />;
    }
    return <File className="h-8 w-8 text-muted-foreground" />;
  };

  return (
    <div className="bg-card rounded-lg shadow-sm border border-border h-full flex flex-col">
      {/* Header */}
      <div className="border-b border-border p-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-foreground">Files & Documents</h3>
          <div className="flex items-center space-x-2">
            <button
              onClick={() => setState(prev => ({ ...prev, showNewFolder: true }))}
              className="flex items-center px-3 py-1.5 text-sm bg-muted text-foreground rounded-full hover:bg-muted/80"
            >
              <FolderPlus className="h-4 w-4 mr-1" />
              New Folder
            </button>
            <button
              onClick={() => setState(prev => ({ ...prev, showUpload: true }))}
              className="flex items-center px-3 py-1.5 text-sm bg-primary text-primary-foreground rounded-full hover:bg-primary/90"
            >
              <Upload className="h-4 w-4 mr-1" />
              Upload
            </button>
          </div>
        </div>

        {/* Breadcrumb Navigation */}
        <nav className="flex items-center space-x-2 text-sm text-muted-foreground mb-4">
          {state.currentFolderPath.map((folder, index) => (
            <React.Fragment key={folder.id || 'root'}>
              <button
                onClick={() => navigateToBreadcrumb(index)}
                className="hover:text-primary hover:underline"
              >
                {folder.name}
              </button>
              {index < state.currentFolderPath.length - 1 && (
                <span className="text-muted-foreground">/</span>
              )}
            </React.Fragment>
          ))}
        </nav>

        {/* Search - BUG-029 FIX: Use debounced search input */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search files..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
          />
        </div>
      </div>

      {/* Upload Modal */}
      {state.showUpload && (
        <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50">
          <div className="bg-card rounded-lg p-6 max-w-2xl w-full mx-4">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold">Upload Files</h3>
              <button
                onClick={() => setState(prev => ({ ...prev, showUpload: false }))}
                className="text-muted-foreground hover:text-foreground"
              >
                <X className="h-6 w-6" />
              </button>
            </div>
            <FileUpload
              workspaceId={workspaceId}
              folderId={state.currentFolderId}
              onUploadComplete={handleUploadComplete}
              onError={handleUploadError}
            />
          </div>
        </div>
      )}

      {/* New Folder Modal */}
      {state.showNewFolder && (
        <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50">
          <div className="bg-card rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-semibold mb-4">Create New Folder</h3>
            <input
              type="text"
              placeholder="Folder name"
              value={newFolderName}
              onChange={(e) => setNewFolderName(e.target.value)}
              className="w-full px-3 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring mb-4"
              onKeyPress={(e) => e.key === 'Enter' && createFolder()}
              autoFocus
            />
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setState(prev => ({ ...prev, showNewFolder: false }))}
                className="px-4 py-2 text-sm text-foreground border border-input rounded-full hover:bg-muted"
              >
                Cancel
              </button>
              <button
                onClick={createFolder}
                disabled={!newFolderName.trim()}
                className="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-full hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Create
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Content */}
      <div className="flex-1 overflow-auto p-4">
        {state.loading ? (
          <div className="flex justify-center items-center h-32">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          </div>
        ) : state.error ? (
          <div className="text-center text-destructive py-8">
            <p>{state.error}</p>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Folders */}
            {state.folders.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-foreground mb-2">Folders</h4>
                <div className="grid grid-cols-1 gap-2">
                  {state.folders.map((folder) => (
                    <div
                      key={folder.id}
                      className="flex items-center p-3 border border-border rounded-lg hover:bg-muted cursor-pointer"
                      onClick={() => navigateToFolder(folder.id, folder.name)}
                    >
                      <Folder className="h-8 w-8 text-primary mr-3" />
                      <div className="flex-1">
                        <h5 className="font-medium text-foreground">{folder.name}</h5>
                        {folder.description && (
                          <p className="text-sm text-muted-foreground">{folder.description}</p>
                        )}
                        <p className="text-xs text-muted-foreground">
                          {folder.documentCount} files • Created {formatDate(folder.createdAt)}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Files */}
            {state.documents.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-foreground mb-2">Files</h4>
                <div className="space-y-2">
                  {state.documents.map((document) => (
                    <div
                      key={document.id}
                      className="flex items-center p-3 border border-border rounded-lg hover:bg-muted"
                    >
                      {getFileIcon(document.mimeType)}
                      <div className="flex-1 ml-3">
                        <h5 className="font-medium text-foreground truncate">
                          {document.originalFileName}
                        </h5>
                        <p className="text-sm text-muted-foreground">
                          {formatFileSize(document.fileSize)} • Uploaded by {document.uploaderName}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {formatDate(document.uploadedAt)} • Downloaded {document.downloadCount} times
                        </p>
                      </div>
                      <div className="flex items-center space-x-2 ml-4">
                        <button
                          onClick={() => downloadFile(document)}
                          className="p-1.5 text-muted-foreground hover:text-primary hover:bg-primary/10 rounded"
                        >
                          <Download className="h-4 w-4" />
                        </button>
                        {(isClient || document.uploadedById === 'current-user') && (
                          <button
                            onClick={() => handleDeleteClick(document.id, document.originalFileName)}
                            className="p-1.5 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded"
                            aria-label={`Delete ${document.originalFileName}`}
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        )}
                        <button className="p-1.5 text-muted-foreground hover:text-foreground hover:bg-muted rounded">
                          <MoreHorizontal className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Empty State */}
            {state.documents.length === 0 && state.folders.length === 0 && !state.searchTerm && (
              <div className="text-center py-12">
                <Folder className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-medium text-foreground mb-2">No files yet</h3>
                <p className="text-muted-foreground mb-4">Upload your first file to get started</p>
                <button
                  onClick={() => setState(prev => ({ ...prev, showUpload: true }))}
                  className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground text-sm font-medium rounded-full hover:bg-primary/90"
                >
                  <Upload className="h-4 w-4 mr-2" />
                  Upload Files
                </button>
              </div>
            )}

            {/* Search No Results */}
            {state.documents.length === 0 && state.folders.length === 0 && state.searchTerm && (
              <div className="text-center py-12">
                <Search className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-medium text-foreground mb-2">No files found</h3>
                <p className="text-muted-foreground">Try adjusting your search terms</p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* BUG-003 FIX: Delete Confirmation Dialog */}
      <ConfirmDialog
        open={deleteConfirm.open}
        onOpenChange={(open) => setDeleteConfirm(prev => ({ ...prev, open }))}
        title="Delete File"
        description={`Are you sure you want to delete "${deleteConfirm.fileName}"? This action cannot be undone.`}
        confirmText="Delete"
        cancelText="Cancel"
        variant="destructive"
        onConfirm={deleteFile}
        onCancel={() => setDeleteConfirm({ open: false, documentId: null, fileName: '' })}
      />
    </div>
  );
}