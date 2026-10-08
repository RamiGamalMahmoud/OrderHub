using OrderHub.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IDocumentSequenceRepository
{
    Task<int> ReserveNextNumberAsync(DocumentType documentType, int year, int month, CancellationToken cancellationToken = default);
}