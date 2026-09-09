using Microsoft.Win32;
using OrderHub.Application.Infrastructure.Installation;
using System;

namespace OrderHub.Infrastructure.Application.Installation;

public sealed class ApplicationConfigurationProvider : IApplicationConfigurationProvider
{
    private const string REGISTRY_PATH = @"Software\Almasajid\OrderHub";
    private const string INSTALLATION_TYPE_VALUE_NAME = "InstallationType";
    private const string VERSION_VALUE_NAME = "Version";

    public ApplicationConfiguration GetConfiguration()
    {
        using var key = Registry.LocalMachine.OpenSubKey(REGISTRY_PATH);

        if (key is null)
            throw new InvalidOperationException("OrderHub application configuration was not found.");

        var installationTypeValue = key.GetValue(INSTALLATION_TYPE_VALUE_NAME);

        if (installationTypeValue is not string installationTypeString)
            throw new InvalidOperationException("OrderHub installation type is missing.");

        if (!Enum.TryParse<InstallationType>(installationTypeString, true, out var installationType))
            throw new InvalidOperationException($"Invalid OrderHub installation type: '{installationTypeString}'.");

        var version = key.GetValue(VERSION_VALUE_NAME) as string;

        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("OrderHub application version is missing.");

        return new ApplicationConfiguration(installationType, version);
    }
}