using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

public sealed class GitHubUpdateService : IUpdateService
{
    private const string OWNER = "RamiGamalMahmoud";
    private const string REPOSITORY = "OrderHubInstaller";
    private const string INSTALLER_FILE_NAME = "OrderHub_setup.exe";

    private readonly HttpClient _httpClient;
    private readonly IApplicationContext _applicationContext;

    public GitHubUpdateService(
        HttpClient httpClient,
        IApplicationContext applicationContext)
    {
        _httpClient = httpClient;
        _applicationContext = applicationContext;
    }

    public async Task<UpdateInfo> CheckForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        Version currentVersion = new Version(_applicationContext.Application.Version);

        string url = $"https://api.github.com/repos/{OWNER}/{REPOSITORY}/releases/latest";

        using HttpRequestMessage request =
            new(HttpMethod.Get, url);

        request.Headers.UserAgent.ParseAdd("OrderHub");
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        GitHubRelease release =
            await response.Content.ReadFromJsonAsync<GitHubRelease>(
                cancellationToken);

        if (release is null)
            return null;

        Version latestVersion =
            ParseVersion(release.TagName);

        if (latestVersion is null)
            return null;

        bool needToUpdate = latestVersion > currentVersion;

        GitHubAsset installer =
            release.Assets?
                .FirstOrDefault(asset =>
                    asset.Name.Equals(
                        INSTALLER_FILE_NAME,
                        StringComparison.OrdinalIgnoreCase));

        if (installer is null)
            return null;

        return new UpdateInfo(
            CurrentVersion: currentVersion,
            LatestVersion: latestVersion,
            ReleaseName: release.Name ?? string.Empty,
            ReleaseNotes: release.Body ?? string.Empty,
            DownloadUrl: installer.BrowserDownloadUrl,
            InstallerFileName: installer.Name,
            NeedToUpdate: needToUpdate);
    }

    public async Task<string> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<double> progress = null,
        CancellationToken cancellationToken = default)
    {
        string updateDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "OrderHub - Downloads",
                "Updates");

        Directory.CreateDirectory(updateDirectory);

        string downloadedFileName = $"{Path.GetFileNameWithoutExtension(update.InstallerFileName)}-{update.LatestVersion}.exe";

        string installerPath =
            Path.Combine(
                updateDirectory,
                downloadedFileName);

        using HttpRequestMessage request =
            new(HttpMethod.Get, update.DownloadUrl);

        request.Headers.UserAgent.ParseAdd("OrderHub");

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        long? totalBytes =
            response.Content.Headers.ContentLength;

        await using Stream input =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        await using FileStream output =
            new(
                installerPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

        byte[] buffer = new byte[81920];

        long totalRead = 0;

        while (true)
        {
            int bytesRead =
                await input.ReadAsync(
                    buffer,
                    cancellationToken);

            if (bytesRead == 0)
                break;

            await output.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken);

            totalRead += bytesRead;

            if (totalBytes is > 0)
            {
                double percentage =
                    totalRead * 100d / totalBytes.Value;

                progress?.Report(percentage);
            }
        }

        progress?.Report(100);

        return installerPath;
    }

    public void RunInstaller(string installerPath)
    {
        if (!File.Exists(installerPath))
        {
            throw new FileNotFoundException(
                "Installer file was not found.",
                installerPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true
        });
    }

    private static Version ParseVersion(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return null;

        string value =
            tagName.Trim().TrimStart('v', 'V');

        return Version.TryParse(
            value,
            out Version version)
            ? version
            : null;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}