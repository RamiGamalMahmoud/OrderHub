using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderHub.Infrastructure;

internal class DesignTimeAppDbContextFactrory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();

        optionsBuilder
            .UseNpgsql("Server=db71346.public.databaseasp.net;Port=5432;Database=db71346;User Id=db71346;Password=W+k27dY#@5nP;Ssl Mode=Prefer;")
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}