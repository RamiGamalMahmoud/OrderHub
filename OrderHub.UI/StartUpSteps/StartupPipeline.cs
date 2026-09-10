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
        _steps = steps
            .OrderBy(s => s.Order)
            .Where(s => s.IsEnabled);

        _startupProgress = startupProgress;
    }

    public async Task RunAsync()
    {
        List<IStartupStep> orderedSteps = _steps.ToList();
        int totalSteps = orderedSteps.Count;

        _startupProgress.Report(0, "جاري تهيئة التطبيق...");

        if (totalSteps == 0)
        {
            _startupProgress.Report(100, "اكتملت التهيئة");
            return;
        }

        for (int index = 0; index < totalSteps; index++)
        {
            IStartupStep step = orderedSteps[index];

            double progress =
                (double)index / totalSteps * 100;

            _startupProgress.Report(
                progress,
                step.DisplayName);

            await step.ExecuteAsync();

            progress =
                (double)(index + 1) / totalSteps * 100;

            _startupProgress.Report(
                progress,
                step.DisplayName);
            //await Task.Delay(1000);
        }

        _startupProgress.Report(100, "اكتملت التهيئة");
    }
}
