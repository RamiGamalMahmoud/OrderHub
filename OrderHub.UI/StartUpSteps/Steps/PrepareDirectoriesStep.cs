using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

public class PrepareDirectoriesStep : IStartupStep
{
    private readonly IApplicationDirectoriesService _dirs;

    public PrepareDirectoriesStep(IApplicationDirectoriesService dirs)
    {
        _dirs = dirs;
    }

    public int Order => (int)StartUpdStepsOrder.PrepareDirectories;
    public string DisplayName => "جاري تجهيز المجلدات";

    public bool IsEnabled => true;

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
