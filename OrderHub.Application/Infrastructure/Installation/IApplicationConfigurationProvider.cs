namespace OrderHub.Application.Infrastructure.Installation;

public interface IApplicationConfigurationProvider
{
    ApplicationConfiguration GetConfiguration();
}