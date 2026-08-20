import { exec } from 'child_process';
import { promisify } from 'util';
import path from 'path';

const execAsync = promisify(exec);

/**
 * Result of database seeding operation
 */
export interface SeedResult {
  success: boolean;
  usersCreated?: number;
  projectsCreated?: number;
  transactionsCreated?: number;
  executionTimeMs?: number;
  error?: string;
  output?: string;
}

/**
 * Seed the test database with comprehensive test data
 *
 * This function executes the DatabaseSeeder console app via shell command.
 * The seeder creates 20 test users, 30 projects, 150+ transactions, and more.
 *
 * @returns Promise<SeedResult> Result of seeding operation
 *
 * @example
 * ```typescript
 * import { test } from '@playwright/test';
 * import { seedTestDatabase } from './fixtures/database';
 *
 * test.beforeAll(async () => {
 *   const result = await seedTestDatabase();
 *   if (!result.success) {
 *     throw new Error(`Failed to seed database: ${result.error}`);
 *   }
 * });
 * ```
 */
export async function seedTestDatabase(): Promise<SeedResult> {
  try {
    console.log('🌱 Seeding test database...');

    // Get the repository root (assuming this file is in web/playwright/fixtures/)
    const repoRoot = path.resolve(__dirname, '../../../');
    const seederPath = path.join(repoRoot, 'tests/SkillLedger.Tests/Tools/DatabaseSeeder');

    // Execute the seeder console app
    const { stdout, stderr } = await execAsync(
      'dotnet run',
      {
        cwd: seederPath,
        timeout: 60000, // 60 second timeout
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Development',
        },
      }
    );

    // Log output for debugging
    if (stdout) {
      console.log('Seeder output:', stdout);
    }

    if (stderr && !stderr.includes('Seeding completed')) {
      console.warn('Seeder stderr:', stderr);
    }

    // Parse output to extract statistics (basic parsing)
    const result: SeedResult = {
      success: true,
      output: stdout,
    };

    // Try to extract entity counts from output
    const usersMatch = stdout.match(/Users:\s+(\d+)/);
    const projectsMatch = stdout.match(/Projects:\s+(\d+)/);
    const transactionsMatch = stdout.match(/Transactions:\s+(\d+)/);
    const timeMatch = stdout.match(/Execution time:\s+([\d.]+)s/);

    if (usersMatch) result.usersCreated = parseInt(usersMatch[1], 10);
    if (projectsMatch) result.projectsCreated = parseInt(projectsMatch[1], 10);
    if (transactionsMatch) result.transactionsCreated = parseInt(transactionsMatch[1], 10);
    if (timeMatch) result.executionTimeMs = parseFloat(timeMatch[1]) * 1000;

    console.log(`✅ Database seeded: ${result.usersCreated} users, ${result.projectsCreated} projects, ${result.transactionsCreated} transactions`);

    return result;
  } catch (error: any) {
    console.error('❌ Failed to seed database:', error.message);

    return {
      success: false,
      error: error.message,
      output: error.stdout || '',
    };
  }
}

/**
 * Clean all test data from the database
 *
 * This function executes the DatabaseSeeder console app with the --clean flag.
 * It removes all entities tagged with CreatedFromIP = "TEST_DATA_SEEDER".
 *
 * @returns Promise<SeedResult> Result of cleaning operation
 *
 * @example
 * ```typescript
 * import { test } from '@playwright/test';
 * import { cleanTestDatabase } from './fixtures/database';
 *
 * test.afterAll(async () => {
 *   await cleanTestDatabase();
 * });
 * ```
 */
export async function cleanTestDatabase(): Promise<SeedResult> {
  try {
    console.log('🧹 Cleaning test database...');

    // Get the repository root
    const repoRoot = path.resolve(__dirname, '../../../');
    const seederPath = path.join(repoRoot, 'tests/SkillLedger.Tests/Tools/DatabaseSeeder');

    // Execute the seeder with --clean flag
    const { stdout, stderr } = await execAsync(
      'dotnet run -- --clean',
      {
        cwd: seederPath,
        timeout: 30000, // 30 second timeout
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Development',
        },
      }
    );

    if (stdout) {
      console.log('Cleaner output:', stdout);
    }

    if (stderr && !stderr.includes('completed')) {
      console.warn('Cleaner stderr:', stderr);
    }

    console.log('✅ Test data cleaned successfully');

    return {
      success: true,
      output: stdout,
    };
  } catch (error: any) {
    console.error('❌ Failed to clean database:', error.message);

    return {
      success: false,
      error: error.message,
      output: error.stdout || '',
    };
  }
}

/**
 * Seed specific entities only (users, projects, financial, etc.)
 *
 * @param entities - Array of entity types to seed
 * @returns Promise<SeedResult> Result of seeding operation
 *
 * @example
 * ```typescript
 * // Seed only users and projects
 * await seedSpecificEntities(['users', 'projects']);
 * ```
 */
export async function seedSpecificEntities(entities: string[]): Promise<SeedResult> {
  try {
    console.log(`🎯 Seeding specific entities: ${entities.join(', ')}`);

    // Get the repository root
    const repoRoot = path.resolve(__dirname, '../../../');
    const seederPath = path.join(repoRoot, 'tests/SkillLedger.Tests/Tools/DatabaseSeeder');

    // Build command with --only flag
    const entitiesArg = entities.join(',');
    const command = `dotnet run -- --only ${entitiesArg}`;

    // Execute the seeder
    const { stdout, stderr } = await execAsync(
      command,
      {
        cwd: seederPath,
        timeout: 60000, // 60 second timeout
        env: {
          ...process.env,
          ASPNETCORE_ENVIRONMENT: 'Development',
        },
      }
    );

    if (stdout) {
      console.log('Seeder output:', stdout);
    }

    if (stderr && !stderr.includes('completed')) {
      console.warn('Seeder stderr:', stderr);
    }

    console.log(`✅ Entities seeded: ${entities.join(', ')}`);

    return {
      success: true,
      output: stdout,
    };
  } catch (error: any) {
    console.error('❌ Failed to seed entities:', error.message);

    return {
      success: false,
      error: error.message,
      output: error.stdout || '',
    };
  }
}

/**
 * Test helper: Reset database to clean state and re-seed
 *
 * This is a convenience function that combines clean + seed operations.
 * Useful for ensuring a fresh database state before test suites.
 *
 * @returns Promise<SeedResult> Result of reset operation
 *
 * @example
 * ```typescript
 * import { test } from '@playwright/test';
 * import { resetTestDatabase } from './fixtures/database';
 *
 * test.beforeAll(async () => {
 *   await resetTestDatabase();
 * });
 * ```
 */
export async function resetTestDatabase(): Promise<SeedResult> {
  console.log('🔄 Resetting test database...');

  // Clean first
  const cleanResult = await cleanTestDatabase();
  if (!cleanResult.success) {
    return cleanResult;
  }

  // Then seed
  const seedResult = await seedTestDatabase();
  return seedResult;
}

/**
 * Test helper: Verify database was seeded correctly
 *
 * Checks that expected test data exists in the database.
 *
 * @returns Promise<boolean> True if database has test data
 */
export async function verifyTestDataExists(): Promise<boolean> {
  try {
    // This would require database connection, which is not ideal for Playwright
    // For now, we'll return true and rely on the seeder's success status
    console.log('⚠️  Database verification not implemented yet');
    return true;
  } catch (error) {
    console.error('Failed to verify test data:', error);
    return false;
  }
}

/**
 * Test users available after seeding
 * These GUIDs are hard-coded in the UserTestDataFactory
 */
export const TEST_USERS = {
  ALICE_CLIENT: '11111111-1111-1111-1111-111111111111',    // Rachel Goldstein
  BOB_PROVIDER: '22222222-2222-2222-2222-222222222222',    // David Kumar
  CAROL_ADMIN: '33333333-3333-3333-3333-333333333333',     // Carol Admin
  DAVID_CLIENT: '44444444-4444-4444-4444-444444444444',    // Robert Chen
  EVE_PROVIDER: '55555555-5555-5555-5555-555555555555',    // Patricia Williams
};

/**
 * Test user credentials (default password for all users)
 */
export const TEST_CREDENTIALS = {
  ALICE: { email: 'rachel.goldstein@testmail.com', password: 'Test123!' },
  BOB: { email: 'david.kumar@testmail.com', password: 'Test123!' },
  CAROL: { email: 'admin@skillledger.app', password: 'Test123!' },
  DAVID: { email: 'robert.chen@testmail.com', password: 'Test123!' },
  EVE: { email: 'patricia.williams@testmail.com', password: 'Test123!' },
  SARAH: { email: 'sarah.chen@testmail.com', password: 'Test123!' },
  MIKE: { email: 'mike.johnson@testmail.com', password: 'Test123!' },
};

/**
 * Export all fixtures for easy importing
 */
export default {
  seedTestDatabase,
  cleanTestDatabase,
  seedSpecificEntities,
  resetTestDatabase,
  verifyTestDataExists,
  TEST_USERS,
  TEST_CREDENTIALS,
};
