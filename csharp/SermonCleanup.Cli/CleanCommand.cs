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
        PrintBanner();

        if (!SermonCleaner.IsFfmpegAvailable())
            return ReportMissingFfmpeg();

        var inputFile = ResolveInputFile(settings);
        var options = PromptForOptions(inputFile);

        PrintSummary(options);
        if (!AnsiConsole.Confirm("Proceed with cleanup?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return (int)ExitCode.Success;
        }

        return await RunCleanupAsync(options, cancellationToken);
    }

    private static void PrintBanner()
    {
        AnsiConsole.Write(new FigletText("Sermon Cleanup").Color(Color.SteelBlue1));
        AnsiConsole.MarkupLine($"[grey]v{AppVersion.Current}[/]");
        AnsiConsole.MarkupLine("[grey]Highpass filter, clip mitigation, compression, loudness normalization, and silence trimming — powered by ffmpeg.[/]");
        AnsiConsole.WriteLine();
    }

    private static int ReportMissingFfmpeg()
    {
        AnsiConsole.MarkupLine("[red]ffmpeg was not found in PATH.[/] Install it from https://ffmpeg.org/download.html and try again.");
        return (int)ExitCode.Error;
    }

    private static string ResolveInputFile(Settings settings)
    {
        if (settings.InputFile is not null && File.Exists(settings.InputFile))
        {
            var inputFile = Path.GetFullPath(settings.InputFile);
            AnsiConsole.MarkupLine($"Input file: [green]{Markup.Escape(inputFile)}[/]");
            return inputFile;
        }

        return FileBrowser.SelectInputFile(FileBrowser.GetDefaultStartDirectory());
    }

    private static CleanupOptions PromptForOptions(string inputFile)
    {
        var defaultOutput = Path.Combine(
            Path.GetDirectoryName(inputFile) ?? ".",
            $"{Path.GetFileNameWithoutExtension(inputFile)}_clean.mp3");

        var outputFile = AnsiConsole.Prompt(
            new TextPrompt<string>("Output file:").DefaultValue(defaultOutput));

        var targetLufs = PromptTarget<LufsTarget>("Target integrated loudness [grey](LUFS)[/]:", -16.0, LufsTarget.TryCreate);
        var targetTp = PromptTarget<TruePeakTarget>("Target true peak [grey](dBTP)[/]:", -1.5, TruePeakTarget.TryCreate);
        var targetLra = PromptTarget<LoudnessRangeTarget>("Target loudness range [grey](LRA)[/]:", 11.0, LoudnessRangeTarget.TryCreate);

        return new CleanupOptions
        {
            InputFile = inputFile,
            OutputFile = outputFile,
            TargetLufs = targetLufs,
            TargetTp = targetTp,
            TargetLra = targetLra
        };
    }

    private delegate bool TryCreateTarget<T>(double value, out T target, out string? error);

    // Validate() only runs when the user types a value — Spectre returns DefaultValue directly
    // on blank input without calling it — so the accepted double is always parsed once here,
    // after the prompt returns, rather than relying on Validate() to have populated a closure.
    private static T PromptTarget<T>(string prompt, double defaultValue, TryCreateTarget<T> tryCreate)
    {
        var value = AnsiConsole.Prompt(
            new TextPrompt<double>(prompt)
                .DefaultValue(defaultValue)
                .Validate(v => tryCreate(v, out _, out var error)
                    ? ValidationResult.Success()
                    : ValidationResult.Error(error!)));

        tryCreate(value, out var target, out _);
        return target;
    }

    private static void PrintSummary(CleanupOptions options)
    {
        var summary = new Table().Border(TableBorder.Rounded).AddColumn("Setting").AddColumn("Value");
        summary.AddRow("Input", Markup.Escape(options.InputFile));
        summary.AddRow("Output", Markup.Escape(options.OutputFile));
        summary.AddRow("Target LUFS", options.TargetLufs.ToString());
        summary.AddRow("Target TP", $"{options.TargetTp} dBTP");
        summary.AddRow("Target LRA", options.TargetLra.ToString());
        AnsiConsole.Write(summary);
    }

    private static async Task<int> RunCleanupAsync(CleanupOptions options, CancellationToken cancellationToken)
    {
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
            return (int)ExitCode.Error;
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

        return (int)ExitCode.Success;
    }
}
