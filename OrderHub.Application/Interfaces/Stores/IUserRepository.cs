using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Stores;
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> GetByUsername(string username);
}
