using MediatR;
using OrderHub.Application.Interfaces;
using System;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces.Repositories;

namespace OrderHub.Application.Features.Orders.Delete;

public static class DeleteOrder
{
    public record Command(int OrderId) : IRequest<Result>;
    internal class Handler(IOrderRepository orderRepository, IUnitOfWork unitOfWork) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var order = await orderRepository.GetForDeleteAsync(request.OrderId, cancellationToken);
            if (order is null)
                // Preserve the legacy EF Remove(null) failure message without attempting a commit.
                return Result.Failure(new ArgumentNullException("entity").Message);
            try
            {
                orderRepository.Remove(order);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(exception.Message);
            }
        }
    }
}
