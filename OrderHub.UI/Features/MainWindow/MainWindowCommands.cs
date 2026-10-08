using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Context;
using OrderHub.UI.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrderHub.UI.Features.MainWindow;
internal partial class MainWindowCommands : ObservableObject
{
    private readonly IApplicationContext _applicationContext;
    private readonly IAppCommands _appCommands;

    public MainWindowCommands(
        IApplicationContext applicationContext,
        IAppCommands appCommands)
    {
        _applicationContext = applicationContext;
        _appCommands = appCommands;

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

        if (value.Command.CanExecute(null))
            value.Command.Execute(null);
        ChildrenCommands = value.ChildrenCommands;
    }

    private void InitializeNavigationCommands()
    {
        NavigationCommands.Add(new MainWindowCommand(
            "لوحة التحكم",
            "ViewDashboard",
            _appCommands.Dashboard.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "الطلبات",
            "ListBox",
            _appCommands.Orders.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "المنتجات",
            "Factory",
            _appCommands.Products.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "الأقسام",
            "Factory",
            _appCommands.Categories.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "المدن",
            "MapMarker",
            _appCommands.Cities.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "العملاء",
            "AccountTie",
            _appCommands.Clients.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "الموردون",
            "BadgeAccount",
            _appCommands.Suppliers.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "شركات الشحن",
            "Ship",
            _appCommands.ShippingCarriers.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "المناديب",
            "Moped",
            _appCommands.Deliverymen.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "مجموعات الواتساب",
            "Whatsapp",
            _appCommands.WhatsappGroups.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "سجل الرسائل",
            "MessageMinusOutline",
            _appCommands.Messages.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "الإعدادات",
            "CogOutline",
            _appCommands.Settings.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "إدارة المستخدمون",
            "AccountGroup",
            _appCommands.Users.OpenCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "تشغيل السيرفر",
            "ServerNetwork",
            _appCommands.ApiServer.RunCommand));

        NavigationCommands.Add(new MainWindowCommand(
            "إيقاف السيرفر",
            "ServerNetworkOff",
            _appCommands.ApiServer.StopCommand));
    }

}
