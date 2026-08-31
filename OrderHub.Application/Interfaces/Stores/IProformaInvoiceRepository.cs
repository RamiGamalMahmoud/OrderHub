using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Stores;

public interface IProformaInvoiceRepository
{
    Task AddAsync(ProformaInvoice proformaInvoice, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProformaInvoice>> GetByDraftReference(Guid draftReference, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProformaInvoice>> GetByOrderId(int orderId, CancellationToken cancellationToken = default);
}
