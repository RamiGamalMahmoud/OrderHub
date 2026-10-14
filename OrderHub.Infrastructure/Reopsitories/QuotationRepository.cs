using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models.CommercialDocuments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal class QuotationRepository : IQuotationRepository
{
    private readonly AppDbContext _dbContext;

    public QuotationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Quotation quotation, CancellationToken cancellationToken = default)
    {
        await _dbContext.AddAsync(quotation, cancellationToken);
    }

    public async Task<IEnumerable<Quotation>> GetByDraftReference(Guid draftReference, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Quotations
            .Where(x => x.SourceDraftReference == draftReference)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Quotation>> GetByOrderId(int orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Quotations
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
