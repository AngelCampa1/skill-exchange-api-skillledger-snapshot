'use client'

import React, { useState } from 'react'
import { useForm, useFieldArray } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, X, DollarSign, Briefcase, Award, MapPin } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'

const providerProfileSchema = z.object({
  companyName: z.string().optional(),
  title: z.string().min(1, 'Professional title is required').max(100, 'Title too long'),
  bio: z.string().min(50, 'Bio must be at least 50 characters').max(2000, 'Bio too long'),
  hourlyRate: z.number().min(15, 'Hourly rate must be at least $15').max(500, 'Hourly rate cannot exceed $500'),
  location: z.string().min(1, 'Location is required').max(100, 'Location too long'),
  website: z.string().url().optional().or(z.literal('')),
  linkedin: z.string().url().optional().or(z.literal('')),
  portfolio: z.string().url().optional().or(z.literal('')),
  skills: z.array(z.object({
    name: z.string().min(1, 'Skill name is required'),
    proficiency: z.number().min(1).max(5),
    yearsExperience: z.number().min(0).max(50)
  })).min(1, 'At least one skill is required').max(10, 'Cannot exceed 10 skills'),
  experience: z.array(z.object({
    company: z.string().min(1, 'Company name is required'),
    position: z.string().min(1, 'Position is required'),
    startDate: z.string(),
    endDate: z.string().optional(),
    description: z.string().min(10, 'Description must be at least 10 characters')
  })).max(5, 'Cannot exceed 5 experiences'),
  availability: z.enum(['full-time', 'part-time', 'contract', 'freelance']),
  preferredWorkHours: z.enum(['morning', 'afternoon', 'evening', 'flexible']),
})

type ProviderProfileFormData = z.infer<typeof providerProfileSchema>

interface ProviderProfileFormProps {
  onSubmit: (data: ProviderProfileFormData) => Promise<void>
  initialData?: Partial<ProviderProfileFormData>
  isLoading?: boolean
}

const PROFICIENCY_LEVELS = [
  { value: 1, label: 'Beginner' },
  { value: 2, label: 'Novice' },
  { value: 3, label: 'Intermediate' },
  { value: 4, label: 'Advanced' },
  { value: 5, label: 'Expert' },
]

const AVAILABILITY_OPTIONS = [
  { value: 'full-time', label: 'Full Time (40+ hrs/week)' },
  { value: 'part-time', label: 'Part Time (20-39 hrs/week)' },
  { value: 'contract', label: 'Contract (Project-based)' },
  { value: 'freelance', label: 'Freelance (Flexible)' },
]

const WORK_HOURS_OPTIONS = [
  { value: 'morning', label: 'Morning (6AM - 12PM)' },
  { value: 'afternoon', label: 'Afternoon (12PM - 6PM)' },
  { value: 'evening', label: 'Evening (6PM - 12AM)' },
  { value: 'flexible', label: 'Flexible' },
]

export default function ProviderProfileForm({ onSubmit, initialData, isLoading = false }: ProviderProfileFormProps) {
  const [currentStep, setCurrentStep] = useState(1)
  
  const {
    register,
    handleSubmit,
    control,
    watch,
    formState: { errors, isValid },
  } = useForm<ProviderProfileFormData>({
    resolver: zodResolver(providerProfileSchema),
    mode: 'onChange',
    defaultValues: {
      skills: [{ name: '', proficiency: 3, yearsExperience: 1 }],
      experience: [],
      availability: 'freelance',
      preferredWorkHours: 'flexible',
      ...initialData
    }
  })

  const {
    fields: skillFields,
    append: appendSkill,
    remove: removeSkill,
  } = useFieldArray({
    control,
    name: 'skills',
  })

  const {
    fields: experienceFields,
    append: appendExperience,
    remove: removeExperience,
  } = useFieldArray({
    control,
    name: 'experience',
  })

  const watchedFields = watch()

  const nextStep = () => {
    if (currentStep < 3) {
      setCurrentStep(prev => prev + 1)
    }
  }

  const prevStep = () => {
    if (currentStep > 1) {
      setCurrentStep(prev => prev - 1)
    }
  }

  const handleFormSubmit = async (data: ProviderProfileFormData) => {
    await onSubmit(data)
  }

  const addSkill = () => {
    if (skillFields.length < 10) {
      appendSkill({ name: '', proficiency: 3, yearsExperience: 1 })
    }
  }

  const addExperience = () => {
    if (experienceFields.length < 5) {
      appendExperience({ company: '', position: '', startDate: '', description: '' })
    }
  }

  const calculateProgress = () => {
    let completedSections = 0
    
    if (watchedFields.title && watchedFields.bio && watchedFields.hourlyRate) completedSections++
    if (watchedFields.skills?.some(s => s.name)) completedSections++
    if (watchedFields.experience?.length > 0 || watchedFields.companyName) completedSections++
    if (watchedFields.availability && watchedFields.preferredWorkHours) completedSections++
    
    return (completedSections / 4) * 100
  }

  return (
    <div className="w-full max-w-4xl mx-auto">
      {/* Progress Bar */}
      <div className="mb-8">
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-foreground">
            Step {currentStep} of 3
          </span>
          <span className="text-sm text-muted-foreground">{Math.round(calculateProgress())}% Complete</span>
        </div>
        <div className="w-full bg-muted rounded-full h-2">
          <div
            className="bg-primary h-2 rounded-full transition-all duration-300"
            style={{ width: `${calculateProgress()}%` }}
          />
        </div>
        <div className="flex justify-between text-xs text-muted-foreground mt-1">
          <span>Basic Info</span>
          <span>Skills & Experience</span>
          <span>Availability</span>
        </div>
      </div>

      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-8">
        {/* Step 1: Basic Information */}
        {currentStep === 1 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Basic Information</h2>
            
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label htmlFor="title" className="block text-sm font-medium text-foreground">
                    Professional Title *
                  </Label>
                  <Input
                    {...register('title')}
                    id="title"
                    data-testid="provider-title-input"
                    placeholder="e.g. Senior Full-Stack Developer"
                    className="mt-1"
                  />
                  {errors.title && (
                    <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
                  )}
                </div>

                <div>
                  <Label htmlFor="companyName" className="block text-sm font-medium text-foreground">
                    Company (Optional)
                  </Label>
                  <Input
                    {...register('companyName')}
                    id="companyName"
                    data-testid="provider-company-input"
                    placeholder="e.g. Tech Solutions Inc."
                    className="mt-1"
                  />
                </div>
              </div>

              <div>
                <Label htmlFor="bio" className="block text-sm font-medium text-foreground">
                  Professional Bio *
                </Label>
                <textarea
                  {...register('bio')}
                  id="bio"
                  data-testid="provider-bio-input"
                  rows={4}
                  className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-ring focus:ring-ring"
                  placeholder="Tell us about your experience, expertise, and what makes you unique..."
                />
                <div className="mt-1 text-sm text-muted-foreground">
                  {watchedFields.bio?.length || 0}/2000 characters
                </div>
                {errors.bio && (
                  <p className="mt-1 text-sm text-destructive">{errors.bio.message}</p>
                )}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label htmlFor="hourlyRate" className="block text-sm font-medium text-foreground">
                    Hourly Rate ($) *
                  </Label>
                  <div className="mt-1 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <DollarSign className="h-4 w-4 text-muted-foreground" />
                    </div>
                    <Input
                      {...register('hourlyRate', { valueAsNumber: true })}
                      id="hourlyRate"
                      data-testid="provider-hourly-rate-input"
                      type="number"
                      min="15"
                      max="500"
                      className="mt-1 pl-8"
                      placeholder="75"
                    />
                  </div>
                  {errors.hourlyRate && (
                    <p className="mt-1 text-sm text-destructive">{errors.hourlyRate.message}</p>
                  )}
                </div>

                <div>
                  <Label htmlFor="location" className="block text-sm font-medium text-foreground">
                    Location *
                  </Label>
                  <div className="mt-1 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <MapPin className="h-4 w-4 text-muted-foreground" />
                    </div>
                    <Input
                      {...register('location')}
                      id="location"
                      data-testid="provider-location-input"
                      placeholder="e.g. New York, NY"
                      className="pl-8"
                    />
                  </div>
                  {errors.location && (
                    <p className="mt-1 text-sm text-destructive">{errors.location.message}</p>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div>
                  <Label htmlFor="website" className="block text-sm font-medium text-foreground">
                    Website
                  </Label>
                  <Input
                    {...register('website')}
                    id="website"
                    data-testid="provider-website-input"
                    type="url"
                    placeholder="https://yourwebsite.com"
                    className="mt-1"
                  />
                  {errors.website && (
                    <p className="mt-1 text-sm text-destructive">{errors.website.message}</p>
                  )}
                </div>

                <div>
                  <Label htmlFor="linkedin" className="block text-sm font-medium text-foreground">
                    LinkedIn
                  </Label>
                  <Input
                    {...register('linkedin')}
                    id="linkedin"
                    data-testid="provider-linkedin-input"
                    type="url"
                    placeholder="https://linkedin.com/in/yourprofile"
                    className="mt-1"
                  />
                  {errors.linkedin && (
                    <p className="mt-1 text-sm text-destructive">{errors.linkedin.message}</p>
                  )}
                </div>

                <div>
                  <Label htmlFor="portfolio" className="block text-sm font-medium text-foreground">
                    Portfolio
                  </Label>
                  <Input
                    {...register('portfolio')}
                    id="portfolio"
                    data-testid="provider-portfolio-input"
                    type="url"
                    placeholder="https://yourportfolio.com"
                    className="mt-1"
                  />
                  {errors.portfolio && (
                    <p className="mt-1 text-sm text-destructive">{errors.portfolio.message}</p>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Step 2: Skills & Experience */}
        {currentStep === 2 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Skills & Experience</h2>
            
            <div className="space-y-8">
              {/* Skills Section */}
              <div>
                <div className="flex justify-between items-center mb-4">
                  <h3 className="text-lg font-medium text-foreground">Skills</h3>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={addSkill}
                    disabled={skillFields.length >= 10}
                    data-testid="add-skill-button"
                  >
                    <Plus className="h-4 w-4 mr-2" />
                    Add Skill
                  </Button>
                </div>

                <div className="space-y-4">
                  {skillFields.map((field, index) => (
                    <div key={field.id} className="border border-border rounded-lg p-4">
                      <div className="flex justify-between items-start mb-4">
                        <h4 className="font-medium text-foreground">Skill #{index + 1}</h4>
                        {skillFields.length > 1 && (
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => removeSkill(index)}
                            className="text-destructive hover:text-destructive"
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        )}
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            Skill Name *
                          </Label>
                          <Input
                            {...register(`skills.${index}.name`)}
                            data-testid={`skill-name-${index}`}
                            placeholder="e.g. React, Node.js, Python"
                            className="mt-1"
                          />
                          {errors.skills?.[index]?.name && (
                            <p className="mt-1 text-sm text-destructive">
                              {errors.skills[index]?.name?.message}
                            </p>
                          )}
                        </div>

                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            Proficiency *
                          </Label>
                          <select
                            {...register(`skills.${index}.proficiency`, { valueAsNumber: true })}
                            data-testid={`skill-proficiency-${index}`}
                            className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-ring focus:ring-ring"
                          >
                            {PROFICIENCY_LEVELS.map((level) => (
                              <option key={level.value} value={level.value}>
                                {level.label}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            Years of Experience *
                          </Label>
                          <Input
                            {...register(`skills.${index}.yearsExperience`, { valueAsNumber: true })}
                            data-testid={`skill-years-${index}`}
                            type="number"
                            min="0"
                            max="50"
                            placeholder="3"
                            className="mt-1"
                          />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>

                {errors.skills && (
                  <p className="mt-2 text-sm text-destructive">{errors.skills.message}</p>
                )}
              </div>

              {/* Experience Section */}
              <div>
                <div className="flex justify-between items-center mb-4">
                  <h3 className="text-lg font-medium text-foreground">Work Experience</h3>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={addExperience}
                    disabled={experienceFields.length >= 5}
                    data-testid="add-experience-button"
                  >
                    <Plus className="h-4 w-4 mr-2" />
                    Add Experience
                  </Button>
                </div>

                <div className="space-y-4">
                  {experienceFields.map((field, index) => (
                    <div key={field.id} className="border border-border rounded-lg p-4">
                      <div className="flex justify-between items-start mb-4">
                        <h4 className="font-medium text-foreground">Experience #{index + 1}</h4>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => removeExperience(index)}
                          className="text-destructive hover:text-destructive"
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            Company *
                          </Label>
                          <Input
                            {...register(`experience.${index}.company`)}
                            data-testid={`experience-company-${index}`}
                            placeholder="e.g. Tech Corp"
                            className="mt-1"
                          />
                        </div>

                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            Position *
                          </Label>
                          <Input
                            {...register(`experience.${index}.position`)}
                            data-testid={`experience-position-${index}`}
                            placeholder="e.g. Senior Developer"
                            className="mt-1"
                          />
                        </div>
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            Start Date *
                          </Label>
                          <Input
                            {...register(`experience.${index}.startDate`)}
                            data-testid={`experience-start-${index}`}
                            type="month"
                            className="mt-1"
                          />
                        </div>

                        <div>
                          <Label className="block text-sm font-medium text-foreground">
                            End Date (Current if empty)
                          </Label>
                          <Input
                            {...register(`experience.${index}.endDate`)}
                            data-testid={`experience-end-${index}`}
                            type="month"
                            className="mt-1"
                          />
                        </div>
                      </div>

                      <div>
                        <Label className="block text-sm font-medium text-foreground">
                          Description *
                        </Label>
                        <textarea
                          {...register(`experience.${index}.description`)}
                          data-testid={`experience-description-${index}`}
                          rows={3}
                          className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-ring focus:ring-ring"
                          placeholder="Describe your responsibilities and achievements..."
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Step 3: Availability */}
        {currentStep === 3 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Availability & Preferences</h2>

            <div className="space-y-6">
              <div>
                <Label className="block text-sm font-medium text-foreground mb-3">
                  Work Availability *
                </Label>
                <div className="space-y-3">
                  {AVAILABILITY_OPTIONS.map((option) => (
                    <label key={option.value} className="flex items-center">
                      <input
                        {...register('availability')}
                        type="radio"
                        value={option.value}
                        data-testid={`availability-${option.value}`}
                        className="mr-3"
                      />
                      <span className="text-foreground">{option.label}</span>
                    </label>
                  ))}
                </div>
                {errors.availability && (
                  <p className="mt-1 text-sm text-destructive">{errors.availability.message}</p>
                )}
              </div>

              <div>
                <Label className="block text-sm font-medium text-foreground mb-3">
                  Preferred Working Hours *
                </Label>
                <div className="space-y-3">
                  {WORK_HOURS_OPTIONS.map((option) => (
                    <label key={option.value} className="flex items-center">
                      <input
                        {...register('preferredWorkHours')}
                        type="radio"
                        value={option.value}
                        data-testid={`work-hours-${option.value}`}
                        className="mr-3"
                      />
                      <span className="text-foreground">{option.label}</span>
                    </label>
                  ))}
                </div>
                {errors.preferredWorkHours && (
                  <p className="mt-1 text-sm text-destructive">{errors.preferredWorkHours.message}</p>
                )}
              </div>

              <Alert>
                <Award className="h-4 w-4 text-primary" />
                <AlertDescription>
                  Complete your profile to increase your chances of being selected for projects.
                  Clients can see your skills, experience, and availability when reviewing applications.
                </AlertDescription>
              </Alert>
            </div>
          </div>
        )}

        {/* Navigation Buttons */}
        <div className="flex justify-between items-center pt-6">
          <div>
            {currentStep > 1 && (
              <Button
                type="button"
                variant="outline"
                onClick={prevStep}
                data-testid="profile-prev-button"
              >
                Previous
              </Button>
            )}
          </div>

          <div className="flex space-x-3">
            {currentStep < 3 ? (
              <Button
                type="button"
                onClick={nextStep}
                data-testid="profile-next-button"
              >
                Next
              </Button>
            ) : (
              <Button
                type="submit"
                disabled={isLoading || !isValid}
                loading={isLoading}
                loadingText="Creating Profile..."
                data-testid="create-profile-button"
              >
                Create Profile
              </Button>
            )}
          </div>
        </div>
      </form>
    </div>
  )
}
