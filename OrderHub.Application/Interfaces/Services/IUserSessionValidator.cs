using OrderHub.Application.Context;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;
public interface IUserSessionValidator
{
    Task<bool> IsValidAsync(UserSession session);
}