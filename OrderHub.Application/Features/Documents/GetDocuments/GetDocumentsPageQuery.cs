using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Documents.GetDocuments;

[RequirePermission(PermissionFeature.Documents, PermissionLevel.Read)]
public sealed record GetDocumentsPageQuery(
    DocumentType Type,
    int PageNumber = 1,
    int PageSize = 20,
    string SearchTerm = null,
    int? OrderId = null) : IRequest<PagedResult<DocumentListItem>>;

internal sealed class GetDocumentsPageQueryHandler(IDocumentReader reader)
    : IRequestHandler<GetDocumentsPageQuery, PagedResult<DocumentListItem>>
{
    public Task<PagedResult<DocumentListItem>> Handle(
        GetDocumentsPageQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Type is not (DocumentType.Invoice or DocumentType.Quotation or DocumentType.ProformaInvoice))
            throw new ArgumentOutOfRangeException(nameof(request.Type));

        if (request.OrderId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.OrderId));

        return reader.GetPageAsync(
            request.Type,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.OrderId,
            cancellationToken);
    }
}
