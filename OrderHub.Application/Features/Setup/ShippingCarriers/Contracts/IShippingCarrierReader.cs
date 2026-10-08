using System.Collections.Generic;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

public interface IShippingCarrierReader
{
    Task<ShippingCarrierEdit> GetShippingCarrierForEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ShippingCarrierListItem>> GetShippingCarriersByNameAsync(string searchTerm, int take, CancellationToken cancellationToken);
    Task<ShippingCarrierListItem> GetShippingCarrierByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<ShippingCarrierListItem>> GetShippingCarriersAsync(CancellationToken cancellationToken);
    Task<IEnumerable<ShippingCarrierInfo>> GetAllShippingCarriersInfoAsync(CancellationToken cancellationToken);
}
