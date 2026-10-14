using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Documents.GetDocuments;

public interface IDocumentReader
{
    Task<PagedResult<DocumentListItem>> GetPageAsync(
        DocumentType type,
        int pageNumber,
        int pageSize,
        string searchTerm,
        int? orderId,
        CancellationToken cancellationToken = default);
}
