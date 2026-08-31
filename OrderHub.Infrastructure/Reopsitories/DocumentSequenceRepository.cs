using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Models;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal class DocumentSequenceRepository : IDocumentSequenceRepository
{
    private readonly AppDbContext _context;

    public DocumentSequenceRepository(AppDbContext context)
    {
        _context = context;
        Debug.WriteLine($"AppDbContext from {GetType().Name} - {context.GetHashCode()}");
    }

    public async Task<int> ReserveNextNumberAsync(
        DocumentType documentType,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var sequence = await _context.DocumentSequences
            .SingleOrDefaultAsync(
                x => x.DocumentType == documentType &&
                     x.Year == year &&
                     x.Month == month,
                cancellationToken);

        if (sequence is null)
        {
            sequence = DocumentSequence.Create(documentType, year, month);

            _context.DocumentSequences.Add(sequence);
        }

        sequence.Increment();

        return sequence.LastNumber;
    }
}