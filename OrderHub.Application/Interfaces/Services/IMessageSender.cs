using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IMessageSender
{
    Task<bool> SendToPhoneAsync(string destination, MessageToSend message);
    Task<bool> SendToGroupAsync(string destination, MessageToSend message);
}
