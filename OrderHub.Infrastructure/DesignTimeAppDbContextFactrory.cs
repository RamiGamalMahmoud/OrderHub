using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using OrderHub.Application.Configuration;

namespace OrderHub.Infrastructure;

internal class DesignTimeAppDbContextFactrory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
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

        DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();

        optionsBuilder
            .UseNpgsql(connectionStringBuilder.ConnectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}