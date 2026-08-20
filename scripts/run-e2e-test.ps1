# Run E2E Tests for SkillLedger
Write-Host "🚀 Running E2E Tests..." -ForegroundColor Cyan

Set-Location web

# Run Playwright tests
npx playwright test --project=chromium --reporter=list

Write-Host "`n✅ Test run complete!" -ForegroundColor Green
Write-Host "Check playwright-report/index.html for detailed results" -ForegroundColor Yellow


