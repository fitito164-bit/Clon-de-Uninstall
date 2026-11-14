using Xunit;
using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Registry;

namespace Quitar.Core.Tests;

public class NameMatcherTests
{
    [Theory]
    [InlineData("7-Zip 23.01 (x64 edition)", "7zip")]
    [InlineData("Notepad++ (64-bit x64)", "notepad")]
    [InlineData("Mozilla Firefox (x64 es-AR)", "mozillafirefox")]
    [InlineData("Instalación de Éxito v2.3", "instalaciondeexito")]
    [InlineData("VLC media player", "vlcmediaplayer")]
    [InlineData("Foo 64-bit", "foo")]
    [InlineData(null, "")]
    public void Squash_removes_noise(string? input, string expected)
    {
        Assert.Equal(expected, NameMatcher.Squash(input));
    }

    [Theory]
    [InlineData("7zip", true)]
    [InlineData("firefox", true)]
    [InlineData("zip", false)]
    [InlineData("windows", false)]
    [InlineData("setup", false)]
    [InlineData("", false)]
    public void Only_specific_names_are_usable(string squashed, bool usable)
    {
        Assert.Equal(usable, NameMatcher.IsUsable(squashed));
    }
}

public class ProtectionRulesTests : IDisposable
{
    private readonly TempDir _t = new();
    private readonly string _programFiles;
    private readonly string _windows;
    private readonly ProtectionRules _rules;

    public ProtectionRulesTests()
    {
        _programFiles = _t.Dir("C", "Program Files");
        _windows = _t.Dir("C", "Windows");
        _rules = new ProtectionRules([_programFiles], [_windows]);
    }

    public void Dispose() => _t.Dispose();

    [Fact]
    public void A_program_folder_inside_a_guarded_root_is_allowed()
    {
        Assert.False(_rules.IsProtected(Path.Combine(_programFiles, "Foo")));
    }

    [Fact]
    public void The_guarded_root_itself_is_protected()
    {
        Assert.True(_rules.IsProtected(_programFiles));
        Assert.True(_rules.IsProtected(_programFiles + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void Ancestors_of_a_guarded_root_are_protected()
    {
        Assert.True(_rules.IsProtected(Path.Combine(_t.Path, "C")));
        Assert.True(_rules.IsProtected(_t.Path));
    }

    [Fact]
    public void Ancestors_of_a_guarded_root_are_protected_even_without_forbidden_trees()
    {
        var users = _t.Dir("Users");
        var profile = _t.Dir("Users", "nico", "AppData");
        var rules = new ProtectionRules([profile], []);

        Assert.True(rules.IsProtected(profile));
        Assert.True(rules.IsProtected(Path.Combine(_t.Path, "Users", "nico")));
        Assert.True(rules.IsProtected(users));
        Assert.False(rules.IsProtected(Path.Combine(profile, "Foo")));
    }

    [Fact]
    public void Nothing_inside_a_forbidden_tree_is_allowed()
    {
        Assert.True(_rules.IsProtected(_windows));
        Assert.True(_rules.IsProtected(Path.Combine(_windows, "System32", "drivers")));
    }

    [Fact]
    public void Dot_dot_cannot_escape_the_rules()
    {
        Assert.True(_rules.IsProtected(Path.Combine(_programFiles, "Foo", "..", "..", "Windows", "System32")));
        Assert.True(_rules.IsProtected(Path.Combine(_programFiles, "Foo", "..")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relativo/carpeta")]
    public void Empty_or_relative_paths_are_protected(string? path)
    {
        Assert.True(_rules.IsProtected(path));
    }

    [Fact]
    public void Drive_root_is_protected()
    {
        Assert.True(_rules.IsProtected(Path.GetPathRoot(_t.Path)));
    }

    [Theory]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Foo", false)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\WOW6432Node\Foo", false)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Acme\Foo", false)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\WOW6432Node\Acme\Foo", false)]
    [InlineData(RegistryScope.User, @"Software\Foo", false)]
    [InlineData(RegistryScope.User, @"Software\Acme\Foo", false)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Foo", false)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{GUID}", false)]
    [InlineData(RegistryScope.User, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Foo", false)]
    [InlineData(RegistryScope.Machine, "SOFTWARE", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\WOW6432Node", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Microsoft", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Microsoft\Windows", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Classes\Foo", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Policies\Foo", true)]
    [InlineData(RegistryScope.Machine, @"SOFTWARE\Acme\Foo\Bar", true)]
    [InlineData(RegistryScope.Machine, @"SYSTEM\CurrentControlSet\Services\Foo", true)]
    [InlineData(RegistryScope.User, "Software", true)]
    [InlineData(RegistryScope.User, @"Software\Microsoft\Office", true)]
    [InlineData(RegistryScope.Machine, "", true)]
    public void Registry_keys_follow_the_allowed_shapes(RegistryScope scope, string subKey, bool isProtected)
    {
        Assert.Equal(isProtected, _rules.IsProtected(new RegistryKeyRef(scope, subKey)));
    }
}
