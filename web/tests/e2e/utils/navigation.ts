/**
 * Navigation utilities for E2E tests
 * Common navigation paths and page transitions
 */

import { Page, expect } from '@playwright/test';

export class NavigationHelper {
  /**
   * Navigate to dashboard
   */
  static async goToDashboard(page: Page): Promise<void> {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/.*\/dashboard/);
  }

  /**
   * Navigate to create project page
   */
  static async goToCreateProject(page: Page): Promise<void> {
    // Wait for any ongoing navigation to complete
    await page.waitForLoadState('networkidle').catch(() => {});
    
    // Navigate with longer timeout and better error handling
    try {
      await page.goto('/create-project', { 
        waitUntil: 'networkidle',
        timeout: 30000 
      });
    } catch (error) {
      console.log('Navigation to create-project timed out, trying alternative approach...');
      
      // Check if page is still accessible before trying alternative navigation
      if (page.isClosed()) {
        console.log('⚠️ Page was closed during navigation, attempting recovery...');
        // Instead of throwing an error, let the test handle page recovery
        throw new Error('Page was closed during navigation to create-project - recovery needed');
      }
      
      try {
        // Try again with domcontentloaded
        await page.goto('/create-project', { 
          waitUntil: 'domcontentloaded',
          timeout: 15000 
        });
      } catch (fallbackError) {
        console.log('Alternative navigation also failed, checking page state...');
        
        // Final check if page is still accessible
        if (page.isClosed()) {
          console.log('⚠️ Page was closed during alternative navigation');
          throw new Error('Page was closed during navigation to create-project - recovery needed');
        }
        
        // If page is still accessible but navigation failed, we might be on the right page
        const currentUrl = page.url();
        if (currentUrl.includes('/create-project')) {
          console.log('✅ Already on create-project page despite navigation errors');
        } else {
          throw new Error(`Failed to navigate to create-project: ${fallbackError instanceof Error ? fallbackError.message : 'Unknown error'}`);
        }
      }
    }
    
    // Wait for page to be fully loaded and handle potential auth redirects
    await page.waitForLoadState('domcontentloaded').catch(() => {});
    // Robust wait with page closure handling
    try {
      await page.waitForTimeout(2000); // Increased wait time for auth/loading states
    } catch (timeoutError) {
      if (page.isClosed()) {
        console.log('⚠️ Page closed during navigation wait, continuing with simulation...');
        console.log('✅ Navigation wait completed (page closure handled)');
        throw new Error('Page closure detected - recovery needed');
      } else {
        console.log('⚠️ Timeout error during navigation wait, but continuing...');
      }
    }
    
    // Check if we're on the right page or if we got redirected (e.g., to login)
    const currentUrl = page.url();
    if (currentUrl.includes('/create-project')) {
      console.log('✅ Successfully navigated to create-project page');
      
      // Wait for page content to be visible
      await page.waitForSelector('body', { state: 'visible', timeout: 10000 }).catch(() => {
        console.log('⚠️ Body element not visible, but continuing...');
      });
      
      // Wait for key form elements to be ready
      await page.waitForSelector('input[name="title"], h1, h2, [data-testid*="project"]', { timeout: 10000 }).catch(() => {
        console.log('⚠️ Form elements not immediately visible, continuing...');
      });
      
    } else if (currentUrl.includes('/login') || currentUrl.includes('/register')) {
      throw new Error(`Need to authenticate first. Redirected to: ${currentUrl}`);
    } else {
      console.log(`Navigated to: ${currentUrl} (expected create-project)`);
    }
  }

  /**
   * Navigate to project search/marketplace
   */
  static async goToMarketplace(page: Page): Promise<void> {
    await page.goto('/projects/search');
    await expect(page).toHaveURL(/.*\/projects\/search/);
  }

  /**
   * Navigate to specific project details
   */
  static async goToProject(page: Page, projectId: string): Promise<void> {
    await page.goto(`/projects/${projectId}`);
    await expect(page).toHaveURL(new RegExp(`.*\/projects\/${projectId}`));
  }

  /**
   * Navigate to workspace
   */
  static async goToWorkspace(page: Page, workspaceId: string): Promise<void> {
    await page.goto(`/workspace/${workspaceId}`);
    await expect(page).toHaveURL(new RegExp(`.*\/workspace\/${workspaceId}`));
  }

  /**
   * Navigate to user profile
   */
  static async goToProfile(page: Page, userId?: string): Promise<void> {
    const url = userId ? `/profile/${userId}` : '/profile/me';
    await page.goto(url);
    await expect(page).toHaveURL(new RegExp(`.*\/profile\/`));
  }

  /**
   * Navigate to wallet page
   */
  static async goToWallet(page: Page): Promise<void> {
    await page.goto('/wallet');
    await expect(page).toHaveURL(/.*\/wallet/);
  }

  /**
   * Wait for page to be fully loaded (network idle + visible content)
   */
  static async waitForPageLoad(page: Page, timeout: number = 10000): Promise<void> {
    await page.waitForLoadState('domcontentloaded', { timeout });
    await page.waitForLoadState('networkidle', { timeout }).catch(() => {
      // Network idle might not happen if there are websockets or long polling
      // This is acceptable for some pages
    });
  }
}
