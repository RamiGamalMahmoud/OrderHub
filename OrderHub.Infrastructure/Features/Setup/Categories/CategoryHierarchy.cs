using System;
using System.Collections.Generic;
using System.Linq;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Infrastructure.Features.Setup.Categories;

// One projected database snapshot; iterative traversal avoids depth limits and per-card queries.
internal sealed class CategoryHierarchy
{
    private readonly Dictionary<int, CategoryHierarchyRow> _rows;
    private readonly ILookup<int?, CategoryHierarchyRow> _children;

    public CategoryHierarchy(IEnumerable<CategoryHierarchyRow> rows)
    {
        _rows = rows.ToDictionary(row => row.Id);
        _children = _rows.Values.ToLookup(row => row.ParentId);
    }

    public GetCategoriesResponse Browse(int? parentId)
    {
        if (parentId.HasValue && !_rows.ContainsKey(parentId.Value))
            return new(false, [], []);

        var totals = _rows.Values.ToDictionary(row => row.Id, row => row.DirectProductsCount);
        var remaining = _rows.Values.ToDictionary(row => row.Id, row => _children[row.Id].Count());
        var leaves = new Queue<int>(remaining.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        int processed = 0;
        while (leaves.TryDequeue(out int id))
        {
            processed++;
            if (_rows[id].ParentId is int parent && _rows.ContainsKey(parent))
            {
                totals[parent] += totals[id];
                if (--remaining[parent] == 0)
                    leaves.Enqueue(parent);
            }
        }
        if (processed != _rows.Count)
            throw new InvalidOperationException("Category hierarchy contains a cycle.");

        return new(true, _children[parentId].OrderBy(row => row.Name).ThenBy(row => row.Id)
            .Select(row => new CategoryIndexItem(row.Id, row.Name, _children[row.Id].Count(), totals[row.Id])).ToList(),
            parentId.HasValue ? Path(parentId.Value) : []);
    }

    public IReadOnlyList<CategoryPathSegment> Path(int id)
    {
        var path = new List<CategoryPathSegment>();
        var visited = new HashSet<int>();
        while (_rows.TryGetValue(id, out var row))
        {
            if (!visited.Add(id))
                throw new InvalidOperationException("Category hierarchy contains a cycle.");
            path.Add(new(row.Id, row.Name));
            if (row.ParentId is not int parent)
                break;
            id = parent;
        }
        path.Reverse();
        return path;
    }

    public IEnumerable<CategoryInfo> Options() => _rows.Values
        .Select(row => new CategoryInfo(row.Id, row.Name,
            string.Join(" / ", Path(row.Id).Select(segment => segment.Name)), _children[row.Id].Any(), row.ParentId))
        .OrderBy(row => row.FullPath).ThenBy(row => row.Id).ToList();
}
