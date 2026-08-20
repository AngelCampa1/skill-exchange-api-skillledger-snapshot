'use client'

import React from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { feedbackApiService, FeedbackCategory } from '@/services/feedbackApiService'

const feedbackSchema = z.object({
  category: z.enum(['General', 'Bug', 'FeatureRequest', 'Other'] as const, {
    required_error: 'Please select a category',
  }),
  message: z.string()
    .min(10, 'Message must be at least 10 characters')
    .max(2000, 'Message cannot exceed 2000 characters'),
  replyToEmail: z.string()
    .email('Please enter a valid email address')
    .optional()
    .or(z.literal('')),
})

type FeedbackFormData = z.infer<typeof feedbackSchema>

interface FeedbackFormProps {
  onSuccess?: () => void
  onError?: (error: string) => void
  userEmail?: string
}

const categoryOptions: { value: FeedbackCategory; label: string; description: string }[] = [
  { value: 'General', label: 'General Feedback', description: 'Share your thoughts about SkillLedger' },
  { value: 'Bug', label: 'Bug Report', description: 'Report something that is not working correctly' },
  { value: 'FeatureRequest', label: 'Feature Request', description: 'Suggest a new feature or improvement' },
  { value: 'Other', label: 'Other', description: 'Anything else you want to share' },
]

export default function FeedbackForm({ onSuccess, onError, userEmail }: FeedbackFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FeedbackFormData>({
    resolver: zodResolver(feedbackSchema),
    defaultValues: {
      category: undefined,
      message: '',
      replyToEmail: userEmail || '',
    },
  })

  const messageLength = watch('message')?.length || 0

  const onSubmit = async (data: FeedbackFormData) => {
    try {
      await feedbackApiService.submitFeedback({
        category: data.category,
        message: data.message,
        replyToEmail: data.replyToEmail || undefined,
      })
      reset()
      onSuccess?.()
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to submit feedback'
      onError?.(errorMessage)
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* Category Selection */}
      <div>
        <label htmlFor="category" className="block text-sm font-medium text-foreground mb-1">
          Category <span className="text-destructive">*</span>
        </label>
        <select
          id="category"
          {...register('category')}
          className={`w-full px-3 py-2 border rounded-lg shadow-sm focus:ring-2 focus:ring-ring ${
            errors.category ? 'border-destructive' : 'border-input'
          }`}
        >
          <option value="">Select a category...</option>
          {categoryOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        {errors.category && (
          <p className="mt-1 text-sm text-destructive">{errors.category.message}</p>
        )}
      </div>

      {/* Message Textarea */}
      <div>
        <label htmlFor="message" className="block text-sm font-medium text-foreground mb-1">
          Your Feedback <span className="text-destructive">*</span>
        </label>
        <textarea
          id="message"
          rows={5}
          {...register('message')}
          placeholder="Tell us what's on your mind..."
          className={`w-full px-3 py-2 border rounded-lg shadow-sm focus:ring-2 focus:ring-ring resize-none ${
            errors.message ? 'border-destructive' : 'border-input'
          }`}
        />
        <div className="flex justify-between mt-1">
          {errors.message ? (
            <p className="text-sm text-destructive">{errors.message.message}</p>
          ) : (
            <span />
          )}
          <span className={`text-sm ${messageLength > 1800 ? 'text-warning' : 'text-muted-foreground'}`}>
            {messageLength}/2000
          </span>
        </div>
      </div>

      {/* Reply Email (Optional) */}
      <div>
        <label htmlFor="replyToEmail" className="block text-sm font-medium text-foreground mb-1">
          Email for Reply <span className="text-muted-foreground">(optional)</span>
        </label>
        <input
          type="email"
          id="replyToEmail"
          {...register('replyToEmail')}
          placeholder="your@email.com"
          className={`w-full px-3 py-2 border rounded-lg shadow-sm focus:ring-2 focus:ring-ring ${
            errors.replyToEmail ? 'border-destructive' : 'border-input'
          }`}
        />
        {errors.replyToEmail && (
          <p className="mt-1 text-sm text-destructive">{errors.replyToEmail.message}</p>
        )}
        <p className="mt-1 text-xs text-muted-foreground">
          Provide your email if you&apos;d like us to follow up with you.
        </p>
      </div>

      {/* Submit Button */}
      <button
        type="submit"
        disabled={isSubmitting}
        className={`w-full py-2.5 px-4 rounded-full font-medium transition-colors ${
          isSubmitting
            ? 'bg-muted text-muted-foreground cursor-not-allowed'
            : 'bg-success text-success-foreground hover:bg-success/90 focus:ring-2 focus:ring-ring focus:ring-offset-2'
        }`}
      >
        {isSubmitting ? (
          <span className="flex items-center justify-center gap-2">
            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
                fill="none"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
              />
            </svg>
            Submitting...
          </span>
        ) : (
          'Submit Feedback'
        )}
      </button>
    </form>
  )
}
