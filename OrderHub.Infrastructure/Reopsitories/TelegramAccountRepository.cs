using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal class TelegramAccountRepository : ITelegramAccountRepository
{
    private readonly AppDbContext _appDbContext;

    public TelegramAccountRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public void Add(TelegramAccount telegramAccount)
    {
        _appDbContext.TelegramAccounts.Add(telegramAccount);
    }

    public async Task<TelegramAccount> GetByOwner(TelegramAccountOwnerType ownerType, int ownerId)
    {
        return await _appDbContext
            .TelegramAccounts
            .SingleOrDefaultAsync(account => account.OwnerType == ownerType && account.OwnerId == ownerId);
    }

    public async Task<TelegramAccount> GetByTelegramUserId(long telegramUserId)
    {
        return await _appDbContext.TelegramAccounts
            .SingleOrDefaultAsync(account => account.TelegramUserId == telegramUserId);
    }
}
