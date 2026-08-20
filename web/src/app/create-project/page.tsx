'use client'

import React, { useState, useEffect, Suspense } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import ProjectCreationForm from '@/components/ProjectCreationForm'
import { ProjectCreationGuard } from '@/components/SubscriptionGuard'
import { logger } from '@/utils/logger'
import { trackEvent } from '@/utils/analytics'

interface Skill {
  id: string
  name: string
  description: string
  category: string
}

interface ProjectFormData {
  title: string
  description: string
  creditBudget: number
  startDate?: string
  endDate?: string
  deliverables: Array<{
    description: string
    orderIndex: number
    isRequired: boolean
  }>
  requiredSkills: Array<{
    skillId: string
    proficiencyRequired: number
    weight: number
  }>
}

interface DraftFormData extends Partial<ProjectFormData> {}

interface CreateProjectResponse {
  success: boolean
  message: string
  project?: {
    id: string
    title: string
    status: string
  }
}

function CreateProjectPageContent() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [availableSkills, setAvailableSkills] = useState<Skill[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isDraftMode, setIsDraftMode] = useState(false)
  const [projectId, setProjectId] = useState<string | null>(null)
  const [initialFormData, setInitialFormData] = useState<Partial<ProjectFormData> | undefined>(undefined)
  const [draftSaveStatus, setDraftSaveStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle')
  
  // Check for draft mode from URL parameters
  useEffect(() => {
    const draftParam = searchParams?.get('draft')
    const idParam = searchParams?.get('id')
    
    if (draftParam === 'true') {
      setIsDraftMode(true)
    }
    
    if (idParam) {
      setProjectId(idParam)
    }
  }, [searchParams])

  // Load available skills - extracted as reusable function for retry capability
  const loadSkills = async () => {
    try {
      setError(null)
      logger.debug('Loading skills from API...', { component: 'CreateProject' })
      const response = await fetch('/api/skill?take=100', {
        credentials: 'include',
      })

      logger.debug('Skills API response', {
        component: 'CreateProject',
        status: response.status,
        statusText: response.statusText
      })

      if (response.ok) {
        const data = await response.json()
        logger.debug('Skills API data structure', {
          component: 'CreateProject',
          keys: Object.keys(data)
        })
        logger.debug('Skills data', { component: 'CreateProject', data })

        // Handle different response structures
        let skillsArray = []
        if (Array.isArray(data.Skills)) {
          skillsArray = data.Skills
        } else if (Array.isArray(data.skills)) {
          skillsArray = data.skills
        } else if (Array.isArray(data)) {
          skillsArray = data
        }

        logger.debug(`Loaded ${skillsArray.length} skills`, { component: 'CreateProject' })
        setAvailableSkills(skillsArray)

        if (skillsArray.length === 0) {
          logger.warn('No skills loaded - form will not render', { component: 'CreateProject' })
          setError('No skills available. Please contact support.')
        }
      } else {
        logger.error('Failed to load skills', undefined, {
          component: 'CreateProject',
          status: response.status
        })
        setError('Failed to load available skills.')
      }
    } catch (error) {
      logger.error('Error loading skills', error, { component: 'CreateProject' })
      setError('Failed to load available skills.')
    }
  }

  // Initial load of skills
  useEffect(() => {
    loadSkills()
  }, [])

  // Load existing project data if editing
  useEffect(() => {
    const loadProjectData = async () => {
      if (!projectId) return

      try {
        const response = await fetch(`/api/project/${projectId}`, {
          credentials: 'include',
        })

        if (response.ok) {
          const project = await response.json()
          
          // Map project data to form format
          const formData: Partial<ProjectFormData> = {
            title: project.title,
            description: project.description,
            creditBudget: project.creditBudget,
            // BUG-UI-003 FIX: Use ISO date formatting for reliable date input compatibility
            startDate: project.startDate ? new Date(project.startDate).toISOString().split('T')[0] : undefined,
            endDate: project.endDate ? new Date(project.endDate).toISOString().split('T')[0] : undefined,
            deliverables: project.deliverables?.map((d: { description: string; isRequired?: boolean }, index: number) => ({
              description: d.description,
              orderIndex: index + 1,
              isRequired: d.isRequired ?? true
            })) || [],
            requiredSkills: project.requiredSkills?.map((rs: { skillId: string; proficiencyRequired?: number; weight?: number }) => ({
              skillId: rs.skillId,
              proficiencyRequired: rs.proficiencyRequired || 3,
              weight: rs.weight || 1
            })) || []
          }
          
          setInitialFormData(formData)
          logger.debug('Loaded project data', { component: 'CreateProject', formData })
        } else if (response.status === 404) {
          setError('Project not found or you do not have permission to edit it.')
        } else {
          setError('Failed to load project data.')
        }
      } catch (error) {
        logger.error('Error loading project', error, { component: 'CreateProject' })
        setError('Failed to load project data.')
      }
    }

    loadProjectData()
  }, [projectId])

  const getCsrfToken = async (): Promise<string | null> => {
    try {
      const response = await fetch('/api/auth/csrf-token', {
        credentials: 'include',
      })
      
      if (response.ok) {
        const data = await response.json()
        return data.token
      }
    } catch (error) {
      logger.error('Failed to get CSRF token', error, { component: 'CreateProject' })
    }

    return null
  }

  const handleSubmit = async (formData: ProjectFormData) => {
    setIsLoading(true)
    setError(null)
    setSuccessMessage(null)

    try {
      const csrfToken = await getCsrfToken()
      if (!csrfToken) {
        throw new Error('Failed to get CSRF token')
      }

      // Convert form data to API format
      const apiData = {
        title: formData.title,
        description: formData.description,
        creditBudget: formData.creditBudget,
        startDate: formData.startDate ? new Date(formData.startDate).toISOString() : undefined,
        endDate: formData.endDate ? new Date(formData.endDate).toISOString() : undefined,
        deliverables: formData.deliverables.map(d => ({
          description: d.description,
          orderIndex: d.orderIndex,
          isRequired: d.isRequired
        })),
        requiredSkills: formData.requiredSkills.map(s => ({
          skillId: s.skillId,
          proficiencyRequired: s.proficiencyRequired,
          weight: s.weight
        }))
      }

      const endpoint = projectId ? `/api/project/${projectId}` : '/api/project'
      const method = projectId ? 'PUT' : 'POST'

      const response = await fetch(endpoint, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include',
        body: JSON.stringify(apiData),
      })

      const result: CreateProjectResponse = await response.json()

      if (response.ok && result.success) {
        setSuccessMessage(result.message || 'Project created successfully!')

        // Track successful project creation
        trackEvent({
          name: projectId ? 'project_updated' : 'project_created',
          category: 'projects',
          priority: 'critical',
          properties: {
            project_id: result.project?.id,
            credit_budget: formData.creditBudget,
            has_deliverables: formData.deliverables.length > 0,
            deliverables_count: formData.deliverables.length,
            required_skills_count: formData.requiredSkills.length,
            has_dates: !!(formData.startDate && formData.endDate),
          },
        })

        // Redirect to project view after success
        setTimeout(() => {
          if (result.project?.id) {
            router.push(`/project/${result.project.id}`)
          } else {
            router.push('/my-projects')
          }
        }, 2000)
      } else {
        if (response.status === 401) {
          setError('You must be logged in to create projects.')
        } else if (response.status === 403) {
          setError('You do not have permission to perform this action.')
        } else if (response.status === 429) {
          setError('Too many requests. Please wait a moment and try again.')
        } else {
          setError(result.message || 'Failed to create project. Please try again.')
        }
      }
    } catch (error) {
      logger.error('Error creating project', error, { component: 'CreateProject' })
      setError('Network error. Please check your connection and try again.')
    } finally {
      setIsLoading(false)
    }
  }

  const handleSaveDraft = async (formData: DraftFormData) => {
    setDraftSaveStatus('saving')

    try {
      const csrfToken = await getCsrfToken()
      if (!csrfToken) {
        logger.error('Failed to get CSRF token for draft save', undefined, { component: 'CreateProject' })
        setDraftSaveStatus('error')
        return
      }

      // Convert form data to API format
      const apiData = {
        title: formData.title,
        description: formData.description,
        creditBudget: formData.creditBudget,
        startDate: formData.startDate ? new Date(formData.startDate).toISOString() : undefined,
        endDate: formData.endDate ? new Date(formData.endDate).toISOString() : undefined,
        deliverables: formData.deliverables?.map(d => ({
          description: d.description,
          orderIndex: d.orderIndex,
          isRequired: d.isRequired
        })),
        requiredSkills: formData.requiredSkills?.map(s => ({
          skillId: s.skillId,
          proficiencyRequired: s.proficiencyRequired,
          weight: s.weight
        }))
      }

      const endpoint = projectId ? `/api/project/${projectId}/draft` : '/api/project/draft'
      const method = projectId ? 'PUT' : 'POST'

      const response = await fetch(endpoint, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include',
        body: JSON.stringify(apiData),
      })

      const result: CreateProjectResponse = await response.json()

      if (response.ok && result.success) {
        setDraftSaveStatus('saved')

        // Update project ID if this was a new draft
        if (!projectId && result.project?.id) {
          setProjectId(result.project.id)
          // Update URL to include project ID for future saves
          const newUrl = new URL(window.location.href)
          newUrl.searchParams.set('id', result.project.id)
          newUrl.searchParams.set('draft', 'true')
          window.history.replaceState({}, '', newUrl.toString())
        }

        // Auto-dismiss after 3 seconds
        setTimeout(() => {
          setDraftSaveStatus('idle')
        }, 3000)
      } else {
        logger.error('Draft save failed', undefined, {
          component: 'CreateProject',
          message: result.message
        })
        setDraftSaveStatus('error')

        // Auto-dismiss error after 5 seconds
        setTimeout(() => {
          setDraftSaveStatus('idle')
        }, 5000)
      }
    } catch (error) {
      logger.error('Error saving draft', error, { component: 'CreateProject' })
      setDraftSaveStatus('error')

      // Auto-dismiss error after 5 seconds
      setTimeout(() => {
        setDraftSaveStatus('idle')
      }, 5000)
    }
  }

  const toggleDraftMode = () => {
    setIsDraftMode(!isDraftMode)
    
    // Update URL to reflect draft mode
    const newUrl = new URL(window.location.href)
    if (!isDraftMode) {
      newUrl.searchParams.set('draft', 'true')
    } else {
      newUrl.searchParams.delete('draft')
    }
    window.history.replaceState({}, '', newUrl.toString())
  }

  return (
    <div className="min-h-screen bg-background py-8">
      <div className="container-premium">
        {/* Header */}
        <div className="mb-8">
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-display text-foreground">
                {projectId ? 'Edit Project' : 'Create New Project'}
              </h1>
              <p className="mt-2 text-muted-foreground">
                {isDraftMode
                  ? 'Working in draft mode - your changes are automatically saved'
                  : 'Create a structured project with clear deliverables and skill requirements'
                }
              </p>
            </div>

            <div className="flex items-center space-x-4">
              {/* Draft Save Status Indicator */}
              {draftSaveStatus !== 'idle' && (
                <div
                  className={`px-3 py-1.5 rounded-md text-xs font-medium transition-all ${
                    draftSaveStatus === 'saving'
                      ? 'bg-info/10 text-info border border-info/20'
                      : draftSaveStatus === 'saved'
                      ? 'bg-success/10 text-success border border-success/20'
                      : 'bg-destructive/10 text-destructive border border-destructive/20'
                  }`}
                >
                  {draftSaveStatus === 'saving' && (
                    <span className="flex items-center gap-1.5">
                      <span className="animate-spin h-3 w-3 border-2 border-info/30 border-t-info rounded-full"></span>
                      Saving draft...
                    </span>
                  )}
                  {draftSaveStatus === 'saved' && '✓ Draft saved'}
                  {draftSaveStatus === 'error' && '✗ Failed to save draft'}
                </div>
              )}

              <button
                onClick={toggleDraftMode}
                className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                  isDraftMode
                    ? 'bg-warning/10 text-warning border border-warning/20'
                    : 'bg-muted text-foreground border border-border'
                }`}
              >
                {isDraftMode ? '📝 Draft Mode' : '📄 Standard Mode'}
              </button>

              <button
                onClick={() => router.back()}
                className="btn-secondary"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-6 bg-destructive/10 border border-destructive/20 rounded-md p-4">
            <div className="flex items-start justify-between">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-destructive">Error</h3>
                  <div className="mt-2 text-sm text-destructive/80">
                    <p>{error}</p>
                  </div>
                </div>
              </div>
              <button
                onClick={() => loadSkills()}
                className="px-3 py-1.5 text-sm font-medium bg-destructive/20 hover:bg-destructive/30 text-destructive rounded-full transition-colors"
              >
                Try Again
              </button>
            </div>
          </div>
        )}

        {/* Success Message */}
        {successMessage && (
          <div className="mb-6 bg-success/10 border border-success/20 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-success" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-success">Success</h3>
                <div className="mt-2 text-sm text-success/80">
                  <p>{successMessage}</p>
                  <p className="mt-1 text-success/70">Redirecting to your project...</p>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Loading State */}
        {availableSkills.length === 0 && !error && (
          <div className="text-center py-12">
            <div className="loading-spinner mx-auto"></div>
            <p className="mt-2 text-muted-foreground">Loading skills...</p>
          </div>
        )}

        {/* Project Creation Form */}
        {availableSkills.length > 0 && (
          <ProjectCreationForm
            availableSkills={availableSkills}
            onSubmit={handleSubmit}
            onSaveDraft={handleSaveDraft}
            initialData={initialFormData}
            isLoading={isLoading}
            isDraftMode={isDraftMode}
          />
        )}

        {/* Help Section */}
        <div className="mt-12 bg-info/10 rounded-lg p-6 border border-info/20">
          <h3 className="text-lg font-semibold text-info mb-4">Tips for Creating a Great Project</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-sm text-foreground">
            <div>
              <h4 className="font-medium mb-2">Writing Project Descriptions</h4>
              <ul className="space-y-1 list-disc list-inside text-muted-foreground">
                <li>Be specific about your goals and expectations</li>
                <li>Include relevant context and background information</li>
                <li>Mention any preferred tools or technologies</li>
                <li>Specify the target audience or use case</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">Setting Requirements</h4>
              <ul className="space-y-1 list-disc list-inside text-muted-foreground">
                <li>Budget fairly - consider the complexity and time required</li>
                <li>Allow reasonable timelines for quality work</li>
                <li>Be clear about deliverable expectations</li>
                <li>Match skill requirements to actual needs</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default function CreateProjectPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="loading-spinner"></div>
      </div>
    }>
      <ProjectCreationGuard>
        <CreateProjectPageContent />
      </ProjectCreationGuard>
    </Suspense>
  )
}