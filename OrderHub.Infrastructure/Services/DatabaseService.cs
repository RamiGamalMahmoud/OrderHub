using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services
{
    internal class DatabaseService : IDatabaseService
    {
        private readonly AppDbContextFactory _appDbContextFactory;

        public DatabaseService(AppDbContextFactory appDbContextFactory)
        {
            _appDbContextFactory = appDbContextFactory;
        }

        public async Task<bool> CanConnectAsync()
        {
            using (AppDbContext context = _appDbContextFactory.CreateDbContext())
            {
                return await context.Database.CanConnectAsync();
            }
        }

        public async Task<bool> HasPendingMigrationsAsync()
        {
            using(AppDbContext context = _appDbContextFactory.CreateDbContext())
            {
                IEnumerable<string> pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                return pendingMigrations.Any();
            }
        }

        public async Task MigrateAsync()
        {
            using (AppDbContext context = _appDbContextFactory.CreateDbContext())
            {
                await context.Database.ExecuteSqlRawAsync("DELETE FROM '__EFMigrationsLock';");
                await context.Database.MigrateAsync();
            }
        }

        public async Task FixCategoriesAsync()
        {
            using (AppDbContext context = _appDbContextFactory.CreateDbContext())
            {
                await context.Database.ExecuteSqlRawAsync("UPDATE categories SET parent_category_id = NULL WHERE id = parent_category_id");
            }
        }
    }
}
