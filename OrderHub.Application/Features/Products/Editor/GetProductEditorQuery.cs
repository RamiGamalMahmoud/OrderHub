using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Products.Editor;

[RequirePermission(PermissionFeature.Products, PermissionLevel.Write)]
public sealed record GetProductEditorQuery(int? ProductId = null) : IRequest<ProductEditorData>;

public sealed record ProductEditorData(ProductDetails Product,
    IReadOnlyList<ProductEditorSupplier> Suppliers,
    IReadOnlyList<ProductEditorProperty> Properties,
    IReadOnlyList<CategoryPathSegment> SelectedCategoryPath);

public sealed record ProductEditorSupplier(int Id, string Name);
public sealed record ProductEditorProperty(int Id, string Name, PropertyType Type,
    string Description, IReadOnlyList<ProductEditorOption> Options);
public sealed record ProductEditorOption(int Id, string Value);
