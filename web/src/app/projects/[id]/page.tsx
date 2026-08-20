'use client'

import { logger } from '@/utils/logger';
import { fetchWithAuth } from '@/utils/apiClient';

import React, { useState, useEffect } from 'react'
import { useRouter, useParams } from 'next/navigation'
import Link from 'next/link'
import dynamic from 'next/dynamic'
import { ArrowLeft, Calendar, DollarSign, MapPin, Clock, CheckCircle, XCircle } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'

const ProjectApplicationForm = dynamic(() => import('@/components/ProjectApplicationForm'), {
  loading: () => React.createElement('div', { className: 'text-center py-4' }, 'Loading form...'),
  ssr: false,
})

interface Project {
  id: string
  title: string
  description: string
  creditBudget: number
  startDate?: string
  endDate?: string
  status: string
  location?: {
    city?: string
    state?: string
    country?: string
  }
  locationCity?: string
  locationState?: string
  locationCountry?: string
  skills?: Array<{
    skillId: string
    skillName: string
    proficiencyRequired: number
    weight: number
  }>
  requiredSkills?: Array<{
    skill: {
      id: string
      name: string
      description?: string
      category?: string
    }
    proficiencyRequired: number
    proficiencyDisplay?: string
    weight: number
    weightDisplay?: string
  }>
  deadline?: string
  deliverables?: Array<{
    id: string
    description: string
    orderIndex: number
    isRequired: boolean
    isCompleted?: boolean
  }>
  milestones?: Array<{
    id: string
    title: string
    description: string
    status: string
    dueDate: string
    deliverables: string[]
  }>
  client: {
    id: string
    userName?: string
    displayName?: string
    profileComplete?: boolean
  }
  createdAt: string
  isUrgent?: boolean
  isFeatured?: boolean
  durationDisplay?: string
}

export default function ProjectDetailPage() {
  const router = useRouter()
  const params = useParams()
  const projectId = params?.id as string
  const { user, isAuthenticated, isInitialized } = useAuth()
  
  const [project, setProject] = useState<Project | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showApplicationForm, setShowApplicationForm] = useState(false)
  const [hasApplied, setHasApplied] = useState(false)

  useEffect(() => {
    const loadProject = async () => {
      if (!projectId) return

      // BUG-006 FIX: Add retry logic for intermittent failures
      const maxRetries = 3
      let lastError: Error | null = null

      for (let attempt = 1; attempt <= maxRetries; attempt++) {
        try {
          setIsLoading(true)
          setError(null)

          const response = await fetch(`/api/project/${projectId}`, {
            credentials: 'include',
          })

          if (response.ok) {
            const data = await response.json()
            // BUG-006 FIX: Handle both response formats and null checks
            const projectData = data.project || data
            if (projectData && projectData.id) {
              setProject(projectData)
              setIsLoading(false)
              return // Success, exit retry loop
            } else {
              throw new Error('Invalid project data received')
            }
          } else if (response.status === 404) {
            setError('Project not found')
            setIsLoading(false)
            return // No retry needed for 404
          } else if (response.status === 401) {
            setError('Please log in to view this project')
            setIsLoading(false)
            return // No retry for auth issues
          } else {
            throw new Error(`Server error: ${response.status}`)
          }
        } catch (err) {
          lastError = err instanceof Error ? err : new Error(String(err))
          logger.error(`Error loading project (attempt ${attempt}/${maxRetries})`, err, { page: 'project-detail', projectId })

          // Wait before retry (exponential backoff)
          if (attempt < maxRetries) {
            await new Promise(resolve => setTimeout(resolve, Math.pow(2, attempt) * 500))
          }
        }
      }

      // All retries failed
      logger.error('All retries failed for project load', lastError, { page: 'project-detail', projectId })
      setError('Unable to load project details. Please try refreshing the page.')
      setIsLoading(false)
    }

    loadProject()
  }, [projectId])

  // Check if user already applied
  useEffect(() => {
    const checkApplication = async () => {
      if (!projectId || !isAuthenticated) return

      try {
        const response = await fetch(`/api/project-applications/can-apply/${projectId}`, {
          credentials: 'include',
        })

        if (response.ok) {
          const data = await response.json()
          setHasApplied(!data.canApply)
        }
      } catch (err) {
        logger.error('Error checking application status', err, { page: 'project-detail' })
      }
    }

    checkApplication()
  }, [projectId, isAuthenticated])

  const handleMilestoneComplete = async (milestoneId: string) => {
    try {
      await fetchWithAuth(`/api/projects/${projectId}/milestones/${milestoneId}/complete`, {
        method: 'POST',
      })

      // Refresh project data to get updated milestones
      const updatedData = await fetchWithAuth<{ project: Project }>(`/api/projects/${projectId}`)
      setProject(updatedData.project)
      logger.info('Milestone marked as complete', { milestoneId })
    } catch (error) {
      logger.error('Error completing milestone', error, { page: 'project-detail' })
    }
  }

  const handleApplicationSuccess = () => {
    setShowApplicationForm(false)
    setHasApplied(true)
    alert('Application submitted successfully! The client will review your proposal and get back to you soon.')
  }

  const getMilestoneStatusClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'completed':
        return 'bg-success/10 text-success'
      case 'in progress':
        return 'bg-primary/10 text-primary'
      case 'pending':
        return 'bg-warning/10 text-warning'
      case 'overdue':
        return 'bg-destructive/10 text-destructive'
      default:
        return 'bg-muted text-muted-foreground'
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading project details...</p>
        </div>
      </div>
    )
  }

  if (error || !project) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background px-6">
        <div className="text-center space-y-6 max-w-md">
          <XCircle className="w-16 h-16 text-destructive mx-auto" />
          <h1 className="text-2xl font-bold text-foreground">{error || 'Project not found'}</h1>
          <p className="text-muted-foreground">The project you're looking for doesn't exist or has been removed.</p>
          <Link href="/projects/search" className="btn-primary inline-block">
            Browse Projects
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-muted">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Back Button */}
        <button
          onClick={() => router.back()}
          className="flex items-center text-muted-foreground hover:text-foreground mb-6"
        >
          <ArrowLeft className="w-5 h-5 mr-2" />
          Back to Projects
        </button>

        {/* Project Header */}
        <div className="bg-card rounded-lg shadow-lg p-8 mb-6">
          <div className="flex items-start justify-between mb-4">
            <div className="flex-1">
              <h1 className="text-3xl font-bold text-foreground mb-2">{project.title}</h1>
              <div className="flex items-center space-x-4 text-sm text-muted-foreground">
                <span>Posted by {project.client.userName || project.client.displayName || 'Unknown'}</span>
                <span>•</span>
                <span>{new Date(project.createdAt).toLocaleDateString()}</span>
                {project.isUrgent && (
                  <>
                    <span>•</span>
                    <span className="bg-destructive/10 text-destructive px-2 py-1 rounded font-medium">Urgent</span>
                  </>
                )}
                {project.isFeatured && (
                  <>
                    <span>•</span>
                    <span className="bg-warning/10 text-warning px-2 py-1 rounded font-medium">Featured</span>
                  </>
                )}
              </div>
            </div>
            <div className="text-right">
              <div className="text-sm text-muted-foreground">Budget</div>
              <div className="text-3xl font-bold text-primary">{project.creditBudget}</div>
              <div className="text-sm text-muted-foreground">credits</div>
            </div>
          </div>

          {/* Project Status */}
          <div className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-success/10 text-success">
            <CheckCircle className="w-4 h-4 mr-1" />
            {project.status}
          </div>
        </div>

        {/* Project Details Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Description */}
            <div className="bg-card rounded-lg shadow p-6">
              <h2 className="text-xl font-bold text-foreground mb-4">Project Description</h2>
              <p className="text-muted-foreground whitespace-pre-wrap leading-relaxed">{project.description}</p>
            </div>

            {/* Deliverables */}
            {project.deliverables && project.deliverables.length > 0 && (
              <div className="bg-card rounded-lg shadow p-6">
                <h2 className="text-xl font-bold text-foreground mb-4">Deliverables</h2>
                <ul className="space-y-3">
                  {project.deliverables
                    .sort((a, b) => a.orderIndex - b.orderIndex)
                    .map((deliverable, index) => (
                      <li key={index} className="flex items-start">
                        <CheckCircle className="w-5 h-5 mr-3 mt-0.5 flex-shrink-0 text-muted-foreground" />
                        <div className="flex-1">
                          <p className="text-foreground">{deliverable.description}</p>
                          <div className="flex items-center space-x-2 mt-1">
                            <span className="text-sm font-medium text-warning">
                              Pending
                            </span>
                          </div>
                        </div>
                      </li>
                    ))}
                </ul>
              </div>
            )}

            {/* Milestones Management */}
            <div className="bg-card rounded-lg shadow p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-bold text-foreground">Project Milestones</h2>
                {user?.id === project?.client.id && (
                  <button
                    onClick={() => setShowApplicationForm(true)}
                    className="btn-primary text-sm px-4 py-2"
                    data-testid="add-milestone-button"
                  >
                    Add Milestone
                  </button>
                )}
              </div>
              <div className="space-y-4">
                {project.milestones && project.milestones.length > 0 ? (
                  project.milestones
                    .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())
                    .map((milestone, index) => (
                      <div key={index} className="bg-card rounded-lg shadow p-4 border border-border">
                        <div className="flex items-center justify-between mb-3">
                          <div className="flex items-center space-x-2">
                            <h3 className="text-lg font-semibold text-foreground">{milestone.title}</h3>
                            <span className={`px-2 py-1 rounded-full text-xs font-medium ${getMilestoneStatusClass(milestone.status)}`}>
                              {milestone.status}
                            </span>
                            <span className="text-xs text-muted-foreground ml-2">
                              Due: {new Date(milestone.dueDate).toLocaleDateString()}
                            </span>
                          </div>
                          <button
                            onClick={() => handleMilestoneComplete(milestone.id)}
                            className={`text-sm px-2 py-1 rounded ${
                              milestone.status === 'Completed' ? 'bg-muted text-muted-foreground' : 'bg-primary text-primary-foreground hover:bg-primary/90'
                            }`}
                            disabled={milestone.status === 'Completed'}
                            data-testid={`complete-milestone-${milestone.id}`}
                          >
                            {milestone.status === 'Completed' ? 'Reopen' : 'Mark Complete'}
                          </button>
                        </div>
                        <div className="mt-3">
                          <p className="text-muted-foreground mb-2">{milestone.description}</p>
                          {milestone.deliverables && milestone.deliverables.length > 0 && (
                            <div className="bg-muted rounded p-3">
                              <h4 className="text-sm font-medium text-foreground mb-2">Related Deliverables:</h4>
                              <ul className="space-y-2">
                                {milestone.deliverables.map((deliverableId, index) => {
                                  const deliverable = project.deliverables?.find(d => d.id === deliverableId)
                                  return deliverable ? (
                                    <li key={index} className="flex items-center text-sm">
                                      <CheckCircle className="w-4 h-4 mr-2 text-success" />
                                      <span>{deliverable.description}</span>
                                    </li>
                                  ) : null
                                })}
                              </ul>
                            </div>
                          )}
                        </div>
                      </div>
                    ))
                ) : (
                  <div className="text-center text-muted-foreground py-8">
                    <p>No milestones defined for this project</p>
                  </div>
                )}
              </div>
            </div>

            {/* Required Skills - handle both skills and requiredSkills formats */}
            {((project.skills && project.skills.length > 0) || (project.requiredSkills && project.requiredSkills.length > 0)) && (
              <div className="bg-card rounded-lg shadow p-6">
                <h2 className="text-xl font-bold text-foreground mb-4">Required Skills</h2>
                <div className="flex flex-wrap gap-3">
                  {/* Handle skills format */}
                  {(project.skills || []).map((skill) => (
                    <div
                      key={skill.skillId}
                      className="bg-primary/10 text-primary px-4 py-2 rounded-lg flex items-center space-x-2"
                    >
                      <span className="font-medium">{skill.skillName}</span>
                      {skill.proficiencyRequired > 1 && (
                        <span className="text-primary">
                          {'★'.repeat(skill.proficiencyRequired)}
                        </span>
                      )}
                    </div>
                  ))}
                  {/* Handle requiredSkills format from API */}
                  {(project.requiredSkills || []).map((rs) => (
                    <div
                      key={rs.skill.id}
                      className="bg-primary/10 text-primary px-4 py-2 rounded-lg flex items-center space-x-2"
                    >
                      <span className="font-medium">{rs.skill.name}</span>
                      {rs.proficiencyDisplay && (
                        <span className="text-xs text-primary ml-1">({rs.proficiencyDisplay})</span>
                      )}
                      {!rs.proficiencyDisplay && rs.proficiencyRequired > 1 && (
                        <span className="text-primary">
                          {'★'.repeat(rs.proficiencyRequired)}
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Project Info */}
            <div className="bg-card rounded-lg shadow p-6">
              <h3 className="text-lg font-bold text-foreground mb-4">Project Information</h3>
              <div className="space-y-4">
                {project.startDate && (
                  <div className="flex items-center text-foreground">
                    <Calendar className="w-5 h-5 mr-3 text-muted-foreground" />
                    <div>
                      <div className="text-sm text-muted-foreground">Start Date</div>
                      <div className="font-medium">{new Date(project.startDate).toLocaleDateString()}</div>
                    </div>
                  </div>
                )}
                {project.endDate && (
                  <div className="flex items-center text-foreground">
                    <Clock className="w-5 h-5 mr-3 text-muted-foreground" />
                    <div>
                      <div className="text-sm text-muted-foreground">End Date</div>
                      <div className="font-medium">{new Date(project.endDate).toLocaleDateString()}</div>
                    </div>
                  </div>
                )}
                {project.location && (
                  <div className="flex items-center text-foreground">
                    <MapPin className="w-5 h-5 mr-3 text-muted-foreground" />
                    <div>
                      <div className="text-sm text-muted-foreground">Location</div>
                      <div className="font-medium">
                        {[project.location.city, project.location.state, project.location.country]
                          .filter(Boolean)
                          .join(', ')}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Apply Button */}
            <div className="bg-card rounded-lg shadow p-6">
              {!isInitialized ? (
                // Show loading skeleton while auth is initializing to prevent "Login to Apply" flash
                <div className="animate-pulse">
                  <div className="h-12 bg-muted rounded-lg" />
                  <div className="h-4 bg-muted rounded mt-3 mx-auto w-2/3" />
                </div>
              ) : !isAuthenticated ? (
                <>
                  <Link href="/login" className="w-full btn-primary text-lg py-3 block text-center">
                    Login to Apply
                  </Link>
                  <p className="text-sm text-muted-foreground mt-3 text-center">
                    You must be logged in to apply to this project
                  </p>
                </>
              ) : hasApplied ? (
                <>
                  <div className="w-full bg-success/10 text-success text-lg py-3 rounded-lg text-center font-medium">
                    ✓ Application Submitted
                  </div>
                  <p className="text-sm text-muted-foreground mt-3 text-center">
                    You've already applied to this project. The client will review your application soon.
                  </p>
                </>
              ) : user?.id === project?.client.id ? (
                <>
                  <div className="w-full bg-muted text-muted-foreground text-lg py-3 rounded-lg text-center font-medium">
                    Your Project
                  </div>
                  <p className="text-sm text-muted-foreground mt-3 text-center">
                    This is your project
                  </p>
                </>
              ) : (
                <>
                  <button
                    onClick={() => setShowApplicationForm(true)}
                    className="w-full btn-primary text-lg py-3"
                    data-testid="apply-button"
                  >
                    Apply to Project
                  </button>
                  <p className="text-sm text-muted-foreground mt-3 text-center">
                    Submit your proposal and collaborate on this exciting project
                  </p>
                </>
              )}
            </div>

            {/* Client Info */}
            <div className="bg-card rounded-lg shadow p-6">
              <h3 className="text-lg font-bold text-foreground mb-4">About the Client</h3>
              <div className="flex items-center space-x-3 mb-3">
                <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center">
                  <span className="text-primary font-bold text-lg">
                    {(project.client.userName || project.client.displayName || 'U').charAt(0).toUpperCase()}
                  </span>
                </div>
                <div>
                  <div className="font-medium text-foreground">{project.client.userName || project.client.displayName || 'Unknown'}</div>
                  {project.client.profileComplete && (
                    <div className="text-sm text-success flex items-center">
                      <CheckCircle className="w-4 h-4 mr-1" />
                      Verified Profile
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Application Form Modal */}
        {showApplicationForm && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="max-w-2xl w-full max-h-[90vh] overflow-y-auto">
              <ProjectApplicationForm
                project={project}
                onSubmit={async (data) => {
                  try {
                    // Convert estimatedDuration to days for backend
                    const durationToDays: Record<string, number> = {
                      'Less than 1 week': 5,
                      '1-2 weeks': 10,
                      '2-4 weeks': 21,
                      '1-2 months': 45,
                      '2-3 months': 75,
                      '3-6 months': 135,
                      '6+ months': 180,
                    }
                    const proposedTimeline = durationToDays[data.estimatedDuration] || 30

                    // Determine if available immediately (start date within 7 days)
                    const startDate = new Date(data.availabilityStartDate)
                    const now = new Date()
                    const daysDiff = Math.ceil((startDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))
                    const isAvailableImmediately = daysDiff <= 7

                    await fetchWithAuth('/api/project-applications', {
                      method: 'POST',
                      body: JSON.stringify({
                        projectId: project.id,
                        coverLetter: data.coverLetter,
                        proposedTimeline: proposedTimeline,
                        isAvailableImmediately: isAvailableImmediately,
                        proposedBudget: Math.round(data.proposedRate),
                      }),
                    })

                    setShowApplicationForm(false)
                    setHasApplied(true)
                    alert('Application submitted successfully! The client will review your proposal and get back to you soon.')
                  } catch (error) {
                    logger.error('Application submission failed', error, { page: 'project-detail', projectId })
                    alert(error instanceof Error ? error.message : 'Failed to submit application. Please try again.')
                    throw error // Re-throw so the form knows submission failed
                  }
                }}
                onCancel={() => setShowApplicationForm(false)}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

