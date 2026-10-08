using System;
using System.Linq;
using OrderHub.Infrastructure.Features.Setup.Categories;

namespace OrderHub.Tests.Infrastructure;

public class CategoryHierarchyTests
{
    [Fact]
    public void Counts_and_paths_support_deep_hierarchies_without_recursive_call_stack()
    {
        const int depth = 10000;
        var hierarchy = new CategoryHierarchy(Enumerable.Range(1, depth)
            .Select(id => new CategoryHierarchyRow(id, $"Category {id}", id == 1 ? null : id - 1, id == depth ? 7 : 0)));
        var root = Assert.Single(hierarchy.Browse(null).Categories);
        Assert.Equal(1, root.DirectChildrenCount);
        Assert.Equal(7, root.TotalProductsCount);
        Assert.Equal(depth, hierarchy.Browse(depth).Path.Count);
    }

    [Fact]
    public void Corrupt_cycle_fails_clearly_instead_of_hanging_or_returning_wrong_counts()
    {
        var hierarchy = new CategoryHierarchy([new(1, "First", 2, 1), new(2, "Second", 1, 2)]);
        Assert.Throws<InvalidOperationException>(() => hierarchy.Browse(null));
        Assert.Throws<InvalidOperationException>(() => hierarchy.Options());
    }
}
