using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace OrderHub.UI.Features.Settings.Home;

public partial class SettingsHomeViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public ObservableCollection<SettingsItemViewModel> Items { get; private set; } = new ObservableCollection<SettingsItemViewModel>();

    public SettingsHomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Items.Add( new SettingsItemViewModel(
            MaterialIconKind.PropertyTag, 
            "خصائص المنتجات", 
            "عرض و تعديل خصائص المنتجات", () =>
            _navigationService.NavigateTo<Features.Settings.Properties.PropertiesView>()));
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