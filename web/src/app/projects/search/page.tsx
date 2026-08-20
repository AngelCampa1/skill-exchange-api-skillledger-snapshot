'use client'
import { logger } from '@/utils/logger';
import { trackEvent } from '@/utils/analytics';

import React, { useState, useEffect, useCallback, Suspense } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import Link from 'next/link'
import { ArrowLeft } from 'lucide-react'
import ProjectSearchForm from '@/components/ProjectSearchForm'
import { ThemeToggle } from '@/components/ThemeToggle'

interface Skill {
  id: string
  name: string
  description: string
  category: string
}

interface Project {
  id: string
  title: string
  description?: string
  shortDescription?: string
  creditBudget: number
  startDate?: string
  endDate?: string
  status: string
  location?: {
    city?: string
    state?: string
    country?: string
  }
  skills?: Array<{
    skillId: string
    skillName: string
    proficiencyRequired: number
    weight: number
  }>
  requiredSkillNames?: string[]
  client: {
    id: string
    userName?: string
    displayName?: string
    profileComplete?: boolean
  }
  createdAt: string
  isUrgent?: boolean
  isFeatured?: boolean
  complexityScore?: number
}

interface SearchFilters {
  query?: string
  skillIds?: string[]
  minBudget?: number
  maxBudget?: number
  minDurationDays?: number
  maxDurationDays?: number
  startDateFrom?: string
  startDateTo?: string
  endDateFrom?: string
  endDateTo?: string
  clientLocation?: string
  latitude?: number
  longitude?: number
  radiusKm?: number
  status?: string[]
  skillMatch?: 'Any' | 'All'
  sortBy?: 'Relevance' | 'Newest' | 'Budget' | 'Deadline'
  page?: number
  pageSize?: number
}

interface SearchResult {
  projects: Project[]
  totalCount: number
  currentPage: number
  totalPages: number
  aggregations: {
    skillCounts: Array<{ skillId: string; skillName: string; count: number }>
    budgetRanges: Array<{ range: string; count: number }>
    locationCounts: Array<{ location: string; count: number }>
    statusCounts: Array<{ status: string; count: number }>
  }
}

function ProjectSearchPageContent() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [availableSkills, setAvailableSkills] = useState<Skill[]>([])
  const [searchResults, setSearchResults] = useState<SearchResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [filters, setFilters] = useState<SearchFilters>({
    page: 1,
    pageSize: 20,
    sortBy: 'Relevance'
  })

  // Parse URL parameters on load
  useEffect(() => {
    const urlFilters: SearchFilters = {
      query: searchParams?.get('q') || undefined,
      minBudget: searchParams?.get('minBudget') ? parseInt(searchParams.get('minBudget')!) : undefined,
      maxBudget: searchParams?.get('maxBudget') ? parseInt(searchParams.get('maxBudget')!) : undefined,
      sortBy: (searchParams?.get('sort') as SearchFilters['sortBy']) || 'Relevance',
      page: searchParams?.get('page') ? parseInt(searchParams.get('page')!) : 1,
      pageSize: 20
    }

    const skillIds = searchParams?.get('skills')?.split(',').filter(Boolean)
    if (skillIds && skillIds.length > 0) {
      urlFilters.skillIds = skillIds
    }

    setFilters(urlFilters)
  }, [searchParams])

  // Load available skills
  useEffect(() => {
    const loadSkills = async () => {
      try {
        const response = await fetch('/api/skills', {
          credentials: 'include',
        })

        if (response.ok) {
          const data = await response.json()

          // Handle multiple possible response structures
          let skillsArray: Skill[] = []
          if (Array.isArray(data)) {
            skillsArray = data
          } else if (data && Array.isArray(data.skills)) {
            skillsArray = data.skills
          } else if (data && Array.isArray(data.Skills)) {
            skillsArray = data.Skills
          } else {
            logger.error('Unexpected skills response format', undefined, { data, page: 'project-search' })
          }

          setAvailableSkills(skillsArray)
        } else {
          logger.error('Failed to load skills', undefined, { page: 'project-search' })
        }
      } catch (error) {
        logger.error('Error loading skills', error, { page: 'project-search' })
      }
    }

    loadSkills()
  }, [])

  const updateURL = useCallback((newFilters: SearchFilters) => {
    const params = new URLSearchParams()
    
    if (newFilters.query) params.set('q', newFilters.query)
    if (newFilters.skillIds && newFilters.skillIds.length > 0) params.set('skills', newFilters.skillIds.join(','))
    if (newFilters.minBudget) params.set('minBudget', newFilters.minBudget.toString())
    if (newFilters.maxBudget) params.set('maxBudget', newFilters.maxBudget.toString())
    if (newFilters.sortBy && newFilters.sortBy !== 'Relevance') params.set('sort', newFilters.sortBy)
    if (newFilters.page && newFilters.page > 1) params.set('page', newFilters.page.toString())

    const newUrl = `/projects/search${params.toString() ? '?' + params.toString() : ''}`
    window.history.pushState({}, '', newUrl)
  }, [])

  // Helper function to map UI sort names to backend field names
  const mapSortField = (sortBy: string): string => {
    switch(sortBy) {
      case 'Newest': return 'created'
      case 'Budget': return 'budget'
      case 'Deadline': return 'endDate'
      case 'Relevance':
      default: return 'relevance'
    }
  }

  const performSearch = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    // Track search initiation
    trackEvent({
      name: 'project_search',
      category: 'search',
      priority: 'high',
      properties: {
        has_query: !!filters.query,
        has_skills_filter: !!(filters.skillIds && filters.skillIds.length > 0),
        has_budget_filter: !!(filters.minBudget || filters.maxBudget),
        sort_by: filters.sortBy || 'Relevance',
      },
    })

    try {
      // Calculate pagination values (skip/take pattern)
      const currentPage = filters.page || 1
      const itemsPerPage = filters.pageSize || 20

      // Build search payload with proper type handling
      // Only include skillIds if they are valid GUIDs
      const validSkillIds = filters.skillIds?.filter(id => {
        // Validate GUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
        return guidRegex.test(id)
      }) || []

      const searchPayload = {
        query: filters.query || undefined,
        // Only include skillIds if we have valid ones
        skillIds: validSkillIds.length > 0 ? validSkillIds : undefined,
        // Transform skillMatch to enum numeric value: Any=1, All=2
        skillMatch: filters.skillMatch === 'All' ? 2 : 1,
        minBudget: filters.minBudget,
        maxBudget: filters.maxBudget,
        minDurationDays: filters.minDurationDays,
        maxDurationDays: filters.maxDurationDays,
        startDateFrom: filters.startDateFrom,
        startDateTo: filters.startDateTo,
        endDateFrom: filters.endDateFrom,
        endDateTo: filters.endDateTo,
        clientLocation: filters.clientLocation,
        latitude: filters.latitude,
        longitude: filters.longitude,
        radiusKm: filters.radiusKm,
        // Default to Published status for public search
        status: filters.status && filters.status.length > 0 ? filters.status : undefined,
        // Transform sortBy from string to SortCriteria array format
        sortBy: filters.sortBy ? [{
          field: mapSortField(filters.sortBy),
          direction: 'desc',
          weight: 1
        }] : undefined,
        // Transform page/pageSize to skip/take
        skip: (currentPage - 1) * itemsPerPage,
        take: itemsPerPage,
        // Always include published only for unauthenticated search
        publishedOnly: true
      }

      const response = await fetch('/api/project-search/advanced', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify(searchPayload),
      })

      if (!response.ok) {
        // Try to get error details from response
        let errorMessage = `Search failed: ${response.statusText}`
        try {
          const errorData = await response.json()
          if (errorData.message) {
            errorMessage = errorData.message
          }
        } catch {
          // Ignore JSON parse error, use default message
        }
        throw new Error(errorMessage)
      }

      const result = await response.json()
      setSearchResults(result)
      updateURL(filters)

      // Track search results received
      trackEvent({
        name: 'search_results',
        category: 'search',
        priority: 'high',
        properties: {
          results_count: result.totalCount || 0,
          has_results: (result.totalCount || 0) > 0,
          page_number: result.currentPage || 1,
          total_pages: result.totalPages || 0,
        },
      })
    } catch (error) {
      logger.error('Search error', error, { page: 'project-search' })
      setError(error instanceof Error ? error.message : 'Search failed')
    } finally {
      setIsLoading(false)
    }
  }, [filters, updateURL])

  // Perform search when filters change
  useEffect(() => {
    if (Object.keys(filters).length > 0) {
      performSearch()
    }
  }, [filters, performSearch])

  const handleFiltersChange = (newFilters: SearchFilters) => {
    setFilters({ ...newFilters, page: 1 }) // Reset to first page when filters change
  }

  const handlePageChange = (page: number) => {
    setFilters(prev => ({ ...prev, page }))
  }

  const handleSortChange = (sortBy: SearchFilters['sortBy']) => {
    setFilters(prev => ({ ...prev, sortBy, page: 1 }))
  }

  return (
    <div className="min-h-screen bg-muted">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* BUG-009 FIX: Navigation Header with theme toggle */}
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
          <h1 className="text-3xl font-bold text-foreground">Find Projects</h1>
          <p className="mt-2 text-muted-foreground">
            Discover opportunities that match your skills and interests
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Search Filters Sidebar */}
          <div className="lg:col-span-1">
            <div className="bg-card rounded-lg shadow p-6 sticky top-6">
              <ProjectSearchForm
                availableSkills={availableSkills}
                initialFilters={filters}
                onFiltersChange={handleFiltersChange}
                isLoading={isLoading}
              />
            </div>
          </div>

          {/* Search Results */}
          <div className="lg:col-span-3">
            {/* Results Header */}
            {searchResults && (
              <div className="bg-card rounded-lg shadow mb-6 p-4">
                <div className="flex justify-between items-center">
                  <div className="text-sm text-muted-foreground">
                    {/* E2E-006 FIX: Handle empty state properly */}
                    {searchResults.totalCount === 0 ? (
                      'No projects found'
                    ) : (
                      <>Showing {((searchResults.currentPage - 1) * (filters.pageSize || 20)) + 1} - {Math.min(searchResults.currentPage * (filters.pageSize || 20), searchResults.totalCount)} of {searchResults.totalCount} projects</>
                    )}
                  </div>
                  <div className="flex items-center space-x-4">
                    <label className="text-sm font-medium text-foreground">Sort by:</label>
                    <select
                      value={filters.sortBy}
                      onChange={(e) => handleSortChange(e.target.value as SearchFilters['sortBy'])}
                      className="border border-input rounded px-3 py-1 text-sm bg-background"
                    >
                      <option value="Relevance">Relevance</option>
                      <option value="Newest">Newest</option>
                      <option value="Budget">Budget (High to Low)</option>
                      <option value="Deadline">Deadline (Soon)</option>
                    </select>
                  </div>
                </div>
              </div>
            )}

            {/* Loading State */}
            {isLoading && (
              <div className="text-center py-12">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
                <p className="mt-4 text-muted-foreground">Searching projects...</p>
              </div>
            )}

            {/* Error State */}
            {error && (
              <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-6 mb-6">
                <div className="flex">
                  <div className="flex-shrink-0">
                    <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-destructive">Search Error</h3>
                    <div className="mt-2 text-sm text-destructive">
                      <p>{error}</p>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Search Results */}
            {searchResults && !isLoading && (
              <div className="space-y-6">
                {searchResults.projects.length === 0 ? (
                  <div className="bg-card rounded-lg shadow p-12 text-center">
                    <svg className="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                    <h3 className="mt-2 text-sm font-medium text-foreground">No projects found</h3>
                    <p className="mt-1 text-sm text-muted-foreground">Try adjusting your search filters or search terms.</p>
                  </div>
                ) : (
                  <>
                    {/* Project Cards */}
                    <div className="project-list" data-testid="project-list">
                    {searchResults.projects.map((project) => (
                      <div key={project.id} className="project-card bg-card rounded-lg shadow hover:shadow-md transition-shadow p-6" data-testid="project-card">
                        <div className="flex justify-between items-start">
                          <div className="flex-1">
                            <div className="flex items-center space-x-2">
                              <h3 className="text-lg font-semibold text-foreground hover:text-primary cursor-pointer"
                                  onClick={() => router.push(`/projects/${project.id}`)}>
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

                            <p className="text-muted-foreground text-sm mt-2 line-clamp-3">
                              {project.description || project.shortDescription}
                            </p>

                            <div className="flex flex-wrap items-center gap-4 mt-4 text-sm text-muted-foreground">
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
                                  {[project.location.city, project.location.state, project.location.country]
                                    .filter(Boolean).join(', ')}
                                </div>
                              )}
                              
                              <div className="flex items-center">
                                <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                </svg>
                                {project.endDate ? new Date(project.endDate).toLocaleDateString() : 'Flexible'}
                              </div>
                            </div>

                            {/* Skills - handle both skills array and requiredSkillNames array */}
                            {((project.skills?.length || 0) > 0 || (project.requiredSkillNames?.length || 0) > 0) && (
                              <div className="mt-3">
                                <div className="flex flex-wrap gap-2">
                                  {/* Handle skills array (object format) */}
                                  {(project.skills || []).slice(0, 5).map((skill) => (
                                    <span key={skill.skillId}
                                          className="bg-primary/10 text-primary text-xs font-medium px-2 py-1 rounded">
                                      {skill.skillName}
                                      {skill.proficiencyRequired > 1 && (
                                        <span className="ml-1 text-primary">
                                          {'★'.repeat(skill.proficiencyRequired)}
                                        </span>
                                      )}
                                    </span>
                                  ))}
                                  {/* Handle requiredSkillNames array (string format) */}
                                  {(project.requiredSkillNames || []).slice(0, 5).map((skillName, index) => (
                                    <span key={index}
                                          className="bg-primary/10 text-primary text-xs font-medium px-2 py-1 rounded">
                                      {skillName}
                                    </span>
                                  ))}
                                  {((project.skills?.length || 0) + (project.requiredSkillNames?.length || 0)) > 5 && (
                                    <span className="bg-muted text-muted-foreground text-xs font-medium px-2 py-1 rounded">
                                      +{((project.skills?.length || 0) + (project.requiredSkillNames?.length || 0)) - 5} more
                                    </span>
                                  )}
                                </div>
                              </div>
                            )}
                          </div>

                          <div className="ml-6 text-right">
                            <div className="text-sm text-muted-foreground">
                              Posted {new Date(project.createdAt).toLocaleDateString()}
                            </div>
                            <div className="text-sm text-muted-foreground mt-1">
                              by {(() => {
                                // Extract username from email if displayName or userName looks like an email
                                const name = project.client.displayName || project.client.userName || 'Unknown'
                                return name.includes('@') ? name.split('@')[0] : name
                              })()}
                            </div>
                            <button
                              onClick={() => router.push(`/projects/${project.id}`)}
                              className="mt-3 bg-primary text-primary-foreground px-4 py-2 rounded text-sm hover:bg-primary/90 transition-colors"
                            >
                              View Project
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                    </div>

                    {/* Pagination */}
                    {searchResults.totalPages > 1 && (
                      <div className="bg-card rounded-lg shadow p-4">
                        <div className="flex items-center justify-between">
                          <div className="text-sm text-foreground">
                            Page {searchResults.currentPage} of {searchResults.totalPages}
                          </div>
                          <div className="flex space-x-1">
                            <button
                              onClick={() => handlePageChange(searchResults.currentPage - 1)}
                              disabled={searchResults.currentPage <= 1}
                              className="px-3 py-1 text-sm border border-input rounded hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              Previous
                            </button>

                            {Array.from({ length: Math.min(5, searchResults.totalPages) }, (_, i) => {
                              const page = i + Math.max(1, searchResults.currentPage - 2)
                              return page <= searchResults.totalPages ? (
                                <button
                                  key={page}
                                  onClick={() => handlePageChange(page)}
                                  className={`px-3 py-1 text-sm border border-input rounded ${
                                    page === searchResults.currentPage
                                      ? 'bg-primary text-primary-foreground border-primary'
                                      : 'hover:bg-muted'
                                  }`}
                                >
                                  {page}
                                </button>
                              ) : null
                            })}

                            <button
                              onClick={() => handlePageChange(searchResults.currentPage + 1)}
                              disabled={searchResults.currentPage >= searchResults.totalPages}
                              className="px-3 py-1 text-sm border border-input rounded hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              Next
                            </button>
                          </div>
                        </div>
                      </div>
                    )}
                  </>
                )}
              </div>
            )}

            {/* Initial State */}
            {!searchResults && !isLoading && !error && (
              <div className="bg-card rounded-lg shadow p-12 text-center">
                <svg className="mx-auto h-16 w-16 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <h3 className="mt-4 text-lg font-medium text-foreground">Start Your Search</h3>
                <p className="mt-2 text-muted-foreground">Use the filters to find projects that match your skills and preferences.</p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

export default function ProjectSearchPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen bg-muted flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    }>
      <ProjectSearchPageContent />
    </Suspense>
  )
}