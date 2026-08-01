namespace SermonCleanup.Core;

public static class AudioFileTypes
{
    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3", ".m4a", ".flac", ".aac", ".ogg", ".wma", ".aiff", ".aif"
    };

    public static bool IsAudioFile(string path) => Extensions.Contains(Path.GetExtension(path));
}
