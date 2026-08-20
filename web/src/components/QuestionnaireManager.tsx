'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import {
  QuestionnaireData,
  QuestionnaireSearchRequest,
  QuestionnaireSearchResult,
  QuestionnaireType,
  getQuestionnaireTypeLabel
} from '../types/questionnaire'
import { questionnaireApiService } from '../services/questionnaireApiService'

interface QuestionnaireManagerProps {
  showTemplatesOnly?: boolean
  onSelectQuestionnaire?: (questionnaire: QuestionnaireData) => void
}

export default function QuestionnaireManager({
  showTemplatesOnly = false,
  onSelectQuestionnaire
}: QuestionnaireManagerProps) {
  const router = useRouter()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [questionnaires, setQuestionnaires] = useState<QuestionnaireData[]>([])
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false
  })

  const [filters, setFilters] = useState<QuestionnaireSearchRequest>({
    searchTerm: '',
    type: undefined,
    isActive: true,
    isTemplate: showTemplatesOnly,
    page: 1,
    pageSize: 20,
    sortBy: 'UpdatedAt',
    sortDescending: true
  })

  const [showCreateModal, setShowCreateModal] = useState(false)

  const loadQuestionnaires = useCallback(async () => {
    try {
      setLoading(true)
      setError(null)

      let result: QuestionnaireSearchResult

      if (showTemplatesOnly) {
        const templates = await questionnaireApiService.getAvailableTemplates()
        result = {
          questionnaires: templates,
          totalCount: templates.length,
          page: 1,
          pageSize: templates.length,
          totalPages: 1,
          hasNextPage: false,
          hasPreviousPage: false
        }
      } else {
        result = await questionnaireApiService.searchQuestionnaires(filters)
      }

      setQuestionnaires(result.questionnaires)
      setPagination({
        page: result.page,
        pageSize: result.pageSize,
        totalCount: result.totalCount,
        totalPages: result.totalPages,
        hasNextPage: result.hasNextPage,
        hasPreviousPage: result.hasPreviousPage
      })
    } catch (err) {
      logger.error('Error loading questionnaires', err, { component: 'QuestionnaireManager' })
      setError(err instanceof Error ? err.message : 'Failed to load questionnaires')
    } finally {
      setLoading(false)
    }
  }, [showTemplatesOnly, filters])

  useEffect(() => {
    loadQuestionnaires()
  }, [loadQuestionnaires])

  const handleSearch = (searchTerm: string) => {
    setFilters(prev => ({
      ...prev,
      searchTerm,
      page: 1
    }))
  }

  const handleFilterChange = (key: keyof QuestionnaireSearchRequest, value: any) => {
    setFilters(prev => ({
      ...prev,
      [key]: value,
      page: 1
    }))
  }

  const handlePageChange = (newPage: number) => {
    setFilters(prev => ({
      ...prev,
      page: newPage
    }))
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this questionnaire?')) {
      return
    }

    try {
      await questionnaireApiService.deleteQuestionnaire(id)
      await loadQuestionnaires()
    } catch (err) {
      logger.error('Error deleting questionnaire', err, { component: 'QuestionnaireManager' })
      alert('Failed to delete questionnaire: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  const handleClone = async (questionnaire: QuestionnaireData) => {
    const newTitle = prompt('Enter a title for the cloned questionnaire:', `Copy of ${questionnaire.title}`)
    if (!newTitle) return

    try {
      const cloned = await questionnaireApiService.cloneQuestionnaire(questionnaire.id, newTitle)
      router.push(`/questionnaires/${cloned.id}/edit`)
    } catch (err) {
      logger.error('Error cloning questionnaire', err, { component: 'QuestionnaireManager' })
      alert('Failed to clone questionnaire: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  const handleToggleStatus = async (questionnaire: QuestionnaireData) => {
    try {
      await questionnaireApiService.setQuestionnaireStatus(questionnaire.id, !questionnaire.isActive)
      await loadQuestionnaires()
    } catch (err) {
      logger.error('Error updating questionnaire status', err, { component: 'QuestionnaireManager' })
      alert('Failed to update questionnaire status: ' + (err instanceof Error ? err.message : 'Unknown error'))
    }
  }

  if (loading && questionnaires.length === 0) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
        <span className="ml-2 text-muted-foreground">Loading questionnaires...</span>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-foreground">
            {showTemplatesOnly ? 'Questionnaire Templates' : 'My Questionnaires'}
          </h1>
          <p className="text-muted-foreground mt-1">
            {showTemplatesOnly 
              ? 'Browse and use questionnaire templates' 
              : 'Create and manage your dynamic questionnaires'}
          </p>
        </div>

        {!showTemplatesOnly && (
          <button
            onClick={() => router.push('/questionnaires/create')}
            className="bg-primary text-primary-foreground px-4 py-2 rounded-lg hover:bg-primary/90 transition-colors"
          >
            Create Questionnaire
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="bg-card p-6 rounded-lg shadow-sm border border-border">
        <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {/* Search */}
          <div>
            <label className="block text-sm font-medium text-foreground mb-1">Search</label>
            <input
              type="text"
              value={filters.searchTerm || ''}
              onChange={(e) => handleSearch(e.target.value)}
              placeholder="Search questionnaires..."
              className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            />
          </div>

          {/* Type Filter */}
          <div>
            <label className="block text-sm font-medium text-foreground mb-1">Type</label>
            <select
              value={filters.type ?? ''}
              onChange={(e) => handleFilterChange('type', e.target.value ? parseInt(e.target.value) : undefined)}
              className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            >
              <option value="">All Types</option>
              {Object.entries(QuestionnaireType)
                .filter(([key]) => isNaN(Number(key)))
                .map(([key, value]) => (
                  <option key={key} value={value}>
                    {getQuestionnaireTypeLabel(value as QuestionnaireType)}
                  </option>
                ))}
            </select>
          </div>

          {/* Status Filter */}
          {!showTemplatesOnly && (
            <div>
              <label className="block text-sm font-medium text-foreground mb-1">Status</label>
              <select
                value={filters.isActive === undefined ? '' : filters.isActive.toString()}
                onChange={(e) => handleFilterChange('isActive', e.target.value === '' ? undefined : e.target.value === 'true')}
                className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              >
                <option value="">All Status</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
          )}

          {/* Sort By */}
          <div>
            <label className="block text-sm font-medium text-foreground mb-1">Sort By</label>
            <select
              value={filters.sortBy || 'UpdatedAt'}
              onChange={(e) => handleFilterChange('sortBy', e.target.value)}
              className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            >
              <option value="UpdatedAt">Last Updated</option>
              <option value="CreatedAt">Date Created</option>
              <option value="Title">Title</option>
              <option value="Type">Type</option>
            </select>
          </div>
        </div>
      </div>

      {/* Error Display */}
      {error && (
        <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-destructive" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-destructive">Error</h3>
              <div className="mt-2 text-sm text-destructive/80">{error}</div>
            </div>
          </div>
        </div>
      )}

      {/* Questionnaires Grid */}
      {questionnaires.length === 0 && !loading ? (
        <div className="text-center py-12">
          <svg className="mx-auto h-12 w-12 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-foreground">No questionnaires found</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {showTemplatesOnly 
              ? 'No templates are currently available.' 
              : 'Get started by creating your first questionnaire.'}
          </p>
          {!showTemplatesOnly && (
            <div className="mt-6">
              <button
                onClick={() => router.push('/questionnaires/create')}
                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-primary-foreground bg-primary hover:bg-primary/90"
              >
                Create Questionnaire
              </button>
            </div>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {questionnaires.map((questionnaire) => (
            <div key={questionnaire.id} className="bg-card rounded-lg shadow-sm border border-border hover:shadow-md transition-shadow">
              <div className="p-6">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <h3 className="text-lg font-semibold text-foreground mb-2">
                      {questionnaire.title}
                    </h3>

                    {questionnaire.description && (
                      <p className="text-muted-foreground text-sm mb-3 line-clamp-2">
                        {questionnaire.description}
                      </p>
                    )}

                    <div className="space-y-2">
                      <div className="flex items-center text-sm text-muted-foreground">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary/10 text-primary">
                          {getQuestionnaireTypeLabel(questionnaire.type)}
                        </span>
                        {questionnaire.isTemplate && (
                          <span className="ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-secondary text-secondary-foreground">
                            Template
                          </span>
                        )}
                        {!questionnaire.isActive && (
                          <span className="ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-muted text-muted-foreground">
                            Inactive
                          </span>
                        )}
                      </div>

                      <div className="text-sm text-muted-foreground">
                        {questionnaire.questionCount} questions • {questionnaire.responseCount} responses
                      </div>

                      <div className="text-xs text-muted-foreground/70">
                        Updated {new Date(questionnaire.updatedAt).toLocaleDateString()}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Actions */}
                <div className="mt-4 flex flex-wrap gap-2">
                  {onSelectQuestionnaire ? (
                    <button
                      onClick={() => onSelectQuestionnaire(questionnaire)}
                      className="flex-1 bg-primary text-primary-foreground px-3 py-2 rounded text-sm hover:bg-primary/90 transition-colors"
                    >
                      Select
                    </button>
                  ) : (
                    <>
                      <button
                        onClick={() => router.push(`/questionnaires/${questionnaire.id}`)}
                        className="flex-1 bg-primary text-primary-foreground px-3 py-2 rounded text-sm hover:bg-primary/90 transition-colors"
                      >
                        View
                      </button>

                      {!showTemplatesOnly && (
                        <>
                          <button
                            onClick={() => router.push(`/questionnaires/${questionnaire.id}/edit`)}
                            className="px-3 py-2 border border-border text-foreground rounded text-sm hover:bg-muted transition-colors"
                          >
                            Edit
                          </button>

                          <button
                            onClick={() => handleClone(questionnaire)}
                            className="px-3 py-2 border border-border text-foreground rounded text-sm hover:bg-muted transition-colors"
                          >
                            Clone
                          </button>

                          <button
                            onClick={() => handleToggleStatus(questionnaire)}
                            className={`px-3 py-2 rounded text-sm transition-colors ${
                              questionnaire.isActive
                                ? 'bg-warning/10 text-warning hover:bg-warning/20'
                                : 'bg-success/10 text-success hover:bg-success/20'
                            }`}
                          >
                            {questionnaire.isActive ? 'Deactivate' : 'Activate'}
                          </button>

                          <button
                            onClick={() => handleDelete(questionnaire.id)}
                            className="px-3 py-2 bg-destructive/10 text-destructive rounded text-sm hover:bg-destructive/20 transition-colors"
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Pagination */}
      {pagination.totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-border bg-card px-4 py-3 sm:px-6">
          <div className="flex flex-1 justify-between sm:hidden">
            <button
              onClick={() => handlePageChange(pagination.page - 1)}
              disabled={!pagination.hasPreviousPage}
              className="relative inline-flex items-center px-4 py-2 text-sm font-medium text-foreground bg-card border border-border rounded-md hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Previous
            </button>
            <button
              onClick={() => handlePageChange(pagination.page + 1)}
              disabled={!pagination.hasNextPage}
              className="relative ml-3 inline-flex items-center px-4 py-2 text-sm font-medium text-foreground bg-card border border-border rounded-md hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
            </button>
          </div>

          <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-foreground">
                Showing{' '}
                <span className="font-medium">
                  {(pagination.page - 1) * pagination.pageSize + 1}
                </span>{' '}
                to{' '}
                <span className="font-medium">
                  {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)}
                </span>{' '}
                of{' '}
                <span className="font-medium">{pagination.totalCount}</span> results
              </p>
            </div>
            
            <div>
              <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px" aria-label="Pagination">
                <button
                  onClick={() => handlePageChange(pagination.page - 1)}
                  disabled={!pagination.hasPreviousPage}
                  className="relative inline-flex items-center px-2 py-2 text-sm font-medium text-muted-foreground bg-card border border-border rounded-l-md hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>

                {/* Page numbers */}
                {Array.from({ length: Math.min(5, pagination.totalPages) }, (_, i) => {
                  const page = i + 1;
                  return (
                    <button
                      key={page}
                      onClick={() => handlePageChange(page)}
                      className={`relative inline-flex items-center px-4 py-2 text-sm font-medium border ${
                        page === pagination.page
                          ? 'z-10 bg-primary/10 border-primary text-primary'
                          : 'bg-card border-border text-muted-foreground hover:bg-muted'
                      }`}
                    >
                      {page}
                    </button>
                  );
                })}

                <button
                  onClick={() => handlePageChange(pagination.page + 1)}
                  disabled={!pagination.hasNextPage}
                  className="relative inline-flex items-center px-2 py-2 text-sm font-medium text-muted-foreground bg-card border border-border rounded-r-md hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </nav>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}