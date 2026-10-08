using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Create;

internal sealed class CreatePropertyCommandHandler(
    IPropertyRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePropertyCommand, int>
{
    public async Task<int> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        Property property = Property.Create(
            request.Name,
            request.PropertyType,
            request.Description,
            request.Options?.Select(option => option?.Value));

        if (await repository.ExistsByNameAsync(property.Name, cancellationToken))
        {
            throw new DomainException("توجد خاصية بهذا الاسم بالفعل.");
        }

        repository.Add(property);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

        return property.Id;
    }
}
