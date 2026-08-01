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

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`

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

```
dotnet publish SermonCleanup.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Building and testing

```
.\build.ps1
```

Restores, builds, and runs the test suite. Warnings are treated as errors solution-wide (see
`Directory.Build.props`), so this doubles as a lint check. Pass `-Configuration Release` to build
in release mode.
