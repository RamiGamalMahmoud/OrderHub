using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrderHub.UI.Features.MainWindow;
internal partial class MainWindowCommands : ObservableObject
{
    public MainWindowCommands()
    {
        InitializeNavigationCommands();
    }
    public ObservableCollection<MainWindowCommand> NavigationCommands { get; } = [];
    [ObservableProperty]
    private IEnumerable<MainWindowCommand> _childrenCommands;

    [ObservableProperty]
    private MainWindowCommand _selectedCommand;

    partial void OnSelectedCommandChanged(MainWindowCommand value)
    {
        if (value is null)
            return;

        value.Action?.Invoke();
        ChildrenCommands = value.ChildrenCommands;
    }

    private void InitializeNavigationCommands()
    {
        NavigationCommands.Add(new MainWindowCommand(
            "لوحة التحكم",
            "ViewDashboard",
            () => NavigationService.Instance.NavigateTo<Features.Dashboard.DashboardView>()));

        NavigationCommands.Add(new MainWindowCommand(
            "الطلبات",
            "ListBox",
            () => NavigationService.Instance.NavigateTo<Features.Orders.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "المنتجات",
            "Factory",
            () => NavigationService.Instance.NavigateTo<Features.Products.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "الأقسام",
            "Factory",
            () => NavigationService.Instance.NavigateTo<Features.Categories.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "المدن",
            "MapMarker",
            () => NavigationService.Instance.NavigateTo<Features.Cities.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "العملاء",
            "AccountTie",
            () => NavigationService.Instance.NavigateTo<Features.Clients.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "الموردون",
            "BadgeAccount",
            () => NavigationService.Instance.NavigateTo<Features.Suppliers.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "شركات الشحن",
            "Ship",
            () => NavigationService.Instance.NavigateTo<Features.ShippingCarriers.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "المناديب",
            "Moped",
            () => NavigationService.Instance.NavigateTo<Features.Deliverymen.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "مجموعات الواتساب",
            "Whatsapp",
            () => NavigationService.Instance.NavigateTo<Features.WhatsappGroups.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "سجل الرسائل",
            "MessageMinusOutline",
            () => NavigationService.Instance.NavigateTo<Features.Messages.Index.View>()));

        NavigationCommands.Add(new MainWindowCommand(
            "الإعدادات",
            "CogOutline",
            () => NavigationService.Instance.NavigateTo<Features.Settings.Home.SettingsHomeView>()));

        NavigationCommands.Add(new MainWindowCommand(
            "إدارة المستخدمون",
            "",
            () => 
            NavigationService.Instance.NavigateTo<Features.Administration.Users.List.UsersView>()));
    }

}
