# Sermon Cleanup (C#)

An interactive console version of [`../powershell/clean-sermon.ps1`](../powershell/clean-sermon.ps1), built with [Spectre.Console](https://spectreconsole.net/)
(interactive prompts/file browser) and [Spectre.Console.Cli](https://spectreconsole.net/cli/) (argument
parsing, subcommands, `--help`/`--version`). Same filter chain, same ffmpeg dependency as the
PowerShell script (see the [root README](../README.md) for what the filter chain does) — this adds
an interactive picker for the input file and prompts for the rest of the settings instead of
positional/named parameters.

## Structure

- **`SermonCleanup.Core`** — domain logic: builds the ffmpeg filter chain, runs the two-pass
  loudnorm analysis/render, parses ffmpeg's JSON output, and checks/applies self-updates from
  GitHub releases (`SelfUpdater`). No console or UI code; it only knows about `CleanupOptions` in
  and a `CleanupResult` (or `SermonCleanupException`) out.
- **`SermonCleanup.Cli`** — the Spectre.Console front end: `Program.cs` just wires up a
  `Spectre.Console.Cli` `CommandApp` with two commands, `CleanCommand` (the interactive file
  browser, prompts, progress spinner, result tables) and `UpdateCommand` (checks/applies an
  update). Contains no ffmpeg or update logic itself — it only calls into `SermonCleanup.Core`.
- **`SermonCleanup.Tests`** — xUnit tests for `SermonCleanup.Core`.

## Commands

- `sermon-cleanup clean [INPUT]` — the interactive cleanup flow. `INPUT` is optional; omit it to
  pick a file from the interactive browser.
- `sermon-cleanup update` — checks the latest GitHub release and, if newer, downloads and swaps in
  the new `sermon-cleanup.exe` (only works for an installed exe, not `dotnet run`).
- `sermon-cleanup` (no arguments) or `--help` — lists the commands above.
- `sermon-cleanup --version` — prints the installed version.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download) — only needed if building from source
- [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`

## Install

One command, no .NET SDK required (downloads a self-contained `sermon-cleanup.exe` from the
latest [release](../../releases) and adds it to your PATH):

```powershell
irm https://raw.githubusercontent.com/jonnochoo/sermon-cleanup/main/install.ps1 | iex
```

Then run `sermon-cleanup clean` from a new terminal. To upgrade later, run `sermon-cleanup update`
instead of re-running the install command.

## Usage

```
cd csharp
dotnet run --project SermonCleanup.Cli -- clean
```

This launches an interactive file browser (starting in your Downloads folder, filtered to
supported audio types) to choose the input recording, then prompts for the output path and
loudness targets (LUFS / true peak / LRA), each defaulting to the same values as the PowerShell
script (`-16`, `-1.5`, `11`).

You can also pass the input file directly to skip the browser:

```
dotnet run --project SermonCleanup.Cli -- clean "sermon.wav"
```

### Publishing a standalone executable

Releases are cut via the [`Release` workflow](../.github/workflows/release.yml)
(Actions → Release → Run workflow). Pick a `bump` of `patch` (default), `minor`, or `major` to
auto-increment from the latest release tag, or `explicit` plus a `version` like `1.2.0` to set
the version yourself. It publishes a self-contained single-file `win-x64` build, tags it
`v<version>`, and attaches `sermon-cleanup.exe` to a new GitHub release — this is what
`install.ps1` downloads.

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
