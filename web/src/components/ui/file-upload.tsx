import * as React from "react"
import Image from "next/image"
import { Upload, X, File, FileImage, FileText, FileVideo, FileAudio } from "lucide-react"
import { Button } from "./button"

export interface FileUploadProps {
  onFilesChange?: (files: File[]) => void
  accept?: string
  maxSize?: number
  maxFiles?: number
  multiple?: boolean
  disabled?: boolean
  label?: string
  helperText?: string
  error?: boolean
  className?: string
  showPreview?: boolean
  // BUG-039 FIX: Add files prop to sync preview with external state
  files?: File[]
  // BUG-055 FIX: Add id for label association
  id?: string
}

// BUG-051 FIX: Parse and format accept prop nicely
function formatAcceptTypes(accept?: string): string {
  if (!accept) return "Any file type"

  // Split by comma and process each type
  const types = accept.split(',').map(t => t.trim())
  const formatted = types.map(type => {
    // Handle MIME types like image/*, video/*, etc.
    if (type.includes('/')) {
      const [category, ext] = type.split('/')
      if (ext === '*') return category.charAt(0).toUpperCase() + category.slice(1) + 's'
      return ext.toUpperCase()
    }
    // Handle extensions like .pdf, .doc, etc.
    if (type.startsWith('.')) {
      return type.substring(1).toUpperCase()
    }
    return type.toUpperCase()
  })

  // Remove duplicates and join
  const unique = Array.from(new Set(formatted))
  if (unique.length > 3) {
    return `${unique.slice(0, 3).join(', ')} +${unique.length - 3} more`
  }
  return unique.join(', ')
}

// BUG-031 FIX: Validate file type against accept types
function isFileTypeValid(file: File, accept?: string): boolean {
  if (!accept) return true

  const acceptedTypes = accept.split(',').map(t => t.trim().toLowerCase())
  const fileType = file.type.toLowerCase()
  const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase()

  return acceptedTypes.some(acceptType => {
    // Handle wildcard MIME types (e.g., image/*)
    if (acceptType.endsWith('/*')) {
      const category = acceptType.split('/')[0]
      return fileType.startsWith(category + '/')
    }
    // Handle exact MIME types
    if (acceptType.includes('/')) {
      return fileType === acceptType
    }
    // Handle file extensions
    if (acceptType.startsWith('.')) {
      return fileExtension === acceptType
    }
    return false
  })
}

// Utility function to format file sizes
function formatFileSize(bytes: number): string {
  if (bytes === 0) return "0 B"
  const k = 1024
  const sizes = ["B", "KB", "MB", "GB"]
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`
}

export const FileUpload = React.forwardRef<HTMLDivElement, FileUploadProps>(
  (
    {
      onFilesChange,
      accept,
      maxSize = 10 * 1024 * 1024, // 10MB default
      maxFiles = 1,
      multiple = false,
      disabled = false,
      label,
      helperText,
      error,
      className,
      showPreview = true,
      // BUG-039 FIX: Add files prop with default
      files: filesProp,
      // BUG-055 FIX: Add id for label association
      id,
    },
    ref
  ) => {
    const [internalFiles, setInternalFiles] = React.useState<File[]>([])
    const [isDragging, setIsDragging] = React.useState(false)
    const [errorMessage, setErrorMessage] = React.useState<string | null>(null)
    const inputRef = React.useRef<HTMLInputElement>(null)

    // BUG-055 FIX: Generate unique id if not provided (hook must be called unconditionally)
    const generatedId = React.useId()
    const inputId = id || generatedId

    // BUG-039 FIX: Use files prop if provided, otherwise use internal state
    const files = filesProp !== undefined ? filesProp : internalFiles
    const setFiles = React.useCallback((newFiles: File[]) => {
      setInternalFiles(newFiles)
    }, [])

    // BUG-039 FIX: Sync internal state with files prop
    React.useEffect(() => {
      if (filesProp !== undefined) {
        setInternalFiles(filesProp)
      }
    }, [filesProp])

    const handleFiles = React.useCallback((newFiles: FileList | null) => {
      if (!newFiles || newFiles.length === 0) return

      setErrorMessage(null)
      const fileArray = Array.from(newFiles)

      // Validate file count
      if (files.length + fileArray.length > maxFiles) {
        setErrorMessage(`Maximum ${maxFiles} file${maxFiles > 1 ? "s" : ""} allowed`)
        return
      }

      // BUG-031 FIX: Validate file types against accept prop
      const invalidTypeFiles = fileArray.filter((file) => !isFileTypeValid(file, accept))
      if (invalidTypeFiles.length > 0) {
        setErrorMessage(
          `Invalid file type${invalidTypeFiles.length > 1 ? "s" : ""}. Accepted: ${formatAcceptTypes(accept)}`
        )
        return
      }

      // Validate file sizes
      const invalidFiles = fileArray.filter((file) => file.size > maxSize)
      if (invalidFiles.length > 0) {
        setErrorMessage(
          `File${invalidFiles.length > 1 ? "s" : ""} too large. Maximum size: ${formatFileSize(maxSize)}`
        )
        return
      }

      const updatedFiles = multiple ? [...files, ...fileArray] : fileArray
      setFiles(updatedFiles)
      onFilesChange?.(updatedFiles)
    }, [files, maxFiles, accept, maxSize, multiple, setFiles, onFilesChange])

    const removeFile = React.useCallback((index: number) => {
      const updatedFiles = files.filter((_, i) => i !== index)
      setFiles(updatedFiles)
      onFilesChange?.(updatedFiles)
      setErrorMessage(null)
    }, [files, setFiles, onFilesChange])

    const clearAll = React.useCallback(() => {
      setFiles([])
      onFilesChange?.([])
      setErrorMessage(null)
      if (inputRef.current) {
        inputRef.current.value = ""
      }
    }, [setFiles, onFilesChange])

    const handleDragOver = React.useCallback((e: React.DragEvent) => {
      e.preventDefault()
      if (!disabled) {
        setIsDragging(true)
      }
    }, [disabled])

    const handleDragLeave = React.useCallback((e: React.DragEvent) => {
      e.preventDefault()
      setIsDragging(false)
    }, [])

    const handleDrop = React.useCallback((e: React.DragEvent) => {
      e.preventDefault()
      setIsDragging(false)
      if (!disabled) {
        handleFiles(e.dataTransfer.files)
      }
    }, [disabled, handleFiles])

    const handleClick = React.useCallback(() => {
      if (!disabled) {
        inputRef.current?.click()
      }
    }, [disabled])

    // BUG-040 FIX: Add keyboard handler for drop zone
    const handleKeyDown = React.useCallback((e: React.KeyboardEvent) => {
      if (disabled) return
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault()
        inputRef.current?.click()
      }
    }, [disabled])

    const getFileIcon = React.useCallback((file: File) => {
      if (file.type.startsWith("image/")) return <FileImage className="h-5 w-5" />
      if (file.type.startsWith("video/")) return <FileVideo className="h-5 w-5" />
      if (file.type.startsWith("audio/")) return <FileAudio className="h-5 w-5" />
      if (file.type.includes("pdf") || file.type.includes("text"))
        return <FileText className="h-5 w-5" />
      return <File className="h-5 w-5" />
    }, [])

    return (
      <div ref={ref} className={`w-full space-y-3 ${className || ""}`}>
        {/* BUG-055 FIX: Associate label with input using htmlFor */}
        {label && (
          <label
            htmlFor={inputId}
            className={`text-sm font-medium ${
              error || errorMessage ? "text-destructive" : "text-foreground"
            }`}
          >
            {label}
          </label>
        )}

        {/* BUG-040 FIX: Add tabIndex, role, and onKeyDown for keyboard accessibility */}
        <div
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={handleClick}
          onKeyDown={handleKeyDown}
          tabIndex={disabled ? -1 : 0}
          role="button"
          aria-label={label ? `${label} drop zone` : "File upload drop zone"}
          className={`
            relative border-2 border-dashed rounded-xl p-8 transition-all duration-200
            focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2
            ${
              isDragging
                ? "border-primary bg-primary/5"
                : error || errorMessage
                ? "border-destructive bg-destructive/5"
                : "border-border bg-background hover:border-primary hover:bg-accent/50"
            }
            ${disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}
          `}
        >
          {/* BUG-055 FIX: Add id for label association */}
          <input
            ref={inputRef}
            id={inputId}
            type="file"
            accept={accept}
            multiple={multiple && files.length < maxFiles}
            onChange={(e) => handleFiles(e.target.files)}
            disabled={disabled}
            className="hidden"
            aria-label={label || "File upload"}
          />

          <div className="flex flex-col items-center justify-center space-y-3 text-center">
            <div
              className={`p-3 rounded-full ${
                isDragging ? "bg-primary/10" : "bg-muted"
              }`}
            >
              <Upload
                className={`h-6 w-6 ${
                  isDragging ? "text-primary" : "text-muted-foreground"
                }`}
                aria-hidden="true"
              />
            </div>

            <div className="space-y-1">
              <p className="text-sm font-medium text-foreground">
                {isDragging
                  ? "Drop files here"
                  : "Click to upload or drag and drop"}
              </p>
              {/* BUG-051 FIX: Use formatted accept types */}
              <p className="text-xs text-muted-foreground">
                {formatAcceptTypes(accept)}{" "}
                • Max {formatFileSize(maxSize)} • Up to {maxFiles} file
                {maxFiles > 1 ? "s" : ""}
              </p>
            </div>
          </div>
        </div>

        {/* File Previews */}
        {showPreview && files.length > 0 && (
          <div className="space-y-2">
            {files.map((file, index) => (
              <div
                key={index}
                className="flex items-center justify-between p-3 bg-muted rounded-lg"
              >
                <div className="flex items-center space-x-3 flex-1 min-w-0">
                  <div className="text-muted-foreground flex-shrink-0">
                    {getFileIcon(file)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-foreground truncate">
                      {file.name}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {formatFileSize(file.size)}
                    </p>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => removeFile(index)}
                  disabled={disabled}
                  className="p-1.5 hover:bg-background rounded-full transition-colors flex-shrink-0"
                  aria-label={`Remove ${file.name}`}
                >
                  <X className="h-4 w-4 text-muted-foreground" />
                </button>
              </div>
            ))}

            {files.length > 0 && (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={clearAll}
                disabled={disabled}
                className="w-full"
              >
                Clear all
              </Button>
            )}
          </div>
        )}

        {(helperText || errorMessage) && (
          <p
            className={`text-xs ${
              error || errorMessage ? "text-destructive" : "text-muted-foreground"
            }`}
            role={error || errorMessage ? "alert" : undefined}
          >
            {errorMessage || helperText}
          </p>
        )}
      </div>
    )
  }
)

FileUpload.displayName = "FileUpload"

// Image upload variant with preview
export interface ImageUploadProps {
  onImageChange?: (file: File | null) => void
  currentImage?: string
  maxSize?: number
  disabled?: boolean
  label?: string
  helperText?: string
  error?: boolean
  className?: string
  aspectRatio?: "square" | "video" | "portrait" | "auto"
}

export const ImageUpload = React.forwardRef<HTMLDivElement, ImageUploadProps>(
  (
    {
      onImageChange,
      currentImage,
      maxSize = 5 * 1024 * 1024,
      disabled = false,
      label,
      helperText,
      error,
      className,
      aspectRatio = "square",
    },
    ref
  ) => {
    const [preview, setPreview] = React.useState<string | null>(currentImage || null)
    const [errorMessage, setErrorMessage] = React.useState<string | null>(null)
    const inputRef = React.useRef<HTMLInputElement>(null)

    const aspectRatioClasses = {
      square: "aspect-square",
      video: "aspect-video",
      portrait: "aspect-[3/4]",
      auto: "aspect-auto",
    }

    const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0]
      if (!file) return

      setErrorMessage(null)

      if (file.size > maxSize) {
        setErrorMessage(`Image too large. Maximum size: ${formatFileSize(maxSize)}`)
        return
      }

      if (!file.type.startsWith("image/")) {
        setErrorMessage("Please select an image file")
        return
      }

      const reader = new FileReader()
      reader.onloadend = () => {
        setPreview(reader.result as string)
      }
      reader.readAsDataURL(file)

      onImageChange?.(file)
    }

    const handleRemove = () => {
      setPreview(null)
      setErrorMessage(null)
      onImageChange?.(null)
      if (inputRef.current) {
        inputRef.current.value = ""
      }
    }

    return (
      <div ref={ref} className={`w-full space-y-3 ${className || ""}`}>
        {label && (
          <label
            className={`text-sm font-medium ${
              error || errorMessage ? "text-destructive" : "text-foreground"
            }`}
          >
            {label}
          </label>
        )}

        <div
          className={`relative border-2 border-dashed rounded-xl overflow-hidden ${
            preview ? "border-transparent" : "border-border"
          } ${aspectRatioClasses[aspectRatio]}`}
        >
          {preview ? (
            <div className="relative w-full h-full">
              <Image
                src={preview}
                alt="Preview"
                fill
                className="object-cover"
                unoptimized
              />
              <button
                type="button"
                onClick={handleRemove}
                disabled={disabled}
                className="absolute top-2 right-2 p-2 bg-foreground/50 hover:bg-foreground/70 text-background rounded-full transition-colors"
                aria-label="Remove image"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          ) : (
            <label
              className={`flex flex-col items-center justify-center w-full h-full p-8 cursor-pointer hover:bg-accent/50 transition-colors ${
                disabled ? "opacity-50 cursor-not-allowed" : ""
              }`}
            >
              <input
                ref={inputRef}
                type="file"
                accept="image/*"
                onChange={handleImageChange}
                disabled={disabled}
                className="hidden"
              />
              <div className="p-3 bg-muted rounded-full mb-3">
                <Upload className="h-6 w-6 text-muted-foreground" aria-hidden="true" />
              </div>
              <p className="text-sm font-medium text-foreground mb-1">
                Click to upload image
              </p>
              <p className="text-xs text-muted-foreground">
                PNG, JPG, GIF • Max {formatFileSize(maxSize)}
              </p>
            </label>
          )}
        </div>

        {(helperText || errorMessage) && (
          <p
            className={`text-xs ${
              error || errorMessage ? "text-destructive" : "text-muted-foreground"
            }`}
            role={error || errorMessage ? "alert" : undefined}
          >
            {errorMessage || helperText}
          </p>
        )}
      </div>
    )
  }
)

ImageUpload.displayName = "ImageUpload"
