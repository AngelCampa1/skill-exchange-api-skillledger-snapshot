'use client'

import { logger } from '@/utils/logger';
import React, { useState, useEffect, useRef } from 'react'
import { z } from 'zod'
import { useDebounce } from '@/hooks/useDebounce'

interface Skill {
  id: string
  name: string
  description: string
  category: string
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

interface ProjectSearchFormProps {
  availableSkills: Skill[]
  initialFilters: SearchFilters
  onFiltersChange: (filters: SearchFilters) => void
  isLoading?: boolean
}

const searchSchema = z.object({
  query: z.string().max(200).optional(),
  skillIds: z.array(z.string()).max(5).optional(),
  minBudget: z.number().min(50).max(5000).optional(),
  maxBudget: z.number().min(50).max(5000).optional(),
  minDurationDays: z.number().min(1).max(365).optional(),
  maxDurationDays: z.number().min(1).max(365).optional(),
  clientLocation: z.string().max(100).optional(),
  radiusKm: z.number().min(1).max(10000).optional(),
  skillMatch: z.enum(['Any', 'All']).optional(),
})

const ProjectSearchForm: React.FC<ProjectSearchFormProps> = ({
  availableSkills,
  initialFilters,
  onFiltersChange,
  isLoading = false
}) => {
  const [filters, setFilters] = useState<SearchFilters>(initialFilters)
  const [queryInput, setQueryInput] = useState(initialFilters.query || '')
  const [skillsSearch, setSkillsSearch] = useState('')
  const [showSkillsDropdown, setShowSkillsDropdown] = useState(false)
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false)
  const [locationPermission, setLocationPermission] = useState<'granted' | 'denied' | 'prompt' | null>(null)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const skillsRef = useRef<HTMLDivElement>(null)

  // Debounce the search query (300ms delay)
  const debouncedQuery = useDebounce(queryInput, 300)

  // Update internal state when initialFilters change
  useEffect(() => {
    setFilters(initialFilters)
    setQueryInput(initialFilters.query || '')
  }, [initialFilters])

  // Update filters when debounced query changes
  useEffect(() => {
    // Normalize both values for comparison (empty string and undefined should be treated the same)
    const normalizedDebouncedQuery = debouncedQuery || undefined
    const normalizedCurrentQuery = filters.query || undefined

    if (normalizedDebouncedQuery !== normalizedCurrentQuery) {
      const newFilters = { ...filters, query: normalizedDebouncedQuery }
      setFilters(newFilters)
      if (validateFilters(newFilters)) {
        onFiltersChange(newFilters)
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedQuery])

  // Close skills dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (skillsRef.current && !skillsRef.current.contains(event.target as Node)) {
        setShowSkillsDropdown(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Check geolocation permission status
  useEffect(() => {
    if (navigator.permissions) {
      navigator.permissions.query({ name: 'geolocation' } as PermissionDescriptor).then(result => {
        setLocationPermission(result.state)
      }).catch(() => {
        // Permission query failed, default to 'prompt' state
        setLocationPermission('prompt')
      })
    }
  }, [])

  const validateFilters = (newFilters: SearchFilters): boolean => {
    try {
      searchSchema.parse(newFilters)
      setErrors({})
      return true
    } catch (error) {
      if (error instanceof z.ZodError) {
        const newErrors: Record<string, string> = {}
        error.errors.forEach(err => {
          if (err.path.length > 0) {
            newErrors[err.path[0] as string] = err.message
          }
        })
        setErrors(newErrors)
      }
      return false
    }
  }

  /**
   * SECURITY FIX: Sanitize and validate input before updating filters
   * Prevents injection attacks and ensures type safety
   */
  const sanitizeFilterValue = <K extends keyof SearchFilters>(
    key: K,
    value: SearchFilters[K]
  ): SearchFilters[K] => {
    if (value === undefined || value === null) {
      return undefined as SearchFilters[K]
    }

    switch (key) {
      case 'query':
      case 'clientLocation':
        // Sanitize string inputs - remove HTML tags and trim
        if (typeof value === 'string') {
          return value
            .replace(/<[^>]*>/g, '') // Remove HTML tags
            .replace(/[<>'"]/g, '') // Remove potentially dangerous characters
            .trim()
            .slice(0, key === 'query' ? 200 : 100) as SearchFilters[K] // Enforce max length
        }
        return value

      case 'minBudget':
      case 'maxBudget':
        // Ensure budget is within valid range
        if (typeof value === 'number') {
          return Math.max(50, Math.min(5000, Math.floor(value))) as SearchFilters[K]
        }
        return value

      case 'minDurationDays':
      case 'maxDurationDays':
        // Ensure duration is within valid range
        if (typeof value === 'number') {
          return Math.max(1, Math.min(365, Math.floor(value))) as SearchFilters[K]
        }
        return value

      case 'radiusKm':
        // Ensure radius is within valid range
        if (typeof value === 'number') {
          return Math.max(1, Math.min(10000, Math.floor(value))) as SearchFilters[K]
        }
        return value

      case 'skillIds':
        // Limit number of skills and validate UUIDs
        if (Array.isArray(value)) {
          const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
          return value
            .filter(id => typeof id === 'string' && uuidRegex.test(id))
            .slice(0, 5) as SearchFilters[K] // Max 5 skills
        }
        return value

      case 'skillMatch':
        // Validate enum value
        if (value === 'Any' || value === 'All') {
          return value as SearchFilters[K]
        }
        return 'Any' as SearchFilters[K]

      case 'sortBy':
        // Validate enum value
        const validSortBy = ['Relevance', 'Newest', 'Budget', 'Deadline']
        if (typeof value === 'string' && validSortBy.includes(value)) {
          return value as SearchFilters[K]
        }
        return 'Relevance' as SearchFilters[K]

      default:
        return value
    }
  }

  /**
   * SECURITY FIX: Type-safe filter change handler with sanitization
   * Replaces unsafe 'any' type with proper generics
   */
  const handleFilterChange = <K extends keyof SearchFilters>(
    key: K,
    value: SearchFilters[K]
  ) => {
    // Sanitize the input value
    const sanitizedValue = sanitizeFilterValue(key, value)
    
    // Update local state with sanitized value
    const newFilters = { ...filters, [key]: sanitizedValue }
    setFilters(newFilters)
    
    // Validate and propagate to parent if valid
    if (validateFilters(newFilters)) {
      onFiltersChange(newFilters)
    }
  }

  const handleSkillToggle = (skillId: string) => {
    const currentSkills = filters.skillIds || []
    const newSkills = currentSkills.includes(skillId)
      ? currentSkills.filter(id => id !== skillId)
      : [...currentSkills, skillId].slice(0, 5) // Limit to 5 skills
    
    handleFilterChange('skillIds', newSkills)
  }

  const handleLocationSearch = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          handleFilterChange('latitude', position.coords.latitude)
          handleFilterChange('longitude', position.coords.longitude)
          if (!filters.radiusKm) {
            handleFilterChange('radiusKm', 50) // Default to 50km radius
          }
        },
        (error) => {
          logger.error('Geolocation error:', error)
          alert('Unable to get your location. Please enter a location manually or allow location access.')
        }
      )
    } else {
      alert('Geolocation is not supported by this browser.')
    }
  }

  const clearFilters = () => {
    const clearedFilters = { page: 1, pageSize: 20, sortBy: 'Relevance' as const }
    setFilters(clearedFilters)
    setErrors({})
    onFiltersChange(clearedFilters)
  }

  // Defensive check: ensure availableSkills is an array
  const skillsArray = Array.isArray(availableSkills) ? availableSkills : []

  const selectedSkills = skillsArray.filter(skill =>
    filters.skillIds?.includes(skill.id)
  )

  const filteredSkills = skillsArray.filter(skill =>
    skill.name.toLowerCase().includes(skillsSearch.toLowerCase()) &&
    !filters.skillIds?.includes(skill.id)
  )

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold text-foreground">Search Filters</h3>

      {/* Search Query */}
      <div>
        <label htmlFor="search-query" className="block text-sm font-medium text-foreground mb-2">
          Search Projects
        </label>
        <input
          id="search-query"
          name="search"
          type="text"
          value={queryInput}
          onChange={(e) => setQueryInput(e.target.value)}
          placeholder="Search titles, descriptions..."
          maxLength={200}
          className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          aria-label="Search projects by title or description"
        />
        {errors.query && (
          <p className="mt-1 text-sm text-destructive">{errors.query}</p>
        )}
      </div>

      {/* Skills Filter */}
      <div ref={skillsRef} className="relative">
        <label htmlFor="skills-search" className="block text-sm font-medium text-foreground mb-2">
          Required Skills
        </label>

        {/* Selected Skills */}
        {selectedSkills.length > 0 && (
          <div className="mb-2 flex flex-wrap gap-2">
            {selectedSkills.map(skill => (
              <span key={skill.id} className="bg-primary/10 text-primary text-sm px-2 py-1 rounded-full flex items-center">
                {skill.name}
                <button
                  onClick={() => handleSkillToggle(skill.id)}
                  className="ml-1 text-primary hover:text-primary/80"
                >
                  ×
                </button>
              </span>
            ))}
          </div>
        )}

        {/* Skill Match Strategy */}
        {selectedSkills.length > 1 && (
          <div className="mb-2">
            <div className="flex space-x-4">
              <label className="flex items-center">
                <input
                  type="radio"
                  name="skillMatch"
                  value="Any"
                  checked={filters.skillMatch !== 'All'}
                  onChange={() => handleFilterChange('skillMatch', 'Any')}
                  className="mr-1"
                />
                <span className="text-sm">Any of these skills</span>
              </label>
              <label className="flex items-center">
                <input
                  type="radio"
                  name="skillMatch"
                  value="All"
                  checked={filters.skillMatch === 'All'}
                  onChange={() => handleFilterChange('skillMatch', 'All')}
                  className="mr-1"
                />
                <span className="text-sm">All of these skills</span>
              </label>
            </div>
          </div>
        )}

        {/* Skills Search */}
        <input
          id="skills-search"
          type="text"
          value={skillsSearch}
          onChange={(e) => {
            setSkillsSearch(e.target.value)
            setShowSkillsDropdown(true)
          }}
          onFocus={() => setShowSkillsDropdown(true)}
          placeholder="Search and select skills..."
          disabled={selectedSkills.length >= 5}
          className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring disabled:bg-muted"
        />

        {/* Skills Dropdown */}
        {showSkillsDropdown && skillsSearch && filteredSkills.length > 0 && (
          <div className="absolute z-10 mt-1 w-full bg-background border border-border rounded-md shadow-lg max-h-60 overflow-y-auto">
            {filteredSkills.slice(0, 10).map(skill => (
              <button
                key={skill.id}
                onClick={() => {
                  handleSkillToggle(skill.id)
                  setSkillsSearch('')
                  setShowSkillsDropdown(false)
                }}
                className="w-full text-left px-3 py-2 hover:bg-accent focus:bg-accent"
              >
                <div className="font-medium">{skill.name}</div>
                <div className="text-sm text-muted-foreground">{skill.category}</div>
              </button>
            ))}
          </div>
        )}

        {selectedSkills.length >= 5 && (
          <p className="mt-1 text-sm text-warning">Maximum 5 skills selected</p>
        )}
      </div>

      {/* Budget Range */}
      <div>
        <label htmlFor="min-budget" className="block text-sm font-medium text-foreground mb-2">
          Credit Budget Range
        </label>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <input
              id="min-budget"
              type="number"
              value={filters.minBudget || ''}
              onChange={(e) => handleFilterChange('minBudget', e.target.value ? parseInt(e.target.value) : undefined)}
              placeholder="Min (50)"
              min="50"
              max="5000"
              className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <input
              type="number"
              value={filters.maxBudget || ''}
              onChange={(e) => handleFilterChange('maxBudget', e.target.value ? parseInt(e.target.value) : undefined)}
              placeholder="Max (5000)"
              min="50"
              max="5000"
              className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
        </div>
        {(errors.minBudget || errors.maxBudget) && (
          <p className="mt-1 text-sm text-destructive">{errors.minBudget || errors.maxBudget}</p>
        )}
      </div>

      {/* Advanced Filters Toggle */}
      <button
        onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
        className="flex items-center text-primary hover:text-primary/80 text-sm font-medium"
      >
        {showAdvancedFilters ? (
          <>
            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
            </svg>
            Hide Advanced Filters
          </>
        ) : (
          <>
            <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
            Show Advanced Filters
          </>
        )}
      </button>

      {/* Advanced Filters */}
      {showAdvancedFilters && (
        <div className="space-y-4 border-t pt-4">
          {/* Duration Range */}
          <div>
            <label className="block text-sm font-medium text-foreground mb-2">
              Project Duration (Days)
            </label>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <input
                  type="number"
                  value={filters.minDurationDays || ''}
                  onChange={(e) => handleFilterChange('minDurationDays', e.target.value ? parseInt(e.target.value) : undefined)}
                  placeholder="Min days"
                  min="1"
                  max="365"
                  className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <input
                  type="number"
                  value={filters.maxDurationDays || ''}
                  onChange={(e) => handleFilterChange('maxDurationDays', e.target.value ? parseInt(e.target.value) : undefined)}
                  placeholder="Max days"
                  min="1"
                  max="365"
                  className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
            </div>
          </div>

          {/* Timeline Filters */}
          <div>
            <label className="block text-sm font-medium text-foreground mb-2">
              Project Start Date
            </label>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <input
                  type="date"
                  value={filters.startDateFrom || ''}
                  onChange={(e) => handleFilterChange('startDateFrom', e.target.value || undefined)}
                  className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
              <div>
                <input
                  type="date"
                  value={filters.startDateTo || ''}
                  onChange={(e) => handleFilterChange('startDateTo', e.target.value || undefined)}
                  className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
            </div>
          </div>

          {/* Location Filter */}
          <div>
            <label htmlFor="client-location" className="block text-sm font-medium text-foreground mb-2">
              Location
            </label>
            <div className="space-y-2">
              <input
                id="client-location"
                type="text"
                value={filters.clientLocation || ''}
                onChange={(e) => handleFilterChange('clientLocation', e.target.value || undefined)}
                placeholder="City, State, Country"
                className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />

              <div className="flex items-center justify-between">
                <button
                  onClick={handleLocationSearch}
                  disabled={locationPermission === 'denied'}
                  className="flex items-center text-sm text-primary hover:text-primary/80 disabled:text-muted-foreground"
                >
                  <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  Use My Location
                </button>

                {(filters.latitude && filters.longitude) && (
                  <div className="text-xs text-muted-foreground">
                    Within {filters.radiusKm || 50}km
                  </div>
                )}
              </div>

              {(filters.latitude && filters.longitude) && (
                <input
                  type="number"
                  value={filters.radiusKm || 50}
                  onChange={(e) => handleFilterChange('radiusKm', e.target.value ? parseInt(e.target.value) : 50)}
                  placeholder="Radius (km)"
                  min="1"
                  max="10000"
                  className="w-full px-3 py-2 border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              )}
            </div>
          </div>
        </div>
      )}

      {/* Action Buttons */}
      <div className="space-y-3">
        <button
          onClick={clearFilters}
          disabled={isLoading}
          className="w-full px-4 py-2 border border-border rounded-full text-sm font-medium text-foreground hover:bg-accent disabled:opacity-50"
        >
          Clear All Filters
        </button>
      </div>

      {/* Filter Summary */}
      {(filters.query || filters.skillIds?.length || filters.minBudget || filters.maxBudget ||
        filters.clientLocation || (filters.latitude && filters.longitude)) && (
        <div className="text-xs text-muted-foreground border-t pt-3">
          <div className="font-medium mb-1">Active Filters:</div>
          <ul className="space-y-1">
            {filters.query && <li>• Search: "{filters.query}"</li>}
            {filters.skillIds?.length && (
              <li>• Skills: {filters.skillIds.length} selected ({filters.skillMatch || 'Any'})</li>
            )}
            {(filters.minBudget || filters.maxBudget) && (
              <li>• Budget: {filters.minBudget || 50} - {filters.maxBudget || 5000} credits</li>
            )}
            {filters.clientLocation && <li>• Location: {filters.clientLocation}</li>}
            {(filters.latitude && filters.longitude) && (
              <li>• Near you: {filters.radiusKm || 50}km radius</li>
            )}
          </ul>
        </div>
      )}
    </div>
  )
}

export default ProjectSearchForm