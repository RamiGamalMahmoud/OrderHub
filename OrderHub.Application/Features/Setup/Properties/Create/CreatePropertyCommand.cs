using MediatR;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Create;

public sealed record CreatePropertyCommand(
    string Name,
    PropertyType PropertyType,
    IReadOnlyCollection<PropertyOptionCreateDto> Options)
    : IRequest<int>;

internal sealed class CreatePropertyCommandHandler(IPropertyStore propertyStore) : IRequestHandler<CreatePropertyCommand, int>
{
    public async Task<int> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        if (await propertyStore.ExistsByNameAsync(request.Name, cancellationToken))
            throw new DomainException("A property with the same name already exists.");

        Property property = Property.Create(
            request.Name,
            request.PropertyType,
            request.Options.Select(x => x.Value));

        return await propertyStore.CreateAsync(property, cancellationToken);
    }
}