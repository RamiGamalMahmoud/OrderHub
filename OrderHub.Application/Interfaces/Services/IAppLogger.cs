using System;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IAppLogger
{
    Task LogInfoAsync(string message);
    Task LogErrorAsync(string message, Exception exception = null);
}
