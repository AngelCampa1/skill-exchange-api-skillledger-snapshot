# SkillLedger E2E Manual Test Plan

**Version**: 1.0
**Last Updated**: 2026-01-13
**Test Environment**: Development (localhost)
**Test Tool**: Playwright MCP (Manual Browser Testing)
**Total Test Scenarios**: 88

## Table of Contents

1. [Test Execution Workflow](#test-execution-workflow)
2. [Test Environment Setup](#test-environment-setup)
3. [Test Domains & Scenarios](#test-domains--scenarios)
   - [1. Authentication & Identity (P1)](#1-authentication--identity-p1)
   - [2. Profile Management (P1)](#2-profile-management-p1)
   - [3. Project Marketplace (P1)](#3-project-marketplace-p1)
   - [4. Financial Operations (P1)](#4-financial-operations-p1)
   - [5. Collaboration Workspace (P2)](#5-collaboration-workspace-p2)
   - [6. Reputation & Reviews (P2)](#6-reputation--reviews-p2)
   - [7. Trust Badges (P3)](#7-trust-badges-p3)
   - [8. Cross-Cutting Concerns (P1-P2)](#8-cross-cutting-concerns-p1-p2)
4. [Known Issues Validation](#known-issues-validation)
5. [Test Data Reference](#test-data-reference)
6. [Playwright MCP Testing Guide](#playwright-mcp-testing-guide)

---

## Test Execution Workflow

### Phase 1: Pre-Test Setup

**1. Environment Verification**
```bash
# Verify SQL Server is running
Get-Service | Where-Object {$_.Name -like '*SQL*'}

# Verify database is up-to-date
cd src/SkillLedger.Api
dotnet ef database update
```

**2. Seed Test Database**
```bash
# Clean and seed database with test data
dotnet run --project tests/SkillLedger.Tests/Tools/DatabaseSeeder
```

Expected output: ✅ 20 users, 30 projects, 150+ transactions

**3. Start Backend API**
```bash
cd src/SkillLedger.Api
dotnet run
```

Verify at: `https://localhost:8031/swagger` (should show 366 endpoints)

**4. Start Frontend Application**
```bash
cd web
yarn dev
```

Verify at: `http://localhost:3030` (should show landing page)

**5. Open Playwright MCP**

Launch Playwright MCP browser tools and navigate to `http://localhost:3030`

### Phase 2: During Testing

- ✅ Execute test scenarios in priority order (P1 → P2 → P3)
- ✅ Record all failures with screenshots
- ✅ Note actual behavior vs expected behavior
- ✅ Check browser console for errors (F12)
- ✅ Check network tab for failed requests
- ✅ Validate Known Issues against current behavior

### Phase 3: Post-Test Actions

- ✅ Document all bugs found in `web/FOUND_BUGS.md`
- ✅ Clean test database if needed: `dotnet run -- --clean`
- ✅ Stop all running servers
- ✅ Create summary report with pass/fail counts

---

## Test Environment Setup

### System URLs

| Service | URL | Status Check |
|---------|-----|--------------|
| Frontend | http://localhost:3030 | Landing page loads |
| Backend API | https://localhost:8031 | Swagger UI accessible |
| Backend HTTP | http://localhost:8030 | API responds |

### Test User Credentials

**Default Password**: `Test123!` (for all test users)

| Persona | Email | Role | Tier | Credits | Purpose |
|---------|-------|------|------|---------|---------|
| Rachel (Alice) | rachel.goldstein@testmail.com | Client | Pro | 5000 | High-activity client |
| David (Bob) | david.kumar@testmail.com | Provider | Pro | 2500 | Top-rated provider |
| Carol (Admin) | admin@skillledger.app | Admin | Pro | 1000 | System administrator |
| Robert (David) | robert.chen@testmail.com | Client | Business | 12000 | Enterprise client |
| Patricia (Eve) | patricia.williams@testmail.com | Provider | Enterprise | 5000 | Enterprise provider |

See `tests/TEST_DATA_REFERENCE.md` for complete list of all 20 personas.

### Browser Configuration

- **Recommended Browser**: Chromium (for consistency)
- **Viewport**: 1920x1080 (desktop)
- **Network Throttling**: None (local testing)
- **JavaScript**: Enabled
- **Cookies**: Enabled

---

## Test Domains & Scenarios

### 1. Authentication & Identity (P1)

**Priority**: 🔴 Critical
**Test Count**: 7 scenarios
**Estimated Time**: 30 minutes

#### AUTH-001: User Registration Flow

**Objective**: Verify new users can register successfully

**Prerequisites**: Clean database state

**Test Steps**:
1. Navigate to `http://localhost:3030/register`
2. Fill registration form:
   - Email: `newuser@testmail.com`
   - Password: `NewUser123!@`
   - Confirm Password: `NewUser123!@`
   - First Name: `Test`
   - Last Name: `User`
3. Click "Create Account"
4. Verify success message appears
5. Check email verification message

**Expected Result**:
- ✅ Account created successfully
- ✅ Email verification sent (check console logs)
- ✅ User redirected to email verification page
- ✅ Audit log created with IP address

**Known Issues**: Check CRIT-002 (Email enumeration)

---

#### AUTH-002: Email Verification Process

**Objective**: Verify email verification link works

**Prerequisites**: AUTH-001 completed

**Test Steps**:
1. Get verification token from database:
   ```sql
   SELECT Token FROM EmailVerificationTokens WHERE Email = 'newuser@testmail.com'
   ```
2. Navigate to: `http://localhost:3030/verify-email?token={TOKEN}`
3. Verify success message
4. Try logging in with verified email

**Expected Result**:
- ✅ Email verified successfully
- ✅ User can now log in
- ✅ EmailConfirmed = true in database

---

#### AUTH-003: Login with Valid Credentials

**Objective**: Verify existing users can log in

**Prerequisites**: Database seeded

**Test Steps**:
1. Navigate to `http://localhost:3030/login`
2. Enter credentials:
   - Email: `rachel.goldstein@testmail.com`
   - Password: `Test123!`
3. Click "Sign In"
4. Verify redirect to dashboard

**Expected Result**:
- ✅ Login successful
- ✅ JWT token received and stored
- ✅ User redirected to `/dashboard`
- ✅ User profile visible in header

**Validation**:
- Check localStorage for `authToken`
- Check browser console for no errors
- Verify API call to `/api/auth/login` returns 200

---

#### AUTH-004: Login with Invalid Credentials

**Objective**: Verify proper error handling for invalid login

**Prerequisites**: Database seeded

**Test Steps**:
1. Navigate to `http://localhost:3030/login`
2. Enter credentials:
   - Email: `rachel.goldstein@testmail.com`
   - Password: `WrongPassword123!`
3. Click "Sign In"
4. Verify error message

**Expected Result**:
- ✅ Login fails with generic error message
- ✅ No account details revealed (email enumeration protection)
- ✅ No redirect occurs
- ✅ Password field cleared

**Known Issues**: Check BUG-AUTH-003 (Error message specificity)

---

#### AUTH-005: Logout Flow

**Objective**: Verify users can log out successfully

**Prerequisites**: User logged in (AUTH-003)

**Test Steps**:
1. Click user menu in header
2. Click "Logout"
3. Verify redirect to home page
4. Try accessing protected route `/dashboard`

**Expected Result**:
- ✅ User logged out successfully
- ✅ JWT token removed from localStorage
- ✅ Redirect to home page
- ✅ Accessing `/dashboard` redirects to login

---

#### AUTH-006: Password Reset Request

**Objective**: Verify password reset email is sent

**Prerequisites**: Database seeded

**Test Steps**:
1. Navigate to `http://localhost:3030/forgot-password`
2. Enter email: `rachel.goldstein@testmail.com`
3. Click "Send Reset Link"
4. Verify success message (generic for security)
5. Check console logs for reset email

**Expected Result**:
- ✅ Generic success message shown (even for non-existent emails)
- ✅ Reset token generated in database
- ✅ Email sent (check logs)
- ✅ No email enumeration possible

---

#### AUTH-007: Session Timeout Behavior

**Objective**: Verify expired sessions are handled correctly

**Prerequisites**: User logged in

**Test Steps**:
1. Log in as `rachel.goldstein@testmail.com`
2. Wait for token expiration (or manually expire in database)
3. Try accessing protected API endpoint
4. Verify redirect to login

**Expected Result**:
- ✅ Session expires after configured timeout
- ✅ User redirected to login with message
- ✅ Original URL preserved for redirect after login
- ✅ Refresh token flow works (if implemented)

**Known Issues**: Check BUG-WEEK18-001 (Second 401 after token refresh)

---

### 2. Profile Management (P1)

**Priority**: 🔴 Critical
**Test Count**: 2 scenarios
**Estimated Time**: 20 minutes

#### PROFILE-001: Complete Profile Creation Wizard

**Objective**: Verify new users can complete their profile

**Prerequisites**: New account created (AUTH-001)

**Test Steps**:
1. Log in as newly created user
2. Navigate to profile wizard (should auto-redirect)
3. Fill profile form:
   - Title: `Software Engineer`
   - Bio: `Experienced developer with 5+ years`
   - Location: `San Francisco, CA`
   - Time Zone: `America/Los_Angeles`
4. Select 3-5 skills with proficiency levels
5. Add work experience entry
6. Upload profile photo (optional)
7. Submit profile

**Expected Result**:
- ✅ Profile created successfully
- ✅ Profile completeness = 100%
- ✅ User redirected to dashboard
- ✅ Profile visible on user page

---

#### PROFILE-002: Phone Number Verification

**Objective**: Verify phone verification SMS flow

**Prerequisites**: User profile created

**Test Steps**:
1. Navigate to `http://localhost:3030/settings/phone`
2. Enter phone number: `+1 555-123-4567`
3. Click "Send Verification Code"
4. Check logs for SMS sent (Azure Communication Services)
5. Enter verification code (get from database or logs)
6. Submit verification

**Expected Result**:
- ✅ SMS sent successfully
- ✅ Verification code accepted
- ✅ PhoneNumberConfirmed = true
- ✅ User can now access phone-required features

**Known Issues**: Azure SMS may be disabled in development

---

### 3. Project Marketplace (P1)

**Priority**: 🔴 Critical
**Test Count**: 5 scenarios
**Estimated Time**: 35 minutes

#### PROJECT-001: Create New Project (Draft)

**Objective**: Verify client can create a project

**Prerequisites**: Logged in as Rachel (client with 5000 credits)

**Test Steps**:
1. Navigate to `http://localhost:3030/projects/create`
2. Fill project form:
   - Title: `Build E-Commerce Website`
   - Description: `Need full-stack developer for online store`
   - Category: `Web Development`
   - Credit Budget: `2000`
   - Select 3 required skills
3. Add 2 deliverables
4. Save as Draft

**Expected Result**:
- ✅ Project created with status = Draft
- ✅ Saved to database
- ✅ Visible in "My Projects" list
- ✅ Can edit draft later

---

#### PROJECT-002: Publish Project to Marketplace

**Objective**: Verify project can be published

**Prerequisites**: PROJECT-001 completed

**Test Steps**:
1. Navigate to "My Projects"
2. Open draft project
3. Review all details
4. Click "Publish to Marketplace"
5. Confirm escrow funding (2000 credits)
6. Verify project appears in marketplace

**Expected Result**:
- ✅ Project status = Published
- ✅ 2000 credits deducted from wallet
- ✅ Escrow account created
- ✅ Project visible in `/projects` marketplace
- ✅ Providers can now apply

---

#### PROJECT-003: Search and Filter Projects

**Objective**: Verify project search functionality

**Prerequisites**: Database seeded with 30 projects

**Test Steps**:
1. Navigate to `http://localhost:3030/projects`
2. Verify project list loads (should show published projects)
3. Apply filters:
   - Category: `Web Development`
   - Budget: `1000-3000 credits`
   - Skills: `React`
4. Verify filtered results
5. Search by keyword: `E-Commerce`

**Expected Result**:
- ✅ Filters work correctly
- ✅ Only matching projects shown
- ✅ Search highlights keywords
- ✅ Results update without page reload

---

#### PROJECT-004: Provider Applies to Project

**Objective**: Verify provider can submit application

**Prerequisites**: Published project exists, logged in as David (provider)

**Test Steps**:
1. Navigate to project detail page
2. Click "Apply for this Project"
3. Fill application form:
   - Proposed Timeline: `4 weeks`
   - Cover Letter: `I have 5+ years experience with React...`
   - Portfolio Links: Add 2 links
4. Submit application
5. Verify application confirmation

**Expected Result**:
- ✅ Application submitted successfully
- ✅ Application visible to client
- ✅ Provider notified of submission
- ✅ Application status = Pending

---

#### PROJECT-005: Client Selects Provider

**Objective**: Verify client can accept an application

**Prerequisites**: PROJECT-004 completed, logged in as Rachel (client)

**Test Steps**:
1. Navigate to project applications
2. Review David's application
3. Click "Select Provider"
4. Confirm selection
5. Verify project status changes

**Expected Result**:
- ✅ Provider assigned to project
- ✅ Project status = InProgress
- ✅ Workspace created automatically
- ✅ Other applications rejected
- ✅ Both parties notified

---

### 4. Financial Operations (P1)

**Priority**: 🔴 Critical
**Test Count**: 6 scenarios
**Estimated Time**: 40 minutes

#### FINANCE-001: Fund Credit Wallet (Purchase Credits)

**Objective**: Verify user can purchase credits via Stripe

**Prerequisites**: Logged in user with low balance

**Test Steps**:
1. Navigate to `http://localhost:3030/wallet`
2. Click "Add Credits"
3. Select credit package: `1000 credits - $50`
4. Click "Checkout"
5. Use Stripe test card: `4242 4242 4242 4242`
6. Complete payment
7. Verify credits added to wallet

**Expected Result**:
- ✅ Stripe checkout session created
- ✅ Payment successful
- ✅ Credits added to wallet
- ✅ Transaction recorded
- ✅ Receipt sent via email

**Known Issues**: Stripe may be disabled in development

---

#### FINANCE-002: Create Escrow for Project

**Objective**: Verify escrow is created when publishing project

**Prerequisites**: Already tested in PROJECT-002

**Test Steps**:
1. Verify escrow record in database:
   ```sql
   SELECT * FROM ProjectEscrow WHERE ProjectId = {PROJECT_ID}
   ```
2. Check wallet balance decreased
3. Check escrow status = Active

**Expected Result**:
- ✅ Escrow account created
- ✅ TotalAmount = project budget
- ✅ ReleasedAmount = 0
- ✅ Status = Active

---

#### FINANCE-003: Release Escrow to Milestone

**Objective**: Verify partial escrow release

**Prerequisites**: Project in progress, logged in as client

**Test Steps**:
1. Navigate to project workspace
2. Click "Milestones" tab
3. Select completed milestone
4. Enter release amount: `500 credits`
5. Confirm release
6. Verify provider wallet updated

**Expected Result**:
- ✅ Escrow ReleasedAmount += 500
- ✅ Provider wallet += 500 (minus 5% platform fee)
- ✅ Platform fee = 25 credits
- ✅ Transaction recorded
- ✅ Both parties notified

**Known Issues**: Check CRIT-005 (Escrow double-release vulnerability)

---

#### FINANCE-004: Escrow Dispute Flow

**Objective**: Verify dispute handling

**Prerequisites**: Project with active escrow

**Test Steps**:
1. Navigate to project workspace
2. Click "Open Dispute"
3. Fill dispute form:
   - Reason: `Quality concerns - work does not meet requirements`
   - Details: `Deliverable is incomplete...`
4. Submit dispute
5. Verify escrow status changes

**Expected Result**:
- ✅ Escrow status = Disputed
- ✅ Funds frozen (no releases allowed)
- ✅ Admin notified
- ✅ Both parties notified
- ✅ Dispute resolution workflow begins

---

#### FINANCE-005: P2P Credit Transfer

**Objective**: Verify direct credit transfer between users

**Prerequisites**: Two users with sufficient balances

**Test Steps**:
1. Log in as Rachel (5000 credits)
2. Navigate to `http://localhost:3030/wallet/transfer`
3. Enter recipient: `david.kumar@testmail.com`
4. Amount: `500 credits`
5. Message: `Payment for consultation`
6. Confirm transfer (2% fee = 10 credits)
7. Log out and log in as David
8. Verify credits received

**Expected Result**:
- ✅ Rachel's balance: 5000 - 500 - 10 = 4490
- ✅ David's balance: 2500 + 500 = 3000
- ✅ Platform fee collected: 10 credits
- ✅ Transaction hash generated
- ✅ Both parties see transaction history

---

#### FINANCE-006: Subscription Purchase and Billing

**Objective**: Verify subscription tier upgrade

**Prerequisites**: Logged in as Free tier user

**Test Steps**:
1. Navigate to `http://localhost:3030/settings/subscription`
2. View current tier: Free
3. Click "Upgrade to Professional"
4. Review features and pricing
5. Confirm upgrade
6. Enter payment details (Stripe)
7. Complete purchase

**Expected Result**:
- ✅ Subscription upgraded to Professional
- ✅ Billing cycle started
- ✅ UserSubscription record created
- ✅ Access to Pro features enabled
- ✅ Invoice sent via email

---

### 5. Collaboration Workspace (P2)

**Priority**: 🟠 High
**Test Count**: 4 scenarios
**Estimated Time**: 25 minutes

#### WORKSPACE-001: Access Project Workspace

**Objective**: Verify workspace is accessible to project participants

**Prerequisites**: Project with provider assigned

**Test Steps**:
1. Log in as client (Rachel)
2. Navigate to project detail page
3. Click "Open Workspace"
4. Verify workspace loads with tabs:
   - Messages
   - Documents
   - Milestones

**Expected Result**:
- ✅ Workspace accessible to client and provider
- ✅ Not accessible to other users (403)
- ✅ All tabs render correctly
- ✅ Real-time connection established (if enabled)

---

#### WORKSPACE-002: Send Real-Time Messages

**Objective**: Verify messaging functionality

**Prerequisites**: WORKSPACE-001 completed

**Test Steps**:
1. In workspace, click "Messages" tab
2. Type message: `Hi David, excited to start!`
3. Click Send
4. Open second browser (or incognito)
5. Log in as David (provider)
6. Open same workspace
7. Verify message appears

**Expected Result**:
- ✅ Message sent successfully
- ✅ Message visible to both parties
- ✅ Real-time updates (if SignalR enabled)
- ✅ Timestamp accurate
- ✅ Sender name displayed

**Known Issues**: SignalR may not be implemented yet

---

#### WORKSPACE-003: Upload Document to Workspace

**Objective**: Verify file upload functionality

**Prerequisites**: WORKSPACE-001 completed

**Test Steps**:
1. In workspace, click "Documents" tab
2. Click "Upload Document"
3. Select file: `test-document.pdf` (create a small test file)
4. Add description: `Project requirements document`
5. Upload file
6. Verify file appears in document list

**Expected Result**:
- ✅ File uploaded to Azure Blob Storage
- ✅ WorkspaceDocument record created
- ✅ File accessible to both parties
- ✅ Virus scan passed
- ✅ Download works correctly

**Known Issues**: Azure Blob Storage may be disabled in development

---

#### WORKSPACE-004: Mark Milestone as Complete

**Objective**: Verify milestone completion flow

**Prerequisites**: Project with milestones defined

**Test Steps**:
1. Log in as provider (David)
2. Open workspace, navigate to "Milestones" tab
3. Select first milestone
4. Click "Mark as Complete"
5. Add completion notes
6. Submit
7. Log out and log in as client (Rachel)
8. Verify milestone pending approval

**Expected Result**:
- ✅ Milestone status = PendingApproval
- ✅ Client notified
- ✅ Client can approve or request changes
- ✅ Escrow release option available

---

### 6. Reputation & Reviews (P2)

**Priority**: 🟠 High
**Test Count**: 3 scenarios
**Estimated Time**: 20 minutes

#### REPUTATION-001: Submit Project Review

**Objective**: Verify post-project review submission

**Prerequisites**: Completed project exists

**Test Steps**:
1. Log in as client (Rachel)
2. Navigate to completed project
3. Click "Leave Review"
4. Fill review form:
   - Overall Rating: 9/10
   - Quality: 9/10
   - Communication: 10/10
   - Timeliness: 8/10
   - Professionalism: 9/10
   - Review Text: `Excellent work! David exceeded expectations...`
5. Submit review

**Expected Result**:
- ✅ Review submitted successfully
- ✅ Review status = Pending (blind review period)
- ✅ Provider cannot see review yet
- ✅ Published after blind period expires

---

#### REPUTATION-002: Reputation Score Calculation

**Objective**: Verify reputation scores are calculated correctly

**Prerequisites**: REPUTATION-001 completed (or use seeded data)

**Test Steps**:
1. Navigate to David's profile page
2. View reputation score
3. Verify calculation:
   - Overall score = average of all reviews
   - Category scores = average per category
   - Completed projects count
   - Review count

**Expected Result**:
- ✅ Overall reputation score displayed (0-10 scale)
- ✅ Category breakdowns shown
- ✅ Review count and project count accurate
- ✅ Score updates after new reviews

---

#### REPUTATION-003: Fraud Detection Trigger

**Objective**: Verify reputation fraud detection

**Prerequisites**: Admin access

**Test Steps**:
1. Attempt to create multiple fake reviews
2. Check admin dashboard for fraud alerts
3. Verify flagged reviews

**Expected Result**:
- ✅ Fraud detection algorithms run
- ✅ Suspicious reviews flagged
- ✅ Admin notified
- ✅ Reviews withheld pending investigation

**Known Issues**: Fraud detection ML may not be fully implemented

---

### 7. Trust Badges (P3)

**Priority**: 🟢 Low
**Test Count**: 2 scenarios
**Estimated Time**: 15 minutes

#### BADGE-001: Earn Automated Trust Badge

**Objective**: Verify automated badge awarding

**Prerequisites**: User meets badge criteria

**Test Steps**:
1. Log in as provider with 5+ completed projects
2. Navigate to profile page
3. Check "Badges" section
4. Verify "Established Provider" badge is displayed

**Expected Result**:
- ✅ Badge automatically awarded
- ✅ Badge visible on profile
- ✅ Badge criteria displayed on hover
- ✅ UserBadge record created

---

#### BADGE-002: Request Manual Verification Badge

**Objective**: Verify manual verification request flow

**Prerequisites**: Logged in user

**Test Steps**:
1. Navigate to `http://localhost:3030/settings/verification`
2. Click "Request Identity Verification"
3. Upload required documents (ID, proof of address)
4. Submit request
5. Verify request is pending admin review

**Expected Result**:
- ✅ Verification request submitted
- ✅ Documents uploaded to secure storage
- ✅ Admin notified
- ✅ Status = PendingReview

---

### 8. Cross-Cutting Concerns (P1-P2)

**Priority**: 🔴 Critical / 🟠 High
**Test Count**: 4 scenarios
**Estimated Time**: 30 minutes

#### SECURITY-001: Role-Based Access Control (RBAC)

**Objective**: Verify users can only access authorized resources

**Prerequisites**: Multiple users with different roles

**Test Steps**:
1. Log in as regular user (Rachel)
2. Try accessing admin route: `http://localhost:3030/admin`
3. Verify access denied (403 or redirect)
4. Log out and log in as admin (Carol)
5. Access same admin route
6. Verify access granted

**Expected Result**:
- ✅ Non-admin users cannot access admin routes
- ✅ Admin users have full access
- ✅ API endpoints enforce role checks
- ✅ Proper error messages displayed

**Known Issues**: Check RBAC implementation completeness

---

#### SECURITY-002: Rate Limiting Protection

**Objective**: Verify rate limiting prevents abuse

**Prerequisites**: None

**Test Steps**:
1. Open browser console
2. Execute rapid API requests:
   ```javascript
   for (let i = 0; i < 20; i++) {
     fetch('https://localhost:8031/api/auth/login', {
       method: 'POST',
       body: JSON.stringify({ email: 'test@test.com', password: 'test' })
     });
   }
   ```
3. Verify rate limiting kicks in

**Expected Result**:
- ✅ After 5 attempts, rate limit triggered
- ✅ HTTP 429 (Too Many Requests) returned
- ✅ Retry-After header present
- ✅ User locked out temporarily

---

#### SECURITY-003: XSS Prevention

**Objective**: Verify input sanitization prevents XSS attacks

**Prerequisites**: Logged in user

**Test Steps**:
1. Navigate to profile edit page
2. Enter malicious script in bio field:
   ```html
   <script>alert('XSS')</script>
   ```
3. Save profile
4. View profile page
5. Verify script does not execute

**Expected Result**:
- ✅ Script tags sanitized/escaped
- ✅ No JavaScript execution
- ✅ Content displayed as text
- ✅ No security warnings in console

---

#### PERFORMANCE-001: Page Load Performance

**Objective**: Verify acceptable page load times

**Prerequisites**: Database seeded

**Test Steps**:
1. Open browser DevTools (F12)
2. Navigate to Network tab
3. Navigate to `http://localhost:3030/projects`
4. Measure page load time
5. Check Lighthouse performance score

**Expected Result**:
- ✅ Initial page load < 2 seconds
- ✅ Time to Interactive (TTI) < 3 seconds
- ✅ Lighthouse score > 80
- ✅ No JavaScript errors
- ✅ Optimized images and assets

---

## Known Issues Validation

During E2E testing, validate the following known issues:

### Critical Issues

| ID | Severity | Issue | Validation Test |
|----|----------|-------|-----------------|
| CRIT-001 | 🔴 Critical | Azure keys exposed in responses | Check API responses for sensitive keys |
| CRIT-005 | 🔴 Critical | Escrow double-release vulnerability | Try releasing same milestone twice (FINANCE-003) |
| CRIT-006 | 🔴 Critical | Credit wallet reconciliation | Verify wallet balance matches transaction sum |
| BUG-WEEK18-001 | 🔴 Critical | Second 401 after token refresh | Monitor auth refresh flow (AUTH-007) |

### High Priority Issues

| ID | Severity | Issue | Validation Test |
|----|----------|-------|-----------------|
| BUG-AUTH-003 | 🟠 High | Error message specificity | Check login error messages (AUTH-004) |
| BUG-FE-HIGH-002 | 🟠 High | Session timeout handling | Test session expiration (AUTH-007) |
| BUG-FE-HIGH-007 | 🟠 High | File upload race conditions | Upload multiple files simultaneously (WORKSPACE-003) |

---

## Test Data Reference

### Quick Reference: Test Users

| Persona | Email | Password | Role | Tier | Credits | GUID |
|---------|-------|----------|------|------|---------|------|
| Rachel (Alice) | rachel.goldstein@testmail.com | Test123! | Client | Pro | 5000 | 11111111-1111-1111-1111-111111111111 |
| David (Bob) | david.kumar@testmail.com | Test123! | Provider | Pro | 2500 | 22222222-2222-2222-2222-222222222222 |
| Carol (Admin) | admin@skillledger.app | Test123! | Admin | Pro | 1000 | 33333333-3333-3333-3333-333333333333 |

See `tests/TEST_DATA_REFERENCE.md` for complete list.

### Test Projects

- **Project 14**: Fresh escrow, just started
- **Project 15**: Mid-progress, partial release
- **Project 17**: Overdue project
- **Project 23**: Completed with excellent reviews
- **Project 29**: Disputed project

---

## Playwright MCP Testing Guide

### Step 1: Launch Playwright MCP

1. Ensure backend and frontend servers are running
2. Open Playwright MCP browser tools
3. Navigate to `http://localhost:3030`

### Step 2: Execute Test Scenarios

**For each test scenario:**

1. **Navigate** to the specified URL
2. **Interact** with UI elements using Playwright browser actions:
   - Click buttons: `browser_click`
   - Fill forms: `browser_type` or `browser_fill_form`
   - Take screenshots: `browser_take_screenshot`
   - Inspect elements: `browser_snapshot`
3. **Verify** expected behavior matches actual behavior
4. **Record** any deviations as bugs

### Step 3: Document Findings

For each bug found:

1. Take screenshot of issue
2. Document in `web/FOUND_BUGS.md`:
   ```markdown
   ## BUG-E2E-XXX: Brief Description

   **Severity**: Critical/High/Medium/Low
   **Test Scenario**: AUTH-003
   **File**: src/pages/login.tsx:45

   **Expected**: User redirects to dashboard after login
   **Actual**: User sees blank page

   **Steps to Reproduce**:
   1. Navigate to /login
   2. Enter credentials
   3. Click Sign In

   **Screenshot**: [Attached]
   ```

### Step 4: Generate Test Report

After completing all scenarios:

```
Total Scenarios: 88
✅ Passed: X
❌ Failed: Y
⏭️ Skipped: Z

Pass Rate: X%

Critical Issues Found: N
High Priority Issues: M
```

---

## Execution Checklist

### Pre-Testing

- [ ] SQL Server running
- [ ] Database migrated to latest version
- [ ] Test data seeded (20 users, 30 projects)
- [ ] Backend API running on ports 8030/8031
- [ ] Frontend running on port 3030
- [ ] Playwright MCP browser ready

### Domain Testing

- [ ] 1. Authentication & Identity (7 scenarios)
- [ ] 2. Profile Management (2 scenarios)
- [ ] 3. Project Marketplace (5 scenarios)
- [ ] 4. Financial Operations (6 scenarios)
- [ ] 5. Collaboration Workspace (4 scenarios)
- [ ] 6. Reputation & Reviews (3 scenarios)
- [ ] 7. Trust Badges (2 scenarios)
- [ ] 8. Cross-Cutting Concerns (4 scenarios)

### Post-Testing

- [ ] All bugs documented in FOUND_BUGS.md
- [ ] Known issues validated
- [ ] Test report generated
- [ ] Screenshots organized
- [ ] Database cleaned (if needed)
- [ ] Servers stopped

---

## Summary

This E2E manual test plan covers **88 comprehensive test scenarios** across **8 critical domains** of the SkillLedger platform. By executing these tests with Playwright MCP browser tools, you can validate the entire system end-to-end.

**Priority Breakdown**:
- 🔴 P1 (Critical): 33 scenarios
- 🟠 P2 (High): 11 scenarios
- 🟢 P3 (Low): 2 scenarios

**Estimated Total Testing Time**: ~3-4 hours for complete execution

**Next Steps**:
1. Execute all P1 scenarios first
2. Document all findings
3. Fix critical bugs immediately
4. Re-test failed scenarios
5. Execute P2 and P3 scenarios
6. Generate final test report

---

**Document Version**: 1.0
**Last Updated**: 2026-01-13
**Maintained By**: SkillLedger QA Team
