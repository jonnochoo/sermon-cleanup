# Sermon Cleanup (PowerShell)

A single-file PowerShell script for cleaning up sermon audio recordings using ffmpeg — the
original implementation. See the [root README](../README.md) for what the filter chain does.
There's also an interactive C# console variant — see [`../csharp/README.md`](../csharp/README.md).

## Requirements

- [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`
- PowerShell

## Usage

```powershell
.\clean-sermon.ps1 -InputFile "sermon.wav"
```

With custom output file and loudness target:

```powershell
.\clean-sermon.ps1 -InputFile "sermon.wav" -OutputFile "sermon_clean.mp3" -TargetLUFS -16
```

If you hit an execution policy error, run instead:

```powershell
powershell -ExecutionPolicy Bypass -File .\clean-sermon.ps1 -InputFile "sermon.wav"
```

### Parameters

| Parameter | Default | Description |
|---|---|---|
| `-InputFile` | *(required)* | Path to the source recording |
| `-OutputFile` | `<InputFile>_clean.mp3` | Path for the cleaned output |
| `-TargetLUFS` | `-16` | Target integrated loudness |
| `-TargetTP` | `-1.5` | Target true peak (dBTP) |
| `-TargetLRA` | `11` | Target loudness range |
