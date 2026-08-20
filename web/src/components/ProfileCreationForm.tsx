'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import SkillSelector, { SelectedSkill } from './SkillSelector'
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
})

type ProfileCreationFormData = z.infer<typeof profileCreationSchema>

export interface ProfileCreationFormDataWithSkills extends ProfileCreationFormData {
  skills?: SelectedSkill[]
  avatarUrl?: string
}

interface ProfileCreationFormProps {
  onSubmit: (data: ProfileCreationFormDataWithSkills) => Promise<void>
  isLoading?: boolean
  initialData?: Partial<ProfileCreationFormDataWithSkills>
  submitButtonText?: string
  showSkillSelection?: boolean
}

export default function ProfileCreationForm({
  onSubmit,
  isLoading = false,
  initialData,
  submitButtonText = "Create Profile",
  showSkillSelection = true
}: ProfileCreationFormProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  const [selectedSkills, setSelectedSkills] = useState<SelectedSkill[]>(initialData?.skills || [])
  const [avatarUrl, setAvatarUrl] = useState<string | undefined>(initialData?.avatarUrl)
  const [skillsError, setSkillsError] = useState<string | null>(null)

  // BUG-005 FIX: Minimum skills requirement
  const MIN_SKILLS_REQUIRED = 3

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileCreationFormData>({
    resolver: zodResolver(profileCreationSchema),
    mode: 'onChange',
    reValidateMode: 'onChange',
    defaultValues: initialData || {
      isPublic: false,
    },
  })

  const firstName = watch('firstName')
  const lastName = watch('lastName')
  const title = watch('title')

  // Check if profile will be complete
  const isProfileComplete = firstName && lastName && title

  const onFormSubmit = async (data: ProfileCreationFormData) => {
    // BUG-005 FIX: Validate minimum skills requirement before submission
    if (showSkillSelection && selectedSkills.length < MIN_SKILLS_REQUIRED) {
      setSkillsError(`Please select at least ${MIN_SKILLS_REQUIRED} skills to create your profile`)
      // Scroll to skills section
      document.getElementById('skills-section')?.scrollIntoView({ behavior: 'smooth' })
      return
    }
    setSkillsError(null)

    // Include skills and avatar in the submission
    const dataWithSkills: ProfileCreationFormDataWithSkills = {
      ...data,
      skills: showSkillSelection ? selectedSkills : undefined,
      avatarUrl: avatarUrl
    }
    await onSubmit(dataWithSkills)
  }

  const handlePhotoUpload = (result: { success: boolean; fileUrl?: string; error?: string }) => {
    if (result.success && result.fileUrl) {
      setAvatarUrl(result.fileUrl)
    } else if (!result.success) {
      logger.error('Photo upload failed:', result.error)
    }
  }

  return (
    <div className="max-w-2xl mx-auto p-6 bg-card rounded-lg shadow-md">
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-foreground">Create Your Profile</h2>
        <p className="text-muted-foreground mt-2">
          Tell us about yourself to get started on SkillLedger
        </p>
      </div>

      <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-6">
        {/* Profile Photo Upload Section */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-foreground">Profile Photo</h3>
          <SimpleProfessionalPhotoUpload
            onUploadComplete={handlePhotoUpload}
            currentPhotoUrl={avatarUrl}
            isLoading={isLoading}
          />
        </div>

        {/* Basic Information Section */}
        <div className="space-y-4 border-t pt-6">
          <h3 className="text-lg font-medium text-foreground">Basic Information</h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="firstName" className="block text-sm font-medium text-foreground mb-1">
                First Name
              </label>
              <input
                {...register('firstName')}
                type="text"
                id="firstName"
                className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                placeholder="Enter your first name"
              />
              {errors.firstName && (
                <p className="mt-1 text-sm text-destructive">{errors.firstName.message}</p>
              )}
            </div>

            <div>
              <label htmlFor="lastName" className="block text-sm font-medium text-foreground mb-1">
                Last Name
              </label>
              <input
                {...register('lastName')}
                type="text"
                id="lastName"
                className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                placeholder="Enter your last name"
              />
              {errors.lastName && (
                <p className="mt-1 text-sm text-destructive">{errors.lastName.message}</p>
              )}
            </div>
          </div>

          <div>
            <label htmlFor="title" className="block text-sm font-medium text-foreground mb-1">
              Professional Title
            </label>
            <input
              {...register('title')}
              type="text"
              id="title"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="e.g., Senior Software Engineer, Marketing Manager"
            />
            {errors.title && (
              <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="company" className="block text-sm font-medium text-foreground mb-1">
              Company
            </label>
            <input
              {...register('company')}
              type="text"
              id="company"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="Enter your company name"
            />
            {errors.company && (
              <p className="mt-1 text-sm text-destructive">{errors.company.message}</p>
            )}
          </div>
        </div>

        {/* Optional Details Section */}
        <div className="border-t pt-6">
          <button
            type="button"
            onClick={() => setIsExpanded(!isExpanded)}
            className="flex items-center justify-between w-full text-left text-lg font-medium text-foreground hover:text-muted-foreground"
          >
            <span>Additional Details (Optional)</span>
            <span className={`transform transition-transform ${isExpanded ? 'rotate-180' : ''}`}>
              ↓
            </span>
          </button>

          {isExpanded && (
            <div className="mt-4 space-y-4">
              <div>
                <label htmlFor="summary" className="block text-sm font-medium text-foreground mb-1">
                  Professional Summary
                </label>
                <textarea
                  {...register('summary')}
                  id="summary"
                  rows={4}
                  className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                  placeholder="Brief description of your professional background and expertise"
                />
                {errors.summary && (
                  <p className="mt-1 text-sm text-destructive">{errors.summary.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="location" className="block text-sm font-medium text-foreground mb-1">
                  Location
                </label>
                <input
                  {...register('location')}
                  type="text"
                  id="location"
                  className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                  placeholder="e.g., San Francisco, CA, USA"
                />
                {errors.location && (
                  <p className="mt-1 text-sm text-destructive">{errors.location.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="timeZone" className="block text-sm font-medium text-foreground mb-1">
                  Time Zone
                </label>
                <select
                  {...register('timeZone')}
                  id="timeZone"
                  className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                >
                  <option value="">Select your time zone</option>
                  <option value="America/New_York">Eastern Time (ET)</option>
                  <option value="America/Chicago">Central Time (CT)</option>
                  <option value="America/Denver">Mountain Time (MT)</option>
                  <option value="America/Los_Angeles">Pacific Time (PT)</option>
                  <option value="Europe/London">London (GMT)</option>
                  <option value="Europe/Paris">Central European Time (CET)</option>
                  <option value="Asia/Tokyo">Japan Time (JST)</option>
                  <option value="Asia/Shanghai">China Time (CST)</option>
                  <option value="Australia/Sydney">Australian Eastern Time (AET)</option>
                </select>
                {errors.timeZone && (
                  <p className="mt-1 text-sm text-destructive">{errors.timeZone.message}</p>
                )}
              </div>

              <div className="space-y-4">
                <h4 className="text-md font-medium text-foreground">Social Links</h4>

                <div>
                  <label htmlFor="websiteUrl" className="block text-sm font-medium text-foreground mb-1">
                    Website
                  </label>
                  <input
                    {...register('websiteUrl')}
                    type="url"
                    id="websiteUrl"
                    className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                    placeholder="https://yourwebsite.com"
                  />
                  {errors.websiteUrl && (
                    <p className="mt-1 text-sm text-destructive">{errors.websiteUrl.message}</p>
                  )}
                </div>

                <div>
                  <label htmlFor="linkedInUrl" className="block text-sm font-medium text-foreground mb-1">
                    LinkedIn Profile
                  </label>
                  <input
                    {...register('linkedInUrl')}
                    type="url"
                    id="linkedInUrl"
                    className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                    placeholder="https://linkedin.com/in/yourname"
                  />
                  {errors.linkedInUrl && (
                    <p className="mt-1 text-sm text-destructive">{errors.linkedInUrl.message}</p>
                  )}
                </div>

                <div>
                  <label htmlFor="gitHubUrl" className="block text-sm font-medium text-foreground mb-1">
                    GitHub Profile
                  </label>
                  <input
                    {...register('gitHubUrl')}
                    type="url"
                    id="gitHubUrl"
                    className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
                    placeholder="https://github.com/yourusername"
                  />
                  {errors.gitHubUrl && (
                    <p className="mt-1 text-sm text-destructive">{errors.gitHubUrl.message}</p>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Skills Section */}
        {showSkillSelection && (
          <div id="skills-section" className="border-t pt-6">
            <SkillSelector
              selectedSkills={selectedSkills}
              onSkillsChange={(skills) => {
                setSelectedSkills(skills)
                // Clear error when user adds skills
                if (skills.length >= MIN_SKILLS_REQUIRED) {
                  setSkillsError(null)
                }
              }}
              minSkills={MIN_SKILLS_REQUIRED}
            />
            {/* BUG-005 FIX: Display validation error for minimum skills */}
            {skillsError && (
              <div className="mt-3 bg-destructive/10 border border-destructive/20 rounded-md p-3">
                <div className="flex">
                  <div className="flex-shrink-0">
                    <span className="text-destructive">✕</span>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm text-destructive">{skillsError}</p>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Privacy Settings */}
        <div className="border-t pt-6">
          <h3 className="text-lg font-medium text-foreground mb-4">Privacy Settings</h3>

          <div className="flex items-center">
            <input
              {...register('isPublic')}
              type="checkbox"
              id="isPublic"
              className="h-4 w-4 text-primary focus:ring-ring border-border rounded"
            />
            <label htmlFor="isPublic" className="ml-2 block text-sm text-foreground">
              Make my profile visible to other users
            </label>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">
            When enabled, other users can find and view your profile for potential collaboration opportunities.
          </p>
        </div>

        {/* Profile Completeness Indicator */}
        {isProfileComplete && (
          <div className="bg-success/10 border border-success/20 rounded-md p-3">
            <div className="flex">
              <div className="flex-shrink-0">
                <span className="text-success">✓</span>
              </div>
              <div className="ml-3">
                <p className="text-sm text-success">
                  Great! Your profile will be marked as complete with the current information.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Submit Button */}
        <div className="pt-6">
          <button
            type="submit"
            disabled={isSubmitting || isLoading || (showSkillSelection && selectedSkills.length < MIN_SKILLS_REQUIRED)}
            className="w-full flex justify-center py-2 px-4 border border-transparent rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting || isLoading ? (
              <>
                <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-primary-foreground" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Creating Profile...
              </>
            ) : (
              submitButtonText
            )}
          </button>
        </div>

        {/* Skip Option */}
        <div className="text-center">
          <button
            type="button"
            className="text-sm text-muted-foreground hover:text-foreground"
            onClick={() => window.history.back()}
          >
            Skip for now
          </button>
        </div>
      </form>
    </div>
  )
}