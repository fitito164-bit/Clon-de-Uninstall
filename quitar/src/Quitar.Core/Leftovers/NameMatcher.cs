using System.Text;
using System.Text.RegularExpressions;
using Quitar.Core.Text;

namespace Quitar.Core.Leftovers;

/// <summary>
/// Compara nombres de programas con nombres de carpetas y claves. Es deliberadamente estricto:
/// los nombres tienen que ser iguales una vez quitados versión, arquitectura, signos y espacios.
/// Preferimos dejar un resto antes que borrar algo ajeno.
/// </summary>
public static partial class NameMatcher
{
    private static readonly HashSet<string> Generic = new(StringComparer.Ordinal)
    {
        "setup", "update", "updater", "installer", "install", "windows", "microsoft", "app", "apps",
        "tool", "tools", "driver", "drivers", "runtime", "common", "shared", "system", "software",
        "program", "programs", "data", "files", "temp", "games", "office", "service", "services",
        "client", "server", "library", "framework", "launcher", "cache", "logs", "config",
    };

    [GeneratedRegex(@"\([^)]*\)|\[[^\]]*\]")]
    private static partial Regex Groups();

    [GeneratedRegex(@"\b(?:x64|x86|amd64|win64|win32|64-?bit|32-?bit)\b")]
    private static partial Regex Architecture();

    [GeneratedRegex(@"\bv?\d+(?:\.\d+)+\b|\bv\d+\b")]
    private static partial Regex Version();

    /// <summary>Forma comparable de un nombre: sin tildes, versión ni arquitectura, solo letras y dígitos.</summary>
    public static string Squash(string? text)
    {
        var s = TextFolding.Fold(text);
        s = Groups().Replace(s, " ");
        s = Architecture().Replace(s, " ");
        s = Version().Replace(s, " ");

        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>¿Es un nombre lo bastante específico como para buscar carpetas y claves con él?</summary>
    public static bool IsUsable(string squashed) => squashed.Length >= 4 && !Generic.Contains(squashed);
}
