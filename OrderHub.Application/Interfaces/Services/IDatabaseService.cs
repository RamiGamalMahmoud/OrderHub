using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IDatabaseService
{
    Task<bool> CanConnectAsync();
    Task FixCategoriesAsync();
    Task<bool> HasPendingMigrationsAsync();
    Task MigrateAsync();
}
