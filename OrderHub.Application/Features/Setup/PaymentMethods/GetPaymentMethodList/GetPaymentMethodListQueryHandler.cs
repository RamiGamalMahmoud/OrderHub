using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;
using OrderHub.Application.Features.Setup.PaymentMethods;

namespace OrderHub.Application.Features.Setup.PaymentMethods;

internal sealed class GetPaymentMethodListQueryHandler(IPaymentMethodReader reader) : IRequestHandler<GetPaymentMethodListQuery, IEnumerable<PaymentMethodListItem>>
{
    public Task<IEnumerable<PaymentMethodListItem>> Handle(GetPaymentMethodListQuery request, CancellationToken cancellationToken) =>
        reader.GetPaymentMethodListAsync(cancellationToken);
}
