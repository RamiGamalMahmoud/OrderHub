using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.Queries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Orders.Queries;

internal class GetOrderItemsEditorQeryHandler(AppDbContextFactory dbContextFactory) : IRequestHandler<GetOrderItemsEditor.Query, IReadOnlyList<GetOrderItemsEditor.OrderItem>>
{
    public async Task<IReadOnlyList<GetOrderItemsEditor.OrderItem>> Handle(GetOrderItemsEditor.Query request, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = dbContextFactory.CreateDbContext();

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => request.ProductIds.Contains(p.Id))
            .Select(p => new GetOrderItemsEditor.OrderItem(
                p.Id,
                p.Price.Value,
                p.Name.Value,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new GetOrderItemsEditor.OrderItemSupplier(s.Id, s.Name.Value))
                .ToList(),

                p.Properties.Select(p => new GetOrderItemsEditor.OrderItemProperty(
                    p.PropertyId,
                    p.Property.Name,
                    p.IsRequired,
                    p.Property.PropertyType,
                    p.Property.Options.Select(o => new GetOrderItemsEditor.OrderItemPropertyOption(
                        o.Id, 
                        o.Value))
                    .ToList()))
                .ToList()))
            .ToListAsync(cancellationToken);
    }
}
