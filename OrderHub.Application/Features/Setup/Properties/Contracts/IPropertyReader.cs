using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Features.Setup.Properties.GetAll;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Contracts;

public interface IPropertyReader
{
    Task<IReadOnlyList<PropertyListItem>> GetAllAsync(CancellationToken cancellationToken);
    Task<PropertyDetails> GetByIdAsync(int id, CancellationToken cancellationToken);
}
