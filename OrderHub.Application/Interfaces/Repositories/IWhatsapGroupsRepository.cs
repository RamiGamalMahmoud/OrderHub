using OrderHub.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;

public interface IWhatsapGroupsRepository
{
    Task<IEnumerable<WhatsappGroupDtos.WhatsappGroupInfoDto>> GetWhatsappGroupInfos();
    Task<IEnumerable<WhatsappGroupDtos.WhatsappGroupListDto>> GetWhatsappGroupLists();
}
