using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;

internal class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly AppDbContext _dbContext;

    public OutboxMessageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void AddRange(IEnumerable<OutboxMessage> messages)
    {
        _dbContext.OutboxMessages.AddRange(messages);
    }

    public async Task<OutboxMessage> Create(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
        return message;
    }

    public Task DeleteAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.OutboxMessages.Remove(message);
        return Task.CompletedTask;
    }

    public async Task<OutboxMessage> GetForResend(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .Include(m => m.Recipient)
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
}
