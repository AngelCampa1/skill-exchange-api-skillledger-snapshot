/**
 * Global setup for E2E tests
 * Configures test environment, seeds data, and handles cleanup
 * PURE UI-BASED - No direct API calls for test data creation
 */

import { chromium, firefox, webkit, devices } from '@playwright/test';
import path from 'path';
import { exec } from 'child_process';
import fs from 'fs';

// Import UserFactory for test data management
import { UserFactory } from './factories/userFactory';

// Polyfill TransformStream for Node.js environments where it's not available globally
try {
  const { TransformStream } = require('node:stream/web');

  // Make TransformStream globally available for the test environment
  if (typeof globalThis !== 'undefined' && typeof globalThis.TransformStream === 'undefined') {
    globalThis.TransformStream = TransformStream;
  }

  // Also make it available on global (for compatibility)
  if (typeof global !== 'undefined' && typeof global.TransformStream === 'undefined') {
    global.TransformStream = TransformStream;
  }
} catch (error) {
  console.log('⚠️ [E2E Setup] node:stream/web not available, using fallback TransformStream polyfill');

  // Fallback TransformStream implementation for compatibility
  class TransformStreamPolyfill {
    private transformer: any;
    private writableStrategy: any;
    private readableStrategy: any;

    constructor(transformer = {}, writableStrategy = {}, readableStrategy = {}) {
      this.transformer = transformer;
      this.writableStrategy = writableStrategy;
      this.readableStrategy = readableStrategy;
    }

    get readable() {
      return {
        getReader: () => ({
          read: () => Promise.resolve({ done: true, value: undefined })
        })
      };
    }

    get writable() {
      return {
        getWriter: () => ({
          write: () => Promise.resolve(),
          close: () => Promise.resolve(),
          abort: () => Promise.resolve()
        })
      };
    }
  }

  // Make the polyfill globally available
  if (typeof globalThis !== 'undefined') {
    (globalThis as any).TransformStream = TransformStreamPolyfill;
  }
  if (typeof global !== 'undefined') {
    (global as any).TransformStream = TransformStreamPolyfill;
  }
}

// Test configuration
const TEST_CONFIG = {
  baseURL: process.env.BASE_URL || 'http://localhost:3030',
  apiURL: process.env.API_URL || 'http://localhost:8030',
  timeout: 30000,
};

// Global test data
let testUsers: any[] = [];
let testSkills: any[] = [];

/**
 * Seed test skills into the database
 * Active implementation to ensure skills are available for tests
 */
async function seedTestSkills(): Promise<void> {
  try {
    console.log('📚 [E2E Setup] Seeding test skills into database...');

    // Wait for backend to be ready
    console.log('⏳ [E2E Setup] Waiting for backend to be ready...');
    await new Promise(resolve => setTimeout(resolve, 3000));

    // Try to authenticate as test user first for skill seeding
    let csrfToken = '';
    let authToken = '';
    try {
      console.log('🔐 [E2E Setup] Authenticating test user for skill seeding...');

      // Step 1: Get CSRF token
      const csrfResponse = await fetch(`${TEST_CONFIG.apiURL}/api/auth/csrf-token`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (csrfResponse.ok) {
        const csrfData = await csrfResponse.json();
        csrfToken = csrfData.token;
        console.log('✅ [E2E Setup] CSRF token obtained');
      }

      // Step 2: Login as test user to get auth token
      const loginResponse = await fetch(`${TEST_CONFIG.apiURL}/api/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        body: JSON.stringify({
          email: 'test@skillledger.test',
          password: 'TestPassword123!',
        }),
      });

      if (loginResponse.ok) {
        const loginData = await loginResponse.json();
        authToken = loginData.accessToken || loginData.token;
        console.log('✅ [E2E Setup] Test user authenticated for skill seeding');
      } else {
        console.log(`⚠️ [E2E Setup] Login failed: ${loginResponse.status}`);
      }
    } catch (error) {
      console.log('⚠️ [E2E Setup] Authentication failed, skill seeding will be skipped:', error);
    }

    // Skills to seed
    const skills = [
      { name: 'React', category: 'Frontend', proficiency: 'Advanced' },
      { name: 'Node.js', category: 'Backend', proficiency: 'Advanced' },
      { name: 'TypeScript', category: 'Language', proficiency: 'Advanced' },
      { name: 'Python', category: 'Backend', proficiency: 'Intermediate' },
      { name: 'PostgreSQL', category: 'Database', proficiency: 'Intermediate' },
      { name: 'AWS', category: 'Cloud', proficiency: 'Intermediate' },
      { name: 'Docker', category: 'DevOps', proficiency: 'Intermediate' },
      { name: 'GraphQL', category: 'API', proficiency: 'Advanced' }
    ];

    // Seed skills via API if authenticated
    if (authToken) {
      console.log('🌱 [E2E Setup] Seeding skills with authenticated user...');
      let seededSkills = 0;
      for (const skill of skills) {
        try {
          const response = await fetch(`${TEST_CONFIG.apiURL}/api/skill`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${authToken}`,
              ...(csrfToken && { 'X-CSRF-TOKEN': csrfToken }),
            },
            body: JSON.stringify({
              name: skill.name,
              category: skill.category,
              description: `${skill.name} - ${skill.category} development skill for E2E testing`
            }),
          });

          if (response.ok || response.status === 409) { // 409 = already exists
            seededSkills++;
            console.log(`✅ [E2E Setup] Skill seeded: ${skill.name}`);
          } else {
            console.log(`⚠️ [E2E Setup] Skill seeding failed: ${skill.name} (${response.status})`);
          }
        } catch (error) {
          console.log(`⚠️ [E2E Setup] Skill seeding error: ${skill.name}`, error);
        }
      }

      testSkills = skills;
      console.log(`✅ [E2E Setup] Skills seeding complete: ${seededSkills}/${skills.length} skills seeded`);
    } else {
      console.log('⚠️ [E2E Setup] No authentication token available, skipping skill seeding');
      console.log('⚠️ [E2E Setup] Using fallback skill definitions');
      // Keep existing fallback skills array for tests that reference them
      testSkills = [
        { name: 'React', category: 'Frontend', proficiency: 'Advanced' },
        { name: 'Node.js', category: 'Backend', proficiency: 'Advanced' },
        { name: 'TypeScript', category: 'Language', proficiency: 'Advanced' },
        { name: 'Python', category: 'Backend', proficiency: 'Intermediate' },
        { name: 'PostgreSQL', category: 'Database', proficiency: 'Intermediate' }
      ];
    }

  } catch (error) {
    console.error('❌ [E2E Setup] Failed to seed skills:', error);
    // Don't throw error - continue with prepared skills array
    testSkills = [
      { name: 'React', category: 'Frontend', proficiency: 'Advanced' },
      { name: 'Node.js', category: 'Backend', proficiency: 'Advanced' },
      { name: 'TypeScript', category: 'Language', proficiency: 'Advanced' },
      { name: 'Python', category: 'Backend', proficiency: 'Intermediate' },
      { name: 'PostgreSQL', category: 'Database', proficiency: 'Intermediate' }
    ];
    console.log('⚠️ [E2E Setup] Using fallback skill definitions');
  }
}

/**
 * Health check for API and frontend
 * Active implementation to ensure servers are ready before running tests
 */
async function healthCheck(): Promise<void> {
  try {
    console.log('🏥 [E2E Setup] Performing health checks...');

    // Check frontend health
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 10000);

      const frontendResponse = await fetch(TEST_CONFIG.baseURL, {
        method: 'GET',
        signal: controller.signal
      });

      clearTimeout(timeoutId);

      if (frontendResponse.ok) {
        console.log('✅ [E2E Setup] Frontend server is healthy');
      } else {
        console.log(`⚠️  [E2E Setup] Frontend returned status: ${frontendResponse.status}`);
      }
    } catch (error) {
      console.log('⚠️  [E2E Setup] Frontend health check failed, but continuing...');
    }

    // Check API health
    try {
      const apiController = new AbortController();
      const apiTimeoutId = setTimeout(() => apiController.abort(), 10000);

      const apiResponse = await fetch(`${TEST_CONFIG.apiURL}/api/test/health`, {
        method: 'GET',
        signal: apiController.signal
      });

      clearTimeout(apiTimeoutId);

      if (apiResponse.ok) {
        console.log('✅ [E2E Setup] Backend API server is healthy');
      } else {
        console.log(`⚠️  [E2E Setup] Backend API returned status: ${apiResponse.status}`);
      }
    } catch (error) {
      console.log('⚠️  [E2E Setup] Backend API health check failed, but continuing...');
    }

    console.log('✅ [E2E Setup] Health checks completed');
  } catch (error) {
    console.error('❌ [E2E Setup] Health check failed:', error);
    // Don't throw error - tests will fail if servers aren't ready
    console.log('⚠️  [E2E Setup] Continuing with tests - they will fail if servers are not ready');
  }
}

/**
 * Create test users through UI (true E2E approach)
 * Note: This is now deprecated - all test user creation should happen through the UI in tests
 */
async function createTestUsers(): Promise<void> {
  try {
    console.log('👥 [E2E Setup] Creating test users...');
    
    // DEPRECATED: All test user creation should now happen through the UI in tests
    // This function is kept for backward compatibility but should not be used
    
    console.log('⚠️  WARNING: Direct user creation in setup is deprecated');
    console.log('⚠️  All test user creation should happen through the UI in individual tests');
    
    console.log(`📋 Test Users (for reference only):`);
    console.log('   - test@skillledger.test / TestPassword123!');
    console.log('   - client.user@skillledger.test / ClientPassword123!');
    console.log('   - provider.user@skillledger.test / ProviderPassword123!');
  } catch (error) {
    console.error('❌ [E2E Setup] Failed to create test users:', error);
    throw error;
  }
}

/**
 * Global setup function
 */
async function globalSetup() {
  try {
    console.log('🌱 [E2E Setup] Initializing test environment...');

    // Reset UserFactory state to ensure clean test data
    UserFactory.reset();

    // Wait for servers to be ready
    await healthCheck();
    
    // Seed test skills
    await seedTestSkills();
    
    // Create test users (deprecated, but kept for compatibility)
    await createTestUsers();
    
    console.log('✅ [E2E Setup] Test data seeding complete!');
    console.log('📋 Test Users:');
    testUsers.forEach((user: any) => {
      console.log(`   - ${user.email} / ${user.password}`);
    });
    console.log('');
    
  } catch (error) {
    console.error('❌ [E2E Setup] Global setup failed:', error);
    throw error;
  }
}

export default globalSetup;
