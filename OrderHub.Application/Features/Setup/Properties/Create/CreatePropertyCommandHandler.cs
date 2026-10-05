using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Create;

internal sealed class CreatePropertyCommandHandler(IPropertyRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<CreatePropertyCommand, int>
{
    public async Task<int> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNameAsync(request.Name, cancellationToken))
            throw new DomainException("A property with the same name already exists.");

        Property property = Property.Create(
            request.Name,
            request.PropertyType,
            description: request.desctiption,
            options: request.Options.Select(x => x.Value));

        repository.Add(property);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceException exception) when (exception.InnerException is not null)
        {
            // The original property operation surfaced the provider exception to its caller.
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
        return property.Id;
    }
}
