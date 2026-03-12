using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Orders
{
    internal class ChangePaymentMethodCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<ChangePaymentMethodCommand, Result>
    {
        private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

        public async Task<Result> Handle(ChangePaymentMethodCommand request, CancellationToken cancellationToken)
        {
            using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
            Order order = await appDbContext.Orders.Where(o => o.Id == request.OrderId).FirstOrDefaultAsync();
            order.PaymentMethodId = request.PaymentMethodId;
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
    }
}
