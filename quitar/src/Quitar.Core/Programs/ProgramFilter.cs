using Quitar.Core.Text;

namespace Quitar.Core.Programs;

/// <summary>
/// Búsqueda instantánea (flujo F05). Cada palabra escrita tiene que aparecer en el nombre,
/// el editor o la versión. No distingue mayúsculas ni tildes.
/// </summary>
public static class ProgramFilter
{
    public static IReadOnlyList<InstalledProgram> Apply(
        IEnumerable<InstalledProgram> programs,
        string? query,
        ProgramGroup? group)
    {
        var tokens = Tokenize(query);
        return programs
            .Where(p => (group is null || p.Group == group) && Matches(p, tokens))
            .ToList();
    }

    /// <summary>Cuántos programas coinciden con la búsqueda en cada grupo.</summary>
    public static IReadOnlyDictionary<ProgramGroup, int> CountByGroup(IEnumerable<InstalledProgram> programs, string? query)
    {
        var tokens = Tokenize(query);
        var counts = Enum.GetValues<ProgramGroup>().ToDictionary(g => g, _ => 0);
        foreach (var p in programs)
        {
            if (Matches(p, tokens))
            {
                counts[p.Group]++;
            }
        }

        return counts;
    }

    private static string[] Tokenize(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : Fold(query).Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

    private static bool Matches(InstalledProgram program, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return true;
        }

        var haystack = Fold(program.Name) + "\u0001" + Fold(program.Publisher) + "\u0001" + Fold(program.Version);
        return tokens.All(t => haystack.Contains(t, StringComparison.Ordinal));
    }

    private static string Fold(string? text) => TextFolding.Fold(text);
}
