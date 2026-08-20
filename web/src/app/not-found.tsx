import Link from 'next/link'
import { Button } from '@/components/ui/button'

const suggestedPages = [
  { href: '/resources', label: 'Articles' },
  { href: '/categories', label: 'Skill Categories' },
  { href: '/features', label: 'Features' },
  { href: '/pricing', label: 'Pricing' },
  { href: '/how-to', label: 'How-To Guides' },
  { href: '/glossary', label: 'Glossary' },
  { href: '/compare', label: 'Comparisons' },
  { href: '/about', label: 'About' },
  { href: '/faq', label: 'FAQ' },
]

export default function NotFound() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center max-w-md">
        <h1 className="text-6xl font-bold text-primary mb-4">404</h1>
        <h2 className="text-2xl font-semibold text-foreground mb-4">Page Not Found</h2>
        <p className="text-muted-foreground mb-8">
          The page you are looking for does not exist or has been moved.
        </p>
        <Link href="/">
          <Button>Return Home</Button>
        </Link>
        <div className="mt-10 pt-8 border-t border-border/30">
          <p className="text-sm font-medium text-muted-foreground mb-4">Popular Pages</p>
          <div className="flex flex-wrap justify-center gap-2">
            {suggestedPages.map((page) => (
              <Link
                key={page.href}
                href={page.href}
                className="text-sm px-3 py-1.5 rounded-full border border-border hover:border-primary hover:text-primary transition-colors"
              >
                {page.label}
              </Link>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
