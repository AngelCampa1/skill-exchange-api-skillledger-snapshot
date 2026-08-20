/**
 * Debug test for registration form submission
 */

import { test, expect } from '@playwright/test';
import { AuthHelper } from './utils/auth';

test.describe('Registration Debug', () => {
  test('should register a new user successfully', async ({ page }) => {
    console.log('🔍 Starting registration debug test...');

    // Navigate to registration page
    await page.goto('/register', { waitUntil: 'domcontentloaded', timeout: 15000 });

    // Wait for form to load
    await page.waitForSelector('input[data-testid="email-input"]', { timeout: 10000 });

    const testUser = {
      email: `debug-test-${Date.now()}@skillledger-test.local`,
      password: 'DebugTest123!@#',
      firstName: 'Debug',
      lastName: 'Test',
      acceptTerms: true,
    };

    console.log('📝 Test user created:', testUser.email);

    try {
      // Attempt registration
      await AuthHelper.register(page, testUser);

      console.log('✅ Registration completed successfully');

      // Take screenshot for verification
      await page.screenshot({ path: `registration-debug-success-${Date.now()}.png`, fullPage: true });

      // Check final URL
      const finalUrl = page.url();
      console.log('📍 Final URL:', finalUrl);

      // Test should be considered successful if we're not on the registration page anymore
      expect(finalUrl).not.toContain('/register');

    } catch (error) {
      console.error('❌ Registration failed:', error);

      // Take screenshot for debugging
      await page.screenshot({ path: `registration-debug-error-${Date.now()}.png`, fullPage: true });

      // Check current URL for debugging
      const currentUrl = page.url();
      console.log('📍 Current URL at error:', currentUrl);

      throw error;
    }
  });
});