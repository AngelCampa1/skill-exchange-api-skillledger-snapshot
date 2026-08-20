'use client'

import Link from 'next/link'
import { useAuth } from '@/contexts/AuthContext'
import { Logo } from './Logo'
import { NewsletterSignup } from './NewsletterSignup'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { categoriesData } from '@/lib/data/categories-data'
import { citiesData } from '@/lib/data/cities-data'

const topCategories = categoriesData
  .filter((c) => c.demandLevel === 'high')
  .slice(0, 6)

const topCities = citiesData.slice(0, 6)

export function SiteFooter() {
  const { isAuthenticated } = useAuth()

  if (isAuthenticated) {
    return null
  }

  return (
    <footer className="bg-card border-t border-border/50 mt-auto">
      <div className="container-premium py-16 lg:py-20">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-8 lg:gap-6">
          {/* Brand */}
          <div className="col-span-2 md:col-span-3 lg:col-span-1">
            <Logo size="small" showText={true} />
            <p className="mt-4 text-sm text-muted-foreground leading-relaxed max-w-xs">
              A professional collaboration platform for exchanging services through a credit-based barter system.
            </p>
            <NewsletterSignup variant="footer" />
          </div>

          {/* Platform */}
          <div>
            <h3 className="font-bold text-sm tracking-tight mb-4">Platform</h3>
            <ul className="space-y-2.5">
              <li><Link href="/register" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Get Started</Link></li>
              <li><Link href="/login" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Sign In</Link></li>
              <li><Link href="/categories" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Skill Categories</Link></li>
              <li><Link href="/industries" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Industries</Link></li>
              <li><Link href="/features" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Features</Link></li>
              <li><Link href="/pricing" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Pricing</Link></li>
              <li><Link href="/about" className="text-sm text-muted-foreground hover:text-foreground transition-colors">About</Link></li>
            </ul>
          </div>

          {/* Resources */}
          <div>
            <h3 className="font-bold text-sm tracking-tight mb-4">Resources</h3>
            <ul className="space-y-2.5">
              <li><Link href="/resources" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Articles</Link></li>
              <li><Link href="/how-to" className="text-sm text-muted-foreground hover:text-foreground transition-colors">How-To Guides</Link></li>
              <li><Link href="/resources/templates" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Templates</Link></li>
              <li><Link href="/tools" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Tools</Link></li>
              <li><Link href="/glossary" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Glossary</Link></li>
              <li><Link href="/trade" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Trade Pairings</Link></li>
              <li><Link href="/locations" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Locations</Link></li>
              <li><Link href="/faq" className="text-sm text-muted-foreground hover:text-foreground transition-colors">FAQ</Link></li>
            </ul>
          </div>

          {/* Explore */}
          <div>
            <h3 className="font-bold text-sm tracking-tight mb-4">Explore</h3>
            <ul className="space-y-2.5">
              {topCategories.map((cat) => (
                <li key={cat.slug}>
                  <Link href={`/categories/${cat.slug}`} className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    {cat.name}
                  </Link>
                </li>
              ))}
              <li><Link href="/categories" className="text-sm text-primary hover:text-primary/80 transition-colors font-medium">View all</Link></li>
            </ul>
          </div>

          {/* Compare */}
          <div>
            <h3 className="font-bold text-sm tracking-tight mb-4">Compare</h3>
            <ul className="space-y-2.5">
              {comparisonsData.slice(0, 5).map((c) => (
                <li key={c.slug}>
                  <Link href={`/compare/${c.slug}`} className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    vs. {c.sideB.name}
                  </Link>
                </li>
              ))}
              <li><Link href="/compare" className="text-sm text-primary hover:text-primary/80 transition-colors font-medium">View all</Link></li>
            </ul>
          </div>

          {/* Cities */}
          <div>
            <h3 className="font-bold text-sm tracking-tight mb-4">Cities</h3>
            <ul className="space-y-2.5">
              {topCities.map((city) => (
                <li key={city.slug}>
                  <Link href={`/skill-exchange/${city.slug}`} className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    {city.city}, {city.state}
                  </Link>
                </li>
              ))}
              <li><Link href="/skill-exchange" className="text-sm text-primary hover:text-primary/80 transition-colors font-medium">View all</Link></li>
            </ul>
          </div>
        </div>

        {/* Bottom */}
        <div className="mt-12 pt-8 border-t border-border/30 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-sm text-muted-foreground">
            &copy; {new Date().getFullYear()} SkillLedger. All rights reserved.
          </p>
          <div className="flex items-center gap-6">
            <Link href="/privacy" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Privacy</Link>
            <Link href="/terms" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Terms</Link>
          </div>
        </div>
      </div>
    </footer>
  )
}
