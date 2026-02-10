using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services
{
    public interface ISessionManager
    {
        Session CurrentSession { get; }
        Task StartNewSession();
    }
}
