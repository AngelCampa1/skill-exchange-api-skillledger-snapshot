import { defineConfig, devices } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/e2e/journeys',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI and limit workers for local stability */
  workers: process.env.CI ? 1 : 4, // Reduced from undefined to 4 workers for server stability
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Global setup and teardown */
  globalSetup: './tests/e2e/setup.ts',
  globalTeardown: './tests/e2e/global-teardown.ts',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: process.env.BASE_URL || 'http://localhost:3030',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',

    /* Take screenshot on failure */
    screenshot: 'only-on-failure',

    /* Record video on failure */
    video: 'retain-on-failure',

    /* Increase timeout for CI/slow systems */
    actionTimeout: 15000,
    navigationTimeout: 45000, // Increased from 30s to 45s for slower pages
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },

    /* Test against branded browsers. */
    {
      name: 'Microsoft Edge',
      use: { ...devices['Desktop Edge'], channel: 'msedge' },
    },
    {
      name: 'Google Chrome',
      use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    },
  ],

  /* Run your local dev servers before starting the tests */
  webServer: process.env.SKIP_SERVER_START ? undefined : [
    {
      // Backend API Server
      command: process.platform === 'win32'
        ? 'cd ..\\src\\SkillLedger.Api && dotnet run'
        : 'cd ../src/SkillLedger.Api && dotnet run',
      url: 'http://localhost:8030/api/test/health',
      reuseExistingServer: !process.env.CI, // Reuse existing server locally for stability
      timeout: 180 * 1000, // Increased timeout for slower startup
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      // Frontend Server
      command: process.platform === 'win32'
        ? 'npm run dev'
        : 'npm run dev',
      url: 'http://localhost:3030',
      reuseExistingServer: !process.env.CI, // Reuse existing server locally for stability
      timeout: 180 * 1000, // Increased timeout for slower startup
      stdout: 'pipe',
      stderr: 'pipe',
    },
  ],
});