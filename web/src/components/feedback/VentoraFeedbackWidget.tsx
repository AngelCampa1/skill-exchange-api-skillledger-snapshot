'use client'

import Script from 'next/script'
import { usePathname } from 'next/navigation'

const CRM_LOADER_DEFAULT = 'https://crm.example.com/w/v1.js'

/**
 * Authenticated-surface route prefixes. The CRM feedback widget must only
 * mount on the logged-in app surface — never on public marketing, login, or
 * registration pages. This mirrors how the app already separates authed
 * routes from public ones.
 */
const PROTECTED_PREFIXES = [
  '/applications',
  '/create-project',
  '/dashboard',
  '/messages',
  '/my-projects',
  '/profile',
  '/projects',
  '/settings',
  '/skill-exchange',
  '/skill-match',
  '/subscription',
  '/trade',
  '/wallet',
  '/workspace',
]

/**
 * shouldRenderVentoraFeedbackWidget — true only on authenticated app routes.
 * Public routes (marketing pages, `/`, `/pricing`, `/login`, `/register`, …)
 * return false so the widget never renders there.
 */
export function shouldRenderVentoraFeedbackWidget(pathname: string | null | undefined) {
  const currentPath = pathname ?? ''
  return PROTECTED_PREFIXES.some(
    (prefix) => currentPath === prefix || currentPath.startsWith(`${prefix}/`),
  )
}

/**
 * VentoraFeedbackWidget — mounts the Ventora CRM feedback-button widget on the
 * authenticated app surface only. Renders nothing when:
 *   - the current route is not an authenticated app route, or
 *   - NEXT_PUBLIC_CRM_WIDGET_KEY is unset (no-op in CI / local dev).
 *
 * The CRM also enforces an origin allowlist server-side; the widget only
 * activates on https://app.skillledger.app — requests from localhost no-op.
 */
export default function VentoraFeedbackWidget() {
  const pathname = usePathname() ?? ''
  const key = process.env.NEXT_PUBLIC_CRM_WIDGET_KEY
  const loaderUrl = process.env.NEXT_PUBLIC_CRM_LOADER_URL || CRM_LOADER_DEFAULT

  if (!shouldRenderVentoraFeedbackWidget(pathname)) return null
  if (!key) return null

  return (
    <Script
      src={loaderUrl}
      data-product={key}
      data-widget="feedback-button"
      strategy="afterInteractive"
    />
  )
}
