using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public class Category : ModelBase
{
    private readonly List<Category> _subCategories = [];
    private Category() { }

    public Category(string name, Category parentCategory = null)
    {
        UpdateName(name);
        MoveTo(parentCategory);
    }

    public EntityName Name { get; private set; }

    public Category ParentCategory { get; private set; }

    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();
    public ICollection<Product> Products {  get; private set; }

    public bool IsRootCategory => ParentCategoryId == null;
    public bool IsLeafCategory => _subCategories.Count == 0;
    public int? ParentCategoryId { get; private set; }

    public void UpdateName(string name)
    {
        Name = new EntityName(name?.Trim());
    }

    public void MoveTo(Category parentCategory)
    {
        if (parentCategory != null && SameCategory(parentCategory, this))
            throw new InvalidOperationException("Category cannot be its own parent");

        if (parentCategory != null && IsAncestorOf(parentCategory))
            throw new InvalidOperationException("Cannot create circular reference in category hierarchy");

        // Remove from old parent
        if (ParentCategory != null && !SameCategory(ParentCategory, parentCategory))
        {
            ParentCategory._subCategories.RemoveAll(category => SameCategory(category, this));
        }

        // Set new parent
        ParentCategory = parentCategory;
        ParentCategoryId = parentCategory?.Id;

        // Add to new parent
        if (parentCategory != null && !parentCategory._subCategories.Any(category => SameCategory(category, this)))
        {
            parentCategory._subCategories.Add(this);
        }
    }

    public void AddSubCategory(Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(subCategory);

        if (SameCategory(subCategory, this))
            throw new InvalidOperationException("Category cannot be its own subcategory");

        if (_subCategories.Any(c => SameCategory(c, subCategory)))
            throw new InvalidOperationException("Subcategory already exists");

        if (subCategory.IsAncestorOf(this))
            throw new InvalidOperationException("Cannot create circular reference in category hierarchy");

        subCategory.MoveTo(this);
    }

    public void RemoveSubCategory(Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(subCategory);

        Category existing = _subCategories.FirstOrDefault(c => SameCategory(c, subCategory));
        if (existing == null)
            throw new InvalidOperationException("Subcategory does not exist");

        existing.MoveTo(null);
    }

    public bool HasSubCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return _subCategories.Any(c => SameCategory(c, category));
    }

    public bool IsAncestorOf(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        Category current = category.ParentCategory;
        while (current != null)
        {
            if (SameCategory(current, this))
                return true;
            current = current.ParentCategory;
        }

        return false;
    }

    public bool IsDescendantOf(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return category.IsAncestorOf(this);
    }

    public int GetLevel()
    {
        int level = 0;
        Category current = ParentCategory;
        while (current != null)
        {
            level++;
            current = current.ParentCategory;
        }
        return level;
    }

    public Category GetRootCategory()
    {
        Category current = this;
        while (current.ParentCategory != null)
        {
            current = current.ParentCategory;
        }
        return current;
    }

    public IEnumerable<Category> GetAncestors()
    {
        List<Category> ancestors = [];
        Category current = ParentCategory;
        while (current != null)
        {
            ancestors.Add(current);
            current = current.ParentCategory;
        }
        return ancestors;
    }

    public IEnumerable<Category> GetDescendants()
    {
        List<Category> descendants = [];
        var pending = new Stack<Category>(_subCategories.AsEnumerable().Reverse());
        while (pending.TryPop(out Category category))
        {
            descendants.Add(category);
            foreach (var child in category._subCategories.AsEnumerable().Reverse())
                pending.Push(child);
        }
        return descendants;
    }

    private static bool SameCategory(Category left, Category right) =>
        ReferenceEquals(left, right) || (left is not null && right is not null && left.Id != 0 && left.Id == right.Id);

    public string FullPath
    {
        get
        {
            const string separator = " > ";
            List<string> path = [];
            Category current = this;
            while (current != null)
            {
                path.Insert(0, current.Name.Value);
                current = current.ParentCategory;
            }
            return string.Join(separator, path);
        }
    }
}
