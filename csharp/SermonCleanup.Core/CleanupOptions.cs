namespace SermonCleanup.Core;

public sealed record CleanupOptions
{
    public required string InputFile { get; init; }
    public required string OutputFile { get; init; }
    public LufsTarget TargetLufs { get; init; } = LufsTarget.Create(-16);
    public TruePeakTarget TargetTp { get; init; } = TruePeakTarget.Create(-1.5);
    public LoudnessRangeTarget TargetLra { get; init; } = LoudnessRangeTarget.Create(11);
}
