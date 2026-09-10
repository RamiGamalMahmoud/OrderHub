using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdateAsync(
        CancellationToken cancellationToken = default);

    Task<string> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default);

    void RunInstaller(string installerPath);
}

public sealed record UpdateInfo(
    Version CurrentVersion,
    Version LatestVersion,
    string ReleaseName,
    string ReleaseNotes,
    string DownloadUrl,
    string InstallerFileName,
    bool NeedToUpdate);