using System.Globalization;

namespace SermonCleanup.Core;

/// <summary>
/// Target integrated loudness, in LUFS, for ffmpeg's loudnorm filter (valid range: -70 to -5).
/// </summary>
public readonly record struct LufsTarget
{
    public const double Min = -70;
    public const double Max = -5;

    public double Value { get; }

    private LufsTarget(double value) => Value = value;

    public static LufsTarget Create(double value) =>
        TryCreate(value, out var target, out var error) ? target : throw new ArgumentOutOfRangeException(nameof(value), value, error);

    public static bool TryCreate(double value, out LufsTarget target, out string? error) =>
        LoudnessTargetRange.TryCreate(value, Min, Max, "LUFS", v => new LufsTarget(v), out target, out error);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator double(LufsTarget target) => target.Value;
}

/// <summary>
/// Target true peak, in dBTP, for ffmpeg's loudnorm filter (valid range: -9 to 0).
/// </summary>
public readonly record struct TruePeakTarget
{
    public const double Min = -9;
    public const double Max = 0;

    public double Value { get; }

    private TruePeakTarget(double value) => Value = value;

    public static TruePeakTarget Create(double value) =>
        TryCreate(value, out var target, out var error) ? target : throw new ArgumentOutOfRangeException(nameof(value), value, error);

    public static bool TryCreate(double value, out TruePeakTarget target, out string? error) =>
        LoudnessTargetRange.TryCreate(value, Min, Max, "dBTP", v => new TruePeakTarget(v), out target, out error);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator double(TruePeakTarget target) => target.Value;
}

/// <summary>
/// Target loudness range, in LU, for ffmpeg's loudnorm filter (valid range: 1 to 20).
/// </summary>
public readonly record struct LoudnessRangeTarget
{
    public const double Min = 1;
    public const double Max = 20;

    public double Value { get; }

    private LoudnessRangeTarget(double value) => Value = value;

    public static LoudnessRangeTarget Create(double value) =>
        TryCreate(value, out var target, out var error) ? target : throw new ArgumentOutOfRangeException(nameof(value), value, error);

    public static bool TryCreate(double value, out LoudnessRangeTarget target, out string? error) =>
        LoudnessTargetRange.TryCreate(value, Min, Max, "LU", v => new LoudnessRangeTarget(v), out target, out error);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator double(LoudnessRangeTarget target) => target.Value;
}

file static class LoudnessTargetRange
{
    public static bool TryCreate<T>(double value, double min, double max, string unit, Func<double, T> create, out T target, out string? error)
    {
        if (double.IsNaN(value) || value < min || value > max)
        {
            target = default!;
            error = $"Must be between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)} {unit}.";
            return false;
        }

        target = create(value);
        error = null;
        return true;
    }
}
