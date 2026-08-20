'use client';

import React, { useState, useEffect } from 'react';
import Image from 'next/image';
import { 
  X, 
  Download, 
  Share, 
  Eye, 
  FileText, 
  Image as ImageIcon, 
  Film, 
  Music, 
  Archive,
  Code,
  ExternalLink,
  ZoomIn,
  ZoomOut,
  RotateCw,
  Maximize2,
  ChevronLeft,
  ChevronRight,
  Clock,
  User,
  HardDrive,
  Tag
} from 'lucide-react';
import { FilePreviewData, WorkspaceDocument } from '@/types/document';
import { AUTH_CONFIG } from '../../constants/auth';

interface DocumentPreviewModalProps {
  document: WorkspaceDocument;
  isOpen: boolean;
  onClose: () => void;
  onDownload?: (document: WorkspaceDocument) => void;
  onShare?: (document: WorkspaceDocument) => void;
  onNext?: () => void;
  onPrevious?: () => void;
  hasNext?: boolean;
  hasPrevious?: boolean;
}

interface PreviewState {
  previewData?: FilePreviewData;
  loading: boolean;
  error?: string;
  zoom: number;
  rotation: number;
  fullscreen: boolean;
}

export default function DocumentPreviewModal({
  document,
  isOpen,
  onClose,
  onDownload,
  onShare,
  onNext,
  onPrevious,
  hasNext = false,
  hasPrevious = false
}: DocumentPreviewModalProps) {
  const [state, setState] = useState<PreviewState>({
    loading: true,
    zoom: 1,
    rotation: 0,
    fullscreen: false
  });

  useEffect(() => {
    if (isOpen && document) {
      loadPreviewData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, document]);

  const loadPreviewData = async () => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/documents/${document.id}/preview`, {
        credentials: AUTH_CONFIG.CREDENTIALS
      });

      if (!response.ok) {
        throw new Error('Failed to load preview');
      }

      const previewData: FilePreviewData = await response.json();
      setState(prev => ({ 
        ...prev, 
        previewData, 
        loading: false,
        zoom: 1,
        rotation: 0
      }));
    } catch (error) {
      setState(prev => ({
        ...prev,
        error: error instanceof Error ? error.message : 'Failed to load preview',
        loading: false
      }));
    }
  };

  const getFileIcon = (mimeType: string) => {
    if (mimeType.startsWith('image/')) return <ImageIcon className="h-6 w-6 text-success" />;
    if (mimeType.startsWith('video/')) return <Film className="h-6 w-6 text-primary" />;
    if (mimeType.startsWith('audio/')) return <Music className="h-6 w-6 text-primary" />;
    if (mimeType.includes('pdf')) return <FileText className="h-6 w-6 text-destructive" />;
    if (mimeType.includes('zip') || mimeType.includes('archive')) return <Archive className="h-6 w-6 text-warning" />;
    if (mimeType.includes('text/') || mimeType.includes('javascript') || mimeType.includes('json')) {
      return <Code className="h-6 w-6 text-muted-foreground" />;
    }
    return <FileText className="h-6 w-6 text-muted-foreground" />;
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

  const handleZoomIn = () => {
    setState(prev => ({ ...prev, zoom: Math.min(prev.zoom * 1.5, 5) }));
  };

  const handleZoomOut = () => {
    setState(prev => ({ ...prev, zoom: Math.max(prev.zoom / 1.5, 0.1) }));
  };

  const handleRotate = () => {
    setState(prev => ({ ...prev, rotation: (prev.rotation + 90) % 360 }));
  };

  const toggleFullscreen = () => {
    setState(prev => ({ ...prev, fullscreen: !prev.fullscreen }));
  };

  // BUG-049 FIX: Check if current file is an image for zoom shortcuts
  const isImage = document.mimeType.startsWith('image/');

  const handleKeyDown = (e: React.KeyboardEvent) => {
    switch (e.key) {
      case 'Escape':
        if (state.fullscreen) {
          toggleFullscreen();
        } else {
          onClose();
        }
        break;
      case 'ArrowLeft':
        onPrevious?.();
        break;
      case 'ArrowRight':
        onNext?.();
        break;
      case '+':
      case '=':
        // BUG-049 FIX: Only zoom for images
        if (isImage) handleZoomIn();
        break;
      case '-':
        // BUG-049 FIX: Only zoom for images
        if (isImage) handleZoomOut();
        break;
      case 'r':
      case 'R':
        // BUG-049 FIX: Only rotate for images
        if (isImage) handleRotate();
        break;
    }
  };

  const renderPreviewContent = () => {
    if (state.loading) {
      return (
        <div className="flex items-center justify-center h-96">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
        </div>
      );
    }

    if (state.error || !state.previewData) {
      return (
        <div className="flex flex-col items-center justify-center h-96 text-muted-foreground">
          <FileText className="h-16 w-16 mb-4" />
          <p className="text-lg font-medium mb-2">Preview not available</p>
          <p className="text-sm">{state.error || 'This file type cannot be previewed'}</p>
          <button
            onClick={() => onDownload?.(document)}
            className="mt-4 inline-flex items-center px-4 py-2 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90"
          >
            <Download className="h-4 w-4 mr-2" />
            Download to view
          </button>
        </div>
      );
    }

    const { previewData } = state;

    if (document.mimeType.startsWith('image/')) {
      return (
        <div className="flex items-center justify-center min-h-96 bg-muted rounded-lg overflow-hidden">
          <Image
            src={previewData.previewUrl || previewData.downloadUrl}
            alt={document.originalFileName}
            width={800}
            height={600}
            className="max-w-full max-h-full object-contain transition-all duration-200"
            style={{
              transform: `scale(${state.zoom}) rotate(${state.rotation}deg)`
            }}
          />
        </div>
      );
    }

    if (document.mimeType.includes('pdf')) {
      return (
        <div className="bg-muted rounded-lg overflow-hidden" style={{ height: '600px' }}>
          <iframe
            src={`${previewData.previewUrl || previewData.downloadUrl}#toolbar=0`}
            className="w-full h-full border-0"
            title={document.originalFileName}
          />
        </div>
      );
    }

    if (document.mimeType.startsWith('text/') ||
        document.mimeType.includes('javascript') ||
        document.mimeType.includes('json')) {
      return (
        <div className="bg-muted text-success rounded-lg p-4 font-mono text-sm overflow-auto max-h-96">
          <pre className="whitespace-pre-wrap">
            {previewData.extractedText || 'Loading content...'}
          </pre>
        </div>
      );
    }

    if (document.mimeType.startsWith('video/')) {
      return (
        <div className="bg-black rounded-lg overflow-hidden">
          <video
            controls
            className="w-full h-auto max-h-[32rem]"
            src={previewData.downloadUrl}
          >
            {/* BUG-038 FIX: Show prominent fallback for video */}
            <div className="flex flex-col items-center justify-center p-8 bg-muted text-muted-foreground">
              <Film className="h-12 w-12 mb-4" />
              <p className="text-lg font-medium">Video playback not supported</p>
              <p className="text-sm mb-4">Your browser does not support video playback.</p>
              <button
                onClick={() => onDownload?.(document)}
                className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90"
              >
                <Download className="h-4 w-4 mr-2" />
                Download video
              </button>
            </div>
          </video>
        </div>
      );
    }

    if (document.mimeType.startsWith('audio/')) {
      return (
        <div className="flex flex-col items-center justify-center h-64 bg-muted rounded-lg">
          <Music className="h-16 w-16 text-primary mb-4" />
          <audio controls className="w-full max-w-md">
            <source src={previewData.downloadUrl} type={document.mimeType} />
          </audio>
          {/* BUG-038 FIX: Show fallback message below audio element */}
          <noscript>
            <p className="mt-4 text-sm text-muted-foreground">Your browser does not support audio playback.</p>
          </noscript>
        </div>
      );
    }

    return (
      <div className="flex flex-col items-center justify-center h-96 text-muted-foreground">
        {getFileIcon(document.mimeType)}
        <p className="text-lg font-medium mb-2 mt-4">Preview not supported</p>
        <p className="text-sm mb-4">This file type cannot be previewed in the browser</p>
        <button
          onClick={() => onDownload?.(document)}
          className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90"
        >
          <Download className="h-4 w-4 mr-2" />
          Download file
        </button>
      </div>
    );
  };

  if (!isOpen) return null;

  return (
    <div
      className={`fixed inset-0 bg-overlay/90 flex items-center justify-center z-50 ${
        state.fullscreen ? 'p-0' : 'p-4'
      }`}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="dialog"
      aria-modal="true"
      aria-labelledby="document-preview-title"
    >
      <div className={`bg-card rounded-lg shadow-xl max-w-6xl w-full overflow-hidden ${
        state.fullscreen ? 'h-full max-w-none rounded-none' : 'max-h-[90vh]'
      }`}>

        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border bg-muted">
          <div className="flex items-center space-x-3">
            {getFileIcon(document.mimeType)}
            <div>
              {/* BUG-010 FIX: Added ID for aria-labelledby */}
              <h3 id="document-preview-title" className="font-semibold text-foreground truncate max-w-md">
                {document.originalFileName}
              </h3>
              <p className="text-sm text-muted-foreground">
                {formatFileSize(document.fileSize)} • v{document.version}
              </p>
            </div>
          </div>
          
          <div className="flex items-center space-x-2">
            {/* Navigation */}
            {(hasPrevious || hasNext) && (
              <div className="flex items-center space-x-1 border-r border-border pr-2 mr-2">
                <button
                  onClick={onPrevious}
                  disabled={!hasPrevious}
                  className="p-1.5 text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Previous file"
                >
                  <ChevronLeft className="h-4 w-4" />
                </button>
                <button
                  onClick={onNext}
                  disabled={!hasNext}
                  className="p-1.5 text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:cursor-not-allowed"
                  title="Next file"
                >
                  <ChevronRight className="h-4 w-4" />
                </button>
              </div>
            )}

            {/* Preview Controls */}
            {state.previewData?.canPreview && document.mimeType.startsWith('image/') && (
              <div className="flex items-center space-x-1 border-r border-border pr-2 mr-2">
                <button
                  onClick={handleZoomOut}
                  className="p-1.5 text-muted-foreground hover:text-foreground"
                  title="Zoom out"
                >
                  <ZoomOut className="h-4 w-4" />
                </button>
                <span className="text-xs text-muted-foreground min-w-12 text-center">
                  {Math.round(state.zoom * 100)}%
                </span>
                <button
                  onClick={handleZoomIn}
                  className="p-1.5 text-muted-foreground hover:text-foreground"
                  title="Zoom in"
                >
                  <ZoomIn className="h-4 w-4" />
                </button>
                <button
                  onClick={handleRotate}
                  className="p-1.5 text-muted-foreground hover:text-foreground"
                  title="Rotate"
                >
                  <RotateCw className="h-4 w-4" />
                </button>
              </div>
            )}

            {/* Action Buttons - BUG-030 FIX: Added Escape hint for fullscreen */}
            <button
              onClick={toggleFullscreen}
              className="p-1.5 text-muted-foreground hover:text-foreground"
              title={state.fullscreen ? "Exit fullscreen (Esc)" : "Enter fullscreen"}
            >
              <Maximize2 className="h-4 w-4" />
            </button>

            <button
              onClick={() => onDownload?.(document)}
              className="p-1.5 text-muted-foreground hover:text-foreground"
              title="Download"
            >
              <Download className="h-4 w-4" />
            </button>

            <button
              onClick={() => onShare?.(document)}
              className="p-1.5 text-muted-foreground hover:text-foreground"
              title="Share"
            >
              <Share className="h-4 w-4" />
            </button>

            <button
              onClick={onClose}
              className="p-1.5 text-muted-foreground hover:text-foreground"
              title="Close (Esc)"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Content - BUG-054 FIX: Use larger max-height for better viewing */}
        <div className={`${state.fullscreen ? 'h-full' : 'max-h-[60vh]'} overflow-auto`}>
          <div className="p-4">
            {renderPreviewContent()}
          </div>
        </div>

        {/* Footer with metadata */}
        {!state.fullscreen && (
          <div className="border-t border-border bg-muted p-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
              <div className="flex items-center space-x-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">Uploaded by:</span>
                <span className="font-medium">{document.uploaderName}</span>
              </div>

              <div className="flex items-center space-x-2">
                <Clock className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">Uploaded:</span>
                <span className="font-medium">{formatDate(document.uploadedAt)}</span>
              </div>

              <div className="flex items-center space-x-2">
                <Download className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">Downloads:</span>
                <span className="font-medium">{document.downloadCount}</span>
              </div>
            </div>

            {document.description && (
              <div className="mt-3 pt-3 border-t border-border">
                <p className="text-sm text-foreground">{document.description}</p>
              </div>
            )}

            {document.tags && document.tags.length > 0 && (
              <div className="mt-3 pt-3 border-t border-border">
                <div className="flex items-center space-x-2">
                  <Tag className="h-4 w-4 text-muted-foreground" />
                  <div className="flex flex-wrap gap-1">
                    {document.tags.map(tag => (
                      <span key={tag} className="px-2 py-1 bg-primary/10 text-primary text-xs rounded">
                        {tag}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}