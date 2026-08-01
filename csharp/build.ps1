<#
Builds and tests the SermonCleanup solution.
Warnings are treated as errors (see Directory.Build.props), so this also
acts as a lint check.

Usage:
    .\build.ps1
    .\build.ps1 -Configuration Release
#>
param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "SermonCleanup.sln"

Write-Host "=== Restoring ===" -ForegroundColor Cyan
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n=== Building ($Configuration) ===" -ForegroundColor Cyan
dotnet build $solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n=== Testing ===" -ForegroundColor Cyan
dotnet test $solution -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nBuild and tests succeeded." -ForegroundColor Green
