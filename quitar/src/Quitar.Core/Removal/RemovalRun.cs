using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Registry;

namespace Quitar.Core.Removal;

public enum RunStatus
{
    Uninstalling,
    Reviewing,
    Deleting,
    Done,
    Cancelled,
    Failed,
}

/// <summary>Una clave del registro exportada a .reg antes de tocarla.</summary>
public sealed class BackupRecord
{
    public RegistryKeyRef Key { get; set; } = new(RegistryScope.Machine, "");

    public string RegFile { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RestoredAt { get; set; }
}

/// <summary>Una línea del registro de operaciones. Solo se agregan, nunca se editan.</summary>
public sealed class LogEntry
{
    public DateTimeOffset Time { get; set; }

    public string Operation { get; set; } = "";

    public string Target { get; set; } = "";

    /// <summary>ok, failed o skipped.</summary>
    public string Result { get; set; } = "";

    public string? Message { get; set; }
}

public static class LogOperation
{
    public const string BackupRegistry = "backup_registry";
    public const string RestoreRegistry = "restore_registry";
    public const string RunUninstaller = "run_uninstaller";
    public const string Scan = "scan";
    public const string DeleteFolder = "delete_folder";
    public const string DeleteFile = "delete_file";
    public const string DeleteRegistryKey = "delete_reg_key";
}

public static class LogResult
{
    public const string Ok = "ok";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}

/// <summary>Todo lo que pasó al quitar un programa: se guarda como un archivo JSON por corrida.</summary>
public sealed class RemovalRun
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ProgramKeyName { get; set; } = "";

    public string ProgramName { get; set; } = "";

    public string? ProgramVersion { get; set; }

    public string? Publisher { get; set; }

    public RegistryScope Scope { get; set; }

    public ProgramArchitecture Architecture { get; set; }

    /// <summary>En este hito solo existe «uninstall»; quedan «force», «batch_force» y «remove_entry» para los siguientes.</summary>
    public string Mode { get; set; } = "uninstall";

    public RunStatus Status { get; set; } = RunStatus.Uninstalling;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EndedAt { get; set; }

    public int? UninstallExitCode { get; set; }

    /// <summary>Explicación breve del estado final, para mostrar en el historial.</summary>
    public string? Note { get; set; }

    public List<LeftoverItem> Items { get; set; } = [];

    public List<BackupRecord> Backups { get; set; } = [];

    public List<LogEntry> Log { get; set; } = [];

    public static RemovalRun For(InstalledProgram program) => new()
    {
        ProgramKeyName = program.KeyName,
        ProgramName = program.Name,
        ProgramVersion = program.Version,
        Publisher = program.Publisher,
        Scope = program.Scope,
        Architecture = program.Architecture,
    };

    public void AddLog(string operation, string target, string result, string? message = null) =>
        Log.Add(new LogEntry
        {
            Time = DateTimeOffset.UtcNow,
            Operation = operation,
            Target = target,
            Result = result,
            Message = message,
        });

    public void Finish(RunStatus status, string? note = null)
    {
        Status = status;
        Note = note ?? Note;
        EndedAt = DateTimeOffset.UtcNow;
    }
}
