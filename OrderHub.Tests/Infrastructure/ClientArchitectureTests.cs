using System;
using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Components;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Clients.Editor;
using OrderHub.Web.Components.Pages.Features.Clients;

namespace OrderHub.Tests.Infrastructure;

public class ClientArchitectureTests
{
    [Fact]
    public void Every_client_request_has_explicit_feature_permission()
    {
        var requests = typeof(CreateClientCommand).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(CreateClientCommand).Namespace &&
                type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToArray();
        Assert.Equal(10, requests.Length);
        foreach (var request in requests)
        {
            var permission = request.GetCustomAttribute<RequirePermissionAttribute>();
            Assert.NotNull(permission);
            Assert.Equal(PermissionFeature.Clients, permission.Feature);
            bool needsWrite = request.Name.EndsWith("Command") || request == typeof(GetClientEditorCitiesQuery);
            Assert.Equal(needsWrite ? PermissionLevel.Write : PermissionLevel.Read, permission.RequiredLevel);
        }
    }

    [Fact]
    public void Client_components_execute_use_cases_without_persistence_injection()
    {
        foreach (var component in new[] { typeof(OrderHub.Web.Components.Pages.Features.Clients.Index), typeof(ClientEditorDialog) })
        {
            var injected = component.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(property => property.GetCustomAttribute<InjectAttribute>() is not null).ToArray();
            Assert.Contains(injected, property => property.PropertyType == typeof(OrderHub.Application.Interfaces.IRequestExecutor));
            Assert.DoesNotContain(injected, property => property.PropertyType.Namespace?.StartsWith("OrderHub.Infrastructure") == true ||
                property.PropertyType.Name.Contains("Repository") || property.PropertyType.Name.Contains("DbContext"));
        }
        Assert.DoesNotContain(typeof(CreateClientCommand).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name.StartsWith("Microsoft.EntityFrameworkCore") || reference.Name == "OrderHub.Infrastructure");
        Assert.Equal(typeof(object), typeof(ClientEditorState).BaseType);
        Assert.Equal(typeof(ClientEditorState).Namespace, typeof(ClientEditorDialog).Namespace);
        Assert.DoesNotContain(typeof(ClientEditorState).GetProperties(), property =>
            property.PropertyType.Namespace?.Contains("Suppliers") == true || property.PropertyType.Namespace?.Contains("ShippingCarriers") == true);
    }
}
