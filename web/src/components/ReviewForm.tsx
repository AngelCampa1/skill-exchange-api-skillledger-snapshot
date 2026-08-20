'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import { Star, Send } from 'lucide-react'

interface ReviewFormProps {
  projectId: string
  projectTitle: string
  providerName?: string
  onSuccess?: () => void
  onCancel?: () => void
}

export default function ReviewForm({
  projectId,
  projectTitle,
  providerName,
  onSuccess,
  onCancel
}: ReviewFormProps) {
  const [rating, setRating] = useState(0)
  const [hoverRating, setHoverRating] = useState(0)
  const [reviewText, setReviewText] = useState('')
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
      logger.error('Failed to get CSRF token', error, { component: 'ReviewForm' })
    }
    
    return null
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (rating === 0) {
      setError('Please select a rating.')
      return
    }

    if (reviewText.length < 100) {
      setError('Review must be at least 100 characters.')
      return
    }

    if (reviewText.length > 1000) {
      setError('Review cannot exceed 1000 characters.')
      return
    }

    setIsLoading(true)

    try {
      const csrfToken = await getCsrfToken()
      if (!csrfToken) {
        throw new Error('Failed to get CSRF token')
      }

      const response = await fetch(`/api/review/project/${projectId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include',
        body: JSON.stringify({
          rating,
          reviewText,
        }),
      })

      const result = await response.json()

      if (response.ok) {
        logger.info('Review submitted', { result })
        if (onSuccess) {
          onSuccess()
        }
      } else {
        logger.error('Failed to submit review', undefined, { result, component: 'ReviewForm' })
        setError(result.message || 'Failed to submit review. Please try again.')
      }
    } catch (error: any) {
      logger.error('Error submitting review', error, { component: 'ReviewForm' })
      setError('Network error. Please check your connection and try again.')
    } finally {
      setIsLoading(false)
    }
  }

  const characterCount = reviewText.length
  const isValidLength = characterCount >= 100 && characterCount <= 1000

  return (
    <div className="bg-card rounded-lg shadow-lg p-6">
      <h2 className="text-2xl font-bold text-foreground mb-2">Leave a Review</h2>
      <p className="text-muted-foreground mb-6">
        {projectTitle}
        {providerName && <span className="block text-sm mt-1">Provider: {providerName}</span>}
      </p>

      {error && (
        <div className="mb-4 bg-destructive/10 border border-destructive/20 rounded-md p-4">
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Star Rating */}
        <div>
          <label className="block text-sm font-medium text-foreground mb-3">
            Rating <span className="text-destructive">*</span>
          </label>
          <div className="flex items-center space-x-2">
            {[1, 2, 3, 4, 5].map((star) => (
              <button
                key={star}
                type="button"
                onClick={() => setRating(star)}
                onMouseEnter={() => setHoverRating(star)}
                onMouseLeave={() => setHoverRating(0)}
                className="focus:outline-none"
                disabled={isLoading}
              >
                <Star
                  className={`w-10 h-10 transition-colors ${
                    star <= (hoverRating || rating)
                      ? 'fill-warning text-warning'
                      : 'text-muted-foreground'
                  }`}
                />
              </button>
            ))}
            <span className="ml-4 text-muted-foreground">
              {rating > 0 ? `${rating} star${rating !== 1 ? 's' : ''}` : 'Select rating'}
            </span>
          </div>
        </div>

        {/* Review Text */}
        <div>
          <label htmlFor="reviewText" className="block text-sm font-medium text-foreground mb-2">
            Your Review <span className="text-destructive">*</span>
          </label>
          <textarea
            id="reviewText"
            name="reviewText"
            placeholder="Share your experience working with this provider. What did they do well? What could be improved?"
            value={reviewText}
            onChange={(e) => setReviewText(e.target.value)}
            className={`w-full h-40 px-4 py-3 border rounded-lg focus:ring-2 focus:ring-ring focus:border-primary resize-none ${
              characterCount > 0 && !isValidLength ? 'border-destructive/50' : 'border-input'
            }`}
            required
            disabled={isLoading}
            data-testid="review-text"
          />
          <div className="flex justify-between mt-2">
            <p className={`text-sm ${
              characterCount < 100 ? 'text-destructive' : characterCount > 1000 ? 'text-destructive' : 'text-muted-foreground'
            }`}>
              {characterCount} / 1000 characters {characterCount < 100 && `(minimum 100)`}
            </p>
            {isValidLength && (
              <p className="text-sm text-success">✓ Valid</p>
            )}
          </div>
        </div>

        <div className="bg-primary/10 border border-primary/20 rounded-lg p-4">
          <p className="text-sm text-primary">
            ℹ️ Your review will be visible on the provider's profile and will help other clients make informed decisions.
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
            disabled={isLoading || rating === 0 || !isValidLength}
            className={`px-6 py-2 rounded-full text-primary-foreground font-medium flex items-center space-x-2 ${
              isLoading || rating === 0 || !isValidLength
                ? 'bg-muted cursor-not-allowed'
                : 'bg-primary hover:bg-primary/90'
            }`}
            data-testid="submit-review-button"
          >
            {isLoading ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary-foreground"></div>
                <span>Submitting...</span>
              </>
            ) : (
              <>
                <Send className="w-4 h-4" />
                <span>Submit Review</span>
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  )
}



