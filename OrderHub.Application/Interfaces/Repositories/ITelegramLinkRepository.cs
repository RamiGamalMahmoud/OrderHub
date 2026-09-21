using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface ITelegramLinkRepository
{
    void Add(TelegramLink link);
    Task<TelegramLink> GetByToken(string token);
}
