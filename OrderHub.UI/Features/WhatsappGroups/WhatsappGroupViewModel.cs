using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Domain.Enums;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;

namespace OrderHub.UI.Features.WhatsappGroups;

public partial class WhatsappGroupViewModel : ObservableObject
{
    public WhatsappGroupViewModel(int id, string name, EnumItem<WhatsappGroupType> whatsappGroupType)
    {
        Id = id;
        Name = name;
        WhatsappGroupType = whatsappGroupType;
    }

    public int Id { get; init; }
    public string Name { get; init; }
    public EnumItem<WhatsappGroupType> WhatsappGroupType { get; init; }

    public static WhatsappGroupViewModel FromDto(WhatsappGroupListDto dto)
    {
        return new WhatsappGroupViewModel(
            dto.Id,
            dto.Name,
            new EnumItem<WhatsappGroupType>(dto.WhatsappGroupType, dto.WhatsappGroupType.GetDescription()));
    }
}
