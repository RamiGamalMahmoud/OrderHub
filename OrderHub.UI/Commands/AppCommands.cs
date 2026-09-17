namespace OrderHub.UI.Commands;

public sealed class AppCommands : IAppCommands
{
    public AppCommands(
        AuthCommands auth,
        NavigationCommands navigation,
        UpdateCommands update,
        SallaCommands salla,
        WhatsappCommands whatsapp,
        ApplicationCommands application,
        DashboardCommands dashboard,
        OrdersCommands orders,
        OrderDraftsCommands orderDrafts,
        DocumentsCommands documents,
        ProductsCommands products,
        CategoriesCommands categories,
        CitiesCommands cities,
        ClientsCommands clients,
        SuppliersCommands suppliers,
        ShippingCarriersCommands shippingCarriers,
        DeliverymenCommands deliverymen,
        WhatsappGroupsCommands whatsappGroups,
        MessagesCommands messages,
        SettingsCommands settings,
        UsersCommands users)
    {
        Auth = auth;
        Navigation = navigation;
        Update = update;
        Salla = salla;
        Whatsapp = whatsapp;
        Application = application;
        Dashboard = dashboard;
        Orders = orders;
        OrderDrafts = orderDrafts;
        Documents = documents;
        Products = products;
        Categories = categories;
        Cities = cities;
        Clients = clients;
        Suppliers = suppliers;
        ShippingCarriers = shippingCarriers;
        Deliverymen = deliverymen;
        WhatsappGroups = whatsappGroups;
        Messages = messages;
        Settings = settings;
        Users = users;
    }

    public static IAppCommands Instance { get; private set; } = null!;

    public AuthCommands Auth { get; }
    public NavigationCommands Navigation { get; }
    public UpdateCommands Update { get; }
    public SallaCommands Salla { get; }
    public WhatsappCommands Whatsapp { get; }
    public ApplicationCommands Application { get; }
    public DashboardCommands Dashboard { get; }
    public OrdersCommands Orders { get; }
    public OrderDraftsCommands OrderDrafts { get; }
    public DocumentsCommands Documents { get; }
    public ProductsCommands Products { get; }
    public CategoriesCommands Categories { get; }
    public CitiesCommands Cities { get; }
    public ClientsCommands Clients { get; }
    public SuppliersCommands Suppliers { get; }
    public ShippingCarriersCommands ShippingCarriers { get; }
    public DeliverymenCommands Deliverymen { get; }
    public WhatsappGroupsCommands WhatsappGroups { get; }
    public MessagesCommands Messages { get; }
    public SettingsCommands Settings { get; }
    public UsersCommands Users { get; }

    public static void SetInstance(IAppCommands instance)
    {
        Instance ??= instance;
    }
}