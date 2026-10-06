using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Extensions;
using OrderHub.Application.Authorization;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Application.Features.Products.Delete;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Application.Features.Products.Index;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Products.Editor;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Services;
using FeatureIndex = OrderHub.Web.Components.Pages.Features.Products.Index;
using FeatureEditor = OrderHub.Web.Components.Pages.Features.Products.Editor.Editor;

namespace OrderHub.Tests.Web;

public class ProductsWebTests
{
    [Fact]
    public void Routes_are_full_pages_and_use_typed_permissions()
    {
        Assert.Equal("/products", Assert.Single(typeof(FeatureIndex).GetCustomAttributes<RouteAttribute>()).Template);
        Assert.Equal(new[] { "/products/create", "/products/{Id:int}/edit" }, typeof(FeatureEditor).GetCustomAttributes<RouteAttribute>().Select(route => route.Template).OrderBy(route => route, StringComparer.Ordinal));
        Assert.Equal(new PermissionAuthorizeAttribute(PermissionFeature.Products, PermissionLevel.Write).Policy,
            typeof(FeatureEditor).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
        Assert.Equal(new PermissionAuthorizeAttribute(PermissionFeature.Products, PermissionLevel.Read).Policy,
            typeof(FeatureIndex).GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Index_write_actions_and_navigation_are_permission_aware(bool write)
    {
        var executor = Executor(write);
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        string html = await fixture.Markup();
        Assert.Equal(write, html.Contains("إضافة منتج"));
        Assert.Equal(write, html.Contains("تعديل المنتج"));
        Assert.Equal(write, html.Contains("حذف المنتج"));
        await fixture.Invoke(index, "Create");
        Assert.EndsWith("/products/create", fixture.NavigationUri);
        await fixture.Invoke(index, "Edit", 7);
        Assert.EndsWith(write ? "/products/7/edit" : "/products/create", fixture.NavigationUri);
    }

    [Fact]
    public async Task Index_search_resets_server_paging_and_load_failures_allow_retry()
    {
        var executor = Executor();
        executor.SetupSequence(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ReturnsAsync(new PagedResult<ProductIndexItem>([], 0, 1, 10));
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        Assert.Contains("تعذر تحميل المنتجات", await fixture.Markup());
        await fixture.Invoke(index, "LoadAsync");
        Assert.Contains("لم تتم إضافة منتجات بعد", await fixture.Markup());
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductIndexItem>([], 0, 1, 10));
        await fixture.Invoke(index, "SearchChangedAsync", "CODE");
        executor.Verify(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Search == "CODE" && query.Page == 1), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("لا توجد منتجات تطابق البحث", await fixture.Markup());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(7)]
    public async Task Create_and_edit_save_one_coherent_command_and_success_navigates(int? id)
    {
        var executor = Executor();
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>(new() { [nameof(FeatureEditor.Id)] = id });
        var state = GetField<ProductEditorState>(editor, "_state");
        Assert.Equal(id.HasValue ? "Existing" : "", state.Name);
        Fill(state);
        await fixture.Refresh(editor);
        await fixture.Invoke(editor, "SaveAsync");
        Assert.EndsWith("/products", fixture.NavigationUri);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()), id.HasValue ? Times.Never() : Times.Once());
        executor.Verify(service => service.ExecuteAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), id.HasValue ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task Validation_failed_persistence_and_retry_keep_editor_open_until_success()
    {
        var executor = Executor();
        executor.SetupSequence(service => service.ExecuteAsync(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure("القسم لم يعد موجودًا."))
            .ReturnsAsync(Result<int>.Success(7));
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>();
        await fixture.Invoke(editor, "SaveAsync");
        executor.Verify(service => service.ExecuteAsync(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        Fill(GetField<ProductEditorState>(editor, "_state"));
        await fixture.Refresh(editor);
        await fixture.Invoke(editor, "SaveAsync");
        Assert.Contains("القسم لم يعد موجودًا", await fixture.Markup());
        Assert.EndsWith("/products/create", fixture.NavigationUri);
        await fixture.Invoke(editor, "SaveAsync");
        Assert.EndsWith("/products", fixture.NavigationUri);
    }

    [Fact]
    public async Task Browsing_parent_children_and_deep_path_does_not_select_until_explicit_action()
    {
        var executor = Executor();
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>();
        var state = GetField<ProductEditorState>(editor, "_state");
        Assert.Null(state.CategoryId);
        await fixture.Invoke(editor, "BrowseAsync", (int?)1);
        Assert.Null(state.CategoryId);
        Assert.Contains("اختيار هذا القسم", await fixture.Markup());
        await fixture.Invoke(editor, "SelectCurrentCategory");
        Assert.Equal(1, state.CategoryId);
        await fixture.Invoke(editor, "OpenCategoryPickerAsync");
        await fixture.Invoke(editor, "BrowseAsync", (int?)2);
        Assert.Equal(1, state.CategoryId);
        await fixture.Invoke(editor, "SelectCurrentCategory");
        Assert.Equal("Root / Child", state.CategoryPath);
        await fixture.Invoke(editor, "OpenCategoryPickerAsync");
        await fixture.Invoke(editor, "BrowseAsync", (int?)40);
        Assert.Equal(2, state.CategoryId);
        await fixture.Invoke(editor, "SelectCurrentCategory");
        Assert.Equal(40, state.CategoryId);
        Assert.Equal(40, state.CategoryPath.Split(" / ").Length);
        await fixture.Invoke(editor, "OpenCategoryPickerAsync");
        await fixture.Invoke(editor, "BrowseAsync", (int?)1);
        Assert.Equal(40, state.CategoryId);
        await fixture.Invoke(editor, "BrowseAsync", (int?)null);
        Assert.Equal(40, state.CategoryId);
        Assert.Contains("كل الأقسام", await fixture.Markup());
    }

    [Fact]
    public async Task Supplier_search_selection_and_typed_property_assignment_work_without_defaults()
    {
        var executor = Executor();
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>();
        var state = GetField<ProductEditorState>(editor, "_state");
        await fixture.SetField(editor, "_supplierTerm", "Supplier");
        await fixture.Invoke(editor, "SetSupplier", 10, true);
        Assert.Contains(10, state.SupplierIds);
        await fixture.Invoke(editor, "SetSupplier", 10, false);
        Assert.Empty(state.SupplierIds);
        foreach (var property in state.Properties)
        {
            await fixture.Invoke(editor, "AddProperty", property);
            property.Required = true;
            await fixture.Invoke(editor, "AddProperty", property);
        }
        await fixture.Refresh(editor);
        Assert.Equal(3, state.CreateCommand().Product.ProductProperties.Count());
        string html = await fixture.Markup();
        Assert.Contains("إدخال قيمة نصية", html);
        Assert.Contains("اختيار نعم أو لا", html);
        Assert.Contains("Red", html);
        Assert.All(state.CreateCommand().Product.ProductProperties, assignment => Assert.True(assignment.IsRequired));
        await fixture.Invoke(editor, "RemoveProperty", state.Properties[0]);
        Assert.Equal(2, state.CreateCommand().Product.ProductProperties.Count());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Missing_product_and_load_error_offer_recoverable_state(bool missing)
    {
        var executor = Executor();
        if (missing)
        {
            executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductEditorQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((ProductEditorData)null);
        }
        else
        {
            executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductEditorQuery>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException());
        }
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>(new() { [nameof(FeatureEditor.Id)] = 7 });
        Assert.Contains(missing ? "المنتج غير موجود" : "تعذر تحميل بيانات المنتج", await fixture.Markup());
        await fixture.Invoke(editor, "SaveAsync");
        executor.Verify(service => service.ExecuteAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        await fixture.Invoke(editor, "Cancel");
        Assert.EndsWith("/products", fixture.NavigationUri);
    }

    [Fact]
    public async Task Saving_guards_double_submit_and_cancel_and_handles_unexpected_failure()
    {
        var executor = Executor();
        var completion = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>())).Returns(completion.Task);
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>();
        Fill(GetField<ProductEditorState>(editor, "_state"));
        await fixture.Refresh(editor);
        var saving = fixture.Invoke(editor, "SaveAsync");
        await fixture.Invoke(editor, "SaveAsync");
        await fixture.Invoke(editor, "Cancel");
        Assert.EndsWith("/products/create", fixture.NavigationUri);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        completion.SetException(new TimeoutException());
        await saving;
        Assert.Contains("تعذر حفظ المنتج", await fixture.Markup());
        await fixture.Invoke(editor, "Cancel");
        Assert.EndsWith("/products", fixture.NavigationUri);
    }

    [Fact]
    public async Task Delete_confirmation_cancellation_block_and_success_refresh_are_distinct()
    {
        var executor = Executor();
        executor.SetupSequence(service => service.ExecuteAsync(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("مرتبط بطلبات"))
            .ReturnsAsync(Result.Success());
        var dialogs = new Mock<IDialogService>();
        dialogs.SetupSequence(service => service.ShowMessageBoxAsync(It.IsAny<string>(), It.IsAny<string>(), "حذف", null, "إلغاء", null))
            .ReturnsAsync((bool?)null).ReturnsAsync(true).ReturnsAsync(true);
        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<FeatureIndex>();
        var row = new ProductIndexItem(7, "Product", "ABC", 10, "Root", 0, 0);
        await fixture.Invoke(index, "DeleteAsync", row);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        await fixture.Invoke(index, "DeleteAsync", row);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        await fixture.Invoke(index, "DeleteAsync", row);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Failed_category_browse_preserves_selection_and_can_retry()
    {
        var executor = Executor();
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>(new() { [nameof(FeatureEditor.Id)] = 7 });
        var state = GetField<ProductEditorState>(editor, "_state");
        executor.SetupSequence(service => service.ExecuteAsync(It.Is<GetProductEditorCategoriesQuery>(query => query.ParentId == 2), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ReturnsAsync(new GetCategoriesResponse(true, [], [new(1, "Root"), new(2, "Child")]));
        await fixture.Invoke(editor, "OpenCategoryPickerAsync");
        await fixture.Invoke(editor, "BrowseAsync", (int?)2);
        Assert.Equal(1, state.CategoryId);
        Assert.Contains("تعذر تحميل الأقسام", await fixture.Markup());
        await fixture.Invoke(editor, "RetryBrowseAsync");
        Assert.Equal(1, state.CategoryId);
        await fixture.Invoke(editor, "SelectCurrentCategory");
        Assert.Equal(2, state.CategoryId);
    }

    [Fact]
    public async Task Native_pager_defaults_to_twenty_and_exposes_required_sizes_without_search_button()
    {
        var executor = PagingExecutor();
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        Assert.Equal(20, fixture.Table(index).RowsPerPage);
        Assert.Equal(new[] { 10, 20, 50 }, fixture.Pager.PageSizeOptions);
        Assert.DoesNotContain(">بحث<", await fixture.Markup());
        executor.Verify(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Page == 1 && query.PageSize == 20), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Typing_is_debounced_and_only_latest_text_automatically_reloads()
    {
        var executor = PagingExecutor();
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        Task first = fixture.TypeSearch(index, "A");
        Task second = fixture.TypeSearch(index, "AB");
        Task latest = fixture.TypeSearch(index, "ABC");
        await fixture.Markup();
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("ABC", fixture.Search.GetState(input => input.Value));
        await Task.WhenAll(first, second, latest);
        executor.Verify(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Search == "ABC" && query.Page == 1), It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Search_and_clear_reset_advanced_page_and_refresh_total_and_range()
    {
        var executor = PagingExecutor();
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        await fixture.Navigate(index, 5);
        await fixture.TypeSearch(index, "needle");
        Assert.Equal(0, fixture.Table(index).CurrentPage);
        Assert.Contains("عرض 1–3 من 3", await fixture.Markup());
        await fixture.TypeSearch(index, "");
        Assert.Equal(0, fixture.Table(index).CurrentPage);
        Assert.Contains("عرض 1–20 من 137", await fixture.Markup());
        executor.Verify(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Search == "" && query.Page == 1 && query.PageSize == 20), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public async Task Changing_native_page_size_resets_and_requests_that_size_from_server(int size)
    {
        var executor = PagingExecutor();
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        if (size == 20)
        {
            await fixture.SetPageSize(index, 10);
        }
        await fixture.Navigate(index, 5);
        await fixture.SetPageSize(index, size);
        Assert.Equal(0, fixture.Table(index).CurrentPage);
        Assert.Equal(size, fixture.Table(index).RowsPerPage);
        Assert.Contains($"عرض 1–{size} من 137", await fixture.Markup());
        executor.Verify(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Page == 1 && query.PageSize == size), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(0, 1, 20)]
    [InlineData(1, 21, 40)]
    [InlineData(6, 121, 137)]
    public async Task Pager_reports_first_middle_and_partial_last_ranges(int page, int first, int last)
    {
        await using var fixture = new HtmlFixture(PagingExecutor().Object);
        var index = await fixture.Render<FeatureIndex>();
        await fixture.Navigate(index, page);
        Assert.Contains($"عرض {first}–{last} من 137", await fixture.Markup());
    }

    [Fact]
    public async Task Empty_search_has_zero_range_and_keeps_page_size_selector()
    {
        await using var fixture = new HtmlFixture(PagingExecutor().Object);
        var index = await fixture.Render<FeatureIndex>();
        await fixture.TypeSearch(index, "missing");
        string html = await fixture.Markup();
        Assert.Contains("عرض 0–0 من 0", html);
        Assert.DoesNotContain("عرض 1–0", html);
        Assert.Contains("لا توجد منتجات تطابق البحث", html);
        Assert.Contains("عدد العناصر", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Out_of_order_results_or_errors_cannot_replace_new_search(bool failOld)
    {
        var executor = PagingExecutor();
        var old = new TaskCompletionSource<PagedResult<ProductIndexItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        executor.Setup(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Search == "old"), It.IsAny<CancellationToken>()))
            .Returns((GetProductIndexQuery _, CancellationToken token) => { started.TrySetResult(token); return old.Task; });
        executor.Setup(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Search == "new"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductIndexItem>([new(1, "New result", "NEW", 1, "Root", 0, 0)], 1, 1, 20));
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        Task oldSearch = fixture.TypeSearch(index, "old");
        CancellationToken oldToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await fixture.TypeSearch(index, "new");
        Assert.True(oldToken.IsCancellationRequested);
        if (failOld) { old.SetException(new TimeoutException()); }
        else { old.SetResult(new PagedResult<ProductIndexItem>([new(2, "Old result", "OLD", 1, "Root", 0, 0)], 1, 1, 20)); }
        await oldSearch;
        string html = await fixture.Markup();
        Assert.Contains("New result", html);
        Assert.DoesNotContain("Old result", html);
        Assert.DoesNotContain("تعذر تحميل المنتجات", html);
    }

    [Fact]
    public async Task Disposal_cancels_delayed_search_and_does_not_dispatch_after_page_exit()
    {
        var executor = PagingExecutor();
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        Task search = fixture.TypeSearch(index, "pending");
        await fixture.DisposePage(index);
        await search;
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Disposal_cancels_inflight_query_and_discards_its_late_response()
    {
        var executor = PagingExecutor();
        var pending = new TaskCompletionSource<PagedResult<ProductIndexItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        executor.Setup(service => service.ExecuteAsync(It.Is<GetProductIndexQuery>(query => query.Search == "pending"), It.IsAny<CancellationToken>()))
            .Returns((GetProductIndexQuery _, CancellationToken token) => { started.TrySetResult(token); return pending.Task; });
        await using var fixture = new HtmlFixture(executor.Object);
        var index = await fixture.Render<FeatureIndex>();
        Task search = fixture.TypeSearch(index, "pending");
        var token = await started.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await fixture.DisposePage(index);
        Assert.True(token.IsCancellationRequested);
        pending.SetResult(new PagedResult<ProductIndexItem>([new(1, "Late result", "LATE", 1, "Root", 0, 0)], 1, 1, 20));
        await search;
        Assert.DoesNotContain("Late result", await fixture.Markup());
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(0)]
    public async Task Deleting_last_row_recovers_valid_page_even_when_all_results_disappear(int remaining)
    {
        int total = 21;
        var executor = Executor();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProductIndexQuery query, CancellationToken _) => Page(query, total));
        executor.Setup(service => service.ExecuteAsync(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { total = remaining; return Result.Success(); });
        var dialogs = new Mock<IDialogService>();
        dialogs.Setup(service => service.ShowMessageBoxAsync(It.IsAny<string>(), It.IsAny<string>(), "حذف", null, "إلغاء", null)).ReturnsAsync(true);
        await using var fixture = new HtmlFixture(executor.Object, dialogs.Object);
        var index = await fixture.Render<FeatureIndex>();
        await fixture.Navigate(index, 1);
        await fixture.Invoke(index, "DeleteAsync", new ProductIndexItem(21, "Last", "LAST", 1, "Root", 0, 0));
        Assert.Equal(0, fixture.Table(index).CurrentPage);
        Assert.Contains(remaining == 0 ? "عرض 0–0 من 0" : "عرض 1–20 من 20", await fixture.Markup());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(40)]
    public async Task Explicit_root_parent_or_deep_selection_closes_and_reopening_preserves_unsaved_state(int category)
    {
        var executor = Executor();
        await using var fixture = new HtmlFixture(executor.Object);
        var editor = await fixture.Render<FeatureEditor>();
        var state = GetField<ProductEditorState>(editor, "_state");
        state.Name = "Unsaved name";
        await fixture.Invoke(editor, "BrowseAsync", (int?)category);
        Assert.Contains("كل الأقسام", await fixture.Markup());
        Assert.Null(state.CategoryId);
        await fixture.Invoke(editor, "SelectCurrentCategory");
        string path = state.CategoryPath;
        Assert.Equal(category, state.CategoryId);
        Assert.DoesNotContain("اختيار هذا القسم", await fixture.Markup());
        Assert.DoesNotContain("كل الأقسام", await fixture.Markup());
        Assert.Contains(path, await fixture.Markup());
        await fixture.Invoke(editor, "OpenCategoryPickerAsync");
        Assert.Equal(category, state.CategoryId);
        Assert.Equal(path, state.CategoryPath);
        Assert.Equal("Unsaved name", state.Name);
        Assert.Contains("اختيار هذا القسم", await fixture.Markup());
        await fixture.Invoke(editor, "BrowseAsync", (int?)1);
        Assert.Equal(category, state.CategoryId);
        Assert.Contains("اختيار هذا القسم", await fixture.Markup());
        await fixture.Invoke(editor, "BrowseAsync", (int?)null);
        Assert.Equal(category, state.CategoryId);
        Assert.Contains("كل الأقسام", await fixture.Markup());
        executor.Verify(service => service.ExecuteAsync(It.IsAny<GetProductEditorQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PagedResult<ProductIndexItem> Page(GetProductIndexQuery query, int total)
    {
        int first = (query.Page - 1) * query.PageSize;
        var rows = Enumerable.Range(first + 1, Math.Max(0, Math.Min(query.PageSize, total - first)))
            .Select(id => new ProductIndexItem(id, "Product " + id, "P" + id, 1, "Root", 0, 0)).ToArray();
        return new PagedResult<ProductIndexItem>(rows, total, query.Page, query.PageSize);
    }

    private static Mock<IRequestExecutor> PagingExecutor()
    {
        var executor = Executor();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProductIndexQuery query, CancellationToken _) => Page(query, query.Search == "missing" ? 0 : string.IsNullOrWhiteSpace(query.Search) ? 137 : 3));
        return executor;
    }

    private static void Fill(ProductEditorState state)
    {
        state.Name = "Product";
        state.Code = "ABC";
        state.Price = 10;
        state.CategoryId = 1;
        state.CategoryPath = "Root";
    }
    private static Mock<IRequestExecutor> Executor(bool write = true)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(write);
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductIndexQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductIndexItem>([new(7, "Product", "ABC", 10, "Root", 0, 0)], 1, 1, 10));
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductEditorQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProductEditorQuery query, CancellationToken _) => new ProductEditorData(
                query.ProductId.HasValue ? new ProductDetails("Existing", "ABC", 10, 1, [], []) : null,
                [new(10, "Supplier")],
                [new(20, "Text", PropertyType.Text, "", []), new(21, "Boolean", PropertyType.Boolean, "", []), new(22, "List", PropertyType.List, "", [new(30, "Red")])],
                query.ProductId.HasValue ? [new(1, "Root")] : []));
        executor.Setup(service => service.ExecuteAsync(It.IsAny<GetProductEditorCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProductEditorCategoriesQuery query, CancellationToken _) => query.ParentId switch
            {
                null => new(true, [new(1, "Root", 1, 0)], []),
                1 => new(true, [new(2, "Child", 1, 0)], [new(1, "Root")]),
                2 => new(true, [], [new(1, "Root"), new(2, "Child")]),
                _ => new(true, [], Enumerable.Range(1, 40).Select(id => new CategoryPathSegment(id, "Level" + id)).ToArray())
            });
        executor.Setup(service => service.ExecuteAsync(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(7));
        executor.Setup(service => service.ExecuteAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Success(7));
        return executor;
    }
    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private sealed class HtmlFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        private readonly HtmlRenderer _renderer;
        private readonly List<IComponent> _components = [];
        private Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent _root;

        public Mock<IMudDialogInstance> DialogInstance { get; } = new();

        public string NavigationUri =>
            _services.GetRequiredService<NavigationManager>().Uri;

        public HtmlFixture(IRequestExecutor executor, IDialogService dialogs = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IComponentActivator>(new CaptureActivator(_components));
            services.AddMudServices();
            services.AddSingleton<IJSRuntime>(new Mock<IJSRuntime>().Object);
            services.AddSingleton<NavigationManager>(new TestNavigation());
            services.AddSingleton(executor);
            services.AddSingleton<PermissionAccess>();

            if (dialogs is not null)
            {
                services.AddSingleton(dialogs);
            }

            _services = services.BuildServiceProvider();
            _renderer = new HtmlRenderer(_services, _services.GetRequiredService<ILoggerFactory>());
        }

        public async Task<T> Render<T>(Dictionary<string, object> parameters = null) where T : IComponent
        {
            T captured = default;
            RenderFragment fragment = builder =>
            {
                builder.OpenComponent<T>(0);
                builder.AddMultipleAttributes(1, parameters ?? []);
                builder.AddComponentReferenceCapture(2, component => captured = (T)component);
                builder.CloseComponent();
            };

            await _renderer.Dispatcher.InvokeAsync(async () =>
            {
                _root = await _renderer.RenderComponentAsync<CascadingValue<IMudDialogInstance>>(
                    ParameterView.FromDictionary(new Dictionary<string, object>
                    {
                        [nameof(CascadingValue<IMudDialogInstance>.Value)] = DialogInstance.Object,
                        [nameof(CascadingValue<IMudDialogInstance>.IsFixed)] = true,
                        [nameof(CascadingValue<IMudDialogInstance>.ChildContent)] = fragment
                    }));
            });

            if (captured is FeatureIndex index)
            {
                await _renderer.Dispatcher.InvokeAsync(async () =>
                {
                    var table = GetField<MudTable<ProductIndexItem>>(index, "_table");
                    // Static HTML rendering skips both post-render lifecycles; activate normal server/pager behavior.
                    typeof(MudTable<ProductIndexItem>)
                        .GetMethod("OnAfterRender", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(table, [true]);
                    await (Task)typeof(MudTable<ProductIndexItem>)
                        .GetMethod("OnAfterRenderAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(table, [true]);
                    await Refresh(index);
                });
            }

            return captured;
        }

        public MudTable<ProductIndexItem> Table(FeatureIndex index) => GetField<MudTable<ProductIndexItem>>(index, "_table");
        public MudTablePager Pager => _components.OfType<MudTablePager>().Single();
        public MudTextField<string> Search => _components.OfType<MudTextField<string>>().Single();

        public Task Navigate(FeatureIndex index, int page) => _renderer.Dispatcher.InvokeAsync(async () =>
        {
            Table(index).NavigateTo(page);
            await Refresh(index);
        });

        public Task SetPageSize(FeatureIndex index, int size) => _renderer.Dispatcher.InvokeAsync(async () =>
        {
            Table(index).SetRowsPerPage(size);
            await Refresh(index);
        });

        public Task TypeSearch(FeatureIndex index, string text) => _renderer.Dispatcher.InvokeAsync(async () =>
        {
            Task pending = Search.ValueChanged.InvokeAsync(text);
            await Refresh(index);
            await pending;
            await Refresh(index);
        });

        public Task DisposePage(FeatureIndex index) => _renderer.Dispatcher.InvokeAsync(index.Dispose);

        public Task<string> Markup() => _renderer.Dispatcher.InvokeAsync(() =>
            WebUtility.HtmlDecode(_root.ToHtmlString()));

        public Task Invoke(object component, string method, params object[] arguments) =>
            _renderer.Dispatcher.InvokeAsync(async () =>
            {
                var result = component.GetType()
                    .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(component, arguments);

                if (result is Task task)
                {
                    await task;
                }

                await Refresh(component);
            });

        public Task SetField(object component, string field, object value) =>
            _renderer.Dispatcher.InvokeAsync(async () =>
            {
                component.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(component, value);

                await Refresh(component);
            });

        public Task Refresh(object component) => _renderer.Dispatcher.InvokeAsync(async () =>
        {
            var render = typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            render.Invoke(component, null);
            if (component is FeatureIndex index)
            {
                typeof(MudTable<ProductIndexItem>)
                    .GetMethod("OnAfterRender", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Table(index), [false]);
                await (Task)typeof(MudTable<ProductIndexItem>)
                    .GetMethod("OnAfterRenderAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Table(index), [false]);
                render.Invoke(component, null);
            }
        });

        public async ValueTask DisposeAsync()
        {
            await _renderer.DisposeAsync();
            await _services.DisposeAsync();
        }
    }

    private sealed class CaptureActivator(List<IComponent> components) : IComponentActivator
    {
        public IComponent CreateInstance(Type componentType)
        {
            var component = (IComponent)Activator.CreateInstance(componentType);
            components.Add(component);
            return component;
        }
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation()
        {
            Initialize("http://localhost/", "http://localhost/products/create");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
        }
    }
}
