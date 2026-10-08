using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Web.Components.Pages.Features.Cities.Editor;

public sealed class CityEditorState
{
    public string Name { get; set; } = string.Empty;

    public static CityEditorState FromCity(CityUpdateInput city) => new()
    {
        Name = city.Name
    };

    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || name.Trim().Length > 100)
        {
            return "اسم المدينة يجب أن يتكون من 2 إلى 100 حرف.";
        }

        return null;
    }
}
