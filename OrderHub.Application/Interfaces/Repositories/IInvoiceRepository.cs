using OrderHub.Domain.Models.CommercialDocuments;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<Invoice> GetByOrderId(int orderId, CancellationToken cancellationToken = default);
}