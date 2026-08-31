using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Stores;

internal class ProformaInvoiceRepository : IProformaInvoiceRepository
{
    private readonly AppDbContext _dbContext;

    public ProformaInvoiceRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProformaInvoice proformaInvoice, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProformaInvoices.AddAsync(proformaInvoice, cancellationToken);
    }

    public async Task<IEnumerable<ProformaInvoice>> GetByDraftReference(Guid draftReference, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProformaInvoices
            .Where(x => x.SourceDraftReference == draftReference)
            .ToListAsync(cancellationToken);
    }

    public Task<IEnumerable<ProformaInvoice>> GetByOrderId(int orderId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
