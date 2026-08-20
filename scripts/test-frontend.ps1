#!/usr/bin/env pwsh
# Simple wrapper to run all frontend (Next.js) tests

param(
    [Parameter(ValueFromRemainingArguments)]
    $RemainingArgs
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& "$ScriptDir/run-tests.ps1" -Frontend @RemainingArgs