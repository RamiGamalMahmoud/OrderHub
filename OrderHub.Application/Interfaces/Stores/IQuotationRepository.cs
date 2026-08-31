using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Stores;

public interface IQuotationRepository
{
    Task AddAsync(Quotation quotation, CancellationToken cancellationToken = default);
    Task<IEnumerable<Quotation>> GetByDraftReference(Guid draftReference, CancellationToken cancellationToken = default);
    Task<IEnumerable<Quotation>> GetByOrderId(int orderId, CancellationToken cancellationToken = default);
}
