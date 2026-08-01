namespace SermonCleanup.Core;

/// <summary>Raised for expected failures (missing ffmpeg, bad input, a failed render pass) as opposed to bugs.</summary>
public sealed class SermonCleanupException : Exception
{
    public string? Details { get; }

    public SermonCleanupException(string message, string? details = null) : base(message)
    {
        Details = details;
    }
}
