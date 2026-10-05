using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;
using OrderHub.Application.Features.Setup.PaymentMethods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Infrastructure.Features.Setup.PaymentMethods;

internal sealed class PaymentMethodReader(AppDbContextFactory contextFactory) : IPaymentMethodReader
{
    public async Task<IEnumerable<PaymentMethodListItem>> GetPaymentMethodListAsync(CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = contextFactory.CreateDbContext();
        return await appDbContext.PaymentMethods.AsNoTracking()
            .Select(p => new PaymentMethodListItem(p.Id, p.DisplayName, p.Description, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}
