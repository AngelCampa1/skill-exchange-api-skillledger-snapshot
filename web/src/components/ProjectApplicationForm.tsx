'use client'

import React, { useState, useEffect, useMemo, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { DollarSign, Clock, Calendar, FileText, Send, AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { trackEvent } from '@/utils/analytics'

const applicationSchema = z.object({
  coverLetter: z.string().min(100, 'Cover letter must be at least 100 characters').max(5000, 'Cover letter cannot exceed 5000 characters'),
  proposedRate: z.number().min(15, 'Proposed rate must be at least $15').max(10000, 'Proposed rate cannot exceed $10,000'),
  estimatedDuration: z.string().min(1, 'Estimated duration is required'),
  availabilityStartDate: z.string().min(1, 'Start date is required'),
  keyQualifications: z.string().min(50, 'Key qualifications must be at least 50 characters').max(2000, 'Key qualifications cannot exceed 2000 characters'),
  approach: z.string().min(100, 'Approach must be at least 100 characters').max(3000, 'Approach cannot exceed 3000 characters'),
  deliverables: z.string().min(50, 'Deliverables must be at least 50 characters').max(2000, 'Deliverables cannot exceed 2000 characters'),
  questions: z.string().optional(),
  attachments: z.array(z.string()).optional(),
})

type ApplicationFormData = z.infer<typeof applicationSchema>

interface Project {
  id: string
  title: string
  description: string
  creditBudget: number
  requiredSkills?: Array<{
    name?: string
    proficiency?: number
    skill?: {
      id: string
      name: string
    }
    proficiencyRequired?: number
    proficiencyDisplay?: string
  }>
  deadline?: string
  endDate?: string
}

interface ProjectApplicationFormProps {
  project: Project
  onSubmit: (data: ApplicationFormData) => Promise<void>
  isLoading?: boolean
  onCancel?: () => void
  client?: {
    id: string
    userName: string
    email: string
  }
  teamMembers?: Array<{
    email: string
    role: string
    permissions: string
  }>
}

const DURATION_OPTIONS = [
  'Less than 1 week',
  '1-2 weeks',
  '2-4 weeks',
  '1-2 months',
  '2-3 months',
  '3-6 months',
  '6+ months'
]

export default function ProjectApplicationForm({ 
  project, 
  onSubmit, 
  isLoading = false, 
  onCancel 
}: ProjectApplicationFormProps) {
  const [currentStep, setCurrentStep] = useState(1)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [characterCounts, setCharacterCounts] = useState({
    coverLetter: 0,
    keyQualifications: 0,
    approach: 0,
    deliverables: 0
  })

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isValid },
    setValue,
    trigger,
    getValues,
  } = useForm<ApplicationFormData>({
    resolver: zodResolver(applicationSchema),
    mode: 'onChange',
    defaultValues: {
      proposedRate: project.creditBudget,
    }
  })

  // BUG-FE-004 FIX: Stable character count calculation using getValues instead of watch
  // Remove getValues from dependency array to prevent unnecessary re-renders
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const updateCharacterCounts = useCallback(() => {
    const values = getValues()
    const newCounts = {
      coverLetter: values.coverLetter?.length || 0,
      keyQualifications: values.keyQualifications?.length || 0,
      approach: values.approach?.length || 0,
      deliverables: values.deliverables?.length || 0
    }

    // Only update if counts actually changed
    setCharacterCounts(prev => {
      if (JSON.stringify(prev) !== JSON.stringify(newCounts)) {
        return newCounts
      }
      return prev
    })
    // BUG-FE-004 FIX: Empty deps - getValues is stable from react-hook-form
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // BUG-FE-004 FIX: Set up manual field change listeners
  // useEffect with watch and updateCharacterCounts is safe because:
  // 1. watch is stable (from react-hook-form)
  // 2. updateCharacterCounts is now stable (empty deps)
  useEffect(() => {
    const subscription = watch((value, { name, type }) => {
      if (name === 'coverLetter' || name === 'keyQualifications' ||
          name === 'approach' || name === 'deliverables') {
        updateCharacterCounts()
      }
    })

    return () => subscription.unsubscribe()
  }, [watch, updateCharacterCounts])

  // BUG-FE-004 FIX: Initialize character counts on mount only
  useEffect(() => {
    updateCharacterCounts()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const nextStep = async () => {
    let isStepValid = false
    
    switch (currentStep) {
      case 1:
        isStepValid = await trigger(['coverLetter', 'proposedRate'])
        break
      case 2:
        isStepValid = await trigger(['estimatedDuration', 'availabilityStartDate', 'keyQualifications'])
        break
      case 3:
        isStepValid = await trigger(['approach', 'deliverables'])
        break
      default:
        isStepValid = true
    }
    
    if (isStepValid && currentStep < 3) {
      setCurrentStep(prev => prev + 1)
    }
  }

  const prevStep = () => {
    if (currentStep > 1) {
      setCurrentStep(prev => prev - 1)
    }
  }

  const handleFormSubmit = async (data: ApplicationFormData) => {
    setIsSubmitting(true)
    try {
      await onSubmit(data)

      // Track successful project application
      trackEvent({
        name: 'application_submitted',
        category: 'projects',
        priority: 'critical',
        properties: {
          project_id: project.id,
          proposed_rate: data.proposedRate,
          estimated_duration: data.estimatedDuration,
          cover_letter_length: data.coverLetter.length,
          has_questions: !!data.questions,
          has_attachments: !!(data.attachments && data.attachments.length > 0),
        },
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  const getFieldsForStep = (step: number) => {
    switch (step) {
      case 1:
        return ['coverLetter', 'proposedRate']
      case 2:
        return ['estimatedDuration', 'availabilityStartDate', 'keyQualifications']
      case 3:
        return ['approach', 'deliverables']
      default:
        return []
    }
  }

  const calculateProgress = () => {
    let completedSections = 0
    const values = getValues()

    // Use form values for progress calculation
    if (values.coverLetter && values.proposedRate) completedSections++
    if (values.estimatedDuration && values.availabilityStartDate && values.keyQualifications) completedSections++
    if (values.approach && values.deliverables) completedSections++

    return (completedSections / 3) * 100
  }

  const getDaysUntilDeadline = () => {
    if (!project.deadline) return null
    const days = Math.ceil((new Date(project.deadline).getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24))
    return days
  }

  const deadlineDays = getDaysUntilDeadline()

  return (
    <div className="w-full max-w-4xl mx-auto">
      {/* Project Summary */}
      <div className="bg-primary/10 border border-primary/20 rounded-lg p-6 mb-6">
        <h3 className="text-lg font-semibold text-primary mb-2">{project.title}</h3>
        <p className="text-primary/80 text-sm mb-4 line-clamp-2">{project.description}</p>

        <div className="flex flex-wrap gap-4 text-sm text-primary/80">
          <div className="flex items-center">
            <DollarSign className="h-4 w-4 mr-1" />
            <span>Budget: {project.creditBudget} credits</span>
          </div>
          
          {project.deadline && (
            <div className="flex items-center">
              <Clock className="h-4 w-4 mr-1" />
              <span>Deadline: {new Date(project.deadline).toLocaleDateString()}</span>
              {deadlineDays !== null && (
                <span className="ml-2 font-medium">
                  ({deadlineDays < 0 ? 'Expired' : deadlineDays === 0 ? 'Today' : `${deadlineDays} days`})
                </span>
              )}
            </div>
          )}
        </div>

        {project.requiredSkills && project.requiredSkills.length > 0 && (
          <div className="mt-3">
            <p className="text-sm font-medium text-primary mb-1">Required Skills:</p>
            <div className="flex flex-wrap gap-1">
              {project.requiredSkills.map((skill, index) => (
                <span
                  key={index}
                  className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-primary/10 text-primary"
                >
                  {skill.name || skill.skill?.name}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>

      {deadlineDays !== null && deadlineDays < 7 && deadlineDays >= 0 && (
        <Alert className="mb-6 border-warning/20 bg-warning/10">
          <AlertTriangle className="h-4 w-4 text-warning" />
          <AlertDescription className="text-warning">
            This project has a deadline approaching in {deadlineDays} days. Make sure to submit your application soon!
          </AlertDescription>
        </Alert>
      )}

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
          <span>Introduction</span>
          <span>Details & Timeline</span>
          <span>Approach & Deliverables</span>
        </div>
      </div>

      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-8">
        {/* Step 1: Introduction */}
        {currentStep === 1 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Introduction & Rate</h2>
            
            <div className="space-y-6">
              <div>
                <Label htmlFor="coverLetter" className="block text-sm font-medium text-foreground">
                  Cover Letter *
                </Label>
                <textarea
                  {...register('coverLetter')}
                  id="coverLetter"
                  data-testid="cover-letter-input"
                  rows={6}
                  className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="Introduce yourself and explain why you're the perfect fit for this project. Highlight your relevant experience and what makes you unique."
                />
                <div className="mt-1 text-sm text-muted-foreground">
                  {characterCounts.coverLetter}/5000 characters
                </div>
                {errors.coverLetter && (
                  <p className="mt-1 text-sm text-destructive">{errors.coverLetter.message}</p>
                )}
              </div>

              <div>
                <Label htmlFor="proposedRate" className="block text-sm font-medium text-foreground">
                  Proposed Rate ($) *
                </Label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                    <DollarSign className="h-4 w-4 text-muted-foreground" />
                  </div>
                  <Input
                    {...register('proposedRate', { valueAsNumber: true })}
                    id="proposedRate"
                    data-testid="proposed-rate-input"
                    type="number"
                    min="15"
                    max="10000"
                    step="50"
                    className="pl-8"
                    placeholder={project.creditBudget.toString()}
                  />
                </div>
                <p className="mt-1 text-sm text-muted-foreground">
                  Project budget: {project.creditBudget} credits. You can propose a different rate if justified.
                </p>
                {errors.proposedRate && (
                  <p className="mt-1 text-sm text-destructive">{errors.proposedRate.message}</p>
                )}
              </div>

              <Alert>
                <FileText className="h-4 w-4 text-primary" />
                <AlertDescription>
                  <strong>Pro Tip:</strong> Personalize your cover letter for this specific project.
                  Mention the project title and explain how your skills directly address the client's needs.
                </AlertDescription>
              </Alert>
            </div>
          </div>
        )}

        {/* Step 2: Details & Timeline */}
        {currentStep === 2 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Details & Timeline</h2>
            
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <Label htmlFor="estimatedDuration" className="block text-sm font-medium text-foreground">
                    Estimated Duration *
                  </Label>
                  <select
                    {...register('estimatedDuration')}
                    id="estimatedDuration"
                    data-testid="duration-select"
                    className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-primary focus:ring-ring"
                  >
                    <option value="">Select duration</option>
                    {DURATION_OPTIONS.map(option => (
                      <option key={option} value={option}>{option}</option>
                    ))}
                  </select>
                  {errors.estimatedDuration && (
                    <p className="mt-1 text-sm text-destructive">{errors.estimatedDuration.message}</p>
                  )}
                </div>

                <div>
                  <Label htmlFor="availabilityStartDate" className="block text-sm font-medium text-foreground">
                    Available Start Date *
                  </Label>
                  <div className="mt-1 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                    </div>
                    <Input
                      {...register('availabilityStartDate')}
                      id="availabilityStartDate"
                      data-testid="start-date-input"
                      type="date"
                      min={new Date().toISOString().split('T')[0]}
                      className="pl-8"
                    />
                  </div>
                  {errors.availabilityStartDate && (
                    <p className="mt-1 text-sm text-destructive">{errors.availabilityStartDate.message}</p>
                  )}
                </div>
              </div>

              <div>
                <Label htmlFor="keyQualifications" className="block text-sm font-medium text-foreground">
                  Key Qualifications *
                </Label>
                <textarea
                  {...register('keyQualifications')}
                  id="keyQualifications"
                  data-testid="qualifications-input"
                  rows={4}
                  className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="Highlight your most relevant qualifications for this project. Focus on skills, experience, and achievements that directly relate to the project requirements."
                />
                <div className="mt-1 text-sm text-muted-foreground">
                  {characterCounts.keyQualifications}/2000 characters
                </div>
                {errors.keyQualifications && (
                  <p className="mt-1 text-sm text-destructive">{errors.keyQualifications.message}</p>
                )}
              </div>

              <div>
                <Label htmlFor="questions" className="block text-sm font-medium text-foreground">
                  Questions for Client (Optional)
                </Label>
                <textarea
                  {...register('questions')}
                  id="questions"
                  data-testid="questions-input"
                  rows={3}
                  className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="Ask any questions you have about the project requirements, timeline, or expectations."
                />
                <p className="mt-1 text-sm text-muted-foreground">
                  Use this to clarify any uncertainties and show your attention to detail.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Step 3: Approach & Deliverables */}
        {currentStep === 3 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Approach & Deliverables</h2>

            <div className="space-y-6">
              <div>
                <Label htmlFor="approach" className="block text-sm font-medium text-foreground">
                  Your Approach *
                </Label>
                <textarea
                  {...register('approach')}
                  id="approach"
                  data-testid="approach-input"
                  rows={5}
                  className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="Describe your approach to completing this project. Include your methodology, tools you'll use, and how you plan to tackle the main challenges."
                />
                <div className="mt-1 text-sm text-muted-foreground">
                  {characterCounts.approach}/3000 characters
                </div>
                {errors.approach && (
                  <p className="mt-1 text-sm text-destructive">{errors.approach.message}</p>
                )}
              </div>

              <div>
                <Label htmlFor="deliverables" className="block text-sm font-medium text-foreground">
                  Key Deliverables *
                </Label>
                <textarea
                  {...register('deliverables')}
                  id="deliverables"
                  data-testid="deliverables-input"
                  rows={4}
                  className="mt-1 block w-full rounded-md border-input shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="List the specific deliverables you'll provide. Be concrete and measurable (e.g., 'Responsive React dashboard with 5 main screens', 'API documentation with Postman collection')."
                />
                <div className="mt-1 text-sm text-muted-foreground">
                  {characterCounts.deliverables}/2000 characters
                </div>
                {errors.deliverables && (
                  <p className="mt-1 text-sm text-destructive">{errors.deliverables.message}</p>
                )}
              </div>

              <Alert>
                <Send className="h-4 w-4 text-primary" />
                <AlertDescription>
                  <strong>Before submitting:</strong> Review your application carefully.
                  Clients look for detailed, specific proposals that show you understand their needs.
                </AlertDescription>
              </Alert>
            </div>
          </div>
        )}

        {/* Navigation Buttons */}
        <div className="flex justify-between items-center pt-6">
          <div className="flex space-x-3">
            {currentStep > 1 && (
              <Button
                type="button"
                variant="outline"
                onClick={prevStep}
                disabled={isSubmitting}
                data-testid="application-prev-button"
              >
                Previous
              </Button>
            )}
            
            {onCancel && (
              <Button
                type="button"
                variant="ghost"
                onClick={onCancel}
                disabled={isSubmitting}
                data-testid="cancel-application-button"
              >
                Cancel
              </Button>
            )}
          </div>

          <div className="flex space-x-3">
            {currentStep < 3 ? (
              <Button
                type="button"
                onClick={nextStep}
                disabled={isSubmitting}
                data-testid="application-next-button"
              >
                Next
              </Button>
            ) : (
              <Button
                type="submit"
                disabled={isSubmitting || isLoading || !isValid}
                loading={isSubmitting || isLoading}
                loadingText="Submitting Application..."
                data-testid="submit-application-button"
              >
                Submit Application
              </Button>
            )}
          </div>
        </div>
      </form>
    </div>
  )
}
