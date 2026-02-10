using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderHub.Infrastructure;

internal class DesignTimeAppDbContextFactrory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AppDbContext> dbContextOptionsBuilder = new();
        dbContextOptionsBuilder
            .UseSqlite("Data Source=./Data/order_hub.db")
            .UseSnakeCaseNamingConvention();
        AppDbContext appDbContext = new(dbContextOptionsBuilder.Options);
        return appDbContext;
    }
}
