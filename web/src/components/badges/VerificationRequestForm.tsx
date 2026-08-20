'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import Image from 'next/image'
import { useForm, Controller } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Upload, X, FileText, ExternalLink, AlertCircle, CheckCircle } from 'lucide-react'
import { Button } from '../ui/button'
import { Badge } from '../ui/badge'
import { VerificationRequestFormProps } from '../../types/badge'

const evidenceSchema = z.object({
  description: z.string().min(10, 'Please provide at least 10 characters describing your evidence'),
  urls: z.array(z.string().url('Please enter valid URLs')).optional(),
  files: z.array(z.any()).optional(),
  additionalInfo: z.string().optional()
})

type EvidenceFormData = z.infer<typeof evidenceSchema>

interface FileUpload {
  file: File
  preview?: string
  type: 'image' | 'document'
}

const badgeEvidenceRequirements: Record<string, string[]> = {
  'VERIFIED_IDENTITY': [
    'Government-issued photo ID',
    'Professional headshot',
    'LinkedIn profile verification'
  ],
  'HIGH_PERFORMER': [
    'Screenshots of high ratings',
    'Client testimonials',
    'Project completion certificates'
  ],
  'TECHNICAL_EXPERT': [
    'Certifications or degrees',
    'Portfolio links',
    'Code samples or repositories'
  ],
  'COMMUNITY_LEADER': [
    'Evidence of mentoring or teaching',
    'Community contributions',
    'Leadership roles documentation'
  ],
  'BUSINESS_VERIFIED': [
    'Business registration documents',
    'Professional licenses',
    'Company website or portfolio'
  ]
}

export default function VerificationRequestForm({ 
  badgeType, 
  onSubmit, 
  onCancel 
}: VerificationRequestFormProps) {
  const [uploadedFiles, setUploadedFiles] = useState<FileUpload[]>([])
  const [urls, setUrls] = useState<string[]>([''])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [dragActive, setDragActive] = useState(false)

  const {
    control,
    register,
    handleSubmit,
    watch,
    formState: { errors },
    setValue,
    getValues
  } = useForm<EvidenceFormData>({
    resolver: zodResolver(evidenceSchema),
    defaultValues: {
      description: '',
      urls: [],
      additionalInfo: ''
    }
  })

  const requirements = badgeEvidenceRequirements[badgeType] || [
    'Relevant documentation',
    'Supporting evidence',
    'Professional references'
  ]

  // Handle file uploads
  const handleFileUpload = (files: FileList) => {
    Array.from(files).forEach(file => {
      if (file.size > 10 * 1024 * 1024) { // 10MB limit
        alert(`File ${file.name} is too large. Maximum size is 10MB.`)
        return
      }

      const isImage = file.type.startsWith('image/')
      const upload: FileUpload = {
        file,
        type: isImage ? 'image' : 'document'
      }

      if (isImage) {
        const reader = new FileReader()
        reader.onload = (e) => {
          upload.preview = e.target?.result as string
          setUploadedFiles(prev => [...prev, upload])
        }
        reader.readAsDataURL(file)
      } else {
        setUploadedFiles(prev => [...prev, upload])
      }
    })
  }

  // Handle drag and drop
  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true)
    } else if (e.type === 'dragleave') {
      setDragActive(false)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFileUpload(e.dataTransfer.files)
    }
  }

  // URL management
  const addUrl = () => {
    setUrls(prev => [...prev, ''])
  }

  const updateUrl = (index: number, value: string) => {
    setUrls(prev => prev.map((url, i) => i === index ? value : url))
  }

  const removeUrl = (index: number) => {
    setUrls(prev => prev.filter((_, i) => i !== index))
  }

  // Remove uploaded file
  const removeFile = (index: number) => {
    setUploadedFiles(prev => prev.filter((_, i) => i !== index))
  }

  // Form submission
  const onFormSubmit = async (data: EvidenceFormData) => {
    setIsSubmitting(true)
    try {
      const evidence: Record<string, any> = {
        description: data.description,
        urls: urls.filter(url => url.trim() !== ''),
        additionalInfo: data.additionalInfo,
        files: uploadedFiles.map(upload => ({
          name: upload.file.name,
          size: upload.file.size,
          type: upload.file.type
        }))
      }

      await onSubmit(evidence)
    } catch (error) {
      logger.error('Failed to submit verification request:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const badgeDisplayName = badgeType.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())

  return (
    <div className="max-w-2xl mx-auto bg-card rounded-lg shadow-lg">
      {/* Header */}
      <div className="border-b border-border p-6">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-semibold text-foreground">Badge Verification Request</h2>
            <Badge variant="outline" className="mt-2">
              {badgeDisplayName}
            </Badge>
          </div>
          <Button variant="ghost" size="sm" onClick={onCancel}>
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <form onSubmit={handleSubmit(onFormSubmit)} className="p-6 space-y-6">
        {/* Requirements Section */}
        <div className="bg-primary/10 border border-primary/20 rounded-lg p-4">
          <h3 className="font-medium text-primary flex items-center gap-2 mb-3">
            <AlertCircle className="h-4 w-4" />
            Required Evidence
          </h3>
          <ul className="space-y-1">
            {requirements.map((requirement, index) => (
              <li key={index} className="text-sm text-primary flex items-start gap-2">
                <span className="text-primary/60">•</span>
                <span>{requirement}</span>
              </li>
            ))}
          </ul>
        </div>

        {/* Description */}
        <div>
          <label htmlFor="description" className="block text-sm font-medium text-foreground mb-2">
            Evidence Description *
          </label>
          <textarea
            {...register('description')}
            id="description"
            rows={4}
            className="w-full px-3 py-2 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            placeholder="Describe the evidence you're providing and how it demonstrates your qualification for this badge..."
          />
          {errors.description && (
            <p className="mt-1 text-sm text-destructive">{errors.description.message}</p>
          )}
        </div>

        {/* File Upload */}
        <div>
          <label className="block text-sm font-medium text-foreground mb-2">
            Upload Supporting Documents
          </label>

          <div
            className={`
              border-2 border-dashed rounded-lg p-6 text-center transition-colors
              ${dragActive ? 'border-primary/40 bg-primary/10' : 'border-border'}
            `}
            onDragEnter={handleDrag}
            onDragLeave={handleDrag}
            onDragOver={handleDrag}
            onDrop={handleDrop}
          >
            <Upload className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
            <p className="text-sm text-muted-foreground mb-2">
              Drag and drop files here, or click to select
            </p>
            <input
              type="file"
              multiple
              accept=".jpg,.jpeg,.png,.pdf,.doc,.docx"
              onChange={(e) => e.target.files && handleFileUpload(e.target.files)}
              className="hidden"
              id="file-upload"
            />
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => document.getElementById('file-upload')?.click()}
            >
              Choose Files
            </Button>
            <p className="text-xs text-muted-foreground mt-2">
              Support for images, PDFs, and documents (max 10MB each)
            </p>
          </div>

          {/* Uploaded Files */}
          {uploadedFiles.length > 0 && (
            <div className="mt-4 space-y-2">
              <h4 className="text-sm font-medium text-foreground">Uploaded Files:</h4>
              {uploadedFiles.map((upload, index) => (
                <div key={index} className="flex items-center gap-3 p-2 bg-muted rounded-lg">
                  {upload.type === 'image' ? (
                    <div className="w-10 h-10 bg-muted rounded overflow-hidden">
                      <Image
                        src={upload.preview || ''}
                        alt={upload.file.name}
                        width={40}
                        height={40}
                        className="w-full h-full object-cover"
                      />
                    </div>
                  ) : (
                    <FileText className="h-10 w-10 text-muted-foreground" />
                  )}

                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-foreground truncate">
                      {upload.file.name}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {(upload.file.size / 1024 / 1024).toFixed(2)} MB
                    </p>
                  </div>
                  
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => removeFile(index)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* URLs */}
        <div>
          <label className="block text-sm font-medium text-foreground mb-2">
            Supporting URLs
          </label>
          <div className="space-y-2">
            {urls.map((url, index) => (
              <div key={index} className="flex gap-2">
                <input
                  type="url"
                  value={url}
                  onChange={(e) => updateUrl(index, e.target.value)}
                  placeholder="https://example.com/evidence"
                  className="flex-1 px-3 py-2 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                />
                {urls.length > 1 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => removeUrl(index)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>
            ))}
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={addUrl}
              className="flex items-center gap-2"
            >
              <ExternalLink className="h-4 w-4" />
              Add URL
            </Button>
          </div>
        </div>

        {/* Additional Information */}
        <div>
          <label htmlFor="additionalInfo" className="block text-sm font-medium text-foreground mb-2">
            Additional Information
          </label>
          <textarea
            {...register('additionalInfo')}
            id="additionalInfo"
            rows={3}
            className="w-full px-3 py-2 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            placeholder="Any additional context or information that supports your verification request..."
          />
        </div>

        {/* Actions */}
        <div className="flex gap-3 pt-4 border-t border-border">
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={isSubmitting}
            className="flex-1"
          >
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={isSubmitting}
            className="flex-1"
          >
            {isSubmitting ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary-foreground mr-2"></div>
                Submitting...
              </>
            ) : (
              <>
                <CheckCircle className="h-4 w-4 mr-2" />
                Submit Request
              </>
            )}
          </Button>
        </div>
      </form>
    </div>
  )
}