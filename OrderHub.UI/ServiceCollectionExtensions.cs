using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Notification.Wpf;
using OrderHub.Application;
using OrderHub.Domain;
using OrderHub.Infrastructure;
using OrderHub.UI.StartUpSteps;

namespace OrderHub.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUI(this IServiceCollection services)
    {
        // Services
        services.AddStartUpSteps();
        services.AddSingleton<Application.Interfaces.Services.INotifier, Services.Notifier>();
        services.AddSingleton<Application.Interfaces.Services.IAppState, Services.AppState>();
        services.AddSingleton<Interfaces.INavigationService, Services.NavigationService>();
        services.AddSingleton<Interfaces.IDialogService, Services.DialogService>();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddSingleton<Application.Interfaces.Services.INotificationService, Services.NotificationService>();

        services.AddSingleton<INotificationManager, NotificationManager>();

        services.AddSingleton(typeof(Interfaces.ISelectionStore<,>), typeof(Stores.SelectionStore<,>));

        services.AddViewModels();
        services.AddViews();

        return services;
    }

    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {

        services.AddSingleton<Features.MainWindow.MainWindowViewModel>();
        services.AddSingleton<Features.Home.HomeViewModel>();
        services.AddTransient<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsViewModel>();
        services.AddSingleton<Features.Suppliers.Index.SuppliersViewModel>();
        services.AddTransient<Features.Cities.Create.CreateCityViewModel>();

        services.AddTransient<Features.Suppliers.Create.ViewModel>();
        services.AddTransient<Features.Suppliers.Update.ViewModel>();

        // Clients
        services.AddSingleton<Features.Clients.Index.ViewModel>();
        services.AddTransient<Features.Clients.Create.ViewModel>();
        services.AddTransient<Features.Clients.Update.ViewModel>();

        // Categories
        services.AddSingleton<Features.Categories.Index.ViewModel>();
        services.AddTransient<Features.Categories.Create.ViewModel>();
        services.AddTransient<Features.Categories.Update.ViewModel>();

        // Products
        services.AddSingleton<Features.Products.Index.ViewModel>();
        services.AddTransient<Features.Products.Create.ViewModel>();
        services.AddTransient<Features.Products.Update.ViewModel>();

        // Deliverymen
        services.AddSingleton<Features.Deliverymen.Index.ViewModel>();
        services.AddTransient<Features.Deliverymen.Create.ViewModel>();
        services.AddTransient<Features.Deliverymen.Edit.ViewModel>();

        // ShippingCarriers
        services.AddSingleton<Features.ShippingCarriers.Index.ViewModel>();
        services.AddTransient<Features.ShippingCarriers.Create.ViewModel>();
        services.AddTransient<Features.ShippingCarriers.Edit.ViewModel>();

        // Orders
        services.AddSingleton<Features.Orders.Index.ViewModel>();
        services.AddTransient<Features.Orders.Create.ViewModel>();
        services.AddTransient<Features.Orders.Edit.ViewModel>();

        // Messages
        services.AddSingleton<Features.Messages.Index.ViewModel>();

        return services;
    }

    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddSingleton<Features.NotFoundView>();
        services.AddSingleton<Features.MainWindow.MainWindowView>();
        services.AddTransient<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsView>();
        services.AddSingleton<Features.Home.HomeView>();
        services.AddSingleton<Features.Splash>();

        // Suppliers
        services.AddSingleton<Features.Suppliers.Index.View>();

        // ❌ To be removed
        services.AddTransient<Features.Suppliers.Create.View>();
        services.AddTransient<Features.Suppliers.Update.View>();
        services.AddTransient<Features.Cities.Create.CreateCityView>();

        services.AddTransient<Features.Suppliers.Update.View>();

        // Clients
        services.AddSingleton<Features.Clients.Index.View>();
        services.AddTransient<Features.Clients.Create.View>();
        services.AddTransient<Features.Clients.Update.View>();

        // Categories
        services.AddSingleton<Features.Categories.Index.View>();
        services.AddTransient<Features.Categories.Create.View>();
        services.AddTransient<Features.Categories.Update.View>();

        // Products
        services.AddSingleton<Features.Products.Index.View>();
        services.AddTransient<Features.Products.Create.View>();
        services.AddTransient<Features.Products.Update.View>();

        // Deliverymen
        services.AddSingleton<Features.Deliverymen.Index.View>();
        services.AddTransient<Features.Deliverymen.Create.View>();
        services.AddTransient<Features.Deliverymen.Edit.View>();

        // ShippingCarriers
        services.AddSingleton<Features.ShippingCarriers.Index.View>();
        services.AddTransient<Features.ShippingCarriers.Create.View>();
        services.AddTransient<Features.ShippingCarriers.Edit.View>();

        // Orders
        services.AddSingleton<Features.Orders.Index.View>();
        services.AddTransient<Features.Orders.Create.View>();
        services.AddTransient<Features.Orders.Edit.View>();

        // Messages
        services.AddSingleton<Features.Messages.Index.View>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddDomain();
        services.AddApplication();
        services.AddInfrastructure();
        services.AddUI();
        return services;
    }
}
