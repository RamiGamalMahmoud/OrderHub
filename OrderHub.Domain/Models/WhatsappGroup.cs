using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public class WhatsappGroup : ModelBase
{
    public string GroupName { get; set; }
    public string GroupLink { get; set; }

    public WhatsappGroupType GroupType { get; set; }
}
