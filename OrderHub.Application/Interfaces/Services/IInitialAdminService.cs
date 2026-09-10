using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;
public interface IInitialAdminService
{
    Task<bool> HasAdminAsync();
}