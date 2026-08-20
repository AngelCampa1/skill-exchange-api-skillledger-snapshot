import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import { Suspense } from 'react'
import './globals.css'
import { AuthProvider } from '@/contexts/AuthContext'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { CookieConsentProvider } from '@/contexts/CookieConsentContext'
import ErrorBoundary from '@/components/ErrorBoundary'  // BUG-HIGH-006 FIX
import { PublicNavbar } from '@/components/PublicNavbar'
import { SiteFooter } from '@/components/SiteFooter'
import VentoraFeedbackWidget from '@/components/feedback/VentoraFeedbackWidget'
import AnalyticsScripts from '@/components/analytics/AnalyticsScripts'
import PageViewTracker from '@/components/analytics/PageViewTracker'
import CookieConsentBanner from '@/components/cookies/CookieConsentBanner'
import { ExitIntentPopup } from '@/components/ExitIntentPopup'
import {
  SITE_CONFIG,
  TARGET_KEYWORDS,
  DEFAULT_OG_IMAGE,
  DEFAULT_TWITTER_IMAGE,
  generateOrganizationSchema,
  generateWebSiteSchema,
} from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'

const inter = Inter({ subsets: ['latin'] })

export const metadata: Metadata = {
  metadataBase: new URL(SITE_CONFIG.url),
  title: {
    default: `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`,
    template: `%s | ${SITE_CONFIG.name}`,
  },
  description: SITE_CONFIG.description,
  keywords: TARGET_KEYWORDS.join(', '),
  authors: [{ name: SITE_CONFIG.name }],
  creator: SITE_CONFIG.name,
  publisher: SITE_CONFIG.name,
  icons: {
    icon: [
      { url: '/favicon.svg', type: 'image/svg+xml' },
      { url: '/favicon.ico' },
    ],
    shortcut: '/favicon-16x16.png',
    apple: '/apple-touch-icon.png',
    other: [
      { rel: 'icon', type: 'image/png', sizes: '32x32', url: '/favicon-32x32.png' },
      { rel: 'icon', type: 'image/png', sizes: '16x16', url: '/favicon-16x16.png' },
    ],
  },
  manifest: '/site.webmanifest',
  alternates: {
    canonical: SITE_CONFIG.url,
  },
  openGraph: {
    type: 'website',
    locale: SITE_CONFIG.locale,
    url: SITE_CONFIG.url,
    siteName: SITE_CONFIG.name,
    title: `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`,
    description: SITE_CONFIG.description,
    images: [DEFAULT_OG_IMAGE],
  },
  twitter: {
    card: 'summary_large_image',
    site: SITE_CONFIG.twitterHandle,
    creator: SITE_CONFIG.twitterHandle,
    title: `${SITE_CONFIG.name} - ${SITE_CONFIG.tagline}`,
    description: SITE_CONFIG.description,
    images: [DEFAULT_TWITTER_IMAGE.url],
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  verification: {
    google: process.env.NEXT_PUBLIC_GOOGLE_VERIFICATION,
  },
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        {/* Structured Data - Organization Schema */}
        <JsonLd schema={generateOrganizationSchema()} />
        {/* Structured Data - WebSite Schema with SearchAction */}
        <JsonLd schema={generateWebSiteSchema()} />
      </head>
      <body className={inter.className}>
        {/* BUG-HIGH-006 FIX: Wrap app with ErrorBoundary to prevent complete crashes */}
        <ErrorBoundary>
          <ThemeProvider>
            <CookieConsentProvider>
              <AuthProvider>
                <PublicNavbar />
                {children}
                <SiteFooter />
                <VentoraFeedbackWidget />
                <Suspense fallback={null}>
                  <PageViewTracker />
                </Suspense>
                <CookieConsentBanner />
                <ExitIntentPopup />
              </AuthProvider>
              <AnalyticsScripts />
            </CookieConsentProvider>
          </ThemeProvider>
        </ErrorBoundary>
      </body>
    </html>
  )
}
