# SkillLedger - Start Both Backend and Frontend
# This script starts both the .NET API and Next.js frontend in separate windows

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " SkillLedger - Full Dev Environment" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check SQL Server status
Write-Host "Checking SQL Server status..." -ForegroundColor Yellow
try {
    $sqlService = Get-Service -Name "MSSQL`$SQLEXPRESS01" -ErrorAction Stop
    if ($sqlService.Status -eq "Running") {
        Write-Host "[OK] SQL Server SQLEXPRESS01 is running" -ForegroundColor Green
    } else {
        Write-Host "[!] SQL Server SQLEXPRESS01 is stopped. Starting..." -ForegroundColor Yellow
        Start-Service -Name "MSSQL`$SQLEXPRESS01"
        Write-Host "[OK] SQL Server started" -ForegroundColor Green
    }
} catch {
    Write-Host "[ERROR] SQL Server SQLEXPRESS01 not found. Please verify installation." -ForegroundColor Red
    Write-Host "Expected service name: MSSQL`$SQLEXPRESS01" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Configuration Summary:" -ForegroundColor Cyan
Write-Host "  - Database: localhost\SQLEXPRESS01 (Windows Auth)" -ForegroundColor White
Write-Host "  - Redis: Disabled (using in-memory cache)" -ForegroundColor White
Write-Host "  - Backend API: https://localhost:8031 / http://localhost:8030" -ForegroundColor White
Write-Host "  - Frontend: http://localhost:3030" -ForegroundColor White
Write-Host ""

# Check if frontend .env.local exists
$envLocalPath = ".\web\.env.local"
if (-not (Test-Path $envLocalPath)) {
    Write-Host "[!] Frontend .env.local not found. Creating from template..." -ForegroundColor Yellow
    if (Test-Path ".\web\.env.example") {
        Copy-Item ".\web\.env.example" $envLocalPath
        Write-Host "[OK] Created .env.local" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] .env.example not found. You may need to create .env.local manually" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Get the current directory
$rootDir = Get-Location

Write-Host "Starting services..." -ForegroundColor Yellow
Write-Host ""

# Start Backend API in new window
Write-Host "[1/2] Starting Backend API..." -ForegroundColor Cyan
$backendPath = Join-Path $rootDir "src\SkillLedger.Api"
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$backendPath'; Write-Host '[BACKEND API]' -ForegroundColor Cyan; Write-Host 'HTTP: http://localhost:8030' -ForegroundColor Green; Write-Host 'HTTPS: https://localhost:8031' -ForegroundColor Green; Write-Host 'Swagger: http://localhost:8030/swagger' -ForegroundColor Yellow; Write-Host ''; dotnet watch run"
)

# Wait a moment for backend to start
Start-Sleep -Seconds 2

# Start Frontend in new window
Write-Host "[2/2] Starting Frontend..." -ForegroundColor Cyan
$frontendPath = Join-Path $rootDir "web"
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$frontendPath'; Write-Host '[FRONTEND]' -ForegroundColor Cyan; Write-Host 'URL: http://localhost:3030' -ForegroundColor Green; Write-Host ''; yarn dev"
)

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host " Services Started Successfully!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Cyan
Write-Host "   Backend API:  http://localhost:8030" -ForegroundColor White
Write-Host "   Backend HTTPS: https://localhost:8031" -ForegroundColor White
Write-Host "   Frontend:     http://localhost:3030" -ForegroundColor White
Write-Host "   Swagger Docs: http://localhost:8030/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Notes:" -ForegroundColor Cyan
Write-Host "   - Two PowerShell windows have been opened" -ForegroundColor Gray
Write-Host "   - Backend API window (dotnet watch run)" -ForegroundColor Gray
Write-Host "   - Frontend window (yarn dev)" -ForegroundColor Gray
Write-Host "   - Close those windows or press Ctrl+C to stop services" -ForegroundColor Gray
Write-Host ""
Write-Host "To run tests:" -ForegroundColor Cyan
Write-Host "   E2E Tests:        cd web && yarn test:e2e" -ForegroundColor White
Write-Host "   Backend Tests:    dotnet test" -ForegroundColor White
Write-Host "   Frontend Tests:   cd web && yarn test" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit this launcher..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

