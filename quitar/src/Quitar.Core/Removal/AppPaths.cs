namespace Quitar.Core.Removal;

/// <summary>Dónde guarda Quitar sus datos: corridas (JSON) y respaldos (.reg), en %LOCALAPPDATA%\Quitar.</summary>
public sealed class AppPaths
{
    public AppPaths(string root) => Root = root;

    public string Root { get; }

    public string RunsDirectory => Path.Combine(Root, "runs");

    public string BackupsDirectory => Path.Combine(Root, "backups");

    public string BackupDirectoryFor(string runId) => Path.Combine(BackupsDirectory, runId);

    public static AppPaths FromEnvironment() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Quitar"));
}
