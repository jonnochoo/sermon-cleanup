using SermonCleanup.Core;

namespace SermonCleanup.Tests;

public class SermonCleanerTests
{
    [Fact]
    public void IsFfmpegAvailable_does_not_throw()
    {
        // Whether ffmpeg is installed varies by machine; this just guards the
        // Win32Exception-swallowing path so a missing ffmpeg fails gracefully.
        var exception = Record.Exception(() => SermonCleaner.IsFfmpegAvailable());
        Assert.Null(exception);
    }

    [Fact]
    public async Task CleanAsync_throws_when_input_file_is_missing()
    {
        var cleaner = new SermonCleaner();
        var options = new CleanupOptions
        {
            InputFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav"),
            OutputFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp3")
        };

        var ex = await Assert.ThrowsAsync<SermonCleanupException>(() => cleaner.CleanAsync(options));
        Assert.Contains("Input file not found", ex.Message);
    }
}
