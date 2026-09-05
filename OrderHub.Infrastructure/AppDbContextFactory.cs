using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Services;

namespace OrderHub.Infrastructure;

internal class AppDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly IDatabaseConfigurationService _databaseConfigurationService;

    public AppDbContextFactory(IDatabaseConfigurationService databaseConfigurationService)
    {
        _databaseConfigurationService = databaseConfigurationService;
    }

    // Used by tests
    public AppDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public virtual AppDbContext CreateDbContext()
    {
        if (_options != null)
            return new AppDbContext(_options);

        DatabaseSettings databaseSettings = new()
        {
            Database = "orderhub_dev"
        };

        NpgsqlConnectionStringBuilder connectionStringBuilder = new()
        {
            Host = databaseSettings.Host,
            Port = databaseSettings.Port,
            Database = databaseSettings.Database,
            Username = databaseSettings.Username,
            Password = databaseSettings.Password
        };

        DbContextOptionsBuilder<AppDbContext> builder = new();

        builder
            .UseNpgsql(connectionStringBuilder.ConnectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}