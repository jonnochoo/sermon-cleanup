using SermonCleanup.Cli;
using SermonCleanup.Core;
using Spectre.Console.Testing;

namespace SermonCleanup.Tests;

public class CleanCommandPromptTests
{
    [Fact]
    public void PromptTarget_accepting_the_default_returns_the_default_value()
    {
        var console = new TestConsole();
        console.Input.PushKey(ConsoleKey.Enter);

        var target = CleanCommand.PromptTarget<LufsTarget>(
            console, "Target integrated loudness:", -16.0, LufsTarget.TryCreate);

        Assert.Equal(-16.0, target.Value);
    }

    [Fact]
    public void PromptTarget_typing_a_value_returns_that_value()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("-18.5");

        var target = CleanCommand.PromptTarget<LufsTarget>(
            console, "Target integrated loudness:", -16.0, LufsTarget.TryCreate);

        Assert.Equal(-18.5, target.Value);
    }

    [Fact]
    public void PromptTarget_reprompts_after_an_out_of_range_value_then_returns_the_valid_one()
    {
        var console = new TestConsole();
        console.Input.PushTextWithEnter("-100");
        console.Input.PushTextWithEnter("-20");

        var target = CleanCommand.PromptTarget<LufsTarget>(
            console, "Target integrated loudness:", -16.0, LufsTarget.TryCreate);

        Assert.Equal(-20.0, target.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PromptTarget_calls_tryCreate_exactly_once_for_the_accepted_value(bool acceptDefault)
    {
        var console = new TestConsole();
        if (acceptDefault)
            console.Input.PushKey(ConsoleKey.Enter);
        else
            console.Input.PushTextWithEnter("-18.5");

        var callCount = 0;
        bool CountingTryCreate(double value, out LufsTarget target, out string? error)
        {
            callCount++;
            return LufsTarget.TryCreate(value, out target, out error);
        }

        CleanCommand.PromptTarget<LufsTarget>(console, "Target integrated loudness:", -16.0, CountingTryCreate);

        Assert.Equal(1, callCount);
    }
}
