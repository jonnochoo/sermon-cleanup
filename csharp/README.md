# Sermon Cleanup (C#)

An interactive console version of `clean-sermon.ps1`, built with [Spectre.Console](https://spectreconsole.net/).
Same filter chain, same ffmpeg dependency — this just adds an interactive picker for the input file
and prompts for the rest of the settings instead of positional/named parameters.

## Structure

- **`SermonCleanup.Core`** — domain logic: builds the ffmpeg filter chain, runs the two-pass
  loudnorm analysis/render, parses ffmpeg's JSON output. No console or UI code; it only knows
  about `CleanupOptions` in and a `CleanupResult` (or `SermonCleanupException`) out.
- **`SermonCleanup.Cli`** — the Spectre.Console front end: the interactive file browser, prompts
  for output path/loudness targets, progress spinner, and result tables. Contains no ffmpeg logic
  itself — it only calls into `SermonCleanup.Core`.
- **`SermonCleanup.Tests`** — xUnit tests for `SermonCleanup.Core`.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download) — only needed if building from source
- [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`

## Install

One command, no .NET SDK required (downloads a self-contained `sermon-cleanup.exe` from the
latest [release](../../releases) and adds it to your PATH):

```powershell
irm https://raw.githubusercontent.com/jonnochoo/sermon-cleanup/main/install.ps1 | iex
```

Then run `sermon-cleanup` from a new terminal. Re-running the command upgrades in place.

## Usage

```
cd csharp
dotnet run --project SermonCleanup.Cli
```

This launches an interactive file browser (starting in your Downloads folder, filtered to
supported audio types) to choose the input recording, then prompts for the output path and
loudness targets (LUFS / true peak / LRA), each defaulting to the same values as the PowerShell
script (`-16`, `-1.5`, `11`).

You can also pass the input file directly to skip the browser:

```
dotnet run --project SermonCleanup.Cli -- "sermon.wav"
```

### Publishing a standalone executable

Releases are cut via the [`Release` workflow](../.github/workflows/release.yml)
(Actions → Release → Run workflow, enter a version like `1.0.0`). It publishes a self-contained
single-file `win-x64` build, tags it `v<version>`, and attaches `sermon-cleanup.exe` to a new
GitHub release — this is what `install.ps1` downloads.

To build the same artifact locally:

```
dotnet publish SermonCleanup.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Building and testing

```
dotnet build SermonCleanup.sln -c Release
dotnet test SermonCleanup.sln -c Release --no-build
```

Warnings are treated as errors solution-wide (see `Directory.Build.props`), so a clean build
doubles as a lint check. The same two commands run in CI on every push/PR that touches `csharp/**`
— see [`.github/workflows/csharp-build.yml`](../.github/workflows/csharp-build.yml).
