using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IWhatsappService
{
    Task Send(string contact, string message);
}
