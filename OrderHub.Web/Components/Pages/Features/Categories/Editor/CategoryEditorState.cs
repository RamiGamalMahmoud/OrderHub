using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Web.Components.Pages.Features.Categories.Editor;

public sealed class CategoryEditorState
{
    public string Name { get; set; } = "";
    public int? ParentId { get; set; }
    public int? OriginalParentId { get; private set; }
    public string ReturnUrl => IndexUrl(OriginalParentId);
    public string SavedUrl => IndexUrl(ParentId);

    public static string IndexUrl(int? parentId) => parentId.HasValue ? $"/categories/{parentId}" : "/categories";

    public static CategoryEditorState Create(int? parentId) => new() { ParentId = parentId, OriginalParentId = parentId };
    public static CategoryEditorState Edit(CategoryEdit category) => new()
    {
        Name = category.Name, ParentId = category.ParentId, OriginalParentId = category.ParentId
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
