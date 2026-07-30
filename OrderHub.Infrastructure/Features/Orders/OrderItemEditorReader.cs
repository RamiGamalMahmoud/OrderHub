using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Features.Orders.GetOrderItemEditor;

namespace OrderHub.Infrastructure.Features.Orders;

internal class OrderItemEditorReader : IOrderItemEditorReader
{
    private readonly AppDbContextFactory _dbContextFactory;

    public OrderItemEditorReader(AppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<OrderItemEditorDto> GetAsync(int productId, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new OrderItemEditorDto(
                p.Id,
                p.Price.Value,
                p.Name.Value,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new OrderItemSupplier(s.Id, s.Name.Value)),
                p.Properties.Select(p => new OrderItemProperty(
                    p.PropertyId,
                    p.Property.Name,
                    p.IsRequired,
                    p.Property.PropertyType,
                    p.Property.Options.Select(o => new OrderItemPropertyOption (o.Id, o.Value))))))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
