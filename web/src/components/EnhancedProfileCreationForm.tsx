'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import SimpleProfessionalPhotoUpload from './SimpleProfessionalPhotoUpload'

const profileCreationSchema = z.object({
  firstName: z.string().optional(),
  lastName: z.string().optional(),
  title: z.string().optional(),
  summary: z.string().optional(),
  company: z.string().optional(),
  websiteUrl: z.string().url('Please enter a valid URL').optional().or(z.literal('')),
  linkedInUrl: z.string().url('Please enter a valid LinkedIn URL').optional().or(z.literal('')),
  gitHubUrl: z.string().url('Please enter a valid GitHub URL').optional().or(z.literal('')),
  location: z.string().optional(),
  timeZone: z.string().optional(),
  isPublic: z.boolean().default(false),
  profileSlug: z.string().optional(),
})

type ProfileCreationFormData = z.infer<typeof profileCreationSchema>

interface ContentModerationResult {
  isApproved: boolean
  severity: 'safe' | 'low' | 'medium' | 'high'
  flaggedContent: string[]
  suggestions: string[]
  requiresHumanReview: boolean
}

interface SlugAvailabilityResult {
  isAvailable: boolean
  suggestion?: string
}

interface EnhancedProfileCreationFormProps {
  onSubmit: (data: ProfileCreationFormData & { photoFileId?: string }) => Promise<void>
  isLoading?: boolean
  initialData?: Partial<ProfileCreationFormData & { profilePhotoUrl?: string }>
  submitButtonText?: string
}

export default function EnhancedProfileCreationForm({ 
  onSubmit, 
  isLoading = false, 
  initialData,
  submitButtonText = "Create Profile"
}: EnhancedProfileCreationFormProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  const [photoFileId, setPhotoFileId] = useState<string | undefined>()
  const [contentModeration, setContentModeration] = useState<{[key: string]: ContentModerationResult}>({})
  const [moderationPending, setModerationPending] = useState<string[]>([])
  const [slugAvailability, setSlugAvailability] = useState<SlugAvailabilityResult | null>(null)
  const [checkingSlug, setCheckingSlug] = useState(false)
  
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileCreationFormData>({
    resolver: zodResolver(profileCreationSchema),
    mode: 'onChange',
    reValidateMode: 'onChange',
    defaultValues: initialData || {
      isPublic: false,
    },
  })

  const watchedFields = watch()
  
  // Generate slug from name
  const generateSlug = useCallback((firstName?: string, lastName?: string): string => {
    if (!firstName && !lastName) return ''
    
    const fullName = `${firstName || ''} ${lastName || ''}`.trim()
    return fullName
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '') // Remove special characters
      .replace(/\s+/g, '-') // Replace spaces with hyphens
      .replace(/-+/g, '-') // Replace multiple hyphens with single
      .replace(/^-|-$/g, '') // Remove leading/trailing hyphens
  }, [])

  // Auto-generate slug when name changes
  useEffect(() => {
    if (watchedFields.firstName || watchedFields.lastName) {
      const generatedSlug = generateSlug(watchedFields.firstName, watchedFields.lastName)
      if (generatedSlug && generatedSlug !== watchedFields.profileSlug) {
        setValue('profileSlug', generatedSlug, { shouldValidate: true })
      }
    }
  }, [watchedFields.firstName, watchedFields.lastName, generateSlug, setValue, watchedFields.profileSlug])

  // Check slug availability
  useEffect(() => {
    if (watchedFields.profileSlug && watchedFields.profileSlug.length >= 3) {
      checkSlugAvailability(watchedFields.profileSlug)
    }
  }, [watchedFields.profileSlug])

  const moderateContent = useCallback(async (fieldName: string, content: string) => {
    if (moderationPending.includes(fieldName)) return
    
    setModerationPending(prev => [...prev, fieldName])
    
    try {
      const response = await fetch('/api/content/moderate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ content, context: fieldName }),
      })

      if (response.ok) {
        const result: ContentModerationResult = await response.json()
        setContentModeration(prev => ({ ...prev, [fieldName]: result }))
      }
    } catch (error) {
      logger.error('Content moderation failed:', error)
      // Allow submission if moderation fails
      setContentModeration(prev => ({ 
        ...prev, 
        [fieldName]: { 
          isApproved: true, 
          severity: 'safe', 
          flaggedContent: [], 
          suggestions: [],
          requiresHumanReview: false 
        } 
      }))
    } finally {
      setModerationPending(prev => prev.filter(f => f !== fieldName))
    }
  }, [moderationPending])

  const checkSlugAvailability = async (slug: string) => {
    setCheckingSlug(true)
    
    try {
      const response = await fetch(`/api/profile/check-slug?slug=${encodeURIComponent(slug)}`)
      if (response.ok) {
        const result: SlugAvailabilityResult = await response.json()
        setSlugAvailability(result)
      }
    } catch (error) {
      logger.error('Slug availability check failed:', error)
      setSlugAvailability({ isAvailable: true })
    } finally {
      setCheckingSlug(false)
    }
  }

  // Content moderation for text fields
  useEffect(() => {
    const fieldsToModerate = ['title', 'summary', 'company']
    
    fieldsToModerate.forEach(field => {
      const value = watchedFields[field as keyof ProfileCreationFormData] as string
      if (value && value.length > 10) {
        moderateContent(field, value)
      }
    })
  }, [watchedFields.title, watchedFields.summary, watchedFields.company, moderateContent, watchedFields])

  const handlePhotoUpload = (result: any) => {
    if (result.success && result.fileId) {
      setPhotoFileId(result.fileId)
    } else if (!result.success) {
      setPhotoFileId(undefined)
    }
  }

  const handleFormSubmit = async (data: ProfileCreationFormData) => {
    // Check for blocked content
    const hasBlockedContent = Object.values(contentModeration).some(
      result => !result.isApproved && result.severity === 'high'
    )
    
    if (hasBlockedContent) {
      return
    }
    
    await onSubmit({
      ...data,
      photoFileId
    })
  }

  const getFieldModerationStatus = (fieldName: string) => {
    const moderation = contentModeration[fieldName]
    if (!moderation) return null
    
    if (moderation.severity === 'high' && !moderation.isApproved) {
      return {
        type: 'error',
        message: `Content violates community guidelines: ${moderation.flaggedContent.join(', ')}`
      }
    }
    
    if (moderation.severity === 'medium' && moderation.requiresHumanReview) {
      return {
        type: 'warning',
        message: 'Content is under review and may require approval before publishing'
      }
    }
    
    if (moderation.suggestions.length > 0) {
      return {
        type: 'info',
        message: `Suggestions: ${moderation.suggestions.join(', ')}`
      }
    }
    
    return null
  }

  return (
    <div className="w-full max-w-2xl mx-auto">
      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-6">
        {/* Photo Upload Section */}
        <div className="border-b border-border pb-6">
          <h3 className="text-lg font-medium text-foreground mb-4">Profile Photo</h3>
          <SimpleProfessionalPhotoUpload
            onUploadComplete={handlePhotoUpload}
            currentPhotoUrl={initialData?.profilePhotoUrl}
            isLoading={isLoading}
          />
        </div>

        {/* Basic Information */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label htmlFor="firstName" className="block text-sm font-medium text-foreground">
              First Name
            </label>
            <input
              {...register('firstName')}
              type="text"
              id="firstName"
              className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
              placeholder="Your first name"
              disabled={isLoading || isSubmitting}
            />
            {errors.firstName && (
              <p className="mt-1 text-sm text-destructive">{errors.firstName.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="lastName" className="block text-sm font-medium text-foreground">
              Last Name
            </label>
            <input
              {...register('lastName')}
              type="text"
              id="lastName"
              className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
              placeholder="Your last name"
              disabled={isLoading || isSubmitting}
            />
            {errors.lastName && (
              <p className="mt-1 text-sm text-destructive">{errors.lastName.message}</p>
            )}
          </div>
        </div>

        {/* Profile URL */}
        <div>
          <label htmlFor="profileSlug" className="block text-sm font-medium text-foreground">
            Profile URL
          </label>
          <div className="mt-1 flex rounded-md shadow-sm">
            <span className="inline-flex items-center px-3 py-2 rounded-l-md border border-r-0 border-input bg-muted text-muted-foreground text-sm">
              skillledger.app/
            </span>
            <input
              {...register('profileSlug')}
              type="text"
              id="profileSlug"
              className="flex-1 block w-full px-3 py-2 border border-input rounded-none rounded-r-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
              placeholder="your-name"
              disabled={isLoading || isSubmitting}
            />
          </div>
          {checkingSlug && (
            <p className="mt-1 text-sm text-muted-foreground">Checking availability...</p>
          )}
          {slugAvailability && !checkingSlug && (
            <div className={`mt-1 text-sm ${slugAvailability.isAvailable ? 'text-success' : 'text-destructive'}`}>
              {slugAvailability.isAvailable ? (
                '✓ URL is available'
              ) : (
                `✗ URL not available. Try: ${slugAvailability.suggestion}`
              )}
            </div>
          )}
          {errors.profileSlug && (
            <p className="mt-1 text-sm text-destructive">{errors.profileSlug.message}</p>
          )}
        </div>

        {/* Professional Title */}
        <div>
          <label htmlFor="title" className="block text-sm font-medium text-foreground">
            Professional Title
          </label>
          <input
            {...register('title')}
            type="text"
            id="title"
            className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
            placeholder="e.g., Senior Software Engineer"
            disabled={isLoading || isSubmitting}
          />
          {moderationPending.includes('title') && (
            <div className="mt-1 flex items-center text-sm text-muted-foreground">
              <div className="animate-spin rounded-full h-3 w-3 border-b-2 border-primary mr-2"></div>
              Checking content...
            </div>
          )}
          {(() => {
            const status = getFieldModerationStatus('title')
            if (!status) return null
            return (
              <div className={`mt-1 text-sm ${
                status.type === 'error' ? 'text-destructive' :
                status.type === 'warning' ? 'text-warning' : 'text-primary'
              }`}>
                {status.message}
              </div>
            )
          })()}
          {errors.title && (
            <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
          )}
        </div>

        {/* Professional Summary */}
        <div>
          <label htmlFor="summary" className="block text-sm font-medium text-foreground">
            Professional Summary
          </label>
          <textarea
            {...register('summary')}
            id="summary"
            rows={4}
            className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
            placeholder="Brief description of your professional background and expertise..."
            disabled={isLoading || isSubmitting}
          />
          {moderationPending.includes('summary') && (
            <div className="mt-1 flex items-center text-sm text-muted-foreground">
              <div className="animate-spin rounded-full h-3 w-3 border-b-2 border-primary mr-2"></div>
              Checking content...
            </div>
          )}
          {(() => {
            const status = getFieldModerationStatus('summary')
            if (!status) return null
            return (
              <div className={`mt-1 text-sm ${
                status.type === 'error' ? 'text-destructive' :
                status.type === 'warning' ? 'text-warning' : 'text-primary'
              }`}>
                {status.message}
              </div>
            )
          })()}
          {errors.summary && (
            <p className="mt-1 text-sm text-destructive">{errors.summary.message}</p>
          )}
        </div>

        {/* Expandable Section */}
        <div className="border-t border-border pt-6">
          <button
            type="button"
            onClick={() => setIsExpanded(!isExpanded)}
            className="flex items-center text-sm font-medium text-primary hover:text-primary/80"
          >
            {isExpanded ? 'Hide' : 'Show'} Additional Information
            <svg
              className={`ml-1 h-4 w-4 transform transition-transform ${isExpanded ? 'rotate-180' : ''}`}
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
        </div>

        {isExpanded && (
          <div className="space-y-6">
            <div>
              <label htmlFor="company" className="block text-sm font-medium text-foreground">
                Current Company
              </label>
              <input
                {...register('company')}
                type="text"
                id="company"
                className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
                placeholder="Company name"
                disabled={isLoading || isSubmitting}
              />
              {moderationPending.includes('company') && (
                <div className="mt-1 flex items-center text-sm text-muted-foreground">
                  <div className="animate-spin rounded-full h-3 w-3 border-b-2 border-primary mr-2"></div>
                  Checking content...
                </div>
              )}
              {(() => {
                const status = getFieldModerationStatus('company')
                if (!status) return null
                return (
                  <div className={`mt-1 text-sm ${
                    status.type === 'error' ? 'text-destructive' :
                    status.type === 'warning' ? 'text-warning' : 'text-primary'
                  }`}>
                    {status.message}
                  </div>
                )
              })()}
              {errors.company && (
                <p className="mt-1 text-sm text-destructive">{errors.company.message}</p>
              )}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label htmlFor="location" className="block text-sm font-medium text-foreground">
                  Location
                </label>
                <input
                  {...register('location')}
                  type="text"
                  id="location"
                  className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
                  placeholder="City, Country"
                  disabled={isLoading || isSubmitting}
                />
                {errors.location && (
                  <p className="mt-1 text-sm text-destructive">{errors.location.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="timeZone" className="block text-sm font-medium text-foreground">
                  Time Zone
                </label>
                <input
                  {...register('timeZone')}
                  type="text"
                  id="timeZone"
                  className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
                  placeholder="e.g., UTC-5, GMT+1"
                  disabled={isLoading || isSubmitting}
                />
                {errors.timeZone && (
                  <p className="mt-1 text-sm text-destructive">{errors.timeZone.message}</p>
                )}
              </div>
            </div>

            {/* Social Links */}
            <div className="space-y-4">
              <h4 className="text-sm font-medium text-foreground">Professional Links</h4>

              <div>
                <label htmlFor="websiteUrl" className="block text-sm font-medium text-foreground">
                  Website
                </label>
                <input
                  {...register('websiteUrl')}
                  type="url"
                  id="websiteUrl"
                  className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
                  placeholder="https://yourwebsite.com"
                  disabled={isLoading || isSubmitting}
                />
                {errors.websiteUrl && (
                  <p className="mt-1 text-sm text-destructive">{errors.websiteUrl.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="linkedInUrl" className="block text-sm font-medium text-foreground">
                  LinkedIn
                </label>
                <input
                  {...register('linkedInUrl')}
                  type="url"
                  id="linkedInUrl"
                  className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
                  placeholder="https://linkedin.com/in/yourprofile"
                  disabled={isLoading || isSubmitting}
                />
                {errors.linkedInUrl && (
                  <p className="mt-1 text-sm text-destructive">{errors.linkedInUrl.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="gitHubUrl" className="block text-sm font-medium text-foreground">
                  GitHub
                </label>
                <input
                  {...register('gitHubUrl')}
                  type="url"
                  id="gitHubUrl"
                  className="mt-1 block w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-ring focus:border-ring"
                  placeholder="https://github.com/yourusername"
                  disabled={isLoading || isSubmitting}
                />
                {errors.gitHubUrl && (
                  <p className="mt-1 text-sm text-destructive">{errors.gitHubUrl.message}</p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Privacy Setting */}
        <div className="border-t border-border pt-6">
          <div className="flex items-center">
            <input
              {...register('isPublic')}
              type="checkbox"
              id="isPublic"
              className="h-4 w-4 text-primary focus:ring-ring border-border rounded"
              disabled={isLoading || isSubmitting}
            />
            <label htmlFor="isPublic" className="ml-2 block text-sm text-foreground">
              Make my profile publicly discoverable
            </label>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">
            When enabled, your profile will be visible to other users and searchable.
          </p>
        </div>

        {/* Submit Button */}
        <div className="border-t border-border pt-6">
          <button
            type="submit"
            disabled={
              isLoading ||
              isSubmitting ||
              Object.values(contentModeration).some(r => !r.isApproved && r.severity === 'high') ||
              (slugAvailability ? !slugAvailability.isAvailable : false)
            }
            className="w-full flex justify-center py-2 px-4 border border-transparent rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading || isSubmitting ? 'Saving...' : submitButtonText}
          </button>
        </div>
      </form>
    </div>
  )
}