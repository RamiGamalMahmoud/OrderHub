using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.DependencyInjection;

namespace OrderHub.Web.Infrastructure;

public static class MigrationRunner
{
    public static async Task<int> RunAsync()
    {
        try
        {
            var builder = Host.CreateApplicationBuilder();

            if (builder.Environment.IsProduction())
            {
                ServerConfiguration.Add(builder.Configuration);
            }

            var connectionString = builder.Configuration
                .GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.Error.WriteLine(
                    "Database connection string is not configured.");

                return 3;
            }

            builder.Services.AddInfrastructure(connectionString);

            using var host = builder.Build();
            using var scope = host.Services.CreateScope();

            var database = scope.ServiceProvider
                .GetRequiredService<IDatabaseService>();

            Console.WriteLine("Checking database connection...");

            if (!await database.CanConnectAsync())
            {
                Console.Error.WriteLine(
                    "Cannot connect to the database.");

                return 1;
            }

            Console.WriteLine("Checking pending migrations...");

            if (!await database.HasPendingMigrationsAsync())
            {
                Console.WriteLine("Database is already up to date.");
                return 0;
            }

            Console.WriteLine("Applying database migrations...");

            await database.MigrateAsync();

            Console.WriteLine("Database migrations completed successfully.");

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Migration failed: {exception.Message}");

            return 2;
        }
    }
}