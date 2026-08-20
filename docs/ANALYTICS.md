# Analytics Implementation Guide

**Last Updated**: December 2025

## Table of Contents
1. [Overview](#overview)
2. [Account Setup](#account-setup)
3. [Quick Start](#quick-start)
4. [Architecture](#architecture)
5. [Tracking Events](#tracking-events)
6. [Event Categories](#event-categories)
7. [Testing](#testing)
8. [Privacy & Compliance](#privacy--compliance)
9. [Troubleshooting](#troubleshooting)

---

## Overview

SkillLedger uses **Google Analytics 4 (GA4)** and **Microsoft Clarity** for comprehensive user analytics tracking. Our implementation follows a **consent-first architecture** with full GDPR compliance.

### Key Features
- ✅ **Consent-First**: No tracking without explicit user consent
- ✅ **Production-Only**: Analytics disabled in development environment
- ✅ **Type-Safe**: Full TypeScript support with comprehensive event types
- ✅ **GDPR Compliant**: IP anonymization, data retention limits, Do Not Track support
- ✅ **TDD Approach**: 88 passing tests with 100% test coverage
- ✅ **Privacy-Focused**: Separate error handling to prevent data leakage

### Tech Stack
- **Google Analytics 4**: Event-based analytics platform
- **Microsoft Clarity**: Session replay and heatmap tool
- **Next.js 14**: App Router with Script component optimization
- **TypeScript**: Strict type safety for all analytics events
- **React Testing Library**: Comprehensive test coverage

---

## Account Setup

### 1. Google Analytics 4 Setup

1. **Create Account**: Visit [https://analytics.google.com/](https://analytics.google.com/)
2. **Create Property**:
   - Property name: `SkillLedger Production`
   - Timezone and currency: Configure as needed
3. **Add Data Stream**:
   - Platform: Web
   - Website URL: `https://skillledger.app` (production URL)
   - Stream name: `SkillLedger Web`
4. **Copy Measurement ID**: Format is `G-XXXXXXXXXX`
5. **Configure Settings**:
   - Navigate to **Admin** → **Data Settings** → **Data Retention**
   - Set retention to **14 months**
   - Enable **Reset user data on new activity**: Yes
6. **Enable Enhanced Measurement**:
   - Go to **Admin** → **Data Streams** → Click your stream
   - Toggle on **Enhanced Measurement**
   - Enable: Page views, Scrolls, Outbound clicks, Site search, File downloads
7. **IP Anonymization**:
   - Already configured in our code via `anonymize_ip: true`

### 2. Microsoft Clarity Setup

1. **Create Account**: Visit [https://clarity.microsoft.com/](https://clarity.microsoft.com/)
2. **Create Project**:
   - Project name: `SkillLedger Production`
   - Website URL: `https://skillledger.app`
3. **Copy Project ID**: Alphanumeric string (e.g., `abc123def456`)
4. **Configure Settings**:
   - Navigate to **Settings** → **Setup**
   - Enable **Mask sensitive text by default**: Yes
   - Set **Recording length**: 60 minutes
   - Enable **IP masking**: Yes (for GDPR compliance)
5. **Configure Cookie Policy**:
   - Set to **Respect Do Not Track**: Yes
   - Set to **Require consent**: Yes (we handle this via our banner)

### 3. Environment Configuration

#### Development (`.env.local`)
```env
# Analytics disabled in development
NEXT_PUBLIC_ENABLE_ANALYTICS=false
NEXT_PUBLIC_GA4_MEASUREMENT_ID=G-XXXXXXXXXX
NEXT_PUBLIC_CLARITY_PROJECT_ID=your-clarity-id
```

#### Production (`.env.production`)
```env
# Analytics enabled in production
NEXT_PUBLIC_ENABLE_ANALYTICS=true
NEXT_PUBLIC_GA4_MEASUREMENT_ID=G-XXXXXXXXXX  # Replace with actual ID
NEXT_PUBLIC_CLARITY_PROJECT_ID=your-clarity-id  # Replace with actual ID
```

**IMPORTANT**: Never commit actual analytics IDs to version control. Use environment-specific configuration.

---

## Quick Start

### Using the Analytics Hook

The `useAnalytics` hook is the primary way to track events in React components:

```typescript
import { useAnalytics } from '@/hooks/useAnalytics'

function MyComponent() {
  const { trackEvent, trackPageView, identify } = useAnalytics()

  const handleButtonClick = () => {
    trackEvent({
      name: 'button_clicked',
      category: 'ui_interaction',
      priority: 'low',
      properties: {
        button_name: 'submit',
        button_location: 'footer',
      },
    })
  }

  return <button onClick={handleButtonClick}>Submit</button>
}
```

### Manual Tracking (Outside React)

For tracking outside React components:

```typescript
import { trackEvent } from '@/utils/analytics'

trackEvent({
  name: 'api_error',
  category: 'errors',
  priority: 'critical',
  properties: {
    error_message: 'Network timeout',
    endpoint: '/api/projects',
  },
})
```

---

## Architecture

### Component Hierarchy

```
RootLayout (layout.tsx)
  └─ CookieConsentProvider
       ├─ AuthProvider
       │    ├─ {children} (app pages)
       │    ├─ PageViewTracker (auto page tracking)
       │    └─ CookieConsentBanner (consent UI)
       └─ AnalyticsScripts (GA4 + Clarity)
```

### Core Components

#### 1. **CookieConsentContext** (`contexts/CookieConsentContext.tsx`)
- Manages user consent state
- Persists consent in localStorage
- Checks Do Not Track browser setting
- Emits consent events to GA4

#### 2. **CookieConsentBanner** (`components/cookies/CookieConsentBanner.tsx`)
- User-facing consent banner
- Accept/Decline buttons
- Keyboard accessible (Enter/Escape)
- Links to Privacy Policy

#### 3. **AnalyticsScripts** (`components/analytics/AnalyticsScripts.tsx`)
- Loads GA4 and Clarity scripts conditionally
- Only loads if consent given and analytics enabled
- Uses Next.js Script component with `strategy="afterInteractive"`

#### 4. **PageViewTracker** (`components/analytics/PageViewTracker.tsx`)
- Automatic page view tracking on route changes
- Includes search params in URL
- No manual tracking needed

#### 5. **useAnalytics Hook** (`hooks/useAnalytics.tsx`)
- React hook for tracking events
- Auto-identifies authenticated users
- Respects consent state
- Stable function references (useCallback)

#### 6. **Analytics Utility** (`utils/analytics.ts`)
- Core tracking functions
- GA4 and Clarity integration
- Separate error handling to prevent cross-contamination

---

## Tracking Events

### Event Structure

All events follow this TypeScript interface:

```typescript
interface AnalyticsEvent {
  name: string                    // Event name (e.g., 'sign_up', 'project_created')
  category: EventCategory         // Event category (see below)
  priority: EventPriority         // 'critical' | 'high' | 'medium' | 'low'
  properties?: Record<string, any> // Optional event properties
  userProperties?: Record<string, any> // Optional user properties
  timestamp?: number              // Optional timestamp (auto-generated)
}
```

### Event Priority Levels

- **Critical**: Business-critical events (authentication, payments, errors)
- **High**: Important user actions (project creation, profile updates)
- **Medium**: Standard interactions (search, filters, navigation)
- **Low**: Minor interactions (UI clicks, hover events)

### Event Properties

Event properties provide additional context:

```typescript
trackEvent({
  name: 'project_created',
  category: 'projects',
  priority: 'critical',
  properties: {
    project_type: 'service',
    project_duration: '3 months',
    has_budget: true,
    skill_count: 5,
  },
})
```

**Best Practices**:
- Use snake_case for property names
- Keep property names consistent across events
- Avoid PII (emails, names) in event properties
- Use boolean values for yes/no questions
- Use numbers for counts and durations

---

## Event Categories

### 1. Authentication Events (`category: 'authentication'`)

**Priority: Critical**

```typescript
// Sign Up
trackEvent({
  name: 'sign_up',
  category: 'authentication',
  priority: 'critical',
  properties: {
    method: 'email',  // 'email' | 'oauth' | 'sso'
    provider: 'google', // Optional: OAuth provider
  },
})

// Sign In
trackEvent({
  name: 'sign_in',
  category: 'authentication',
  priority: 'critical',
  properties: {
    method: 'email',
    remember_me: true,
  },
})

// Logout
trackEvent({
  name: 'logout',
  category: 'authentication',
  priority: 'critical',
})

// Password Reset
trackEvent({
  name: 'password_reset',
  category: 'authentication',
  priority: 'critical',
  properties: {
    success: true,
  },
})

// Email Verification
trackEvent({
  name: 'email_verification',
  category: 'authentication',
  priority: 'critical',
  properties: {
    success: true,
  },
})
```

### 2. Monetization Events (`category: 'monetization'`)

**Priority: Critical**

**IMPORTANT**: Use GA4's recommended e-commerce event names for proper integration with GA4 reports.

```typescript
// View Subscription Plans
trackEvent({
  name: 'view_item',  // GA4 recommended name
  category: 'monetization',
  priority: 'critical',
  properties: {
    item_name: 'Premium Plan',
    item_category: 'subscription',
    currency: 'USD',
    value: 29.99,
  },
})

// Select Subscription Tier
trackEvent({
  name: 'select_item',  // GA4 recommended name
  category: 'monetization',
  priority: 'critical',
  properties: {
    item_name: 'Premium Plan',
    tier: 'premium',
    billing_cycle: 'monthly',
  },
})

// Begin Checkout
trackEvent({
  name: 'begin_checkout',  // GA4 recommended name
  category: 'monetization',
  priority: 'critical',
  properties: {
    currency: 'USD',
    value: 29.99,
    item_name: 'Premium Plan',
  },
})

// Purchase Success
trackEvent({
  name: 'purchase',  // GA4 recommended name
  category: 'monetization',
  priority: 'critical',
  properties: {
    transaction_id: 'txn_123456',
    currency: 'USD',
    value: 29.99,
    item_name: 'Premium Plan',
    payment_method: 'stripe',
  },
})

// Purchase Failure
trackEvent({
  name: 'purchase_failed',
  category: 'monetization',
  priority: 'critical',
  properties: {
    error_message: 'Card declined',
    payment_method: 'stripe',
  },
})

// Cancel Subscription
trackEvent({
  name: 'cancel_subscription',
  category: 'monetization',
  priority: 'critical',
  properties: {
    tier: 'premium',
    reason: 'too expensive',
  },
})
```

### 3. Project Events (`category: 'projects'`)

**Priority: Critical to High**

```typescript
// Project Created
trackEvent({
  name: 'project_created',
  category: 'projects',
  priority: 'critical',
  properties: {
    project_type: 'service',
    has_budget: true,
    skill_count: 5,
  },
})

// Project Search
trackEvent({
  name: 'search',  // GA4 recommended name
  category: 'projects',
  priority: 'high',
  properties: {
    search_term: 'web development',
    filters_applied: 'location,skills',
  },
})

// Application Submitted
trackEvent({
  name: 'application_submitted',
  category: 'projects',
  priority: 'critical',
  properties: {
    project_id: 'proj_123',
    has_proposal: true,
  },
})
```

### 4. Credit Events (`category: 'credits'`)

**Priority: Critical**

```typescript
// Credit Transfer
trackEvent({
  name: 'credit_transfer',
  category: 'credits',
  priority: 'critical',
  properties: {
    amount: 100,
    recipient_id: 'user_456',
    transaction_type: 'project_payment',
  },
})
```

### 5. Profile Events (`category: 'profile'`)

**Priority: High**

```typescript
// Wizard Step Completed
trackEvent({
  name: 'wizard_step_completed',
  category: 'profile',
  priority: 'high',
  properties: {
    step_number: 2,
    step_name: 'skills',
  },
})

// Profile Published
trackEvent({
  name: 'profile_published',
  category: 'profile',
  priority: 'high',
  properties: {
    profile_complete: true,
    skill_count: 10,
  },
})
```

### 6. Messaging Events (`category: 'messaging'`)

**Priority: High**

```typescript
// Message Sent
trackEvent({
  name: 'message_sent',
  category: 'messaging',
  priority: 'high',
  properties: {
    message_type: 'text',
    has_attachment: false,
  },
})

// Message Received
trackEvent({
  name: 'message_received',
  category: 'messaging',
  priority: 'high',
  properties: {
    message_type: 'text',
  },
})
```

### 7. Search Events (`category: 'search'`)

**Priority: Medium to High**

```typescript
// Search Query Entered
trackEvent({
  name: 'search',
  category: 'search',
  priority: 'high',
  properties: {
    search_term: 'react developer',
    search_type: 'freelancer',
  },
})

// Search Filters Applied
trackEvent({
  name: 'filter_applied',
  category: 'search',
  priority: 'medium',
  properties: {
    filter_type: 'location',
    filter_value: 'San Francisco',
  },
})
```

### 8. Navigation Events (`category: 'navigation'`)

**Priority: Low to Medium**

**NOTE**: Page views are tracked automatically by `PageViewTracker`. Manual tracking is only needed for special cases.

```typescript
// Manual Page View (if needed)
trackPageView('/dashboard', 'Dashboard - SkillLedger')

// Route Change (tracked automatically)
// No manual tracking needed
```

### 9. Form Events (`category: 'forms'`)

**Priority: Medium**

```typescript
// Form Submitted
trackEvent({
  name: 'form_submitted',
  category: 'forms',
  priority: 'medium',
  properties: {
    form_name: 'contact_form',
    form_valid: true,
  },
})

// Form Validation Error
trackEvent({
  name: 'form_error',
  category: 'forms',
  priority: 'medium',
  properties: {
    form_name: 'contact_form',
    error_field: 'email',
    error_message: 'Invalid email format',
  },
})
```

### 10. UI Interaction Events (`category: 'ui_interaction'`)

**Priority: Low**

```typescript
// Button Click
trackEvent({
  name: 'button_clicked',
  category: 'ui_interaction',
  priority: 'low',
  properties: {
    button_name: 'download_report',
    button_location: 'dashboard',
  },
})

// Modal Opened
trackEvent({
  name: 'modal_opened',
  category: 'ui_interaction',
  priority: 'low',
  properties: {
    modal_name: 'settings',
  },
})
```

### 11. Feedback Events (`category: 'feedback'`)

**Priority: High**

```typescript
// Feedback Submitted
trackEvent({
  name: 'feedback_submitted',
  category: 'feedback',
  priority: 'high',
  properties: {
    feedback_type: 'bug_report',
    rating: 4,
  },
})
```

### 12. Error Events (`category: 'errors'`)

**Priority: Critical**

```typescript
// Use trackException for error tracking
import { trackException } from '@/utils/analytics'

try {
  // Code that may throw
} catch (error) {
  trackException(error as Error, 'checkout_flow')
}
```

### 13. Performance Events (`category: 'performance'`)

**Priority: Medium**

```typescript
// Use trackTiming for performance metrics
import { trackTiming } from '@/utils/analytics'

trackTiming('page_load', 1234)  // milliseconds
trackTiming('api_response', 567)
```

---

## Testing

### Running Tests

```bash
# Run all analytics tests
cd web && yarn test

# Run with coverage
cd web && yarn test --coverage

# Run specific test suite
cd web && yarn test useAnalytics.test.tsx

# Watch mode (for TDD)
cd web && yarn test --watch
```

### Test Coverage

**Current Status**: 88/88 tests passing (100% pass rate)

- `CookieConsentContext`: 16 tests
- `CookieConsentBanner`: 15 tests
- `analytics.ts`: 38 tests
- `useAnalytics`: 19 tests

### Testing Analytics in Components

```typescript
import { render } from '@testing-library/react'
import { useAnalytics } from '@/hooks/useAnalytics'

// Mock the analytics module
jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
  trackPageView: jest.fn(),
}))

test('tracks event when button clicked', () => {
  const { getByText } = render(<MyComponent />)
  const button = getByText('Submit')

  fireEvent.click(button)

  expect(analyticsModule.trackEvent).toHaveBeenCalledWith({
    name: 'button_clicked',
    category: 'ui_interaction',
    priority: 'low',
    properties: { button_name: 'submit' },
  })
})
```

### Manual Testing in Browser

1. **Enable Analytics in Development**:
   ```env
   # .env.local
   NEXT_PUBLIC_ENABLE_ANALYTICS=true
   ```

2. **Check Console**:
   - Open browser DevTools → Console
   - Look for `[Analytics]` log messages
   - Verify events are being tracked

3. **GA4 Real-Time Reports**:
   - Go to GA4 → Reports → Realtime
   - Trigger events in your app
   - Verify events appear in real-time report (within 30 seconds)

4. **Clarity Dashboard**:
   - Go to Clarity project dashboard
   - Verify session recordings appear
   - Check heatmaps and click tracking

---

## Privacy & Compliance

### GDPR Compliance Checklist

- ✅ **Explicit Consent**: Required before any tracking
- ✅ **IP Anonymization**: Enabled in GA4
- ✅ **Data Retention**: 14 months for GA4, 60 days for Clarity
- ✅ **Do Not Track**: Honored automatically
- ✅ **Opt-Out**: Easy consent withdrawal
- ✅ **Transparency**: Comprehensive privacy policy
- ✅ **Data Minimization**: Only essential data collected

### Consent Flow

1. **First Visit**: Cookie banner appears
2. **User Accepts**: `consentGiven = true` → Analytics enabled
3. **User Declines**: `consentGiven = false` → No tracking
4. **Do Not Track**: Auto-decline if DNT browser setting enabled
5. **Withdrawal**: User can revoke consent anytime

### Data Collection Practices

**What We Track**:
- Page views and navigation patterns
- Button clicks and form interactions
- Search queries and filters
- Error messages and technical issues
- Performance metrics (Core Web Vitals)
- Device type, browser, screen resolution
- Anonymized IP address and general location

**What We DON'T Track**:
- Personal Identifiable Information (PII)
- Passwords or payment details
- Exact GPS coordinates
- Private messages or communications
- Sensitive form inputs (credit cards, SSN)

### Error Isolation

Our implementation uses **separate try-catch blocks** for GA4 and Clarity to prevent error cross-contamination:

```typescript
// Separate try-catch for GA4
try {
  if (window.gtag) {
    window.gtag('event', event.name, params)
  }
} catch (error) {
  console.error('GA4 error:', error)
}

// Separate try-catch for Clarity
try {
  if (window.clarity) {
    window.clarity('set', 'last_event', event.name)
  }
} catch (error) {
  console.error('Clarity error:', error)
}
```

This prevents a GA4 error from blocking Clarity tracking and vice versa.

---

## Troubleshooting

### Events Not Appearing in GA4

**Issue**: Events tracked but not showing in GA4 Real-Time reports

**Solutions**:
1. **Check Consent**: Verify `consentGiven === true` in browser console
   ```javascript
   localStorage.getItem('cookie-consent')  // Should be 'true'
   ```

2. **Check Environment**: Verify `NEXT_PUBLIC_ENABLE_ANALYTICS=true`
   ```javascript
   console.log(process.env.NEXT_PUBLIC_ENABLE_ANALYTICS)
   ```

3. **Check Measurement ID**: Verify correct GA4 ID in `.env.production`
   ```env
   NEXT_PUBLIC_GA4_MEASUREMENT_ID=G-XXXXXXXXXX
   ```

4. **Check Network Tab**:
   - Open DevTools → Network
   - Filter by `google-analytics.com`
   - Verify requests are being sent

5. **Wait 24-48 Hours**: GA4 reports may have delay for non-realtime data

### Clarity Sessions Not Recording

**Issue**: Clarity project shows no sessions

**Solutions**:
1. **Check Consent**: Same as above
2. **Check Project ID**: Verify correct Clarity ID in `.env.production`
3. **Check Network Tab**:
   - Filter by `clarity.ms`
   - Verify Clarity script loaded

4. **Check Clarity Settings**:
   - Ensure recording is enabled in project settings
   - Check recording length limit (60 minutes)

### TypeScript Errors

**Issue**: `Property 'gtag' does not exist on type 'Window'`

**Solution**: Ensure `global.d.ts` is included in `tsconfig.json`:
```json
{
  "include": ["src/types/global.d.ts", ...]
}
```

### Tests Failing

**Issue**: Analytics tests failing after changes

**Solutions**:
1. **Clear Mocks**: Add `jest.clearAllMocks()` in `beforeEach`
2. **Check Mock Setup**: Verify mocks are properly configured
3. **Check Imports**: Ensure correct import paths
4. **Run Single Test**: Isolate failing test with `test.only()`

### Performance Issues

**Issue**: Page load times increased after analytics implementation

**Solutions**:
1. **Verify Script Strategy**: Ensure using `strategy="afterInteractive"`
2. **Check Lighthouse Score**: Should drop <5 points
3. **Optimize Event Tracking**: Avoid tracking in tight loops
4. **Use Debouncing**: For frequent events (scroll, mouse move)

---

## Best Practices

### 1. Event Naming Conventions
- Use `snake_case` for event names
- Be descriptive but concise
- Use GA4 recommended names for e-commerce events
- Avoid special characters except underscore

### 2. Event Properties
- Keep property count low (<10 per event)
- Use consistent property names across events
- Avoid nesting objects in properties
- Use boolean values for yes/no questions

### 3. Performance
- Track critical events only
- Debounce frequent events (scroll, resize)
- Avoid tracking in render loops
- Use `priority` field to indicate importance

### 4. Privacy
- Never track PII without anonymization
- Respect user consent state
- Honor Do Not Track setting
- Keep data retention minimal

### 5. Testing
- Write tests for all tracking calls
- Mock analytics functions in tests
- Verify consent checking in tests
- Test error handling

---

## Additional Resources

- [Google Analytics 4 Documentation](https://support.google.com/analytics/answer/10089681)
- [Microsoft Clarity Documentation](https://docs.microsoft.com/en-us/clarity/)
- [GDPR Compliance Guide](https://gdpr.eu/what-is-gdpr/)
- [Next.js Script Optimization](https://nextjs.org/docs/app/building-your-application/optimizing/scripts)
- [Privacy Policy](../web/src/app/privacy/page.tsx)

---

## Support

For questions or issues:
- **Analytics Setup**: Contact DevOps team
- **Privacy Concerns**: privacy@skillledger.app
- **Bug Reports**: Create GitHub issue with `[analytics]` tag
- **Feature Requests**: Submit proposal to product team
