using System.Text.Json.Serialization;
using Quitar.Core.Programs;

namespace Quitar.Core.Registry;

/// <summary>
/// Una clave del registro, siempre expresada en la vista de 64 bits: las claves de 32 bits
/// se escriben con su ruta real (<c>SOFTWARE\WOW6432Node\...</c>). Así una ruta significa una sola cosa.
/// </summary>
public sealed record RegistryKeyRef(RegistryScope Scope, string SubKey)
{
    [JsonIgnore]
    public string HiveShort => Scope == RegistryScope.Machine ? "HKLM" : "HKCU";

    /// <summary>Ruta tal como la entiende reg.exe, por ejemplo <c>HKLM\SOFTWARE\Foo</c>.</summary>
    [JsonIgnore]
    public string RegExePath => $@"{HiveShort}\{SubKey}";

    public override string ToString() => RegExePath;

    public RegistryKeyRef Child(string name) => this with { SubKey = $@"{SubKey}\{name}" };

    /// <summary>La subclave de Uninstall de un programa.</summary>
    public static RegistryKeyRef ForUninstallEntry(RegistryScope scope, ProgramArchitecture architecture, string keyName)
    {
        var wow = scope == RegistryScope.Machine && architecture == ProgramArchitecture.X86 ? @"WOW6432Node\" : "";
        return new RegistryKeyRef(scope, $@"SOFTWARE\{wow}Microsoft\Windows\CurrentVersion\Uninstall\{keyName}");
    }

    public static RegistryKeyRef ForUninstallEntry(InstalledProgram program) =>
        ForUninstallEntry(program.Scope, program.Architecture, program.KeyName);
}
