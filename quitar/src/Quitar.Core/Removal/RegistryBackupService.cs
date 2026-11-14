using Quitar.Core.Registry;

namespace Quitar.Core.Removal;

public sealed class BackupFailedException : Exception
{
    public BackupFailedException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public sealed record RestoreSummary(int Restored, int Failed);

/// <summary>Exporta claves a .reg antes de tocarlas y las vuelve a cargar si el usuario se arrepiente.</summary>
public sealed class RegistryBackupService
{
    private readonly IRegistryEditor _registry;
    private readonly AppPaths _paths;

    public RegistryBackupService(IRegistryEditor registry, AppPaths paths)
    {
        _registry = registry;
        _paths = paths;
    }

    /// <summary>Guarda la clave en un .reg dentro de la carpeta de la corrida y lo registra en ella.</summary>
    /// <exception cref="BackupFailedException">Si no se pudo: en ese caso no hay que tocar la clave.</exception>
    public BackupRecord Backup(RemovalRun run, RegistryKeyRef key)
    {
        try
        {
            var directory = _paths.BackupDirectoryFor(run.Id);
            Directory.CreateDirectory(directory);

            var file = Path.Combine(directory, $"{run.Backups.Count + 1:000}-{SafeLeaf(key.SubKey)}.reg");
            _registry.Export(key, file);

            if (!File.Exists(file) || new FileInfo(file).Length == 0)
            {
                throw new IOException("El archivo de respaldo quedó vacío o no se creó.");
            }

            var record = new BackupRecord { Key = key, RegFile = file, CreatedAt = DateTimeOffset.UtcNow };
            run.Backups.Add(record);
            run.AddLog(LogOperation.BackupRegistry, key.ToString(), LogResult.Ok, file);
            return record;
        }
        catch (Exception e) when (e is not BackupFailedException)
        {
            run.AddLog(LogOperation.BackupRegistry, key.ToString(), LogResult.Failed, e.Message);
            throw new BackupFailedException($"No se pudo respaldar {key}: {e.Message}", e);
        }
    }

    /// <summary>Vuelve a cargar los respaldos de la corrida que no se restauraron todavía, del último al primero.</summary>
    public RestoreSummary Restore(RemovalRun run)
    {
        int restored = 0, failed = 0;
        foreach (var backup in Enumerable.Reverse(run.Backups).Where(b => b.RestoredAt is null))
        {
            try
            {
                _registry.Import(backup.RegFile);
                backup.RestoredAt = DateTimeOffset.UtcNow;
                run.AddLog(LogOperation.RestoreRegistry, backup.Key.ToString(), LogResult.Ok, backup.RegFile);
                restored++;
            }
            catch (Exception e)
            {
                run.AddLog(LogOperation.RestoreRegistry, backup.Key.ToString(), LogResult.Failed, e.Message);
                failed++;
            }
        }

        return new RestoreSummary(restored, failed);
    }

    private static string SafeLeaf(string subKey)
    {
        var leaf = subKey[(subKey.LastIndexOf('\\') + 1)..];
        var chars = leaf.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_').Take(40).ToArray();
        return chars.Length == 0 ? "clave" : new string(chars);
    }
}
