using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IDataMigrationService
{
    Task MigrateLegacyAttachmentsAsync();
}
