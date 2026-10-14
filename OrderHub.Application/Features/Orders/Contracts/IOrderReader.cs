using OrderHub.Application.Features.Orders.GetForEdit;
using OrderHub.Application.Features.Orders.GetDetails;
using OrderHub.Application.Features.Orders.GetPage;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Application.Features.Orders.GetSummaries;

namespace OrderHub.Application.Features.Orders.Contracts;

public interface IOrderReader
{
    Task<OrderDetails> GetDetailsAsync(int id, CancellationToken cancellationToken);
    Task<OrderEditorData> GetForEditAsync(int orderId, CancellationToken cancellationToken);
    Task<PagedResult<OrderListItem>> GetPageAsync(int pageNumber, int pageSize, string searchTerm, DateTime? fromDate, DateTime? toDate, int? paymentMethodId, OrderStatus? orderStatus, CancellationToken cancellationToken, bool archivedOnly = false);
    Task<OrderWorkflowState> GetWorkflowAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<OrderHistoryEntry>> GetHistoryAsync(int orderId, int? stepId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<OrderSummaryData> GetSummariesAsync(int orderId, CancellationToken cancellationToken);
}
