namespace OrderHub.UI.Commands;
public interface IAppCommands
{
    AuthCommands Auth { get; }
    NavigationCommands Navigation { get; }
    UpdateCommands Update { get; }
    SallaCommands Salla { get; }
    WhatsappCommands Whatsapp { get; }
    ApplicationCommands Application { get; }
    DashboardCommands Dashboard { get; }
    OrdersCommands Orders { get; }
    OrderDraftsCommands OrderDrafts { get; }
    ProductsCommands Products { get; }
    CategoriesCommands Categories { get; }
    CitiesCommands Cities { get; }
    ClientsCommands Clients { get; }
    SuppliersCommands Suppliers { get; }
    ShippingCarriersCommands ShippingCarriers { get; }
    DeliverymenCommands Deliverymen { get; }
    WhatsappGroupsCommands WhatsappGroups { get; }
    MessagesCommands Messages { get; }
    SettingsCommands Settings { get; }
    UsersCommands Users { get; }
    DocumentsCommands Documents { get; }
}