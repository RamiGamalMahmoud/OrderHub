using System.Collections.Generic;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace OrderHub.Application.Features.Setup.Deliverymen.Contracts;

public interface IDeliverymanReader
{
    Task<IEnumerable<DeliverymanListItem>> GetAllDeliverymenListAsync(CancellationToken cancellationToken);
    Task<DeliverymanInput> GetDeliverymanForEditAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<DeliverymanListItem>> GetDeliverymenByNameAsync(string searchTerm, int take, CancellationToken cancellationToken);
    Task<DeliverymanListItem> GetDeliverymanByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<DeliverymanInfo>> GetAllDeliverymenInfoAsync(CancellationToken cancellationToken);
}
