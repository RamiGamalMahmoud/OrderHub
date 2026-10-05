using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Update;

internal sealed class UpdatePropertyCommandHandler(IPropertyRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<UpdatePropertyCommand>
{
    public async Task Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;


        Property property = await repository.GetForUpdateAsync(dto.Id, cancellationToken)
            ?? throw new Domain.Exceptions.DomainException("Property not found.");

        property.Rename(dto.Name);
        property.ChangeType(dto.PropertyType);
        property.ChangeDescription(dto.Description);

        HashSet<int> optionIds = dto.Options
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        foreach (PropertyOption option in property.Options.ToList())
        {
            if (!optionIds.Contains(option.Id))
            {
                property.RemoveOption(option.Id);
            }
        }

        foreach (PropertyOptionUpdate optionUpdateDto in dto.Options)
        {
            if (!optionUpdateDto.Id.HasValue)
            {
                property.AddOption(optionUpdateDto.Value);
            }

            if (optionUpdateDto.Id.HasValue && property.Options.Any(x => x.Id == optionUpdateDto.Id.Value))
            {
                PropertyOption propertyOption = property.Options.First(x => x.Id == optionUpdateDto.Id.Value);
                if (propertyOption.Value != optionUpdateDto.Value)
                {
                    propertyOption.Rename(optionUpdateDto.Value);
                }
            }
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceException exception) when (exception.InnerException is not null)
        {
            // Preserve the prior exception type and diagnostics without an EF Core dependency.
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }

    }
}
