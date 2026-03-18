using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public class StartupPipeline : IStartupPipeline
{
    private readonly IEnumerable<IStartupStep> _steps;

    public StartupPipeline(IEnumerable<IStartupStep> steps)
    {
        _steps = steps.OrderBy(s => s.Order);
    }

    public async Task RunAsync()
    {
        foreach (IStartupStep step in _steps)
        {
            await step.ExecuteAsync();
        }
    }
}