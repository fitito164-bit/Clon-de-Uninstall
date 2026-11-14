using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using Quitar.Core.Programs;

namespace Quitar.Core.Registry;

/// <summary>
/// Lee el registro real de Windows. Solo lectura: nunca escribe ni borra nada.
/// Recorre las dos vistas (64 y 32 bits) de la máquina y del usuario actual.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsUninstallSource : IUninstallSource
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly (RegistryHive Hive, RegistryView View, RegistryScope Scope, ProgramArchitecture Arch)[] Sources =
    [
        (RegistryHive.LocalMachine, RegistryView.Registry64, RegistryScope.Machine, ProgramArchitecture.X64),
        (RegistryHive.LocalMachine, RegistryView.Registry32, RegistryScope.Machine, ProgramArchitecture.X86),
        // HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall no se redirige entre vistas: leerlo
        // dos veces mostraba cada programa de usuario duplicado.
        (RegistryHive.CurrentUser, RegistryView.Registry64, RegistryScope.User, ProgramArchitecture.X64),
    ];

    public IEnumerable<UninstallEntry> ReadEntries()
    {
        foreach (var (hive, view, scope, arch) in Sources)
        {
            using var uninstall = TryOpenUninstall(hive, view);
            if (uninstall is null)
            {
                continue;
            }

            string[] names;
            try
            {
                names = uninstall.GetSubKeyNames();
            }
            catch (Exception e) when (IsAccessProblem(e))
            {
                continue;
            }

            foreach (var name in names)
            {
                var entry = TryReadEntry(uninstall, name, scope, arch);
                if (entry is not null)
                {
                    yield return entry;
                }
            }
        }
    }

    private static RegistryKey? TryOpenUninstall(RegistryHive hive, RegistryView view)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            return baseKey.OpenSubKey(UninstallPath);
        }
        catch (Exception e) when (IsAccessProblem(e))
        {
            return null;
        }
    }

    private static UninstallEntry? TryReadEntry(RegistryKey parent, string name, RegistryScope scope, ProgramArchitecture arch)
    {
        try
        {
            using var key = parent.OpenSubKey(name);
            if (key is null)
            {
                return null;
            }

            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var valueName in key.GetValueNames())
            {
                if (valueName.Length == 0)
                {
                    continue; // valor predeterminado
                }

                // Sin expandir variables de entorno: el texto crudo se conserva tal cual.
                values[valueName] = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            }

            return new UninstallEntry(scope, arch, name, values);
        }
        catch (Exception e) when (IsAccessProblem(e))
        {
            return null;
        }
    }

    private static bool IsAccessProblem(Exception e) =>
        e is SecurityException or UnauthorizedAccessException or IOException;
}
