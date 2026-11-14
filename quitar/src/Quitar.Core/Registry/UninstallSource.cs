using Quitar.Core.Programs;

namespace Quitar.Core.Registry;

/// <summary>Una subclave de Uninstall con todos sus valores, sin interpretar.</summary>
public sealed record UninstallEntry(
    RegistryScope Scope,
    ProgramArchitecture Architecture,
    string KeyName,
    IReadOnlyDictionary<string, object?> Values);

/// <summary>
/// Origen de las entradas de desinstalación. Existe para poder probar el lector
/// con datos falsos sin tocar el registro real.
/// </summary>
public interface IUninstallSource
{
    IEnumerable<UninstallEntry> ReadEntries();
}
