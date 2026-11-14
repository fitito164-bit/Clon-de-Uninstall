using System.Globalization;
using Quitar.Core.Removal;

namespace Quitar.Desktop;

/// <summary>Una corrida del historial con sus textos listos para mostrar.</summary>
public sealed class HistoryRow
{
    public HistoryRow(RemovalRun run) => Run = run;

    public RemovalRun Run { get; }

    public string WhenText => Run.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public string ProgramText => string.IsNullOrEmpty(Run.ProgramVersion) ? Run.ProgramName : $"{Run.ProgramName} {Run.ProgramVersion}";

    public string StatusText => Run.Status switch
    {
        RunStatus.Uninstalling => Strings.StatusUninstalling,
        RunStatus.Reviewing => Strings.StatusReviewing,
        RunStatus.Deleting => Strings.StatusDeleting,
        RunStatus.Done => Strings.StatusDone,
        RunStatus.Cancelled => Strings.StatusCancelled,
        _ => Strings.StatusFailed,
    };

    public int PendingBackups => Run.Backups.Count(b => b.RestoredAt is null);

    public string BackupsText => $"{Run.Backups.Count - PendingBackups} / {Run.Backups.Count} restaurados";

    public string NoteText => Run.Note ?? "";
}
