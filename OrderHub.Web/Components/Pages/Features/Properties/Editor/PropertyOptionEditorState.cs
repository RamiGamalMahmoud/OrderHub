namespace OrderHub.Web.Components.Pages.Features.Properties.Editor;

public sealed class PropertyOptionEditorState
{
    public Guid Key { get; } = Guid.NewGuid();

    public int? Id { get; init; }

    public string Value { get; set; } = string.Empty;
}
