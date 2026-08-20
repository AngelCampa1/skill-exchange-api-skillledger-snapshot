'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react'
import { useAuth } from '@/contexts/AuthContext'

interface SavedSearch {
  id: string
  name: string
  description?: string
  searchCriteria: {
    query?: string
    skillIds?: string[]
    minBudget?: number
    maxBudget?: number
    clientLocation?: string
    [key: string]: unknown
  }
  emailNotifications: boolean
  notificationFrequency: 'Immediately' | 'Daily' | 'Weekly'
  isActive: boolean
  createdAt: string
  lastExecutedAt?: string
  resultsCount?: number
}

type SearchCriteria = SavedSearch['searchCriteria'];

interface SavedSearchesProps {
  onExecuteSearch?: (searchCriteria: SearchCriteria) => void
}

const SavedSearches: React.FC<SavedSearchesProps> = ({ onExecuteSearch }) => {
  const { user, isAuthenticated } = useAuth()
  const [savedSearches, setSavedSearches] = useState<SavedSearch[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showEditModal, setShowEditModal] = useState(false)
  const [selectedSearch, setSelectedSearch] = useState<SavedSearch | null>(null)
  const [newSearchName, setNewSearchName] = useState('')
  const [newSearchDescription, setNewSearchDescription] = useState('')
  const [emailNotifications, setEmailNotifications] = useState(false)
  const [notificationFrequency, setNotificationFrequency] = useState<'Immediately' | 'Daily' | 'Weekly'>('Daily')

  const loadSavedSearches = useCallback(async () => {
    if (!user) return

    setIsLoading(true)
    setError(null)

    try {
      const response = await fetch(`/api/project-search/saved-searches`, {
        credentials: 'include',
      })

      if (response.ok) {
        const searches = await response.json()
        setSavedSearches(searches)
      } else if (response.status === 401) {
        setError('Please log in to view saved searches')
      } else {
        throw new Error('Failed to load saved searches')
      }
    } catch (error) {
      logger.error('Error loading saved searches', error, { component: 'SavedSearches' })
      setError('Failed to load saved searches')
    } finally {
      setIsLoading(false)
    }
  }, [user])

  useEffect(() => {
    if (isAuthenticated) {
      loadSavedSearches()
    }
  }, [isAuthenticated, loadSavedSearches])

  const createSavedSearch = async (searchCriteria: SearchCriteria) => {
    if (!user || !newSearchName.trim()) return

    try {
      const response = await fetch('/api/project-search/saved-searches', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({
          name: newSearchName.trim(),
          description: newSearchDescription.trim() || undefined,
          searchCriteria,
          emailNotifications,
          notificationFrequency,
          isActive: true
        }),
      })

      if (response.ok) {
        await loadSavedSearches()
        setShowCreateModal(false)
        setNewSearchName('')
        setNewSearchDescription('')
        setEmailNotifications(false)
        setNotificationFrequency('Daily')
      } else {
        throw new Error('Failed to save search')
      }
    } catch (error) {
      logger.error('Error saving search', error, { component: 'SavedSearches' })
      setError('Failed to save search')
    }
  }

  const updateSavedSearch = async (searchId: string, updates: Partial<SavedSearch>) => {
    try {
      const response = await fetch(`/api/project-search/saved-searches/${searchId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify(updates),
      })

      if (response.ok) {
        await loadSavedSearches()
        setShowEditModal(false)
        setSelectedSearch(null)
      } else {
        throw new Error('Failed to update saved search')
      }
    } catch (error) {
      logger.error('Error updating saved search', error, { component: 'SavedSearches' })
      setError('Failed to update saved search')
    }
  }

  const deleteSavedSearch = async (searchId: string) => {
    if (!confirm('Are you sure you want to delete this saved search?')) return

    try {
      const response = await fetch(`/api/project-search/saved-searches/${searchId}`, {
        method: 'DELETE',
        credentials: 'include',
      })

      if (response.ok) {
        await loadSavedSearches()
      } else {
        throw new Error('Failed to delete saved search')
      }
    } catch (error) {
      logger.error('Error deleting saved search', error, { component: 'SavedSearches' })
      setError('Failed to delete saved search')
    }
  }

  const executeSearch = (search: SavedSearch) => {
    if (onExecuteSearch) {
      onExecuteSearch(search.searchCriteria)
    }
    
    // Update last executed time
    updateSavedSearch(search.id, { lastExecutedAt: new Date().toISOString() })
  }

  const formatSearchCriteria = (criteria: SearchCriteria): string => {
    const parts = []
    
    if (criteria.query) parts.push(`"${criteria.query}"`)
    if (criteria.skillIds?.length) parts.push(`${criteria.skillIds.length} skills`)
    if (criteria.minBudget || criteria.maxBudget) {
      parts.push(`$${criteria.minBudget || 50}-${criteria.maxBudget || 5000}`)
    }
    if (criteria.clientLocation) parts.push(criteria.clientLocation)
    
    return parts.length > 0 ? parts.join(', ') : 'All projects'
  }

  const openEditModal = (search: SavedSearch) => {
    setSelectedSearch(search)
    setNewSearchName(search.name)
    setNewSearchDescription(search.description || '')
    setEmailNotifications(search.emailNotifications)
    setNotificationFrequency(search.notificationFrequency)
    setShowEditModal(true)
  }

  if (!isAuthenticated) {
    return (
      <div className="bg-card rounded-lg shadow p-6 text-center">
        <svg className="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
        </svg>
        <h3 className="mt-2 text-sm font-medium text-foreground">Authentication Required</h3>
        <p className="mt-1 text-sm text-muted-foreground">Please log in to manage saved searches.</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-semibold text-foreground">Saved Searches</h2>
        <button
          onClick={() => setShowCreateModal(true)}
          className="bg-primary text-primary-foreground px-4 py-2 rounded hover:bg-primary/90 text-sm"
        >
          Save Current Search
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
            </div>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
          <p className="mt-2 text-muted-foreground">Loading saved searches...</p>
        </div>
      ) : savedSearches.length === 0 ? (
        <div className="bg-card rounded-lg shadow p-8 text-center">
          <svg className="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-foreground">No saved searches</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Create your first saved search to get notified of new projects matching your criteria.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {savedSearches.map((search) => (
            <div key={search.id} className="bg-card rounded-lg shadow p-6">
              <div className="flex justify-between items-start">
                <div className="flex-1">
                  <div className="flex items-center space-x-2">
                    <h3 className="text-lg font-medium text-foreground">{search.name}</h3>
                    {!search.isActive && (
                      <span className="bg-muted text-muted-foreground text-xs font-medium px-2 py-1 rounded">
                        Inactive
                      </span>
                    )}
                    {search.emailNotifications && (
                      <span className="bg-primary/10 text-primary text-xs font-medium px-2 py-1 rounded">
                        📧 {search.notificationFrequency}
                      </span>
                    )}
                  </div>

                  {search.description && (
                    <p className="text-muted-foreground text-sm mt-1">{search.description}</p>
                  )}

                  <div className="mt-2 text-sm text-muted-foreground">
                    <p><strong>Criteria:</strong> {formatSearchCriteria(search.searchCriteria)}</p>
                    <p className="mt-1">
                      <strong>Created:</strong> {new Date(search.createdAt).toLocaleDateString()}
                      {search.lastExecutedAt && (
                        <span className="ml-4">
                          <strong>Last used:</strong> {new Date(search.lastExecutedAt).toLocaleDateString()}
                        </span>
                      )}
                    </p>
                    {search.resultsCount !== undefined && (
                      <p className="mt-1">
                        <strong>Recent matches:</strong> {search.resultsCount} projects
                      </p>
                    )}
                  </div>
                </div>

                <div className="ml-6 flex space-x-2">
                  <button
                    onClick={() => executeSearch(search)}
                    className="bg-primary text-primary-foreground px-3 py-1 rounded text-sm hover:bg-primary/90"
                  >
                    Execute
                  </button>
                  <button
                    onClick={() => openEditModal(search)}
                    className="bg-muted text-foreground px-3 py-1 rounded text-sm hover:bg-muted/80"
                  >
                    Edit
                  </button>
                  <button
                    onClick={() => deleteSavedSearch(search.id)}
                    className="bg-destructive text-destructive-foreground px-3 py-1 rounded text-sm hover:bg-destructive/90"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create Saved Search Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-background/80 flex items-center justify-center z-50">
          <div className="bg-card rounded-lg p-6 w-full max-w-md border border-border">
            <h3 className="text-lg font-medium text-foreground mb-4">Save Search</h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-2">Search Name</label>
                <input
                  type="text"
                  value={newSearchName}
                  onChange={(e) => setNewSearchName(e.target.value)}
                  placeholder="e.g., React Developer Projects"
                  className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-2">Description (Optional)</label>
                <textarea
                  value={newSearchDescription}
                  onChange={(e) => setNewSearchDescription(e.target.value)}
                  placeholder="Brief description of this search..."
                  rows={3}
                  className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div className="space-y-2">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={emailNotifications}
                    onChange={(e) => setEmailNotifications(e.target.checked)}
                    className="mr-2"
                  />
                  <span className="text-sm">Email notifications for new matches</span>
                </label>

                {emailNotifications && (
                  <select
                    value={notificationFrequency}
                    onChange={(e) => setNotificationFrequency(e.target.value as 'Immediately' | 'Daily' | 'Weekly')}
                    className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="Immediately">Immediately</option>
                    <option value="Daily">Daily digest</option>
                    <option value="Weekly">Weekly digest</option>
                  </select>
                )}
              </div>
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setShowCreateModal(false)}
                className="px-4 py-2 border border-border rounded text-sm text-foreground hover:bg-muted"
              >
                Cancel
              </button>
              <button
                onClick={() => createSavedSearch({})} // You would pass current search criteria here
                disabled={!newSearchName.trim()}
                className="px-4 py-2 bg-primary text-primary-foreground rounded text-sm hover:bg-primary/90 disabled:opacity-50"
              >
                Save Search
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Saved Search Modal */}
      {showEditModal && selectedSearch && (
        <div className="fixed inset-0 bg-background/80 flex items-center justify-center z-50">
          <div className="bg-card rounded-lg p-6 w-full max-w-md border border-border">
            <h3 className="text-lg font-medium text-foreground mb-4">Edit Saved Search</h3>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-2">Search Name</label>
                <input
                  type="text"
                  value={newSearchName}
                  onChange={(e) => setNewSearchName(e.target.value)}
                  className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-2">Description</label>
                <textarea
                  value={newSearchDescription}
                  onChange={(e) => setNewSearchDescription(e.target.value)}
                  rows={3}
                  className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div className="space-y-2">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={emailNotifications}
                    onChange={(e) => setEmailNotifications(e.target.checked)}
                    className="mr-2"
                  />
                  <span className="text-sm">Email notifications</span>
                </label>

                {emailNotifications && (
                  <select
                    value={notificationFrequency}
                    onChange={(e) => setNotificationFrequency(e.target.value as 'Immediately' | 'Daily' | 'Weekly')}
                    className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="Immediately">Immediately</option>
                    <option value="Daily">Daily digest</option>
                    <option value="Weekly">Weekly digest</option>
                  </select>
                )}
              </div>

              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={selectedSearch.isActive}
                  onChange={(e) => setSelectedSearch({ ...selectedSearch, isActive: e.target.checked })}
                  className="mr-2"
                />
                <span className="text-sm">Active (receive notifications)</span>
              </label>
            </div>

            <div className="flex justify-end space-x-3 mt-6">
              <button
                onClick={() => setShowEditModal(false)}
                className="px-4 py-2 border border-border rounded text-sm text-foreground hover:bg-muted"
              >
                Cancel
              </button>
              <button
                onClick={() => updateSavedSearch(selectedSearch.id, {
                  name: newSearchName.trim(),
                  description: newSearchDescription.trim() || undefined,
                  emailNotifications,
                  notificationFrequency,
                  isActive: selectedSearch.isActive
                })}
                disabled={!newSearchName.trim()}
                className="px-4 py-2 bg-primary text-primary-foreground rounded text-sm hover:bg-primary/90 disabled:opacity-50"
              >
                Update Search
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default SavedSearches