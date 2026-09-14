using Microsoft.Extensions.DependencyInjection;

namespace OrderHub.UI.DependencyInjection;
internal static class ViewModelRegistration
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {

        services.AddTransient<Features.MainWindow.MainWindowViewModel>();
        services.AddSingleton<Features.Orders.Index.OrderDraftsDrawerViewModel>();
        services.AddSingleton<Features.Home.HomeViewModel>();
        services.AddTransient<Features.Settings.ClientCredentialsSettings.ClientCredentialsSettingsViewModel>();
        services.AddSingleton<Features.Suppliers.Index.SuppliersViewModel>();
        services.AddSingleton<Features.Cities.Index.ViewModel>();
        services.AddTransient<Features.Cities.Create.CreateCityViewModel>();
        services.AddTransient<Features.Cities.Edit.ViewModel>();

        // Dashboard
        services.AddSingleton<Features.Dashboard.DashboardViewModel>();

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
        services.AddTransient<Features.Messages.SendMessage.SendMessageViewModel>();

        // Documents
        services.AddTransient<Features.Documents.Preview.PreviewDocumentViewModel>();

        // Whatsapp
        services.AddSingleton<Features.WhatsappGroups.Index.WhatsappGroupsViewModel>();
        services.AddTransient<Features.WhatsappGroups.Create.ViewModel>();
        services.AddTransient<Features.WhatsappGroups.Update.ViewModel>();

        // Settings
        services.AddSingleton<Features.Settings.Home.SettingsHomeViewModel>();
        services.AddSingleton<Features.Settings.Properties.PropertiesViewModel>();

        // Database Configuration
        services.AddTransient<Features.DatabaseConfiguration.DatabaseConfigurationViewModel>();

        // Authentication
        services.AddTransient<Features.Authentication.ApiLogin.ApiLoginViewModel>();
        services.AddTransient<Features.Authentication.DatabaseLogin.DatabaseLoginViewModel>();

        // Administration
        services.AddTransient<Features.Administration.Admin.CreateAdmin.CreateAdminViewModel>();

        // Administration | Users
        services.AddSingleton<Features.Administration.Users.List.UsersViewModel>();
        services.AddTransient<Features.Administration.Users.Details.UserDetailsViewModel>();
        services.AddTransient<Features.Administration.Users.Create.CreateUserViewModel>();
        services.AddTransient<Features.Administration.Users.Update.UpdateUserViewModel>();

        return services;
    }
}
