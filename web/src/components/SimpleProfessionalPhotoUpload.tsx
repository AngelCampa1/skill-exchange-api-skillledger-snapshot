'use client'

import { logger } from '@/utils/logger';

import React, { useState, useRef, useCallback } from 'react'
import Image from 'next/image'

interface PhotoUploadResult {
  success: boolean
  fileId?: string
  fileUrl?: string
  error?: string
  moderationStatus?: 'pending' | 'approved' | 'rejected'
  requiresHumanReview?: boolean
}

interface SimpleProfessionalPhotoUploadProps {
  onUploadComplete: (result: PhotoUploadResult) => void
  currentPhotoUrl?: string
  isLoading?: boolean
}

interface UploadProgress {
  loaded: number
  total: number
  percentage: number
}

export default function SimpleProfessionalPhotoUpload({
  onUploadComplete,
  currentPhotoUrl,
  isLoading = false
}: SimpleProfessionalPhotoUploadProps) {
  const [uploading, setUploading] = useState(false)
  const [uploadProgress, setUploadProgress] = useState<UploadProgress | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(currentPhotoUrl || null)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [moderationPending, setModerationPending] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [dragOver, setDragOver] = useState(false)

  const validateFile = (file: File): { isValid: boolean; error?: string } => {
    // File size validation (5MB max)
    const maxSize = 5 * 1024 * 1024 // 5MB
    if (file.size > maxSize) {
      return { isValid: false, error: 'File size must be less than 5MB' }
    }

    // File type validation
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp']
    if (!allowedTypes.includes(file.type)) {
      return { isValid: false, error: 'Only JPEG, PNG, and WebP images are allowed' }
    }

    // File name validation
    if (file.name.length > 255) {
      return { isValid: false, error: 'File name is too long' }
    }

    return { isValid: true }
  }

  const createPreview = (file: File): string => {
    return URL.createObjectURL(file)
  }

  const uploadFile = async (file: File): Promise<PhotoUploadResult> => {
    try {
      // Fetch CSRF token
      const csrfResponse = await fetch('/api/csrf-token')
      if (!csrfResponse.ok) {
        throw new Error('Failed to get security token')
      }
      const { token: csrfToken } = await csrfResponse.json()

      // Create form data
      const formData = new FormData()
      formData.append('file', file)
      formData.append('type', 'profile-photo')

      // Upload with progress tracking
      const xhr = new XMLHttpRequest()
      
      return new Promise((resolve, reject) => {
        xhr.upload.addEventListener('progress', (event) => {
          if (event.lengthComputable) {
            const progress: UploadProgress = {
              loaded: event.loaded,
              total: event.total,
              percentage: Math.round((event.loaded / event.total) * 100)
            }
            setUploadProgress(progress)
          }
        })

        xhr.addEventListener('load', () => {
          setUploadProgress(null)
          
          if (xhr.status >= 200 && xhr.status < 300) {
            try {
              const result = JSON.parse(xhr.responseText)
              resolve(result)
            } catch (parseError) {
              reject(new Error('Invalid response from server'))
            }
          } else {
            try {
              const errorResult = JSON.parse(xhr.responseText)
              resolve({
                success: false,
                error: errorResult.error || 'Upload failed'
              })
            } catch {
              resolve({
                success: false,
                error: `Upload failed with status ${xhr.status}`
              })
            }
          }
        })

        xhr.addEventListener('error', () => {
          setUploadProgress(null)
          reject(new Error('Network error during upload'))
        })

        xhr.addEventListener('abort', () => {
          setUploadProgress(null)
          reject(new Error('Upload cancelled'))
        })

        xhr.open('POST', '/api/profile/avatar/upload')
        xhr.setRequestHeader('X-CSRF-Token', csrfToken)
        xhr.send(formData)
      })
    } catch (error) {
      setUploadProgress(null)
      logger.error('Upload error', error, { component: 'SimpleProfessionalPhotoUpload' })
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Upload failed'
      }
    }
  }

  const handleFileSelect = useCallback(async (files: FileList | null) => {
    if (!files || files.length === 0) return
    
    const file = files[0]
    setError(null)
    setSuccess(null)
    setModerationPending(false)

    // Validate file
    const validation = validateFile(file)
    if (!validation.isValid) {
      setError(validation.error!)
      return
    }

    // Create preview
    const preview = createPreview(file)
    setPreviewUrl(preview)

    // Start upload
    setUploading(true)
    
    try {
      const result = await uploadFile(file)
      
      if (result.success) {
        if (result.moderationStatus === 'pending') {
          setModerationPending(true)
          setSuccess('Photo uploaded successfully! Content review in progress.')
        } else if (result.moderationStatus === 'approved') {
          setSuccess('Photo uploaded and approved successfully!')
        } else {
          setSuccess('Photo uploaded successfully!')
        }
        
        onUploadComplete(result)
      } else {
        setError(result.error || 'Upload failed')
        // Restore previous preview on error
        setPreviewUrl(currentPhotoUrl || null)
      }
    } catch (error) {
      logger.error('Upload error', error, { component: 'SimpleProfessionalPhotoUpload' })
      setError('Upload failed. Please try again.')
      setPreviewUrl(currentPhotoUrl || null)
    } finally {
      setUploading(false)
    }
  }, [currentPhotoUrl, onUploadComplete])

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(true)
  }

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(false)
    handleFileSelect(e.dataTransfer.files)
  }

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    handleFileSelect(e.target.files)
  }

  const handleClickUpload = () => {
    if (!isLoading && !uploading) {
      fileInputRef.current?.click()
    }
  }

  const handleRemovePhoto = async () => {
    if (!currentPhotoUrl) return
    
    try {
      const csrfResponse = await fetch('/api/csrf-token')
      if (!csrfResponse.ok) {
        throw new Error('Failed to get security token')
      }
      const { token: csrfToken } = await csrfResponse.json()

      const response = await fetch('/api/profile/avatar', {
        method: 'DELETE',
        headers: {
          'X-CSRF-Token': csrfToken,
        }
      })

      if (response.ok) {
        setPreviewUrl(null)
        setSuccess('Photo removed successfully')
        onUploadComplete({ success: true, fileId: undefined, fileUrl: undefined })
      } else {
        setError('Failed to remove photo')
      }
    } catch (error) {
      logger.error('Remove photo error', error, { component: 'SimpleProfessionalPhotoUpload' })
      setError('Failed to remove photo')
    }
  }

  return (
    <div className="w-full max-w-md mx-auto">
      <div className="space-y-4">
        {/* Photo Preview */}
        {previewUrl && (
          <div className="relative">
            <div className="w-32 h-32 mx-auto rounded-full overflow-hidden bg-muted border-4 border-border relative">
              <Image
                src={previewUrl}
                alt="Profile preview"
                fill
                className="object-cover"
              />
            </div>

            {moderationPending && (
              <div className="absolute -bottom-2 -right-2">
                <div className="bg-warning/10 border border-warning/20 rounded-full p-1">
                  <svg className="w-4 h-4 text-warning" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
                  </svg>
                </div>
              </div>
            )}

            <button
              onClick={handleRemovePhoto}
              className="absolute -top-2 -right-2 bg-destructive/10 hover:bg-destructive/20 border border-destructive/20 rounded-full p-1"
              title="Remove photo"
            >
              <svg className="w-4 h-4 text-destructive" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
              </svg>
            </button>
          </div>
        )}

        {/* Hidden file input */}
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/jpg,image/png,image/webp"
          onChange={handleFileInputChange}
          className="hidden"
          disabled={isLoading || uploading}
        />

        {/* Upload Area */}
        <div
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={handleClickUpload}
          className={`
            border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors
            ${dragOver ? 'border-primary bg-primary/10' : 'border-border hover:border-primary/50'}
            ${(isLoading || uploading) ? 'opacity-50 cursor-not-allowed' : ''}
          `}
        >
          {uploading ? (
            <div className="space-y-2">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
              <p className="text-sm text-muted-foreground">
                Uploading... {uploadProgress?.percentage || 0}%
              </p>
              {uploadProgress && (
                <div className="w-full bg-muted rounded-full h-2">
                  <div
                    className="bg-primary h-2 rounded-full transition-all"
                    style={{ width: `${uploadProgress.percentage}%` }}
                  />
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-2">
              <svg className="mx-auto h-12 w-12 text-muted-foreground" stroke="currentColor" fill="none" viewBox="0 0 48 48">
                <path d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
              </svg>
              <div className="text-sm text-muted-foreground">
                <p className="font-medium">
                  {dragOver ? 'Drop your photo here' : 'Upload a professional photo'}
                </p>
                <p className="text-xs text-muted-foreground mt-1">
                  Drag and drop or click to select
                </p>
                <p className="text-xs text-muted-foreground">
                  JPEG, PNG, or WebP • Max 5MB • Square format recommended
                </p>
              </div>
            </div>
          )}
        </div>

        {/* Status Messages */}
        {error && (
          <div className="bg-destructive/10 border border-destructive/20 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-destructive">{error}</p>
              </div>
            </div>
          </div>
        )}

        {success && (
          <div className="bg-success/10 border border-success/20 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-success" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-success">{success}</p>
              </div>
            </div>
          </div>
        )}

        {moderationPending && (
          <div className="bg-warning/10 border border-warning/20 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-warning" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-warning">
                  Content Review in Progress
                </h3>
                <div className="mt-2 text-sm text-warning">
                  <p>Your photo is being reviewed for content policy compliance. This usually takes a few minutes.</p>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Photo Guidelines */}
        <div className="bg-muted border border-border rounded-md p-4">
          <h4 className="text-sm font-medium text-foreground mb-2">Professional Photo Guidelines</h4>
          <ul className="text-xs text-muted-foreground space-y-1">
            <li>• Use a clear, high-quality headshot</li>
            <li>• Face should be clearly visible and well-lit</li>
            <li>• Professional attire recommended</li>
            <li>• Avoid group photos, selfies, or inappropriate content</li>
            <li>• Square format works best (1:1 aspect ratio)</li>
          </ul>
        </div>
      </div>
    </div>
  )
}