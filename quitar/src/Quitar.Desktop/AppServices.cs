using Quitar.Core.Leftovers;
using Quitar.Core.Registry;
using Quitar.Core.Removal;
using Quitar.Core.Uninstall;

namespace Quitar.Desktop;

/// <summary>Arma los servicios del núcleo con las piezas reales de Windows. Es el único lugar que las conoce.</summary>
public sealed class AppServices
{
    public required AppPaths Paths { get; init; }

    public required IRunStore Store { get; init; }

    public required RegistryBackupService Backups { get; init; }

    public required RemovalService Removal { get; init; }

    public required UninstallWorkflow Workflow { get; init; }

    public static AppServices Create()
    {
        var paths = AppPaths.FromEnvironment();
        var registry = new WindowsRegistryEditor();
        var rules = ProtectionRules.FromEnvironment(paths.Root);
        var store = new JsonRunStore(paths.RunsDirectory);
        var backups = new RegistryBackupService(registry, paths);
        var scanner = new LeftoverScanner(registry, ScanLocations.FromEnvironment(), rules);
        var runner = new UninstallRunner(
            new ProcessUninstallerHost(),
            program => registry.KeyExists(RegistryKeyRef.ForUninstallEntry(program)));

        return new AppServices
        {
            Paths = paths,
            Store = store,
            Backups = backups,
            Removal = new RemovalService(registry, new RecycleBinFileRemover(), rules, backups, store),
            Workflow = new UninstallWorkflow(runner, scanner, backups, registry, store),
        };
    }
}
