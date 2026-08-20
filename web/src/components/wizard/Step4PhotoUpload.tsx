'use client'

import React, { useState, useRef } from 'react'
import Image from 'next/image'
import { PhotoUpload } from '@/types/profile'

interface Step4PhotoUploadProps {
  photo: PhotoUpload
  onUpdate: (photo: PhotoUpload) => void
  onNext: () => void
  onBack: () => void
}

export default function Step4PhotoUpload({
  photo,
  onUpdate,
  onNext,
  onBack,
}: Step4PhotoUploadProps) {
  const [preview, setPreview] = useState<string | null>(photo.avatarUrl || null)
  const [selectedFile, setSelectedFile] = useState<File | null>(photo.file || null)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  /**
   * P1 SECURITY FIX: Enhanced file validation to prevent malicious uploads
   * Client-side validation + server-side validation required
   */
  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    // P1 FIX: Validate file extension (in addition to MIME type)
    const allowedExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.webp']
    const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'))
    
    if (!allowedExtensions.includes(fileExtension)) {
      setError(`Invalid file type. Allowed: ${allowedExtensions.join(', ')}`)
      return
    }

    // P1 FIX: Validate MIME type (defense in depth)
    const allowedMimeTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
    if (!allowedMimeTypes.includes(file.type)) {
      setError('Invalid image format. Please upload JPG, PNG, GIF, or WebP')
      return
    }

    // Validate file size (max 5MB)
    if (file.size > 5 * 1024 * 1024) {
      setError('Image size must be less than 5MB')
      return
    }

    // P1 FIX: Validate image dimensions and magic bytes
    try {
      await validateImageFile(file)
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Invalid image file')
      return
    }

    setError(null)
    setSelectedFile(file)

    // Create preview
    const reader = new FileReader()
    reader.onloadend = () => {
      setPreview(reader.result as string)
    }
    reader.readAsDataURL(file)
  }

  /**
   * P1 SECURITY FIX: Validate image file using magic bytes
   * This prevents disguised executables from being uploaded
   */
  const validateImageFile = async (file: File): Promise<void> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()
      
      reader.onload = (e) => {
        const arr = new Uint8Array(e.target?.result as ArrayBuffer)
        
        // Check magic bytes (file signatures)
        let isValid = false
        
        // JPEG: FF D8 FF
        if (arr[0] === 0xFF && arr[1] === 0xD8 && arr[2] === 0xFF) {
          isValid = true
        }
        // PNG: 89 50 4E 47
        else if (arr[0] === 0x89 && arr[1] === 0x50 && arr[2] === 0x4E && arr[3] === 0x47) {
          isValid = true
        }
        // GIF: 47 49 46 38
        else if (arr[0] === 0x47 && arr[1] === 0x49 && arr[2] === 0x46 && arr[3] === 0x38) {
          isValid = true
        }
        // WebP: 52 49 46 46 ... 57 45 42 50
        else if (arr[0] === 0x52 && arr[1] === 0x49 && arr[2] === 0x46 && arr[3] === 0x46 &&
                 arr[8] === 0x57 && arr[9] === 0x45 && arr[10] === 0x42 && arr[11] === 0x50) {
          isValid = true
        }
        
        if (!isValid) {
          reject(new Error('File is not a valid image (magic bytes check failed)'))
          return
        }
        
        // Additional validation: Load as image to verify it's actually an image
        const img = document.createElement('img')
        const url = URL.createObjectURL(file)
        
        img.onload = () => {
          URL.revokeObjectURL(url)
          
          // Validate reasonable dimensions (prevent memory bombs)
          if (img.width > 10000 || img.height > 10000) {
            reject(new Error('Image dimensions too large (max 10000x10000)'))
            return
          }
          
          resolve()
        }
        
        img.onerror = () => {
          URL.revokeObjectURL(url)
          reject(new Error('File cannot be loaded as an image'))
        }
        
        img.src = url
      }
      
      reader.onerror = () => {
        reject(new Error('Failed to read file'))
      }
      
      // Read first 12 bytes for magic byte validation
      reader.readAsArrayBuffer(file.slice(0, 12))
    })
  }

  const handleRemovePhoto = () => {
    setPreview(null)
    setSelectedFile(null)
    setError(null)
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  const handleNext = () => {
    onUpdate({
      avatarUrl: preview || undefined,
      file: selectedFile || undefined,
    })
    onNext()
  }

  const handleSkip = () => {
    onUpdate({ avatarUrl: undefined, file: undefined })
    onNext()
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-foreground">Profile Photo</h2>
        <p className="text-muted-foreground mt-2">
          Upload a profile picture to help others recognize you (optional)
        </p>
      </div>

      <div className="space-y-6">
        {/* Photo Preview */}
        <div className="flex justify-center">
          <div className="relative">
            {preview ? (
              <div className="relative w-48 h-48 rounded-full overflow-hidden border-4 border-border">
                <Image
                  src={preview}
                  alt="Profile preview"
                  fill
                  className="object-cover"
                  sizes="192px"
                  priority
                />
              </div>
            ) : (
              <div className="w-48 h-48 rounded-full bg-muted flex items-center justify-center border-4 border-border">
                <svg
                  className="w-24 h-24 text-muted-foreground"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path
                    fillRule="evenodd"
                    d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
            )}
          </div>
        </div>

        {/* Upload Controls */}
        <div className="flex flex-col items-center space-y-4">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            onChange={handleFileSelect}
            className="hidden"
            id="photo-upload"
          />

          {!preview ? (
            <label
              htmlFor="photo-upload"
              className="px-6 py-3 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 cursor-pointer"
            >
              Choose Photo
            </label>
          ) : (
            <div className="flex space-x-3">
              <label
                htmlFor="photo-upload"
                className="px-6 py-2 border border-input text-foreground rounded-md hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 cursor-pointer"
              >
                Change Photo
              </label>
              <button
                type="button"
                onClick={handleRemovePhoto}
                className="px-6 py-2 border border-destructive/50 text-destructive rounded-full hover:bg-destructive/10 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
              >
                Remove Photo
              </button>
            </div>
          )}

          <p className="text-sm text-muted-foreground">
            Recommended: Square image, at least 400x400px, max 5MB
          </p>
        </div>

        {/* Error Message */}
        {error && (
          <div className="bg-destructive/10 border border-destructive/20 rounded-md p-4">
            <p className="text-sm text-destructive">{error}</p>
          </div>
        )}

        {/* Info Box */}
        <div className="bg-info/10 border border-info/20 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg
                className="h-5 w-5 text-info"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fillRule="evenodd"
                  d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                  clipRule="evenodd"
                />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-info">
                A clear profile photo helps build trust with other users. You can always add or
                change your photo later from your profile settings.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation Buttons */}
      <div className="flex justify-between pt-6 border-t border-border mt-6">
        <button
          type="button"
          onClick={onBack}
          className="px-6 py-2 border border-input text-foreground rounded-full hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Back
        </button>
        <div className="flex space-x-3">
          {!preview && (
            <button
              type="button"
              onClick={handleSkip}
              className="px-6 py-2 border border-input text-foreground rounded-full hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
            >
              Skip for now
            </button>
          )}
          <button
            type="button"
            onClick={handleNext}
            className="px-6 py-2 bg-primary text-primary-foreground rounded-full hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
          >
            Next Step
          </button>
        </div>
      </div>
    </div>
  )
}
