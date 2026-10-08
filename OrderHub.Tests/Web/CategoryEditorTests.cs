using System;
using System.Linq;
using System.Reflection;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Categories.Editor;
using OrderHub.Web.Infrastructure.Authorization;

namespace OrderHub.Tests.Web;

public class CategoryEditorTests
{
    [Theory]
    [InlineData(null, "/categories")]
    [InlineData(10, "/categories/10")]
    public void Return_is_explicit_and_save_returns_to_selected_parent(int? parent, string expected)
    {
        var create = CategoryEditorState.Create(parent);
        Assert.Equal(parent, create.ParentId);
        Assert.Equal(expected, create.ReturnUrl);
        var edit = CategoryEditorState.Edit(new(24, "Category", parent));
        Assert.Equal(expected, edit.ReturnUrl);
        edit.ParentId = 42;
        Assert.Equal(expected, edit.ReturnUrl);
        Assert.Equal("/categories/42", edit.SavedUrl);
        edit.ParentId = null;
        Assert.Equal("/categories", edit.SavedUrl);
    }

    [Fact]
    public void Parent_selector_excludes_self_and_all_descendants_but_preserves_other_paths()
    {
        CategoryInfo[] all = [new(1, "Root", "Root", true), new(2, "Child", "Root / Child", true, 1),
            new(3, "Deep", "Root / Child / Deep", false, 2), new(4, "Other", "Other", true),
            new(5, "Child", "Other / Child", false, 4)];
        Assert.Equal(new[] { 4, 5 }, CategoryEditorState.ParentOptions(all, 1).Select(category => category.Id));
        Assert.Equal(5, CategoryEditorState.ParentOptions(all, null).Count);
        Assert.Equal("Other / Child", CategoryEditorState.ParentOptions(all, 1).Last().FullPath);
    }

    [Fact]
    public void All_category_requests_and_web_routes_have_correct_typed_permissions()
    {
        var types = typeof(GetCategoriesQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == "OrderHub.Application.Features.Setup.Categories" &&
                (type.Name.EndsWith("Query") || type.Name.EndsWith("Command"))).ToList();
        Assert.Equal(6, types.Count);
        foreach (var type in types)
        {
            var metadata = type.GetCustomAttribute<RequirePermissionAttribute>();
            Assert.NotNull(metadata);
            Assert.Equal(PermissionFeature.Categories, metadata.Feature);
            Assert.Equal(type.Name.EndsWith("Query") ? PermissionLevel.Read : PermissionLevel.Write, metadata.RequiredLevel);
        }
        Assert.Equal(new PermissionAuthorizeAttribute(PermissionFeature.Categories, PermissionLevel.Read).Policy,
            typeof(OrderHub.Web.Components.Pages.Features.Categories.Index).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
        Assert.Equal(new PermissionAuthorizeAttribute(PermissionFeature.Categories, PermissionLevel.Write).Policy,
            typeof(Editor).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
    }
}
