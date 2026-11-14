using Xunit;
using Quitar.Core.Programs;
using Quitar.Core.Uninstall;

namespace Quitar.Core.Tests;

public class UninstallCommandParserTests
{
    private static string NoExpand(string s) => s;

    [Fact]
    public void Quoted_path_with_arguments()
    {
        var c = UninstallCommandParser.Parse("\"C:\\Program Files\\Foo\\unins000.exe\" /SILENT /NORESTART", NoExpand);

        Assert.NotNull(c);
        Assert.Equal("C:\\Program Files\\Foo\\unins000.exe", c!.FileName);
        Assert.Equal("/SILENT /NORESTART", c.Arguments);
    }

    [Fact]
    public void Quoted_path_without_arguments()
    {
        var c = UninstallCommandParser.Parse("\"C:\\Foo\\uninstall.exe\"", NoExpand);

        Assert.Equal("C:\\Foo\\uninstall.exe", c!.FileName);
        Assert.Equal("", c.Arguments);
    }

    [Fact]
    public void Unquoted_path_with_spaces_is_cut_at_the_extension()
    {
        var c = UninstallCommandParser.Parse("C:\\Program Files\\Foo Bar\\uninst.exe /S /D=C:\\x", NoExpand);

        Assert.Equal("C:\\Program Files\\Foo Bar\\uninst.exe", c!.FileName);
        Assert.Equal("/S /D=C:\\x", c.Arguments);
    }

    [Fact]
    public void Unquoted_path_without_arguments()
    {
        var c = UninstallCommandParser.Parse("C:\\Foo\\remove.exe", NoExpand);

        Assert.Equal("C:\\Foo\\remove.exe", c!.FileName);
        Assert.Equal("", c.Arguments);
    }

    [Fact]
    public void Command_without_extension_is_split_at_the_first_space()
    {
        var c = UninstallCommandParser.Parse("rundll32 shell32.dll,Control_RunDLL x", NoExpand);

        Assert.Equal("rundll32", c!.FileName);
        Assert.Equal("shell32.dll,Control_RunDLL x", c.Arguments);
    }

    [Theory]
    [InlineData("MsiExec.exe /I{11111111-2222-3333-4444-555555555555}", "/X{11111111-2222-3333-4444-555555555555}")]
    [InlineData("MsiExec.exe /i{11111111-2222-3333-4444-555555555555}", "/X{11111111-2222-3333-4444-555555555555}")]
    [InlineData("MsiExec.exe /X{11111111-2222-3333-4444-555555555555}", "/X{11111111-2222-3333-4444-555555555555}")]
    [InlineData("msiexec /I{11111111-2222-3333-4444-555555555555} /qb", "/X{11111111-2222-3333-4444-555555555555} /qb")]
    public void Msi_install_switch_becomes_uninstall(string raw, string expectedArguments)
    {
        var c = UninstallCommandParser.Parse(raw, NoExpand);

        Assert.Equal(expectedArguments, c!.Arguments);
    }

    [Fact]
    public void Non_msi_programs_keep_their_slash_i()
    {
        var c = UninstallCommandParser.Parse("\"C:\\Foo\\setup.exe\" /I{abc}", NoExpand);

        Assert.Equal("/I{abc}", c!.Arguments);
    }

    [Fact]
    public void Environment_variables_are_expanded()
    {
        var c = UninstallCommandParser.Parse("%SystemRoot%\\system32\\foo.exe /u", s => s.Replace("%SystemRoot%", "C:\\Windows"));

        Assert.Equal("C:\\Windows\\system32\\foo.exe", c!.FileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"C:\\sin cierre.exe")]
    public void Unusable_strings_give_null(string? raw)
    {
        Assert.Null(UninstallCommandParser.Parse(raw, NoExpand));
    }

    [Fact]
    public void Windows_installer_program_without_string_gets_msiexec()
    {
        var p = Entry.Program("Foo") with
        {
            KeyName = "{11111111-2222-3333-4444-555555555555}",
            IsWindowsInstaller = true,
        };

        var c = UninstallCommandParser.For(p, NoExpand);

        Assert.Equal("msiexec.exe", c!.FileName);
        Assert.Equal("/X{11111111-2222-3333-4444-555555555555}", c.Arguments);
    }

    [Fact]
    public void Program_without_any_way_to_uninstall_gives_null()
    {
        Assert.Null(UninstallCommandParser.For(Entry.Program("Foo"), NoExpand));
    }
}
