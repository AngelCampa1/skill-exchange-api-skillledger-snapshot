@echo off
cd web
call npx playwright test --project=chromium --reporter=list > ..\test-output.txt 2>&1
echo Test completed! Check test-output.txt for results
type ..\test-output.txt





