using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Exceptions;
using OrderHub.Domain.Models;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Update;

internal sealed class UpdatePropertyCommandHandler(
    IPropertyRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePropertyCommand>
{
    public async Task Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        bool transactionEnded = false;

        try
        {
            await UpdateAsync(request, cancellationToken);
            transactionEnded = true;
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        finally
        {
            if (!transactionEnded)
            {
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }
    }

    private async Task UpdateAsync(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        Property property = await repository.GetForUpdateAsync(input.Id, cancellationToken)
            ?? throw new DomainException("الخاصية غير موجودة.");

        var options = input.Options?.ToList() ?? [];

        if (await repository.NameExistsForOtherAsync(input.Name?.Trim(), property.Id, cancellationToken))
        {
            throw new DomainException("توجد خاصية بهذا الاسم بالفعل.");
        }

        if (property.PropertyType != input.PropertyType &&
            await repository.IsInUseAsync(property.Id, cancellationToken))
        {
            throw new DomainException("لا يمكن تغيير نوع خاصية مستخدمة في منتجات أو طلبات.");
        }

        var changedValues = property.Options
            .Where(existing => !options.Any(option => option?.Id == existing.Id &&
                string.Equals(option.Value?.Trim(), existing.Value, StringComparison.Ordinal)))
            .Select(option => option.Value)
            .Distinct()
            .ToList();

        if (changedValues.Count > 0 &&
            await repository.AreOptionValuesInUseAsync(property.Id, changedValues, cancellationToken))
        {
            throw new DomainException("لا يمكن حذف أو تعديل خيار مستخدم في طلبات محفوظة.");
        }

        property.UpdateDefinition(
            input.Name,
            input.PropertyType,
            input.Description,
            options.Select(option => new PropertyOptionChange(option?.Id, option?.Value)).ToList());

        await repository.PrepareOptionRenamesAsync(property.Id, cancellationToken);

        try
        {
            // Persist the property and option diff once within the operation transaction.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
