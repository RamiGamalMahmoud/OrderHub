using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Documents.GetDocuments;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Documents;

internal sealed class DocumentReader(AppDbContextFactory contextFactory) : IDocumentReader
{
    public async Task<PagedResult<DocumentListItem>> GetPageAsync(
        DocumentType type,
        int pageNumber,
        int pageSize,
        string searchTerm,
        int? orderId,
        CancellationToken cancellationToken = default)
    {
        using var context = contextFactory.CreateDbContext();
        var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        return type switch
        {
            DocumentType.Invoice => await context.Invoices.AsNoTracking()
                .Where(document => !orderId.HasValue || document.OrderId == orderId.Value)
                .Where(document => term == null || document.DocumentNumber.Contains(term) ||
                    document.CustomerName.Contains(term) || document.Order.OrderNumber.Contains(term))
                .OrderByDescending(document => document.IssueDate).ThenByDescending(document => document.Id)
                .Select(document => new DocumentListItem(document.Id, DocumentType.Invoice,
                    document.DocumentNumber, document.IssueDate, document.CustomerName,
                    document.OrderId, document.Order.OrderNumber, document.TotalVat,
                    document.TotalAmount, null))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken),

            DocumentType.Quotation => await context.Quotations.AsNoTracking()
                .Where(document => !orderId.HasValue || document.OrderId == orderId.Value)
                .Where(document => term == null || document.DocumentNumber.Contains(term) ||
                    document.CustomerName.Contains(term) ||
                    (document.Order != null && document.Order.OrderNumber.Contains(term)))
                .OrderByDescending(document => document.IssueDate).ThenByDescending(document => document.Id)
                .Select(document => new DocumentListItem(document.Id, DocumentType.Quotation,
                    document.DocumentNumber, document.IssueDate, document.CustomerName,
                    document.OrderId, document.Order == null ? null : document.Order.OrderNumber,
                    document.TotalVat, document.TotalAmount, document.ValidUntil))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken),

            DocumentType.ProformaInvoice => await context.ProformaInvoices.AsNoTracking()
                .Where(document => !orderId.HasValue || document.OrderId == orderId.Value)
                .Where(document => term == null || document.DocumentNumber.Contains(term) ||
                    document.CustomerName.Contains(term) ||
                    (document.Order != null && document.Order.OrderNumber.Contains(term)))
                .OrderByDescending(document => document.IssueDate).ThenByDescending(document => document.Id)
                .Select(document => new DocumentListItem(document.Id, DocumentType.ProformaInvoice,
                    document.DocumentNumber, document.IssueDate, document.CustomerName,
                    document.OrderId, document.Order == null ? null : document.Order.OrderNumber,
                    document.TotalVat, document.TotalAmount, null))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken),

            _ => throw new System.ArgumentOutOfRangeException(nameof(type))
        };
    }
}
