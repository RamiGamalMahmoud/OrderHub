namespace OrderHub.Application.Common;

public static class CacheKeys
{
    public const string AllCategories = "categories_all";
    public const string AllCities = "cities_all";
    public const string AllOrderStatuses = "order_statuses_all";
    public const string AllShippingCarriers = "shipping_carriers_all";
    
    public static string CategoryById(int id) => $"category_{id}";
    public static string CityById(int id) => $"city_{id}";
    public static string ProductById(int id) => $"product_{id}";
    public static string ClientById(int id) => $"client_{id}";
    public static string SupplierById(int id) => $"supplier_{id}";
    public static string OrderById(int id) => $"order_{id}";
    
    public static string ProductsByCategory(int categoryId) => $"products_category_{categoryId}";
    public static string ProductsPage(int pageNumber, int pageSize) => $"products_page_{pageNumber}_{pageSize}";
    public static string ClientsPage(int pageNumber, int pageSize) => $"clients_page_{pageNumber}_{pageSize}";
    public static string OrdersPage(int pageNumber, int pageSize) => $"orders_page_{pageNumber}_{pageSize}";
}
