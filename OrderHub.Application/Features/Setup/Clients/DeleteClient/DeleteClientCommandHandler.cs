using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Clients.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class DeleteClientCommandHandler(IClientRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteClientCommand, Result>
{
    public async Task<Result> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {

        Client client = await repository.FindAsync(request.Id, cancellationToken);
        if (client is null)
        {
            return Result.Failure("العميل غير موجود.");
        }

        if (await repository.IsUsedByOrdersAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف العميل لأنه مرتبط بطلبات.");
        }

        repository.Remove(client);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف العميل.");
        }
    }
}
