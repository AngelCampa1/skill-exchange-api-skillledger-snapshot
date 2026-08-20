'use client'

import { logger } from '@/utils/logger'
import { useEffect, useState } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import Link from 'next/link'
import { FileText, Clock, CheckCircle, XCircle, AlertCircle, ExternalLink, Filter } from 'lucide-react'
import LogoutButton from '@/components/LogoutButton'
import { ThemeToggle } from '@/components/ThemeToggle'

interface Application {
  id: string
  projectId: string
  projectTitle: string
  status: string
  proposedBudget: number
  estimatedDuration: string
  coverLetter: string
  createdAt: string
  updatedAt: string
  clientName?: string
}

interface ApplicationSearchResult {
  applications: Application[]
  totalCount: number
  pageSize: number
  currentPage: number
  totalPages: number
}

// BUG-008: My Applications page implementation
export default function MyApplicationsPage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()
  const [applications, setApplications] = useState<Application[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const pageSize = 10

  const fetchApplications = async () => {
    try {
      setLoading(true)
      setError(null)

      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: pageSize.toString(),
      })

      if (statusFilter !== 'all') {
        params.append('status', statusFilter)
      }

      const response = await fetch(`/api/project-applications/my-applications?${params}`, {
        credentials: 'include',
      })

      if (response.ok) {
        const data: ApplicationSearchResult = await response.json()
        setApplications(data.applications || [])
        setTotalPages(data.totalPages || 1)
      } else if (response.status === 401) {
        setError('Please log in to view your applications')
      } else {
        setError('Failed to load applications')
      }
    } catch (err) {
      logger.error('Failed to fetch applications:', err)
      setError('An error occurred while loading your applications')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
        .finally(() => {
          window.location.href = '/login'
        })
      return
    }

    if (isAuthenticated && user) {
      fetchApplications()
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, authLoading, user, statusFilter, currentPage])

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'accepted':
        return <CheckCircle className="w-5 h-5 text-success" />
      case 'rejected':
        return <XCircle className="w-5 h-5 text-destructive" />
      case 'pending':
        return <Clock className="w-5 h-5 text-warning" />
      case 'withdrawn':
        return <AlertCircle className="w-5 h-5 text-muted-foreground" />
      default:
        return <FileText className="w-5 h-5 text-muted-foreground" />
    }
  }

  const getStatusClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'accepted':
        return 'bg-success/10 text-success'
      case 'rejected':
        return 'bg-destructive/10 text-destructive'
      case 'pending':
        return 'bg-warning/10 text-warning'
      case 'withdrawn':
        return 'bg-muted text-muted-foreground'
      default:
        return 'bg-muted text-muted-foreground'
    }
  }

  if (authLoading || loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading your applications...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Navigation */}
      <nav className="nav-blur border-b border-border/50 sticky top-0 z-50 backdrop-blur-xl bg-background/80">
        <div className="container-responsive py-4">
          <div className="flex items-center justify-between">
            <Link href="/" className="text-title font-bold text-foreground hover:text-primary transition-colors">
              SkillLedger
            </Link>
            <Link href="/" className="text-body text-foreground/70 hover:text-foreground transition-colors px-4">
              Dashboard
            </Link>
            <div className="flex items-center gap-4">
              <ThemeToggle />
              {user && (
                <div className="flex items-center gap-4">
                  <span className="text-sm text-muted-foreground hidden md:inline">
                    Welcome back, <span className="text-foreground font-medium">{user.firstName || user.email}</span>
                  </span>
                  <LogoutButton />
                </div>
              )}
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container-responsive py-12">
        <div className="max-w-6xl mx-auto space-golden-lg">
          {/* Header */}
          <div className="space-golden-sm">
            <h1 className="text-display gradient-text">My Applications</h1>
            <p className="text-heading text-muted-foreground leading-relaxed">
              Track the status of your project applications
            </p>
          </div>

          {/* Filters */}
          <div className="card p-4 flex flex-wrap items-center gap-4">
            <div className="flex items-center gap-2">
              <Filter className="w-5 h-5 text-muted-foreground" />
              <span className="text-sm font-medium text-foreground">Filter by status:</span>
            </div>
            <div className="flex flex-wrap gap-2">
              {['all', 'Pending', 'Accepted', 'Rejected', 'Withdrawn'].map((status) => (
                <button
                  key={status}
                  onClick={() => {
                    setStatusFilter(status)
                    setCurrentPage(1)
                  }}
                  className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                    statusFilter === status
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted text-muted-foreground hover:bg-muted/80'
                  }`}
                >
                  {status === 'all' ? 'All' : status}
                </button>
              ))}
            </div>
          </div>

          {/* Error State */}
          {error && (
            <div className="card p-6 bg-destructive/10 border-destructive/50">
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <AlertCircle className="w-6 h-6 text-destructive" />
                  <p className="text-destructive">{error}</p>
                </div>
                <button
                  onClick={() => fetchApplications()}
                  className="px-4 py-2 text-sm font-medium bg-destructive/20 hover:bg-destructive/30 text-destructive rounded-lg transition-colors"
                >
                  Try Again
                </button>
              </div>
            </div>
          )}

          {/* Applications List */}
          {!error && applications.length === 0 ? (
            <div className="card p-12 text-center">
              <div className="w-16 h-16 bg-muted/50 rounded-full flex items-center justify-center mx-auto mb-4">
                <FileText className="w-8 h-8 text-muted-foreground" />
              </div>
              <h2 className="text-title text-foreground mb-2">No applications found</h2>
              <p className="text-muted-foreground mb-6">
                {statusFilter !== 'all'
                  ? `You don't have any ${statusFilter.toLowerCase()} applications.`
                  : "You haven't applied to any projects yet."}
              </p>
              <Link href="/projects/search" className="btn-primary inline-block">
                Browse Projects
              </Link>
            </div>
          ) : (
            <div className="space-y-4">
              {applications.map((app) => (
                <div
                  key={app.id}
                  className="card p-6 hover:border-primary/30 transition-colors"
                >
                  <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
                    <div className="flex-1">
                      <div className="flex items-start gap-3 mb-3">
                        {getStatusIcon(app.status)}
                        <div>
                          <h3 className="text-subheading font-semibold text-foreground">
                            {app.projectTitle}
                          </h3>
                          {app.clientName && (
                            <p className="text-sm text-muted-foreground">Client: {app.clientName}</p>
                          )}
                        </div>
                      </div>

                      <div className="flex flex-wrap gap-4 text-sm text-muted-foreground mb-3">
                        <span>Proposed: {app.proposedBudget} credits</span>
                        <span>Duration: {app.estimatedDuration}</span>
                        <span>Applied: {new Date(app.createdAt).toLocaleDateString()}</span>
                      </div>

                      {app.coverLetter && (
                        <p className="text-sm text-muted-foreground line-clamp-2">
                          {app.coverLetter}
                        </p>
                      )}
                    </div>

                    <div className="flex flex-col items-end gap-3">
                      <span className={`px-3 py-1 rounded-full text-sm font-medium ${getStatusClass(app.status)}`}>
                        {app.status}
                      </span>
                      <Link
                        href={`/projects/${app.projectId}`}
                        className="flex items-center gap-1 text-sm text-primary hover:underline"
                      >
                        View Project <ExternalLink className="w-4 h-4" />
                      </Link>
                    </div>
                  </div>
                </div>
              ))}

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex justify-center gap-2 pt-4">
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={currentPage === 1}
                    className="btn-secondary px-4 py-2 disabled:opacity-50"
                  >
                    Previous
                  </button>
                  <span className="flex items-center px-4 text-sm text-muted-foreground">
                    Page {currentPage} of {totalPages}
                  </span>
                  <button
                    onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                    disabled={currentPage === totalPages}
                    className="btn-secondary px-4 py-2 disabled:opacity-50"
                  >
                    Next
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      </main>
    </div>
  )
}
