namespace SermonCleanup.Core;

public sealed record LoudnessStats(
    double InputI,
    double InputTp,
    double InputLra,
    double InputThresh,
    double TargetOffset);
