using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Delete;

internal sealed class DeletePropertyHandler(IPropertyRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeletePropertyCommand, Result>
{
    public async Task<Result> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var id = request.Id;


        Property property = await repository.FindAsync(id, cancellationToken);

        if (property is not null)
        {
            repository.Remove(property);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (System.Exception)
            {
                return Result.Failure();
            }
        }
        return Result.Failure();
    }
}
