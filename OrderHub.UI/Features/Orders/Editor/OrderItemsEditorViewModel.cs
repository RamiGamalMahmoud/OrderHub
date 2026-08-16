using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class OrderItemsEditorViewModel : ObservableObject
{
    public IEnumerable<OrderItemViewModel> Items => _items;
    private readonly ObservableCollection<OrderItemViewModel> _items = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectNextCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectPreviousCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveItemCommand))]
    private OrderItemViewModel _selectedItem;

    [ObservableProperty]
    private decimal _totalPrice;

    public int ItemsCount => _items.Count;
    public bool IsValid => _items.Count > 0 && _items.All(i => i.IsValid);

    public OrderItemsEditorViewModel()
    {
        _items.CollectionChanged += (_, _) => ItemsCollectionChanged();
    }

    public void Add(OrderItemViewModel item)
    {
        if (item is null)
            return;

        _items.Add(item);
        item.PropertyChanged += Item_PropertyChanged;
        SelectedItem = item;
    }

    private void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        TotalPrice = _items.Sum(x => x.SubTotal);
        OnPropertyChanged();
    }

    public void Remove(OrderItemViewModel item)
    {
        if (item is null)
            return;

        _items.Remove(item);
        item.PropertyChanged -= Item_PropertyChanged;

        if (ReferenceEquals(SelectedItem, item))
        {
            SelectedItem = _items.LastOrDefault();
        }
    }

    public void Clear()
    {
        _items.Clear();
        SelectedItem = null;
    }

    private void ItemsCollectionChanged()
    {
        TotalPrice = _items.Sum(x => x.SubTotal);
        OnPropertyChanged(nameof(ItemsCount));
    }

    [RelayCommand(CanExecute = nameof(CanSelectNext))]
    private void SelectNext()
    {
        int index = _items.IndexOf(SelectedItem);

        if (index >= 0 && index < _items.Count - 1)
        {
            SelectedItem = _items[index + 1];
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectPrevious))]
    private void SelectPrevious()
    {
        int index = _items.IndexOf(SelectedItem);

        if (index > 0)
        {
            SelectedItem = _items[index - 1];
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveItem))]
    private void RemoveItem(OrderItemViewModel item)
    {
        Remove(item);
    }

    private bool CanSelectNext()
    {
        int index = _items.IndexOf(SelectedItem);
        return index >= 0 && index < _items.Count - 1;
    }

    private bool CanSelectPrevious()
    {
        int index = _items.IndexOf(SelectedItem);
        return index > 0;
    }

    private bool CanRemoveItem(OrderItemViewModel item)
        => item is not null;
}