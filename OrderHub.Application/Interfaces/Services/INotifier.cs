using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface INotifier
{
    Task Error(string message);
    Task Success(string message);
    Task Notify(string message);
}