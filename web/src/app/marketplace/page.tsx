'use client'

import { logger } from '@/utils/logger';
import React, { useState, useEffect, useCallback, useMemo, Suspense } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { Search, Filter, Clock, DollarSign, MapPin, Briefcase, Star, ChevronDown, X, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { ThemeToggle } from '@/components/ThemeToggle'

interface Project {
  id: string
  title: string
  description?: string
  shortDescription?: string
  creditBudget: number
  clientName?: string
  clientCompany?: string
  location?: string
  client?: {
    id: string
    displayName: string
    email?: string
    userName?: string
    firstName?: string
    lastName?: string
  }
  requiredSkills?: Array<{
    name: string
    proficiency: number
  }>
  requiredSkillNames?: string[]
  status: string
  createdAt: string
  deadline?: string
  endDate?: string
  applicationsCount?: number
  clientRating?: number
}

interface MarketplaceFilters {
  search: string
  skills: string[]
  budgetMin: number
  budgetMax: number
  location: string
  availability: 'all' | 'remote' | 'onsite' | 'hybrid'
  sortBy: string  // Backend field name: 'CreatedAt' | 'Budget' | 'EndDate' | 'title'
  sortDirection: 'asc' | 'desc'
}

// Sort option mapping for dropdown
// MARKETPLACE SORT FIX: Use lowercase sortBy values to match backend switch statement exactly
const SORT_OPTIONS = [
  { value: 'newest', label: 'Newest First', sortBy: 'createdat', sortDirection: 'desc' as const },
  { value: 'budget_high', label: 'Highest Budget', sortBy: 'budget', sortDirection: 'desc' as const },
  { value: 'budget_low', label: 'Lowest Budget', sortBy: 'budget', sortDirection: 'asc' as const },
  { value: 'deadline', label: 'Deadline Soon', sortBy: 'enddate', sortDirection: 'asc' as const },
]

const SKILL_OPTIONS = [
  'React', 'Node.js', 'TypeScript', 'Python', 'JavaScript', 
  'Java', 'C#', 'PHP', 'Ruby', 'Go', 'Swift', 'Kotlin',
  'AWS', 'Azure', 'Google Cloud', 'Docker', 'Kubernetes',
  'PostgreSQL', 'MongoDB', 'MySQL', 'Redis', 'Elasticsearch',
  'UI/UX Design', 'Graphic Design', 'Product Design'
]

const BUDGET_RANGES = [
  { label: 'Under 500 credits', value: { min: 0, max: 500 } },
  { label: '500 - 1000 credits', value: { min: 500, max: 1000 } },
  { label: '1000 - 2500 credits', value: { min: 1000, max: 2500 } },
  { label: '2500 - 5000 credits', value: { min: 2500, max: 5000 } },
  { label: 'Over 5000 credits', value: { min: 5000, max: 10000 } }
]

function MarketplacePageContent() {
  const searchParams = useSearchParams()
  // BUG-MED-007 FIX: Use Next.js router for internal navigation
  const router = useRouter()
  const [projects, setProjects] = useState<Project[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showFilters, setShowFilters] = useState(false)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  
  const [filters, setFilters] = useState<MarketplaceFilters>({
    search: searchParams.get('search') || '',
    skills: [],
    budgetMin: 0,
    budgetMax: 5000,
    location: '',
    availability: 'all',
    sortBy: 'createdat',  // MARKETPLACE SORT FIX: Use lowercase to match backend
    sortDirection: 'desc'
  })

  // Note: Mock data removed - marketplace now uses real API data only
  // If no projects are available, users will see "No projects found" message

  const loadProjects = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      // Build query params matching backend ProjectSearchDto
      // Backend uses skip/take pagination, not page numbers
      const pageSize = 12
      const skip = (currentPage - 1) * pageSize

      const queryParams = new URLSearchParams({
        query: filters.search,
        skillNames: filters.skills.join(','),  // Backend will convert names to IDs
        sortBy: filters.sortBy,
        sortDirection: filters.sortDirection,
        skip: skip.toString(),
        take: pageSize.toString()
      })
      // Only send budget params if they differ from defaults (0 and 5000)
      if (filters.budgetMin > 0) {
        queryParams.set('minBudget', filters.budgetMin.toString())
      }
      if (filters.budgetMax < 5000) {
        queryParams.set('maxBudget', filters.budgetMax.toString())
      }

      const response = await fetch(`/api/project/marketplace?${queryParams}`, {
        credentials: 'include',
      })

      if (response.ok) {
        const data = await response.json()
        // Handle both array format (from /api/project/marketplace) and object format (from other endpoints)
        const projectsArray = Array.isArray(data) ? data : (data.projects || [])
        setProjects(projectsArray)
        setTotalPages(data.totalPages || Math.ceil(projectsArray.length / 12) || 1)
      } else {
        // API returned an error - show error message and empty state
        logger.warn('Marketplace API error', { status: response.status })
        setError('Unable to load projects. Please try again later.')
        setProjects([])
        setTotalPages(1)
      }
    } catch (err) {
      // Network error - show error message and empty state
      logger.error('Marketplace network error', { error: err })
      setError('Unable to connect to the server. Please check your connection and try again.')
      setProjects([])
      setTotalPages(1)
    } finally {
      setLoading(false)
    }
  }, [filters, currentPage])

  useEffect(() => {
    loadProjects()
  }, [loadProjects])

  const handleFilterChange = (key: keyof MarketplaceFilters, value: any) => {
    setFilters(prev => ({ ...prev, [key]: value }))
    setCurrentPage(1)
  }

  const clearFilters = () => {
    setFilters({
      search: '',
      skills: [],
      budgetMin: 0,
      budgetMax: 5000,
      location: '',
      availability: 'all',
      sortBy: 'createdat',  // MARKETPLACE SORT FIX: Use lowercase to match backend
      sortDirection: 'desc'
    })
    setCurrentPage(1)
  }

  const getProficiencyLabel = (level: number) => {
    const labels = ['', 'Beginner', 'Novice', 'Intermediate', 'Advanced', 'Expert']
    return labels[level] || 'Unknown'
  }

  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric', 
      year: 'numeric' 
    })
  }

  const getDaysUntilDeadline = (deadline: string) => {
    const days = Math.ceil((new Date(deadline).getTime() - new Date().getTime()) / (1000 * 60 * 60 * 24))
    if (days < 0) return 'Expired'
    if (days === 0) return 'Today'
    if (days === 1) return 'Tomorrow'
    return `${days} days`
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container-premium py-8">
        {/* BUG-008 FIX: Navigation Header with theme toggle */}
        <div className="flex items-center justify-between mb-8">
          <Link
            href="/"
            className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
          >
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Home
          </Link>
          <ThemeToggle />
        </div>

        {/* Header */}
        <div className="mb-8">
          <h1 className="text-display text-foreground mb-2">Project Marketplace</h1>
          <p className="text-muted-foreground">Find exciting projects that match your skills and expertise</p>
        </div>

        {/* Search and Filters */}
        <div className="card-premium p-6 mb-6">
          <div className="flex flex-col lg:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
                <Input
                  type="text"
                  placeholder="Search projects by title, description, or skills..."
                  value={filters.search}
                  onChange={(e) => handleFilterChange('search', e.target.value)}
                  data-testid="marketplace-search-input"
                  className="pl-10"
                />
              </div>
            </div>
            
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setShowFilters(!showFilters)}
                data-testid="toggle-filters-button"
              >
                <Filter className="h-4 w-4 mr-2" />
                Filters
                {(filters.skills.length > 0 || filters.location || filters.budgetMin > 0 || filters.budgetMax < 5000) && (
                  <span className="ml-2 bg-primary/10 text-primary text-xs px-2 py-1 rounded-full">
                    Active
                  </span>
                )}
              </Button>

              <select
                value={SORT_OPTIONS.find(opt => opt.sortBy === filters.sortBy && opt.sortDirection === filters.sortDirection)?.value || 'newest'}
                onChange={(e) => {
                  const selectedOption = SORT_OPTIONS.find(opt => opt.value === e.target.value)
                  if (selectedOption) {
                    setFilters(prev => ({
                      ...prev,
                      sortBy: selectedOption.sortBy,
                      sortDirection: selectedOption.sortDirection
                    }))
                    setCurrentPage(1)
                  }
                }}
                data-testid="sort-select"
                className="rounded-md border-border bg-card text-foreground shadow-sm"
              >
                {SORT_OPTIONS.map(option => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Advanced Filters */}
          {showFilters && (
            <div className="mt-6 pt-6 border-t border-border">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {/* Skills Filter */}
                <div>
                  <Label className="block text-sm font-medium text-foreground mb-2">Skills</Label>
                  <div className="space-y-2 max-h-32 overflow-y-auto">
                    {SKILL_OPTIONS.map(skill => (
                      <label key={skill} className="flex items-center">
                        <input
                          type="checkbox"
                          checked={filters.skills.includes(skill)}
                          onChange={(e) => {
                            if (e.target.checked) {
                              handleFilterChange('skills', [...filters.skills, skill])
                            } else {
                              handleFilterChange('skills', filters.skills.filter(s => s !== skill))
                            }
                          }}
                          data-testid={`skill-filter-${skill}`}
                          className="mr-2"
                        />
                        <span className="text-sm text-foreground">{skill}</span>
                      </label>
                    ))}
                  </div>
                </div>

                {/* Budget Filter */}
                <div>
                  <Label className="block text-sm font-medium text-foreground mb-2">Budget Range</Label>
                  <div className="space-y-2">
                    <div className="flex gap-2">
                      <Input
                        type="number"
                        placeholder="Min"
                        value={filters.budgetMin}
                        onChange={(e) => handleFilterChange('budgetMin', parseInt(e.target.value) || 0)}
                        data-testid="budget-min-input"
                      />
                      <Input
                        type="number"
                        placeholder="Max"
                        value={filters.budgetMax}
                        onChange={(e) => handleFilterChange('budgetMax', parseInt(e.target.value) || 5000)}
                        data-testid="budget-max-input"
                      />
                    </div>
                    <div className="flex flex-wrap gap-x-2 gap-y-1 mt-1">
                      {BUDGET_RANGES.map(range => (
                        <button
                          key={range.label}
                          type="button"
                          onClick={() => {
                            handleFilterChange('budgetMin', range.value.min)
                            handleFilterChange('budgetMax', range.value.max)
                          }}
                          className="text-xs text-primary hover:text-primary/80 underline"
                        >
                          {range.label}
                        </button>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Location Filter */}
                <div>
                  <Label className="block text-sm font-medium text-foreground mb-2">Location</Label>
                  <Input
                    type="text"
                    placeholder="City, State, or 'Remote'"
                    value={filters.location}
                    onChange={(e) => handleFilterChange('location', e.target.value)}
                    data-testid="location-filter-input"
                  />
                  <div className="mt-2 space-y-1">
                    {['Remote', 'Onsite', 'Hybrid'].map(location => (
                      <button
                        key={location}
                        type="button"
                        onClick={() => handleFilterChange('location', location)}
                        className="text-xs text-primary hover:text-primary/80"
                      >
                        {location}
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              <div className="mt-4 flex justify-end">
                <Button
                  variant="outline"
                  onClick={clearFilters}
                  data-testid="clear-filters-button"
                >
                  <X className="h-4 w-4 mr-2" />
                  Clear Filters
                </Button>
              </div>
            </div>
          )}
        </div>

        {/* Error State */}
        {error && (
          <Alert className="mb-6 border-destructive/20 bg-destructive/10">
            <AlertDescription className="text-destructive">
              {error}
            </AlertDescription>
          </Alert>
        )}

        {/* Loading State */}
        {loading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[1, 2, 3, 4, 5, 6].map(i => (
              <div key={i} className="card-premium p-6 animate-pulse">
                <div className="h-4 bg-muted rounded w-3/4 mb-2"></div>
                <div className="h-3 bg-muted rounded w-1/2 mb-4"></div>
                <div className="h-3 bg-muted rounded w-full mb-2"></div>
                <div className="h-3 bg-muted rounded w-full mb-4"></div>
                <div className="flex justify-between items-center">
                  <div className="h-3 bg-muted rounded w-1/4"></div>
                  <div className="h-3 bg-muted rounded w-1/4"></div>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <>
            {/* Projects Grid */}
            {projects.length > 0 ? (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {projects.map(project => (
                  <div
                    key={project.id}
                    className="card-interactive p-6"
                    onClick={() => router.push(`/projects/${project.id}`)}
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => e.key === 'Enter' && router.push(`/projects/${project.id}`)}
                  >
                    <div className="flex justify-between items-start mb-4">
                      <h3 className="text-lg font-semibold text-foreground line-clamp-2 min-h-[3.5rem]">
                        {project.title}
                      </h3>
                      <span className="status-success flex-shrink-0 ml-2">
                        {project.status}
                      </span>
                    </div>

                    <p className="text-muted-foreground text-sm mb-4 line-clamp-3">
                      {project.description || project.shortDescription}
                    </p>

                    <div className="flex items-center justify-between mb-4">
                      <div className="flex items-center text-sm text-muted-foreground">
                        <DollarSign className="h-4 w-4 mr-1" />
                        <span className="font-medium text-foreground">{project.creditBudget} credits</span>
                      </div>
                      {(project.deadline || project.endDate) && (
                        <div className="flex items-center text-sm text-muted-foreground">
                          <Clock className="h-4 w-4 mr-1" />
                          <span>{getDaysUntilDeadline(project.deadline || project.endDate!)}</span>
                        </div>
                      )}
                    </div>

                    {project.location && (
                      <div className="flex items-center text-sm text-muted-foreground mb-3">
                        <MapPin className="h-4 w-4 mr-1" />
                        <span>{project.location}</span>
                      </div>
                    )}

                    <div className="flex flex-wrap gap-1 mb-4">
                      {/* Handle both requiredSkills (object array) and requiredSkillNames (string array) formats */}
                      {(project.requiredSkills || []).slice(0, 3).map((skill, index) => (
                        <span
                          key={index}
                          className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-primary/10 text-primary"
                        >
                          {skill.name} ({getProficiencyLabel(skill.proficiency)})
                        </span>
                      ))}
                      {(project.requiredSkillNames || []).slice(0, 3).map((skillName, index) => (
                        <span
                          key={index}
                          className="inline-flex items-center px-2 py-1 rounded text-xs font-medium bg-primary/10 text-primary"
                        >
                          {skillName}
                        </span>
                      ))}
                      {((project.requiredSkills?.length || 0) + (project.requiredSkillNames?.length || 0)) > 3 && (
                        <span className="text-xs text-muted-foreground">
                          +{((project.requiredSkills?.length || 0) + (project.requiredSkillNames?.length || 0)) - 3} more
                        </span>
                      )}
                    </div>

                    <div className="flex items-center justify-between pt-4 border-t border-border">
                      <div>
                        <p className="text-sm font-medium text-foreground">
                          {(() => {
                            // Extract username from email if displayName looks like an email
                            const name = project.clientName || project.client?.displayName || project.client?.userName || 'Unknown'
                            return name.includes('@') ? name.split('@')[0] : name
                          })()}
                        </p>
                        {project.clientCompany && (
                          <p className="text-xs text-muted-foreground">{project.clientCompany}</p>
                        )}
                        {project.clientRating && (
                          <div className="flex items-center mt-1">
                            <Star className="h-3 w-3 text-warning fill-current" />
                            <span className="text-xs text-muted-foreground ml-1">{project.clientRating}</span>
                          </div>
                        )}
                      </div>
                      <div className="text-right">
                        {project.applicationsCount !== undefined && (
                          <p className="text-xs text-muted-foreground mb-1">
                            {project.applicationsCount} applications
                          </p>
                        )}
                        <Button
                          size="sm"
                          onClick={(e) => {
                            e.stopPropagation() // BUG-003 FIX: Prevent card div from intercepting click
                            router.push(`/projects/${project.id}`)
                          }}
                          data-testid={`view-project-${project.id}`}
                        >
                          View Details
                        </Button>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-12">
                <Briefcase className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-medium text-foreground mb-2">No projects found</h3>
                <p className="text-muted-foreground mb-4">
                  Try adjusting your filters or search terms to find more projects.
                </p>
                <Button variant="outline" onClick={clearFilters}>
                  Clear Filters
                </Button>
              </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-8 flex justify-center">
                <div className="flex items-center space-x-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={currentPage === 1}
                    data-testid="prev-page-button"
                  >
                    Previous
                  </Button>

                  <span className="text-sm text-foreground">
                    Page {currentPage} of {totalPages}
                  </span>

                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                    disabled={currentPage === totalPages}
                    data-testid="next-page-button"
                  >
                    Next
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

export default function MarketplacePage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="loading-spinner"></div>
      </div>
    }>
      <MarketplacePageContent />
    </Suspense>
  )
}
