using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SermonCleanup.Core;

/// <summary>
/// Runs a sermon recording through an ffmpeg filter chain: highpass filter, clip
/// mitigation, compression, two-pass loudness normalization, and silence trimming.
/// </summary>
public sealed class SermonCleaner
{
    // highpass    : removes rumble / mic handling noise below 90Hz
    // asoftclip   : gently rounds off harsh clipped peaks (mitigation, not true declipping)
    // acompressor : evens out speaker dynamics (loud vs quiet passages)
    private const string PreFilters =
        "highpass=f=90,asoftclip=type=tanh:threshold=0.9,acompressor=threshold=0.1:ratio=3:attack=20:release=300:makeup=1.4:knee=6";

    public static bool IsFfmpegAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("ffmpeg", "-version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    public async Task<CleanupResult> CleanAsync(
        CleanupOptions options,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(options.InputFile))
            throw new SermonCleanupException($"Input file not found: {options.InputFile}");

        if (!IsFfmpegAvailable())
            throw new SermonCleanupException("ffmpeg not found in PATH. Install it from https://ffmpeg.org/download.html");

        var stats = await AnalyzeAsync(options, progress, cancellationToken).ConfigureAwait(false);
        await RenderAsync(options, stats, progress, cancellationToken).ConfigureAwait(false);
        return new CleanupResult(options.OutputFile, stats);
    }

    public async Task<LoudnessStats> AnalyzeAsync(
        CleanupOptions options,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Pass 1: analyzing loudness...");

        var filter =
            $"{PreFilters},loudnorm=I={Fmt(options.TargetLufs)}:TP={Fmt(options.TargetTp)}:LRA={Fmt(options.TargetLra)}:print_format=json";

        var (_, stdErr) = await RunFfmpegAsync(
            ["-i", options.InputFile, "-af", filter, "-f", "null", "-"],
            cancellationToken).ConfigureAwait(false);

        var match = Regex.Match(stdErr, "\\{[^{}]*\"input_i\"[^{}]*\\}");
        if (!match.Success)
            throw new SermonCleanupException("Could not parse loudnorm analysis output from ffmpeg.", stdErr);

        LoudnormAnalysis dto;
        try
        {
            dto = JsonSerializer.Deserialize<LoudnormAnalysis>(match.Value)
                  ?? throw new SermonCleanupException("ffmpeg returned an empty loudnorm analysis.", stdErr);
        }
        catch (JsonException ex)
        {
            throw new SermonCleanupException("Failed to parse loudnorm analysis JSON from ffmpeg.", stdErr + "\n" + ex.Message);
        }

        return new LoudnessStats(
            ParseDouble(dto.InputI),
            ParseDouble(dto.InputTp),
            ParseDouble(dto.InputLra),
            ParseDouble(dto.InputThresh),
            ParseDouble(dto.TargetOffset));
    }

    public async Task RenderAsync(
        CleanupOptions options,
        LoudnessStats stats,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Pass 2: applying filters and rendering output...");

        var loudnormPass2 =
            $"loudnorm=I={Fmt(options.TargetLufs)}:TP={Fmt(options.TargetTp)}:LRA={Fmt(options.TargetLra)}:" +
            $"measured_I={Fmt(stats.InputI)}:measured_TP={Fmt(stats.InputTp)}:" +
            $"measured_LRA={Fmt(stats.InputLra)}:measured_thresh={Fmt(stats.InputThresh)}:" +
            $"offset={Fmt(stats.TargetOffset)}:linear=true:print_format=summary";

        // silenceremove: trims leading silence + any internal silence longer than 1s,
        // leaving 0.3s of padding so cuts don't sound abrupt
        const string silenceFilter =
            "silenceremove=start_periods=1:start_duration=0.3:start_threshold=-35dB:start_silence=0.1:" +
            "stop_periods=-1:stop_duration=1:stop_threshold=-35dB:stop_silence=0.3";

        var fullChain = $"{PreFilters},{loudnormPass2},{silenceFilter}";

        var outputDir = Path.GetDirectoryName(Path.GetFullPath(options.OutputFile));
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        var (exitCode, stdErr) = await RunFfmpegAsync(
            [
                "-i", options.InputFile,
                "-af", fullChain,
                "-ar", "44100",
                "-c:a", "libmp3lame",
                "-b:a", "192k",
                "-y", options.OutputFile
            ],
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
            throw new SermonCleanupException("ffmpeg failed during the render pass.", stdErr);
    }

    private static double ParseDouble(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static string Fmt(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static async Task<(int ExitCode, string StdErr)> RunFfmpegAsync(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("ffmpeg")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in arguments)
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdErr = await stdErrTask.ConfigureAwait(false);
        await stdOutTask.ConfigureAwait(false);

        return (process.ExitCode, stdErr);
    }

    private sealed class LoudnormAnalysis
    {
        [JsonPropertyName("input_i")] public string InputI { get; set; } = "0";
        [JsonPropertyName("input_tp")] public string InputTp { get; set; } = "0";
        [JsonPropertyName("input_lra")] public string InputLra { get; set; } = "0";
        [JsonPropertyName("input_thresh")] public string InputThresh { get; set; } = "0";
        [JsonPropertyName("target_offset")] public string TargetOffset { get; set; } = "0";
    }
}
