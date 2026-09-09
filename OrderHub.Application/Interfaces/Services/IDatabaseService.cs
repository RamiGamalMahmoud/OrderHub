using OrderHub.Application.Configuration;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IDatabaseService
{
    Task<bool> CanConnectAsync();
    Task<bool> CanConnectAsync(DatabaseSettings databaseSettings);
    Task FixCategoriesAsync();
    Task<bool> HasPendingMigrationsAsync();
    Task MigrateAsync();
}
