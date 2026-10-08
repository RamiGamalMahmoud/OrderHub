using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Contracts;

public interface IPropertyRepository
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task<bool> NameExistsForOtherAsync(string name, int id, CancellationToken cancellationToken);

    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken);

    Task<bool> AreOptionValuesInUseAsync(
        int propertyId,
        IReadOnlyCollection<string> values,
        CancellationToken cancellationToken);

    Task<Property> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task PrepareOptionRenamesAsync(int propertyId, CancellationToken cancellationToken);

    Task<Property> FindAsync(int id, CancellationToken cancellationToken);

    void Add(Property property);

    void Remove(Property property);
}
