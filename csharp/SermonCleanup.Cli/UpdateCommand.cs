using SermonCleanup.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SermonCleanup.Cli;

internal sealed class UpdateCommand : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        if (!SelfUpdater.TryGetCurrentExecutablePath(out var exePath))
        {
            AnsiConsole.MarkupLine(
                "[yellow]Self-update only works for an installed sermon-cleanup.exe[/] " +
                "(not when running via 'dotnet run'). Reinstall with:");
            AnsiConsole.MarkupLine("[grey]irm https://raw.githubusercontent.com/jonnochoo/sermon-cleanup/main/install.ps1 | iex[/]");
            return (int)ExitCode.Error;
        }

        using var http = new HttpClient();

        ReleaseAsset release;
        try
        {
            release = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Checking for updates...", _ => SelfUpdater.GetLatestReleaseAsync(http, cancellationToken: cancellationToken));
        }
        catch (SermonCleanupException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return (int)ExitCode.Error;
        }

        if (!SelfUpdater.IsUpdateAvailable(AppVersion.Current, release.Version))
        {
            AnsiConsole.MarkupLine($"[green]Already up to date[/] (v{AppVersion.Current}).");
            return (int)ExitCode.Success;
        }

        if (!AnsiConsole.Confirm($"Update from v{AppVersion.Current} to v{release.Version}?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return (int)ExitCode.Success;
        }

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Downloading v{release.Version}...", _ => SelfUpdater.ApplyUpdateAsync(http, exePath, release.DownloadUrl, cancellationToken));
        }
        catch (SermonCleanupException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return (int)ExitCode.Error;
        }

        AnsiConsole.MarkupLine($"[green]Updated to v{release.Version}.[/] Restart sermon-cleanup to use the new version.");
        return (int)ExitCode.Success;
    }
}
