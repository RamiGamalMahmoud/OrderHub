using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Commands;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace OrderHub.Tests.UI;

public class FeatureCommandAuthorizationTests
{
    [Fact]
    public void SingletonCommandsRefreshForEachUserPermissionChangeAndLogout()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddCommands();
        services.AddSingleton(Mock.Of<IRequestExecutor>());
        services.AddSingleton(Mock.Of<IApplicationDirectoriesService>());
        using var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<IApplicationContext>();
        var orders = provider.GetRequiredService<OrdersCommands>();
        var categories = provider.GetRequiredService<CategoriesCommands>();
        var documents = provider.GetRequiredService<DocumentsCommands>();

        (ICommand Command, object Parameter, PermissionFeature Feature, bool Write)[] commands =
        [
            (orders.OpenCommand, null, PermissionFeature.Orders, false),
            (orders.CreateCommand, null, PermissionFeature.Orders, true),
            (orders.EditCommand, 1, PermissionFeature.Orders, true),
            (orders.DeleteCommand, 1, PermissionFeature.Orders, true),
            (orders.BroadcastOrderCommand, 1, PermissionFeature.Orders, true),
            (orders.BroadcastClientCommand, 1, PermissionFeature.Orders, true),
            (orders.BroadcastSupplierCommand, 1, PermissionFeature.Orders, true),
            (orders.BroadcastShippingCarrierCommand, 1, PermissionFeature.Orders, true),
            (orders.BroadcastDeliverymanCommand, 1, PermissionFeature.Orders, true),
            (categories.OpenCommand, null, PermissionFeature.Categories, false),
            (categories.CreateCommand, null, PermissionFeature.Categories, true),
            (categories.EditCommand, 1, PermissionFeature.Categories, true),
            (categories.DeleteCommand, 1, PermissionFeature.Categories, true),
            (documents.GenerateInvoiceCommand, 1, PermissionFeature.Documents, true),
            (documents.GenerateDeliveryNoteCommand, 1, PermissionFeature.Documents, true),
            (provider.GetRequiredService<ProductsCommands>().OpenCommand, null, PermissionFeature.Products, false),
            (provider.GetRequiredService<CitiesCommands>().OpenCommand, null, PermissionFeature.Cities, false),
            (provider.GetRequiredService<ClientsCommands>().OpenCommand, null, PermissionFeature.Clients, false),
            (provider.GetRequiredService<SuppliersCommands>().OpenCommand, null, PermissionFeature.Suppliers, false),
            (provider.GetRequiredService<ShippingCarriersCommands>().OpenCommand, null, PermissionFeature.ShippingCarriers, false),
            (provider.GetRequiredService<DeliverymenCommands>().OpenCommand, null, PermissionFeature.Deliverymen, false),
            (provider.GetRequiredService<WhatsappGroupsCommands>().OpenCommand, null, PermissionFeature.WhatsAppGroups, false),
            (provider.GetRequiredService<MessagesCommands>().OpenCommand, null, PermissionFeature.Messages, false),
            (provider.GetRequiredService<SettingsCommands>().OpenCommand, null, PermissionFeature.Settings, false),
            (provider.GetRequiredService<UsersCommands>().OpenCommand, null, PermissionFeature.Users, false),
        ];

        var notifications = new Dictionary<ICommand, int>();
        foreach (var entry in commands)
        {
            notifications[entry.Command] = 0;
            entry.Command.CanExecuteChanged += (_, _) => notifications[entry.Command]++;
            Assert.False(entry.Command.CanExecute(entry.Parameter));
        }

        int expectedNotifications = 0;
        foreach (var feature in Enum.GetValues<PermissionFeature>())
        {
            foreach (bool canWrite in new[] { false, true })
            {
                context.SetUser(new UserSession(1, "user", "User", false,
                    [new UserPermissionSession(feature, true, canWrite)]));
                expectedNotifications++;
                foreach (var entry in commands)
                {
                    Assert.Equal(expectedNotifications, notifications[entry.Command]);
                    Assert.Equal(entry.Feature == feature && (!entry.Write || canWrite),
                        entry.Command.CanExecute(entry.Parameter));
                }
            }
        }

        context.SetUser(new UserSession(2, "admin", "Admin", true, []));
        expectedNotifications++;
        foreach (var entry in commands)
        {
            Assert.Equal(expectedNotifications, notifications[entry.Command]);
            Assert.True(entry.Command.CanExecute(entry.Parameter));
        }

        context.ClearUser();
        expectedNotifications++;
        foreach (var entry in commands)
        {
            Assert.Equal(expectedNotifications, notifications[entry.Command]);
            Assert.False(entry.Command.CanExecute(entry.Parameter));
        }

        Assert.Same(orders, provider.GetRequiredService<OrdersCommands>());
        Assert.Same(categories, provider.GetRequiredService<CategoriesCommands>());
        Assert.Same(documents, provider.GetRequiredService<DocumentsCommands>());
        Assert.True(provider.GetRequiredService<DashboardCommands>().OpenCommand.CanExecute(null));
    }
}
