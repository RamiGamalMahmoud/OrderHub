using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IWhatsappService
{
    Task<bool> Send(string contact, string message);
    Task<bool> StartWhatsApp(string url = "https://web.whatsapp.com/");
}
