using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace OrderHub.Infrastructure;

internal class DesignTimeAppDbContextFactrory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        NpgsqlConnectionStringBuilder connectionStringBuilder = new()
        {
            Host = "localhost",
            Port = 5432,
            Database = "orderhub",
            Username = "postgres",
            Password = "12015",
        };

        DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();

        optionsBuilder
            .UseNpgsql(connectionStringBuilder.ConnectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}