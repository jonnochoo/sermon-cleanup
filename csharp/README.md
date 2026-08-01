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

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`

## Usage

```
cd csharp
dotnet run --project SermonCleanup.Cli
```

This launches an interactive file browser (starting in the current directory) to choose the
input recording, then prompts for the output path and loudness targets (LUFS / true peak / LRA),
each defaulting to the same values as the PowerShell script (`-16`, `-1.5`, `11`).

You can also pass the input file directly to skip the browser:

```
dotnet run --project SermonCleanup.Cli -- "sermon.wav"
```

### Publishing a standalone executable

```
dotnet publish SermonCleanup.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
