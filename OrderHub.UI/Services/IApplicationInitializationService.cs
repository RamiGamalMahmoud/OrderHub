using System.Threading.Tasks;

namespace OrderHub.UI.Services;
public interface IApplicationInitializationService
{
    Task<bool> EnterApplicationAsync();
    Task InitializeAsync();
}
