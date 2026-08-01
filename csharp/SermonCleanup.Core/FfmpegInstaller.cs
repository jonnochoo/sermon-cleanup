using System.ComponentModel;
using System.Diagnostics;

namespace SermonCleanup.Core;

/// <summary>Checks for winget and installs ffmpeg through it when a manual install isn't wanted.</summary>
public static class FfmpegInstaller
{
    public const string WingetPackageId = "Gyan.FFmpeg";

    public static bool IsWingetAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("winget", "--version")
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

    public static async Task InstallAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report($"Installing ffmpeg via winget ({WingetPackageId})...");

        var startInfo = new ProcessStartInfo("winget")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "install", "--id", WingetPackageId, "-e", "--accept-source-agreements", "--accept-package-agreements" })
            startInfo.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdOut = await stdOutTask.ConfigureAwait(false);
        var stdErr = await stdErrTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
            throw new SermonCleanupException("winget install failed.", stdOut + "\n" + stdErr);
    }
}
