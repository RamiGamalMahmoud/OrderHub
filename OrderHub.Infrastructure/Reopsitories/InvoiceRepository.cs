using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models.CommercialDocuments;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public Task<Invoice> GetByOrderId(int orderId, CancellationToken cancellationToken = default)
    {
        return _context.Invoices
            .Where(inv => inv.OrderId == orderId)
            .Include(inv => inv.Order)
            .Include(inv => inv.Items)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}