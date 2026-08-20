'use client';

import { logger } from '@/utils/logger';
import React from 'react';
import { Paperclip, Send, File, Download, Eye } from 'lucide-react';
import { AUTH_CONFIG } from '../../constants/auth';

interface FileShareIntegrationProps {
  workspaceId: string;
  onFileShare?: (fileId: string, fileName: string) => void;
  onFileAttach?: () => void;
}

interface SharedFile {
  id: string;
  originalFileName: string;
  fileSize: number;
  mimeType: string;
  uploadedBy: string;
  uploadedAt: string;
  downloadUrl: string;
}

interface FileShareMessageProps {
  file: SharedFile;
  onDownload?: (file: SharedFile) => void;
  onPreview?: (file: SharedFile) => void;
}

export function FileShareMessage({ file, onDownload, onPreview }: FileShareMessageProps) {
  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getFileIcon = (mimeType: string) => {
    if (mimeType.startsWith('image/')) {
      return <File className="h-6 w-6 text-success" />;
    }
    if (mimeType.includes('pdf')) {
      return <File className="h-6 w-6 text-destructive" />;
    }
    if (mimeType.includes('word') || mimeType.includes('document')) {
      return <File className="h-6 w-6 text-primary" />;
    }
    return <File className="h-6 w-6 text-muted-foreground" />;
  };

  const canPreview = (mimeType: string): boolean => {
    return mimeType.startsWith('image/') || 
           mimeType.includes('pdf') || 
           mimeType.includes('text/');
  };

  return (
    <div className="bg-muted border border-border rounded-lg p-4 max-w-sm">
      <div className="flex items-start space-x-3">
        {getFileIcon(file.mimeType)}
        <div className="flex-1 min-w-0">
          <h4 className="text-sm font-medium text-foreground truncate">
            {file.originalFileName}
          </h4>
          <p className="text-xs text-muted-foreground mt-1">
            {formatFileSize(file.fileSize)} • Shared by {file.uploadedBy}
          </p>
          <div className="flex items-center space-x-2 mt-2">
            <button
              onClick={() => onDownload?.(file)}
              className="inline-flex items-center px-2 py-1 text-xs bg-primary/10 text-primary rounded hover:bg-primary/20"
            >
              <Download className="h-3 w-3 mr-1" />
              Download
            </button>
            {canPreview(file.mimeType) && (
              <button
                onClick={() => onPreview?.(file)}
                className="inline-flex items-center px-2 py-1 text-xs bg-muted text-foreground rounded hover:bg-muted/80"
              >
                <Eye className="h-3 w-3 mr-1" />
                Preview
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export function FileAttachButton({ workspaceId, onFileAttach }: FileShareIntegrationProps) {
  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (!files || files.length === 0) return;

    const file = files[0];
    
    // Validate file size (max 50MB)
    if (file.size > 50 * 1024 * 1024) {
      alert('File size must be less than 50MB');
      return;
    }

    try {
      // Upload the file
      const formData = new FormData();
      formData.append('file', file);
      formData.append('workspaceId', workspaceId);

      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch('/api/fileshare/upload', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        body: formData
      });

      if (!response.ok) {
        throw new Error('Failed to upload file');
      }

      const result = await response.json();
      
      // Notify parent component about the file attachment
      onFileAttach?.();
      
      // You could also emit an event or call a callback with the file info
      // For integration with messaging system
      if (result.id) {
        // This could trigger a message with file attachment
        logger.info('File uploaded successfully', { result });
      }
    } catch (error) {
      logger.error('File upload error', error, { component: 'FileShareIntegration' });
      alert('Failed to upload file. Please try again.');
    }

    // Reset input
    event.target.value = '';
  };

  return (
    <div className="relative">
      <input
        type="file"
        id={`file-upload-${workspaceId}`}
        className="hidden"
        onChange={handleFileSelect}
        multiple={false}
      />
      <label
        htmlFor={`file-upload-${workspaceId}`}
        className="inline-flex items-center p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-lg cursor-pointer transition-colors"
        title="Attach file"
      >
        <Paperclip className="h-5 w-5" />
      </label>
    </div>
  );
}

// Hook for integrating file sharing with messaging
export function useFileShareIntegration(workspaceId: string) {
  const downloadFile = async (fileId: string, fileName: string) => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/fileshare/download/${fileId}`, {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to download file');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      logger.error('Download error', error, { component: 'FileShareIntegration' });
      alert('Failed to download file. Please try again.');
    }
  };

  const previewFile = async (fileId: string, mimeType: string) => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/fileshare/download/${fileId}`, {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to load file');
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      
      // Open in new window for preview
      const newWindow = window.open(url, '_blank');
      if (!newWindow) {
        // Fallback to download if popup blocked
        const link = document.createElement('a');
        link.href = url;
        link.target = '_blank';
        document.body.appendChild(link);
        link.click();
        link.remove();
      }
      
      // Clean up URL after some time
      setTimeout(() => {
        window.URL.revokeObjectURL(url);
      }, 60000);
    } catch (error) {
      logger.error('Preview error', error, { component: 'FileShareIntegration' });
      alert('Failed to preview file. Please try again.');
    }
  };

  const shareFileInMessage = async (fileId: string, messageText?: string) => {
    // This would integrate with the messaging system to share a file
    // Implementation depends on the messaging system structure
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch('/api/messaging/send', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          workspaceId,
          messageText: messageText || 'Shared a file',
          attachedFileId: fileId,
          messageType: 'FileShare'
        })
      });

      if (!response.ok) {
        throw new Error('Failed to share file in message');
      }

      return await response.json();
    } catch (error) {
      logger.error('File share error', error, { component: 'FileShareIntegration' });
      throw error;
    }
  };

  return {
    downloadFile,
    previewFile,
    shareFileInMessage
  };
}

export default useFileShareIntegration;