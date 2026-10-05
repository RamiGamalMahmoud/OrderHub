using OrderHub.Application.Features.Orders.GetForEdit;
using OrderHub.Application.Features.Orders.GetDetails;
using OrderHub.Application.Features.Orders.GetPage;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Orders.Contracts;

public interface IOrderReader
{
    Task<OrderDetails> GetDetailsAsync(int id, CancellationToken cancellationToken);
    Task<OrderEditorData> GetForEditAsync(int orderId, CancellationToken cancellationToken);
    Task<PagedResult<OrderListItem>> GetPageAsync(int pageNumber, int pageSize, string searchTerm, DateTime? fromDate, DateTime? toDate, int? paymentMethodId, OrderStatus? orderStatus, CancellationToken cancellationToken);
}
