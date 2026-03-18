using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IMessageSender
{
    Task<bool> SendAsync(string contact, string message);
}
