using Xunit;
using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Registry;
using Quitar.Core.Removal;
using Quitar.Core.Uninstall;

namespace Quitar.Core.Tests;

public class UninstallWorkflowTests : IDisposable
{
    private readonly TempDir _t = new();
    private readonly string _programFiles;
    private readonly FakeRegistry _registry = new();
    private readonly FakeHost _host = new();
    private readonly MemoryStore _store = new();
    private readonly UninstallWorkflow _workflow;
    private readonly InstalledProgram _program;
    private readonly RegistryKeyRef _entry;

    public UninstallWorkflowTests()
    {
        _programFiles = _t.Dir("Program Files");
        var locations = new ScanLocations([new ScanFolder(_programFiles, LeftoverRisk.Low, "pf")], [], [], [_programFiles]);
        var rules = new ProtectionRules([_programFiles], [_t.Dir("Windows")]);
        var paths = new AppPaths(_t.Dir("Quitar"));
        var backups = new RegistryBackupService(_registry, paths);
        var scanner = new LeftoverScanner(_registry, locations, rules);
        var runner = new UninstallRunner(_host, p => _registry.KeyExists(RegistryKeyRef.ForUninstallEntry(p)), delay: (_, _) => Task.CompletedTask);
        _workflow = new UninstallWorkflow(runner, scanner, backups, _registry, _store);

        _program = Entry.Program("Foo Editor", "Acme") with
        {
            KeyName = "Foo Editor",
            UninstallString = "\"C:\\Foo\\unins000.exe\" /SILENT",
            InstallLocation = Path.Combine(_programFiles, "Foo Editor"),
        };
        _entry = RegistryKeyRef.ForUninstallEntry(_program);
        _registry.Add(_entry.Scope, _entry.SubKey);
        _t.Dir("Program Files", "Foo Editor");
        _t.File("Program Files/Foo Editor/foo.exe");
    }

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task Backs_up_the_entry_then_runs_the_uninstaller_then_scans()
    {
        // El desinstalador de verdad quita su entrada del registro.
        _host.OnRun = _ => { _registry.DeleteKeyTree(_entry); _registry.Events.Add("uninstaller"); return 0; };
        _registry.AddMachine(@"SOFTWARE\Foo Editor");

        var result = await _workflow.PrepareAsync(_program);

        Assert.Equal(PrepareStatus.ReadyToReview, result.Status);
        Assert.Equal("export:" + _entry, _registry.Events[0]);
        Assert.Equal("delete:" + _entry, _registry.Events[1]);
        Assert.Equal("uninstaller", _registry.Events[2]);

        var run = result.Run;
        Assert.Equal(RunStatus.Reviewing, run.Status);
        Assert.Equal(2, run.Items.Count); // carpeta de instalación + clave SOFTWARE\Foo Editor
        Assert.Single(run.Backups);
        Assert.Equal(_entry, run.Backups[0].Key);
        Assert.Equal("C:\\Foo\\unins000.exe", _host.Calls[0].FileName);
        Assert.Equal(0, run.UninstallExitCode);
        Assert.Contains(run.Log, l => l.Operation == LogOperation.RunUninstaller && l.Result == LogResult.Ok);
        Assert.True(ReferenceEquals(run, _store.Runs[run.Id]));
    }

    [Fact]
    public async Task Nothing_is_launched_if_the_backup_fails()
    {
        _registry.FailExport = true;

        var result = await _workflow.PrepareAsync(_program);

        Assert.Equal(PrepareStatus.BackupFailed, result.Status);
        Assert.Empty(_host.Calls);
        Assert.Equal(RunStatus.Failed, result.Run.Status);
        Assert.True(_registry.KeyExists(_entry));
    }

    [Fact]
    public async Task Program_without_an_uninstaller_is_reported_and_nothing_runs()
    {
        var result = await _workflow.PrepareAsync(_program with { UninstallString = null });

        Assert.Equal(PrepareStatus.NoUninstaller, result.Status);
        Assert.Empty(_host.Calls);
        Assert.Empty(_registry.Events);
    }

    [Fact]
    public async Task If_the_program_is_still_installed_no_leftovers_are_offered()
    {
        _host.OnRun = _ => 1602; // el usuario canceló el desinstalador
        var run = await _workflow.PrepareAsync(_program);

        Assert.Equal(PrepareStatus.StillInstalled, run.Status);
        Assert.Empty(run.Run.Items);
        Assert.Equal(RunStatus.Cancelled, run.Run.Status);
        Assert.True(Directory.Exists(Path.Combine(_programFiles, "Foo Editor")));
    }

    [Fact]
    public async Task Launch_failure_ends_the_run_as_failed()
    {
        _host.Throw = new InvalidOperationException("sin permiso");

        var result = await _workflow.PrepareAsync(_program);

        Assert.Equal(PrepareStatus.LaunchFailed, result.Status);
        Assert.Equal(RunStatus.Failed, result.Run.Status);
        Assert.Contains("sin permiso", result.Message);
    }

    [Fact]
    public async Task Clean_uninstall_with_nothing_left_finishes_immediately()
    {
        Directory.Delete(Path.Combine(_programFiles, "Foo Editor"), recursive: true);
        _host.OnRun = _ => { _registry.DeleteKeyTree(_entry); return 0; };

        var result = await _workflow.PrepareAsync(_program);

        Assert.Equal(PrepareStatus.NothingLeft, result.Status);
        Assert.Equal(RunStatus.Done, result.Run.Status);
    }
}

public class JsonRunStoreTests : IDisposable
{
    private readonly TempDir _t = new();

    public void Dispose() => _t.Dispose();

    private static RemovalRun Sample()
    {
        var run = new RemovalRun { ProgramName = "Foo", Status = RunStatus.Reviewing };
        run.Items.Add(new LeftoverItem
        {
            Kind = LeftoverKind.RegistryKey,
            Key = new RegistryKeyRef(RegistryScope.User, @"Software\Foo"),
            Path = @"HKCU\Software\Foo",
            Risk = LeftoverRisk.Medium,
            Selected = true,
            Reason = "r",
        });
        run.Backups.Add(new BackupRecord { Key = new RegistryKeyRef(RegistryScope.Machine, @"SOFTWARE\Foo"), RegFile = "a.reg", CreatedAt = DateTimeOffset.UtcNow });
        run.AddLog(LogOperation.Scan, "Foo", LogResult.Ok, "1 elemento");
        return run;
    }

    [Fact]
    public void A_run_survives_a_round_trip_with_everything_in_it()
    {
        var store = new JsonRunStore(_t.Dir("runs"));
        var run = Sample();

        store.Save(run);
        var loaded = Assert.Single(store.List());

        Assert.Equal(run.Id, loaded.Id);
        Assert.Equal(RunStatus.Reviewing, loaded.Status);
        Assert.Equal(new RegistryKeyRef(RegistryScope.User, @"Software\Foo"), loaded.Items[0].Key);
        Assert.Equal(LeftoverRisk.Medium, loaded.Items[0].Risk);
        Assert.Equal(@"SOFTWARE\Foo", loaded.Backups[0].Key.SubKey);
        Assert.Equal("1 elemento", loaded.Log[0].Message);
    }

    [Fact]
    public void Saving_twice_replaces_the_file_and_leaves_no_temp_files()
    {
        var dir = _t.Dir("runs");
        var store = new JsonRunStore(dir);
        var run = Sample();

        store.Save(run);
        run.Status = RunStatus.Done;
        store.Save(run);

        Assert.Single(Directory.GetFiles(dir));
        Assert.Equal(RunStatus.Done, Assert.Single(store.List()).Status);
    }

    [Fact]
    public void A_corrupt_file_does_not_hide_the_rest_of_the_history()
    {
        var dir = _t.Dir("runs");
        var store = new JsonRunStore(dir);
        store.Save(Sample());
        File.WriteAllText(Path.Combine(dir, "roto.json"), "{ esto no es json");

        Assert.Single(store.List());
    }

    [Fact]
    public void Missing_folder_gives_an_empty_history()
    {
        Assert.Empty(new JsonRunStore(Path.Combine(_t.Path, "no-existe")).List());
    }

    [Fact]
    public void Newest_run_comes_first()
    {
        var store = new JsonRunStore(_t.Dir("runs"));
        var old = Sample();
        old.StartedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var recent = Sample();
        store.Save(old);
        store.Save(recent);

        Assert.Equal(recent.Id, store.List()[0].Id);
    }

    [Fact]
    public void Interrupted_runs_are_closed_when_the_app_starts_again()
    {
        var store = new JsonRunStore(_t.Dir("runs"));
        var deleting = Sample(); deleting.Status = RunStatus.Deleting;
        var reviewing = Sample(); reviewing.Status = RunStatus.Reviewing;
        var done = Sample(); done.Status = RunStatus.Done;
        store.Save(deleting); store.Save(reviewing); store.Save(done);

        store.MarkInterrupted();

        var byId = store.List().ToDictionary(r => r.Id);
        Assert.Equal(RunStatus.Failed, byId[deleting.Id].Status);
        Assert.Equal(RunStatus.Cancelled, byId[reviewing.Id].Status);
        Assert.Equal(RunStatus.Done, byId[done.Id].Status);
        Assert.NotNull(byId[deleting.Id].EndedAt);
    }
}
