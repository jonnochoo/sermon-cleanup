using SermonCleanup.Cli;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("sermon-cleanup");
    config.SetApplicationVersion(AppVersion.Current);

    config.AddCommand<CleanCommand>("clean")
        .WithDescription("Clean up a sermon recording (highpass filter, compression, loudness normalization, silence trimming).");

    config.AddCommand<UpdateCommand>("update")
        .WithDescription("Update sermon-cleanup to the latest release.");

    config.AddCommand<VerifyCommand>("verify")
        .WithDescription("Check that ffmpeg is installed, and offer to install it via winget if not.");
});

return await app.RunAsync(args);
