'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useRef, useCallback } from 'react'
import { useForm, useFieldArray } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'

// Schema definitions matching backend DTOs
const deliverableSchema = z.object({
  description: z.string().min(1, 'Deliverable description is required').max(500, 'Description cannot exceed 500 characters'),
  orderIndex: z.number().min(0).max(100),
  isRequired: z.boolean().default(true),
})

const skillRequirementSchema = z.object({
  skillId: z.string().min(1, 'Skill selection is required'),
  proficiencyRequired: z.number().min(1, 'Proficiency level is required').max(5, 'Proficiency level cannot exceed 5'),
  weight: z.number().min(1).max(5).default(3),
})

const projectSchema = z.object({
  title: z.string().min(1, 'Project title is required').max(100, 'Title cannot exceed 100 characters'),
  description: z.string().min(1, 'Project description is required').max(5000, 'Description cannot exceed 5000 characters'),
  // BUG-002 FIX: Updated max to match enterprise limits (50000)
  creditBudget: z.number().min(50, 'Credit budget must be at least 50').max(50000, 'Credit budget cannot exceed 50,000'),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  deliverables: z.array(deliverableSchema).min(1, 'At least one deliverable is required').max(10, 'Cannot exceed 10 deliverables'),
  requiredSkills: z.array(skillRequirementSchema).min(1, 'At least one skill is required').max(5, 'Cannot exceed 5 skills'),
}).refine((data) => {
  if (data.startDate && data.endDate) {
    return new Date(data.endDate) > new Date(data.startDate)
  }
  return true
}, {
  message: 'End date must be after start date',
  path: ['endDate']
}).refine((data) => {
  if (data.endDate) {
    return new Date(data.endDate) > new Date()
  }
  return true
}, {
  message: 'End date must be in the future',
  path: ['endDate']
})

const draftSchema = z.object({
  title: z.string().max(100, 'Title cannot exceed 100 characters').optional(),
  description: z.string().max(5000, 'Description cannot exceed 5000 characters').optional(),
  // BUG-002 FIX: Updated max to match enterprise limits (50000)
  creditBudget: z.number().min(50).max(50000).optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  deliverables: z.array(deliverableSchema).max(10).optional(),
  requiredSkills: z.array(skillRequirementSchema).max(5).optional(),
})

type ProjectFormData = z.infer<typeof projectSchema>
type DraftFormData = z.infer<typeof draftSchema>

interface Skill {
  id: string
  name: string
  description: string
  category: string
}

interface ProjectCreationFormProps {
  availableSkills: Skill[]
  onSubmit: (data: ProjectFormData) => Promise<void>
  onSaveDraft: (data: DraftFormData) => Promise<void>
  initialData?: Partial<ProjectFormData>
  isLoading?: boolean
  isDraftMode?: boolean
}

const PROFICIENCY_LEVELS = [
  { value: 1, label: 'Beginner' },
  { value: 2, label: 'Novice' },
  { value: 3, label: 'Intermediate' },
  { value: 4, label: 'Advanced' },
  { value: 5, label: 'Expert' },
]

const WEIGHT_LEVELS = [
  { value: 1, label: 'Low Priority' },
  { value: 2, label: 'Nice to Have' },
  { value: 3, label: 'Important' },
  { value: 4, label: 'High Priority' },
  { value: 5, label: 'Critical' },
]

const ProjectCreationForm = React.memo<ProjectCreationFormProps>(function ProjectCreationForm({
  availableSkills,
  onSubmit,
  onSaveDraft,
  initialData,
  isLoading = false,
  isDraftMode = false
}) {
  const [currentStep, setCurrentStep] = useState(1)
  const [lastSavedDraft, setLastSavedDraft] = useState<Date | null>(null)
  const [isAutoSaving, setIsAutoSaving] = useState(false)

  const schema = isDraftMode ? draftSchema : projectSchema
  
  const {
    register,
    handleSubmit,
    watch,
    control,
    trigger,
    getValues,
    formState: { errors, isSubmitting, isValid },
  } = useForm<ProjectFormData>({
    resolver: zodResolver(schema),
    mode: 'onChange',
    reValidateMode: 'onChange',
    defaultValues: {
      deliverables: [{ description: '', orderIndex: 1, isRequired: true }],
      requiredSkills: [{ skillId: '', proficiencyRequired: 3, weight: 3 }],
      ...initialData
    }
  })

  const {
    fields: deliverableFields,
    append: appendDeliverable,
    remove: removeDeliverable,
  } = useFieldArray({
    control,
    name: 'deliverables',
  })

  const {
    fields: skillFields,
    append: appendSkill,
    remove: removeSkill,
  } = useFieldArray({
    control,
    name: 'requiredSkills',
  })

  const watchedFields = watch()

  // Helper function to check if form has meaningful data
  const hasFormData = (data: DraftFormData): boolean => {
    return !!(data.title || data.description || data.creditBudget ||
           (data.deliverables && data.deliverables.some((d) => d.description)) ||
           (data.requiredSkills && data.requiredSkills.some((s) => s.skillId)))
  }

  // BUG-UX-013 FIX: Debounced auto-save to prevent excessive API calls
  // Use ref to store the debounce timer
  const autoSaveTimerRef = useRef<NodeJS.Timeout | null>(null)
  const lastSavedDataRef = useRef<string>('')

  // Memoized auto-save function
  const performAutoSave = useCallback(async () => {
    const currentData = getValues()
    if (!hasFormData(currentData)) return

    // Check if data has actually changed since last save
    const currentDataString = JSON.stringify(currentData)
    if (currentDataString === lastSavedDataRef.current) return

    setIsAutoSaving(true)
    try {
      await onSaveDraft(currentData)
      setLastSavedDraft(new Date())
      lastSavedDataRef.current = currentDataString
      logger.debug('Auto-save completed', { component: 'ProjectCreationForm' })
    } catch (error) {
      logger.error('Auto-save failed', error, { component: 'ProjectCreationForm' })
    } finally {
      setIsAutoSaving(false)
    }
  }, [getValues, onSaveDraft])

  // Auto-save draft functionality with debounce
  useEffect(() => {
    if (!isDraftMode) return

    // Clear existing timer
    if (autoSaveTimerRef.current) {
      clearTimeout(autoSaveTimerRef.current)
    }

    // BUG-UX-013 FIX: Debounce auto-save by 2 seconds after last change
    autoSaveTimerRef.current = setTimeout(() => {
      performAutoSave()
    }, 2000)

    return () => {
      if (autoSaveTimerRef.current) {
        clearTimeout(autoSaveTimerRef.current)
      }
    }
  }, [isDraftMode, watchedFields, performAutoSave])

  // Backup auto-save every 30 seconds (in case debounced save missed something)
  useEffect(() => {
    if (!isDraftMode) return

    const backupInterval = setInterval(() => {
      performAutoSave()
    }, 30000)

    return () => clearInterval(backupInterval)
  }, [isDraftMode, performAutoSave])

  const handleFormSubmit = async (data: ProjectFormData) => {
    await onSubmit(data)
  }

  const handleSaveDraftClick = async () => {
    const currentData = getValues()
    setIsAutoSaving(true)
    try {
      await onSaveDraft(currentData)
      // Use state update without setTimeout to avoid act warnings
      setLastSavedDraft(new Date())
      setIsAutoSaving(false)
    } catch (error) {
      logger.error('Manual save failed', error, { component: 'ProjectCreationForm' })
      setIsAutoSaving(false)
    }
  }

  const nextStep = async () => {
    const fieldsToValidate = getFieldsForStep(currentStep)
    const isStepValid = await trigger(fieldsToValidate)
    
    if (isStepValid && currentStep < 4) {
      setCurrentStep(prev => prev + 1)
    }
  }

  const prevStep = () => {
    if (currentStep > 1) {
      setCurrentStep(prev => prev - 1)
    }
  }

  const getFieldsForStep = (step: number) => {
    switch (step) {
      case 1:
        return ['title', 'description'] as const
      case 2:
        return ['creditBudget', 'startDate', 'endDate'] as const
      case 3:
        return ['deliverables'] as const
      case 4:
        return ['requiredSkills'] as const
      default:
        return []
    }
  }

  const canProceedToNextStep = () => {
    const fieldsToValidate = getFieldsForStep(currentStep)
    return fieldsToValidate.every(field => {
      const fieldError = errors[field]
      const fieldValue = watchedFields[field]
      
      // Check if field has error
      if (fieldError) return false
      
      // Check if required field has value
      if (field === 'title' || field === 'description') {
        return fieldValue && typeof fieldValue === 'string' && fieldValue.trim().length > 0
      }
      if (field === 'creditBudget') {
        // FE-MED-004 FIX: Sync validation with schema max (50000)
        return fieldValue && typeof fieldValue === 'number' && fieldValue >= 50 && fieldValue <= 50000
      }
      if (field === 'deliverables') {
        const deliverables = fieldValue as typeof watchedFields.deliverables
        return deliverables && Array.isArray(deliverables) && deliverables.some((d) => d.description?.trim())
      }
      if (field === 'requiredSkills') {
        const skills = fieldValue as typeof watchedFields.requiredSkills
        return skills && Array.isArray(skills) && skills.some((s) => s.skillId)
      }
      
      return true
    })
  }

  const calculateProgress = () => {
    let completedSections = 0
    
    if (watchedFields.title && watchedFields.description) completedSections++
    if (watchedFields.creditBudget && watchedFields.startDate && watchedFields.endDate) completedSections++
    if (watchedFields.deliverables?.some(d => d.description)) completedSections++
    if (watchedFields.requiredSkills?.some(s => s.skillId)) completedSections++
    
    return (completedSections / 4) * 100
  }

  // Call debug function when form state changes
  useEffect(() => {
    // Debug function to help identify validation issues
    const debugFormState = () => {
      if (process.env.NODE_ENV === 'development') {
        logger.debug('Form Debug State', {
          isValid,
          isSubmitting,
          isLoading,
          isDraftMode,
          currentStep,
          watchedFields,
          errors,
          submitDisabled: isSubmitting || isLoading || (!isDraftMode && !isValid),
          component: 'ProjectCreationForm'
        });
      }
    }

    debugFormState();
  }, [isValid, isSubmitting, isLoading, isDraftMode, currentStep, watchedFields, errors]);

  return (
    <div className="w-full max-w-4xl mx-auto">
      {/* Progress Bar */}
      <div className="mb-8">
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-foreground">
            Step {currentStep} of 4
          </span>
          {isDraftMode && (
            <div className="text-sm text-muted-foreground">
              {isAutoSaving ? (
                'Saving draft...'
              ) : lastSavedDraft ? (
                `Draft saved at ${lastSavedDraft.toLocaleTimeString()}`
              ) : (
                'Draft mode - changes are auto-saved'
              )}
            </div>
          )}
        </div>
        <div className="w-full bg-muted rounded-full h-2">
          <div
            className="bg-primary h-2 rounded-full transition-all duration-300"
            style={{ width: `${Math.max((currentStep / 4) * 100, calculateProgress())}%` }}
          />
        </div>
        <div className="flex justify-between text-xs text-muted-foreground mt-1">
          <span>Basic Info</span>
          <span>Budget & Timeline</span>
          <span>Deliverables</span>
          <span>Skills Required</span>
        </div>
      </div>

      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-8">
        {/* Step 1: Basic Information */}
        {currentStep === 1 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Project Basic Information</h2>

            <div className="space-y-6">
              <div>
                <label htmlFor="title" className="block text-sm font-medium text-foreground">
                  Project Title *
                </label>
                <input
                  {...register('title')}
                  type="text"
                  id="title"
                  data-testid="project-title-input"
                  className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="Enter a clear, descriptive project title"
                />
                {errors.title && (
                  <p className="mt-2 text-sm text-destructive">{errors.title.message}</p>
                )}
              </div>

              <div>
                <label htmlFor="description" className="block text-sm font-medium text-foreground">
                  Project Description *
                </label>
                <textarea
                  {...register('description')}
                  id="description"
                  data-testid="project-description-input"
                  rows={6}
                  className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                  placeholder="Describe your project in detail. Include objectives, requirements, and expectations."
                />
                <div className="mt-1 text-sm text-muted-foreground">
                  {watchedFields.description?.length || 0}/5000 characters
                </div>
                {errors.description && (
                  <p className="mt-2 text-sm text-destructive">{errors.description.message}</p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Step 2: Budget and Timeline */}
        {currentStep === 2 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Budget and Timeline</h2>

            <div className="space-y-6">
              <div>
                <label htmlFor="creditBudget" className="block text-sm font-medium text-foreground">
                  Credit Budget *
                </label>
                <div className="mt-1 relative rounded-md shadow-sm">
                  <input
                    {...register('creditBudget', { valueAsNumber: true })}
                    type="number"
                    id="creditBudget"
                    data-testid="project-budget-input"
                    min="50"
                    max="50000"
                    className="block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                    placeholder="Enter credit amount (50-50,000)"
                  />
                  <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                    <span className="text-muted-foreground sm:text-sm">credits</span>
                  </div>
                </div>
                <p className="mt-1 text-sm text-muted-foreground">
                  Credits are used to compensate collaborators. Range: 50-50,000 credits.
                </p>
                {errors.creditBudget && (
                  <p className="mt-2 text-sm text-destructive">{errors.creditBudget.message}</p>
                )}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label htmlFor="startDate" className="block text-sm font-medium text-foreground">
                    Preferred Start Date
                  </label>
                  <input
                    {...register('startDate')}
                    type="date"
                    id="startDate"
                    data-testid="project-start-date-input"
                    min={new Date().toISOString().split('T')[0]}
                    className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                  />
                  {errors.startDate && (
                    <p className="mt-2 text-sm text-destructive">{errors.startDate.message}</p>
                  )}
                </div>

                <div>
                  <label htmlFor="endDate" className="block text-sm font-medium text-foreground">
                    Target Completion Date
                  </label>
                  <input
                    {...register('endDate')}
                    type="date"
                    id="endDate"
                    data-testid="project-end-date-input"
                    min={watchedFields.startDate || new Date().toISOString().split('T')[0]}
                    className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                  />
                  {errors.endDate && (
                    <p className="mt-2 text-sm text-destructive">{errors.endDate.message}</p>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Step 3: Deliverables */}
        {currentStep === 3 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Project Deliverables</h2>
            
            <div className="space-y-4">
              {deliverableFields.map((field, index) => (
                <div key={field.id} className="border border-border rounded-lg p-4">
                  <div className="flex justify-between items-center mb-4">
                    <h3 className="text-lg font-medium text-foreground">
                      Deliverable #{index + 1}
                    </h3>
                    {deliverableFields.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeDeliverable(index)}
                        className="text-destructive hover:text-destructive/80"
                      >
                        Remove
                      </button>
                    )}
                  </div>

                  <div className="space-y-4">
                    <div>
                      <label className="block text-sm font-medium text-foreground">
                        Description *
                      </label>
                      <textarea
                        {...register(`deliverables.${index}.description`)}
                        data-testid={`deliverable-description-${index}`}
                        rows={3}
                        className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                        placeholder="What needs to be delivered?"
                      />
                      {errors.deliverables?.[index]?.description && (
                        <p className="mt-1 text-sm text-destructive">
                          {errors.deliverables[index]?.description?.message}
                        </p>
                      )}
                    </div>

                    <div className="flex items-center space-x-4">
                      <div className="flex items-center">
                        <input
                          {...register(`deliverables.${index}.isRequired`)}
                          type="checkbox"
                          id={`deliverable-required-${index}`}
                          className="h-4 w-4 text-primary focus:ring-ring border-border rounded"
                        />
                        <label htmlFor={`deliverable-required-${index}`} className="ml-2 text-sm text-foreground">
                          Required for project completion
                        </label>
                      </div>

                      <input
                        {...register(`deliverables.${index}.orderIndex`, { valueAsNumber: true })}
                        type="hidden"
                        value={index + 1}
                      />
                    </div>
                  </div>
                </div>
              ))}

              {deliverableFields.length < 10 && (
                <button
                  type="button"
                  onClick={() => appendDeliverable({ description: '', orderIndex: deliverableFields.length + 1, isRequired: true })}
                  className="w-full border-2 border-dashed border-border rounded-lg p-4 text-center text-muted-foreground hover:border-muted-foreground/50 hover:text-foreground"
                >
                  + Add Another Deliverable
                </button>
              )}

              {errors.deliverables && (
                <p className="text-sm text-destructive">{errors.deliverables.message}</p>
              )}
            </div>
          </div>
        )}

        {/* Step 4: Skills Required */}
        {currentStep === 4 && (
          <div className="bg-card p-6 rounded-lg shadow-md">
            <h2 className="text-2xl font-bold text-foreground mb-6">Skills Required</h2>
            
            <div className="space-y-4">
              {skillFields.map((field, index) => (
                <div key={field.id} className="border border-border rounded-lg p-4">
                  <div className="flex justify-between items-center mb-4">
                    <h3 className="text-lg font-medium text-foreground">
                      Skill Requirement #{index + 1}
                    </h3>
                    {skillFields.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeSkill(index)}
                        className="text-destructive hover:text-destructive/80"
                      >
                        Remove
                      </button>
                    )}
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="md:col-span-1">
                      <label className="block text-sm font-medium text-foreground">
                        Skill *
                      </label>
                      <select
                        {...register(`requiredSkills.${index}.skillId`)}
                        data-testid={`skill-select-${index}`}
                        className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                      >
                        <option value="">Select a skill</option>
                        {availableSkills.map((skill) => (
                          <option key={skill.id} value={skill.id}>
                            {skill.name} ({skill.category})
                          </option>
                        ))}
                      </select>
                      {errors.requiredSkills?.[index]?.skillId && (
                        <p className="mt-1 text-sm text-destructive">
                          {errors.requiredSkills[index]?.skillId?.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-foreground">
                        Proficiency Level *
                      </label>
                      <select
                        {...register(`requiredSkills.${index}.proficiencyRequired`, { valueAsNumber: true })}
                        data-testid={`proficiency-select-${index}`}
                        className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                      >
                        {PROFICIENCY_LEVELS.map((level) => (
                          <option key={level.value} value={level.value}>
                            {level.label}
                          </option>
                        ))}
                      </select>
                      {errors.requiredSkills?.[index]?.proficiencyRequired && (
                        <p className="mt-1 text-sm text-destructive">
                          {errors.requiredSkills[index]?.proficiencyRequired?.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-foreground">
                        Importance
                      </label>
                      <select
                        {...register(`requiredSkills.${index}.weight`, { valueAsNumber: true })}
                        data-testid={`weight-select-${index}`}
                        className="mt-1 block w-full rounded-md border-border shadow-sm focus:border-primary focus:ring-ring"
                      >
                        {WEIGHT_LEVELS.map((weight) => (
                          <option key={weight.value} value={weight.value}>
                            {weight.label}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
              ))}

              {skillFields.length < 5 && (
                <button
                  type="button"
                  onClick={() => appendSkill({ skillId: '', proficiencyRequired: 3, weight: 3 })}
                  className="w-full border-2 border-dashed border-border rounded-lg p-4 text-center text-muted-foreground hover:border-muted-foreground/50 hover:text-foreground"
                >
                  + Add Another Skill Requirement
                </button>
              )}

              {errors.requiredSkills && (
                <p className="text-sm text-destructive">{errors.requiredSkills.message}</p>
              )}
            </div>
          </div>
        )}

        {/* Navigation Buttons */}
        <div className="flex justify-between items-center pt-6">
          <div className="flex space-x-3">
            {currentStep > 1 && (
              <button
                type="button"
                onClick={prevStep}
                className="px-4 py-2 border border-border rounded-full shadow-sm text-sm font-medium text-foreground bg-card hover:bg-muted focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
              >
                Previous
              </button>
            )}

            {isDraftMode && (
              <button
                type="button"
                onClick={handleSaveDraftClick}
                disabled={isAutoSaving}
                className="px-4 py-2 border border-primary/30 rounded-full shadow-sm text-sm font-medium text-primary bg-primary/10 hover:bg-primary/20 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50"
              >
                {isAutoSaving ? 'Saving...' : 'Save Draft'}
              </button>
            )}
          </div>

          <div className="flex space-x-3">
            {currentStep < 4 ? (
              <button
                type="button"
                onClick={nextStep}
                disabled={!canProceedToNextStep()}
                className="px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next
              </button>
            ) : (
              <button
                type="submit"
                disabled={isSubmitting || isLoading || (!isDraftMode && !isValid)}
                className="px-4 py-2 border border-transparent rounded-full shadow-sm text-sm font-medium text-success-foreground bg-success hover:bg-success/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
                data-testid="create-project-submit-button"
              >
                {isSubmitting || isLoading ? 'Creating Project...' : 'Create Project'}
              </button>
            )}
          </div>
        </div>
      </form>
    </div>
  )
})

export default ProjectCreationForm
