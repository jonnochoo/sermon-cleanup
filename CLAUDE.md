# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

Two independent implementations of the same sermon-audio cleanup pipeline, kept in sync
conceptually but not sharing code:

- `clean-sermon.ps1` — a single-file PowerShell script (the original).
- `csharp/` — an interactive C# console rewrite (Spectre.Console front end).

Both shell out to `ffmpeg` and must be kept behaviorally equivalent: same filter chain, same
default loudness targets (`-16` LUFS / `-1.5` dBTP / `11` LRA). If you change the filter chain or
defaults in one, check whether the other needs the same change.

## Commands

### PowerShell variant

No build step — edit and run directly:

```powershell
.\clean-sermon.ps1 -InputFile "sermon.wav" -OutputFile "out.mp3"
```

### C# variant (run from `csharp/`)

```
dotnet build SermonCleanup.sln -c Release
dotnet test SermonCleanup.sln -c Release --no-build
dotnet run --project SermonCleanup.Cli                    # full interactive flow
dotnet run --project SermonCleanup.Cli -- "sermon.wav"    # skip the file browser
```

Run a single test class/method with the standard xUnit filter, e.g.:

```
dotnet test SermonCleanup.sln --filter "FullyQualifiedName~AudioFileTypesTests"
```

**Warnings are treated as errors solution-wide** (`csharp/Directory.Build.props`), so a warning
fails `dotnet build`, not just the CI lint step. CI (`.github/workflows/csharp-build.yml`) runs
`dotnet restore` / `build -c Release` / `test -c Release` on `ubuntu-latest` for every push to
`main` and every PR touching `csharp/**`.

Both `ffmpeg` (in `PATH`) and the .NET 8 SDK are required to build/run/test the C# variant —
`SermonCleaner.IsFfmpegAvailable()` and the missing-input-file test only check the *absence*
path; nothing in CI actually invokes ffmpeg (it isn't installed on the runner).

### Cutting a release

Trigger the [`Release` workflow](.github/workflows/release.yml) manually (Actions → Release → Run
workflow) with a `version` input like `1.0.0`. It publishes a self-contained single-file `win-x64`
build of `SermonCleanup.Cli`, renames it to the stable asset name `sermon-cleanup.exe`, and creates
a GitHub release tagged `v<version>` with that exe attached and auto-generated notes. There's no
separate tag-push step — the tag is created by `gh release create` as part of the workflow.

`install.ps1` (repo root) is the one-command installer end users run
(`irm .../install.ps1 | iex`): it looks up the latest release via the GitHub API, requires an
asset literally named `sermon-cleanup.exe` (matching what the workflow produces), downloads it to
`%LOCALAPPDATA%\SermonCleanup`, and adds that directory to the user `PATH`. If you rename the
published executable, update both the workflow and this script together.

## Git & PR workflow

- Never commit directly to `main`. Work on a `feature/<short-description>` branch and open a PR.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):
  `<type>: <summary>` (`feat`, `fix`, `docs`, `build`, `refactor`, `test`, `chore`, ...). Keep each
  commit to one logical change rather than bundling unrelated work.
- PR descriptions use exactly three headings, in this order:
  - `## Intent` — why this change, in a sentence or two.
  - `## What Changed` — the changes, calling out specific files/lines a reviewer should look at
    closely (not just a restated file list).
  - `## Risk` — what could break and how it was verified (or wasn't).
- Before opening or updating a PR touching `csharp/`, run `dotnet build -c Release` and
  `dotnet test -c Release` from `csharp/` and confirm 0 warnings — this is what CI enforces, and
  warnings-as-errors means a warning is a build failure either way.
- Push the branch and open the PR with `gh pr create`; don't merge without the user's go-ahead.

## C# architecture

The C# solution is deliberately split so domain logic never depends on the console:

- **`SermonCleanup.Core`** — all ffmpeg orchestration. No reference to Spectre.Console or any
  console/UI type.
  - `SermonCleaner` builds the two-pass ffmpeg filter chain (highpass → asoftclip → acompressor →
    loudnorm pass 1 analyze → loudnorm pass 2 render with `measured_*`/`offset` from pass 1 →
    silenceremove) and runs it via `Process`. `AnalyzeAsync` regex-extracts the `loudnorm`
    JSON block from ffmpeg's stderr and parses it (loudnorm reports numeric fields as JSON
    *strings*, hence the `LoudnormAnalysis` DTO with string properties parsed via
    `double.Parse(..., CultureInfo.InvariantCulture)`).
  - Expected failures (missing input, missing ffmpeg, unparseable ffmpeg output, non-zero exit)
    are surfaced as `SermonCleanupException`, not raw exceptions — the CLI catches only that type.
  - `CleanupOptions` (input) → `CleanupResult` (output: path + `LoudnessStats`) is the entire
    public contract other code should depend on.
  - `AudioFileTypes.IsAudioFile` is the single source of truth for which extensions are
    "supported" — both the CLI file browser and any future variant should call into this rather
    than re-listing extensions.

- **`SermonCleanup.Cli`** — Spectre.Console only. `Program.cs` (top-level statements) drives
  prompts/summary/progress/results; `FileBrowser.SelectInputFile` is a self-contained interactive
  directory walker (default start dir: `FileBrowser.GetDefaultStartDirectory()`, which is the
  user's Downloads folder, falling back to home then cwd). The CLI must not contain ffmpeg
  arguments or parsing logic — that belongs in Core. Interactive prompts require a real console
  (Spectre.Console can't read piped/redirected stdin), so non-interactive repros of Core behavior
  should call `SermonCleaner` directly rather than driving `Program.cs`.

- **`SermonCleanup.Tests`** — xUnit, references only `SermonCleanup.Core` (no UI to test). Keep
  new Core logic unit-testable without invoking a real ffmpeg process where possible.
