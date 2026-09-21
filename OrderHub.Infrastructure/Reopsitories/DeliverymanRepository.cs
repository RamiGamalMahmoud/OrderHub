using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal class DeliverymanRepository(AppDbContext appDbContext) :
    RepositoryBase<Deliveryman>(appDbContext), IDeliverymanRepository
{
    public override void Add(Deliveryman model)
    {
        _appDbContext.Deliverymen.Add(model);
    }

    public async Task<Deliveryman> GetById(int deliverymanId)
    {
        return await _appDbContext
            .Deliverymen
            .SingleOrDefaultAsync(deliveryman => deliveryman.Id == deliverymanId);
    }
}
