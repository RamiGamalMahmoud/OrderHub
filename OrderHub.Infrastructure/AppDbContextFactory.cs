using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;

namespace OrderHub.Infrastructure;

internal class AppDbContextFactory(IApplicationDirectoriesService applicationDirectoriesService)
{
    public AppDbContext CreateDbContext()
    {
        DbContextOptionsBuilder<AppDbContext> dbContextOptionsBuilder = new();
        dbContextOptionsBuilder
            .UseSqlite($"Data Source={applicationDirectoriesService.DatabaseFilePath}")
            .UseSnakeCaseNamingConvention();
        AppDbContext appDbContext = new(dbContextOptionsBuilder.Options);
        return appDbContext;
    }
}
