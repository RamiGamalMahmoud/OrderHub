using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.Delete;
using OrderHub.Application.Features.Products.Index;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.Enums;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Models;

namespace OrderHub.Tests.Infrastructure;

public class ProductsFeatureTests
{
    [Fact]
    public async Task Index_search_pages_and_editor_lookups_are_deterministic()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        Assert.Empty((await Send(provider, new GetProductIndexQuery())).Items);
        foreach (var name in new[] { "Zulu", "Alpha", "Alpha" })
        {
            Assert.True((await Send(provider, new CreateProductCommand(Input(ids.Root) with { Name = name, SelectedSuppliersIds = [ids.Supplier], ProductProperties = [new(ids.Text, true)] }))).IsSuccess);
        }
        var first = await Send(provider, new GetProductIndexQuery(PageSize: 2));
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(new[] { "Alpha", "Alpha" }, first.Items.Select(item => item.Name));
        Assert.True(first.Items.First().Id < first.Items.Last().Id);
        Assert.Equal(1, first.Items.First().SuppliersCount);
        Assert.Equal(1, first.Items.First().PropertiesCount);
        Assert.Equal("Zulu", Assert.Single((await Send(provider, new GetProductIndexQuery(Page: 2, PageSize: 2))).Items).Name);
        foreach (var term in new[] { "abc", "Supplier", "Root" })
        {
            Assert.Equal(3, (await Send(provider, new GetProductIndexQuery(term))).TotalCount);
        }
        var editor = await Send(provider, new GetProductEditorQuery(first.Items.First().Id));
        Assert.Equal("Alpha", editor.Product.Name);
        Assert.Equal("Root", Assert.Single(editor.SelectedCategoryPath).Name);
        Assert.Equal(new[] { "Boolean", "List", "Text" }, editor.Properties.Select(item => item.Name));
        Assert.Equal(2, editor.Properties.Single(item => item.Type == PropertyType.List).Options.Count);
        Assert.Single(editor.Suppliers);
        Assert.Null(await Send(provider, new GetProductEditorQuery(int.MaxValue)));
        Assert.Null(await Send(provider, new GetProductQuery(int.MaxValue)));
        Assert.Null((await Send(provider, new GetProductEditorQuery())).Product);
    }

    [Fact]
    public async Task Root_parent_with_children_and_deep_categories_are_selectable_and_browsed_by_existing_hierarchy()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        int parentId = ids.Child;
        using (var db = factory.CreateDbContext())
        {
            for (int depth = 0; depth < 30; depth++)
            {
                var child = new Category("Level " + depth, await db.Categories.FindAsync(parentId));
                db.Categories.Add(child);
                await db.SaveChangesAsync();
                parentId = child.Id;
            }
        }
        foreach (int category in new[] { ids.Root, ids.Child, parentId })
        {
            Assert.True((await Send(provider, new CreateProductCommand(Input(category)))).IsSuccess);
        }
        var root = await Send(provider, new GetProductEditorCategoriesQuery());
        Assert.Single(root.Categories);
        var parent = await Send(provider, new GetProductEditorCategoriesQuery(ids.Root));
        Assert.Equal(ids.Child, Assert.Single(parent.Categories).Id);
        Assert.Equal(ids.Root, Assert.Single(parent.Path).Id);
        var deep = await Send(provider, new GetProductEditorCategoriesQuery(parentId));
        Assert.Equal(32, deep.Path.Count);
        Assert.Empty(deep.Categories);
        Assert.False((await Send(provider, new GetProductEditorCategoriesQuery(int.MaxValue))).ParentExists);
    }

    [Fact]
    public async Task Supplier_and_property_reconciliation_preserves_surviving_ids_and_definitions()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        int id = (await Send(provider, new CreateProductCommand(Input(ids.Root) with
        {
            SelectedSuppliersIds = [ids.Supplier],
            ProductProperties = [new(ids.Text, true), new(ids.Boolean, false), new(ids.List, false)]
        }))).Value;
        int assignmentId;
        int[] optionIds;
        using (var db = factory.CreateDbContext())
        {
            assignmentId = (await db.ProductProperties.SingleAsync(item => item.ProductId == id && item.PropertyId == ids.Text)).Id;
            optionIds = await db.PropertyOptions.Select(item => item.Id).ToArrayAsync();
        }
        var update = new ProductUpdateInput(" Changed ", " new ", 0, ids.Child, [ids.Supplier], [new(ids.Text, false), new(ids.List, true)]);
        Assert.True((await Send(provider, new UpdateProductCommand(id, update))).IsSuccess);
        using var verify = factory.CreateDbContext();
        var product = await verify.Products.Include(item => item.Properties).Include(item => item.Suppliers).SingleAsync();
        Assert.Equal("Changed", product.Name.Value);
        Assert.Equal("NEW", product.Code);
        Assert.Equal(2, product.Properties.Count);
        Assert.Equal(assignmentId, product.Properties.Single(item => item.PropertyId == ids.Text).Id);
        Assert.False(product.Properties.Single(item => item.PropertyId == ids.Text).IsRequired);
        Assert.Equal(ids.Supplier, Assert.Single(product.Suppliers).Id);
        Assert.Equal(optionIds, await verify.PropertyOptions.Select(item => item.Id).ToArrayAsync());
        Assert.Equal(3, await verify.Properties.CountAsync());
    }

    [Fact]
    public async Task Supplier_and_property_add_remove_updates_keep_existing_entities()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        int secondId;
        using (var db = factory.CreateDbContext())
        {
            var second = new Supplier("Second");
            db.Suppliers.Add(second);
            await db.SaveChangesAsync();
            secondId = second.Id;
        }
        int id = (await Send(provider, new CreateProductCommand(Input(ids.Root) with
        {
            SelectedSuppliersIds = [ids.Supplier], ProductProperties = [new(ids.Text, true)]
        }))).Value;
        Assert.True((await Send(provider, new UpdateProductCommand(id,
            new("Product", "ABC", 10, ids.Root, [ids.Supplier, secondId], [new(ids.Text, false), new(ids.Boolean, true)])))).IsSuccess);
        Assert.Equal(2, (await Send(provider, new GetProductQuery(id))).SelectedSuppliersIds.Count());
        Assert.True((await Send(provider, new UpdateProductCommand(id,
            new("Product", "ABC", 10, ids.Root, [secondId], [new(ids.Text, false), new(ids.Boolean, true)])))).IsSuccess);
        var saved = await Send(provider, new GetProductQuery(id));
        Assert.Equal(secondId, Assert.Single(saved.SelectedSuppliersIds));
        Assert.Equal(2, saved.ProductProperties.Count());
        using var verify = factory.CreateDbContext();
        Assert.Equal(2, await verify.Suppliers.CountAsync());
        Assert.Equal(3, await verify.Properties.CountAsync());
    }

    [Theory]
    [InlineData("name")]
    [InlineData("code")]
    [InlineData("price")]
    [InlineData("precision")]
    [InlineData("category")]
    [InlineData("supplier")]
    [InlineData("property")]
    [InlineData("duplicate-property")]
    [InlineData("duplicate-supplier")]
    [InlineData("null")]
    public async Task Invalid_input_does_not_create_or_partially_change_product(string field)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        var bad = field switch
        {
            "name" => Input(ids.Root) with { Name = "x" },
            "code" => Input(ids.Root) with { Code = "12345678901" },
            "price" => Input(ids.Root) with { Price = -1 },
            "precision" => Input(ids.Root) with { Price = 1.001m },
            "category" => Input(int.MaxValue),
            "supplier" => Input(ids.Root) with { SelectedSuppliersIds = [int.MaxValue] },
            "property" => Input(ids.Root) with { ProductProperties = [new(int.MaxValue, true)] },
            "duplicate-property" => Input(ids.Root) with { ProductProperties = [new(ids.Text, true), new(ids.Text, false)] },
            "duplicate-supplier" => Input(ids.Root) with { SelectedSuppliersIds = [ids.Supplier, ids.Supplier] },
            _ => null
        };
        Assert.False((await Send(provider, new CreateProductCommand(bad))).IsSuccess);
        Assert.Empty((await Send(provider, new GetProductIndexQuery())).Items);
        int id = (await Send(provider, new CreateProductCommand(Input(ids.Root)))).Value;
        var update = bad is null ? null : new ProductUpdateInput(bad.Name, bad.Code, bad.Price, bad.CategoryId, bad.SelectedSuppliersIds, bad.ProductProperties.Select(item => new ProductPropertyUpdate(item.PropertyId, item.IsRequired)));
        Assert.False((await Send(provider, new UpdateProductCommand(id, update))).IsSuccess);
        var saved = await Send(provider, new GetProductQuery(id));
        Assert.Equal("Product", saved.Name);
        Assert.Equal("ABC", saved.Code);
        Assert.Equal(10, saved.Price);
        Assert.Equal(ids.Root, saved.CategoryId);
        Assert.Empty(saved.SelectedSuppliersIds);
        Assert.Empty(saved.ProductProperties);
    }

    [Fact]
    public async Task Atomic_database_failure_rolls_back_relationship_and_basic_data_changes()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        int id = (await Send(provider, new CreateProductCommand(Input(ids.Root) with { SelectedSuppliersIds = [ids.Supplier], ProductProperties = [new(ids.Text, true)] }))).Value;
        using (var db = factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_product BEFORE UPDATE ON products BEGIN SELECT RAISE(ABORT, 'test'); END;");
        }
        Assert.False((await Send(provider, new UpdateProductCommand(id, new("Changed", "NEW", 50, ids.Child, [], [new(ids.Boolean, true)])))).IsSuccess);
        var saved = await Send(provider, new GetProductQuery(id));
        Assert.Equal("Product", saved.Name);
        Assert.Equal(ids.Supplier, Assert.Single(saved.SelectedSuppliersIds));
        Assert.Equal(ids.Text, Assert.Single(saved.ProductProperties).PropertyId);
    }

    [Theory]
    [InlineData("order")]
    [InlineData("quotation")]
    [InlineData("proforma")]
    [InlineData("invoice")]
    [InlineData("draft")]
    public async Task All_real_history_references_block_deletion_and_survive_edits(string reference)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        int id = (await Send(provider, new CreateProductCommand(Input(ids.Root) with { ProductProperties = [new(ids.List, true)] }))).Value;
        string draftJson = null;
        using (var db = factory.CreateDbContext())
        {
            var now = DateTime.UtcNow;
            if (reference == "draft")
            {
                var user = User.Create("owner", "hash", "Owner");
                db.Users.Add(user);
                await db.SaveChangesAsync();
                var data = new OrderDraftData(null, "Client", null, null, null, null, null,
                    [new OrderDraftItem(id, "Historical product", "Root", 10, 2, 0, null, null, [new(ids.List, "Red")])], []);
                draftJson = JsonSerializer.Serialize(new Draft(Guid.NewGuid(), user.Id, now, data));
                db.DraftRecords.Add(new DraftRecord { Id = Guid.NewGuid(), UserId = user.Id, Data = draftJson, LastModified = now });
            }
            else if (reference == "quotation")
            {
                var document = Quotation.Create("Q1", now, "Client", "Phone", "Address", 20, 0, 20, now.AddDays(1), Guid.NewGuid());
                document.AddItem(QuotationItem.Create(id, "Historical product", 2, 10, 0, 20, 0, 20));
                db.Quotations.Add(document);
            }
            else if (reference == "proforma")
            {
                var document = ProformaInvoice.Create("P1", now, "Client", "Phone", "Address", 20, 0, 20, Guid.NewGuid());
                document.AddItem(ProformaInvoiceItem.Create(id, "Historical product", 2, 10, 0, 20, 0, 20));
                db.ProformaInvoices.Add(document);
            }
            else
            {
                var city = new City("City");
                var client = new Client("Client", "Street", city, "123456", "+20");
                db.Clients.Add(client);
                await db.SaveChangesAsync();
                var order = new Order(client.Id, "O1");
                if (reference == "order")
                {
                    order.AddOrderItem(id, "Historical product", 10, 2, 0, "Supplier snapshot", ids.Supplier, [new(ids.List, "Red")]);
                }
                order.AddDeliveryStep(new OrderDeliveryStep { StepOrder = 1, DeliveryMethod = DeliveryMethod.Pickup });
                db.Orders.Add(order);
                await db.SaveChangesAsync();
                if (reference == "invoice")
                {
                    var invoice = Invoice.Create("I1", now, "Client", "Phone", "Address", 20, 0, 20, order.Id);
                    invoice.AddItem(new InvoiceItem(id, "Historical product", 2, 10, 0, 20, 0, 20));
                    db.Invoices.Add(invoice);
                }
            }
            await db.SaveChangesAsync();
        }
        Assert.False((await Send(provider, new DeleteProductCommand(id))).IsSuccess);
        if (reference != "draft")
        {
            using var direct = factory.CreateDbContext();
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => direct.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM products WHERE id = {id}"));
        }
        Assert.True((await Send(provider, new UpdateProductCommand(id, new("Changed", "NEW", 99, ids.Child, [], [])))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Single(verify.Products);
        if (reference == "draft") { Assert.Equal(draftJson, Assert.Single(verify.DraftRecords).Data); }
        if (reference == "order")
        {
            var item = Assert.Single(verify.OrderItems);
            Assert.Equal("Historical product", item.ProductName);
            Assert.Equal(10, item.UnitPrice.Value);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(20, item.SubTotal.Value);
            Assert.Equal("Supplier snapshot", item.SupplierName);
            Assert.Equal("Red", Assert.Single(verify.OrderItemProperties).Value);
            Assert.Single(verify.OrderDeliverySteps);
        }
        CommercialDocumentItem snapshot = reference switch
        {
            "quotation" => Assert.Single(verify.QuotationItems),
            "proforma" => Assert.Single(verify.ProformaInvoiceItems),
            "invoice" => Assert.Single(verify.InvoicesItem),
            _ => null
        };
        if (snapshot is not null)
        {
            Assert.Equal("Historical product", snapshot.ProductName);
            Assert.Equal(10, snapshot.UnitPrice);
            Assert.Equal(2, snapshot.Quantity);
            Assert.Equal(20, snapshot.Subtotal);
        }
    }

    [Fact]
    public async Task Unused_delete_cleans_only_product_owned_links_and_missing_mutations_are_clear()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        var ids = await Seed(factory);
        int id = (await Send(provider, new CreateProductCommand(Input(ids.Root) with { SelectedSuppliersIds = [ids.Supplier], ProductProperties = [new(ids.Text, true)] }))).Value;
        Assert.True((await Send(provider, new DeleteProductCommand(id))).IsSuccess);
        Assert.False((await Send(provider, new DeleteProductCommand(id))).IsSuccess);
        Assert.False((await Send(provider, new UpdateProductCommand(id, new("Product", "ABC", 10, ids.Root, [], [])))).IsSuccess);
        using var verify = factory.CreateDbContext();
        Assert.Empty(verify.ProductProperties);
        Assert.Single(verify.Suppliers);
        Assert.Equal(3, await verify.Properties.CountAsync());
        Assert.Equal(2, await verify.Categories.CountAsync());
    }

    private static ProductCreateInput Input(int category) => new("Product", "ABC", 10, category, [], []);
    private sealed record SeedIds(int Root, int Child, int Supplier, int Text, int Boolean, int List);
    private static async Task<SeedIds> Seed(TestDbFactory factory)
    {
        using var db = factory.CreateDbContext();
        var root = new Category("Root");
        db.Categories.Add(root);
        await db.SaveChangesAsync();
        var child = new Category("Child", root);
        var supplier = new Supplier("Supplier");
        var text = Property.Create("Text", PropertyType.Text);
        var boolean = Property.Create("Boolean", PropertyType.Boolean);
        var list = Property.Create("List", PropertyType.List, options: ["Red", "Blue"]);
        db.Categories.Add(child);
        db.Suppliers.Add(supplier);
        db.Properties.AddRange(text, boolean, list);
        await db.SaveChangesAsync();
        return new(root.Id, child.Id, supplier.Id, text.Id, boolean.Id, list.Id);
    }
    private static async Task<T> Send<T>(ServiceProvider provider, IRequest<T> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
