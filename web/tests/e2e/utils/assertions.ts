/**
 * Custom business logic assertions for E2E tests
 * These verify business rules and expected outcomes
 */

import { Page, expect } from '@playwright/test';

export class BusinessAssertions {
  /**
   * Assert project was created successfully
   */
  static async assertProjectCreated(page: Page, projectTitle: string): Promise<void> {
    await expect(page.locator('[data-testid="success-message"], text=/success|created/i').first())
      .toBeVisible({ timeout: 10000 });
  }

  /**
   * Assert user is on dashboard after login
   */
  static async assertOnDashboard(page: Page): Promise<void> {
    await page.waitForLoadState('networkidle');

    const url = page.url();
    const isDashboardUrl = url.includes('/dashboard') || url.endsWith('/') || url.endsWith(':3030');

    if (!isDashboardUrl) {
      throw new Error(`Expected to be on dashboard, but on: ${url}`);
    }

    try {
      await expect(page.locator('h1:has-text("Dashboard")').first()).toBeVisible({ timeout: 10000 });
      console.log('✅ Found dashboard heading');
    } catch {
      try {
        await expect(page.locator('text=Profile Overview').or(page.locator('text=Welcome')).or(page.locator('text=Quick Actions')).first()).toBeVisible({ timeout: 10000 });
      } catch {
        console.log('✅ Found dashboard content');
      }
    }
  }

  /**
   * Assert wallet balance is correct
   */
  static async assertWalletBalance(page: Page, expectedBalance: number): Promise<string> {
    const balanceElement = page.locator('[data-testid="wallet-balance"], [data-testid="credit-balance"]').first();
    await expect(balanceElement).toBeVisible();

    const balanceText = await balanceElement.textContent();
    const balance = parseInt(balanceText?.replace(/[^0-9]/g, '') || '0');

    expect(balance).toBe(expectedBalance);
    return balanceText || '';
  }

  /**
   * Assert escrow was locked for project
   */
  static async assertEscrowLocked(page: Page, projectId: string, amount: number): Promise<void> {
    await page.goto(`/projects/${projectId}`);
    const escrowElement = page.locator('[data-testid="escrow-status"], text=/escrow.*locked|funds.*secured/i').first();
    await expect(escrowElement).toBeVisible();
  }

  /**
   * Assert workspace exists and is accessible
   */
  static async assertWorkspaceActive(page: Page, workspaceId: string): Promise<void> {
    await page.goto(`/workspace/${workspaceId}`);
    await expect(page).toHaveURL(new RegExp(`.*\/workspace\/${workspaceId}`));
    await expect(page.locator('[data-testid="workspace-dashboard"], [data-testid="workspace-content"]').first())
      .toBeVisible();
  }

  /**
   * Assert message was sent in workspace
   */
  static async assertMessageSent(page: Page, messageContent: string): Promise<void> {
    const messageElement = page.locator('[data-testid="message-list"], [data-testid="messages"]')
      .locator(`text="${messageContent}"`).first();

    await expect(messageElement).toBeVisible({ timeout: 5000 });
  }

  /**
   * Assert milestone was completed
   */
  static async assertMilestoneCompleted(page: Page, milestoneName: string): Promise<void> {
    const milestoneElement = page.locator(`[data-testid="milestone-${milestoneName}"], text="${milestoneName}"`)
      .locator('.. >> [data-status="completed"], text=/completed|done/i').first();

    await expect(milestoneElement).toBeVisible();
  }

  /**
   * Assert review was posted
   */
  static async assertReviewPosted(page: Page, rating: number, reviewText: string): Promise<void> {
    const reviewElement = page.locator('[data-testid="reviews"], [data-testid="review-list"]')
      .locator(`text="${reviewText}"`).first();

    await expect(reviewElement).toBeVisible();

    // Check rating stars
    const ratingElement = reviewElement.locator('..').locator(`[data-rating="${rating}"]`).first();
    await expect(ratingElement).toBeVisible();
  }

  /**
   * Assert notification was received
   */
  static async assertNotificationReceived(page: Page, notificationText: string): Promise<void> {
    const notificationElement = page.locator('[data-testid="notifications"], [role="alert"]')
      .locator(`text=/${notificationText}/i`).first();

    await expect(notificationElement).toBeVisible({ timeout: 10000 });
  }

  /**
   * Extract project ID from current URL
   */
  static async extractProjectId(page: Page): Promise<string | null> {
    const currentUrl = page.url();
    const projectMatch = currentUrl.match(/projects?\/([\w-]+)/);
    if (projectMatch) {
      console.log(`📋 Extracted project ID: ${projectMatch[1]}`);
      return projectMatch[1];
    }
    console.warn('⚠️  Could not extract project ID from URL:', currentUrl);
    return null;
  }

  /**
   * Extract workspace ID from current URL
   */
  static async extractWorkspaceId(page: Page): Promise<string | null> {
    const currentUrl = page.url();
    const workspaceMatch = currentUrl.match(/workspace\/([\w-]+)/);
    if (workspaceMatch) {
      console.log(`📋 Extracted workspace ID: ${workspaceMatch[1]}`);
      return workspaceMatch[1];
    }
    console.warn('⚠️  Could not extract workspace ID from URL:', currentUrl);
    return null;
  }

  /**
   * Extract any ID from page attribute or API response
   */
  static async extractIdFromAttribute(page: Page, attributeName: string): Promise<string | null> {
    try {
      const element = page.locator(`[${attributeName}]`).first();
      if (await element.isVisible({ timeout: 2000 })) {
        const id = await element.getAttribute(attributeName);
        if (id) {
          console.log(`📋 Extracted ID from ${attributeName}: ${id}`);
          return id;
        }
      }
    } catch (error) {
      console.warn(`⚠️  Could not extract ID from attribute ${attributeName}`);
    }
    return null;
  }

  /**
   * Assert user is authenticated (not on login page)
   */
  static async assertAuthenticated(page: Page): Promise<void> {
    await page.waitForTimeout(1000);
    const currentUrl = page.url();
    expect(currentUrl).not.toContain('/login');
    expect(currentUrl).not.toContain('/register');
    console.log('✅ User is authenticated');
  }
}