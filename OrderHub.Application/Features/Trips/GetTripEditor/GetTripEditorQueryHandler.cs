using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Trips.GetTripEditor;

internal sealed class GetTripEditorQueryHandler(ITripReader reader)
    : IRequestHandler<GetTripEditorQuery, Result<TripEditorData>>
{
    public async Task<Result<TripEditorData>> Handle(
        GetTripEditorQuery request,
        CancellationToken cancellationToken)
    {
        var editor = await reader.GetEditorAsync(request.Id, cancellationToken);

        return editor is null
            ? Result<TripEditorData>.Failure("المشوار غير موجود.", "NotFound")
            : Result<TripEditorData>.Success(editor);
    }
}
