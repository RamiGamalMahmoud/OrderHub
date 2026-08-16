using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Models;
using System.Linq;

namespace OrderHub.Infrastructure.ReadServices
{
    internal class ProductReadService
    {
        private readonly AppDbContextFactory _contextFactory;

        public ProductReadService(AppDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public IQueryable<Product> GetProducts()
        {
            using AppDbContext dbContext = _contextFactory.CreateDbContext();
            var products = dbContext.Products.AsNoTracking();
            return products;
        }
    }
}
