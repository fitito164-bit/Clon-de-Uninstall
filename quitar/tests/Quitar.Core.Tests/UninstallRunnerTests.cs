using Xunit;
using Quitar.Core.Programs;
using Quitar.Core.Uninstall;

namespace Quitar.Core.Tests;

public class UninstallRunnerTests
{
    private static readonly UninstallCommand Command = new("uninst.exe", "");
    private static readonly InstalledProgram Program = Entry.Program("Foo");

    private static Task NoDelay(TimeSpan _, CancellationToken __) => Task.CompletedTask;

    [Fact]
    public async Task Entry_gone_right_after_exit_is_completed()
    {
        var host = new FakeHost { OnRun = _ => 0 };
        var present = true;
        host.OnRun = _ => { present = false; return 0; };
        var runner = new UninstallRunner(host, _ => present, delay: NoDelay);

        var outcome = await runner.RunAsync(Program, Command, CancellationToken.None);

        Assert.Equal(UninstallOutcomeKind.Completed, outcome.Kind);
        Assert.Equal(0, outcome.ExitCode);
    }

    [Fact]
    public async Task Keeps_waiting_for_a_launcher_that_exits_early_and_finishes_later()
    {
        // El desinstalador se copia a temp, relanza esa copia y sale: la entrada desaparece segundos después.
        var host = new FakeHost();
        var checks = 0;
        var runner = new UninstallRunner(host, _ => ++checks <= 4, delay: NoDelay);

        var outcome = await runner.RunAsync(Program, Command, CancellationToken.None);

        Assert.Equal(UninstallOutcomeKind.Completed, outcome.Kind);
        Assert.True(checks > 2);
    }

    [Fact]
    public async Task Entry_that_never_disappears_ends_as_still_installed_after_the_grace_period()
    {
        var host = new FakeHost { OnRun = _ => 1 };
        var delays = 0;
        var runner = new UninstallRunner(
            host,
            _ => true,
            grace: TimeSpan.FromSeconds(10),
            poll: TimeSpan.FromSeconds(2),
            delay: (_, _) => { delays++; return Task.CompletedTask; });

        var outcome = await runner.RunAsync(Program, Command, CancellationToken.None);

        Assert.Equal(UninstallOutcomeKind.StillInstalled, outcome.Kind);
        Assert.Equal(1, outcome.ExitCode);
        Assert.Equal(5, delays);
        Assert.Contains("código 1", outcome.Message);
    }

    [Fact]
    public async Task Launch_failure_is_reported_with_the_reason()
    {
        var host = new FakeHost { Throw = new InvalidOperationException("no se encontró el archivo") };
        var runner = new UninstallRunner(host, _ => true, delay: NoDelay);

        var outcome = await runner.RunAsync(Program, Command, CancellationToken.None);

        Assert.Equal(UninstallOutcomeKind.LaunchFailed, outcome.Kind);
        Assert.Contains("no se encontró", outcome.Message);
    }

    [Fact]
    public async Task Stop_waiting_looks_at_the_registry_once_and_does_not_hang()
    {
        var host = new FakeHost();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var runner = new UninstallRunner(host, _ => true);

        var outcome = await runner.RunAsync(Program, Command, cts.Token);

        Assert.Equal(UninstallOutcomeKind.StillInstalled, outcome.Kind);
    }
}
