using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface IDeliverymanRepository
{
    void Add(Deliveryman deliveryman);
    Task<Deliveryman> GetById(int deliverymanId);
}
