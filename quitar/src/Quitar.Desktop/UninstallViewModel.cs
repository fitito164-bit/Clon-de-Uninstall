using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Quitar.Core.Leftovers;
using Quitar.Core.Programs;
using Quitar.Core.Removal;

namespace Quitar.Desktop;

/// <summary>Estado de la ventana de desinstalación: preparar, revisar restos, borrar, ver el registro.</summary>
public sealed class UninstallViewModel : INotifyPropertyChanged
{
    private readonly InstalledProgram _program;
    private readonly AppServices _services;
    private readonly CancellationTokenSource _stopWaiting = new();

    private RemovalRun? _run;
    private IReadOnlyList<LeftoverRow> _rows = [];
    private string _status = Strings.Starting;
    private string _logText = "";
    private bool _isBusy;
    private bool _canStopWaiting;
    private bool _canDelete;
    private bool _showLog;

    public UninstallViewModel(InstalledProgram program, AppServices services)
    {
        _program = program;
        _services = services;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title => string.Format(CultureInfo.CurrentCulture, Strings.UninstallWindowTitle, _program.Name);

    public IReadOnlyList<LeftoverRow> Rows
    {
        get => _rows;
        private set => Set(ref _rows, value);
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public string LogText
    {
        get => _logText;
        private set => Set(ref _logText, value);
    }

    /// <summary>Hay una operación en curso: la ventana no se puede cerrar.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => Set(ref _isBusy, value);
    }

    public bool CanStopWaiting
    {
        get => _canStopWaiting;
        private set => Set(ref _canStopWaiting, value);
    }

    /// <summary>Hay restos para revisar y todavía no se borraron.</summary>
    public bool CanDelete
    {
        get => _canDelete;
        private set
        {
            if (Set(ref _canDelete, value))
            {
                Raise(nameof(ShowLeftovers));
            }
        }
    }

    public bool ShowLeftovers => _rows.Count > 0;

    public bool ShowLog
    {
        get => _showLog;
        private set => Set(ref _showLog, value);
    }

    public async Task StartAsync()
    {
        IsBusy = true;
        CanStopWaiting = true;
        var progress = new Progress<string>(message => Status = message);

        try
        {
            var result = await _services.Workflow.PrepareAsync(_program, progress, _stopWaiting.Token);
            _run = result.Run;

            switch (result.Status)
            {
                case PrepareStatus.ReadyToReview:
                    Rows = result.Run.Items.Select(i => new LeftoverRow(i)).ToList();
                    Raise(nameof(ShowLeftovers));
                    CanDelete = true;
                    Status = string.Format(CultureInfo.CurrentCulture, Strings.ReviewHint, _program.Name);
                    break;

                default:
                    Status = result.Message ?? "";
                    ShowTheLog();
                    break;
            }
        }
        catch (Exception e)
        {
            Status = string.Format(CultureInfo.CurrentCulture, Strings.UnexpectedError, e.Message);
            ShowTheLog();
        }
        finally
        {
            CanStopWaiting = false;
            IsBusy = false;
        }
    }

    /// <summary>Deja de esperar al desinstalador: no lo cierra, solo sigue con lo que haya quedado en el registro.</summary>
    public void StopWaiting() => _stopWaiting.Cancel();

    /// <summary>Cuántos elementos hay marcados de cada tipo y si alguno es de riesgo alto (para el aviso de confirmación).</summary>
    public (int Disk, int Keys, bool HasHighRisk) CountSelected()
    {
        var selected = _rows.Where(r => r.IsChecked).Select(r => r.Item).ToList();
        var keys = selected.Count(i => i.Kind == LeftoverKind.RegistryKey);
        return (selected.Count - keys, keys, selected.Any(i => i.Risk == LeftoverRisk.High));
    }

    public async Task DeleteAsync()
    {
        if (_run is null || !CanDelete)
        {
            return;
        }

        CanDelete = false;
        IsBusy = true;
        Status = Strings.DeletingStatus;
        foreach (var row in _rows)
        {
            row.IsEditable = false;
        }

        var run = _run;
        var progress = new Progress<string>(message => Status = Strings.DeletingStatus + " " + message);
        try
        {
            var summary = await Task.Run(() => _services.Removal.Execute(run, progress));
            foreach (var row in _rows)
            {
                row.Refresh();
            }

            Status = string.Format(CultureInfo.CurrentCulture, Strings.DoneStatus, summary.Deleted, summary.Failed, summary.Skipped);
        }
        catch (Exception e)
        {
            run.Finish(RunStatus.Failed, e.Message);
            _services.Store.Save(run);
            Status = string.Format(CultureInfo.CurrentCulture, Strings.UnexpectedError, e.Message);
        }
        finally
        {
            ShowTheLog();
            IsBusy = false;
        }
    }

    /// <summary>Se cerró la ventana sin borrar: la corrida queda como cancelada en el historial.</summary>
    public void AbandonReview()
    {
        if (_run is { Status: RunStatus.Reviewing } run)
        {
            run.Finish(RunStatus.Cancelled, "Se cerró la ventana antes de borrar los restos.");
            _services.Store.Save(run);
        }
    }

    private void ShowTheLog()
    {
        if (_run is not null)
        {
            LogText = RunLogFormatter.Format(_run);
        }

        ShowLog = true;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Raise(name);
        return true;
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
