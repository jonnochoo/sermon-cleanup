using System.Reflection;

namespace SermonCleanup.Cli;

internal static class AppVersion
{
    public static string Current { get; } = Compute();

    private static string Compute()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "dev" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
