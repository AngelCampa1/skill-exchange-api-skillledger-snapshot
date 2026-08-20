'use client'

/**
 * Analytics Scripts Component
 *
 * Loads Google Analytics 4 and Microsoft Clarity scripts when consent is given.
 * Uses Next.js Script component for optimal loading performance.
 */

import Script from 'next/script'
import { useCookieConsent } from '@/contexts/CookieConsentContext'

export default function AnalyticsScripts() {
  const { consentGiven } = useCookieConsent()

  const enabled = process.env.NEXT_PUBLIC_ENABLE_ANALYTICS === 'true'
  const ga4Id = process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID
  const clarityId = process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID

  // Don't load scripts if analytics is disabled or consent not given
  if (!enabled || !consentGiven) {
    return null
  }

  return (
    <>
      {/* Google Analytics 4 */}
      {ga4Id && (
        <>
          <Script
            src={`https://www.googletagmanager.com/gtag/js?id=${ga4Id}`}
            strategy="afterInteractive"
            onLoad={() => {
              // Initialize GA4 with consent mode
              if (typeof window !== 'undefined' && window.gtag) {
                window.gtag('js', new Date())
                window.gtag('config', ga4Id, {
                  anonymize_ip: true,
                  cookie_flags: 'SameSite=None;Secure',
                  send_page_view: false, // We handle page views manually
                })
              }
            }}
          />
          <Script
            id="ga4-init"
            strategy="afterInteractive"
            dangerouslySetInnerHTML={{
              __html: `
                window.dataLayer = window.dataLayer || [];
                function gtag(){dataLayer.push(arguments);}
                gtag('consent', 'default', {
                  'analytics_storage': '${consentGiven ? 'granted' : 'denied'}'
                });
              `,
            }}
          />
        </>
      )}

      {/* Microsoft Clarity */}
      {clarityId && (
        <Script
          id="clarity-init"
          strategy="afterInteractive"
          dangerouslySetInnerHTML={{
            __html: `
              (function(c,l,a,r,i,t,y){
                c[a]=c[a]||function(){(c[a].q=c[a].q||[]).push(arguments)};
                t=l.createElement(r);t.async=1;t.src="https://www.clarity.ms/tag/"+i;
                y=l.getElementsByTagName(r)[0];y.parentNode.insertBefore(t,y);
              })(window, document, "clarity", "script", "${clarityId}");
            `,
          }}
        />
      )}
    </>
  )
}
