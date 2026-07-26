<#
Sermon Audio Cleanup Script
----------------------------
Cleans up a sermon recording: highpass filter, clip mitigation, compression,
two-pass loudness normalization, and silence trimming.

Requires ffmpeg in PATH: https://ffmpeg.org/download.html

Usage:
    .\Clean-Sermon.ps1 -InputFile "sermon.wav"
    .\Clean-Sermon.ps1 -InputFile "sermon.wav" -OutputFile "sermon_clean.mp3" -TargetLUFS -16

If you get an execution policy error, run instead:
    powershell -ExecutionPolicy Bypass -File .\Clean-Sermon.ps1 -InputFile "sermon.wav"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$InputFile,

    [Parameter(Mandatory=$false)]
    [string]$OutputFile = "$([System.IO.Path]::GetFileNameWithoutExtension($InputFile))_clean.mp3",

    [double]$TargetLUFS = -16,
    [double]$TargetTP = -1.5,
    [double]$TargetLRA = 11
)

if (-not (Test-Path $InputFile)) {
    Write-Error "Input file not found: $InputFile"
    exit 1
}

if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Error "ffmpeg not found in PATH. Install it from https://ffmpeg.org/download.html"
    exit 1
}

Write-Host "=== Sermon Cleanup: $InputFile ===" -ForegroundColor Cyan

# --- Static filters applied before loudness normalization ---
# highpass    : removes rumble / mic handling noise below 90Hz
# asoftclip   : gently rounds off harsh clipped peaks (mitigation, not true declipping)
# acompressor : evens out speaker dynamics (loud vs quiet passages)
$preFilters = "highpass=f=90,asoftclip=type=tanh:threshold=0.9,acompressor=threshold=0.1:ratio=3:attack=20:release=300:makeup=1.4:knee=6"

# --- Pass 1: measure loudness stats ---
Write-Host "Pass 1: analyzing loudness..." -ForegroundColor Yellow

$analyzeArgs = @(
    "-i", $InputFile,
    "-af", "$preFilters,loudnorm=I=${TargetLUFS}:TP=${TargetTP}:LRA=${TargetLRA}:print_format=json",
    "-f", "null", "-"
)

$analysisOutput = & ffmpeg @analyzeArgs 2>&1 | Out-String

# Extract the JSON block from ffmpeg's stderr output
$jsonMatch = [regex]::Match($analysisOutput, '\{[^{}]*"input_i"[^{}]*\}')
if (-not $jsonMatch.Success) {
    Write-Error "Could not parse loudnorm analysis. Full ffmpeg output below:"
    Write-Host $analysisOutput
    exit 1
}

$stats = $jsonMatch.Value | ConvertFrom-Json

Write-Host "  Measured Integrated Loudness: $($stats.input_i) LUFS"
Write-Host "  Measured True Peak:           $($stats.input_tp) dBTP"
Write-Host "  Measured LRA:                 $($stats.input_lra)"

# --- Pass 2: apply full chain with measured loudnorm values ---
Write-Host "Pass 2: applying filters and rendering output..." -ForegroundColor Yellow

$loudnormPass2 = "loudnorm=I=${TargetLUFS}:TP=${TargetTP}:LRA=${TargetLRA}:" +
    "measured_I=$($stats.input_i):measured_TP=$($stats.input_tp):" +
    "measured_LRA=$($stats.input_lra):measured_thresh=$($stats.input_thresh):" +
    "offset=$($stats.target_offset):linear=true:print_format=summary"

# silenceremove: trims leading silence + any internal silence longer than 1s,
# leaving 0.3s of padding so cuts don't sound abrupt
$silenceFilter = "silenceremove=start_periods=1:start_duration=0.3:start_threshold=-35dB:start_silence=0.1:" +
    "stop_periods=-1:stop_duration=1:stop_threshold=-35dB:stop_silence=0.3"

$fullChain = "$preFilters,$loudnormPass2,$silenceFilter"

$renderArgs = @(
    "-i", $InputFile,
    "-af", $fullChain,
    "-ar", "44100",
    "-c:a", "libmp3lame",
    "-b:a", "192k",
    "-y", $OutputFile
)

& ffmpeg @renderArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nDone! Output saved to: $OutputFile" -ForegroundColor Green
} else {
    Write-Error "ffmpeg failed during the render pass."
}
