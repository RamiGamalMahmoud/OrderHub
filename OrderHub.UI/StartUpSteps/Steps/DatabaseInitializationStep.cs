using OrderHub.Application.Interfaces.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

public class DatabaseInitializationStep : IStartupStep
{
    private readonly IDatabaseService _db;
    private readonly INotifier _notifier;

    public DatabaseInitializationStep(IDatabaseService db, INotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public int Order => (int)StartUpdStepsOrder.DatabaseInitialization;
    public string DisplayName => "جاري فحص قاعدة البيانات";

    public bool IsEnabled => true;

    public async Task ExecuteAsync()
    {
        if (!await _db.CanConnectAsync())
        {
            await _notifier.Error("خطأ في الاتصال بقاعدة البيانات");
            throw new Exception("DB connection failed");
        }

        await _db.FixCategoriesAsync();

        if (await _db.HasPendingMigrationsAsync())
        {
            await _db.MigrateAsync();
            await _notifier.Success("تم تحديث قاعدة البيانات");
        }
    }
}
