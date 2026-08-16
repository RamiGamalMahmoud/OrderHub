using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.ReadServices;

internal class LookupService : ILookupService
{
    private readonly AppDbContextFactory _contextFactory;

    public LookupService(AppDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IEnumerable<CategoryLookup>> GetCategories()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.Categories
            .AsNoTracking()
            .Select(c => new CategoryLookup(c.Id, c.Name.Value, c.FullPath, c.SubCategories.Count > 0, c.ParentCategoryId))
            .ToListAsync();
    }

    public async Task<IEnumerable<CityLookup>> GetCities()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.Cities
            .AsNoTracking()
            .Select(x => new CityLookup(x.Id, x.Name.Value))
            .ToListAsync();
    }

    public async Task<IEnumerable<ClientLookup>> GetClients()
    {
        using AppDbContext dbContext= _contextFactory.CreateDbContext();
        return await dbContext.Clients
            .AsNoTracking()
            .Select(x => new ClientLookup(x.Id, x.Name.Value, x.Phone.Number.FullNumber, x.Address.FullAddress))
            .ToListAsync();
    }

    public async Task<IEnumerable<DeliverymanLookup>> GetDeliveryMethods()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.Deliverymen
            .AsNoTracking()
            .Select(x => new DeliverymanLookup(x.Id, x.Name.Value))
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentMethodLookup>> GetPaymentMethods()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.PaymentMethods
            .Select(x => new PaymentMethodLookup(x.Id, x.DisplayName, x.DisplayName, x.IsActive))
            .ToListAsync();
    }

    public async Task<IEnumerable<ShippingCarrierLookup>> GetShippingCarriers()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.ShippingCarriers
            .Select(x => new ShippingCarrierLookup(x.Id, x.Name.Value))
            .ToListAsync();
    }

    public async Task<IEnumerable<SupplierLookup>> GetSuppliers()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.Suppliers
            .Select(x => new SupplierLookup(x.Id, x.Name.Value))
            .ToListAsync();
    }
}
