using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using Quitar.Core.Leftovers;

namespace Quitar.Desktop;

/// <summary>Una fila de la revisión de restos: el elemento y sus textos listos para mostrar.</summary>
public sealed class LeftoverRow : INotifyPropertyChanged
{
    private bool _isEditable = true;

    public LeftoverRow(LeftoverItem item) => Item = item;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LeftoverItem Item { get; }

    public bool IsChecked
    {
        get => Item.Selected;
        set
        {
            if (Item.Selected == value)
            {
                return;
            }

            Item.Selected = value;
            Raise();
        }
    }

    /// <summary>Solo se puede marcar y desmarcar mientras se revisa, no durante ni después del borrado.</summary>
    public bool IsEditable
    {
        get => _isEditable;
        set
        {
            if (_isEditable == value)
            {
                return;
            }

            _isEditable = value;
            Raise();
        }
    }

    public string KindText => Item.Kind switch
    {
        LeftoverKind.Folder => Strings.KindFolder,
        LeftoverKind.File => Strings.KindFile,
        LeftoverKind.Shortcut => Strings.KindShortcut,
        _ => Strings.KindRegistryKey,
    };

    public string RiskText => Item.Risk switch
    {
        LeftoverRisk.Low => Strings.RiskLow,
        LeftoverRisk.Medium => Strings.RiskMedium,
        _ => Strings.RiskHigh,
    };

    public string Path => Item.Path;

    public string Reason => Item.Reason;

    public string ResultText => Item.State switch
    {
        LeftoverState.Deleted => Strings.ResultDeleted,
        LeftoverState.Failed => string.Format(CultureInfo.CurrentCulture, Strings.ResultFailed, Item.Message),
        LeftoverState.Skipped => string.Format(CultureInfo.CurrentCulture, Strings.ResultSkipped, Item.Message),
        _ => "",
    };

    /// <summary>Después de borrar: vuelve a leer el estado del elemento.</summary>
    public void Refresh()
    {
        Raise(nameof(IsChecked));
        Raise(nameof(ResultText));
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
