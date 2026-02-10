using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ProductDtos;
using static OrderHub.Application.Queries.ProductQueries;

namespace OrderHub.Infrastructure.QueryHandlers.Products;

internal class GetProductForEditQueryHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<GetProductForEditQuery, ProductEdtiDto>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<ProductEdtiDto> Handle(GetProductForEditQuery request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Product product = await appDbContext
            .Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Suppliers)
            .Where(p => p.Id == request.Id)
            .FirstOrDefaultAsync();

        return new ProductEdtiDto(
            product.Id,
            product.Name.Value,
            product.Code,
            product.Price.Value,
            product.Category.Id,
            product.Suppliers.Select(s => s.Id));
    }
}
