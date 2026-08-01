using SermonCleanup.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SermonCleanup.Cli;

internal sealed class VerifyCommand : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("Checking for ffmpeg...");

        if (SermonCleaner.IsFfmpegAvailable())
        {
            AnsiConsole.MarkupLine("[green]ffmpeg found in PATH.[/] sermon-cleanup is ready to use.");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]ffmpeg was not found in PATH.[/]");
        AnsiConsole.WriteLine();

        if (!FfmpegInstaller.IsWingetAvailable())
        {
            AnsiConsole.MarkupLine("Install it manually from https://ffmpeg.org/download.html,");
            AnsiConsole.MarkupLine($"or via winget once available: [grey]winget install --id {FfmpegInstaller.WingetPackageId} -e[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("It can be installed via winget:");
        AnsiConsole.MarkupLine($"[grey]winget install --id {FfmpegInstaller.WingetPackageId} -e[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm("Install it now?"))
        {
            AnsiConsole.MarkupLine("[yellow]Skipped.[/] Run 'sermon-cleanup verify' again once ffmpeg is installed.");
            return 1;
        }

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Installing ffmpeg via winget...", ctx =>
                    FfmpegInstaller.InstallAsync(new Progress<string>(message => ctx.Status(message)), cancellationToken));
        }
        catch (SermonCleanupException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            if (!string.IsNullOrWhiteSpace(ex.Details))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]winget output:[/]");
                AnsiConsole.WriteLine(ex.Details);
            }
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Installed.[/] Open a new terminal and run 'sermon-cleanup verify' again to confirm.");
        return 0;
    }
}
