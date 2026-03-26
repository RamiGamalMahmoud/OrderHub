using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;

namespace OrderHub.Infrastructure.Repositories;

internal class WhatsapGroupsRepository(AppDbContextFactory appDbContextFactory) : IWhatsapGroupsRepository
{
    public async Task<IEnumerable<WhatsappGroupListDto>> GetWhatsappGroupLists()
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext.Set<WhatsappGroup>()
            .Select(w => new WhatsappGroupListDto(w.Id, w.GroupName, w.GroupType))
            .ToListAsync();
    }

    public async Task<IEnumerable<WhatsappGroupInfoDto>> GetWhatsappGroupInfos()
    {
        using AppDbContext appDbContext = appDbContextFactory.CreateDbContext();
        return await appDbContext.Set<WhatsappGroup>()
            .Select(w => new WhatsappGroupInfoDto(w.Id, w.GroupName, w.GroupType))
            .ToListAsync();
    }
}
