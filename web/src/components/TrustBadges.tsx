import { ShieldCheck, Lock, CreditCard, Shield, CheckCircle } from 'lucide-react'

const badges = [
  { icon: ShieldCheck, label: '256-bit SSL Encryption' },
  { icon: Lock, label: 'Escrow-Protected Exchanges' },
  { icon: CreditCard, label: 'Stripe-Secured Payments' },
  { icon: Shield, label: 'GDPR Compliant' },
  { icon: CheckCircle, label: '30-Day Free Trial' },
]

export function TrustBadges() {
  return (
    <section className="py-8 bg-muted/30">
      <div className="container-premium">
        <div className="flex flex-wrap items-center justify-center gap-6 sm:gap-10">
          {badges.map((badge) => (
            <div key={badge.label} className="flex items-center gap-2 text-muted-foreground text-sm">
              <badge.icon className="w-5 h-5 shrink-0 text-primary" />
              <span className="font-medium">{badge.label}</span>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
