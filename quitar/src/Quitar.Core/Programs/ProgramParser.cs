using System.Globalization;
using System.Text.RegularExpressions;
using Quitar.Core.Registry;

namespace Quitar.Core.Programs;

/// <summary>Convierte una entrada cruda del registro en un <see cref="InstalledProgram"/>.</summary>
public static partial class ProgramParser
{
    private static readonly string[] DateFormats = ["yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd"];

    private static readonly string[] MicrosoftComponentWords =
        ["Redistributable", "Runtime", "Visual C++", "Windows Software Development Kit"];

    [GeneratedRegex(@"\bKB\d{6,8}\b", RegexOptions.IgnoreCase)]
    private static partial Regex KbNumber();

    [GeneratedRegex(@"^(security update for|update for|hotfix for|cumulative update for|service pack)\b", RegexOptions.IgnoreCase)]
    private static partial Regex UpdatePrefix();

    [GeneratedRegex(@"^\{[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\}$", RegexOptions.IgnoreCase)]
    private static partial Regex GuidKey();

    /// <returns>El programa, o null si Windows no lo mostraría (sin nombre).</returns>
    public static InstalledProgram? TryParse(UninstallEntry entry)
    {
        var v = entry.Values;

        var name = GetString(v, "DisplayName");
        if (name is null)
        {
            return null;
        }

        var publisher = GetString(v, "Publisher");
        var parentKey = GetString(v, "ParentKeyName");
        var parentDisplay = GetString(v, "ParentDisplayName");
        var releaseType = GetString(v, "ReleaseType");

        var isUpdate =
            parentKey is not null
            || parentDisplay is not null
            || IsUpdateRelease(releaseType)
            || KbNumber().IsMatch(name)
            || UpdatePrefix().IsMatch(name);

        var systemComponent = GetLong(v, "SystemComponent") == 1;

        return new InstalledProgram
        {
            Id = $"{entry.Scope}|{entry.Architecture}|{entry.KeyName}",
            KeyName = entry.KeyName,
            Name = name,
            Publisher = publisher,
            Version = GetString(v, "DisplayVersion"),
            InstallDate = ParseDate(GetString(v, "InstallDate")),
            SizeBytes = ParseSize(GetLong(v, "EstimatedSize")),
            InstallLocation = GetString(v, "InstallLocation"),
            UninstallString = GetString(v, "UninstallString"),
            QuietUninstallString = GetString(v, "QuietUninstallString"),
            Architecture = entry.Architecture,
            Scope = entry.Scope,
            Group = Classify(systemComponent, isUpdate, publisher, name),
            IsUpdate = isUpdate,
            IsWindowsInstaller = GetLong(v, "WindowsInstaller") == 1 || GuidKey().IsMatch(entry.KeyName),
            ParentKeyName = parentKey,
        };
    }

    private static ProgramGroup Classify(bool systemComponent, bool isUpdate, string? publisher, string name)
    {
        if (systemComponent)
        {
            return ProgramGroup.Hidden;
        }

        if (isUpdate || IsMicrosoftComponent(publisher, name))
        {
            return ProgramGroup.System;
        }

        return ProgramGroup.Apps;
    }

    private static bool IsMicrosoftComponent(string? publisher, string name) =>
        publisher is not null
        && publisher.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
        && MicrosoftComponentWords.Any(w => name.Contains(w, StringComparison.OrdinalIgnoreCase));

    private static bool IsUpdateRelease(string? releaseType) =>
        releaseType is not null
        && (releaseType.Contains("update", StringComparison.OrdinalIgnoreCase)
            || releaseType.Contains("hotfix", StringComparison.OrdinalIgnoreCase)
            || releaseType.Contains("service pack", StringComparison.OrdinalIgnoreCase));

    private static DateOnly? ParseDate(string? text) =>
        text is not null
        && DateOnly.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;

    /// <summary>EstimatedSize viene en KB; 0 o ausente significa "se desconoce".</summary>
    private static long? ParseSize(long? kilobytes) =>
        kilobytes is > 0 ? kilobytes * 1024 : null;

    private static string? GetString(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out var raw) || raw is null)
        {
            return null;
        }

        var text = raw switch
        {
            string s => s,
            string[] many => string.Join(" ", many),
            _ => Convert.ToString(raw, CultureInfo.InvariantCulture),
        };

        text = text?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static long? GetLong(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            int i => i,
            uint u => u,
            long l => l,
            string s when long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }
}
