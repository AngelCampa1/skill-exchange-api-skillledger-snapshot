/**
 * Pill Canon — source-text assertions for CSS utility classes and global-error.
 *
 * DOM render tests are impractical for globals.css (pure CSS, no component)
 * and global-error.tsx (renders a full <html> document). We read the source
 * files directly and assert that pill-shaped corner tokens are present and
 * the old non-pill tokens are absent on the targeted classes/elements.
 */

import * as fs from 'fs'
import * as path from 'path'

// __dirname = web/src/app/__tests__ — go up 3 levels to reach web/
const webRoot = path.resolve(__dirname, '..', '..', '..')

describe('Pill Canon — source-text checks', () => {
  describe('globals.css .btn-* classes', () => {
    let cssSource: string

    beforeAll(() => {
      cssSource = fs.readFileSync(
        path.join(webRoot, 'src', 'app', 'globals.css'),
        'utf-8'
      )
    })

    it('.btn-primary uses rounded-full (not rounded-2xl)', () => {
      // Extract the .btn-primary rule block
      const btnPrimaryMatch = cssSource.match(/\.btn-primary\s*\{[^}]+\}/)
      expect(btnPrimaryMatch).not.toBeNull()
      const rule = btnPrimaryMatch![0]
      expect(rule).toContain('rounded-full')
      expect(rule).not.toContain('rounded-2xl')
    })

    it('.btn-secondary uses rounded-full (not rounded-2xl)', () => {
      const btnSecondaryMatch = cssSource.match(/\.btn-secondary\s*\{[^}]+\}/)
      expect(btnSecondaryMatch).not.toBeNull()
      const rule = btnSecondaryMatch![0]
      expect(rule).toContain('rounded-full')
      expect(rule).not.toContain('rounded-2xl')
    })

    it('.btn-ghost uses rounded-full (not rounded-xl)', () => {
      const btnGhostMatch = cssSource.match(/\.btn-ghost\s*\{[^}]+\}/)
      expect(btnGhostMatch).not.toBeNull()
      const rule = btnGhostMatch![0]
      expect(rule).toContain('rounded-full')
      expect(rule).not.toContain('rounded-xl')
    })
  })

  describe('global-error.tsx recovery button', () => {
    let source: string

    beforeAll(() => {
      source = fs.readFileSync(
        path.join(webRoot, 'src', 'app', 'global-error.tsx'),
        'utf-8'
      )
    })

    it('recovery button uses rounded-full (not rounded-md)', () => {
      expect(source).toContain('rounded-full')
      expect(source).not.toContain('rounded-md')
    })
  })
})
