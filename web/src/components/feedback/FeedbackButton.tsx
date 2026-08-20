'use client'

import React, { useState, useEffect } from 'react'
import FeedbackForm from './FeedbackForm'

export default function FeedbackButton() {
  const [isOpen, setIsOpen] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)
  const [showError, setShowError] = useState<string | null>(null)

  // Close modal on escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('keydown', handleEscape)
      // Prevent body scroll when modal is open
      document.body.style.overflow = 'hidden'
    }

    return () => {
      document.removeEventListener('keydown', handleEscape)
      document.body.style.overflow = 'unset'
    }
  }, [isOpen])

  const handleSuccess = () => {
    setShowSuccess(true)
    setShowError(null)
    // Auto-close after showing success
    setTimeout(() => {
      setShowSuccess(false)
      setIsOpen(false)
    }, 2000)
  }

  const handleError = (error: string) => {
    setShowError(error)
    setShowSuccess(false)
  }

  const handleClose = () => {
    setIsOpen(false)
    setShowSuccess(false)
    setShowError(null)
  }

  return (
    <>
      {/* BUG-006 FIX: Floating Button - smaller on mobile to avoid content overlap */}
      <button
        onClick={() => setIsOpen(true)}
        className="fixed bottom-4 right-4 z-40 flex items-center justify-center gap-2 w-10 h-10 sm:w-auto sm:h-auto sm:px-4 sm:py-3 bg-success hover:bg-success/90 text-success-foreground rounded-full shadow-lg hover:shadow-xl transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        aria-label="Send feedback"
      >
        {/* Feedback Icon */}
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className="h-5 w-5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2}
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"
          />
        </svg>
        <span className="font-medium text-sm hidden sm:inline">Feedback</span>
      </button>

      {/* Modal Backdrop */}
      {isOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4"
          role="dialog"
          aria-modal="true"
          aria-labelledby="feedback-modal-title"
        >
          {/* Backdrop */}
          <div
            className="absolute inset-0 bg-overlay/70 backdrop-blur-sm"
            onClick={handleClose}
            aria-hidden="true"
          />

          {/* Modal Content */}
          <div className="relative w-full max-w-md bg-card rounded-xl shadow-2xl transform transition-all">
            {/* Header */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-border">
              <h2 id="feedback-modal-title" className="text-lg font-semibold text-foreground">
                Send Feedback
              </h2>
              <button
                onClick={handleClose}
                className="p-1 rounded-full text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                aria-label="Close feedback form"
              >
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Body */}
            <div className="px-6 py-4">
              {showSuccess ? (
                <div className="flex flex-col items-center py-8 text-center">
                  <div className="w-16 h-16 bg-success/10 rounded-full flex items-center justify-center mb-4">
                    <svg className="w-8 h-8 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                  </div>
                  <h3 className="text-lg font-medium text-foreground mb-2">Thank you!</h3>
                  <p className="text-muted-foreground">Your feedback has been submitted successfully.</p>
                </div>
              ) : (
                <>
                  {showError && (
                    <div className="mb-4 p-3 bg-destructive/10 border border-destructive/20 rounded-lg">
                      <p className="text-sm text-destructive">{showError}</p>
                    </div>
                  )}
                  <p className="text-sm text-muted-foreground mb-4">
                    We appreciate your feedback! Let us know how we can improve SkillLedger.
                  </p>
                  <FeedbackForm onSuccess={handleSuccess} onError={handleError} />
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  )
}
