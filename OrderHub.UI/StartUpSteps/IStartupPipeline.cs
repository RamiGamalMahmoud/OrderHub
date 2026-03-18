using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public interface IStartupPipeline
{
    Task RunAsync();
}