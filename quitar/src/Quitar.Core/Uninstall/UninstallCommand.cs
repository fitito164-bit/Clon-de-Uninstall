using System.Text.RegularExpressions;
using Quitar.Core.Programs;

namespace Quitar.Core.Uninstall;

/// <summary>El desinstalador de un programa separado en ejecutable y argumentos.</summary>
public sealed record UninstallCommand(string FileName, string Arguments);

/// <summary>
/// Interpreta la cadena UninstallString del registro: con comillas, sin comillas y con espacios,
/// con variables de entorno (%systemroot%) y los MSI que guardan «/I{código}» en vez de «/X{código}».
/// </summary>
public static partial class UninstallCommandParser
{
    [GeneratedRegex(@"^(?<file>.+?\.(?:exe|bat|cmd|com|msi))(?:\s+(?<args>.*))?$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex UnquotedWithExtension();

    [GeneratedRegex(@"(?<![A-Za-z])/[Ii]\s*(?=\{)")]
    private static partial Regex MsiInstallSwitch();

    [GeneratedRegex(@"^\{[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\}$", RegexOptions.IgnoreCase)]
    private static partial Regex ProductCode();

    /// <summary>El comando para desinstalar el programa, o null si no hay forma de hacerlo.</summary>
    public static UninstallCommand? For(InstalledProgram program, Func<string, string>? expand = null)
    {
        var parsed = Parse(program.UninstallString, expand);
        if (parsed is not null)
        {
            return parsed;
        }

        // Sin cadena de desinstalación pero con código de producto: Windows Installer sabe quitarlo.
        if (program.IsWindowsInstaller && ProductCode().IsMatch(program.KeyName))
        {
            return new UninstallCommand("msiexec.exe", $"/X{program.KeyName}");
        }

        return null;
    }

    public static UninstallCommand? Parse(string? raw, Func<string, string>? expand = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        expand ??= Environment.ExpandEnvironmentVariables;
        var text = expand(raw.Trim());

        string file;
        string args;

        if (text.StartsWith('"'))
        {
            var close = text.IndexOf('"', 1);
            if (close < 0)
            {
                return null;
            }

            file = text[1..close];
            args = text[(close + 1)..].Trim();
        }
        else if (UnquotedWithExtension().Match(text) is { Success: true } m)
        {
            file = m.Groups["file"].Value;
            args = m.Groups["args"].Value.Trim();
        }
        else
        {
            var space = text.IndexOfAny([' ', '\t']);
            file = space < 0 ? text : text[..space];
            args = space < 0 ? "" : text[(space + 1)..].Trim();
        }

        if (file.Length == 0)
        {
            return null;
        }

        if (IsMsiExec(file))
        {
            args = MsiInstallSwitch().Replace(args, "/X");
        }

        return new UninstallCommand(file, args);
    }

    private static bool IsMsiExec(string file)
    {
        var name = file.Replace('/', '\\');
        name = name[(name.LastIndexOf('\\') + 1)..];
        return name.Equals("msiexec", StringComparison.OrdinalIgnoreCase)
            || name.Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase);
    }
}
