using Quitar.Core.Programs;
using Xunit;

namespace Quitar.Core.Tests;

public class ProgramParserTests
{
    [Fact]
    public void Entry_without_display_name_is_skipped()
    {
        var entry = Entry.Make("NoName", new() { ["Publisher"] = "Alguien" });
        Assert.Null(ProgramParser.TryParse(entry));
    }

    [Fact]
    public void Entry_with_blank_display_name_is_skipped()
    {
        var entry = Entry.Make("Blank", new() { ["DisplayName"] = "   " });
        Assert.Null(ProgramParser.TryParse(entry));
    }

    [Fact]
    public void Regular_app_is_parsed_into_apps_group()
    {
        var entry = Entry.Make("MiApp", new()
        {
            ["DisplayName"] = " Mi App ",
            ["Publisher"] = "Ejemplo SA",
            ["DisplayVersion"] = "1.2.3",
            ["InstallDate"] = "20240131",
            ["EstimatedSize"] = 2048,
            ["InstallLocation"] = @"C:\Program Files\MiApp",
            ["UninstallString"] = @"""C:\Program Files\MiApp\unins000.exe""",
        });

        var p = ProgramParser.TryParse(entry);

        Assert.NotNull(p);
        Assert.Equal("Mi App", p.Name);
        Assert.Equal("Ejemplo SA", p.Publisher);
        Assert.Equal("1.2.3", p.Version);
        Assert.Equal(new DateOnly(2024, 1, 31), p.InstallDate);
        Assert.Equal(2048L * 1024, p.SizeBytes);
        Assert.Equal(ProgramGroup.Apps, p.Group);
        Assert.False(p.IsUpdate);
        Assert.Equal("Machine|X64|MiApp", p.Id);
    }

    [Fact]
    public void Zero_estimated_size_means_unknown()
    {
        var entry = Entry.Make("A", new() { ["DisplayName"] = "A", ["EstimatedSize"] = 0 });
        Assert.Null(ProgramParser.TryParse(entry)!.SizeBytes);
    }

    [Fact]
    public void Invalid_install_date_is_ignored()
    {
        var entry = Entry.Make("A", new() { ["DisplayName"] = "A", ["InstallDate"] = "ayer" });
        Assert.Null(ProgramParser.TryParse(entry)!.InstallDate);
    }

    [Fact]
    public void System_component_goes_to_hidden_group()
    {
        var entry = Entry.Make("Oculto", new() { ["DisplayName"] = "Oculto", ["SystemComponent"] = 1 });
        Assert.Equal(ProgramGroup.Hidden, ProgramParser.TryParse(entry)!.Group);
    }

    [Fact]
    public void Entry_with_parent_key_is_an_update_in_system_group()
    {
        var entry = Entry.Make("Parche", new()
        {
            ["DisplayName"] = "Parche de algo",
            ["ParentKeyName"] = "AlgoPadre",
        });

        var p = ProgramParser.TryParse(entry)!;

        Assert.True(p.IsUpdate);
        Assert.Equal("AlgoPadre", p.ParentKeyName);
        Assert.Equal(ProgramGroup.System, p.Group);
    }

    [Theory]
    [InlineData("Security Update for Microsoft Word 2016 (KB4011610)")]
    [InlineData("Update for Windows (KB5001234)")]
    [InlineData("Hotfix for Algo")]
    public void Update_names_are_detected(string name)
    {
        var entry = Entry.Make("X", new() { ["DisplayName"] = name });
        Assert.True(ProgramParser.TryParse(entry)!.IsUpdate);
    }

    [Fact]
    public void App_whose_name_merely_starts_with_update_is_not_an_update()
    {
        var entry = Entry.Make("X", new() { ["DisplayName"] = "Update Checker Pro" });
        var p = ProgramParser.TryParse(entry)!;
        Assert.False(p.IsUpdate);
        Assert.Equal(ProgramGroup.Apps, p.Group);
    }

    [Fact]
    public void Microsoft_redistributable_goes_to_system_group()
    {
        var entry = Entry.Make("VC", new()
        {
            ["DisplayName"] = "Microsoft Visual C++ 2015-2022 Redistributable (x64)",
            ["Publisher"] = "Microsoft Corporation",
        });
        Assert.Equal(ProgramGroup.System, ProgramParser.TryParse(entry)!.Group);
    }

    [Fact]
    public void Microsoft_office_stays_in_apps_group()
    {
        var entry = Entry.Make("Office", new()
        {
            ["DisplayName"] = "Microsoft Office Profesional",
            ["Publisher"] = "Microsoft Corporation",
        });
        Assert.Equal(ProgramGroup.Apps, ProgramParser.TryParse(entry)!.Group);
    }

    [Fact]
    public void Guid_key_marks_windows_installer()
    {
        var entry = Entry.Make("{3F2504E0-4F89-11D3-9A0C-0305E82C3301}", new() { ["DisplayName"] = "MSI App" });
        Assert.True(ProgramParser.TryParse(entry)!.IsWindowsInstaller);
    }

    [Fact]
    public void Environment_variables_in_uninstall_string_are_kept_raw()
    {
        // Caso del foro del original: %windir% no se debe expandir al leer el registro.
        var entry = Entry.Make("Env", new()
        {
            ["DisplayName"] = "Env",
            ["UninstallString"] = @"%windir%\system32\calc.exe",
        });
        Assert.Equal(@"%windir%\system32\calc.exe", ProgramParser.TryParse(entry)!.UninstallString);
    }

    [Fact]
    public void Same_program_in_two_architectures_gets_different_ids()
    {
        var a = Entry.Make("Dup", new() { ["DisplayName"] = "Dup" }, arch: ProgramArchitecture.X64);
        var b = Entry.Make("Dup", new() { ["DisplayName"] = "Dup" }, arch: ProgramArchitecture.X86);
        Assert.NotEqual(ProgramParser.TryParse(a)!.Id, ProgramParser.TryParse(b)!.Id);
    }
}
