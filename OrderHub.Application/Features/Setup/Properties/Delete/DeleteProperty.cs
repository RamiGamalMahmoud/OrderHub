using MediatR;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Delete;

public static class DeleteProperty
{
public sealed record Command(int Id) : IRequest<Result>;

    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly IPropertyStore _propertyStore;

        public Handler(IPropertyStore propertyStore)
        {
            _propertyStore = propertyStore;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            return await _propertyStore.DeleteAsync(request.Id);
        }
    }
}
