using Microsoft.EntityFrameworkCore;

namespace OrderHub.Infrastructure;

internal class AppDbContextFactory
{
    private readonly string _connectionString;

    public AppDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public virtual AppDbContext CreateDbContext()
    {
        DbContextOptionsBuilder<AppDbContext> builder = new();

        builder
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}