using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class FileAppLogger : IAppLogger
{
    private readonly IApplicationDirectoriesService _directoriesService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileAppLogger(IApplicationDirectoriesService directoriesService)
    {
        _directoriesService = directoriesService;
    }

    public Task LogInfoAsync(string message) => WriteAsync("INFO", message);

    public Task LogErrorAsync(string message, Exception exception = null)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(message);

        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        return WriteAsync("ERROR", builder.ToString().TrimEnd());
    }

    private async Task WriteAsync(string level, string message)
    {
        string logFilePath = Path.Combine(_directoriesService.LogsDirectory, $"orderhub-{DateTime.Now:yyyy-MM-dd}.log");
        string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}{Environment.NewLine}";

        await _lock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(logFilePath, entry, Encoding.UTF8);
        }
        finally
        {
            _lock.Release();
        }
    }
}
