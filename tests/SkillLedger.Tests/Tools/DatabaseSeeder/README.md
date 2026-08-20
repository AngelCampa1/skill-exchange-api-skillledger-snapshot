# SkillLedger Database Test Data Seeder

Console application for seeding realistic test data into the SkillLedger development database.

## Overview

The DatabaseSeeder is a standalone console application that populates the SkillLedger database with comprehensive test data covering all system entities. This is essential for:

- **E2E Manual Testing**: Provides realistic data for Playwright MCP testing
- **Development**: Pre-populated database for local development
- **Integration Testing**: Consistent test data for automated tests
- **Demo Environments**: Realistic data for demonstrations

## Features

- ✅ **20 Test User Personas** - From free tier to enterprise users
- ✅ **30 Test Projects** - Across all states (draft, published, in-progress, completed, disputed)
- ✅ **Encrypted Financial Data** - Credit wallets with AES-256 encryption
- ✅ **Complete Transaction History** - 150+ transactions with hash chains
- ✅ **Collaboration Data** - Workspaces, messages, documents
- ✅ **Reputation System** - Project reviews and reputation scores
- ✅ **Idempotent Operation** - Can run multiple times safely
- ✅ **Selective Seeding** - Seed specific entities only
- ✅ **Clean Operation** - Remove all test data

## Prerequisites

- .NET 9.0 SDK
- SQL Server (localhost\SQLEXPRESS01)
- SkillLedger database created and migrated

## Installation

No installation required. The tool is part of the SkillLedger repository.

## Usage

### Basic Commands

```bash
# Seed entire database (recommended)
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder

# Clean all test data
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder -- --clean

# Seed specific entities only
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder -- --only users,projects

# Verbose output with detailed timing
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder -- --verbose

# Show help
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder -- --help
```

### Running from Tool Directory

```bash
cd tests/SkillLedger.Tests/Tools/DatabaseSeeder

# Seed entire database
dotnet run

# Clean test data
dotnet run -- --clean

# Seed specific entities
dotnet run -- --only users,financial
```

## Command-Line Options

| Option | Description |
|--------|-------------|
| `--clean` | Remove all test data (tagged with `CreatedFromIP = "TEST_DATA_SEEDER"`) |
| `--only <entities>` | Seed only specific entities (comma-separated) |
| `--verbose`, `-v` | Show detailed output with timing information |
| `--help`, `-h` | Display help message |

## Entity Options (for --only)

| Entity | Description | Data Created |
|--------|-------------|--------------|
| `users` | Test user personas | 20 users, profiles, roles |
| `projects` | Test projects | 30 projects, deliverables, applications |
| `financial` | Financial data | Credit wallets, transactions, escrow |
| `collaboration` | Workspace data | Workspaces, messages, documents |
| `reputation` | Reviews and ratings | Project reviews, reputation scores |

## Seeding Phases

The seeder executes in 8 phases to respect foreign key dependencies:

### Phase 1: Foundation Data
- ✅ 4 subscription tiers (Free, Professional, Business, Enterprise)
- ✅ 40+ skills (React, Node.js, Python, UI/UX, etc.)
- ✅ 30 badge definitions (Verified Professional, Top Rated, etc.)

### Phase 2: User Data
- ✅ 20 test users via ASP.NET Identity with password hashing
- ✅ 3 roles (Client, Provider, Admin)
- ✅ User-role assignments

### Phase 3: User-Related Data
- ✅ 20 user profiles with bios and titles
- ✅ 20 encrypted credit wallets (AES-256)
- ✅ User subscriptions and skill proficiencies
- ✅ Work experience and education

### Phase 4: Project Data
- ✅ 30 projects in various states
- ✅ 200+ project deliverables
- ✅ 100+ project applications

### Phase 5: Financial Data
- ✅ 20 escrow accounts (Active, PartiallyReleased, Completed, Disputed)
- ✅ 150+ credit transactions (deposits, releases, fees, bonuses)
- ✅ P2P credit transfers
- ✅ Transaction hash chains

### Phase 6: Collaboration Data
- ✅ 15 project workspaces
- ✅ 45+ workspace messages
- ✅ 12+ workspace documents

### Phase 7: Reputation Data
- ✅ 50+ project reviews with various ratings
- ✅ Reputation score calculations
- ✅ Skill endorsements

### Phase 8: Audit Data
- ✅ 1000+ audit log entries

## Test User Credentials

All test users have the default password: **Test123!**

### Key Test Personas

| Name | Email | Role | Tier | Credits | GUID |
|------|-------|------|------|---------|------|
| Rachel Goldstein (Alice) | rachel.goldstein@testmail.com | Client | Pro | 5000 | 11111111-1111-1111-1111-111111111111 |
| David Kumar (Bob) | david.kumar@testmail.com | Provider | Pro | 2500 | 22222222-2222-2222-2222-222222222222 |
| Carol Admin | admin@skillledger.app | Admin | Pro | 1000 | 33333333-3333-3333-3333-333333333333 |
| Robert Chen (David) | robert.chen@testmail.com | Client | Business | 12000 | 44444444-4444-4444-4444-444444444444 |
| Patricia Williams (Eve) | patricia.williams@testmail.com | Provider | Enterprise | 5000 | 55555555-5555-5555-5555-555555555555 |

See `tests/TEST_DATA_REFERENCE.md` for complete list of all 20 personas.

## Configuration

Configuration is managed in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS01;Database=SkillLedgerDb_Dev;..."
  },
  "TestData": {
    "DefaultPassword": "Test123!",
    "UserCount": 20,
    "ProjectCount": 30,
    "UseHardCodedGuids": true,
    "CleanBeforeSeed": true
  }
}
```

### Connection String

The default connection string targets **localhost\SQLEXPRESS01** with Windows Authentication. For different environments:

**Windows Native (Default)**:
```
Server=localhost\SQLEXPRESS01;Database=SkillLedgerDb_Dev;Trusted_Connection=True;...
```

**Docker/Linux**:
```
Server=localhost,9030;Database=SkillLedgerDb_Dev;User Id=sa;Password=YourPassword;...
```

## Idempotency

The seeder is fully idempotent:

1. **Automatic Cleanup**: Before seeding, all existing test data is removed
2. **Test Data Marker**: All seeded entities have `CreatedFromIP = "TEST_DATA_SEEDER"`
3. **Safe Re-runs**: Running multiple times produces the same result
4. **No Duplicates**: Hard-coded GUIDs ensure consistent persona IDs

## Integration with Playwright

### Manual Testing (Playwright MCP)

Before starting E2E manual testing with Playwright MCP:

```bash
# Seed the database
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder

# Start backend API
dotnet run --project src/SkillLedger.Api

# Start frontend (in separate terminal)
cd web && yarn dev

# Now execute E2E tests manually with Playwright MCP browser tools
```

### Automated Testing (Playwright Fixtures)

In automated Playwright tests, use the database fixtures:

```typescript
import { test } from '@playwright/test';
import { seedTestDatabase } from './fixtures/database';

test.beforeAll(async () => {
  await seedTestDatabase(); // Executes console app via shell
});

test('user can login with test credentials', async ({ page }) => {
  await page.goto('http://localhost:3030/login');
  await page.fill('[name="email"]', 'rachel.goldstein@testmail.com');
  await page.fill('[name="password"]', 'Test123!');
  // ... test continues
});
```

## Output Example

```
═══════════════════════════════════════════════════════════
🌱 SkillLedger Database Test Data Seeder
═══════════════════════════════════════════════════════════

🌱 Seeding full test database...
🧹 Cleaning existing test data...
✅ Phase 1: Foundation data seeded (4 tiers, 50 skills, 30 badges)
✅ Phase 2: User data seeded (20 users, 3 roles)
✅ Phase 3: User-related data seeded (20 profiles, 20 wallets)
✅ Phase 4: Project data seeded (30 projects, 200+ deliverables)
✅ Phase 5: Financial data seeded (20 escrows, 150+ transactions)
✅ Phase 6: Collaboration data seeded (15 workspaces, 45+ messages)
✅ Phase 7: Reputation data seeded (50+ reviews)
✅ Phase 8: Audit data seeded (1000+ logs)

═══════════════════════════════════════════════════════════
✅ Database seeded successfully!
═══════════════════════════════════════════════════════════

📊 Summary:
   Users:               20
   Profiles:            20
   Projects:            30
   Wallets:             20
   Transactions:        150
   Escrow Accounts:     20
   Workspaces:          15
   Messages:            45
   Documents:           12
   Reviews:             50

⏱️  Execution time: 2.34s

═══════════════════════════════════════════════════════════
```

## Troubleshooting

### Database Connection Errors

```
❌ ERROR: Seeding failed!
   A network-related or instance-specific error occurred...
```

**Solution**: Verify SQL Server is running and connection string is correct.

```bash
# Check SQL Server status (Windows)
Get-Service | Where-Object {$_.Name -like '*SQL*'}

# Test connection
sqlcmd -S localhost\SQLEXPRESS01 -E -Q "SELECT @@VERSION"
```

### Foreign Key Constraint Violations

```
❌ ERROR: The INSERT statement conflicted with the FOREIGN KEY constraint...
```

**Solution**: Ensure database migrations are up to date.

```bash
cd src/SkillLedger.Api
dotnet ef database update
```

### Encryption Service Errors

```
❌ ERROR: Unable to encrypt wallet balance...
```

**Solution**: Verify `Encryption:MasterKey` is set in `appsettings.json`.

### Permission Denied

```
❌ ERROR: Access to the database is denied...
```

**Solution**: Ensure your Windows user has permissions on the SQL Server instance.

## Development

### Adding New Test Data

To add new test personas or scenarios:

1. **Edit factories** in `src/SkillLedger.Infrastructure/Services/TestData/`
   - `UserTestDataFactory.cs` - Add new user personas
   - `ProjectTestDataFactory.cs` - Add new project scenarios
   - `CreditTestDataFactory.cs` - Add financial scenarios
   - `WorkspaceTestDataFactory.cs` - Add collaboration scenarios

2. **Update TestDataSeederService.cs** if adding new entities

3. **Test changes**:
   ```bash
   dotnet run -- --clean  # Clean first
   dotnet run             # Verify new data is seeded
   ```

### Running Tests

```bash
# Build the console app
dotnet build

# Run with verbose output
dotnet run -- --verbose

# Test selective seeding
dotnet run -- --only users
dotnet run -- --only financial
```

## Security Notes

- ⚠️ **Development Only**: This tool is for development/testing environments only
- ⚠️ **Never use in production**: All data is tagged as test data
- ⚠️ **Weak Encryption Key**: The default encryption key is for development only
- ⚠️ **Simple Passwords**: All users have the same password (Test123!)
- ⚠️ **Predictable Data**: Hard-coded GUIDs make data predictable for testing

## Performance

- **Typical execution time**: 2-5 seconds for full seed
- **Database size after seeding**: ~50 MB
- **Entity count**: 1000+ entities across all tables
- **Optimizations**: Batch inserts where possible, minimal logging

## License

Part of the SkillLedger project. See main repository LICENSE file.

## Support

For issues or questions:
- Check `tests/E2E_TEST_PLAN.md` for test data requirements
- Check `tests/TEST_DATA_REFERENCE.md` for persona details
- Review this README's troubleshooting section
- Check existing test data in database with SQL queries

## Related Documentation

- `tests/E2E_TEST_PLAN.md` - Comprehensive E2E test scenarios (88 tests)
- `tests/TEST_DATA_REFERENCE.md` - Complete persona reference guide
- `web/playwright/fixtures/database.ts` - Playwright integration fixtures
- `docs/TDD_GUIDE.md` - Testing philosophy and practices
- `CLAUDE.md` - Project configuration and port assignments
