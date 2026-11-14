using Quitar.Core.Leftovers;
using Quitar.Core.Registry;

namespace Quitar.Core.Removal;

public sealed record RemovalSummary(int Deleted, int Failed, int Skipped, bool Cancelled);

/// <summary>
/// Borra los restos marcados. Reglas que no se negocian: una clave de registro solo se borra después de
/// haberla exportado a .reg; los archivos van a la Papelera; nada protegido se toca, aunque esté en la lista.
/// </summary>
public sealed class RemovalService
{
    private readonly IRegistryEditor _registry;
    private readonly IFileRemover _files;
    private readonly ProtectionRules _rules;
    private readonly RegistryBackupService _backups;
    private readonly IRunStore _store;

    public RemovalService(
        IRegistryEditor registry,
        IFileRemover files,
        ProtectionRules rules,
        RegistryBackupService backups,
        IRunStore store)
    {
        _registry = registry;
        _files = files;
        _rules = rules;
        _backups = backups;
        _store = store;
    }

    public RemovalSummary Execute(RemovalRun run, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        int deleted = 0, failed = 0, skipped = 0;
        var cancelled = false;

        run.Status = RunStatus.Deleting;
        _store.Save(run);

        // Primero los archivos y después el registro: si el borrado se corta, lo más delicado queda para el final.
        var pending = run.Items
            .Where(i => i.State == LeftoverState.Found)
            .OrderBy(i => i.Kind == LeftoverKind.RegistryKey ? 1 : 0)
            .ToList();

        foreach (var item in pending)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            if (!item.Selected)
            {
                Mark(run, item, LeftoverState.Skipped, "No estaba marcado.", null);
                skipped++;
                continue;
            }

            progress?.Report(item.Path);
            var state = item.Kind == LeftoverKind.RegistryKey ? DeleteKey(run, item) : DeleteFileOrFolder(run, item);
            switch (state)
            {
                case LeftoverState.Deleted: deleted++; break;
                case LeftoverState.Failed: failed++; break;
                default: skipped++; break;
            }

            _store.Save(run); // si la aplicación se cierra ahora, queda constancia de lo ya hecho
        }

        run.Finish(
            cancelled ? RunStatus.Cancelled : RunStatus.Done,
            cancelled ? "El borrado se canceló a mitad de camino." : null);
        _store.Save(run);

        return new RemovalSummary(deleted, failed, skipped, cancelled);
    }

    private LeftoverState DeleteKey(RemovalRun run, LeftoverItem item)
    {
        var key = item.Key;
        if (key is null || _rules.IsProtected(key))
        {
            return Mark(run, item, LeftoverState.Skipped, "Clave protegida: no se toca.", LogOperation.DeleteRegistryKey);
        }

        if (!_registry.KeyExists(key))
        {
            return Mark(run, item, LeftoverState.Skipped, "Ya no existe.", LogOperation.DeleteRegistryKey);
        }

        try
        {
            _backups.Backup(run, key);
        }
        catch (BackupFailedException e)
        {
            // Sin respaldo no se borra.
            return Mark(run, item, LeftoverState.Failed, "No se borró porque no se pudo respaldar. " + e.Message, LogOperation.DeleteRegistryKey);
        }

        try
        {
            _registry.DeleteKeyTree(key);
            return Mark(run, item, LeftoverState.Deleted, null, LogOperation.DeleteRegistryKey);
        }
        catch (Exception e)
        {
            return Mark(run, item, LeftoverState.Failed, e.Message, LogOperation.DeleteRegistryKey);
        }
    }

    private LeftoverState DeleteFileOrFolder(RemovalRun run, LeftoverItem item)
    {
        var isDirectory = item.Kind == LeftoverKind.Folder;
        var operation = isDirectory ? LogOperation.DeleteFolder : LogOperation.DeleteFile;

        if (_rules.IsProtected(item.Path))
        {
            return Mark(run, item, LeftoverState.Skipped, "Ruta protegida: no se toca.", operation);
        }

        var exists = isDirectory ? Directory.Exists(item.Path) : File.Exists(item.Path);
        if (!exists)
        {
            return Mark(run, item, LeftoverState.Skipped, "Ya no existe.", operation);
        }

        try
        {
            _files.Remove(item.Path, isDirectory);
            return Mark(run, item, LeftoverState.Deleted, "Enviado a la Papelera de reciclaje.", operation);
        }
        catch (Exception e)
        {
            return Mark(run, item, LeftoverState.Failed, e.Message, operation);
        }
    }

    private static LeftoverState Mark(RemovalRun run, LeftoverItem item, LeftoverState state, string? message, string? operation)
    {
        item.State = state;
        item.Message = message;
        if (operation is not null)
        {
            var result = state switch
            {
                LeftoverState.Deleted => LogResult.Ok,
                LeftoverState.Failed => LogResult.Failed,
                _ => LogResult.Skipped,
            };
            run.AddLog(operation, item.Path, result, message);
        }

        return state;
    }
}
