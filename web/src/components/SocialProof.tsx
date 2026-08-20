import { Star } from 'lucide-react'

const stats = [
  { value: '19', label: 'Skill Categories' },
  { value: '50+', label: 'Cities' },
  { value: '$0', label: 'Platform Fees' },
  { value: '3', label: 'Free Exchanges' },
]

const testimonials = [
  {
    quote: 'Your story could be here. Join the waitlist to be among our first skill exchangers.',
    name: 'Be the first',
    role: 'Early member',
  },
]

export function SocialProof() {
  return (
    <section className="py-24 lg:py-32">
      <div className="container-premium">
        {/* Stats Bar */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-20">
          {stats.map((stat) => (
            <div key={stat.label} className="text-center">
              <div className="text-4xl lg:text-5xl font-black bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent mb-2">
                {stat.value}
              </div>
              <div className="text-sm text-muted-foreground font-medium">{stat.label}</div>
            </div>
          ))}
        </div>

        {/* Testimonials */}
        <div className="text-center mb-12">
          <div className="flex items-center justify-center gap-1 mb-4">
            {[...Array(5)].map((_, i) => (
              <Star key={i} className="w-5 h-5 fill-yellow-400 text-yellow-400" />
            ))}
          </div>
          <h2 className="text-3xl lg:text-4xl font-black tracking-tight mb-6">
            <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
              What Professionals Are Saying
            </span>
          </h2>
          <p className="text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed">
            Join the waitlist and be among the first to exchange skills on SkillLedger.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {testimonials.map((t) => (
            <div key={t.name} className="card-feature p-8">
              <blockquote className="text-foreground leading-relaxed mb-6">
                &ldquo;{t.quote}&rdquo;
              </blockquote>
              <div className="w-10 h-10 rounded-full bg-primary/10 text-primary flex items-center justify-center text-sm font-bold mb-3">
                {t.name.split(' ').map(n => n[0]).join('')}
              </div>
              <div>
                <div className="font-bold text-sm">{t.name}</div>
                <div className="text-xs text-muted-foreground">{t.role}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}
