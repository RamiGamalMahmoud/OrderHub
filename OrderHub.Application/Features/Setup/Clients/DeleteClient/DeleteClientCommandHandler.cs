using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class DeleteClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    ITripUsageReader tripUsageReader) : IRequestHandler<DeleteClientCommand, Result>
{
    public async Task<Result> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        Client client = await repository.FindAsync(request.Id, cancellationToken);
        if (client is null)
        {
            return Result.Failure("العميل غير موجود.");
        }

        if (await repository.HasBusinessReferencesAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف العميل لأنه مرتبط بطلبات أو مسودات طلبات.");
        }

        if (await tripUsageReader.HasPartyTripsAsync(TripPartyType.Client, request.Id, cancellationToken))
        {
            return Result.Failure(
                "لا يمكن حذف العميل لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة.",
                "Conflict");
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
