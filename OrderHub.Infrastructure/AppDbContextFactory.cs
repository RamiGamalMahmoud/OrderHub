using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Services;
using System;

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

        DatabaseSettings databaseSettings = _databaseConfigurationService.GetSettings();

        if (databaseSettings is null)
            throw new InvalidOperationException(
                "Database configuration has not been configured.");

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

    internal AppDbContext CreateDbContext(string host, int port, string database, string username, string password)
    {
        NpgsqlConnectionStringBuilder connectionStringBuilder = new()
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password
        };

        DbContextOptionsBuilder<AppDbContext> builder = new();

        builder
            .UseNpgsql(connectionStringBuilder.ConnectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(builder.Options);
    }
}