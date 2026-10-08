using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Features.Setup.Categories;

namespace OrderHub.Tests.Infrastructure;

public class CategoryPersistenceTests
{
    [Fact]
    public async Task Browse_returns_one_level_recursive_counts_and_ordered_breadcrumbs()
    {
        using var factory = TestDb.CreateFactory();
        int rootId, childId, deepId, emptyId;
        using (var db = factory.CreateDbContext())
        {
            var root = new Category("Audio");
            var child = new Category("Speakers", root);
            var deep = new Category("Wireless", child);
            var sibling = new Category("Microphones", root);
            var empty = new Category("Empty");
            db.Categories.AddRange(root, child, deep, sibling, empty);
            foreach (var (category, count) in new[] { (root, 2), (child, 5), (deep, 7), (sibling, 4) })
                for (int i = 0; i < count; i++)
                    db.Products.Add(new Product($"Product {i}", $"P{i}", category, new Money(10)));
            await db.SaveChangesAsync();
            rootId = root.Id; childId = child.Id; deepId = deep.Id; emptyId = empty.Id;
        }
        var reader = new CategoryReader(factory);
        var roots = await reader.GetCategoriesAsync(null, default);
        Assert.True(roots.ParentExists);
        Assert.Empty(roots.Path);
        Assert.Equal(2, roots.Categories.Count);
        var audio = Assert.Single(roots.Categories, category => category.Id == rootId);
        Assert.Equal(2, audio.DirectChildrenCount);
        Assert.Equal(18, audio.TotalProductsCount);
        Assert.True(audio.HasChildren);
        var children = await reader.GetCategoriesAsync(rootId, default);
        Assert.Equal(new[] { "Microphones", "Speakers" }, children.Categories.Select(category => category.Name));
        var speakers = Assert.Single(children.Categories, category => category.Id == childId);
        Assert.Equal(1, speakers.DirectChildrenCount);
        Assert.Equal(12, speakers.TotalProductsCount);
        var nested = await reader.GetCategoriesAsync(deepId, default);
        Assert.Empty(nested.Categories);
        Assert.Equal(new[] { rootId, childId, deepId }, nested.Path.Select(segment => segment.Id));
        Assert.Equal(new[] { "Audio", "Speakers", "Wireless" }, nested.Path.Select(segment => segment.Name));
        Assert.Empty((await reader.GetCategoriesAsync(emptyId, default)).Categories);
        Assert.False((await reader.GetCategoriesAsync(int.MaxValue, default)).ParentExists);
        var options = (await reader.GetCategoriesInfoAsync(default)).ToList();
        Assert.Equal("Audio / Speakers / Wireless", options.Single(category => category.Id == deepId).FullPath);
        Assert.False(options.Single(category => category.Id == deepId).HasSubCategories);
        Assert.True(options.Single(category => category.Id == rootId).HasSubCategories);
    }

    [Fact]
    public async Task Empty_database_returns_an_empty_root_level()
    {
        using var factory = TestDb.CreateFactory();
        var response = await new CategoryReader(factory).GetCategoriesAsync(null, default);
        Assert.True(response.ParentExists);
        Assert.Empty(response.Categories);
        Assert.Empty(response.Path);
    }

    [Fact]
    public async Task Create_normalizes_names_validates_parents_and_preserves_sibling_uniqueness()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.True((await Execute(provider, new CreateCategoryCommand(new("  Parent  ", null)))).IsSuccess);
        var parent = Assert.Single((await Execute(provider, new GetCategoriesQuery())).Categories);
        Assert.Equal("Parent", parent.Name);
        Assert.True((await Execute(provider, new CreateCategoryCommand(new("Child", parent.Id)))).IsSuccess);
        var child = Assert.Single((await Execute(provider, new GetCategoriesQuery(parent.Id))).Categories);
        Assert.Equal(parent.Id, (await Execute(provider, new GetCategoryForEditQuery(child.Id))).ParentId);
        Assert.False((await Execute(provider, new CreateCategoryCommand(new("Child", parent.Id)))).IsSuccess);
        Assert.False((await Execute(provider, new CreateCategoryCommand(new("Missing", int.MaxValue)))).IsSuccess);
        using var db = factory.CreateDbContext();
        Assert.Equal(2, await db.Categories.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" a ")]
    public async Task Invalid_names_fail_without_persistence(string name)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.False((await Execute(provider, new CreateCategoryCommand(new(name, null)))).IsSuccess);
        Assert.True((await Execute(provider, new CreateCategoryCommand(new("Original", null)))).IsSuccess);
        var original = Assert.Single((await Execute(provider, new GetCategoriesQuery())).Categories);
        Assert.False((await Execute(provider, new UpdateCategoryCommand(new(original.Id, name, null)))).IsSuccess);
        Assert.Equal("Original", (await Execute(provider, new GetCategoryForEditQuery(original.Id))).Name);
    }

    [Fact]
    public async Task Excessive_length_is_rejected_after_trimming()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        Assert.False((await Execute(provider, new CreateCategoryCommand(new(new string('a', 101), null)))).IsSuccess);
        Assert.True((await Execute(provider, new CreateCategoryCommand(new("  " + new string('a', 100) + "  ", null)))).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Update_rejects_self_child_and_deeper_descendant_even_without_loaded_navigations(int parentIndex)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await SeedChain(factory);
        var result = await Execute(provider, new UpdateCategoryCommand(new(ids[0], "Changed", ids[parentIndex])));
        Assert.False(result.IsSuccess);
        var root = await Execute(provider, new GetCategoryForEditQuery(ids[0]));
        Assert.Null(root.ParentId);
        Assert.Equal("Root", root.Name);
    }

    [Fact]
    public async Task Update_supports_rename_move_to_another_branch_and_move_to_root()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await SeedChain(factory);
        Assert.True((await Execute(provider, new CreateCategoryCommand(new("Other branch", null)))).IsSuccess);
        var other = (await Execute(provider, new GetCategoriesQuery())).Categories.Single(category => category.Id != ids[0]);
        Assert.True((await Execute(provider, new UpdateCategoryCommand(new(ids[1], " Renamed ", other.Id)))).IsSuccess);
        var moved = await Execute(provider, new GetCategoryForEditQuery(ids[1]));
        Assert.Equal("Renamed", moved.Name);
        Assert.Equal(other.Id, moved.ParentId);
        Assert.Empty((await Execute(provider, new GetCategoriesQuery(ids[0]))).Categories);
        Assert.Single((await Execute(provider, new GetCategoriesQuery(ids[1]))).Categories);
        Assert.True((await Execute(provider, new UpdateCategoryCommand(new(ids[1], "Renamed", null)))).IsSuccess);
        Assert.Null((await Execute(provider, new GetCategoryForEditQuery(ids[1]))).ParentId);
        Assert.Equal(3, (await Execute(provider, new GetCategoriesQuery())).Categories.Count);
        // An unchanged edit should not detect itself as a duplicate.
        Assert.True((await Execute(provider, new UpdateCategoryCommand(new(ids[1], "Renamed", null)))).IsSuccess);
    }

    [Fact]
    public async Task Update_rejects_missing_target_missing_parent_and_duplicate_sibling()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await SeedChain(factory);
        Assert.False((await Execute(provider, new UpdateCategoryCommand(new(int.MaxValue, "Valid", null)))).IsSuccess);
        Assert.False((await Execute(provider, new UpdateCategoryCommand(new(ids[1], "Valid", int.MaxValue)))).IsSuccess);
        Assert.False((await Execute(provider, new UpdateCategoryCommand(new(ids[1], "Root", null)))).IsSuccess);
        Assert.Null(await Execute(provider, new GetCategoryForEditQuery(int.MaxValue)));
    }

    [Fact]
    public async Task Delete_blocks_structural_and_product_use_and_allows_unused_leaf()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await SeedChain(factory);
        using (var db = factory.CreateDbContext())
        {
            var leaf = await db.Categories.FindAsync(ids[2]);
            db.Products.Add(new Product("Direct product", "P1", leaf, new Money(10)));
            await db.SaveChangesAsync();
        }
        Assert.False((await Execute(provider, new DeleteCategoryCommand(ids[0]))).IsSuccess);
        Assert.False((await Execute(provider, new DeleteCategoryCommand(ids[1]))).IsSuccess);
        Assert.Equal("لا يمكن حذف القسم لأنه مرتبط بمنتجات.", (await Execute(provider, new DeleteCategoryCommand(ids[2]))).ErrorMessage);
        Assert.False((await Execute(provider, new DeleteCategoryCommand(int.MaxValue))).IsSuccess);
        Assert.True((await Execute(provider, new CreateCategoryCommand(new("Unused", ids[0])))).IsSuccess);
        var unused = (await Execute(provider, new GetCategoriesQuery(ids[0]))).Categories.Single(category => category.Name == "Unused");
        Assert.True((await Execute(provider, new DeleteCategoryCommand(unused.Id))).IsSuccess);
        Assert.Null(await Execute(provider, new GetCategoryForEditQuery(unused.Id)));
    }

    [Fact]
    public async Task Category_read_only_actor_cannot_execute_any_write_request()
    {
        using var factory = TestDb.CreateFactory();
        var ids = await SeedChain(factory);
        int userId;
        using (var db = factory.CreateDbContext())
        {
            var role = new Role("Category reader");
            role.SetPermission(PermissionFeature.Categories, PermissionLevel.Read);
            var user = User.Create("categoryreader", "hash", "Reader");
            user.AssignRole(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        }
        var services = new ServiceCollection();
        services.AddLogging();
        OrderHub.Application.ServiceCollectionExtensions.AddApplication(services);
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        services.RemoveAll<OrderHub.Infrastructure.AppDbContextFactory>();
        services.AddSingleton<OrderHub.Infrastructure.AppDbContextFactory>(factory);
        services.AddScoped<ICurrentUser>(_ => OrderHub.Tests.Application.AuthorizationTests.Identity(true, userId, false));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Assert.Single((await Execute(provider, new GetCategoriesQuery())).Categories);
        Assert.NotNull(await Execute(provider, new GetCategoryForEditQuery(ids[0])));
        Assert.Equal(3, (await Execute(provider, new GetCategoriesInfoQuery())).Count());
        foreach (IRequest<Result> write in new IRequest<Result>[] {
            new CreateCategoryCommand(new("Denied", null)),
            new UpdateCategoryCommand(new(ids[0], "Denied", null)), new DeleteCategoryCommand(ids[2]) })
            await Assert.ThrowsAsync<AuthorizationDeniedException>(() => Execute(provider, write));
        using var verify = factory.CreateDbContext();
        Assert.Equal(3, await verify.Categories.CountAsync());
        Assert.Equal("Root", (await verify.Categories.FindAsync(ids[0])).Name.Value);
    }

    [Fact]
    public async Task Cancelled_category_read_reaches_the_database()
    {
        using var factory = TestDb.CreateFactory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CategoryReader(factory).GetCategoriesAsync(null, cancellation.Token));
    }

    private static async Task<int[]> SeedChain(TestDbFactory factory)
    {
        using var db = factory.CreateDbContext();
        var root = new Category("Root");
        var child = new Category("Child", root);
        var deep = new Category("Deep", child);
        db.Categories.AddRange(root, child, deep);
        await db.SaveChangesAsync();
        return [root.Id, child.Id, deep.Id];
    }

    private static async Task<T> Execute<T>(ServiceProvider provider, IRequest<T> request)
    {
        // Match the Web executor's fresh context per operation.
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
