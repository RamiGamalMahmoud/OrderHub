using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public class PrepareDirectoriesStep : IStartupStep
{
    private readonly IApplicationDirectoriesService _dirs;

    public PrepareDirectoriesStep(IApplicationDirectoriesService dirs)
    {
        _dirs = dirs;
    }

    public int Order => 1;
    public string DisplayName => "جاري تجهيز المجلدات";

    public Task ExecuteAsync()
    {
        _dirs.EnsureAppDirectoryCreated();
        _dirs.EnsureStorageDirectoryCreated();
        _dirs.EnsureDatabaseFileCreated();
        _dirs.EnsureWhatsAppProfilesDirectoryCreated();
        _dirs.EnsureLogsDirectoryCreated();

        return Task.CompletedTask;
    }
}
