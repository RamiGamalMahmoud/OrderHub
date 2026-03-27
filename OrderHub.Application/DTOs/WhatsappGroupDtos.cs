using OrderHub.Domain.Enums;

namespace OrderHub.Application.DTOs;

public static class WhatsappGroupDtos
{
    public record WhatsappGroupListDto(int Id, string Name, WhatsappGroupType WhatsappGroupType);
    public record WhatsappGroupInfoDto(int Id, string Name, WhatsappGroupType WhatsappGroupType);
    public record WhatsappGroupEditDto(int Id, string Name, WhatsappGroupType WhatsappGroupType, string GroupLink);
}
