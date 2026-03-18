using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;

namespace OrderHub.Infrastructure;

internal class AppDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IApplicationDirectoriesService _directories;

    public AppDbContextFactory(IApplicationDirectoriesService directories)
    {
        _directories = directories;
    }

    public AppDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public AppDbContext CreateDbContext()
    {
        if (_options != null)
            return new AppDbContext(_options);

        DbContextOptionsBuilder<AppDbContext> builder = new DbContextOptionsBuilder<AppDbContext>();

        builder.UseSqlite($"Data Source={_directories.DatabaseFilePath}")
               .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}
