# SkillLedger Test Data Reference Guide

**Version**: 1.0
**Last Updated**: 2026-01-13
**Database**: SkillLedgerDb_Dev
**Seeder Version**: 1.0

## Table of Contents

1. [Quick Reference](#quick-reference)
2. [Test User Personas (All 20)](#test-user-personas-all-20)
3. [Test Project Scenarios (All 30)](#test-project-scenarios-all-30)
4. [Financial Data Overview](#financial-data-overview)
5. [Hard-Coded GUIDs for Tests](#hard-coded-guids-for-tests)
6. [Common Test Scenarios](#common-test-scenarios)
7. [Database Queries](#database-queries)

---

## Quick Reference

### Test User Credentials

**Default Password for ALL users**: `Test123!`

| # | Name | Email | Role | Tier | Credits | Status | GUID |
|---|------|-------|------|------|---------|--------|------|
| 1 | Sarah Chen | sarah.chen@testmail.com | Client | Free | 100 | Active | 10000000-0000-0000-0000-000000000001 |
| 2 | Mike Johnson | mike.johnson@testmail.com | Provider | Free | 85 | Active | 10000000-0000-0000-0000-000000000002 |
| 3 | Emily Rodriguez | emily.rodriguez@testmail.com | Provider | Free | 15 | Active | 10000000-0000-0000-0000-000000000003 |
| 4 | James Park | james.park@testmail.com | Provider | Free | 45 | **Suspended** | 10000000-0000-0000-0000-000000000004 |
| 5 | Lisa Wong | lisa.wong@testmail.com | Client | Free | 100 | Active (Empty) | 10000000-0000-0000-0000-000000000005 |
| 6 | David Kumar (Bob) | david.kumar@testmail.com | Provider | Pro | 2500 | Active | **22222222-2222-2222-2222-222222222222** |
| 7 | Rachel Goldstein (Alice) | rachel.goldstein@testmail.com | Client | Pro | 5000 | Active | **11111111-1111-1111-1111-111111111111** |
| 8 | Marcus Thompson | marcus.thompson@testmail.com | Provider | Pro (Trial) | 300 | Active | 10000000-0000-0000-0000-000000000008 |
| 9 | Sophia Martinez | sophia.martinez@testmail.com | Provider | Pro (Past Due) | 180 | Active | 10000000-0000-0000-0000-000000000009 |
| 10 | Alex Kim | alex.kim@testmail.com | Provider | Pro | 1200 | Active | 10000000-0000-0000-0000-000000000010 |
| 11 | Jennifer Lee | jennifer.lee@testmail.com | Client | Business | 8500 | Active | 10000000-0000-0000-0000-000000000011 |
| 12 | Robert Chen (David) | robert.chen@testmail.com | Client | Business | 12000 | Active | **44444444-4444-4444-4444-444444444444** |
| 13 | Maria Santos | maria.santos@testmail.com | Client | Business | 450 | Active | 10000000-0000-0000-0000-000000000013 |
| 14 | Thomas Anderson | thomas.anderson@testmail.com | Client | Enterprise | 50000 | Active | 10000000-0000-0000-0000-000000000014 |
| 15 | Patricia Williams (Eve) | patricia.williams@testmail.com | Provider | Enterprise | 5000 | Active | **55555555-5555-5555-5555-555555555555** |
| 16 | Carol Administrator | admin@skillledger.app | Admin | Pro | 1000 | Active | **33333333-3333-3333-3333-333333333333** |
| 17 | Moderator User | moderator@skillledger.app | Moderator | Pro | 1000 | Active | 10000000-0000-0000-0000-000000000017 |
| 18 | John Doe | banned.user@testmail.com | Client | Free | 0 | **Banned** | 10000000-0000-0000-0000-000000000018 |
| 19 | Zero Balance | zero.balance@testmail.com | Provider | Free | 0 | Active | 10000000-0000-0000-0000-000000000019 |
| 20 | High Risk | high.risk@testmail.com | Provider | Free | 150 | Active (Flagged) | 10000000-0000-0000-0000-000000000020 |

---

## Test User Personas (All 20)

### Free Tier Users (5)

#### 1. Sarah Chen - New Free User
- **Email**: sarah.chen@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Free
- **Credits**: 100 (starting credits only)
- **Status**: Active
- **Phone Verified**: No
- **Tax Compliant**: No
- **Profile**: Complete
- **Purpose**: Test new user onboarding, free tier limitations
- **Use Cases**:
  - Register new account flow
  - Complete profile wizard
  - Test free tier restrictions
  - Attempt to exceed free tier limits

#### 2. Mike Johnson - Free User with Activity
- **Email**: mike.johnson@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Free
- **Credits**: 85
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: No
- **Profile**: Complete (Frontend Developer)
- **Purpose**: Free tier provider with some activity
- **Use Cases**:
  - Apply to projects as free provider
  - Test free tier application limits
  - Phone verification flow

#### 3. Emily Rodriguez - Free User Near Limit
- **Email**: emily.rodriguez@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Free
- **Credits**: 15 (very low)
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: No
- **Profile**: Complete (Content Writer)
- **Purpose**: Test low balance scenarios
- **Use Cases**:
  - Low balance warnings
  - Upgrade prompts
  - Limited transaction capabilities

#### 4. James Park - Suspended Free User
- **Email**: james.park@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Free
- **Credits**: 45
- **Status**: **Suspended** (Policy violation - spam)
- **Phone Verified**: Yes
- **Tax Compliant**: No
- **Wallet**: **Blocked**
- **Purpose**: Test suspended account restrictions
- **Use Cases**:
  - Login as suspended user
  - Attempt restricted actions
  - Wallet blocked behavior

#### 5. Lisa Wong - Empty State Free User
- **Email**: lisa.wong@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Free
- **Credits**: 100
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: No
- **Profile**: **No profile created** (empty state)
- **Purpose**: Test empty profile state
- **Use Cases**:
  - Profile creation wizard
  - Empty dashboard state
  - New user experience

---

### Professional Tier Users (5)

#### 6. David Kumar (Bob) - Active Pro Provider
- **Email**: david.kumar@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Professional
- **Credits**: 2500
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **GUID**: **22222222-2222-2222-2222-222222222222** (Hard-coded for tests)
- **Profile**: Complete (Full-Stack Developer)
- **Reputation**: 9.2/10 (47 completed projects)
- **Purpose**: High-reputation pro provider (Bob persona)
- **Use Cases**:
  - Apply to projects
  - Complete project deliverables
  - Receive escrow releases
  - High-value transactions

#### 7. Rachel Goldstein (Alice) - Active Pro Client
- **Email**: rachel.goldstein@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Professional
- **Credits**: 5000
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **GUID**: **11111111-1111-1111-1111-111111111111** (Hard-coded for tests)
- **Profile**: Complete (Startup Founder)
- **Active Projects**: 3
- **Purpose**: High-activity pro client (Alice persona)
- **Use Cases**:
  - Create and publish projects
  - Fund escrow accounts
  - Release milestones
  - Leave reviews

#### 8. Marcus Thompson - Pro in Trial
- **Email**: marcus.thompson@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Professional (Trial period)
- **Credits**: 300
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (Mobile App Developer)
- **Purpose**: Test trial subscription behavior
- **Use Cases**:
  - Trial period features
  - Conversion to paid subscription
  - Trial expiration handling

#### 9. Sophia Martinez - Pro Past Due
- **Email**: sophia.martinez@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Professional (Payment past due)
- **Credits**: 180
- **Status**: Active (with warnings)
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (Data Scientist)
- **Purpose**: Test subscription payment failures
- **Use Cases**:
  - Past due notices
  - Limited feature access
  - Payment retry flow

#### 10. Alex Kim - Pro with Promotion
- **Email**: alex.kim@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Professional (with 20% discount)
- **Credits**: 1200
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (Backend Engineer)
- **Purpose**: Test subscription promotions
- **Use Cases**:
  - Promotional pricing
  - Discount codes
  - Billing with promotions

---

### Business Tier Users (3)

#### 11. Jennifer Lee - Business Tier Team Lead
- **Email**: jennifer.lee@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Business
- **Credits**: 8500
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (Design Agency Owner)
- **Team Size**: 5 members
- **Purpose**: Business tier features, team management
- **Use Cases**:
  - Multi-user team accounts
  - Bulk project creation
  - Team billing
  - Advanced analytics

#### 12. Robert Chen (David) - Business Tier API User
- **Email**: robert.chen@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Business
- **Credits**: 12000
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **GUID**: **44444444-4444-4444-4444-444444444444** (Hard-coded for tests)
- **Profile**: Complete (CTO)
- **API Access**: Enabled
- **Purpose**: API integration testing (David persona)
- **Use Cases**:
  - API authentication
  - Programmatic project creation
  - Webhook subscriptions
  - API rate limiting

#### 13. Maria Santos - Business Tier Cancelled
- **Email**: maria.santos@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Business (Cancelled, grace period)
- **Credits**: 450
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (Marketing Director)
- **Purpose**: Test subscription cancellation flow
- **Use Cases**:
  - Cancelled subscription grace period
  - Data export before downgrade
  - Downgrade to free tier

---

### Enterprise Tier Users (2)

#### 14. Thomas Anderson - Enterprise Admin
- **Email**: thomas.anderson@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Enterprise
- **Credits**: 50000
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (VP of Engineering)
- **Organization**: Enterprise account with SSO
- **Purpose**: Enterprise features, high-value transactions
- **Use Cases**:
  - SSO/SAML login
  - Large escrow accounts (>10,000 credits)
  - Custom SLA
  - Dedicated support

#### 15. Patricia Williams (Eve) - Enterprise Compliance Officer
- **Email**: patricia.williams@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Enterprise
- **Credits**: 5000
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **GUID**: **55555555-5555-5555-5555-555555555555** (Hard-coded for tests)
- **Profile**: Complete (Compliance Manager)
- **Purpose**: Compliance and audit testing (Eve persona)
- **Use Cases**:
  - Tax compliance workflows
  - Audit log access
  - Compliance reporting
  - W-9/1099 generation

---

### Admin and Special Users (5)

#### 16. Carol Administrator - System Admin
- **Email**: admin@skillledger.app
- **Password**: Test123!
- **Role**: **Admin** (Full system access)
- **Tier**: Professional
- **Credits**: 1000
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **GUID**: **33333333-3333-3333-3333-333333333333** (Hard-coded for tests)
- **Profile**: Complete (System Administrator)
- **Purpose**: Admin functionality testing (Carol persona)
- **Use Cases**:
  - Admin dashboard access
  - User management (suspend, ban, verify)
  - Dispute resolution
  - System configuration
  - Audit log review

#### 17. Moderator User - Content Moderator
- **Email**: moderator@skillledger.app
- **Password**: Test123!
- **Role**: **Moderator** (Limited admin access)
- **Tier**: Professional
- **Credits**: 1000
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: Yes
- **Profile**: Complete (Content Moderator)
- **Purpose**: Moderator role testing
- **Use Cases**:
  - Content review and flagging
  - User reports handling
  - Limited administrative actions

#### 18. John Doe - Banned User
- **Email**: banned.user@testmail.com
- **Password**: Test123!
- **Role**: Client
- **Tier**: Free
- **Credits**: 0
- **Status**: **Banned** (Terms of Service violation)
- **Phone Verified**: No
- **Tax Compliant**: No
- **Profile**: None
- **Purpose**: Test banned account restrictions
- **Use Cases**:
  - Login attempts as banned user
  - All actions should be blocked
  - Appeal process

#### 19. Zero Balance - Edge Case User
- **Email**: zero.balance@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Free
- **Credits**: **0** (no starting credits used)
- **Status**: Active
- **Phone Verified**: Yes
- **Tax Compliant**: No
- **Profile**: Complete
- **Purpose**: Zero balance edge case testing
- **Use Cases**:
  - Actions with zero credits
  - Wallet funding from zero
  - Credit purchase flow

#### 20. High Risk - Fraud Watch User
- **Email**: high.risk@testmail.com
- **Password**: Test123!
- **Role**: Provider
- **Tier**: Free
- **Credits**: 150
- **Status**: Active (Flagged for fraud monitoring)
- **Phone Verified**: Yes
- **Tax Compliant**: No
- **Profile**: Complete
- **Purpose**: Fraud detection system testing
- **Use Cases**:
  - Fraud monitoring alerts
  - Transaction verification requirements
  - Enhanced KYC checks

---

## Test Project Scenarios (All 30)

### Draft Projects (5)

| ID | Title | Client | Status | Budget | Purpose |
|----|-------|--------|--------|--------|---------|
| 1 | Mobile App Redesign | Sarah Chen | Draft (Incomplete) | 500 | Incomplete draft, missing skills |
| 2 | SEO Optimization | Mike Johnson | Draft (Complete) | 800 | Ready to publish |
| 3 | Logo Design | Emily Rodriguez | Draft (With Errors) | 200 | Validation errors |
| 4 | Data Analysis | Rachel Goldstein | Draft | 1500 | Standard draft |
| 5 | API Integration | Jennifer Lee | Draft | 3000 | High-value draft |

### Published Projects (8)

| ID | Title | Client | Status | Budget | Applications | Purpose |
|----|-------|--------|--------|--------|--------------|---------|
| 6 | E-Commerce Website | Rachel Goldstein | Published (New) | 2000 | 0 | Just published, no applications yet |
| 7 | Marketing Campaign | Robert Chen | Published | 1500 | 12 | Multiple applications |
| 8 | Video Editing | Jennifer Lee | Published (Urgent) | 1000 | 5 | Urgent deadline |
| 9 | Mobile Game Dev | Thomas Anderson | Published (Featured) | 8000 | 25 | Featured project, many applicants |
| 10 | Content Writing | Rachel Goldstein | Published (Remote) | 600 | 8 | Remote-only project |
| 11 | Database Migration | Robert Chen | Published | 2500 | 3 | Technical project |
| 12 | Brand Strategy | Jennifer Lee | Published (Private) | 4000 | 2 | Private/invite-only |
| 13 | UI/UX Redesign | Thomas Anderson | Published | 3500 | 15 | Popular project |

### In-Progress Projects (8)

| ID | Title | Client | Provider | Status | Budget | Released | Purpose |
|----|-------|--------|----------|--------|--------|----------|---------|
| 14 | React Dashboard | Rachel Goldstein | David Kumar | InProgress (Fresh) | 2000 | 0 | Just started, no releases |
| 15 | API Development | Robert Chen | David Kumar | InProgress (Mid) | 3000 | 800 | Mid-progress, partial release |
| 16 | Mobile App | Jennifer Lee | Patricia Williams | InProgress (Near Done) | 4000 | 1200 | Near completion |
| 17 | Website Redesign | Thomas Anderson | David Kumar | InProgress (Overdue) | 5000 | 500 | Past deadline |
| 18 | Data Pipeline | Robert Chen | Patricia Williams | InProgress | 6000 | 2000 | On track |
| 19 | Marketing Site | Rachel Goldstein | Alex Kim | InProgress (High Value) | 10000 | 3000 | Large project |
| 20 | Mobile Feature | Jennifer Lee | David Kumar | InProgress | 2500 | 500 | Regular progress |
| 21 | Backend Service | Thomas Anderson | Patricia Williams | InProgress | 8000 | 2500 | Enterprise project |

### Completed Projects (5)

| ID | Title | Client | Provider | Status | Budget | Completion | Reviews | Purpose |
|----|-------|--------|----------|--------|--------|------------|---------|---------|
| 22 | Landing Page | Rachel Goldstein | David Kumar | Completed (Recent) | 1500 | 3 days ago | No reviews yet | Recent completion |
| 23 | Brand Identity | Robert Chen | David Kumar | Completed | 2000 | 30 days ago | ⭐ 9/10 & 10/10 | Excellent reviews |
| 24 | System Migration | Thomas Anderson | Patricia Williams | Completed (Bonus) | 15000 | 45 days ago | ⭐ 10/10 & 10/10 | Outstanding, with bonus |
| 25 | Content Strategy | Jennifer Lee | Marcus Thompson | Completed | 1200 | 60 days ago | ⭐ 8/10 & 9/10 | Good reviews |
| 26 | Website Updates | Rachel Goldstein | Alex Kim | Completed (Mixed) | 800 | 90 days ago | ⭐ 6/10 & 9/10 | Mixed reviews |

### Cancelled/Disputed Projects (4)

| ID | Title | Client | Provider | Status | Budget | Reason | Purpose |
|----|-------|--------|----------|--------|--------|--------|---------|
| 27 | Mobile Feature | Rachel Goldstein | Marcus Thompson | Cancelled (Client) | 1000 | Client changed requirements | Client-initiated cancellation |
| 28 | API Service | Robert Chen | Alex Kim | Cancelled (Provider) | 2500 | Provider unavailable | Provider-initiated cancellation |
| 29 | E-Learning Platform | Jennifer Lee | David Kumar | **Disputed** | 5000 | Quality concerns | Active dispute |
| 30 | Analytics Dashboard | Thomas Anderson | Patricia Williams | Cancelled (Mutual) | 3000 | Mutual agreement | Mutual cancellation |

---

## Financial Data Overview

### Credit Wallet Balances (Encrypted in DB)

| User | Current Balance | Pending | Total Earned | Total Spent | Wallet Status |
|------|-----------------|---------|--------------|-------------|---------------|
| Sarah Chen | 100 | 0 | 100 | 0 | Active |
| Mike Johnson | 85 | 0 | 100 | 15 | Active |
| Emily Rodriguez | 15 | 0 | 100 | 85 | Active (Low) |
| James Park | 45 | 0 | 100 | 55 | **Blocked** |
| Lisa Wong | 100 | 0 | 100 | 0 | Active |
| David Kumar (Bob) | 2500 | 0 | 5000 | 2500 | Active |
| Rachel Goldstein (Alice) | 5000 | 2000 | 10000 | 5000 | Active |
| Marcus Thompson | 300 | 0 | 800 | 500 | Active |
| Sophia Martinez | 180 | 0 | 500 | 320 | Active |
| Alex Kim | 1200 | 0 | 2000 | 800 | Active |
| Jennifer Lee | 8500 | 4000 | 15000 | 6500 | Active |
| Robert Chen (David) | 12000 | 3000 | 25000 | 13000 | Active |
| Maria Santos | 450 | 0 | 1000 | 550 | Active |
| Thomas Anderson | 50000 | 15000 | 100000 | 50000 | Active |
| Patricia Williams (Eve) | 5000 | 2500 | 12000 | 7000 | Active |
| Carol Admin | 1000 | 0 | 1000 | 0 | Active |
| Moderator | 1000 | 0 | 1000 | 0 | Active |
| Banned User | 0 | 0 | 0 | 0 | Banned |
| Zero Balance | 0 | 0 | 0 | 0 | Active |
| High Risk | 150 | 0 | 200 | 50 | Active (Flagged) |

### Escrow Accounts (Sample)

| Project ID | Client | Provider | Total | Released | Status |
|------------|--------|----------|-------|----------|--------|
| 14 | Rachel | David | 2000 | 0 | Active (Fresh) |
| 15 | Robert | David | 3000 | 800 | PartiallyReleased |
| 16 | Jennifer | Patricia | 4000 | 1200 | PartiallyReleased |
| 29 | Jennifer | David | 5000 | 600 | **Disputed** |
| 23 | Robert | David | 2000 | 2000 | Completed |
| 24 | Thomas | Patricia | 15000 | 15000 | Completed |

### Sample Transactions

| Type | From | To | Amount | Date | Status |
|------|------|-----|--------|------|--------|
| StartingCredit | System | All Users | 100 | Various | Completed |
| Purchase | External | Rachel | 5000 | 30 days ago | Completed |
| EscrowDeposit | Rachel | Escrow (Project 14) | 2000 | 7 days ago | Completed |
| EscrowRelease | Escrow (Project 15) | David | 800 | 5 days ago | Completed |
| PlatformFee | David | Platform | 40 | 5 days ago | Completed |
| BonusPayment | Thomas | Patricia | 500 | 45 days ago | Completed |
| P2PTransfer | Rachel | David | 500 | 15 days ago | Completed |

---

## Hard-Coded GUIDs for Tests

### TypeScript/JavaScript

```typescript
// Core Test Personas (Hard-coded GUIDs)
export const TEST_USERS = {
  ALICE_CLIENT: '11111111-1111-1111-1111-111111111111',    // Rachel Goldstein
  BOB_PROVIDER: '22222222-2222-2222-2222-222222222222',    // David Kumar
  CAROL_ADMIN: '33333333-3333-3333-3333-333333333333',     // Carol Admin
  DAVID_CLIENT: '44444444-4444-4444-4444-444444444444',    // Robert Chen
  EVE_PROVIDER: '55555555-5555-5555-5555-555555555555',    // Patricia Williams
};

// All Test Users (Sequential GUIDs)
export const ALL_TEST_USERS = {
  SARAH_CHEN: '10000000-0000-0000-0000-000000000001',
  MIKE_JOHNSON: '10000000-0000-0000-0000-000000000002',
  EMILY_RODRIGUEZ: '10000000-0000-0000-0000-000000000003',
  JAMES_PARK_SUSPENDED: '10000000-0000-0000-0000-000000000004',
  LISA_WONG_EMPTY: '10000000-0000-0000-0000-000000000005',
  // ... (6-20 omitted for brevity)
};

// Example Usage in Playwright
test('Alice can create project', async ({ page }) => {
  await loginAsUser(page, TEST_USERS.ALICE_CLIENT);
  // ... test continues
});
```

### C# (Backend Tests)

```csharp
// Core Test Personas
public static class TestUserIds
{
    public static readonly Guid AliceClient = new Guid("11111111-1111-1111-1111-111111111111");
    public static readonly Guid BobProvider = new Guid("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CarolAdmin = new Guid("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DavidClient = new Guid("44444444-4444-4444-4444-444444444444");
    public static readonly Guid EveProvider = new Guid("55555555-5555-5555-5555-555555555555");
}

// Example Usage
var user = await Context.Users.FindAsync(TestUserIds.AliceClient);
```

### SQL Queries

```sql
-- Alice (Rachel Goldstein)
SELECT * FROM Users WHERE Id = '11111111-1111-1111-1111-111111111111';

-- Bob (David Kumar)
SELECT * FROM Users WHERE Id = '22222222-2222-2222-2222-222222222222';

-- Carol (Admin)
SELECT * FROM Users WHERE Id = '33333333-3333-3333-3333-333333333333';
```

---

## Common Test Scenarios

### Scenario 1: Complete Project Flow (Alice → Bob)

```
1. Login as Alice (rachel.goldstein@testmail.com / Test123!)
2. Create project: "Build E-Commerce Website" (2000 credits)
3. Publish project (escrow funded)
4. Logout

5. Login as Bob (david.kumar@testmail.com / Test123!)
6. Browse projects, find Alice's project
7. Apply with cover letter and timeline
8. Logout

9. Login as Alice
10. Review Bob's application
11. Select Bob as provider
12. Project status → InProgress, Workspace created

13. Login as Bob
14. Complete milestone 1
15. Mark as complete, request release (500 credits)

16. Login as Alice
17. Review deliverable, approve milestone
18. Release 500 credits to Bob

19. Repeat steps 13-18 for remaining milestones
20. Final release, project status → Completed

21. Both users leave reviews
22. Reviews published after blind period
```

### Scenario 2: Dispute Resolution (Jennifer → David)

```
1. Login as Jennifer (jennifer.lee@testmail.com / Test123!)
2. Navigate to in-progress Project 29 (assigned to David)
3. Open workspace, review deliverables
4. Determine work does not meet requirements
5. Click "Open Dispute"
6. Describe issue: "Quality concerns - incomplete features"
7. Submit dispute
8. Escrow status → Disputed, funds frozen

9. Login as Carol Admin (admin@skillledger.app / Test123!)
10. Access Admin Dashboard → Disputes
11. Review Project 29 dispute details
12. Review evidence from both parties
13. Make decision: Partial release or full refund
14. Close dispute with resolution
```

### Scenario 3: Subscription Upgrade (Sarah → Professional)

```
1. Login as Sarah (sarah.chen@testmail.com / Test123!)
2. Attempt to create 4th project (free tier limit = 3)
3. Receive upgrade prompt
4. Navigate to Settings → Subscription
5. Select "Upgrade to Professional" ($29/month)
6. Enter payment details (Stripe test card)
7. Confirm upgrade
8. Subscription tier → Professional
9. Now can create unlimited projects
```

---

## Database Queries

### Verify Test Data Seeded

```sql
-- Count all test users
SELECT COUNT(*) FROM Users WHERE CreatedFromIP = 'TEST_DATA_SEEDER';
-- Expected: 20

-- Count all test projects
SELECT COUNT(*) FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER';
-- Expected: 30

-- Count all test transactions
SELECT COUNT(*) FROM CreditTransactions WHERE InitiatedFromIP = 'TEST_DATA_SEEDER';
-- Expected: 150+
```

### Get Alice's Projects

```sql
SELECT p.Id, p.Title, p.Status, p.CreditBudget, p.CreatedAt
FROM Projects p
WHERE p.ClientId = '11111111-1111-1111-1111-111111111111'
ORDER BY p.CreatedAt DESC;
```

### Get Bob's Completed Projects

```sql
SELECT p.Id, p.Title, p.Status, p.CreditBudget, p.CompletedAt
FROM Projects p
WHERE p.ProviderId = '22222222-2222-2222-2222-222222222222'
  AND p.Status = 4 -- ProjectStatus.Completed
ORDER BY p.CompletedAt DESC;
```

### Get Rachel's Wallet Balance (Encrypted)

```sql
SELECT
    w.Id,
    w.UserId,
    w.EncryptedBalance, -- Encrypted, need to decrypt
    w.LastTransactionAt,
    w.IsBlocked
FROM CreditWallets w
WHERE w.UserId = '11111111-1111-1111-1111-111111111111';
```

### Get Active Escrow Accounts

```sql
SELECT
    e.Id,
    e.ProjectId,
    p.Title AS ProjectTitle,
    u1.Email AS ClientEmail,
    u2.Email AS ProviderEmail,
    e.TotalAmount,
    e.ReleasedAmount,
    e.Status
FROM ProjectEscrow e
INNER JOIN Projects p ON e.ProjectId = p.Id
INNER JOIN Users u1 ON e.ClientId = u1.Id
INNER JOIN Users u2 ON e.ProviderId = u2.Id
WHERE e.Status IN (0, 2) -- Active or PartiallyReleased
ORDER BY e.CreatedAt DESC;
```

### Get All Transactions for a User

```sql
SELECT
    t.Id,
    t.Type,
    t.Amount,
    t.Status,
    t.Description,
    t.CreatedAt,
    u1.Email AS FromUser,
    u2.Email AS ToUser
FROM CreditTransactions t
LEFT JOIN Users u1 ON t.FromUserId = u1.Id
LEFT JOIN Users u2 ON t.ToUserId = u2.Id
WHERE t.FromUserId = '11111111-1111-1111-1111-111111111111'
   OR t.ToUserId = '11111111-1111-1111-1111-111111111111'
ORDER BY t.CreatedAt DESC;
```

---

## Clean Test Data

### Remove All Test Data

```bash
# Using the console app
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder -- --clean
```

### SQL Script to Clean Test Data

```sql
-- WARNING: This deletes ALL test data
-- Run only in development environment

BEGIN TRANSACTION;

-- Delete in reverse dependency order
DELETE FROM AuditLogs WHERE CreatedFromIP = 'TEST_DATA_SEEDER';
DELETE FROM ProjectReviews WHERE CreatedAt IN (SELECT CreatedAt FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM WorkspaceDocuments WHERE WorkspaceId IN (SELECT Id FROM ProjectWorkspaces WHERE CreatedAt IN (SELECT CreatedAt FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER'));
DELETE FROM WorkspaceMessages WHERE WorkspaceId IN (SELECT Id FROM ProjectWorkspaces WHERE CreatedAt IN (SELECT CreatedAt FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER'));
DELETE FROM ProjectWorkspaces WHERE CreatedAt IN (SELECT CreatedAt FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM CreditTransfers WHERE InitiatedFromIP = 'TEST_DATA_SEEDER';
DELETE FROM CreditTransactions WHERE InitiatedFromIP = 'TEST_DATA_SEEDER';
DELETE FROM ProjectEscrow WHERE CreatedFromIP = 'TEST_DATA_SEEDER';
DELETE FROM ProjectApplications WHERE CreatedAt IN (SELECT CreatedAt FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM ProjectDeliverables WHERE ProjectId IN (SELECT Id FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM ProjectSkills WHERE ProjectId IN (SELECT Id FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM Projects WHERE CreatedFromIP = 'TEST_DATA_SEEDER';
DELETE FROM CreditWallets WHERE UserId IN (SELECT Id FROM Users WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM Profiles WHERE UserId IN (SELECT Id FROM Users WHERE CreatedFromIP = 'TEST_DATA_SEEDER');
DELETE FROM Users WHERE CreatedFromIP = 'TEST_DATA_SEEDER';

COMMIT;
```

---

## Summary

This test data reference provides:

- ✅ **20 diverse user personas** covering all tiers and states
- ✅ **30 project scenarios** across all statuses
- ✅ **150+ financial transactions** with complete audit trail
- ✅ **Hard-coded GUIDs** for predictable test references
- ✅ **Common test scenarios** for E2E testing
- ✅ **SQL queries** for data verification

**Default Password**: `Test123!` for all users

**Quick Login URLs**:
- Login: http://localhost:3030/login
- Register: http://localhost:3030/register
- Dashboard: http://localhost:3030/dashboard

For complete E2E test scenarios, see `tests/E2E_TEST_PLAN.md`.

---

**Document Version**: 1.0
**Last Updated**: 2026-01-13
**Maintained By**: SkillLedger QA Team
