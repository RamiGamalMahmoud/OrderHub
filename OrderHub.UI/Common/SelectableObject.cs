using CommunityToolkit.Mvvm.ComponentModel;

namespace OrderHub.UI.Common;

public partial class SelectableObject<T> : ObservableObject
{
    public SelectableObject(T value)
    {
        Value = value;
    }
    public T Value { get; }

    [ObservableProperty]
    private bool _isSelected;
}
