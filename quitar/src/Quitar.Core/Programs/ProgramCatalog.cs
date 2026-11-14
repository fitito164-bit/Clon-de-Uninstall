using Quitar.Core.Registry;

namespace Quitar.Core.Programs;

/// <summary>Arma la lista completa de programas instalados, sin duplicados y ordenada por nombre.</summary>
public static class ProgramCatalog
{
    public static IReadOnlyList<InstalledProgram> Load(IUninstallSource source)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<InstalledProgram>();

        foreach (var entry in source.ReadEntries())
        {
            var program = ProgramParser.TryParse(entry);
            if (program is null || !seen.Add(program.Id))
            {
                continue;
            }

            list.Add(program);
        }

        list.Sort(static (a, b) =>
        {
            var byName = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return byName != 0 ? byName : string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
        });

        return list;
    }
}
