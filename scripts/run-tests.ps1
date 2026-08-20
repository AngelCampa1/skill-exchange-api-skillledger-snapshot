#!/usr/bin/env pwsh
# SkillLedger Test Execution Script
# Supports selective test execution with parallel optimization

param(
    [Parameter(HelpMessage="Test category to run (Fast, Unit, Integration, Security, Performance, EndToEnd, BDD, All)")]
    [ValidateSet("Fast", "Unit", "Integration", "Security", "Performance", "EndToEnd", "BDD", "Financial", "Core", "Messaging", "Document", "All", "Backend", "Frontend")]
    [string]$Category = "All",
    
    [Parameter(HelpMessage="Run all backend (.NET) tests")]
    [switch]$Backend,
    
    [Parameter(HelpMessage="Run all frontend (Next.js) tests")]
    [switch]$Frontend,
    
    [Parameter(HelpMessage="Enable code coverage collection")]
    [switch]$Coverage,
    
    [Parameter(HelpMessage="Verbose test output")]
    [switch]$Verbose,
    
    [Parameter(HelpMessage="Run in watch mode")]
    [switch]$Watch,
    
    [Parameter(HelpMessage="Number of parallel threads (default: 4)")]
    [int]$Threads = 4
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Handle Backend and Frontend switches
if ($Backend) { $Category = "Backend" }
if ($Frontend) { $Category = "Frontend" }

# Set working directory to project root
$ProjectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $ProjectRoot

Write-Host "🧪 SkillLedger Test Runner" -ForegroundColor Cyan
Write-Host "📂 Project: $ProjectRoot" -ForegroundColor Gray
Write-Host "🔧 Category: $Category" -ForegroundColor Yellow

# Handle Backend and Frontend separately
if ($Category -eq "Backend") {
    Write-Host "🔧 Running Backend (.NET) Tests" -ForegroundColor Cyan
    $Filter = $null
    
    # Build dotnet test command for backend
    $TestArgs = @(
        "test"
        "tests/SkillLedger.Tests/"
        "--nologo"
        "--configuration", "Debug"
        "--settings", "tests/runsettings.xml"
    )
    $TestType = "Backend"
    
} elseif ($Category -eq "Frontend") {
    Write-Host "🔧 Running Frontend (Next.js) Tests" -ForegroundColor Cyan
    Set-Location "$ProjectRoot/web"
    
    # Check for yarn or npm
    $TestCommand = if (Get-Command yarn -ErrorAction SilentlyContinue) { "yarn" } else { "npm" }
    
    if ($Coverage) {
        $TestScript = "test:coverage"
    } elseif ($Watch) {
        $TestScript = "test:watch"
    } else {
        $TestScript = "test"
    }
    
    Write-Host "🎯 Using: $TestCommand $TestScript" -ForegroundColor Green
    $TestType = "Frontend"
    
} else {
    # Build test filter based on category for .NET tests
    $Filter = switch ($Category) {
        "Fast"        { "FullyQualifiedName~FastTest" }
        "Unit"        { "FullyQualifiedName~Unit" }
        "Integration" { "FullyQualifiedName~Integration" }
        "Security"    { "FullyQualifiedName~Security" }
        "Performance" { "FullyQualifiedName~Performance" }
        "EndToEnd"    { "FullyQualifiedName~EndToEnd" }
        "BDD"         { "FullyQualifiedName~BDD" }
        "Financial"   { "FullyQualifiedName~Financial" }
        "Core"        { "FullyQualifiedName~Core" }
        "Messaging"   { "FullyQualifiedName~Messaging" }
        "Document"    { "FullyQualifiedName~Document" }
        "All"         { $null }
        default       { "FullyQualifiedName~$Category" }
    }

    # Build dotnet test command
    $TestArgs = @(
        "test"
        "tests/SkillLedger.Tests/"
        "--nologo"
        "--configuration", "Debug"
        "--settings", "tests/runsettings.xml"
    )
    $TestType = "Backend"
}

if ($Filter) {
    $TestArgs += "--filter", $Filter
    Write-Host "🎯 Filter: $Filter" -ForegroundColor Green
}

if ($Coverage) {
    $TestArgs += "--collect:XPlat Code Coverage"
    Write-Host "📊 Coverage: Enabled" -ForegroundColor Green
} else {
    Write-Host "📊 Coverage: Disabled (use -Coverage to enable)" -ForegroundColor Gray
}

if ($Verbose) {
    $TestArgs += "--verbosity", "detailed"
    Write-Host "📝 Verbosity: Detailed" -ForegroundColor Green
} else {
    $TestArgs += "--verbosity", "minimal"
}

if ($Watch) {
    $TestArgs += "--watch"
    Write-Host "👀 Watch Mode: Enabled" -ForegroundColor Green
}

# Set parallel execution environment
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:MSBUILDDISABLENODEREUSE = "1"

Write-Host "🚀 Starting tests..." -ForegroundColor Cyan
Write-Host "⚡ Parallel Threads: $Threads" -ForegroundColor Yellow

try {
    $StartTime = Get-Date
    
    # Execute tests based on type
    if ($TestType -eq "Frontend") {
        # Execute frontend tests
        if ($TestCommand -eq "yarn") {
            $Process = Start-Process -FilePath "yarn" -ArgumentList $TestScript -NoNewWindow -PassThru -Wait
        } else {
            $Process = Start-Process -FilePath "npm" -ArgumentList "run", $TestScript -NoNewWindow -PassThru -Wait
        }
        $TestTypeName = "Frontend"
    } else {
        # Execute backend tests
        $Process = Start-Process -FilePath "dotnet" -ArgumentList $TestArgs -NoNewWindow -PassThru -Wait
        $TestTypeName = "Backend"
    }
    
    $EndTime = Get-Date
    $Duration = $EndTime - $StartTime
    
    if ($Process.ExitCode -eq 0) {
        Write-Host "✅ $TestTypeName tests completed successfully!" -ForegroundColor Green
        Write-Host "⏱️  Duration: $($Duration.TotalSeconds.ToString('F1'))s" -ForegroundColor Green
    } else {
        Write-Host "❌ $TestTypeName tests failed with exit code: $($Process.ExitCode)" -ForegroundColor Red
        exit $Process.ExitCode
    }
} catch {
    Write-Host "❌ Error running tests: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "" -ForegroundColor Gray
Write-Host "💡 Available test categories:" -ForegroundColor Cyan
Write-Host "   - Fast: Quick unit tests (< 100ms)" -ForegroundColor Gray
Write-Host "   - Unit: All unit tests" -ForegroundColor Gray
Write-Host "   - Integration: Database and API tests" -ForegroundColor Gray
Write-Host "   - Security: Security-focused tests" -ForegroundColor Gray
Write-Host "   - Performance: Performance benchmarks" -ForegroundColor Gray
Write-Host "   - EndToEnd: Full workflow tests" -ForegroundColor Gray
Write-Host "   - BDD: Behavior-driven development tests" -ForegroundColor Gray
Write-Host "   - Financial: Financial domain tests" -ForegroundColor Gray
Write-Host "   - Core: Core business logic tests" -ForegroundColor Gray
Write-Host "   - Messaging: Real-time messaging tests" -ForegroundColor Gray
Write-Host "   - Document: Document management tests" -ForegroundColor Gray