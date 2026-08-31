using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

public class DatabaseInitializationStep : IStartupStep
{
    private readonly IDatabaseService _db;
    private readonly IDataMigrationService _migrationService;

    public DatabaseInitializationStep(IDatabaseService db, IDataMigrationService migrationService)
    {
        _db = db;
        _migrationService = migrationService;
    }

    public int Order => (int)StartUpdStepsOrder.DatabaseInitialization;
    public string DisplayName => "جاري فحص قاعدة البيانات";

    public bool IsEnabled => true;

    public async Task ExecuteAsync()
    {
        if (!await _db.CanConnectAsync())
        {
            NotificationService.Instance.ShowError("خطأ في الاتصال بقاعدة البيانات");
            throw new Exception("DB connection failed");
        }

        await _db.FixCategoriesAsync();

        if (await _db.HasPendingMigrationsAsync())
        {
            await _db.MigrateAsync();
            NotificationService.Instance.ShowSuccess("تم تحديث قاعدة البيانات");
        }

        await _migrationService.MigrateLegacyAttachmentsAsync();
    }
}
