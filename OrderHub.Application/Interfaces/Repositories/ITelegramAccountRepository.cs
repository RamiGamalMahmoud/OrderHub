using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface ITelegramAccountRepository
{
    Task<TelegramAccount> GetByOwner(TelegramAccountOwnerType ownerType, int ownerId);
    Task<TelegramAccount> GetByTelegramUserId(long telegramUserId);
    void Add(TelegramAccount telegramAccount);
}
