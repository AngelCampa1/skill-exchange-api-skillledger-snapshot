'use client';

import React, { useState, useCallback, useRef, useEffect } from 'react';
import { useDropzone } from 'react-dropzone';
import { Upload, X, File, AlertCircle, CheckCircle } from 'lucide-react';
import { AUTH_CONFIG } from '../../constants/auth';
import { logger } from '../../utils/logger';

interface FileUploadProps {
  workspaceId: string;
  folderId?: string;
  onUploadComplete?: (files: UploadedFile[]) => void;
  onError?: (error: string) => void;
  maxFiles?: number;
  maxSizeMB?: number;
  acceptedFileTypes?: string[];
}

interface UploadedFile {
  id: string;
  name: string;
  size: number;
  url: string;
  mimeType: string;
}

interface UploadProgress {
  file: File;
  progress: number;
  status: 'uploading' | 'success' | 'error';
  error?: string;
  result?: UploadedFile;
}

export default function FileUpload({
  workspaceId,
  folderId,
  onUploadComplete,
  onError,
  maxFiles = 10,
  maxSizeMB = 50,
  acceptedFileTypes = [
    'image/*',
    'application/pdf',
    '.doc',
    '.docx',
    '.txt',
    '.rtf',
    '.xls',
    '.xlsx',
    '.ppt',
    '.pptx'
  ]
}: FileUploadProps) {
  const [uploadProgress, setUploadProgress] = useState<UploadProgress[]>([]);
  const [isUploading, setIsUploading] = useState(false);

  // BUG-HIGH-002 FIX: Track active XHR requests for cleanup
  const activeXHRs = useRef<Set<XMLHttpRequest>>(new Set());

  // BUG-HIGH-002 FIX: Cleanup active XHR requests on unmount
  useEffect(() => {
    // Capture ref value for cleanup to satisfy react-hooks/exhaustive-deps
    const xhrs = activeXHRs.current;
    return () => {
      xhrs.forEach(xhr => {
        xhr.abort();
      });
      xhrs.clear();
    };
  }, []);

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    if (acceptedFiles.length === 0) return;

    // Check file count limit
    if (acceptedFiles.length > maxFiles) {
      onError?.(`Maximum ${maxFiles} files allowed`);
      return;
    }

    // Check file size limits
    const oversizedFiles = acceptedFiles.filter(file => file.size > maxSizeMB * 1024 * 1024);
    if (oversizedFiles.length > 0) {
      onError?.(`Files too large. Maximum size: ${maxSizeMB}MB`);
      return;
    }

    setIsUploading(true);
    const initialProgress = acceptedFiles.map(file => ({
      file,
      progress: 0,
      status: 'uploading' as const
    }));
    setUploadProgress(initialProgress);

    const uploadResults: UploadedFile[] = [];

    for (let i = 0; i < acceptedFiles.length; i++) {
      const file = acceptedFiles[i];
      try {
        const result = await uploadFile(file, workspaceId, folderId, (progress) => {
          setUploadProgress(prev => 
            prev.map((item, index) => 
              index === i ? { ...item, progress } : item
            )
          );
        });

        setUploadProgress(prev => 
          prev.map((item, index) => 
            index === i ? { ...item, status: 'success', result } : item
          )
        );

        uploadResults.push(result);
      } catch (error) {
        // BUG-MED-006 FIX: Include file name and context in error messages for better debugging
        const errorMessage = error instanceof Error ? error.message : 'Upload failed';
        const contextualError = `Failed to upload "${file.name}": ${errorMessage}`;

        setUploadProgress(prev =>
          prev.map((item, index) =>
            index === i ? { ...item, status: 'error', error: contextualError } : item
          )
        );

        logger.error('File upload failed', error, {
          fileName: file.name,
          fileSize: file.size,
          workspaceId,
          folderId
        });

        onError?.(contextualError);
      }
    }

    setIsUploading(false);
    if (uploadResults.length > 0) {
      onUploadComplete?.(uploadResults);
    }

    // BUG-HIGH-002 FIX: Clear progress after 3 seconds with cleanup
    const timer = setTimeout(() => {
      setUploadProgress([]);
    }, 3000);

    // Cleanup timer on component unmount
    return () => clearTimeout(timer);
  }, [workspaceId, folderId, maxFiles, maxSizeMB, onUploadComplete, onError]);

  const { getRootProps, getInputProps, isDragActive, fileRejections } = useDropzone({
    onDrop,
    accept: acceptedFileTypes.reduce((acc, type) => {
      acc[type] = [];
      return acc;
    }, {} as Record<string, string[]>),
    maxSize: maxSizeMB * 1024 * 1024,
    maxFiles,
    disabled: isUploading
  });

  const uploadFile = async (
    file: File, 
    workspaceId: string, 
    folderId?: string,
    onProgress?: (progress: number) => void
  ): Promise<UploadedFile> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('workspaceId', workspaceId);
    if (folderId) {
      formData.append('folderId', folderId);
    }

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // BUG-HIGH-002 FIX: Track this XHR for cleanup
      activeXHRs.current.add(xhr);

      // BUG-FE-002 FIX: Use httpOnly cookies instead of localStorage token
      xhr.withCredentials = true;

      xhr.upload.addEventListener('progress', (event) => {
        if (event.lengthComputable) {
          const progress = Math.round((event.loaded / event.total) * 100);
          onProgress?.(progress);
        }
      });

      xhr.addEventListener('load', () => {
        // BUG-HIGH-002 FIX: Remove from active set when complete
        activeXHRs.current.delete(xhr);

        if (xhr.status === 200) {
          try {
            const result = JSON.parse(xhr.responseText);
            resolve(result);
          } catch (error) {
            reject(new Error('Invalid response format'));
          }
        } else {
          try {
            const errorResponse = JSON.parse(xhr.responseText);
            reject(new Error(errorResponse.message || `HTTP ${xhr.status}`));
          } catch {
            reject(new Error(`Upload failed with status ${xhr.status}`));
          }
        }
      });

      xhr.addEventListener('error', () => {
        // BUG-HIGH-002 FIX: Remove from active set on error
        activeXHRs.current.delete(xhr);
        reject(new Error('Network error occurred'));
      });

      xhr.addEventListener('abort', () => {
        // BUG-HIGH-002 FIX: Remove from active set on abort
        activeXHRs.current.delete(xhr);
        reject(new Error('Upload cancelled'));
      });

      xhr.open('POST', '/api/fileshare/upload');
      xhr.send(formData);
    });
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getFileIcon = (fileName: string) => {
    const extension = fileName.toLowerCase().split('.').pop();
    return <File className="h-4 w-4" />;
  };

  const removeUpload = (index: number) => {
    setUploadProgress(prev => prev.filter((_, i) => i !== index));
  };

  return (
    <div className="w-full">
      {/* Drop Zone */}
      <div
        {...getRootProps()}
        className={`
          border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors
          ${isDragActive
            ? 'border-primary bg-primary/10'
            : 'border-border hover:border-muted-foreground'
          }
          ${isUploading ? 'pointer-events-none opacity-50' : ''}
        `}
      >
        <input {...getInputProps()} />
        <Upload className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
        {isDragActive ? (
          <p className="text-primary font-medium">Drop files here...</p>
        ) : (
          <div>
            <p className="text-muted-foreground mb-2">
              Drag & drop files here, or <span className="text-primary font-medium">browse</span>
            </p>
            <p className="text-sm text-muted-foreground">
              Max {maxFiles} files, {maxSizeMB}MB each
            </p>
          </div>
        )}
      </div>

      {/* File Rejections */}
      {fileRejections.length > 0 && (
        <div className="mt-4 p-3 bg-destructive/10 border border-destructive/20 rounded-lg">
          <div className="flex items-center mb-2">
            <AlertCircle className="h-4 w-4 text-destructive mr-2" />
            <span className="text-sm font-medium text-destructive">Some files were rejected:</span>
          </div>
          {fileRejections.map(({ file, errors }, index) => (
            <div key={index} className="text-sm text-destructive ml-6">
              <span className="font-medium">{file.name}</span>: {errors[0]?.message}
            </div>
          ))}
        </div>
      )}

      {/* Upload Progress */}
      {uploadProgress.length > 0 && (
        <div className="mt-4 space-y-2">
          {uploadProgress.map((item, index) => (
            <div key={index} className="p-3 border border-border rounded-lg">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center space-x-2">
                  {getFileIcon(item.file.name)}
                  <span className="text-sm font-medium text-foreground truncate">
                    {item.file.name}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    ({formatFileSize(item.file.size)})
                  </span>
                </div>
                <div className="flex items-center space-x-2">
                  {item.status === 'success' && (
                    <CheckCircle className="h-4 w-4 text-success" />
                  )}
                  {item.status === 'error' && (
                    <AlertCircle className="h-4 w-4 text-destructive" />
                  )}
                  <button
                    onClick={() => removeUpload(index)}
                    className="text-muted-foreground hover:text-foreground"
                    aria-label={`Remove ${item.file.name} from upload queue`}
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              </div>

              {item.status === 'uploading' && (
                <div className="w-full bg-muted rounded-full h-2">
                  <div
                    className="bg-primary h-2 rounded-full transition-all duration-300"
                    style={{ width: `${item.progress}%` }}
                  ></div>
                </div>
              )}

              {item.status === 'error' && item.error && (
                <p className="text-sm text-destructive mt-1">{item.error}</p>
              )}

              {item.status === 'success' && (
                <p className="text-sm text-success mt-1">Upload successful!</p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}