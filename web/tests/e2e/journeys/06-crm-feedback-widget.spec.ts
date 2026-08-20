/**
 * E2E: CRM Feedback Widget — authenticated surface mount
 *
 * Verifies that the Ventora CRM feedback-button loader script is injected into
 * the page DOM after a user navigates to an authenticated route (dashboard).
 *
 * Pre-requisites to RUN this spec:
 *   1. Backend and frontend servers are running (yarn dev / dotnet run).
 *   2. A valid test user has been registered and its email verified.
 *   3. .env.local contains:
 *        NEXT_PUBLIC_CRM_WIDGET_KEY=wk_LOCALTESTPLACEHOLDER00000000000000
 *        NEXT_PUBLIC_CRM_LOADER_URL=https://crm.example.com/w/v1.js
 *
 * NOTE: This spec does NOT assert that the CRM fetch returns 200 — the CRM
 * enforces an origin allowlist and will no-op on localhost, which is expected.
 * We only assert that the loader <script> tag is present in the DOM.
 *
 * STATUS: WRITTEN — not run in CI (requires live servers + seeded test user).
 * Run manually: cd web && yarn test:e2e --grep "CRM feedback"
 */

import { test, expect } from '@playwright/test'
import { AuthHelper } from '../utils/auth'

const CRM_LOADER_DEFAULT = 'https://crm.example.com/w/v1.js'

// Credentials for a pre-existing verified test account.
// Override via env vars to avoid committing real credentials.
const TEST_EMAIL = process.env.E2E_TEST_EMAIL || 'e2e-feedback@skillledger.test'
const TEST_PASSWORD = process.env.E2E_TEST_PASSWORD || 'TestPass123!'

test.describe('CRM Feedback Widget — authenticated surface', () => {
  test.setTimeout(60_000)

  test('loader script is injected after navigating to an authenticated route', async ({ page }) => {
    // 1. Log in via the UI
    await page.goto('/login')
    await AuthHelper.login(page, { email: TEST_EMAIL, password: TEST_PASSWORD })

    // 2. Navigate to an authenticated route (dashboard is protected by middleware)
    await page.goto('/dashboard')
    await page.waitForURL('**/dashboard**', { timeout: 30_000 })

    // 3. The widget mounts with strategy="afterInteractive" — wait for it
    const loaderUrl = process.env.NEXT_PUBLIC_CRM_LOADER_URL || CRM_LOADER_DEFAULT

    const scriptHandle = await page.waitForSelector(
      `script[data-widget="feedback-button"][src="${loaderUrl}"]`,
      { timeout: 15_000 }
    )
    expect(scriptHandle).not.toBeNull()

    // 4. Confirm data-product attribute is present (truthy — exact key is env-specific)
    const dataProduct = await scriptHandle.getAttribute('data-product')
    expect(dataProduct).toBeTruthy()
  })
})
