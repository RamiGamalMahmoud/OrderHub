using OrderHub.Application.Features.Orders.GetItemEditor;
using OrderHub.Application.Features.Orders.GetItemsEditor;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace OrderHub.Infrastructure.Features.Orders;

internal class OrderItemEditorReader : IOrderItemEditorReader
{
    private readonly AppDbContextFactory _dbContextFactory;

    public OrderItemEditorReader(AppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<OrderItemEditorData> GetAsync(int productId, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new OrderItemEditorData(
                p.Id,
                p.Price.Value,
                p.Name.Value,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new OrderItemEditorSupplier(s.Id, s.Name.Value)),
                p.Properties.Select(p => new OrderItemEditorProperty(
                    p.PropertyId,
                    p.Property.Name,
                    p.IsRequired,
                    p.Property.PropertyType,
                    p.Property.Options.Select(o => new OrderItemEditorPropertyOption(o.Id, o.Value))))))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderCatalogItem>> GetItemsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new OrderCatalogItem(
                p.Id,
                p.Price.Value,
                p.Name.Value,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new OrderCatalogSupplier(s.Id, s.Name.Value))
                .ToList(),

                p.Properties.Select(p => new OrderCatalogProperty(
                    p.PropertyId,
                    p.Property.Name,
                    p.IsRequired,
                    p.Property.PropertyType,
                    p.Property.Options.Select(o => new OrderCatalogPropertyOption(
                        o.Id,
                        o.Value))
                    .ToList()))
                .ToList()))
            .ToListAsync(cancellationToken);
    }
}
