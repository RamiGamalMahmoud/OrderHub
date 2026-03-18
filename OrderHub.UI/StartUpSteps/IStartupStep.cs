using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public interface IStartupStep
{
    Task ExecuteAsync();
    int Order { get; }
}