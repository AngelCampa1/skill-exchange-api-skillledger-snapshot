'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react'
import { useRouter, useParams } from 'next/navigation'
import Link from 'next/link'
import { ArrowLeft, User, Calendar, DollarSign, Clock, CheckCircle, XCircle, Mail } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'

interface Application {
  id: string
  providerId: string
  providerName: string
  providerEmail: string
  coverLetter: string
  proposedTimeline?: number
  proposedBudget?: number
  availabilityDetails?: string
  isAvailableImmediately: boolean
  status: string
  submittedAt: string
  skillMatchScore?: number
}

export default function ProjectApplicationsPage() {
  const router = useRouter()
  const params = useParams()
  const projectId = params?.id as string
  const { user, isAuthenticated } = useAuth()

  const [applications, setApplications] = useState<Application[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedApplication, setSelectedApplication] = useState<Application | null>(null)
  const [isSelecting, setIsSelecting] = useState(false)

  useEffect(() => {
    const loadApplications = async () => {
      if (!projectId || !isAuthenticated) return

      try {
        setIsLoading(true)
        const response = await fetch(`/api/project-applications/project/${projectId}`, {
          credentials: 'include',
        })

        if (response.ok) {
          const data = await response.json()
          setApplications(data.items || data.applications || data || [])
        } else if (response.status === 403) {
          setError('You do not have permission to view these applications')
        } else {
          setError('Failed to load applications')
        }
      } catch (err) {
        logger.error('Error loading applications:', err)
        setError('An error occurred while loading applications')
      } finally {
        setIsLoading(false)
      }
    }

    loadApplications()
  }, [projectId, isAuthenticated])

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
      logger.error('Failed to get CSRF token:', error)
    }
    
    return null
  }

  const handleSelectProvider = async (application: Application) => {
    if (!confirm(`Are you sure you want to select ${application.providerName} for this project? This will create a workspace and lock escrow funds.`)) {
      return
    }

    setIsSelecting(true)

    try {
      const csrfToken = await getCsrfToken()
      if (!csrfToken) {
        throw new Error('Failed to get CSRF token')
      }

      const response = await fetch(`/api/project-applications/${application.id}/status`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include',
        body: JSON.stringify({
          status: 'Accepted',
          notes: 'Provider selected for project'
        }),
      })

      if (response.ok) {
        alert(`Success! ${application.providerName} has been selected. A workspace has been created and you can now collaborate.`)
        router.push(`/projects/${projectId}`)
      } else {
        const result = await response.json()
        alert(`Failed to select provider: ${result.message || 'Unknown error'}`)
      }
    } catch (error: any) {
      logger.error('Error selecting provider:', error)
      alert(`Error: ${error.message}`)
    } finally {
      setIsSelecting(false)
    }
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <p className="text-lg text-muted-foreground mb-4">You must be logged in to view applications</p>
          <Link href="/login" className="btn-primary">
            Login
          </Link>
        </div>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading applications...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background px-6">
        <div className="text-center space-y-6 max-w-md">
          <XCircle className="w-16 h-16 text-destructive mx-auto" />
          <h1 className="text-2xl font-bold text-foreground">{error}</h1>
          <Link href={`/projects/${projectId}`} className="btn-primary inline-block">
            Back to Project
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
          Back to Project
        </button>

        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-foreground mb-2">Project Applications</h1>
          <p className="text-muted-foreground">Review and select the best provider for your project</p>
          <div className="mt-4 flex items-center space-x-4">
            <div className="bg-card px-4 py-2 rounded-lg shadow">
              <span className="text-sm text-muted-foreground">Total Applications:</span>
              <span className="ml-2 font-bold text-foreground">{applications.length}</span>
            </div>
          </div>
        </div>

        {/* Applications List */}
        {applications.length === 0 ? (
          <div className="bg-card rounded-lg shadow p-12 text-center">
            <Mail className="w-16 h-16 text-muted-foreground mx-auto mb-4" />
            <h2 className="text-xl font-bold text-foreground mb-2">No Applications Yet</h2>
            <p className="text-muted-foreground mb-6">
              Your project is live! Providers will start applying soon.
            </p>
            <Link href={`/projects/${projectId}`} className="btn-primary inline-block">
              View Project
            </Link>
          </div>
        ) : (
          <div className="space-y-6">
            {applications.map((application) => (
              <div key={application.id} className="bg-card rounded-lg shadow-lg p-6 hover:shadow-xl transition-shadow">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex items-center space-x-4">
                    <div className="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center">
                      <User className="w-8 h-8 text-primary" />
                    </div>
                    <div>
                      <h3 className="text-xl font-bold text-foreground">{application.providerName}</h3>
                      <p className="text-sm text-muted-foreground">{application.providerEmail}</p>
                      <div className="flex items-center space-x-3 mt-2">
                        <span className={`px-2 py-1 rounded text-xs font-medium ${
                          application.status === 'Pending' ? 'bg-warning/10 text-warning' :
                          application.status === 'Accepted' ? 'bg-success/10 text-success' :
                          'bg-muted text-muted-foreground'
                        }`}>
                          {application.status}
                        </span>
                        {application.isAvailableImmediately && (
                          <span className="bg-success/10 text-success px-2 py-1 rounded text-xs font-medium">
                            ⚡ Available Immediately
                          </span>
                        )}
                        {application.skillMatchScore && (
                          <span className="bg-primary/10 text-primary px-2 py-1 rounded text-xs font-medium">
                            {Math.round(application.skillMatchScore * 100)}% Skill Match
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="text-right text-sm text-muted-foreground">
                    <div>Applied {new Date(application.submittedAt).toLocaleDateString()}</div>
                  </div>
                </div>

                {/* Proposal Details */}
                <div className="mb-4 space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {application.proposedTimeline && (
                      <div className="flex items-center text-foreground">
                        <Clock className="w-5 h-5 mr-2 text-muted-foreground" />
                        <div>
                          <div className="text-sm text-muted-foreground">Timeline</div>
                          <div className="font-medium">{application.proposedTimeline} days</div>
                        </div>
                      </div>
                    )}
                    {application.proposedBudget && (
                      <div className="flex items-center text-foreground">
                        <DollarSign className="w-5 h-5 mr-2 text-muted-foreground" />
                        <div>
                          <div className="text-sm text-muted-foreground">Budget</div>
                          <div className="font-medium">{application.proposedBudget} credits</div>
                        </div>
                      </div>
                    )}
                  </div>

                  <div>
                    <h4 className="font-medium text-foreground mb-2">Cover Letter</h4>
                    <p className="text-muted-foreground whitespace-pre-wrap leading-relaxed">{application.coverLetter}</p>
                  </div>

                  {application.availabilityDetails && (
                    <div>
                      <h4 className="font-medium text-foreground mb-2">Availability</h4>
                      <p className="text-muted-foreground">{application.availabilityDetails}</p>
                    </div>
                  )}
                </div>

                {/* Action Buttons */}
                {application.status === 'Pending' && (
                  <div className="flex justify-end space-x-4 pt-4 border-t border-border">
                    <button
                      onClick={() => handleSelectProvider(application)}
                      disabled={isSelecting}
                      className="px-6 py-2 bg-success hover:bg-success/90 text-success-foreground rounded-lg font-medium flex items-center space-x-2 disabled:opacity-50"
                      data-testid="select-provider-button"
                    >
                      <CheckCircle className="w-4 h-4" />
                      <span>Select Provider</span>
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}



