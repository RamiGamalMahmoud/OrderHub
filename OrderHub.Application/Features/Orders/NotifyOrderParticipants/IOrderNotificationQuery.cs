using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants;

public interface IOrderNotificationQuery
{
    Task<OrderNotification> GetForNotificationAsync(int orderId, CancellationToken cancellationToken = default);
}
