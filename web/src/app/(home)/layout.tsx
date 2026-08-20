import { Metadata } from 'next'
import { SITE_CONFIG, TARGET_KEYWORDS } from '@/lib/seo'

// Home page uses root layout's default title to avoid template duplication
// Only override description and keywords
export const metadata: Metadata = {
  description: `${SITE_CONFIG.description} Connect with verified professionals, exchange services through a credit-based barter system, and build your professional reputation.`,
  keywords: [
    ...TARGET_KEYWORDS,
    'professional networking',
    'skill bartering',
    'service exchange marketplace',
    'professional services platform',
  ].join(', '),
  alternates: {
    canonical: SITE_CONFIG.url,
  },
}

export default function HomeLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <>{children}</>
}
