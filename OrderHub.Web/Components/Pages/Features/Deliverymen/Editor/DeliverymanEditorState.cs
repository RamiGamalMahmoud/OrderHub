using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Web.Components.Pages.Features.Deliverymen.Editor;

public sealed class DeliverymanEditorState
{
    public string Name { get; set; } = string.Empty;

    public int? CityId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public static DeliverymanEditorState FromRead(DeliverymanInput deliveryman) => new()
    {
        Name = deliveryman.Name,
        CityId = deliveryman.CityId,
        PhoneNumber = deliveryman.PhoneNumber ?? string.Empty
    };

    public CreateDeliverymanCommand CreateCommand() =>
        new(new(Name, CityId.GetValueOrDefault(), PhoneNumber));

    public UpdateDeliverymanCommand UpdateCommand(int deliverymanId) =>
        new(deliverymanId, new(Name, CityId.GetValueOrDefault(), PhoneNumber));

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return "اسم المندوب يجب أن يتكون من 2 إلى 100 حرف.";
        }

        return null;
    }

    public static string? ValidatePhone(string phoneNumber) =>
        phoneNumber?.Trim().Length > 15 ? "رقم هاتف المندوب يجب ألا يتجاوز 15 حرفًا." : null;

    public string? ValidateAdditional() => ValidatePhone(PhoneNumber);
}
