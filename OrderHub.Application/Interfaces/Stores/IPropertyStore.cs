using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using OrderHub.Application.Features.Setup.Properties.Update;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IPropertyStore
{
    // Queries

    Task<IReadOnlyList<PropertyListDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PropertyDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    // Commands

    Task<int> CreateAsync(Property property, CancellationToken cancellationToken = default);
    Task UpdateAsync(PropertyUpdateDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);


}
