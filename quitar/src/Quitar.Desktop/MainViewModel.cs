using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Quitar.Core.Programs;

namespace Quitar.Desktop;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly Func<IReadOnlyList<InstalledProgram>> _loader;
    private IReadOnlyList<InstalledProgram> _all = [];
    private IReadOnlyList<ProgramRow> _rows = [];
    private GroupTab _selectedTab;
    private string _searchText = "";
    private string _statusText = Strings.Loading;
    private bool _isLoading;
    private long _loadMilliseconds;

    public MainViewModel(Func<IReadOnlyList<InstalledProgram>> loader)
    {
        _loader = loader;
        Tabs =
        [
            new GroupTab(ProgramGroup.Apps, Strings.GroupApps),
            new GroupTab(ProgramGroup.System, Strings.GroupSystem),
            new GroupTab(ProgramGroup.Hidden, Strings.GroupHidden),
        ];
        _selectedTab = Tabs[0];
        RefreshCommand = new RelayCommand(() => _ = LoadAsync(), () => !_isLoading);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<GroupTab> Tabs { get; }

    public ICommand RefreshCommand { get; }

    public GroupTab SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (value is null || ReferenceEquals(_selectedTab, value))
            {
                return;
            }

            _selectedTab = value;
            OnPropertyChanged();
            Apply();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value)
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            Apply();
        }
    }

    public IReadOnlyList<ProgramRow> Rows
    {
        get => _rows;
        private set
        {
            _rows = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        StatusText = Strings.Loading;
        try
        {
            var watch = Stopwatch.StartNew();
            _all = await Task.Run(_loader);
            _loadMilliseconds = watch.ElapsedMilliseconds;
            Apply();
        }
        catch (Exception e)
        {
            StatusText = string.Format(CultureInfo.CurrentCulture, Strings.LoadError, e.Message);
        }
        finally
        {
            _isLoading = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void Apply()
    {
        var counts = ProgramFilter.CountByGroup(_all, _searchText);
        foreach (var tab in Tabs)
        {
            tab.Count = counts.GetValueOrDefault(tab.Group);
        }

        var found = ProgramFilter.Apply(_all, _searchText, _selectedTab.Group);
        Rows = found.Select(p => new ProgramRow(p)).ToList();
        StatusText = string.Format(
            CultureInfo.CurrentCulture, Strings.StatusFormat, found.Count, _all.Count, _loadMilliseconds);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
