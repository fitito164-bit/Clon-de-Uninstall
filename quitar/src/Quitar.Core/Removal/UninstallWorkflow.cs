using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Registry;
using Quitar.Core.Uninstall;

namespace Quitar.Core.Removal;

public enum PrepareStatus
{
    /// <summary>Se desinstaló y hay restos para revisar.</summary>
    ReadyToReview,

    /// <summary>Se desinstaló y no quedó nada que limpiar.</summary>
    NothingLeft,

    /// <summary>Faltó el respaldo: no se llegó a lanzar el desinstalador.</summary>
    BackupFailed,

    NoUninstaller,

    LaunchFailed,

    /// <summary>El programa sigue instalado: se canceló o falló el desinstalador.</summary>
    StillInstalled,
}

public sealed record PrepareResult(PrepareStatus Status, RemovalRun Run, string? Message = null);

/// <summary>
/// El camino completo de «desinstalar con respaldo»: respalda la entrada del programa, ejecuta su
/// desinstalador, escanea los restos y deja la corrida lista para revisar. Borrar los restos es otro
/// paso (<see cref="RemovalService"/>) porque antes tiene que decidir el usuario.
/// </summary>
public sealed class UninstallWorkflow
{
    private readonly UninstallRunner _runner;
    private readonly LeftoverScanner _scanner;
    private readonly RegistryBackupService _backups;
    private readonly IRegistryEditor _registry;
    private readonly IRunStore _store;

    public UninstallWorkflow(
        UninstallRunner runner,
        LeftoverScanner scanner,
        RegistryBackupService backups,
        IRegistryEditor registry,
        IRunStore store)
    {
        _runner = runner;
        _scanner = scanner;
        _backups = backups;
        _registry = registry;
        _store = store;
    }

    /// <param name="stopWaiting">Deja de esperar al desinstalador (no lo cierra).</param>
    public async Task<PrepareResult> PrepareAsync(
        InstalledProgram program,
        IProgress<string>? progress = null,
        CancellationToken stopWaiting = default)
    {
        var run = RemovalRun.For(program);
        _store.Save(run);

        var command = UninstallCommandParser.For(program);
        if (command is null)
        {
            run.AddLog(LogOperation.RunUninstaller, program.Name, LogResult.Failed, "El programa no tiene desinstalador registrado.");
            run.Finish(RunStatus.Failed, "El programa no tiene desinstalador registrado.");
            _store.Save(run);
            return new PrepareResult(PrepareStatus.NoUninstaller, run, run.Note);
        }

        // 1) Respaldo de la entrada del programa: sin él no se desinstala.
        var entry = RegistryKeyRef.ForUninstallEntry(program);
        if (_registry.KeyExists(entry))
        {
            progress?.Report("Guardando una copia de la entrada del programa en el registro…");
            try
            {
                _backups.Backup(run, entry);
            }
            catch (BackupFailedException e)
            {
                run.Finish(RunStatus.Failed, "No se desinstaló: " + e.Message);
                _store.Save(run);
                return new PrepareResult(PrepareStatus.BackupFailed, run, run.Note);
            }

            _store.Save(run);
        }

        // 2) El desinstalador del propio programa.
        progress?.Report("Ejecutando el desinstalador del programa…");
        var outcome = await _runner.RunAsync(program, command, stopWaiting, progress).ConfigureAwait(false);
        run.UninstallExitCode = outcome.ExitCode;
        run.AddLog(
            LogOperation.RunUninstaller,
            $"{command.FileName} {command.Arguments}".Trim(),
            outcome.Kind == UninstallOutcomeKind.Completed ? LogResult.Ok : LogResult.Failed,
            outcome.Message ?? (outcome.ExitCode is { } c ? $"Código de salida {c}" : null));

        switch (outcome.Kind)
        {
            case UninstallOutcomeKind.LaunchFailed:
                run.Finish(RunStatus.Failed, "No se pudo iniciar el desinstalador: " + outcome.Message);
                _store.Save(run);
                return new PrepareResult(PrepareStatus.LaunchFailed, run, run.Note);

            case UninstallOutcomeKind.StillInstalled:
                run.Finish(RunStatus.Cancelled, outcome.Message);
                _store.Save(run);
                return new PrepareResult(PrepareStatus.StillInstalled, run, outcome.Message);
        }

        // 3) Restos.
        progress?.Report("Buscando restos del programa…");
        var items = await Task.Run(() => _scanner.Scan(program)).ConfigureAwait(false);
        run.Items.AddRange(items);
        run.AddLog(LogOperation.Scan, program.Name, LogResult.Ok, $"{items.Count} elemento(s) encontrados.");

        if (items.Count == 0)
        {
            run.Finish(RunStatus.Done, "El programa se desinstaló y no quedaron restos.");
            _store.Save(run);
            return new PrepareResult(PrepareStatus.NothingLeft, run, run.Note);
        }

        run.Status = RunStatus.Reviewing;
        _store.Save(run);
        return new PrepareResult(PrepareStatus.ReadyToReview, run);
    }
}
