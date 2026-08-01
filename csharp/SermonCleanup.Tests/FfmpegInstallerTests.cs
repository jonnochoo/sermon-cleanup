using SermonCleanup.Core;

namespace SermonCleanup.Tests;

public class FfmpegInstallerTests
{
    [Fact]
    public void IsWingetAvailable_does_not_throw()
    {
        var exception = Record.Exception(() => FfmpegInstaller.IsWingetAvailable());
        Assert.Null(exception);
    }
}
