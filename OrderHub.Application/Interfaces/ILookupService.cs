using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces;

public interface ILookupService
{
    Task<IEnumerable<CategoryLookup>> GetCategories();
    Task<IEnumerable<CityLookup>> GetCities();
    Task<IEnumerable<DeliverymanLookup>> GetDeliveryMethods();
    Task<IEnumerable<ShippingCarrierLookup>> GetShippingCarriers();
    Task<IEnumerable<SupplierLookup>> GetSuppliers();
    Task<IEnumerable<ClientLookup>> GetClients();
    Task<IEnumerable<PaymentMethodLookup>> GetPaymentMethods();
}
