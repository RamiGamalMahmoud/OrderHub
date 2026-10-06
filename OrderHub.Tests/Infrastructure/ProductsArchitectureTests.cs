using System.Linq;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Components;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OrderHub.Web.Components.Pages.Features.Products.Editor;

namespace OrderHub.Tests.Infrastructure;

public class ProductsArchitectureTests
{
    [Fact]
    public void All_requests_use_product_permissions_and_editor_lookups_require_write()
    {
        var requests = typeof(CreateProductCommand).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("OrderHub.Application.Features.Products") == true &&
                type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToArray();
        Assert.Equal(10, requests.Length);
        foreach (var request in requests)
        {
            var permission = request.GetCustomAttribute<RequirePermissionAttribute>();
            Assert.NotNull(permission);
            Assert.Equal(PermissionFeature.Products, permission.Feature);
            var level = request.Name.EndsWith("Command") || request.Namespace.EndsWith("Editor") ? PermissionLevel.Write : PermissionLevel.Read;
            Assert.Equal(level, permission.RequiredLevel);
        }
        Assert.DoesNotContain(typeof(CreateProductCommand).Assembly.GetReferencedAssemblies(), reference => reference.Name.StartsWith("Microsoft.EntityFrameworkCore") || reference.Name == "OrderHub.Infrastructure");
    }

    [Fact]
    public void Web_has_no_persistence_injection_and_history_FKs_restrict_product_deletion()
    {
        foreach (var component in new[] { typeof(Editor), typeof(OrderHub.Web.Components.Pages.Features.Products.Index) })
        {
            var injected = component.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(property => property.GetCustomAttribute<InjectAttribute>() is not null).ToArray();
            Assert.Contains(injected, property => property.PropertyType == typeof(IRequestExecutor));
            Assert.DoesNotContain(injected, property => property.PropertyType.Namespace?.StartsWith("OrderHub.Infrastructure") == true || property.PropertyType.Name.Contains("Repository"));
        }
        using var factory = TestDb.CreateFactory();
        using var db = factory.CreateDbContext();
        var references = db.Model.FindEntityType(typeof(Product)).GetReferencingForeignKeys().Where(fk => !fk.IsOwnership).ToArray();
        Assert.Equal(4, references.Count(fk => fk.DeleteBehavior == DeleteBehavior.Restrict));
        Assert.Equal(2, references.Count(fk => fk.DeleteBehavior == DeleteBehavior.Cascade));
        Assert.DoesNotContain(typeof(ProductProperty).GetProperties(), property => property.Name.Contains("Value") || property.Name.Contains("Option"));
    }
}
