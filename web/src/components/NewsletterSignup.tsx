'use client'

import { useState, FormEvent } from'react'
import { useFormTracking } from'@/hooks/useFormTracking'
import { trackEvent } from'@/utils/analytics'

interface NewsletterSignupProps {
  variant?:'inline' |'footer' |'section'
}

export function NewsletterSignup({ variant ='inline' }: NewsletterSignupProps) {
  const [email, setEmail] = useState('')
  const [status, setStatus] = useState<'idle' |'success' |'error'>('idle')
  const [errorMessage, setErrorMessage] = useState('')

  const { trackFieldChange, trackFormSubmit, trackValidationError } = useFormTracking({
    formName:'newsletter_signup',
    category:'forms',
  })

  function validateEmail(value: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault()

    if (!validateEmail(email)) {
      setStatus('error')
      setErrorMessage('Please enter a valid email address.')
      trackValidationError(['email'])
      return
    }

    // Simulate successful signup (no API endpoint yet)
    setStatus('success')
    trackFormSubmit(true)
    trackEvent({
      name:'newsletter_signup',
      category:'forms',
      priority:'high',
      properties: { variant },
    })
  }

  if (variant ==='footer') {
    return (
      <div className="mt-6">
        <h3 className="font-bold text-sm tracking-tight mb-3">Weekly Tips</h3>
        <p className="text-xs text-muted-foreground mb-2">Skill exchange trends and tips.</p>
        {status ==='success' ? (
          <p className="text-sm text-green-600">Thanks for subscribing!</p>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-2">
            <input
              type="email"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value)
                trackFieldChange('email')
              }}
              placeholder="Your email"
              aria-label="Email for newsletter"
              className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
            />
            <button
              type="submit"
              className="w-full rounded-full bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors"
            >
              Get Tips
            </button>
            {status ==='error' && (
              <p className="text-xs text-red-500">{errorMessage}</p>
            )}
          </form>
        )}
      </div>
    )
  }

  if (variant ==='section') {
    return (
      <section className="py-24 lg:py-32 bg-muted/30">
        <div className="container-premium text-center">
          <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
            <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
              Get Weekly Skill Exchange Insights
            </span>
          </h2>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto mb-8">
            Get weekly insights on skill exchange trends, tips for successful collaborations, and platform updates delivered to your inbox.
          </p>
          {status ==='success' ? (
            <p className="text-lg font-medium text-green-600">
              You&apos;re subscribed! Check your inbox for a welcome email.
            </p>
          ) : (
            <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3 max-w-md mx-auto">
              <input
                type="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value)
                  trackFieldChange('email')
                }}
                placeholder="Enter your email"
                aria-label="Email for newsletter"
                className="flex-1 rounded-lg border border-border bg-background px-4 py-3 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
              />
              <button
                type="submit"
                className="rounded-full bg-primary px-6 py-3 text-sm font-semibold text-primary-foreground hover:bg-primary/90 transition-colors shadow-lg hover:shadow-xl"
              >
                Get Weekly Tips
              </button>
            </form>
          )}
          <p className="text-xs text-muted-foreground mt-3">One email per week. Unsubscribe anytime.</p>
          {status ==='error' && (
            <p className="mt-3 text-sm text-red-500">{errorMessage}</p>
          )}
        </div>
      </section>
    )
  }

  // Default: inline variant
  return (
    <div className="my-12 rounded-xl border border-border bg-muted/30 p-6 sm:p-8">
      <h3 className="text-lg font-bold mb-2">Enjoyed this article?</h3>
      <p className="text-sm text-muted-foreground mb-4">
        Get more insights on skill exchange delivered to your inbox every week.
      </p>
      {status ==='success' ? (
        <p className="text-sm font-medium text-green-600">
          You&apos;re subscribed! Check your inbox for a welcome email.
        </p>
      ) : (
        <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3">
          <input
            type="email"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value)
              trackFieldChange('email')
            }}
            placeholder="Enter your email"
            aria-label="Email for newsletter"
            className="flex-1 rounded-lg border border-border bg-background px-4 py-2.5 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
          />
          <button
            type="submit"
            className="rounded-full bg-primary px-5 py-2.5 text-sm font-semibold text-primary-foreground hover:bg-primary/90 transition-colors"
          >
            Get More Like This
          </button>
        </form>
      )}
      {status ==='error' && (
        <p className="mt-2 text-xs text-red-500">{errorMessage}</p>
      )}
    </div>
  )
}
