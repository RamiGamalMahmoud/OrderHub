using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal class TelegramLinkRepository : ITelegramLinkRepository
{
    private readonly AppDbContext _appDbContext;

    public TelegramLinkRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public void Add(TelegramLink link)
    {
        _appDbContext.TelegramLinks.Add(link);
    }

    public async Task<TelegramLink> GetByToken(string token)
    {
        return await _appDbContext
            .TelegramLinks
            .SingleOrDefaultAsync(link => link.Token == token);
    }
}
