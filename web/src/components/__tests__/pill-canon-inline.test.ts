/**
 * Source-guard test: verifies that key changed files no longer contain
 * non-pill radius tokens on <button> elements.
 *
 * This is a node/fs scan test — it does NOT render components.
 * It reads source files and asserts that previously-offending button
 * className strings have been updated to rounded-full.
 */

import * as fs from 'fs'
import * as path from 'path'

const src = path.resolve(__dirname, '..', '..')

function readFile(relPath: string): string {
  return fs.readFileSync(path.join(src, relPath), 'utf-8')
}

// Regex to detect old (non-pill) radius tokens in any context
const NON_PILL_BUTTON_PATTERN = /rounded-(sm|md|lg|xl|2xl|none)/

describe('Pill canon inline guard', () => {
  it('EnhancedRegistrationForm: submit button uses rounded-full', () => {
    const content = readFile('components/EnhancedRegistrationForm.tsx')
    // The submit button className should contain rounded-full and not rounded-md
    expect(content).toContain('rounded-full shadow-sm text-sm font-medium text-primary-foreground bg-primary')
    expect(content).not.toContain('rounded-md shadow-sm text-sm font-medium text-primary-foreground bg-primary')
  })

  it('app/reviews/page: filter buttons use rounded-full', () => {
    const content = readFile('app/reviews/page.tsx')
    expect(content).toContain('rounded-full text-sm font-semibold transition-all')
    expect(content).not.toContain('rounded-lg text-sm font-semibold transition-all')
  })

  it('components/cookies/CookieConsentBanner: action buttons use rounded-full', () => {
    const content = readFile('components/cookies/CookieConsentBanner.tsx')
    expect(content).toContain('rounded-full focus:outline-none focus:ring-2 focus:ring-gray-500')
    expect(content).not.toContain('rounded-md focus:outline-none focus:ring-2 focus:ring-gray-500')
  })

  it('components/ui/dialog: close button uses rounded-full', () => {
    const content = readFile('components/ui/dialog.tsx')
    expect(content).toContain('rounded-full opacity-70 ring-offset-background')
    expect(content).not.toContain('rounded-sm opacity-70 ring-offset-background')
    // Dialog panel container must NOT be changed
    expect(content).toContain('sm:rounded-lg')
  })

  it('components/wizard/Step4PhotoUpload: nav buttons use rounded-full', () => {
    const content = readFile('components/wizard/Step4PhotoUpload.tsx')
    expect(content).not.toContain('rounded-md hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"\n        >\n          Back')
    expect(content).toContain('rounded-full hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2')
    // labels are NOT buttons — label classes should remain unchanged (rounded-md is OK on labels)
  })

  it('components/workspace/EnhancedWorkspaceDashboard: tab and action buttons use rounded-full', () => {
    const content = readFile('components/workspace/EnhancedWorkspaceDashboard.tsx')
    expect(content).toContain('rounded-full transition-colors')
    expect(content).not.toContain('rounded-lg transition-colors')
  })

  it('components/feedback/FeedbackButton: close button uses rounded-full', () => {
    const content = readFile('components/feedback/FeedbackButton.tsx')
    expect(content).toContain('rounded-full text-muted-foreground hover:text-foreground hover:bg-muted transition-colors')
    expect(content).not.toContain('rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted transition-colors')
  })
})
