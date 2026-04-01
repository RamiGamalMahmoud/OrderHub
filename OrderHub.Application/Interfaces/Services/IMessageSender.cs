using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IMessageSender
{
    Task<bool> SendToPhoneAsync(string phoneNumber, string message);
    Task<bool> SendToGroupAsync(string groupLink, string message);
}
