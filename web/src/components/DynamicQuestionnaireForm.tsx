'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { z, ZodString } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'

// Types matching the backend DTOs
export enum QuestionType {
  Text = 0,
  LongText = 1,
  Number = 2,
  Email = 3,
  Phone = 4,
  Date = 5,
  Time = 6,
  DateTime = 7,
  Boolean = 8,
  Radio = 9,
  Checkbox = 10,
  Dropdown = 11,
  MultipleChoice = 12,
  Rating = 13,
  FileUpload = 14,
  Url = 15
}

export interface QuestionOption {
  id: string
  optionText: string
  optionValue?: string
  displayOrder: number
  isDefault: boolean
}

export interface QuestionnaireQuestion {
  id: string
  questionText: string
  description?: string
  type: QuestionType
  isRequired: boolean
  displayOrder: number
  defaultValue?: string
  placeholderText?: string
  validationRegex?: string
  validationMessage?: string
  minValue?: number
  maxValue?: number
  options: QuestionOption[]
}

export interface QuestionnaireData {
  id: string
  title: string
  description?: string
  questions: QuestionnaireQuestion[]
}

export interface QuestionResponse {
  questionId: string
  responseValue?: string
  selectedOptionIds?: string[]
  fileAttachments?: string[]
}

interface DynamicQuestionnaireFormProps {
  questionnaire: QuestionnaireData
  initialResponses?: QuestionResponse[]
  onSubmit: (responses: QuestionResponse[]) => Promise<void>
  onSaveDraft?: (responses: QuestionResponse[]) => Promise<void>
  isLoading?: boolean
  isReadOnly?: boolean
}

// Create dynamic validation schema based on questionnaire questions
const createValidationSchema = (questions: QuestionnaireQuestion[]) => {
  const schemaObject: Record<string, z.ZodTypeAny> = {}

  questions.forEach(question => {
    let fieldSchema: z.ZodTypeAny

    switch (question.type) {
      case QuestionType.Email:
        fieldSchema = z.string().email('Please enter a valid email address')
        break
      case QuestionType.Url:
        fieldSchema = z.string().url('Please enter a valid URL')
        break
      case QuestionType.Number:
        fieldSchema = z.string().refine(val => !isNaN(Number(val)), 'Please enter a valid number')
        if (question.minValue !== undefined || question.maxValue !== undefined) {
          fieldSchema = fieldSchema.refine(val => {
            const num = Number(val)
            if (question.minValue !== undefined && num < question.minValue) return false
            if (question.maxValue !== undefined && num > question.maxValue) return false
            return true
          }, `Value must be between ${question.minValue ?? '-∞'} and ${question.maxValue ?? '∞'}`)
        }
        break
      case QuestionType.Phone:
        fieldSchema = z.string().regex(/^[\+]?[1-9][\d]{0,15}$/, 'Please enter a valid phone number')
        break
      case QuestionType.Date:
        fieldSchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Please enter a valid date (YYYY-MM-DD)')
        break
      default:
        fieldSchema = z.string()
        if (question.minValue !== undefined) {
          fieldSchema = (fieldSchema as ZodString).min(question.minValue, `Minimum length is ${question.minValue} characters`)
        }
        if (question.maxValue !== undefined) {
          fieldSchema = (fieldSchema as ZodString).max(question.maxValue, `Maximum length is ${question.maxValue} characters`)
        }
        if (question.validationRegex) {
          try {
            const regex = new RegExp(question.validationRegex)
            fieldSchema = (fieldSchema as ZodString).regex(regex, question.validationMessage || 'Invalid format')
          } catch (e) {
            logger.warn('Invalid regex pattern', { pattern: question.validationRegex })
          }
        }
    }

    if (!question.isRequired) {
      fieldSchema = fieldSchema.optional().or(z.literal(''))
    }

    schemaObject[question.id] = fieldSchema
  })

  return z.object(schemaObject)
}

export default function DynamicQuestionnaireForm({
  questionnaire,
  initialResponses = [],
  onSubmit,
  onSaveDraft,
  isLoading = false,
  isReadOnly = false
}: DynamicQuestionnaireFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isSavingDraft, setIsSavingDraft] = useState(false)

  // Create validation schema
  const validationSchema = createValidationSchema(questionnaire.questions)
  type FormData = z.infer<typeof validationSchema>

  const {
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isDirty },
  } = useForm<FormData>({
    resolver: zodResolver(validationSchema),
    mode: 'onChange',
  })

  // Set initial values from existing responses
  useEffect(() => {
    initialResponses.forEach(response => {
      if (response.responseValue) {
        setValue(response.questionId, response.responseValue)
      }
    })
  }, [initialResponses, setValue])

  const watchedValues = watch()

  // Auto-save draft every 30 seconds if there are changes
  const convertFormDataToResponses = useCallback((data: FormData): QuestionResponse[] => {
    return questionnaire.questions.map(question => {
      const value = data[question.id]
      return {
        questionId: question.id,
        responseValue: value || undefined,
      }
    })
  }, [questionnaire.questions])

  const handleSaveDraft = useCallback(async () => {
    if (!onSaveDraft || isSavingDraft) return

    try {
      setIsSavingDraft(true)
      const responses = convertFormDataToResponses(watchedValues)
      await onSaveDraft(responses)
    } catch (error) {
      logger.error('Error saving draft:', error)
    } finally {
      setIsSavingDraft(false)
    }
  }, [onSaveDraft, isSavingDraft, watchedValues, convertFormDataToResponses])

  useEffect(() => {
    if (!onSaveDraft || !isDirty || isReadOnly) return

    const interval = setInterval(() => {
      handleSaveDraft()
    }, 30000)

    return () => clearInterval(interval)
  }, [isDirty, onSaveDraft, isReadOnly, handleSaveDraft])

  const onFormSubmit = async (data: FormData) => {
    try {
      setIsSubmitting(true)
      const responses = convertFormDataToResponses(data)
      await onSubmit(responses)
    } catch (error) {
      logger.error('Error submitting questionnaire:', error)
    } finally {
      setIsSubmitting(false)
    }
  }

  const renderQuestion = (question: QuestionnaireQuestion) => {
    const error = errors[question.id]

    const commonProps = {
      disabled: isLoading || isSubmitting || isReadOnly,
      className: `w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring ${
        error ? 'border-destructive' : 'border-input'
      }`
    }

    switch (question.type) {
      case QuestionType.Text:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <input
                {...field}
                type="text"
                placeholder={question.placeholderText}
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.LongText:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <textarea
                {...field}
                rows={4}
                placeholder={question.placeholderText}
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.Number:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <input
                {...field}
                type="number"
                min={question.minValue}
                max={question.maxValue}
                placeholder={question.placeholderText}
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.Email:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <input
                {...field}
                type="email"
                placeholder={question.placeholderText || "Enter your email"}
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.Phone:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <input
                {...field}
                type="tel"
                placeholder={question.placeholderText || "Enter your phone number"}
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.Date:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <input
                {...field}
                type="date"
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.Url:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <input
                {...field}
                type="url"
                placeholder={question.placeholderText || "https://"}
                {...commonProps}
              />
            )}
          />
        )

      case QuestionType.Boolean:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <div className="flex items-center">
                <input
                  {...field}
                  type="checkbox"
                  checked={field.value === 'true'}
                  onChange={(e) => field.onChange(e.target.checked ? 'true' : 'false')}
                  disabled={isLoading || isSubmitting || isReadOnly}
                  className="h-4 w-4 text-primary focus:ring-ring border-input rounded"
                />
                <span className="ml-2 text-sm text-foreground">Yes</span>
              </div>
            )}
          />
        )

      case QuestionType.Radio:
      case QuestionType.MultipleChoice:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <div className="space-y-2">
                {question.options.map(option => (
                  <div key={option.id} className="flex items-center">
                    <input
                      {...field}
                      type="radio"
                      value={option.optionValue || option.optionText}
                      checked={field.value === (option.optionValue || option.optionText)}
                      disabled={isLoading || isSubmitting || isReadOnly}
                      className="h-4 w-4 text-primary focus:ring-ring border-input"
                    />
                    <label className="ml-2 text-sm text-foreground">
                      {option.optionText}
                    </label>
                  </div>
                ))}
              </div>
            )}
          />
        )

      case QuestionType.Dropdown:
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <select {...field} {...commonProps}>
                <option value="">-- Select an option --</option>
                {question.options.map(option => (
                  <option key={option.id} value={option.optionValue || option.optionText}>
                    {option.optionText}
                  </option>
                ))}
              </select>
            )}
          />
        )

      case QuestionType.Rating:
        const maxRating = question.maxValue || 5
        return (
          <Controller
            name={question.id}
            control={control}
            render={({ field }) => (
              <div className="flex space-x-2">
                {Array.from({ length: maxRating }, (_, i) => i + 1).map(rating => (
                  <button
                    key={rating}
                    type="button"
                    onClick={() => field.onChange(rating.toString())}
                    disabled={isLoading || isSubmitting || isReadOnly}
                    className={`w-10 h-10 rounded-full border-2 ${
                      parseInt(field.value || '0') >= rating
                        ? 'bg-primary border-primary text-primary-foreground'
                        : 'border-input text-muted-foreground hover:border-primary/50'
                    }`}
                  >
                    {rating}
                  </button>
                ))}
              </div>
            )}
          />
        )

      default:
        return (
          <div className="text-muted-foreground italic">
            Question type not supported: {QuestionType[question.type]}
          </div>
        )
    }
  }

  // Sort questions by display order
  const sortedQuestions = [...questionnaire.questions].sort((a, b) => a.displayOrder - b.displayOrder)

  return (
    <div className="max-w-4xl mx-auto p-6 bg-card rounded-lg shadow-md">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">{questionnaire.title}</h1>
        {questionnaire.description && (
          <p className="text-muted-foreground">{questionnaire.description}</p>
        )}
      </div>

      <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-8">
        {sortedQuestions.map((question, index) => (
          <div key={question.id} className="border-b border-border pb-6 last:border-b-0">
            <div className="mb-4">
              <label className="block text-lg font-medium text-foreground mb-2">
                {index + 1}. {question.questionText}
                {question.isRequired && <span className="text-destructive ml-1">*</span>}
              </label>

              {question.description && (
                <p className="text-sm text-muted-foreground mb-3">{question.description}</p>
              )}

              {renderQuestion(question)}

              {errors[question.id] && (
                <p className="mt-1 text-sm text-destructive">
                  {errors[question.id]?.message?.toString()}
                </p>
              )}
            </div>
          </div>
        ))}

        {!isReadOnly && (
          <div className="flex justify-between items-center pt-6 border-t border-border">
            <div className="flex items-center space-x-4">
              {onSaveDraft && (
                <button
                  type="button"
                  onClick={handleSaveDraft}
                  disabled={isSavingDraft || isLoading || isSubmitting}
                  className="px-4 py-2 text-sm font-medium text-foreground bg-muted border border-border rounded-full hover:bg-muted/80 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50"
                >
                  {isSavingDraft ? 'Saving...' : 'Save Draft'}
                </button>
              )}

              {isSavingDraft && (
                <span className="text-sm text-muted-foreground">Draft saved automatically</span>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting || isLoading}
              className="px-6 py-2 bg-primary text-primary-foreground font-medium rounded-full hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? (
                <>
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-primary-foreground inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Submitting...
                </>
              ) : (
                'Submit'
              )}
            </button>
          </div>
        )}
      </form>
    </div>
  )
}