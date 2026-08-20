'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'

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
  skills: Array<{
    skillId: string
    skillName: string
    proficiencyRequired: number
    weight: number
  }>
  client: {
    id: string
    userName: string
    profileComplete: boolean
  }
  createdAt: string
  isUrgent: boolean
  isFeatured: boolean
  complexityScore: number
  matchScore?: number
  matchReasons?: string[]
}

interface ProjectRecommendationsProps {
  limit?: number
  showMatchReasons?: boolean
  onProjectClick?: (projectId: string) => void
  excludeProjectIds?: string[]
}

const DEFAULT_EXCLUDE_PROJECT_IDS: string[] = []

const ProjectRecommendations: React.FC<ProjectRecommendationsProps> = ({
  limit = 6,
  showMatchReasons = true,
  onProjectClick,
  excludeProjectIds = DEFAULT_EXCLUDE_PROJECT_IDS
}) => {
  const { user, isAuthenticated } = useAuth()
  const router = useRouter()
  const [recommendations, setRecommendations] = useState<Project[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshing, setRefreshing] = useState(false)

  const loadRecommendations = useCallback(async () => {
    if (!user) return

    setIsLoading(true)
    setError(null)

    try {
      const params = new URLSearchParams({
        limit: limit.toString(),
      })
      
      if (excludeProjectIds.length > 0) {
        params.append('exclude', excludeProjectIds.join(','))
      }

      const response = await fetch(`/api/project-search/recommendations?${params}`, {
        credentials: 'include',
      })

      if (response.ok) {
        const projects = await response.json()
        setRecommendations(projects)
      } else if (response.status === 401) {
        setError('Please log in to see personalized recommendations')
      } else if (response.status === 404) {
        // No recommendations available - not an error
        setRecommendations([])
      } else {
        throw new Error('Failed to load recommendations')
      }
    } catch (error) {
      logger.error('Error loading recommendations:', error)
      setError('Failed to load recommendations')
    } finally {
      setIsLoading(false)
    }
  }, [user, excludeProjectIds, limit])

  useEffect(() => {
    if (isAuthenticated && user) {
      loadRecommendations()
    } else {
      setIsLoading(false)
    }
  }, [isAuthenticated, user, loadRecommendations])

  const refreshRecommendations = async () => {
    setRefreshing(true)
    await loadRecommendations()
    setRefreshing(false)
  }

  const handleProjectClick = (projectId: string) => {
    if (onProjectClick) {
      onProjectClick(projectId)
    } else {
      router.push(`/projects/${projectId}`)
    }
  }

  const getMatchReasonIcon = (reason: string) => {
    if (reason.toLowerCase().includes('skill')) {
      return (
        <svg className="w-4 h-4 text-info" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
        </svg>
      )
    } else if (reason.toLowerCase().includes('budget')) {
      return (
        <svg className="w-4 h-4 text-success" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
        </svg>
      )
    } else if (reason.toLowerCase().includes('location')) {
      return (
        <svg className="w-4 h-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
      )
    } else {
      return (
        <svg className="w-4 h-4 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      )
    }
  }

  const getMatchScoreColor = (score?: number) => {
    if (!score) return 'bg-muted text-muted-foreground'
    if (score >= 0.8) return 'bg-success/10 text-success'
    if (score >= 0.6) return 'bg-info/10 text-info'
    if (score >= 0.4) return 'bg-warning/10 text-warning'
    return 'bg-destructive/10 text-destructive'
  }

  if (!isAuthenticated) {
    return (
      <div className="bg-card rounded-lg shadow p-6 text-center">
        <svg className="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
        </svg>
        <h3 className="mt-2 text-sm font-medium text-foreground">Authentication Required</h3>
        <p className="mt-1 text-sm text-muted-foreground">Please log in to see personalized project recommendations.</p>
        <button
          onClick={() => router.push('/login')}
          className="mt-4 bg-primary text-primary-foreground px-4 py-2 rounded text-sm hover:bg-primary/90"
        >
          Sign In
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-xl font-semibold text-foreground">Recommended for You</h2>
          <p className="text-sm text-muted-foreground mt-1">
            Projects that match your skills and preferences
          </p>
        </div>
        <button
          onClick={refreshRecommendations}
          disabled={refreshing}
          className="flex items-center text-primary hover:text-primary/80 text-sm font-medium disabled:opacity-50"
        >
          <svg className={`w-4 h-4 mr-1 ${refreshing ? 'animate-spin' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          {refreshing ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>

      {error && (
        <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-destructive">{error}</p>
              <button
                onClick={loadRecommendations}
                className="mt-2 text-sm text-destructive hover:text-destructive/80 underline"
              >
                Try again
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: Math.min(limit, 6) }).map((_, i) => (
            <div key={i} className="bg-card rounded-lg shadow p-6 animate-pulse">
              <div className="h-4 bg-muted rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-muted rounded w-full mb-2"></div>
              <div className="h-3 bg-muted rounded w-2/3 mb-4"></div>
              <div className="space-y-2">
                <div className="h-3 bg-muted rounded w-1/2"></div>
                <div className="h-3 bg-muted rounded w-1/3"></div>
              </div>
            </div>
          ))}
        </div>
      ) : recommendations.length === 0 ? (
        <div className="bg-card rounded-lg shadow p-8 text-center">
          <svg className="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-foreground">No recommendations available</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Complete your profile and add skills to get personalized project recommendations.
          </p>
          <button
            onClick={() => router.push('/profile/me')}
            className="mt-4 bg-primary text-primary-foreground px-4 py-2 rounded text-sm hover:bg-primary/90"
          >
            Update Profile
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {recommendations.map((project) => (
            <div
              key={project.id}
              className="bg-card rounded-lg shadow hover:shadow-md transition-shadow cursor-pointer"
              onClick={() => handleProjectClick(project.id)}
            >
              <div className="p-6">
                {/* Header with match score */}
                <div className="flex justify-between items-start mb-3">
                  <div className="flex-1">
                    <div className="flex items-center space-x-2 mb-2">
                      <h3 className="text-lg font-semibold text-foreground line-clamp-2 hover:text-primary">
                        {project.title}
                      </h3>
                      {project.isUrgent && (
                        <span className="bg-destructive/10 text-destructive text-xs font-medium px-2 py-1 rounded">
                          Urgent
                        </span>
                      )}
                      {project.isFeatured && (
                        <span className="bg-warning/10 text-warning text-xs font-medium px-2 py-1 rounded">
                          Featured
                        </span>
                      )}
                    </div>
                  </div>
                  
                  {project.matchScore && (
                    <div className={`text-xs font-medium px-2 py-1 rounded ${getMatchScoreColor(project.matchScore)}`}>
                      {Math.round(project.matchScore * 100)}% match
                    </div>
                  )}
                </div>

                {/* Description */}
                <p className="text-muted-foreground text-sm mb-4 line-clamp-3">
                  {project.description}
                </p>

                {/* Match reasons */}
                {showMatchReasons && project.matchReasons && project.matchReasons.length > 0 && (
                  <div className="mb-4">
                    <p className="text-xs font-medium text-foreground mb-2">Why this matches:</p>
                    <div className="space-y-1">
                      {project.matchReasons.slice(0, 3).map((reason, index) => (
                        <div key={index} className="flex items-center text-xs text-muted-foreground">
                          {getMatchReasonIcon(reason)}
                          <span className="ml-2">{reason}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Project details */}
                <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground mb-4">
                  <div className="flex items-center">
                    <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1" />
                    </svg>
                    {project.creditBudget} credits
                  </div>
                  
                  {project.location && (
                    <div className="flex items-center">
                      <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                      </svg>
                      {[project.location.city, project.location.state]
                        .filter(Boolean).join(', ')}
                    </div>
                  )}
                  
                  {project.endDate && (
                    <div className="flex items-center">
                      <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      {new Date(project.endDate).toLocaleDateString()}
                    </div>
                  )}
                </div>

                {/* Skills */}
                {project.skills.length > 0 && (
                  <div className="mb-4">
                    <div className="flex flex-wrap gap-2">
                      {project.skills.slice(0, 4).map((skill) => (
                        <span key={skill.skillId}
                              className="bg-info/10 text-info text-xs font-medium px-2 py-1 rounded">
                          {skill.skillName}
                          {skill.proficiencyRequired > 1 && (
                            <span className="ml-1 text-info">
                              {'★'.repeat(skill.proficiencyRequired)}
                            </span>
                          )}
                        </span>
                      ))}
                      {project.skills.length > 4 && (
                        <span className="bg-muted text-muted-foreground text-xs font-medium px-2 py-1 rounded">
                          +{project.skills.length - 4} more
                        </span>
                      )}
                    </div>
                  </div>
                )}

                {/* Footer */}
                <div className="flex justify-between items-center text-xs text-muted-foreground border-t border-border pt-3">
                  <div>
                    Posted {new Date(project.createdAt).toLocaleDateString()}
                  </div>
                  <div>
                    by {project.client.userName}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* View all recommendations link */}
      {recommendations.length > 0 && (
        <div className="text-center">
          <button
            onClick={() => router.push('/projects/search')}
            className="text-primary hover:text-primary/80 text-sm font-medium"
          >
            View all recommended projects →
          </button>
        </div>
      )}
    </div>
  )
}

export default ProjectRecommendations