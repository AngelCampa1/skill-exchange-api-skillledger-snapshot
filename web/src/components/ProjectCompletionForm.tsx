'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import { CheckCircle, Send } from 'lucide-react'

interface ProjectCompletionFormProps {
  projectId: string
  projectTitle: string
  onSuccess?: () => void
  onCancel?: () => void
}

export default function ProjectCompletionForm({
  projectId,
  projectTitle,
  onSuccess,
  onCancel
}: ProjectCompletionFormProps) {
  const [deliverablesConfirmed, setDeliverablesConfirmed] = useState(false)
  const [qualityConfirmed, setQualityConfirmed] = useState(false)
  const [notes, setNotes] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
      logger.error('Failed to get CSRF token', error, { component: 'ProjectCompletionForm' })
    }
    
    return null
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (!deliverablesConfirmed || !qualityConfirmed) {
      setError('Please confirm all deliverables have been met.')
      return
    }

    setIsLoading(true)

    try {
      const csrfToken = await getCsrfToken()
      if (!csrfToken) {
        throw new Error('Failed to get CSRF token')
      }

      const response = await fetch(`/api/project/${projectId}/complete`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include',
        body: JSON.stringify({
          notes: notes || undefined
        }),
      })

      const result = await response.json()

      if (response.ok) {
        logger.info('Project marked as complete', { result })
        if (onSuccess) {
          onSuccess()
        }
      } else {
        logger.error('Failed to complete project', undefined, { result, component: 'ProjectCompletionForm' })
        setError(result.message || 'Failed to mark project as complete. Please try again.')
      }
    } catch (error: any) {
      logger.error('Error completing project', error, { component: 'ProjectCompletionForm' })
      setError('Network error. Please check your connection and try again.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="bg-card rounded-lg shadow-lg p-6">
      <h2 className="text-2xl font-bold text-foreground mb-2">Complete Project</h2>
      <p className="text-muted-foreground mb-6">{projectTitle}</p>

      {error && (
        <div className="mb-4 bg-destructive/10 border border-destructive/20 rounded-md p-4">
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Deliverables Confirmation */}
        <div className="bg-primary/10 border border-primary/20 rounded-lg p-4">
          <h3 className="font-medium text-primary mb-3">Before completing this project:</h3>
          <div className="space-y-3">
            <div className="flex items-start">
              <input
                type="checkbox"
                id="deliverablesConfirmed"
                checked={deliverablesConfirmed}
                onChange={(e) => setDeliverablesConfirmed(e.target.checked)}
                className="mt-1 w-4 h-4 text-primary border-border rounded focus:ring-ring"
                disabled={isLoading}
              />
              <label htmlFor="deliverablesConfirmed" className="ml-3 text-sm text-primary">
                All project deliverables have been completed and reviewed
              </label>
            </div>
            <div className="flex items-start">
              <input
                type="checkbox"
                id="qualityConfirmed"
                checked={qualityConfirmed}
                onChange={(e) => setQualityConfirmed(e.target.checked)}
                className="mt-1 w-4 h-4 text-primary border-border rounded focus:ring-ring"
                disabled={isLoading}
              />
              <label htmlFor="qualityConfirmed" className="ml-3 text-sm text-primary">
                The work meets the agreed-upon quality standards
              </label>
            </div>
          </div>
        </div>

        {/* Optional Notes */}
        <div>
          <label htmlFor="notes" className="block text-sm font-medium text-foreground mb-2">
            Completion Notes (Optional)
          </label>
          <textarea
            id="notes"
            name="notes"
            placeholder="Add any final notes about the project completion..."
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={500}
            className="w-full h-24 px-4 py-3 border border-input rounded-lg focus:ring-2 focus:ring-ring focus:border-primary resize-none"
            disabled={isLoading}
          />
          <p className="text-sm text-muted-foreground mt-1">{notes.length} / 500 characters</p>
        </div>

        <div className="bg-warning/10 border border-warning/20 rounded-lg p-4">
          <p className="text-sm text-warning">
            ⚠️ Once you mark the project as complete, the provider will be notified and the project will move to the approval phase.
          </p>
        </div>

        {/* Action Buttons */}
        <div className="flex justify-end space-x-4 pt-4 border-t border-border">
          {onCancel && (
            <button
              type="button"
              onClick={onCancel}
              className="px-6 py-2 border border-border rounded-full text-foreground hover:bg-muted font-medium"
              disabled={isLoading}
            >
              Cancel
            </button>
          )}
          <button
            type="submit"
            disabled={isLoading || !deliverablesConfirmed || !qualityConfirmed}
            className={`px-6 py-2 rounded-full font-medium flex items-center space-x-2 ${
              isLoading || !deliverablesConfirmed || !qualityConfirmed
                ? 'bg-muted text-muted-foreground cursor-not-allowed'
                : 'bg-success text-success-foreground hover:bg-success/90'
            }`}
            data-testid="complete-project-button"
          >
            {isLoading ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-success-foreground"></div>
                <span>Processing...</span>
              </>
            ) : (
              <>
                <CheckCircle className="w-4 h-4" />
                <span>Mark as Complete</span>
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  )
}



