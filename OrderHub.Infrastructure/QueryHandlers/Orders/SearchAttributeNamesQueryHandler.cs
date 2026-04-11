using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.Queries.OrderQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Orders;

internal class SearchAttributeNamesQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<SearchAttributeNamesQuery, IEnumerable<AttributeNameDto>>
{
    public async Task<IEnumerable<AttributeNameDto>> Handle(SearchAttributeNamesQuery request, CancellationToken cancellationToken)
    {
        if(string.IsNullOrEmpty( request.SearchTerm))
            return Enumerable.Empty<AttributeNameDto>();
        var searchTerm = request.SearchTerm.Trim();
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext.AttributeNames
            .Where(a => a.Name.Contains(searchTerm))
            .Select(a => new AttributeNameDto(a.Name))
            .ToListAsync(cancellationToken);
    }
}
