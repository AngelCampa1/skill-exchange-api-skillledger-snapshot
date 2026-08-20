# SkillLedger - E2E Test Data Seeding Script
# This script creates test users in the database for E2E tests

param(
    [string]$ConnectionString = "Server=localhost\SQLEXPRESS01;Database=SkillLedgerDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
    [switch]$Clean = $false
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " SkillLedger E2E Test Data Seeder" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Import required assemblies
Add-Type -AssemblyName "System.Data"

# Test users to create
$testUsers = @(
    @{
        Id = "10000000-0000-0000-0000-000000000001"
        Email = "test@skillledger.test"
        FirstName = "Test"
        LastName = "User"
        Password = "TestPassword123!"
        Role = "User"
    },
    @{
        Id = "10000000-0000-0000-0000-000000000002"
        Email = "client.user@skillledger.test"
        FirstName = "Client"
        LastName = "User"
        Password = "ClientPassword123!"
        Role = "User"
    },
    @{
        Id = "10000000-0000-0000-0000-000000000003"
        Email = "provider.user@skillledger.test"
        FirstName = "Provider"
        LastName = "User"
        Password = "ProviderPassword123!"
        Role = "User"
    }
)

try {
    # Clean existing test data if requested
    if ($Clean) {
        Write-Host "[CLEAN] Removing existing test users..." -ForegroundColor Yellow
        
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $testEmails = $testUsers | ForEach-Object { "'$($_.Email)'" }
        $emailList = $testEmails -join ","
        
        $cleanupSql = @"
-- Disable foreign key constraints temporarily
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'

-- Delete related data for test users
DELETE FROM UserSkills WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ($emailList))
DELETE FROM UserProfiles WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ($emailList))
DELETE FROM Projects WHERE ClientId IN (SELECT Id FROM Users WHERE Email IN ($emailList))
DELETE FROM Users WHERE Email IN ($emailList)

-- Re-enable foreign key constraints
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'
"@
        
        $command = $connection.CreateCommand()
        $command.CommandText = $cleanupSql
        $command.ExecuteNonQuery() | Out-Null
        
        $connection.Close()
        
        Write-Host "[CLEAN] Test users removed successfully!" -ForegroundColor Green
        Write-Host ""
    }
    
    # Create test users via API (so passwords are properly hashed)
    Write-Host "[SEED] Creating test users via backend API..." -ForegroundColor Cyan
    Write-Host ""
    
    $apiUrl = "http://localhost:8030"
    
    # Check if API is running
    try {
        $healthCheck = Invoke-WebRequest -Uri "$apiUrl/api/health" -Method GET -UseBasicParsing -ErrorAction SilentlyContinue
    } catch {
        Write-Host "[ERROR] Backend API is not running at $apiUrl" -ForegroundColor Red
        Write-Host "[INFO] Please start the backend first with: dotnet run --project src/SkillLedger.Api" -ForegroundColor Yellow
        exit 1
    }
    
    foreach ($user in $testUsers) {
        Write-Host "[SEED] Creating user: $($user.Email)..." -ForegroundColor Cyan
        
        $registerDto = @{
            email = $user.Email
            password = $user.Password
            confirmPassword = $user.Password
            firstName = $user.FirstName
            lastName = $user.LastName
            acceptedTerms = $true
        } | ConvertTo-Json
        
        try {
            # Register the user
            $response = Invoke-RestMethod -Uri "$apiUrl/api/auth/register" `
                -Method POST `
                -Body $registerDto `
                -ContentType "application/json" `
                -ErrorAction Stop
            
            if ($response.success) {
                Write-Host "  [OK] User registered: $($user.Email)" -ForegroundColor Green
                
                # Now we need to manually verify the email (bypass verification for E2E tests)
                # Connect to database and update user status
                $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
                $connection.Open()
                
                $verifySql = @"
UPDATE Users
SET Status = 2, -- EmailVerified status
    EmailVerified = 1,
    EmailVerifiedAt = GETUTCDATE()
WHERE Email = @Email
"@
                
                $command = $connection.CreateCommand()
                $command.CommandText = $verifySql
                $command.Parameters.AddWithValue("@Email", $user.Email) | Out-Null
                $rows = $command.ExecuteNonQuery()
                
                $connection.Close()
                
                if ($rows -gt 0) {
                    Write-Host "  [OK] Email verified for: $($user.Email)" -ForegroundColor Green
                } else {
                    Write-Host "  [WARN] Could not verify email for: $($user.Email)" -ForegroundColor Yellow
                }
            } else {
                Write-Host "  [ERROR] Failed to register: $($response.message)" -ForegroundColor Red
            }
        } catch {
            # Check if user already exists
            if ($_.Exception.Message -like "*already registered*" -or $_.ErrorDetails.Message -like "*already registered*") {
                Write-Host "  [SKIP] User already exists: $($user.Email)" -ForegroundColor Yellow
            } else {
                Write-Host "  [ERROR] $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        
        Write-Host ""
    }
    
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host " Test Data Seeding Complete!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test Users Created:" -ForegroundColor Cyan
    foreach ($user in $testUsers) {
        Write-Host "  - $($user.Email) / $($user.Password)" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "You can now run E2E tests with these credentials." -ForegroundColor Green
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "[ERROR] Failed to seed test data:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Yellow
    exit 1
}

# Instructions
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Usage Instructions" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To seed test data:" -ForegroundColor White
Write-Host "  .\scripts\seed-e2e-test-data.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "To clean and re-seed:" -ForegroundColor White
Write-Host "  .\scripts\seed-e2e-test-data.ps1 -Clean" -ForegroundColor Gray
Write-Host ""
Write-Host "To use a different database:" -ForegroundColor White
Write-Host '  .\scripts\seed-e2e-test-data.ps1 -ConnectionString "Server=...;Database=..." ' -ForegroundColor Gray
Write-Host ""

