using System;
using System.Linq;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Domain;

public class CategoryTests
{
    [Fact]
    public void Unsaved_categories_keep_distinct_identity_and_domain_relationships()
    {
        var root = new Category(" Root ");
        var child = new Category("Child", root);
        var second = new Category("Second", root);
        var deep = new Category("Deep", child);
        Assert.Equal("Root", root.Name.Value);
        Assert.Equal(2, root.SubCategories.Count);
        Assert.Equal(3, root.GetDescendants().Count());
        Assert.Throws<InvalidOperationException>(() => root.MoveTo(root));
        Assert.Throws<InvalidOperationException>(() => root.MoveTo(child));
        Assert.Throws<InvalidOperationException>(() => root.MoveTo(deep));
        var branch = new Category("Other branch");
        child.MoveTo(branch);
        Assert.Same(branch, child.ParentCategory);
        Assert.Same(second, Assert.Single(root.SubCategories));
        branch.RemoveSubCategory(child);
        Assert.Null(child.ParentCategory);
        Assert.Null(child.ParentCategoryId);
        Assert.Empty(branch.SubCategories);
        Assert.True(child.IsRootCategory);
    }
}
