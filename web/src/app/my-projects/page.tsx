'use client'

import { logger } from '@/utils/logger'
import { useEffect, useState } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import Link from 'next/link'
import { Briefcase, Clock, CheckCircle, Users, DollarSign, Plus, ExternalLink, Edit, AlertCircle } from 'lucide-react'
import LogoutButton from '@/components/LogoutButton'
import { ThemeToggle } from '@/components/ThemeToggle'

interface Project {
  id: string
  title: string
  description: string
  status: string
  creditBudget: number
  startDate?: string
  endDate?: string
  createdAt: string
  applicationCount?: number
  isUrgent?: boolean
  isFeatured?: boolean
}

// BUG-009: My Projects page implementation
export default function MyProjectsPage() {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('all')
  const [currentPage, setCurrentPage] = useState(0)
  const [hasMore, setHasMore] = useState(true)
  const pageSize = 10

  const fetchProjects = async () => {
    try {
      setLoading(true)
      setError(null)

      const params = new URLSearchParams({
        skip: (currentPage * pageSize).toString(),
        take: pageSize.toString(),
        includeNonPublic: 'true',
      })

      const response = await fetch(`/api/project/my-projects?${params}`, {
        credentials: 'include',
      })

      if (response.ok) {
        const data: Project[] = await response.json()
        setProjects(data || [])
        setHasMore(data.length === pageSize)
      } else if (response.status === 401) {
        setError('Please log in to view your projects')
      } else {
        setError('Failed to load projects')
      }
    } catch (err) {
      logger.error('Failed to fetch projects:', err)
      setError('An error occurred while loading your projects')
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
      fetchProjects()
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, authLoading, user, currentPage])

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active':
      case 'published':
        return <CheckCircle className="w-5 h-5 text-success" />
      case 'draft':
        return <Edit className="w-5 h-5 text-muted-foreground" />
      case 'completed':
        return <CheckCircle className="w-5 h-5 text-primary" />
      case 'cancelled':
        return <AlertCircle className="w-5 h-5 text-destructive" />
      default:
        return <Clock className="w-5 h-5 text-warning" />
    }
  }

  const getStatusClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active':
      case 'published':
        return 'bg-success/10 text-success'
      case 'draft':
        return 'bg-muted text-muted-foreground'
      case 'completed':
        return 'bg-primary/10 text-primary'
      case 'cancelled':
        return 'bg-destructive/10 text-destructive'
      default:
        return 'bg-warning/10 text-warning'
    }
  }

  // Filter projects by status
  const filteredProjects = statusFilter === 'all'
    ? projects
    : projects.filter(p => p.status.toLowerCase() === statusFilter.toLowerCase())

  if (authLoading || loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading your projects...</p>
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
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div className="space-golden-sm">
              <h1 className="text-display gradient-text">My Projects</h1>
              <p className="text-heading text-muted-foreground leading-relaxed">
                Manage your posted projects and track applications
              </p>
            </div>
            <Link href="/create-project" className="btn-primary inline-flex items-center gap-2 self-start">
              <Plus className="w-5 h-5" />
              Create Project
            </Link>
          </div>

          {/* Filters */}
          <div className="card p-4 flex flex-wrap items-center gap-4">
            <div className="flex items-center gap-2">
              <Briefcase className="w-5 h-5 text-muted-foreground" />
              <span className="text-sm font-medium text-foreground">Filter by status:</span>
            </div>
            <div className="flex flex-wrap gap-2">
              {['all', 'Active', 'Draft', 'Completed', 'Cancelled'].map((status) => (
                <button
                  key={status}
                  onClick={() => setStatusFilter(status)}
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
              <div className="flex items-center gap-3">
                <AlertCircle className="w-6 h-6 text-destructive" />
                <p className="text-destructive">{error}</p>
              </div>
            </div>
          )}

          {/* Projects List */}
          {!error && filteredProjects.length === 0 ? (
            <div className="card p-12 text-center">
              <div className="w-16 h-16 bg-muted/50 rounded-full flex items-center justify-center mx-auto mb-4">
                <Briefcase className="w-8 h-8 text-muted-foreground" />
              </div>
              <h2 className="text-title text-foreground mb-2">No projects found</h2>
              <p className="text-muted-foreground mb-6">
                {statusFilter !== 'all'
                  ? `You don't have any ${statusFilter.toLowerCase()} projects.`
                  : "You haven't created any projects yet."}
              </p>
              <Link href="/create-project" className="btn-primary inline-flex items-center gap-2">
                <Plus className="w-5 h-5" />
                Create Your First Project
              </Link>
            </div>
          ) : (
            <div className="space-y-4">
              {filteredProjects.map((project) => (
                <div
                  key={project.id}
                  className="card p-6 hover:border-primary/30 transition-colors"
                >
                  <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
                    <div className="flex-1">
                      <div className="flex items-start gap-3 mb-3">
                        {getStatusIcon(project.status)}
                        <div>
                          <div className="flex items-center gap-2 mb-1">
                            <h3 className="text-subheading font-semibold text-foreground">
                              {project.title}
                            </h3>
                            {project.isUrgent && (
                              <span className="px-2 py-0.5 text-xs font-medium bg-destructive/10 text-destructive rounded">
                                Urgent
                              </span>
                            )}
                            {project.isFeatured && (
                              <span className="px-2 py-0.5 text-xs font-medium bg-warning/10 text-warning rounded">
                                Featured
                              </span>
                            )}
                          </div>
                          <p className="text-sm text-muted-foreground line-clamp-2">
                            {project.description}
                          </p>
                        </div>
                      </div>

                      <div className="flex flex-wrap gap-4 text-sm text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <DollarSign className="w-4 h-4" />
                          {project.creditBudget} credits
                        </span>
                        {project.applicationCount !== undefined && (
                          <span className="flex items-center gap-1">
                            <Users className="w-4 h-4" />
                            {project.applicationCount} applications
                          </span>
                        )}
                        <span className="flex items-center gap-1">
                          <Clock className="w-4 h-4" />
                          Created {new Date(project.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                    </div>

                    <div className="flex flex-col items-end gap-3">
                      <span className={`px-3 py-1 rounded-full text-sm font-medium ${getStatusClass(project.status)}`}>
                        {project.status}
                      </span>
                      <div className="flex gap-2">
                        <Link
                          href={`/projects/${project.id}/applications`}
                          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
                        >
                          <Users className="w-4 h-4" />
                          View Applications
                        </Link>
                        <Link
                          href={`/projects/${project.id}`}
                          className="flex items-center gap-1 text-sm text-primary hover:underline"
                        >
                          View Details <ExternalLink className="w-4 h-4" />
                        </Link>
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {/* Pagination */}
              <div className="flex justify-center gap-2 pt-4">
                <button
                  onClick={() => setCurrentPage((p) => Math.max(0, p - 1))}
                  disabled={currentPage === 0}
                  className="btn-secondary px-4 py-2 disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="flex items-center px-4 text-sm text-muted-foreground">
                  Page {currentPage + 1}
                </span>
                <button
                  onClick={() => setCurrentPage((p) => p + 1)}
                  disabled={!hasMore}
                  className="btn-secondary px-4 py-2 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}
