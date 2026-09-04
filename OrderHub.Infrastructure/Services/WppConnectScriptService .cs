using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class WppConnectScriptService : IWppConnectScriptService
{
    private const string WPP_SCRIPT_URL =
        "https://cdn.jsdelivr.net/npm/@wppconnect/wa-js@latest/dist/wppconnect-wa.js";

    private readonly IApplicationDirectoriesService _directoriesService;

    public WppConnectScriptService(
        IApplicationDirectoriesService directoriesService)
    {
        _directoriesService = directoriesService;
    }

    public async Task<string> PrepareAsync()
    {
        string directoryPath = Path.Combine(
            Path.GetTempPath(),
            "OrderHub_WppConnect");

        Directory.CreateDirectory(directoryPath);

        string scriptFilePath = Path.Combine(
            directoryPath,
            "wppconnect-wa.js");

        if (File.Exists(scriptFilePath) &&
            new FileInfo(scriptFilePath).Length > 0)
        {
            return await File.ReadAllTextAsync(scriptFilePath);
        }

        return await DownloadAsync(scriptFilePath);
    }

    private static async Task<string> DownloadAsync(string scriptFilePath)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        string script = await httpClient.GetStringAsync(WPP_SCRIPT_URL);

        if (string.IsNullOrWhiteSpace(script))
        {
            throw new InvalidOperationException(
                "The downloaded WPP script is empty.");
        }

        await File.WriteAllTextAsync(scriptFilePath, script);
        return script;
    }
}