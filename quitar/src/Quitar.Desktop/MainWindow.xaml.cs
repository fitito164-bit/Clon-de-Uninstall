using System.Windows;
using System.Windows.Input;
using Quitar.Core.Programs;
using Quitar.Core.Registry;
using Quitar.Core.Uninstall;
using System.Globalization;

namespace Quitar.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(() => ProgramCatalog.Load(new WindowsUninstallSource()));
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            SearchBox.Focus();
            await _viewModel.LoadAsync();
        };
    }

    /// <summary>F5 actualiza la lista y Esc limpia el filtro.</summary>
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            if (_viewModel.RefreshCommand.CanExecute(null))
            {
                _viewModel.RefreshCommand.Execute(null);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Enter && ProgramsGrid.IsKeyboardFocusWithin && UninstallButton.IsEnabled)
        {
            Uninstall_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && !string.IsNullOrEmpty(_viewModel.SearchText))
        {
            _viewModel.SearchText = "";
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    /// <summary>Escribir en cualquier lugar manda el texto al buscador.</summary>
    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (SearchBox.IsKeyboardFocused || string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0]))
        {
            return;
        }

        SearchBox.Focus();
        SearchBox.Text += e.Text;
        SearchBox.CaretIndex = SearchBox.Text.Length;
        e.Handled = true;
    }

    private void ProgramsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        UninstallButton.IsEnabled = ProgramsGrid.SelectedItems.Count == 1;

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        if (ProgramsGrid.SelectedItems.Count != 1 || ProgramsGrid.SelectedItem is not ProgramRow row)
        {
            return;
        }

        var program = row.Program;
        if (UninstallCommandParser.For(program) is null)
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, Strings.NoUninstaller, program.Name),
                Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var text = string.Format(CultureInfo.CurrentCulture, Strings.ConfirmUninstall, program.Name);
        if (program.Group != ProgramGroup.Apps)
        {
            text += Strings.ConfirmSystemComponent;
        }

        if (MessageBox.Show(this, text, Strings.AppTitle, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
        {
            return;
        }

        new UninstallWindow(program, App.Services) { Owner = this }.ShowDialog();
        await _viewModel.LoadAsync();
    }

    private void History_Click(object sender, RoutedEventArgs e) =>
        new HistoryWindow(App.Services) { Owner = this }.ShowDialog();
}
