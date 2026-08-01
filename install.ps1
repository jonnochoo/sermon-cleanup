<#
Installs sermon-cleanup: downloads the latest GitHub release and adds it to your PATH.

One-command install (from PowerShell):
    irm https://raw.githubusercontent.com/jonnochoo/sermon-cleanup/main/install.ps1 | iex

Re-running this script upgrades to the latest release in place.
#>
param(
    [string]$Repo = "jonnochoo/sermon-cleanup",
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "SermonCleanup")
)

$ErrorActionPreference = "Stop"

Write-Host "Fetching latest release info for $Repo..." -ForegroundColor Cyan
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest"

$asset = $release.assets | Where-Object { $_.name -eq "sermon-cleanup.exe" }
if (-not $asset) {
    Write-Error "Could not find sermon-cleanup.exe in the latest release ($($release.tag_name))."
    exit 1
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
$exePath = Join-Path $InstallDir "sermon-cleanup.exe"

Write-Host "Downloading $($release.tag_name) to $exePath..." -ForegroundColor Cyan
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $exePath

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
$pathEntries = $userPath -split ";" | Where-Object { $_ -ne "" }
if ($pathEntries -notcontains $InstallDir) {
    Write-Host "Adding $InstallDir to your PATH..." -ForegroundColor Cyan
    [Environment]::SetEnvironmentVariable("Path", "$userPath;$InstallDir", "User")
    $env:Path = "$env:Path;$InstallDir"
    Write-Host "Added. Open a new terminal for the PATH change to take effect there." -ForegroundColor Yellow
} else {
    Write-Host "$InstallDir is already on your PATH." -ForegroundColor Green
}

Write-Host "`nInstalled sermon-cleanup $($release.tag_name). Run 'sermon-cleanup' from a new terminal to get started." -ForegroundColor Green
Write-Host "Note: ffmpeg must also be in your PATH — see https://ffmpeg.org/download.html" -ForegroundColor Yellow
