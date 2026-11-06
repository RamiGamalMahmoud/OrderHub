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
public sealed record DeleteOldDraftsCommand(int OlderThanDays = 30) : IRequest<Result<int>>;

internal sealed class DeleteOldDraftsCommandHandler(
    ICurrentUser currentUser,
    IDraftRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : IRequestHandler<DeleteOldDraftsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(DeleteOldDraftsCommand request, CancellationToken cancellationToken)
    {
        if (request.OlderThanDays <= 0)
            return Result<int>.Failure("عدد الأيام يجب أن يكون أكبر من صفر.");

        try
        {
            int userId = DraftUser.Require(currentUser);
            DateTime cutoff = clock.GetUtcNow().UtcDateTime.AddDays(-request.OlderThanDays);
            int deleted = await repository.StageDeleteBeforeAsync(userId, cutoff, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(deleted);
        }
        catch (UnauthorizedAccessException exception) { return Result<int>.Failure(exception.Message); }
        catch (PersistenceException exception) { return Result<int>.Failure(exception.Message); }
    }
}
