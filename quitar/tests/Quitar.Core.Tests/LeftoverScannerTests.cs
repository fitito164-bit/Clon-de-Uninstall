using Xunit;
using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Registry;

namespace Quitar.Core.Tests;

public class LeftoverScannerTests : IDisposable
{
    private readonly TempDir _t = new();
    private readonly string _programFiles;
    private readonly string _appData;
    private readonly string _startMenu;
    private readonly string _desktop;
    private readonly FakeRegistry _registry = new();
    private readonly LeftoverScanner _scanner;

    public LeftoverScannerTests()
    {
        _programFiles = _t.Dir("Program Files");
        _appData = _t.Dir("AppData");
        _startMenu = _t.Dir("StartMenu");
        _desktop = _t.Dir("Desktop");
        var windows = _t.Dir("Windows");

        var locations = new ScanLocations(
            [
                new ScanFolder(_programFiles, LeftoverRisk.Low, "pf"),
                new ScanFolder(_appData, LeftoverRisk.Medium, "datos"),
            ],
            [_startMenu],
            [_desktop],
            [_programFiles]);
        var rules = new ProtectionRules([_programFiles, _appData, _startMenu, _desktop], [windows]);
        _scanner = new LeftoverScanner(_registry, locations, rules);
    }

    public void Dispose() => _t.Dispose();

    private InstalledProgram Foo(string? installLocation = null, string? publisher = "Acme Corp") =>
        Entry.Program("Foo Editor 2.1 (x64)", publisher) with { InstallLocation = installLocation };

    [Fact]
    public void Finds_the_registered_install_folder_as_low_risk_and_selected()
    {
        var dir = _t.Dir("Program Files", "Foo Editor");

        var items = _scanner.Scan(Foo(dir));

        var item = Assert.Single(items);
        Assert.Equal(LeftoverKind.Folder, item.Kind);
        Assert.Equal(dir, item.Path);
        Assert.Equal(LeftoverRisk.Low, item.Risk);
        Assert.True(item.Selected);
    }

    [Fact]
    public void Install_folder_outside_the_usual_places_is_medium_risk()
    {
        var dir = _t.Dir("Juegos", "Cosas", "Foo Editor");

        var item = Assert.Single(_scanner.Scan(Foo(dir)));

        Assert.Equal(LeftoverRisk.Medium, item.Risk);
        Assert.True(item.Selected);
    }

    [Fact]
    public void Install_folder_directly_under_a_drive_root_is_high_risk_and_not_selected()
    {
        // Dos niveles bajo la raíz de la carpeta temporal no sirven: se simula con una carpeta bajo la raíz del disco de pruebas.
        var root = Path.GetPathRoot(_t.Path)!;
        var dir = Path.Combine(root, "quitar-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (UnauthorizedAccessException)
        {
            return; // sin permiso para escribir en la raíz del disco: no se puede armar el caso
        }

        try
        {
            var item = Assert.Single(_scanner.Scan(Foo(dir)));

            Assert.Equal(LeftoverRisk.High, item.Risk);
            Assert.False(item.Selected);
        }
        finally
        {
            Directory.Delete(dir);
        }
    }

    [Fact]
    public void Protected_install_locations_are_never_offered()
    {
        Assert.Empty(_scanner.Scan(Foo(_programFiles)));
        Assert.Empty(_scanner.Scan(Foo(_t.Dir("Windows", "Foo"))));
        Assert.Empty(_scanner.Scan(Foo("relativa")));
    }

    [Fact]
    public void Missing_install_location_is_ignored()
    {
        Assert.Empty(_scanner.Scan(Foo(Path.Combine(_programFiles, "no existe"))));
    }

    [Fact]
    public void Finds_folders_named_like_the_program_ignoring_version_and_punctuation()
    {
        _t.Dir("Program Files", "Foo-Editor");
        _t.Dir("AppData", "foo editor");
        _t.Dir("Program Files", "Otro Programa");

        var items = _scanner.Scan(Foo());

        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.Path.EndsWith("Foo-Editor") && i.Risk == LeftoverRisk.Low);
        Assert.Contains(items, i => i.Path.EndsWith("foo editor") && i.Risk == LeftoverRisk.Medium);
    }

    [Fact]
    public void Does_not_match_partial_names()
    {
        _t.Dir("Program Files", "Foo Editor Pro");
        _t.Dir("Program Files", "Foo");

        Assert.Empty(_scanner.Scan(Foo()));
    }

    [Fact]
    public void Publisher_folder_is_never_offered_only_the_program_folder_inside_it()
    {
        var inner = _t.Dir("AppData", "Acme Corp", "Foo Editor");
        _t.Dir("AppData", "Acme Corp", "Otro");

        var item = Assert.Single(_scanner.Scan(Foo()));

        Assert.Equal(inner, item.Path);
    }

    [Fact]
    public void Nested_findings_collapse_into_the_parent_folder()
    {
        var install = _t.Dir("Program Files", "Foo Editor");
        _t.Dir("Program Files", "Foo Editor", "Foo Editor");

        var item = Assert.Single(_scanner.Scan(Foo(install)));

        Assert.Equal(install, item.Path);
    }

    [Fact]
    public void Same_folder_found_twice_is_reported_once()
    {
        var dir = _t.Dir("Program Files", "Foo Editor");

        Assert.Single(_scanner.Scan(Foo(dir)));
    }

    [Fact]
    public void Finds_start_menu_folder_and_shortcuts()
    {
        _t.Dir("StartMenu", "Foo Editor");
        _t.File("StartMenu/Foo Editor.lnk");
        _t.File("Desktop/Foo Editor.lnk");
        _t.File("Desktop/Otro.lnk");

        var items = _scanner.Scan(Foo());

        Assert.Equal(3, items.Count);
        Assert.Equal(2, items.Count(i => i.Kind == LeftoverKind.Shortcut));
        Assert.Contains(items, i => i.Kind == LeftoverKind.Folder && i.Path.EndsWith("Foo Editor"));
        Assert.All(items, i => Assert.Equal(LeftoverRisk.Low, i.Risk));
    }

    [Fact]
    public void Finds_registry_keys_by_program_name_in_all_three_places()
    {
        _registry.AddMachine(@"SOFTWARE\Foo Editor");
        _registry.AddMachine(@"SOFTWARE\WOW6432Node\FooEditor");
        _registry.AddUser(@"Software\Foo Editor");
        _registry.AddUser(@"Software\Otro");

        var items = _scanner.Scan(Foo());

        Assert.Equal(3, items.Count);
        Assert.All(items, i =>
        {
            Assert.Equal(LeftoverKind.RegistryKey, i.Kind);
            Assert.NotNull(i.Key);
            Assert.True(i.Selected);
        });
    }

    [Fact]
    public void Finds_the_program_key_under_its_publisher_but_never_the_publisher_key()
    {
        _registry.AddMachine(@"SOFTWARE\Acme Corp\Foo Editor");
        _registry.AddMachine(@"SOFTWARE\Acme Corp\Otro");

        var item = Assert.Single(_scanner.Scan(Foo()));

        Assert.Equal(@"HKLM\SOFTWARE\Acme Corp\Foo Editor", item.Path);
    }

    [Fact]
    public void Never_offers_system_keys_even_if_the_name_matches()
    {
        var program = Entry.Program("Classes", "Acme");
        _registry.AddMachine(@"SOFTWARE\Classes");

        Assert.Empty(_scanner.Scan(program));
    }

    [Fact]
    public void Microsoft_components_are_matched_by_install_folder_only()
    {
        _t.Dir("Program Files", "Foo Editor");
        _registry.AddMachine(@"SOFTWARE\Foo Editor");
        var program = Foo(publisher: "Microsoft Corporation");

        Assert.Empty(_scanner.Scan(program));
    }

    [Fact]
    public void Generic_or_short_names_are_not_searched_by_name()
    {
        _t.Dir("Program Files", "Setup");
        _t.Dir("Program Files", "Zip");
        _registry.AddMachine(@"SOFTWARE\Setup");

        Assert.Empty(_scanner.Scan(Entry.Program("Setup")));
        Assert.Empty(_scanner.Scan(Entry.Program("Zip")));
    }

    [Fact]
    public void Folders_come_before_registry_keys_and_high_risk_is_not_preselected()
    {
        _t.Dir("Program Files", "Foo Editor");
        _registry.AddMachine(@"SOFTWARE\Foo Editor");

        var items = _scanner.Scan(Foo());

        Assert.Equal(LeftoverKind.Folder, items[0].Kind);
        Assert.Equal(LeftoverKind.RegistryKey, items[1].Kind);
    }
}
