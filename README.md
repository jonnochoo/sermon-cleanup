# Sermon Cleanup

Cleans up sermon audio recordings with ffmpeg: a highpass filter, soft clip mitigation,
compression, two-pass loudness normalization, and silence trimming. Two independent
implementations, kept behaviorally equivalent (same filter chain, same default loudness targets):

- [`powershell/`](powershell/README.md) — a single-file PowerShell script (the original).
- [`csharp/`](csharp/README.md) — an interactive C# console rewrite (Spectre.Console front end).
  Install it with one command:

  ```powershell
  irm https://raw.githubusercontent.com/jonnochoo/sermon-cleanup/main/install.ps1 | iex
  ```

Both require [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`. See each
variant's README for details on what the filter chain does, usage, and parameters.
