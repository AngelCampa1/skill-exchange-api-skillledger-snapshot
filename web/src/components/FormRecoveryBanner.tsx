'use client'

import { useState } from 'react'
import { RotateCcw, X } from 'lucide-react'

interface FormRecoveryBannerProps {
  onContinue: () => void
  onStartFresh: () => void
}

export default function FormRecoveryBanner({
  onContinue,
  onStartFresh,
}: FormRecoveryBannerProps) {
  const [dismissed, setDismissed] = useState(false)

  if (dismissed) return null

  return (
    <div className="bg-primary/10 border border-primary/20 rounded-xl p-4 mb-6">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <RotateCcw className="w-5 h-5 text-primary flex-shrink-0" />
          <p className="text-sm text-foreground font-medium">
            Welcome back! We saved your progress.
          </p>
        </div>
        <div className="flex items-center gap-3 flex-shrink-0">
          <button
            onClick={() => {
              setDismissed(true)
              onContinue()
            }}
            className="text-sm font-medium text-primary hover:text-primary/80 transition-colors"
          >
            Continue
          </button>
          <button
            onClick={() => {
              setDismissed(true)
              onStartFresh()
            }}
            className="text-sm text-muted-foreground hover:text-foreground transition-colors flex items-center gap-1"
          >
            <X className="w-3.5 h-3.5" />
            Start fresh
          </button>
        </div>
      </div>
    </div>
  )
}
