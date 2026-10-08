namespace OrderHub.Web.Infrastructure;

public static class ServerConfiguration
{
    public static void Add(IConfigurationBuilder configuration)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "OrderHub",
            "Config",
            "appsettings.Server.json");

        configuration.AddJsonFile(
            path,
            optional: false,
            reloadOnChange: false);
    }
}