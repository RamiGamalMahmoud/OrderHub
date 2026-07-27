using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Delete;

public record DeleteProductCommand(int Id) : IRequest<Result>;

internal class DeleteProductCommandHandler(IProductStore productStore) : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        return await productStore.DeleteAsync(request.Id);
    }
}
