using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Quitar.Core.Removal;

namespace Quitar.Desktop;

public partial class HistoryWindow : Window
{
    private readonly AppServices _services;

    public HistoryWindow(AppServices services)
    {
        InitializeComponent();
        _services = services;
        Reload();
    }

    private HistoryRow? Selected => RunsList.SelectedItem as HistoryRow;

    private void Reload()
    {
        var rows = _services.Store.List().Select(r => new HistoryRow(r)).ToList();
        RunsList.ItemsSource = rows;
        LogBox.Text = rows.Count == 0 ? Strings.HistoryEmpty : "";
        RestoreButton.IsEnabled = false;
    }

    private void RunsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var row = Selected;
        LogBox.Text = row is null ? "" : RunLogFormatter.Format(row.Run);
        RestoreButton.IsEnabled = row is { PendingBackups: > 0 };
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var row = Selected;
        if (row is null || row.PendingBackups == 0)
        {
            return;
        }

        var ask = string.Format(CultureInfo.CurrentCulture, Strings.ConfirmRestore, row.PendingBackups, row.Run.ProgramName);
        if (MessageBox.Show(this, ask, Strings.AppTitle, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
        {
            return;
        }

        RestoreButton.IsEnabled = false;
        var run = row.Run;
        RestoreSummary summary;
        try
        {
            summary = await Task.Run(() =>
            {
                var result = _services.Backups.Restore(run);
                _services.Store.Save(run);
                return result;
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, Strings.UnexpectedError, ex.Message),
                Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            Reload();
            return;
        }

        Reload();
        MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, Strings.RestoreResult, summary.Restored, summary.Failed),
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
