using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IWppConnectScriptService
{
    Task<string> PrepareAsync();
}