using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Domain.Common;

namespace OrderHub.Web.Components.Pages.Features.Categories.Editor;

public sealed class CategoryEditorState
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Color { get; set; } = HexColor.Default;
    public int? ParentId { get; set; }

    public static CategoryEditorState Create(int? parentId, string color = HexColor.Default) => new()
    {
        ParentId = parentId,
        Color = color
    };
    public static CategoryEditorState Edit(CategoryEdit category) => new()
    {
        Name = category.Name,
        Description = category.Description,
        Color = category.Color,
        ParentId = category.ParentId,
    };

    public static IReadOnlyList<CategoryInfo> ParentOptions(IEnumerable<CategoryInfo> categories, int? categoryId)
    {
        var all = categories.ToList();
        if (!categoryId.HasValue) return all;
        var children = all.ToLookup(category => category.ParentId);
        var excluded = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(categoryId.Value);
        while (queue.TryDequeue(out int id))
        {
            if (!excluded.Add(id)) continue;
            foreach (var child in children[id]) queue.Enqueue(child.Id);
        }
        return all.Where(category => !excluded.Contains(category.Id)).ToList();
    }
}
