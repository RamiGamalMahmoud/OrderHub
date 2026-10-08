using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

internal sealed class CheckForUpdateStep : IStartupStep
{
    private readonly IUpdateService _updateService;
    private readonly IApplicationContext _applicationContext;

    public int Order => (int)StartUpdStepsOrder.CheckForUpdateStep;

    public string DisplayName => "التحقق من وجود تحديثات";

    public bool IsEnabled => true;

    public CheckForUpdateStep(
        IUpdateService updateService,
        IApplicationContext applicationContext)
    {
        _updateService = updateService;
        _applicationContext = applicationContext;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            UpdateInfo updateInfo =
                await _updateService.CheckForUpdateAsync();

            if (updateInfo is null)
                return;

            _applicationContext.SetUpdate(updateInfo);

            if (!updateInfo.NeedToUpdate)
                return;

            NotificationService.Instance.Show(
                $"يتوفر تحديث جديد للبرنامج ({updateInfo.LatestVersion}).");
        }
        catch
        {
            // فشل التحقق من التحديث لا يمنع تشغيل البرنامج.
        }
    }
}