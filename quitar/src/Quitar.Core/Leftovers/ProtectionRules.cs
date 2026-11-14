using System.Text.RegularExpressions;
using Quitar.Core.Programs;
using Quitar.Core.Registry;

namespace Quitar.Core.Leftovers;

/// <summary>
/// Lo que jamás se ofrece ni se borra. Se aplica dos veces: al escanear y justo antes de borrar,
/// así que una lista de restos alterada tampoco puede llevarse algo del sistema.
/// </summary>
public sealed partial class ProtectionRules
{
    /// <summary>Claves directas de Software que no son de ningún programa.</summary>
    private static readonly HashSet<string> SystemSoftwareKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft", "Windows", "Classes", "Policies", "WOW6432Node", "Clients",
        "RegisteredApplications", "ODBC", "Wow6432Node",
    };

    [GeneratedRegex(@"^SOFTWARE\\(?:WOW6432Node\\)?Microsoft\\Windows\\CurrentVersion\\Uninstall\\[^\\]+$", RegexOptions.IgnoreCase)]
    private static partial Regex UninstallEntryShape();

    [GeneratedRegex(@"^SOFTWARE\\(?:WOW6432Node\\)?(?<a>[^\\]+)(?:\\(?<b>[^\\]+))?$", RegexOptions.IgnoreCase)]
    private static partial Regex SoftwareShape();

    private readonly string[] _guarded;
    private readonly string[] _forbidden;

    /// <param name="guardedFolders">Carpetas que no se pueden borrar (ni ellas ni sus ancestros), pero sí lo que hay dentro.</param>
    /// <param name="forbiddenTrees">Carpetas donde no se puede borrar nada, ni ellas ni nada de lo que contienen.</param>
    public ProtectionRules(IEnumerable<string> guardedFolders, IEnumerable<string> forbiddenTrees)
    {
        _guarded = Normalize(guardedFolders);
        _forbidden = Normalize(forbiddenTrees);
    }

    public static ProtectionRules FromEnvironment(string appDataRoot)
    {
        string Get(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);

        var localAppData = Get(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Get(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Get(Environment.SpecialFolder.ProgramFilesX86);
        var programData = Get(Environment.SpecialFolder.CommonApplicationData);
        var windows = Get(Environment.SpecialFolder.Windows);

        string[] guarded =
        [
            programFiles, programFilesX86, programData, localAppData,
            Combine(localAppData, "Programs"),
            Get(Environment.SpecialFolder.ApplicationData),
            Get(Environment.SpecialFolder.UserProfile),
            Get(Environment.SpecialFolder.DesktopDirectory),
            Get(Environment.SpecialFolder.CommonDesktopDirectory),
            Get(Environment.SpecialFolder.MyDocuments),
            Get(Environment.SpecialFolder.MyPictures),
            Get(Environment.SpecialFolder.MyMusic),
            Get(Environment.SpecialFolder.MyVideos),
            Get(Environment.SpecialFolder.StartMenu),
            Get(Environment.SpecialFolder.CommonStartMenu),
            Get(Environment.SpecialFolder.Programs),
            Get(Environment.SpecialFolder.CommonPrograms),
            Get(Environment.SpecialFolder.CommonProgramFiles),
            Get(Environment.SpecialFolder.CommonProgramFilesX86),
        ];

        string[] forbidden =
        [
            windows,
            Combine(programFiles, "WindowsApps"),
            Combine(programFiles, "Common Files"),
            Combine(programFilesX86, "Common Files"),
            Combine(programData, "Microsoft"),
            Combine(localAppData, "Microsoft"),
            appDataRoot,
        ];

        return new ProtectionRules(guarded, forbidden);
    }

    public bool IsProtected(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            return true;
        }

        var full = Trim(Path.GetFullPath(path));

        var root = Path.GetPathRoot(full);
        if (root is not null && Trim(root).Equals(full, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var guarded in _guarded)
        {
            // La carpeta resguardada y cualquiera de sus ancestros (C:\Users, la raíz...) no se tocan.
            if (full.Equals(guarded, StringComparison.OrdinalIgnoreCase) || IsInside(full, guarded))
            {
                return true;
            }
        }

        foreach (var tree in _forbidden)
        {
            // El árbol prohibido, todo lo que hay dentro y sus ancestros.
            if (full.Equals(tree, StringComparison.OrdinalIgnoreCase) || IsInside(tree, full) || IsInside(full, tree))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Solo se pueden tocar tres formas de clave: <c>Software\X</c>, <c>Software\X\Y</c> (con X que no sea del
    /// sistema) y la entrada de un programa en Uninstall. Todo lo demás está protegido.
    /// </summary>
    public bool IsProtected(RegistryKeyRef key)
    {
        var sub = key.SubKey.Trim('\\');
        if (key.Scope == RegistryScope.User && sub.StartsWith(@"Software\", StringComparison.OrdinalIgnoreCase))
        {
            sub = "SOFTWARE" + sub["Software".Length..];
        }

        if (UninstallEntryShape().IsMatch(sub))
        {
            return false;
        }

        var m = SoftwareShape().Match(sub);
        if (!m.Success)
        {
            return true;
        }

        return SystemSoftwareKeys.Contains(m.Groups["a"].Value);
    }

    /// <summary>¿<paramref name="ancestor"/> contiene a <paramref name="path"/> (estrictamente)?</summary>
    private static bool IsInside(string ancestor, string path) =>
        path.Length > ancestor.Length
        && path.StartsWith(ancestor, StringComparison.OrdinalIgnoreCase)
        && (path[ancestor.Length] == Path.DirectorySeparatorChar || path[ancestor.Length] == Path.AltDirectorySeparatorChar);

    private static string[] Normalize(IEnumerable<string> folders) =>
        folders
            .Where(p => !string.IsNullOrWhiteSpace(p) && Path.IsPathFullyQualified(p))
            .Select(p => Trim(Path.GetFullPath(p)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string Trim(string path)
    {
        var root = Path.GetPathRoot(path) ?? "";
        return path.Length > root.Length ? path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : path;
    }

    private static string Combine(string a, string b) => string.IsNullOrEmpty(a) ? "" : Path.Combine(a, b);
}
