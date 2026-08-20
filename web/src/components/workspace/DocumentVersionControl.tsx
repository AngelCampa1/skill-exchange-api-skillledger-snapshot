'use client';

import React, { useState, useEffect } from 'react';
import { 
  Clock, 
  User, 
  Download, 
  Eye, 
  RotateCcw, 
  FileText, 
  AlertCircle,
  CheckCircle,
  Upload,
  MessageSquare,
  HardDrive,
  Calendar
} from 'lucide-react';
import { DocumentVersion, WorkspaceDocument } from '@/types/document';
import { AUTH_CONFIG } from '../../constants/auth';

interface DocumentVersionControlProps {
  document: WorkspaceDocument;
  isOpen: boolean;
  onClose: () => void;
  onVersionRestore: (versionId: string) => void;
  onVersionDownload: (version: DocumentVersion) => void;
  onVersionPreview: (version: DocumentVersion) => void;
  onNewVersionUpload: (file: File, description: string) => void;
  className?: string;
}

interface VersionControlState {
  versions: DocumentVersion[];
  loading: boolean;
  error?: string;
  showUploadNewVersion: boolean;
  uploadDescription: string;
  uploading: boolean;
  selectedVersions: Set<string>;
}

export default function DocumentVersionControl({
  document,
  isOpen,
  onClose,
  onVersionRestore,
  onVersionDownload,
  onVersionPreview,
  onNewVersionUpload,
  className = ''
}: DocumentVersionControlProps) {
  const [state, setState] = useState<VersionControlState>({
    versions: [],
    loading: true,
    showUploadNewVersion: false,
    uploadDescription: '',
    uploading: false,
    selectedVersions: new Set()
  });

  const [uploadFile, setUploadFile] = useState<File | null>(null);

  useEffect(() => {
    if (isOpen) {
      loadVersionHistory();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, document.id]);

  const loadVersionHistory = async () => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/documents/${document.id}/versions`, {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to load version history');
      }

      const versions: DocumentVersion[] = await response.json();
      setState(prev => ({ ...prev, versions, loading: false }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to load versions',
        loading: false
      }));
    }
  };

  const handleNewVersionUpload = async () => {
    if (!uploadFile || !state.uploadDescription.trim()) {
      return;
    }

    setState(prev => ({ ...prev, uploading: true }));

    try {
      await onNewVersionUpload(uploadFile, state.uploadDescription);
      
      // Reset upload state
      setUploadFile(null);
      setState(prev => ({
        ...prev,
        showUploadNewVersion: false,
        uploadDescription: '',
        uploading: false
      }));
      
      // Reload versions
      await loadVersionHistory();
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to upload new version',
        uploading: false
      }));
    }
  };

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setUploadFile(file);
    }
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

  const getTimeDifference = (current: string, previous?: string): string => {
    if (!previous) return 'Initial version';
    
    const currentDate = new Date(current);
    const previousDate = new Date(previous);
    const diffMs = currentDate.getTime() - previousDate.getTime();
    
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    
    if (diffDays > 0) return `${diffDays} day${diffDays > 1 ? 's' : ''} later`;
    if (diffHours > 0) return `${diffHours} hour${diffHours > 1 ? 's' : ''} later`;
    if (diffMinutes > 0) return `${diffMinutes} minute${diffMinutes > 1 ? 's' : ''} later`;
    return 'Just now';
  };

  const compareVersions = (version1: DocumentVersion, version2: DocumentVersion) => {
    const sizeDiff = version2.fileSize - version1.fileSize;
    const sizeDiffFormatted = sizeDiff > 0 ? `+${formatFileSize(sizeDiff)}` : formatFileSize(Math.abs(sizeDiff));
    
    return {
      sizeDifference: sizeDiff,
      sizeDifferenceFormatted: sizeDiffFormatted,
      hasChanges: sizeDiff !== 0 || version1.fileName !== version2.fileName
    };
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50 p-4">
      <div className={`bg-card rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden ${className}`}>

        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-border bg-muted">
          <div className="flex items-center space-x-3">
            <Clock className="h-6 w-6 text-primary" />
            <div>
              <h3 className="text-lg font-semibold text-foreground">Version History</h3>
              <p className="text-sm text-muted-foreground">{document.originalFileName}</p>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            <button
              onClick={() => setState(prev => ({ ...prev, showUploadNewVersion: true }))}
              className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground text-sm font-medium rounded-full hover:bg-primary/90"
            >
              <Upload className="h-4 w-4 mr-2" />
              Upload New Version
            </button>
            <button
              onClick={onClose}
              className="text-muted-foreground hover:text-foreground"
            >
              <X className="h-6 w-6" />
            </button>
          </div>
        </div>

        {/* Upload New Version Modal */}
        {state.showUploadNewVersion && (
          <div className="fixed inset-0 bg-overlay/90 flex items-center justify-center z-60">
            <div className="bg-card rounded-lg p-6 max-w-md w-full mx-4">
              <h3 className="text-lg font-semibold mb-4">Upload New Version</h3>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-foreground mb-2">
                    Select File
                  </label>
                  <input
                    type="file"
                    onChange={handleFileSelect}
                    className="w-full px-3 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
                    accept={document.mimeType}
                  />
                  {uploadFile && (
                    <p className="text-sm text-muted-foreground mt-1">
                      Selected: {uploadFile.name} ({formatFileSize(uploadFile.size)})
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium text-foreground mb-2">
                    Change Description
                  </label>
                  <textarea
                    value={state.uploadDescription}
                    onChange={(e) => setState(prev => ({ ...prev, uploadDescription: e.target.value }))}
                    placeholder="Describe what changed in this version..."
                    className="w-full px-3 py-2 border border-input rounded-lg focus:ring-ring focus:border-ring"
                    rows={3}
                  />
                </div>
              </div>

              <div className="flex justify-end space-x-3 mt-6">
                <button
                  onClick={() => setState(prev => ({ ...prev, showUploadNewVersion: false }))}
                  className="px-4 py-2 text-sm text-foreground border border-input rounded-full hover:bg-muted"
                  disabled={state.uploading}
                >
                  Cancel
                </button>
                <button
                  onClick={handleNewVersionUpload}
                  disabled={!uploadFile || !state.uploadDescription.trim() || state.uploading}
                  className="px-4 py-2 text-sm bg-primary text-primary-foreground rounded-full hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {state.uploading ? 'Uploading...' : 'Upload Version'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-auto max-h-[70vh]">
          {state.loading ? (
            <div className="flex items-center justify-center h-64">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            </div>
          ) : state.error ? (
            <div className="flex items-center justify-center h-64">
              <div className="text-center">
                <AlertCircle className="h-12 w-12 text-destructive mx-auto mb-4" />
                <p className="text-destructive">{state.error}</p>
                <button
                  onClick={loadVersionHistory}
                  className="mt-2 text-primary hover:text-primary/90 text-sm underline"
                >
                  Try again
                </button>
              </div>
            </div>
          ) : (
            <div className="p-6">
              {/* Version Timeline */}
              <div className="relative">
                {/* Timeline line */}
                <div className="absolute left-8 top-0 bottom-0 w-px bg-border"></div>

                {state.versions.map((version, index) => {
                  const previousVersion = state.versions[index + 1];
                  const comparison = previousVersion ? compareVersions(version, previousVersion) : null;
                  const timeDiff = getTimeDifference(version.uploadedAt, previousVersion?.uploadedAt);

                  return (
                    <div key={version.id} className="relative pb-8">
                      {/* Timeline marker */}
                      <div className={`absolute left-6 w-4 h-4 rounded-full border-2 ${
                        version.isCurrentVersion
                          ? 'bg-success border-success'
                          : 'bg-background border-border'
                      }`}></div>

                      {/* Version card */}
                      <div className="ml-16 bg-card border border-border rounded-lg p-4 hover:shadow-md transition-shadow">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center space-x-3 mb-2">
                              <h4 className="font-medium text-foreground">
                                Version {version.versionNumber}
                                {version.isCurrentVersion && (
                                  <span className="ml-2 px-2 py-1 bg-success/10 text-success text-xs rounded-full">
                                    Current
                                  </span>
                                )}
                              </h4>
                              <span className="text-sm text-muted-foreground">{timeDiff}</span>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-3">
                              <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                                <User className="h-4 w-4" />
                                <span>{version.uploaderName}</span>
                              </div>
                              <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                                <Calendar className="h-4 w-4" />
                                <span>{formatDate(version.uploadedAt)}</span>
                              </div>
                              <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                                <HardDrive className="h-4 w-4" />
                                <span>{formatFileSize(version.fileSize)}</span>
                                {comparison && comparison.sizeDifference !== 0 && (
                                  <span className={`text-xs px-1 py-0.5 rounded ${
                                    comparison.sizeDifference > 0
                                      ? 'bg-destructive/10 text-destructive'
                                      : 'bg-success/10 text-success'
                                  }`}>
                                    {comparison.sizeDifferenceFormatted}
                                  </span>
                                )}
                              </div>
                              <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                                <FileText className="h-4 w-4" />
                                <span className="truncate">{version.fileName}</span>
                              </div>
                            </div>

                            {version.changeDescription && (
                              <div className="flex items-start space-x-2 mb-3">
                                <MessageSquare className="h-4 w-4 text-muted-foreground mt-0.5" />
                                <p className="text-sm text-foreground">{version.changeDescription}</p>
                              </div>
                            )}

                            {comparison && comparison.hasChanges && (
                              <div className="mb-3">
                                <div className="flex items-center space-x-2 text-xs text-muted-foreground">
                                  <span>Changes from previous version:</span>
                                  {comparison.sizeDifference !== 0 && (
                                    <span className={`px-1 py-0.5 rounded ${
                                      comparison.sizeDifference > 0 ? 'bg-destructive/10 text-destructive' : 'bg-success/10 text-success'
                                    }`}>
                                      Size {comparison.sizeDifference > 0 ? 'increased' : 'decreased'}
                                    </span>
                                  )}
                                </div>
                              </div>
                            )}
                          </div>

                          {/* Action buttons */}
                          <div className="flex items-center space-x-2 ml-4">
                            <button
                              onClick={() => onVersionPreview(version)}
                              className="p-1.5 text-muted-foreground hover:text-primary hover:bg-primary/10 rounded"
                              title="Preview"
                            >
                              <Eye className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => onVersionDownload(version)}
                              className="p-1.5 text-muted-foreground hover:text-success hover:bg-success/10 rounded"
                              title="Download"
                            >
                              <Download className="h-4 w-4" />
                            </button>
                            {!version.isCurrentVersion && (
                              <button
                                onClick={() => onVersionRestore(version.id)}
                                className="p-1.5 text-muted-foreground hover:text-warning hover:bg-warning/10 rounded"
                                title="Restore this version"
                              >
                                <RotateCcw className="h-4 w-4" />
                              </button>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>

              {state.versions.length === 0 && (
                <div className="text-center py-12">
                  <Clock className="h-12 w-12 text-muted mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-foreground mb-2">No version history</h3>
                  <p className="text-muted-foreground">This document doesn't have any previous versions.</p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="border-t border-border bg-muted p-4">
          <div className="flex items-center justify-between text-sm text-muted-foreground">
            <div className="flex items-center space-x-4">
              <span>Total versions: {state.versions.length}</span>
              <span>Current version: {document.version}</span>
            </div>
            <button
              onClick={onClose}
              className="px-4 py-2 text-foreground border border-input rounded-full hover:bg-muted"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

// Missing X icon component
const X = ({ className }: { className?: string }) => (
  <svg className={className} fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
  </svg>
);