using System.ComponentModel;
using System.Globalization;
using System.Windows;
using Quitar.Core.Programs;

namespace Quitar.Desktop;

public partial class UninstallWindow : Window
{
    private readonly UninstallViewModel _viewModel;

    public UninstallWindow(InstalledProgram program, AppServices services)
    {
        InitializeComponent();

        _viewModel = new UninstallViewModel(program, services);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.StartAsync();
    }

    private void StopWaiting_Click(object sender, RoutedEventArgs e) => _viewModel.StopWaiting();

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var (disk, keys, highRisk) = _viewModel.CountSelected();
        if (disk + keys == 0)
        {
            return;
        }

        var text = string.Format(
            CultureInfo.CurrentCulture,
            Strings.ConfirmDelete,
            disk,
            keys,
            highRisk ? Strings.ConfirmDeleteHighRisk : "");

        if (MessageBox.Show(this, text, Strings.AppTitle, MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
        {
            return;
        }

        await _viewModel.DeleteAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_viewModel.IsBusy)
        {
            e.Cancel = true;
            MessageBox.Show(this, Strings.BusyClosing, Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _viewModel.AbandonReview();
    }
}
