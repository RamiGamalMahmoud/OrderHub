using OrderHub.Application.Infrastructure.Installation;

namespace OrderHub.Application.Context;
public sealed record ApplicationInfo(
    InstallationType InstallationType,
    string Version);