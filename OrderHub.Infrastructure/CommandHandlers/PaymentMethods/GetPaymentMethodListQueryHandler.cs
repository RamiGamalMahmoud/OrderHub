using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;
using static OrderHub.Application.Queries.PaymentMothodQueries;

namespace OrderHub.Infrastructure.CommandHandlers.PaymentMethods;

internal class GetPaymentMethodListQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetPaymentMethodListQuery, IEnumerable<PaymentMethodListDto>>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<IEnumerable<PaymentMethodListDto>> Handle(GetPaymentMethodListQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        return await appDbContext.PaymentMethods
            .Select(p => new PaymentMethodListDto(p.Id, p.DisplayName, p.Description, p.IsActive))
            .ToListAsync();
    }
}
