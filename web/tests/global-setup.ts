/**
 * Playwright Global Setup
 * Runs once before all E2E tests
 * Seeds test data into the database
 */

import { FullConfig } from '@playwright/test';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8030';

interface TestUser {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

const TEST_USERS: TestUser[] = [
  {
    email: 'test@skillledger.test',
    firstName: 'Test',
    lastName: 'User',
    password: 'TestPassword123!',
  },
  {
    email: 'client.user@skillledger.test',
    firstName: 'Client',
    lastName: 'User',
    password: 'ClientPassword123!',
  },
  {
    email: 'provider.user@skillledger.test',
    firstName: 'Provider',
    lastName: 'User',
    password: 'ProviderPassword123!',
  },
];

async function globalSetup(config: FullConfig) {
  console.log('\n🌱 [E2E Setup] Seeding test data...\n');

  // Check if backend is running
  try {
    const healthCheck = await fetch(`${API_URL}/api/health`, {
      method: 'GET',
    });

    if (!healthCheck.ok) {
      throw new Error('Health check failed');
    }
  } catch (error) {
    console.error('\n❌ [E2E Setup] Backend API is not running!');
    console.error(`   Expected: ${API_URL}`);
    console.error('   Please start the backend first:');
    console.error('   > dotnet run --project src/SkillLedger.Api\n');
    process.exit(1);
  }

  // Create test users
  for (const user of TEST_USERS) {
    try {
      console.log(`📝 Creating user: ${user.email}...`);

      const response = await fetch(`${API_URL}/api/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: user.email,
          password: user.password,
          confirmPassword: user.password,
          firstName: user.firstName,
          lastName: user.lastName,
          acceptedTerms: true,
        }),
      });

      const data = await response.json();

      if (response.ok && data.success) {
        console.log(`   ✅ User registered: ${user.email}`);

        // Auto-verify email using test endpoint (E2E testing only)
        try {
          const verifyResponse = await fetch(`${API_URL}/api/test/verify-email-auto`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({ email: user.email }),
          });

          if (verifyResponse.ok) {
            console.log(`   ✅ Email auto-verified: ${user.email}`);
          } else {
            console.warn(`   ⚠️  Could not auto-verify email: ${user.email}`);
          }
        } catch (error) {
          console.warn(`   ⚠️  Email verification error for ${user.email}`);
        }
      } else if (data.message?.includes('already registered') || data.message?.includes('already exists')) {
        console.log(`   ⏩ User already exists: ${user.email}`);
      } else {
        console.warn(`   ⚠️  Failed to register ${user.email}: ${data.message}`);
      }
    } catch (error: any) {
      if (error.message?.includes('already registered')) {
        console.log(`   ⏩ User already exists: ${user.email}`);
      } else {
        console.error(`   ❌ Error registering ${user.email}:`, error.message);
      }
    }
  }

  console.log('\n✅ [E2E Setup] Test data seeding complete!\n');
  console.log('📋 Test Users:');
  TEST_USERS.forEach(user => {
    console.log(`   - ${user.email} / ${user.password}`);
  });
  console.log('');
}

export default globalSetup;

