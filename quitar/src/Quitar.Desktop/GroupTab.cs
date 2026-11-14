using System.ComponentModel;
using Quitar.Core.Programs;

namespace Quitar.Desktop;

/// <summary>Una pestaña del selector de grupos, con la cantidad de coincidencias.</summary>
public sealed class GroupTab : INotifyPropertyChanged
{
    private int _count;

    public GroupTab(ProgramGroup group, string title)
    {
        Group = group;
        Title = title;
    }

    public ProgramGroup Group { get; }

    public string Title { get; }

    public int Count
    {
        get => _count;
        set
        {
            if (_count == value)
            {
                return;
            }

            _count = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
        }
    }

    public string Label => $"{Title} ({Count})";

    public event PropertyChangedEventHandler? PropertyChanged;
}
