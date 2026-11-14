using Quitar.Core.Programs;

namespace Quitar.Core.Uninstall;

public enum UninstallOutcomeKind
{
    /// <summary>El programa ya no figura en el registro.</summary>
    Completed,

    /// <summary>El desinstalador terminó (o se dejó de esperar) y el programa sigue en el registro.</summary>
    StillInstalled,

    NoCommand,

    LaunchFailed,
}

public sealed record UninstallOutcome(UninstallOutcomeKind Kind, int? ExitCode = null, string? Message = null);

/// <summary>
/// Lanza el desinstalador y espera. Muchos desinstaladores se copian a una carpeta temporal, relanzan esa
/// copia y salen enseguida; por eso, cuando el proceso termina se sigue esperando hasta que la entrada
/// del programa desaparezca del registro (o se agote el margen, o el usuario deje de esperar).
/// </summary>
public sealed class UninstallRunner
{
    public static readonly TimeSpan DefaultGrace = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan DefaultPoll = TimeSpan.FromSeconds(2);

    private readonly IUninstallerHost _host;
    private readonly Func<InstalledProgram, bool> _entryExists;
    private readonly TimeSpan _grace;
    private readonly TimeSpan _poll;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public UninstallRunner(
        IUninstallerHost host,
        Func<InstalledProgram, bool> entryExists,
        TimeSpan? grace = null,
        TimeSpan? poll = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _host = host;
        _entryExists = entryExists;
        _grace = grace ?? DefaultGrace;
        _poll = poll ?? DefaultPoll;
        _delay = delay ?? Task.Delay;
    }

    public async Task<UninstallOutcome> RunAsync(
        InstalledProgram program,
        UninstallCommand command,
        CancellationToken stopWaiting,
        IProgress<string>? progress = null)
    {
        int? exitCode = null;
        try
        {
            exitCode = await _host.RunAsync(command, stopWaiting).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // El usuario dejó de esperar: se mira cómo quedó el registro y se sigue.
        }
        catch (Exception e)
        {
            return new UninstallOutcome(UninstallOutcomeKind.LaunchFailed, null, e.Message);
        }

        var waited = TimeSpan.Zero;
        while (!stopWaiting.IsCancellationRequested && _entryExists(program) && waited < _grace)
        {
            progress?.Report("Esperando a que el desinstalador termine…");
            try
            {
                await _delay(_poll, stopWaiting).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            waited += _poll;
        }

        if (_entryExists(program))
        {
            var detail = exitCode is { } code
                ? $"El desinstalador terminó (código {code}) pero el programa sigue en el registro."
                : "El programa sigue en el registro.";
            return new UninstallOutcome(UninstallOutcomeKind.StillInstalled, exitCode, detail);
        }

        return new UninstallOutcome(UninstallOutcomeKind.Completed, exitCode);
    }
}
