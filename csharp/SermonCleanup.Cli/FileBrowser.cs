using SermonCleanup.Core;
using Spectre.Console;

namespace SermonCleanup.Cli;

/// <summary>Interactive directory/file picker used to choose the input recording.</summary>
internal static class FileBrowser
{
    private const string ManualEntryPath = "\0manual";
    private const string ParentDirPath = "\0parent";

    /// <summary>The user's Downloads folder, falling back to their home directory or the current directory if it can't be found.</summary>
    public static string GetDefaultStartDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home))
        {
            var downloads = Path.Combine(home, "Downloads");
            if (Directory.Exists(downloads))
                return downloads;
        }

        return !string.IsNullOrEmpty(home) && Directory.Exists(home)
            ? home
            : Directory.GetCurrentDirectory();
    }

    public static string SelectInputFile(string startDirectory)
    {
        var currentDir = new DirectoryInfo(startDirectory);

        while (true)
        {
            var choices = new List<(string Display, string Path)>();

            if (currentDir.Parent is not null)
                choices.Add(("[grey].. (up a directory)[/]", ParentDirPath));

            IEnumerable<DirectoryInfo> subDirs;
            IEnumerable<FileInfo> files;
            try
            {
                subDirs = currentDir.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);
                files = currentDir.GetFiles()
                    .Where(f => AudioFileTypes.IsAudioFile(f.FullName))
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine("[red]Access denied to that directory.[/]");
                currentDir = currentDir.Parent ?? currentDir;
                continue;
            }

            foreach (var dir in subDirs)
                choices.Add(($"[blue]{Markup.Escape(dir.Name)}/[/]", dir.FullName));

            foreach (var file in files)
                choices.Add(($"[green]{Markup.Escape(file.Name)}[/]", file.FullName));

            choices.Add(("[yellow]Type a path manually...[/]", ManualEntryPath));

            var prompt = new SelectionPrompt<string>()
                .Title($"Select an input file  [grey]{Markup.Escape(currentDir.FullName)}[/]")
                .PageSize(15)
                .MoreChoicesText("[grey](move up/down to see more)[/]")
                .UseConverter(path => choices.First(c => c.Path == path).Display)
                .AddChoices(choices.Select(c => c.Path));

            var selected = AnsiConsole.Prompt(prompt);

            switch (selected)
            {
                case ParentDirPath:
                    currentDir = currentDir.Parent ?? currentDir;
                    continue;

                case ManualEntryPath:
                    var manualPath = AnsiConsole.Ask<string>("Enter a file path:");
                    if (!File.Exists(manualPath))
                    {
                        AnsiConsole.MarkupLine($"[red]File not found:[/] {Markup.Escape(manualPath)}");
                        continue;
                    }
                    if (!AudioFileTypes.IsAudioFile(manualPath))
                    {
                        AnsiConsole.MarkupLine($"[red]Unsupported file type:[/] {Markup.Escape(Path.GetExtension(manualPath))}");
                        continue;
                    }
                    return Path.GetFullPath(manualPath);

                default:
                    if (Directory.Exists(selected))
                    {
                        currentDir = new DirectoryInfo(selected);
                        continue;
                    }
                    return selected;
            }
        }
    }
}
