'use client'

import { useState, useEffect, FormEvent } from'react'
import Link from'next/link'
import { useExitIntent } from'@/hooks/useExitIntent'
import { trackEvent } from'@/utils/analytics'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from'@/components/ui/dialog'

export function ExitIntentPopup() {
  const { showPopup, dismissPopup } = useExitIntent()
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)

  // Track when popup is shown
  useEffect(() => {
    if (showPopup) {
      trackEvent({
        name:'exit_intent_shown',
        category:'ui_interaction',
        priority:'medium',
      })
    }
  }, [showPopup])

  function handleDismiss() {
    trackEvent({
      name:'exit_intent_dismissed',
      category:'ui_interaction',
      priority:'low',
    })
    dismissPopup()
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) return

    // TODO: POST email to newsletter API endpoint when available
    setSubmitted(true)
    trackEvent({
      name:'exit_intent_converted',
      category:'ui_interaction',
      priority:'high',
      properties: { captured: true },
    })

    // Close after 3 seconds
    setTimeout(() => {
      dismissPopup()
    }, 3000)
  }

  return (
    <Dialog open={showPopup} onOpenChange={(open) => { if (!open) handleDismiss() }}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="text-2xl">Wait — Get Your Free Exchange Starter Kit</DialogTitle>
          <DialogDescription className="text-base">
            We&apos;ll send you a step-by-step guide to your first skill exchange, plus a credit valuation cheat sheet.
          </DialogDescription>
        </DialogHeader>

        {submitted ? (
          <div className="py-4 text-center space-y-3">
            <p className="text-lg font-semibold text-green-600">
              Thanks! We&apos;ll let you know when the starter kit is ready.
            </p>
            <Link href="/categories" className="text-sm text-primary hover:underline">
              Explore skill categories while you wait
            </Link>
          </div>
        ) : (
          <div className="space-y-4 pt-2">
            <form onSubmit={handleSubmit} className="space-y-3">
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Enter your email"
                aria-label="Email for free guide"
                className="w-full rounded-lg border border-border bg-background px-4 py-3 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
              />
              <button
                type="submit"
                className="w-full rounded-full bg-primary px-4 py-3 text-sm font-semibold text-primary-foreground hover:bg-primary/90 transition-colors"
              >
                Send My Free Kit
              </button>
            </form>
            <button
              type="button"
              onClick={handleDismiss}
              className="w-full text-center text-sm text-muted-foreground hover:text-foreground transition-colors py-1"
            >
              I prefer paying cash for services
            </button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
