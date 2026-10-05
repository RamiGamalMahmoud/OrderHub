using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Interfaces;

namespace OrderHub.Application.Features.Products.Delete;

internal sealed class DeleteProductHandler(IProductRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var id = request.Id;


        Product product = await repository.FindAsync(id, cancellationToken);

        if (product is null)
            return Result.Failure("المنتج غير موجود.");

        if (await repository.IsUsedByOrdersAsync(id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المنتج لأنه مرتبط بطلبات.");
        }

        repository.Remove(product);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف المنتج.");
        }
    }
}
