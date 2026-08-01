using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SermonCleanup.Core;

/// <summary>Checks for and applies updates to the installed sermon-cleanup.exe from GitHub releases.</summary>
public static class SelfUpdater
{
    public const string DefaultRepo = "jonnochoo/sermon-cleanup";
    private const string AssetName = "sermon-cleanup.exe";

    /// <summary>
    /// The path to the running executable, or false if running via the .NET host
    /// (e.g. `dotnet run`) rather than a published, self-contained sermon-cleanup.exe.
    /// </summary>
    public static bool TryGetCurrentExecutablePath([NotNullWhen(true)] out string? path)
    {
        var processPath = Environment.ProcessPath;
        if (processPath is not null &&
            !string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            path = processPath;
            return true;
        }

        path = null;
        return false;
    }

    public static bool IsUpdateAvailable(string currentVersion, string latestVersion) =>
        Version.TryParse(currentVersion, out var current) &&
        Version.TryParse(latestVersion, out var latest) &&
        latest > current;

    public static async Task<ReleaseAsset> GetLatestReleaseAsync(
        HttpClient http,
        string repo = DefaultRepo,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repo}/releases/latest");
        request.Headers.UserAgent.ParseAdd("sermon-cleanup-updater");

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new SermonCleanupException($"Could not fetch the latest release for {repo} (HTTP {(int)response.StatusCode}).");

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = doc.RootElement;

        var tagName = root.GetProperty("tag_name").GetString()
            ?? throw new SermonCleanupException("Latest release has no tag name.");

        JsonElement? asset = null;
        foreach (var candidate in root.GetProperty("assets").EnumerateArray())
        {
            if (candidate.GetProperty("name").GetString() == AssetName)
            {
                asset = candidate;
                break;
            }
        }

        if (asset is null)
            throw new SermonCleanupException($"Latest release ({tagName}) has no '{AssetName}' asset.");

        var downloadUrl = asset.Value.GetProperty("browser_download_url").GetString()
            ?? throw new SermonCleanupException($"Release asset '{AssetName}' has no download URL.");

        return new ReleaseAsset(tagName, tagName.TrimStart('v'), downloadUrl);
    }

    /// <summary>
    /// Downloads <paramref name="downloadUrl"/> and swaps it in for <paramref name="currentExePath"/>.
    /// Windows allows renaming a running executable even though it can't be overwritten in place,
    /// so the current exe is renamed aside (best-effort deleted afterward) rather than replaced directly.
    /// </summary>
    public static async Task ApplyUpdateAsync(
        HttpClient http,
        string currentExePath,
        string downloadUrl,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(currentExePath))
            ?? throw new SermonCleanupException($"Could not determine the directory of {currentExePath}.");

        var newPath = Path.Combine(directory, AssetName + ".new");
        var backupPath = Path.Combine(directory, AssetName + ".old");

        if (File.Exists(backupPath))
        {
            try { File.Delete(backupPath); }
            catch (IOException) { /* left over from a previous update while still in use; ignore */ }
        }

        using (var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
        {
            if (!response.IsSuccessStatusCode)
                throw new SermonCleanupException($"Could not download the update (HTTP {(int)response.StatusCode}).");

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var destination = File.Create(newPath);
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        }

        File.Move(currentExePath, backupPath, overwrite: true);
        File.Move(newPath, currentExePath, overwrite: true);

        try { File.Delete(backupPath); }
        catch (IOException) { /* still in use by the running process; cleaned up on the next update */ }
    }
}
