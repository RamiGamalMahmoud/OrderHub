using Microsoft.Win32;
using OrderHub.Application.Infrastructure.Installation;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OrderHub.Infrastructure.Services;

public sealed class ServerConfiguration : IServerConfiguration
{
    private const string REGISTRY_PATH = @"SOFTWARE\Almasajid\OrderHub";
    private const string SERVER_ADDRESS_VALUE_NAME = "ServerAddress";

    private readonly IApplicationConfigurationProvider _configurationProvider;
    private readonly Lazy<string> _localNetworkAddress;

    public ServerConfiguration(
        IApplicationConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider;

        _localNetworkAddress = new Lazy<string>(
            GetLocalNetworkAddress);
    }

    public string ServerAddress
    {
        get
        {
            var configuration =
                _configurationProvider.GetConfiguration();

            if (configuration.InstallationType != InstallationType.Client)
                return _localNetworkAddress.Value;

            return GetServerAddress()
                ?? throw new InvalidOperationException(
                    "Server address is not configured.");
        }
    }

    public string ApiBaseUrl =>
        $"http://{ServerAddress}:5000/api/";

    private static string GetServerAddress()
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64);

        using RegistryKey key =
            baseKey.OpenSubKey(REGISTRY_PATH);

        return key?
            .GetValue(SERVER_ADDRESS_VALUE_NAME)?
            .ToString();
    }

    private static string GetLocalNetworkAddress()
    {
        var networkInterface = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(x =>
                x.OperationalStatus == OperationalStatus.Up &&
                x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                x.GetIPProperties()
                    .GatewayAddresses
                    .Any(g =>
                        g.Address.AddressFamily ==
                        AddressFamily.InterNetwork))
            .OrderBy(x =>
                x.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                    ? 0
                    : x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                        ? 1
                        : 2)
            .FirstOrDefault();

        var address = networkInterface?
            .GetIPProperties()
            .UnicastAddresses
            .FirstOrDefault(x =>
                x.Address.AddressFamily ==
                AddressFamily.InterNetwork)?
            .Address
            .ToString();

        return address
            ?? throw new InvalidOperationException(
                "Local network address could not be determined.");
    }
}