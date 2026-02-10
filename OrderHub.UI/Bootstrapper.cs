using Microsoft.Extensions.Hosting;

namespace OrderHub.UI
{
    internal class Bootstrapper
    {
        private readonly IHost _host;
        private static Bootstrapper _bootstrapper;
        public static Bootstrapper Instance
        {
            get
            {
                _bootstrapper ??= new Bootstrapper();
                return _bootstrapper;
            }
        }

        private Bootstrapper()
        {
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddServices();
                })
                .Build();
        }
    }
}
