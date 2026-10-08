using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;
using OrderHub.Application.Features.Setup.PaymentMethods;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.PaymentMethods.Contracts;

public interface IPaymentMethodReader
{
    Task<IEnumerable<PaymentMethodListItem>> GetPaymentMethodListAsync(CancellationToken cancellationToken);
}
