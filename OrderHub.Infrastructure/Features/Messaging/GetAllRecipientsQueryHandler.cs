using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Messaging;

internal class GetAllRecipientsQueryHandler(AppDbContextFactory appDbContext) : IRequestHandler<GetAllRecipients.Query, IReadOnlyList<GetAllRecipients.Recipient>>
{
    public async Task<IReadOnlyList<GetAllRecipients.Recipient>> Handle(GetAllRecipients.Query request, CancellationToken cancellationToken)
    {
        using AppDbContext dbContext = appDbContext.CreateDbContext();

        var clients = await dbContext.Clients.AsNoTracking()
            .Select(client => new GetAllRecipients.Recipient(
                client.Id,
                client.Name.Value,
                client.Phone.Number.FullNumber,
                Domain.Enums.RecipientType.Client))
            .ToListAsync(cancellationToken: cancellationToken);

        var suppliers = await dbContext
            .Suppliers
            .AsNoTracking()
            .Select(supplier => new GetAllRecipients.Recipient(
                supplier.Id,
                supplier.Name.Value,
                supplier.WhatsappGroup.GroupLink,
                Domain.Enums.RecipientType.Supplier))
            .ToListAsync(cancellationToken: cancellationToken);

        var deliverymen = await dbContext
            .Deliverymen
            .AsNoTracking()
            .Select(deliveryman => new GetAllRecipients.Recipient(
                deliveryman.Id,
                deliveryman.Name.Value,
                deliveryman.WhatsappGroup.GroupLink,
                Domain.Enums.RecipientType.Deliveryman))
            .ToListAsync(cancellationToken: cancellationToken);

        var shippingCarriers = await dbContext
            .ShippingCarriers
            .AsNoTracking()
            .Select(shippingCarrier => new GetAllRecipients.Recipient(
                shippingCarrier.Id,
                shippingCarrier.Name.Value,
                shippingCarrier.Phone.Number.FullNumber,
                Domain.Enums.RecipientType.ShippingCarrier))
            .ToListAsync(cancellationToken: cancellationToken);

        return clients
            .Concat(shippingCarriers)
            .Concat(deliverymen)
            .Concat(suppliers)
            .ToList();
    }
}
