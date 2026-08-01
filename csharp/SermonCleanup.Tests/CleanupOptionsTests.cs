using SermonCleanup.Core;

namespace SermonCleanup.Tests;

public class CleanupOptionsTests
{
    [Fact]
    public void Defaults_match_the_documented_loudness_targets()
    {
        var options = new CleanupOptions
        {
            InputFile = "in.wav",
            OutputFile = "out.mp3"
        };

        Assert.Equal(-16, options.TargetLufs);
        Assert.Equal(-1.5, options.TargetTp);
        Assert.Equal(11, options.TargetLra);
    }
}
