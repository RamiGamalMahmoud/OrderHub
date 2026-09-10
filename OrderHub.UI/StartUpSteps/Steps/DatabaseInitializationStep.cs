using OrderHub.Application.Infrastructure.Installation;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

public class DatabaseInitializationStep : IStartupStep
{
    private readonly IDatabaseService _db;
    private readonly IApplicationConfigurationProvider _typeProvider;

    public DatabaseInitializationStep(IDatabaseService db, IApplicationConfigurationProvider typeProvider)
    {
        _db = db;
        _typeProvider = typeProvider;
    }

    public int Order => (int)StartUpdStepsOrder.DatabaseInitialization;
    public string DisplayName => "جاري فحص قاعدة البيانات";

    public bool IsEnabled => true;

    public async Task ExecuteAsync()
    {
        try
        {
            if (await _db.CanConnectAsync() && _typeProvider.GetConfiguration().InstallationType is InstallationType.Server)
            {
                if (await _db.HasPendingMigrationsAsync())
                {
                    await _db.MigrateAsync();
                    NotificationService.Instance.ShowSuccess("تم تحديث قاعدة البيانات");
                }

                await _db.FixCategoriesAsync();
            }
            else
            {
                NotificationService.Instance.ShowError("خطأ في الاتصال بقاعدة البيانات");
            }
        }
        catch (Exception)
        {
            NotificationService.Instance.ShowError("خطأ في الاتصال بقاعدة البيانات");
        }
    }
}
