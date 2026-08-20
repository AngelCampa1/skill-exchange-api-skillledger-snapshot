import nextCoreWebVitals from 'eslint-config-next/core-web-vitals'

/**
 * ESLint flat config.
 *
 * Next.js 16 removed the `next lint` command, so linting now runs through the
 * ESLint CLI (`eslint .`) against this flat config. We extend the same ruleset
 * `next lint` used (`next/core-web-vitals`) and re-apply the project's prior
 * rule overrides from the retired `.eslintrc.json`.
 *
 * `eslint-config-next` 16 newly bundles `eslint-plugin-react-hooks` v6, which
 * adds React Compiler readiness rules (set-state-in-effect, immutability,
 * purity, preserve-manual-memoization, static-components, refs). The previous
 * `next lint` gate never enforced these. To restore the gate at parity instead
 * of forcing a large, risky cross-app refactor in this change, those rules are
 * set to `warn` so they stay visible for a dedicated Compiler-readiness pass.
 * `react-hooks/rules-of-hooks` stays at its default `error` severity.
 */
export default [
  {
    ignores: [
      '.next/**',
      '.open-next/**',
      '.wrangler/**',
      'out/**',
      'dist/**',
      'coverage/**',
      'playwright-report/**',
      'test-results/**',
      'node_modules/**',
      'next-env.d.ts',
    ],
  },
  ...nextCoreWebVitals,
  {
    rules: {
      'react/no-unescaped-entities': 'off',
      'react-hooks/exhaustive-deps': 'warn',
      'no-console': 'warn',
      'react/no-danger': 'warn',
      // React Compiler readiness rules — tracked for a dedicated pass; see header.
      'react-hooks/set-state-in-effect': 'warn',
      'react-hooks/immutability': 'warn',
      'react-hooks/purity': 'warn',
      'react-hooks/preserve-manual-memoization': 'warn',
      'react-hooks/static-components': 'warn',
      'react-hooks/refs': 'warn',
    },
  },
  {
    // Console output is expected in test harnesses, e2e helpers, and dev scripts.
    files: [
      '**/*.test.{ts,tsx}',
      '**/__tests__/**',
      'tests/**',
      'scripts/**',
      'playwright.config.ts',
      'jest.setup.{ts,js}',
    ],
    rules: {
      'no-console': 'off',
    },
  },
]
