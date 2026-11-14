using Quitar.Core.Programs;
using Quitar.Core.Registry;

namespace Quitar.Core.Leftovers;

/// <summary>
/// Busca lo que un programa dejó atrás: su carpeta de instalación, carpetas y claves con su nombre,
/// y accesos directos. Solo lee. Todo lo que devuelve existe en este momento y pasó las reglas de protección.
/// </summary>
public sealed class LeftoverScanner
{
    private readonly IRegistryEditor _registry;
    private readonly ScanLocations _locations;
    private readonly ProtectionRules _rules;

    public LeftoverScanner(IRegistryEditor registry, ScanLocations locations, ProtectionRules rules)
    {
        _registry = registry;
        _locations = locations;
        _rules = rules;
    }

    public IReadOnlyList<LeftoverItem> Scan(InstalledProgram program, CancellationToken cancellationToken = default)
    {
        var found = new List<LeftoverItem>();

        var nameKey = NameMatcher.Squash(program.Name);
        var publisherKey = NameMatcher.Squash(program.Publisher);

        // Los componentes de Microsoft comparten nombres con carpetas del sistema: solo se mira su carpeta de instalación.
        var isMicrosoft = program.Publisher?.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) == true;
        var byName = NameMatcher.IsUsable(nameKey) && !isMicrosoft;
        var byPublisher = byName && NameMatcher.IsUsable(publisherKey);

        AddInstallLocation(program, found);
        cancellationToken.ThrowIfCancellationRequested();

        if (byName)
        {
            AddNamedFolders(nameKey, byPublisher ? publisherKey : null, found, cancellationToken);
            AddShortcuts(nameKey, found, cancellationToken);
            AddRegistryKeys(nameKey, byPublisher ? publisherKey : null, found, cancellationToken);
        }

        return Finish(found);
    }

    private void AddInstallLocation(InstalledProgram program, List<LeftoverItem> found)
    {
        var location = program.InstallLocation?.Trim().Trim('"');
        if (string.IsNullOrEmpty(location) || !Path.IsPathFullyQualified(location))
        {
            return;
        }

        var full = Path.GetFullPath(location).TrimEnd('\\', '/');
        if (!Directory.Exists(full) || _rules.IsProtected(full) || IsReparsePoint(full))
        {
            return;
        }

        var inInstallRoot = _locations.InstallRoots.Any(r => IsUnder(r, full));
        var risk = inInstallRoot
            ? LeftoverRisk.Low
            : Path.GetDirectoryName(full) is { } parent && Path.GetPathRoot(full)?.TrimEnd('\\', '/') is { } root
                && parent.TrimEnd('\\', '/').Equals(root, StringComparison.OrdinalIgnoreCase)
                    ? LeftoverRisk.High // directamente bajo la raíz de un disco: puede ser una carpeta compartida
                    : LeftoverRisk.Medium;

        found.Add(new LeftoverItem
        {
            Kind = LeftoverKind.Folder,
            Path = full,
            Risk = risk,
            Reason = "Carpeta de instalación que el programa registró",
        });
    }

    private void AddNamedFolders(string nameKey, string? publisherKey, List<LeftoverItem> found, CancellationToken ct)
    {
        foreach (var folder in _locations.Folders)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var dir in SafeDirectories(folder.Path))
            {
                var key = NameMatcher.Squash(System.IO.Path.GetFileName(dir));
                if (key == nameKey)
                {
                    AddFolder(dir, folder.Risk, folder.Reason, found);
                }
                else if (publisherKey is not null && key == publisherKey)
                {
                    // Editor\Programa: se ofrece solo la carpeta del programa, nunca la del editor.
                    foreach (var inner in SafeDirectories(dir))
                    {
                        if (NameMatcher.Squash(System.IO.Path.GetFileName(inner)) == nameKey)
                        {
                            AddFolder(inner, folder.Risk, folder.Reason, found);
                        }
                    }
                }
            }
        }
    }

    private void AddShortcuts(string nameKey, List<LeftoverItem> found, CancellationToken ct)
    {
        foreach (var start in _locations.StartMenuPrograms)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var dir in SafeDirectories(start))
            {
                if (NameMatcher.Squash(System.IO.Path.GetFileName(dir)) == nameKey)
                {
                    AddFolder(dir, LeftoverRisk.Low, "Carpeta del menú Inicio con el nombre del programa", found);
                }
            }

            AddShortcutFiles(start, nameKey, found);
        }

        foreach (var desktop in _locations.DesktopFolders)
        {
            AddShortcutFiles(desktop, nameKey, found);
        }
    }

    private void AddShortcutFiles(string folder, string nameKey, List<LeftoverItem> found)
    {
        foreach (var file in SafeFiles(folder, "*.lnk"))
        {
            if (NameMatcher.Squash(System.IO.Path.GetFileNameWithoutExtension(file)) == nameKey && !_rules.IsProtected(file))
            {
                found.Add(new LeftoverItem
                {
                    Kind = LeftoverKind.Shortcut,
                    Path = file,
                    Risk = LeftoverRisk.Low,
                    Reason = "Acceso directo con el nombre del programa",
                });
            }
        }
    }

    private void AddRegistryKeys(string nameKey, string? publisherKey, List<LeftoverItem> found, CancellationToken ct)
    {
        (RegistryScope Scope, string Prefix)[] prefixes =
        [
            (RegistryScope.Machine, "SOFTWARE"),
            (RegistryScope.Machine, @"SOFTWARE\WOW6432Node"),
            (RegistryScope.User, "Software"),
        ];

        foreach (var (scope, prefix) in prefixes)
        {
            ct.ThrowIfCancellationRequested();
            var parent = new RegistryKeyRef(scope, prefix);
            foreach (var name in _registry.GetSubKeyNames(parent))
            {
                var key = NameMatcher.Squash(name);
                if (key == nameKey)
                {
                    AddRegistryKey(parent.Child(name), "Clave de configuración con el nombre del programa", found);
                }
                else if (publisherKey is not null && key == publisherKey)
                {
                    var publisher = parent.Child(name);
                    foreach (var inner in _registry.GetSubKeyNames(publisher))
                    {
                        if (NameMatcher.Squash(inner) == nameKey)
                        {
                            AddRegistryKey(publisher.Child(inner), "Clave de configuración del programa bajo la de su editor", found);
                        }
                    }
                }
            }
        }
    }

    private void AddRegistryKey(RegistryKeyRef key, string reason, List<LeftoverItem> found)
    {
        if (_rules.IsProtected(key) || !_registry.KeyExists(key))
        {
            return;
        }

        found.Add(new LeftoverItem
        {
            Kind = LeftoverKind.RegistryKey,
            Path = key.ToString(),
            Key = key,
            Risk = LeftoverRisk.Low,
            Reason = reason,
        });
    }

    private void AddFolder(string dir, LeftoverRisk risk, string reason, List<LeftoverItem> found)
    {
        if (_rules.IsProtected(dir) || IsReparsePoint(dir))
        {
            return;
        }

        found.Add(new LeftoverItem { Kind = LeftoverKind.Folder, Path = dir, Risk = risk, Reason = reason });
    }

    /// <summary>Quita duplicados y lo que ya queda dentro de otra carpeta encontrada, y fija qué llega marcado.</summary>
    private static IReadOnlyList<LeftoverItem> Finish(List<LeftoverItem> found)
    {
        var unique = new List<LeftoverItem>();
        foreach (var item in found)
        {
            if (!unique.Any(u => u.Kind == item.Kind && u.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)))
            {
                unique.Add(item);
            }
        }

        var folders = unique.Where(u => u.Kind == LeftoverKind.Folder).Select(u => u.Path).ToList();
        var result = unique
            .Where(u => u.Kind is LeftoverKind.RegistryKey
                || !folders.Any(f => IsUnder(f, u.Path)))
            .OrderBy(u => u.Kind == LeftoverKind.RegistryKey ? 1 : 0)
            .ThenBy(u => u.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var item in result)
        {
            item.Selected = item.Risk != LeftoverRisk.High;
        }

        return result;
    }

    private static bool IsUnder(string folder, string path)
    {
        var f = folder.TrimEnd('\\', '/');
        return path.Length > f.Length
            && path.StartsWith(f, StringComparison.OrdinalIgnoreCase)
            && (path[f.Length] == '\\' || path[f.Length] == '/');
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return true; // si no se puede ni leer, no se toca
        }
    }

    private static IEnumerable<string> SafeDirectories(string path)
    {
        try
        {
            return Directory.Exists(path) ? Directory.GetDirectories(path) : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<string> SafeFiles(string path, string pattern)
    {
        try
        {
            return Directory.Exists(path) ? Directory.GetFiles(path, pattern) : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
