using SermonCleanup.Core;

namespace SermonCleanup.Tests;

public class SelfUpdaterTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.0.1", "1.0.0", false)]
    public void IsUpdateAvailable_compares_versions_correctly(string current, string latest, bool expected)
    {
        Assert.Equal(expected, SelfUpdater.IsUpdateAvailable(current, latest));
    }

    [Theory]
    [InlineData("dev", "1.0.0")]
    [InlineData("1.0.0", "not-a-version")]
    [InlineData("not-a-version", "not-a-version")]
    public void IsUpdateAvailable_returns_false_for_unparseable_versions(string current, string latest)
    {
        Assert.False(SelfUpdater.IsUpdateAvailable(current, latest));
    }

    [Fact]
    public void TryGetCurrentExecutablePath_does_not_report_the_dotnet_host_as_the_app()
    {
        // Whatever process is running the tests, the heuristic must never say "yes, self-update
        // this" while pointing at a file literally named dotnet(.exe) — that's the one case it
        // exists to rule out.
        var result = SelfUpdater.TryGetCurrentExecutablePath(out var path);

        if (result)
        {
            Assert.NotEqual("dotnet", Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Null(path);
        }
    }
}
