using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace OrderHub.UI.StartUpSteps
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddStartUpSteps(this IServiceCollection services)
        {
            services.AddSingleton<IStartupPipeline, StartupPipeline>();

            Assembly assembly = Assembly.GetExecutingAssembly();

            var stepTypes = assembly
                .GetTypes()
                .Where(t => typeof(IStartupStep).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface).ToList();

            foreach( Type stepType in stepTypes)
            {
                services.AddTransient(typeof(IStartupStep), stepType);
            }

            return services;
        }
    }
}
