using OrderHub.Application.Configuration;

namespace OrderHub.Application.Interfaces.Services;

public interface IDatabaseConfigurationService
{
    DatabaseSettings GetSettings();

    void SaveSettings(DatabaseSettings settings);
}