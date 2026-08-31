using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IOrderEntitySequenceService
{
    Task EnsureEntitySequencesAsync(Order order, CancellationToken cancellationToken);
}
