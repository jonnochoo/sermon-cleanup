namespace SermonCleanup.Core;

public sealed record CleanupOptions
{
    public required string InputFile { get; init; }
    public required string OutputFile { get; init; }
    public double TargetLufs { get; init; } = -16;
    public double TargetTp { get; init; } = -1.5;
    public double TargetLra { get; init; } = 11;
}
