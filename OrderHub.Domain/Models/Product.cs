using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class Product : ModelBase
{
    private readonly List<Supplier> _suppliers = [];

    private Product() { }

    public Product(string name, string code, Category category, Money price)
    {
        Rename(name);
        ChangeProductCode(code);
        UpdateCategory(category);
        UpdatePrice(price);
    }

    public EntityName Name { get; private set; }

    public string Code { get; private set; }

    public Money Price { get; private set; }

    public Category Category { get; private set; }

    public IReadOnlyCollection<Supplier> Suppliers => _suppliers.AsReadOnly();

    public void Rename(string name)
    {
        Name = new EntityName(name);
    }

    public void ChangeProductCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim().ToUpperInvariant();
    }

    public void UpdateCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        Category = category;
    }

    public void UpdatePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        Price = price;
    }

    public void ClearSuppliers() => _suppliers.Clear();

    public void AddSupplier(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        if (_suppliers.Any(s => s == supplier))
            throw new InvalidOperationException("Supplier already associated with this product");

        _suppliers.Add(supplier);
    }

    public void RemoveSupplier(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var existing = _suppliers.FirstOrDefault(s => s == supplier);
        if (existing == null)
            throw new InvalidOperationException("Supplier is not associated with this product");

        _suppliers.Remove(existing);
    }

    public void RemoveSupplier(int supplierId)
    {
        var supplier = _suppliers.FirstOrDefault(s => s.Id == supplierId);
        if (supplier == null)
            throw new InvalidOperationException("Supplier is not associated with this product");

        _suppliers.Remove(supplier);
    }

    public bool HasSupplier(int supplierId)
    {
        return _suppliers.Any(s => s.Id == supplierId);
    }

    public bool IsAvailableFromSupplier(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        return _suppliers.Any(s => s == supplier);
    }
}