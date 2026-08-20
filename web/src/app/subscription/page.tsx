'use client'

import { logger } from'@/utils/logger';

import Link from'next/link'
import { ArrowLeft, CheckCircle } from'lucide-react'
import { TierSelectionFlow } from'@/components/TierSelectionFlow'
import { ThemeToggle } from'@/components/ThemeToggle'
import { useAuth } from'@/contexts/AuthContext'

export default function SubscriptionPage() {
  const { user, isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading subscription options...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null // Middleware will redirect
  }

  const handleCheckoutSuccess = (result: any) => {
    logger.debug('Checkout successful:', result)
    // The user will be redirected to Stripe, so no immediate action needed
  }

  const handleCheckoutError = (error: Error) => {
    logger.error('Checkout failed:', error)
    // You could show a toast notification here
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Navigation */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="container-premium">
          <div className="flex justify-between items-center h-20">
            <Link
              href="/dashboard"
              className="flex items-center space-golden-sm text-heading text-foreground hover:text-primary transition-colors duration-300"
            >
              <ArrowLeft className="w-5 h-5 mr-2" />
              Back to Dashboard
            </Link>

            <div className="flex items-center space-golden-sm">
              <span className="text-caption text-muted-foreground">Welcome</span>
              <span className="text-body text-foreground ml-2">{user?.userName}</span>
              <ThemeToggle />
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container-premium py-12 lg:py-20">
        <div className="max-w-7xl mx-auto space-y-12">
          {/* Header */}
          <header className="text-center space-y-6 animate-fade-in">
            <div className="inline-flex items-center px-4 py-2 bg-primary/10 text-primary rounded-full text-sm font-medium">
              <CheckCircle className="w-4 h-4 mr-2" />
              Choose the perfect plan for your needs
            </div>

            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight">
              <span className="bg-gradient-to-r from-primary via-primary to-secondary bg-clip-text text-transparent">
                Upgrade Your Experience
              </span>
            </h1>

            <p className="text-lg text-muted-foreground max-w-3xl mx-auto leading-relaxed">
              Unlock premium features, increase your project limits, and take your professional collaboration to the next level with our flexible subscription plans.
            </p>
          </header>

          {/* Trust Indicators */}
          <div className="flex flex-wrap justify-center items-center gap-8 py-8 border-y border-border/50">
            <div className="flex items-center space-golden-sm">
              <div className="w-12 h-12 bg-success/10 rounded-xl flex items-center justify-center">
                <CheckCircle className="w-6 h-6 text-success" />
              </div>
              <div>
                <div className="text-subheading text-foreground">No Setup Fees</div>
                <div className="text-caption text-muted-foreground">Start instantly</div>
              </div>
            </div>

            <div className="flex items-center space-golden-sm">
              <div className="w-12 h-12 bg-primary/10 rounded-xl flex items-center justify-center">
                <CheckCircle className="w-6 h-6 text-primary" />
              </div>
              <div>
                <div className="text-subheading text-foreground">Cancel Anytime</div>
                <div className="text-caption text-muted-foreground">No long-term contracts</div>
              </div>
            </div>

            <div className="flex items-center space-golden-sm">
              <div className="w-12 h-12 bg-secondary/10 rounded-xl flex items-center justify-center">
                <CheckCircle className="w-6 h-6 text-secondary" />
              </div>
              <div>
                <div className="text-subheading text-foreground">30-Day Guarantee</div>
                <div className="text-caption text-muted-foreground">Full refund if not satisfied</div>
              </div>
            </div>
          </div>

          {/* Tier Selection Flow */}
          <section className="animate-slide-in">
            <TierSelectionFlow
              onCheckoutSuccess={handleCheckoutSuccess}
              onCheckoutError={handleCheckoutError}
            />
          </section>

          {/* FAQ Section */}
          <section className="space-y-8 py-16">
            <div className="text-center space-y-4">
              <h2 className="text-3xl lg:text-4xl font-black tracking-tight">
                <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                  Frequently Asked Questions
                </span>
              </h2>
              <p className="text-lg text-muted-foreground">
                Got questions about our subscription plans? We've got answers.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-4xl mx-auto">
              <div className="card-interactive p-6">
                <h3 className="text-subheading text-foreground mb-3">Can I change plans anytime?</h3>
                <p className="text-body text-muted-foreground">
                  Yes! You can upgrade or downgrade your plan at any time. Upgrades take effect immediately,
                  while downgrades take effect at the next billing cycle.
                </p>
              </div>

              <div className="card-interactive p-6">
                <h3 className="text-subheading text-foreground mb-3">What payment methods do you accept?</h3>
                <p className="text-body text-muted-foreground">
                  We accept all major credit cards, debit cards, and PayPal through our secure
                  payment processor, Stripe.
                </p>
              </div>

              <div className="card-interactive p-6">
                <h3 className="text-subheading text-foreground mb-3">Is there a free trial?</h3>
                <p className="text-body text-muted-foreground">
                  Every new account includes a 30-day free trial. A credit card is required upfront,
                  but you won&apos;t be charged until the trial ends.
                </p>
              </div>

              <div className="card-interactive p-6">
                <h3 className="text-subheading text-foreground mb-3">What happens if I exceed my limits?</h3>
                <p className="text-body text-muted-foreground">
                  You'll receive notifications when approaching your limits. You can upgrade your plan
                  at any time to accommodate your growing needs.
                </p>
              </div>
            </div>
          </section>

          {/* CTA Section */}
          <section className="text-center py-16 bg-gradient-to-r from-primary/10 via-transparent to-secondary/10 rounded-3xl p-12">
            <h2 className="text-3xl font-black text-foreground mb-4">
              Ready to get started?
            </h2>
            <p className="text-lg text-muted-foreground mb-8 max-w-2xl mx-auto">
              Pick a plan that fits your needs and start collaborating today.
            </p>
            <Link
              href="/dashboard"
              className="btn-primary text-lg px-8 py-4"
            >
              Go to Dashboard
            </Link>
          </section>
        </div>
      </main>
    </div>
  )
}