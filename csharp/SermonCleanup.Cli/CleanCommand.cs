using System.ComponentModel;
using System.Globalization;
using SermonCleanup.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SermonCleanup.Cli;

internal sealed class CleanCommand : AsyncCommand<CleanCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[INPUT]")]
        [Description("Path to the recording to clean. If omitted, an interactive file browser opens.")]
        public string? InputFile { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new FigletText("Sermon Cleanup").Color(Color.SteelBlue1));
        AnsiConsole.MarkupLine($"[grey]v{AppVersion.Current}[/]");
        AnsiConsole.MarkupLine("[grey]Highpass filter, clip mitigation, compression, loudness normalization, and silence trimming — powered by ffmpeg.[/]");
        AnsiConsole.WriteLine();

        if (!SermonCleaner.IsFfmpegAvailable())
        {
            AnsiConsole.MarkupLine("[red]ffmpeg was not found in PATH.[/] Run [grey]sermon-cleanup verify[/] for install help, or see https://ffmpeg.org/download.html.");
            return 1;
        }

        string inputFile;
        if (settings.InputFile is not null && File.Exists(settings.InputFile))
        {
            inputFile = Path.GetFullPath(settings.InputFile);
            AnsiConsole.MarkupLine($"Input file: [green]{Markup.Escape(inputFile)}[/]");
        }
        else
        {
            inputFile = FileBrowser.SelectInputFile(FileBrowser.GetDefaultStartDirectory());
        }

        var defaultOutput = Path.Combine(
            Path.GetDirectoryName(inputFile) ?? ".",
            $"{Path.GetFileNameWithoutExtension(inputFile)}_clean.mp3");

        var outputFile = AnsiConsole.Prompt(
            new TextPrompt<string>("Output file:").DefaultValue(defaultOutput));

        var targetLufs = AnsiConsole.Prompt(
            new TextPrompt<double>("Target integrated loudness [grey](LUFS)[/]:").DefaultValue(-16.0));

        var targetTp = AnsiConsole.Prompt(
            new TextPrompt<double>("Target true peak [grey](dBTP)[/]:").DefaultValue(-1.5));

        var targetLra = AnsiConsole.Prompt(
            new TextPrompt<double>("Target loudness range [grey](LRA)[/]:").DefaultValue(11.0));

        var options = new CleanupOptions
        {
            InputFile = inputFile,
            OutputFile = outputFile,
            TargetLufs = targetLufs,
            TargetTp = targetTp,
            TargetLra = targetLra
        };

        var summary = new Table().Border(TableBorder.Rounded).AddColumn("Setting").AddColumn("Value");
        summary.AddRow("Input", Markup.Escape(options.InputFile));
        summary.AddRow("Output", Markup.Escape(options.OutputFile));
        summary.AddRow("Target LUFS", options.TargetLufs.ToString(CultureInfo.InvariantCulture));
        summary.AddRow("Target TP", $"{options.TargetTp.ToString(CultureInfo.InvariantCulture)} dBTP");
        summary.AddRow("Target LRA", options.TargetLra.ToString(CultureInfo.InvariantCulture));
        AnsiConsole.Write(summary);

        if (!AnsiConsole.Confirm("Proceed with cleanup?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return 0;
        }

        var cleaner = new SermonCleaner();
        LoudnessStats? stats = null;

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Starting...", async ctx =>
                {
                    var progress = new Progress<string>(message => ctx.Status(message));
                    var result = await cleaner.CleanAsync(options, progress, cancellationToken);
                    stats = result.Stats;
                });
        }
        catch (SermonCleanupException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            if (!string.IsNullOrWhiteSpace(ex.Details))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]ffmpeg output:[/]");
                AnsiConsole.WriteLine(ex.Details);
            }
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Done![/] Output saved to [bold]{Markup.Escape(options.OutputFile)}[/]");

        if (stats is not null)
        {
            var statsTable = new Table().Border(TableBorder.Rounded).AddColumn("Measured").AddColumn("Value");
            statsTable.AddRow("Integrated Loudness", $"{stats.InputI.ToString(CultureInfo.InvariantCulture)} LUFS");
            statsTable.AddRow("True Peak", $"{stats.InputTp.ToString(CultureInfo.InvariantCulture)} dBTP");
            statsTable.AddRow("Loudness Range", stats.InputLra.ToString(CultureInfo.InvariantCulture));
            AnsiConsole.Write(statsTable);
        }

        return 0;
    }
}
