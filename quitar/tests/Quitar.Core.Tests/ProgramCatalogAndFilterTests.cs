using System.Globalization;
using Quitar.Core.Formatting;
using Quitar.Core.Programs;
using Xunit;

namespace Quitar.Core.Tests;

public class ProgramCatalogTests
{
    [Fact]
    public void Load_skips_nameless_entries_and_sorts_by_name_ignoring_case()
    {
        var source = new FakeSource(
            Entry.Make("z", new() { ["DisplayName"] = "zeta" }),
            Entry.Make("n", new() { ["Publisher"] = "sin nombre" }),
            Entry.Make("a", new() { ["DisplayName"] = "Alfa" }),
            Entry.Make("b", new() { ["DisplayName"] = "beta" }));

        var list = ProgramCatalog.Load(source);

        Assert.Equal(["Alfa", "beta", "zeta"], list.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Load_removes_exact_duplicates_by_id()
    {
        var one = Entry.Make("Dup", new() { ["DisplayName"] = "Dup" });
        var list = ProgramCatalog.Load(new FakeSource(one, one));
        Assert.Single(list);
    }

    [Fact]
    public void Load_keeps_same_key_from_machine_and_user_scope()
    {
        var list = ProgramCatalog.Load(new FakeSource(
            Entry.Make("Same", new() { ["DisplayName"] = "Same" }, RegistryScope.Machine),
            Entry.Make("Same", new() { ["DisplayName"] = "Same" }, RegistryScope.User)));
        Assert.Equal(2, list.Count);
    }
}

public class ProgramFilterTests
{
    private static readonly InstalledProgram[] Sample =
    [
        Entry.Program("Mozilla Firefox", "Mozilla", "130.0"),
        Entry.Program("Microsoft Edge", "Microsoft Corporation", "129.0", ProgramGroup.System),
        Entry.Program("Instalación de prueba", "Peña y Cía", "1.0"),
        Entry.Program("Oculto interno", null, null, ProgramGroup.Hidden),
    ];

    [Fact]
    public void Empty_query_returns_everything_in_group()
    {
        var found = ProgramFilter.Apply(Sample, "", ProgramGroup.Apps);
        Assert.Equal(2, found.Count);
    }

    [Fact]
    public void Null_group_searches_all_groups()
    {
        Assert.Equal(4, ProgramFilter.Apply(Sample, null, null).Count);
    }

    [Fact]
    public void Query_ignores_case_and_matches_publisher_and_version()
    {
        Assert.Single(ProgramFilter.Apply(Sample, "FIREFOX", null));
        Assert.Single(ProgramFilter.Apply(Sample, "mozilla", null));
        Assert.Single(ProgramFilter.Apply(Sample, "130.0", null));
    }

    [Fact]
    public void Query_ignores_accents_in_both_directions()
    {
        Assert.Single(ProgramFilter.Apply(Sample, "instalacion", null));
        Assert.Single(ProgramFilter.Apply(Sample, "Instalación", null));
        Assert.Single(ProgramFilter.Apply(Sample, "pena", null));
    }

    [Fact]
    public void Every_word_must_match_but_in_any_order()
    {
        Assert.Single(ProgramFilter.Apply(Sample, "firefox mozilla", null));
        Assert.Single(ProgramFilter.Apply(Sample, "mozilla firefox", null));
        Assert.Empty(ProgramFilter.Apply(Sample, "firefox edge", null));
    }

    [Fact]
    public void Counts_by_group_follow_the_query()
    {
        var all = ProgramFilter.CountByGroup(Sample, "");
        Assert.Equal(2, all[ProgramGroup.Apps]);
        Assert.Equal(1, all[ProgramGroup.System]);
        Assert.Equal(1, all[ProgramGroup.Hidden]);

        var micro = ProgramFilter.CountByGroup(Sample, "micro");
        Assert.Equal(0, micro[ProgramGroup.Apps]);
        Assert.Equal(1, micro[ProgramGroup.System]);
    }
}

public class SizeFormatterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData(0L, "")]
    [InlineData(512L, "512 B")]
    [InlineData(2048L, "2 KB")]
    [InlineData(3L * 1024 * 1024, "3.0 MB")]
    [InlineData(5L * 1024 * 1024 * 1024 + 512L * 1024 * 1024, "5.5 GB")]
    public void Formats_sizes(long? bytes, string expected)
    {
        Assert.Equal(expected, SizeFormatter.Format(bytes, CultureInfo.InvariantCulture));
    }
}
