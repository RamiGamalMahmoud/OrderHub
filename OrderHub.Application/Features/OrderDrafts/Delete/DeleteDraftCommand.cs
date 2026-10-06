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

namespace OrderHub.Application.Features.OrderDrafts.Delete;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
public sealed record DeleteDraftCommand(Guid DraftId) : IRequest<Result>;

internal sealed class DeleteDraftCommandHandler(ICurrentUser currentUser, IDraftRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteDraftCommand, Result>
{
    public async Task<Result> Handle(DeleteDraftCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = DraftUser.Require(currentUser);
            await repository.StageDeleteAsync(request.DraftId, userId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (UnauthorizedAccessException exception) { return Result.Failure(exception.Message); }
        catch (PersistenceException exception) { return Result.Failure(exception.Message); }
    }
}
