namespace OrderHub.Application.Infrastructure.Installation;

public record ApplicationConfiguration(
    InstallationType InstallationType,
    string Version);