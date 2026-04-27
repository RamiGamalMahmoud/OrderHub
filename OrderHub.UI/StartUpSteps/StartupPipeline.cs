using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps;

public class StartupPipeline : IStartupPipeline
{
    private readonly IEnumerable<IStartupStep> _steps;
    private readonly StartupProgress _startupProgress;

    public StartupPipeline(IEnumerable<IStartupStep> steps, StartupProgress startupProgress)
    {
        _steps = steps.OrderBy(s => s.Order).Where(s => s.IsEnabled);
        _startupProgress = startupProgress;
    }

    public async Task RunAsync()
    {
        List<IStartupStep> orderedSteps = _steps.ToList();
        int totalSteps = orderedSteps.Count;

        _startupProgress.Report(0, "جاري تهيئة التطبيق...");

        for (int index = 0; index < totalSteps; index++)
        {
            IStartupStep step = orderedSteps[index];
            _startupProgress.Report((double)index / totalSteps * 100, step.DisplayName);
            await System.Windows.Application.Current.Dispatcher.Invoke(step.ExecuteAsync);
            //await step.ExecuteAsync();
            _startupProgress.Report((double)(index + 1) / totalSteps * 100, step.DisplayName);
            await Task.Delay(500);
        }

        _startupProgress.Report(100, "اكتملت التهيئة");
    }
}
