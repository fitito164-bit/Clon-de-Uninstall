using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using Quitar.Core.Programs;

namespace Quitar.Core.Registry;

/// <summary>
/// Registro real de Windows. Leer y borrar usa la API de .NET; exportar e importar usa reg.exe,
/// que maneja todos los tipos de valor y el formato .reg estándar.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRegistryEditor : IRegistryEditor
{
    private static RegistryKey OpenBase(RegistryScope scope) =>
        RegistryKey.OpenBaseKey(
            scope == RegistryScope.Machine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            RegistryView.Registry64);

    public bool KeyExists(RegistryKeyRef key)
    {
        try
        {
            using var root = OpenBase(key.Scope);
            using var sub = root.OpenSubKey(key.SubKey);
            return sub is not null;
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryKeyRef key)
    {
        try
        {
            using var root = OpenBase(key.Scope);
            using var sub = root.OpenSubKey(key.SubKey);
            return sub?.GetSubKeyNames() ?? [];
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException or IOException)
        {
            return [];
        }
    }

    public void Export(RegistryKeyRef key, string destinationFile)
    {
        RunRegExe("export", key.RegExePath, destinationFile, "/y", "/reg:64");
        if (!File.Exists(destinationFile) || new FileInfo(destinationFile).Length == 0)
        {
            throw new IOException($"reg.exe no generó el archivo de respaldo de {key}.");
        }
    }

    public void DeleteKeyTree(RegistryKeyRef key)
    {
        using var root = OpenBase(key.Scope);
        root.DeleteSubKeyTree(key.SubKey, throwOnMissingSubKey: false);
    }

    public void Import(string regFile)
    {
        if (!File.Exists(regFile))
        {
            throw new FileNotFoundException("No se encuentra el archivo de respaldo.", regFile);
        }

        RunRegExe("import", regFile, "/reg:64");
    }

    private static void RunRegExe(params string[] arguments)
    {
        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "reg.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new IOException("No se pudo iniciar reg.exe.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var detail = (error.Result + " " + output.Result).Trim();
            throw new IOException($"reg.exe {arguments[0]} falló (código {process.ExitCode}): {detail}");
        }
    }
}
