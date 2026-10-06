using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.OrderDrafts.Save;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record SaveDraftCommand(Guid DraftId, OrderDraftData Data) : IRequest<Result>;

internal sealed class SaveDraftCommandHandler(ICurrentUser currentUser, IDraftRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<SaveDraftCommand, Result>
{
    public async Task<Result> Handle(SaveDraftCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = DraftUser.Require(currentUser);
            await repository.StageSaveAsync(new Draft(request.DraftId, userId, DateTime.UtcNow, request.Data), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (UnauthorizedAccessException exception) { return Result.Failure(exception.Message); }
        catch (PersistenceException exception) { return Result.Failure(exception.Message); }
    }
}
