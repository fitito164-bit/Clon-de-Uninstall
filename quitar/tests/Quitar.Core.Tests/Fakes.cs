using Quitar.Core.Programs;
using Quitar.Core.Registry;

namespace Quitar.Core.Tests;

internal sealed class FakeSource : IUninstallSource
{
    private readonly UninstallEntry[] _entries;

    public FakeSource(params UninstallEntry[] entries) => _entries = entries;

    public IEnumerable<UninstallEntry> ReadEntries() => _entries;
}

internal static class Entry
{
    public static UninstallEntry Make(
        string keyName,
        Dictionary<string, object?> values,
        RegistryScope scope = RegistryScope.Machine,
        ProgramArchitecture arch = ProgramArchitecture.X64) =>
        new(scope, arch, keyName, new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase));

    public static InstalledProgram Program(
        string name,
        string? publisher = null,
        string? version = null,
        ProgramGroup group = ProgramGroup.Apps) =>
        new()
        {
            Id = "Machine|X64|" + name,
            KeyName = name,
            Name = name,
            Publisher = publisher,
            Version = version,
            Group = group,
        };
}

/// <summary>Carpeta temporal que se borra sola al terminar la prueba.</summary>
internal sealed class TempDir : IDisposable
{
    public TempDir() => Path = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "quitar-tests-" + Guid.NewGuid().ToString("N"));

    public string Path { get; }

    public string Dir(params string[] parts)
    {
        var full = System.IO.Path.Combine([Path, .. parts]);
        Directory.CreateDirectory(full);
        return full;
    }

    public string File(string relative, string content = "x")
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>Registro falso en memoria: sin distinguir mayúsculas, como el real.</summary>
internal sealed class FakeRegistry : Quitar.Core.Registry.IRegistryEditor
{
    private readonly HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Imported { get; } = [];

    public List<string> Deleted { get; } = [];

    public List<string> Events { get; } = [];

    public bool FailExport { get; set; }

    public bool FailDelete { get; set; }

    public bool FailImport { get; set; }

    public FakeRegistry Add(Quitar.Core.Programs.RegistryScope scope, string subKey)
    {
        // Al crear una clave existen también sus ancestros.
        var parts = subKey.Split('\\');
        for (var i = 1; i <= parts.Length; i++)
        {
            _keys.Add(Id(scope, string.Join('\\', parts.Take(i))));
        }

        return this;
    }

    public FakeRegistry AddMachine(string subKey) => Add(Quitar.Core.Programs.RegistryScope.Machine, subKey);

    public FakeRegistry AddUser(string subKey) => Add(Quitar.Core.Programs.RegistryScope.User, subKey);

    public bool Has(Quitar.Core.Programs.RegistryScope scope, string subKey) => _keys.Contains(Id(scope, subKey));

    public bool KeyExists(Quitar.Core.Registry.RegistryKeyRef key) => _keys.Contains(Id(key));

    public IReadOnlyList<string> GetSubKeyNames(Quitar.Core.Registry.RegistryKeyRef key)
    {
        var prefix = Id(key) + "\\";
        return _keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => k[prefix.Length..])
            .Where(rest => !rest.Contains('\\'))
            .ToList();
    }

    public void Export(Quitar.Core.Registry.RegistryKeyRef key, string destinationFile)
    {
        Events.Add("export:" + key);
        if (FailExport)
        {
            throw new IOException("export falló (simulado)");
        }

        File.WriteAllText(destinationFile, "Windows Registry Editor Version 5.00\r\n[" + key + "]\r\n");
    }

    public void DeleteKeyTree(Quitar.Core.Registry.RegistryKeyRef key)
    {
        Events.Add("delete:" + key);
        if (FailDelete)
        {
            throw new UnauthorizedAccessException("acceso denegado (simulado)");
        }

        var id = Id(key);
        _keys.RemoveWhere(k => k.Equals(id, StringComparison.OrdinalIgnoreCase)
            || k.StartsWith(id + "\\", StringComparison.OrdinalIgnoreCase));
        Deleted.Add(key.ToString());
    }

    public void Import(string regFile)
    {
        Events.Add("import:" + System.IO.Path.GetFileName(regFile));
        if (FailImport)
        {
            throw new IOException("import falló (simulado)");
        }

        Imported.Add(regFile);
    }

    private static string Id(Quitar.Core.Registry.RegistryKeyRef key) => Id(key.Scope, key.SubKey);

    private static string Id(Quitar.Core.Programs.RegistryScope scope, string subKey) => scope + ":" + subKey.Trim('\\');
}

internal sealed class FakeFileRemover : Quitar.Core.Removal.IFileRemover
{
    public List<string> Removed { get; } = [];

    public HashSet<string> Failing { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Remove(string path, bool isDirectory)
    {
        if (Failing.Contains(path))
        {
            throw new IOException("El proceso no puede obtener acceso al archivo (simulado)");
        }

        if (isDirectory)
        {
            Directory.Delete(path, recursive: true);
        }
        else
        {
            File.Delete(path);
        }

        Removed.Add(path);
    }
}

internal sealed class FakeHost : Quitar.Core.Uninstall.IUninstallerHost
{
    public List<Quitar.Core.Uninstall.UninstallCommand> Calls { get; } = [];

    public Func<Quitar.Core.Uninstall.UninstallCommand, int> OnRun { get; set; } = _ => 0;

    public Exception? Throw { get; set; }

    public Task<int> RunAsync(Quitar.Core.Uninstall.UninstallCommand command, CancellationToken cancellationToken)
    {
        Calls.Add(command);
        if (Throw is not null)
        {
            throw Throw;
        }

        return Task.FromResult(OnRun(command));
    }
}

internal sealed class MemoryStore : Quitar.Core.Removal.IRunStore
{
    public Dictionary<string, Quitar.Core.Removal.RemovalRun> Runs { get; } = [];

    public void Save(Quitar.Core.Removal.RemovalRun run) => Runs[run.Id] = run;

    public IReadOnlyList<Quitar.Core.Removal.RemovalRun> List() => Runs.Values.ToList();

    public void MarkInterrupted()
    {
    }
}
