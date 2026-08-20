# SkillLedger - Windows Development Startup Script
# This script starts the backend API and provides instructions for starting the frontend

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " SkillLedger - Windows Development" -ForegroundColor Cyan
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

Write-Host "Starting Backend API..." -ForegroundColor Yellow
Write-Host "  Location: src\SkillLedger.Api" -ForegroundColor Gray
Write-Host "  HTTP: http://localhost:8030" -ForegroundColor Gray
Write-Host "  HTTPS: https://localhost:8031" -ForegroundColor Gray
Write-Host "  Swagger: https://localhost:8030/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop the API" -ForegroundColor Gray
Write-Host ""
Write-Host "To start the frontend (in a new terminal):" -ForegroundColor Cyan
Write-Host "  cd web" -ForegroundColor Yellow
Write-Host "  npm install    # (if not already installed)" -ForegroundColor Yellow
Write-Host "  npm run dev" -ForegroundColor Yellow
Write-Host ""
Write-Host "Starting in 3 seconds..." -ForegroundColor Gray
Start-Sleep -Seconds 3

# Start the API
Set-Location "src\SkillLedger.Api"
dotnet watch run

