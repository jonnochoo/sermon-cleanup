using SermonCleanup.Core;

namespace SermonCleanup.Tests;

public class LoudnessTargetsTests
{
    [Theory]
    [InlineData(-70)]
    [InlineData(-16)]
    [InlineData(-5)]
    public void LufsTarget_accepts_values_within_range(double value)
    {
        Assert.True(LufsTarget.TryCreate(value, out var target, out var error));
        Assert.Equal(value, target.Value);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(-70.1)]
    [InlineData(-4.9)]
    [InlineData(double.NaN)]
    public void LufsTarget_rejects_values_outside_range(double value)
    {
        Assert.False(LufsTarget.TryCreate(value, out _, out var error));
        Assert.NotNull(error);
        Assert.Throws<ArgumentOutOfRangeException>(() => LufsTarget.Create(value));
    }

    [Theory]
    [InlineData(-9)]
    [InlineData(-1.5)]
    [InlineData(0)]
    public void TruePeakTarget_accepts_values_within_range(double value)
    {
        Assert.True(TruePeakTarget.TryCreate(value, out var target, out var error));
        Assert.Equal(value, target.Value);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(-9.1)]
    [InlineData(0.1)]
    public void TruePeakTarget_rejects_values_outside_range(double value)
    {
        Assert.False(TruePeakTarget.TryCreate(value, out _, out var error));
        Assert.NotNull(error);
        Assert.Throws<ArgumentOutOfRangeException>(() => TruePeakTarget.Create(value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    [InlineData(20)]
    public void LoudnessRangeTarget_accepts_values_within_range(double value)
    {
        Assert.True(LoudnessRangeTarget.TryCreate(value, out var target, out var error));
        Assert.Equal(value, target.Value);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(20.1)]
    public void LoudnessRangeTarget_rejects_values_outside_range(double value)
    {
        Assert.False(LoudnessRangeTarget.TryCreate(value, out _, out var error));
        Assert.NotNull(error);
        Assert.Throws<ArgumentOutOfRangeException>(() => LoudnessRangeTarget.Create(value));
    }
}
