using Xunit;
using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Registry;
using Quitar.Core.Removal;

namespace Quitar.Core.Tests;

public class RemovalServiceTests : IDisposable
{
    private readonly TempDir _t = new();
    private readonly string _programFiles;
    private readonly FakeRegistry _registry = new();
    private readonly FakeFileRemover _files = new();
    private readonly MemoryStore _store = new();
    private readonly RegistryBackupService _backups;
    private readonly RemovalService _service;
    private readonly AppPaths _paths;

    public RemovalServiceTests()
    {
        _programFiles = _t.Dir("Program Files");
        _paths = new AppPaths(_t.Dir("Quitar"));
        var rules = new ProtectionRules([_programFiles], [_t.Dir("Windows")]);
        _backups = new RegistryBackupService(_registry, _paths);
        _service = new RemovalService(_registry, _files, rules, _backups, _store);
    }

    public void Dispose() => _t.Dispose();

    private static LeftoverItem KeyItem(string subKey, bool selected = true) => new()
    {
        Kind = LeftoverKind.RegistryKey,
        Key = new RegistryKeyRef(RegistryScope.Machine, subKey),
        Path = @"HKLM\" + subKey,
        Selected = selected,
        Reason = "test",
    };

    private static LeftoverItem FolderItem(string path, bool selected = true) => new()
    {
        Kind = LeftoverKind.Folder,
        Path = path,
        Selected = selected,
        Reason = "test",
    };

    [Fact]
    public void Registry_keys_are_exported_before_they_are_deleted()
    {
        _registry.AddMachine(@"SOFTWARE\Foo");
        var run = new RemovalRun { Items = [KeyItem(@"SOFTWARE\Foo")] };

        var summary = _service.Execute(run);

        Assert.Equal(1, summary.Deleted);
        Assert.Equal(new[] { "export:HKLM\\SOFTWARE\\Foo", "delete:HKLM\\SOFTWARE\\Foo" }, _registry.Events);
        var backup = Assert.Single(run.Backups);
        Assert.True(File.Exists(backup.RegFile));
        Assert.Equal(LeftoverState.Deleted, run.Items[0].State);
        Assert.Equal(RunStatus.Done, run.Status);
    }

    [Fact]
    public void A_key_is_not_deleted_when_its_backup_fails()
    {
        _registry.AddMachine(@"SOFTWARE\Foo");
        _registry.FailExport = true;
        var run = new RemovalRun { Items = [KeyItem(@"SOFTWARE\Foo")] };

        var summary = _service.Execute(run);

        Assert.Equal(1, summary.Failed);
        Assert.Equal(0, summary.Deleted);
        Assert.True(_registry.Has(RegistryScope.Machine, @"SOFTWARE\Foo"));
        Assert.Empty(_registry.Deleted);
        Assert.Equal(LeftoverState.Failed, run.Items[0].State);
        Assert.Contains(run.Log, l => l.Operation == LogOperation.BackupRegistry && l.Result == LogResult.Failed);
    }

    [Fact]
    public void Folders_go_to_the_recycle_bin()
    {
        var dir = _t.Dir("Program Files", "Foo");
        var run = new RemovalRun { Items = [FolderItem(dir)] };

        var summary = _service.Execute(run);

        Assert.Equal(1, summary.Deleted);
        Assert.Equal(new[] { dir }, _files.Removed);
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public void Protected_items_are_skipped_even_when_they_are_in_the_list()
    {
        var run = new RemovalRun
        {
            Items =
            [
                FolderItem(_programFiles),
                FolderItem(_t.Dir("Windows", "System32")),
                KeyItem(@"SOFTWARE\Microsoft"),
                KeyItem("SOFTWARE"),
            ],
        };
        _registry.AddMachine(@"SOFTWARE\Microsoft");

        var summary = _service.Execute(run);

        Assert.Equal(4, summary.Skipped);
        Assert.Empty(_files.Removed);
        Assert.Empty(_registry.Deleted);
        Assert.Empty(run.Backups);
        Assert.All(run.Items, i => Assert.Equal(LeftoverState.Skipped, i.State));
    }

    [Fact]
    public void Unselected_items_are_left_alone()
    {
        var dir = _t.Dir("Program Files", "Foo");
        _registry.AddMachine(@"SOFTWARE\Foo");
        var run = new RemovalRun { Items = [FolderItem(dir, selected: false), KeyItem(@"SOFTWARE\Foo", selected: false)] };

        var summary = _service.Execute(run);

        Assert.Equal(2, summary.Skipped);
        Assert.True(Directory.Exists(dir));
        Assert.True(_registry.Has(RegistryScope.Machine, @"SOFTWARE\Foo"));
    }

    [Fact]
    public void A_failing_item_does_not_stop_the_others()
    {
        var locked = _t.Dir("Program Files", "Locked");
        var free = _t.Dir("Program Files", "Free");
        _files.Failing.Add(locked);
        var run = new RemovalRun { Items = [FolderItem(locked), FolderItem(free)] };

        var summary = _service.Execute(run);

        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Deleted);
        Assert.Contains("acceso", run.Items[0].Message);
        Assert.Equal(LeftoverState.Failed, run.Items[0].State);
        Assert.Contains(run.Log, l => l.Operation == LogOperation.DeleteFolder && l.Result == LogResult.Failed);
    }

    [Fact]
    public void Things_that_no_longer_exist_are_skipped_not_failed()
    {
        var run = new RemovalRun { Items = [FolderItem(Path.Combine(_programFiles, "Fantasma")), KeyItem(@"SOFTWARE\Fantasma")] };

        var summary = _service.Execute(run);

        Assert.Equal(2, summary.Skipped);
        Assert.Equal(0, summary.Failed);
    }

    [Fact]
    public void Files_are_processed_before_registry_keys()
    {
        var dir = _t.Dir("Program Files", "Foo");
        _registry.AddMachine(@"SOFTWARE\Foo");
        var run = new RemovalRun { Items = [KeyItem(@"SOFTWARE\Foo"), FolderItem(dir)] };

        _service.Execute(run);

        var order = run.Log.Where(l => l.Operation is LogOperation.DeleteFolder or LogOperation.DeleteRegistryKey)
            .Select(l => l.Operation).ToList();
        Assert.Equal(new[] { LogOperation.DeleteFolder, LogOperation.DeleteRegistryKey }, order);
    }

    [Fact]
    public void Cancelling_stops_between_items_and_marks_the_run_cancelled()
    {
        var a = _t.Dir("Program Files", "A");
        var b = _t.Dir("Program Files", "B");
        var run = new RemovalRun { Items = [FolderItem(a), FolderItem(b)] };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var summary = _service.Execute(run, null, cts.Token);

        Assert.True(summary.Cancelled);
        Assert.Equal(RunStatus.Cancelled, run.Status);
        Assert.True(Directory.Exists(a));
        Assert.True(Directory.Exists(b));
    }

    [Fact]
    public void The_run_is_saved_while_it_progresses()
    {
        var dir = _t.Dir("Program Files", "Foo");
        var run = new RemovalRun { Items = [FolderItem(dir)] };

        _service.Execute(run);

        Assert.True(_store.Runs.ContainsKey(run.Id));
        Assert.Equal(RunStatus.Done, _store.Runs[run.Id].Status);
        Assert.NotNull(run.EndedAt);
    }

    [Fact]
    public void Restore_reimports_backups_newest_first_and_marks_them_restored()
    {
        _registry.AddMachine(@"SOFTWARE\A").AddMachine(@"SOFTWARE\B");
        var run = new RemovalRun { Items = [KeyItem(@"SOFTWARE\A"), KeyItem(@"SOFTWARE\B")] };
        _service.Execute(run);

        var summary = _backups.Restore(run);

        Assert.Equal(2, summary.Restored);
        Assert.Equal(0, summary.Failed);
        Assert.Equal(2, _registry.Imported.Count);
        Assert.StartsWith("002-", Path.GetFileName(_registry.Imported[0]));
        Assert.StartsWith("001-", Path.GetFileName(_registry.Imported[1]));
        Assert.All(run.Backups, b => Assert.NotNull(b.RestoredAt));

        // Una segunda vez no repite nada.
        Assert.Equal(0, _backups.Restore(run).Restored);
        Assert.Equal(2, _registry.Imported.Count);
    }

    [Fact]
    public void A_failed_restore_stays_pending_and_is_logged()
    {
        _registry.AddMachine(@"SOFTWARE\A");
        var run = new RemovalRun { Items = [KeyItem(@"SOFTWARE\A")] };
        _service.Execute(run);
        _registry.FailImport = true;

        var summary = _backups.Restore(run);

        Assert.Equal(1, summary.Failed);
        Assert.Null(run.Backups[0].RestoredAt);
        Assert.Contains(run.Log, l => l.Operation == LogOperation.RestoreRegistry && l.Result == LogResult.Failed);
    }
}

public class RunLogFormatterTests
{
    [Fact]
    public void Each_entry_becomes_one_line_with_result_operation_target_and_message()
    {
        var run = new RemovalRun();
        run.AddLog(LogOperation.DeleteFolder, @"C:\Foo", LogResult.Failed, "en uso");
        run.AddLog(LogOperation.Scan, "Foo", LogResult.Ok);

        var lines = RunLogFormatter.Format(run).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Contains("FAILED", lines[0]);
        Assert.Contains(@"delete_folder  C:\Foo  —  en uso", lines[0]);
        Assert.Contains("scan  Foo", lines[1]);
    }
}
