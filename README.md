# Sermon Cleanup

Two independent implementations of the same sermon-audio cleanup pipeline, kept behaviorally
equivalent (same filter chain, same default loudness targets):

- [`powershell/`](powershell/README.md) — a single-file PowerShell script (the original).
- [`csharp/`](csharp/README.md) — an interactive C# console rewrite (Spectre.Console front end).
  Install it with one command:

  ```powershell
  irm https://raw.githubusercontent.com/jonnochoo/sermon-cleanup/main/install.ps1 | iex
  ```

Both require [ffmpeg](https://ffmpeg.org/download.html) available in your `PATH`. See each
variant's README for install/usage/parameters.

## What it does

Runs a recording through a filter chain to make it sound more polished and consistent:

1. **Highpass filter** — removes rumble and mic handling noise below 90Hz
2. **Soft clip mitigation** — gently rounds off harsh clipped peaks
3. **Compression** — evens out dynamics between loud and quiet passages
4. **Two-pass loudness normalization** — analyzes the audio, then normalizes it to a target integrated loudness (LUFS), true peak, and loudness range
5. **Silence trimming** — trims leading silence and shortens long internal silences, leaving a small padding so cuts don't sound abrupt

Output is rendered as an MP3 (44.1kHz, 192kbps).

```
 Input file
     |
     v
 +-------------------+
 |  Highpass filter  |  removes rumble/handling noise below 90Hz
 +-------------------+
     |
     v
 +-------------------+
 |  Soft clip fix    |  rounds off harsh clipped peaks
 +-------------------+
     |
     v
 +-------------------+
 |  Compression       |  evens out loud/quiet passages
 +-------------------+
     |
     v
 +-------------------------------+
 |  Loudness normalization        |
 |   Pass 1: analyze (measure)    |
 |   Pass 2: apply (normalize)    |
 +-------------------------------+
     |
     v
 +-------------------+
 |  Silence trimming  |  trims leading/long internal silence
 +-------------------+
     |
     v
 Output MP3 (44.1kHz, 192kbps)
```

## Filter definitions

| Filter | ffmpeg name | What it does |
|---|---|---|
| Highpass | `highpass=f=90` | Cuts frequencies below 90Hz, removing low-frequency rumble, HVAC noise, and mic handling thumps while leaving speech untouched |
| Soft clip mitigation | `asoftclip=type=tanh` | Smoothly rounds off samples that were clipped during recording, reducing the harshness of digital distortion (not true declipping — it can't recover lost audio, only make the clipping less jarring) |
| Compression | `acompressor` | Reduces the volume of loud passages above a threshold so that quiet and loud speech end up closer in level; `ratio` controls how strongly, `attack`/`release` control reaction speed, `makeup` boosts overall level afterward, `knee` softens the transition into compression |
| Loudness normalization | `loudnorm` | Implements the EBU R128 standard, adjusting the audio so it hits a target loudness rather than just a target peak. Run twice: pass 1 measures the file's actual loudness stats, pass 2 uses those measurements to normalize accurately (`linear=true`) |
| Silence trimming | `silenceremove` | Detects stretches of near-silence (below a dB threshold) and cuts them down — trimming dead air at the start and shortening long pauses mid-recording, while leaving a bit of padding so cuts don't feel abrupt |

### Loudness terms (used by `loudnorm`)

- **LUFS** (Loudness Units Full Scale) — a perceptual loudness measurement, similar to volume as a listener actually hears it, rather than a raw peak amplitude. `-16 LUFS` is a common target for spoken-word/podcast content.
- **True Peak (dBTP)** — the highest instantaneous signal level, including peaks that occur *between* samples (which a simple peak meter can miss). Keeping this below 0 dBTP avoids clipping on playback devices that reconstruct the waveform.
- **LRA** (Loudness Range) — how much the loudness varies over the course of the recording, in loudness units. A lower LRA means more consistent volume throughout; a higher LRA preserves more natural dynamic variation.
