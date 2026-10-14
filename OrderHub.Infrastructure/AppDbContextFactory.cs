using Microsoft.EntityFrameworkCore;

namespace OrderHub.Infrastructure;

internal class AppDbContextFactory
{
    private readonly string _connectionString;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    internal AppDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public virtual AppDbContext CreateDbContext()
    {
        if (_options is not null)
            return new AppDbContext(_options);

        DbContextOptionsBuilder<AppDbContext> builder = new();

        builder
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}
