using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IWhatsappService
{
    void Close();
    Task<bool> SendAsync(string destination, MessageToSend message);
    Task<bool> StartWhatsAppAsync(string url = "https://web.whatsapp.com/");
}
