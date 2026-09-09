using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

public class DatabaseInitializationStep : IStartupStep
{
    private readonly IDatabaseService _db;

    public DatabaseInitializationStep(IDatabaseService db)
    {
        _db = db;
    }

    public int Order => (int)StartUpdStepsOrder.DatabaseInitialization;
    public string DisplayName => "جاري فحص قاعدة البيانات";

    public bool IsEnabled
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    public async Task ExecuteAsync()
    {
        try
        {
            if (await _db.CanConnectAsync())
            {
                await _db.FixCategoriesAsync();

                if (await _db.HasPendingMigrationsAsync())
                {
                    await _db.MigrateAsync();
                    NotificationService.Instance.ShowSuccess("تم تحديث قاعدة البيانات");
                }
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
