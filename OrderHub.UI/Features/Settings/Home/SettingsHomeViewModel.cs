using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using OrderHub.UI.Services;
using System;
using System.Collections.ObjectModel;

namespace OrderHub.UI.Features.Settings.Home;

public partial class SettingsHomeViewModel : ObservableObject
{
    public ObservableCollection<SettingsItemViewModel> Items { get; private set; } = [];

    public SettingsHomeViewModel()
    {
        Items.Add(new SettingsItemViewModel(
            MaterialIconKind.PropertyTag,
            "خصائص المنتجات",
            "عرض و تعديل خصائص المنتجات", () =>
            NavigationService.Instance.NavigateTo<Features.Settings.Properties.PropertiesView>()));
    }
}

public sealed class SettingsItemViewModel
{
    public MaterialIconKind Icon { get; }

    public string Title { get; }

    public string Description { get; }

    public IRelayCommand Command { get; }

    public SettingsItemViewModel(
        MaterialIconKind icon,
        string title,
        string description,
        Action execute)
    {
        Icon = icon;
        Title = title;
        Description = description;

        Command = new RelayCommand(execute);
    }
}