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

    public bool IsRootCategory => ParentCategoryId == null;
    public bool IsLeafCategory => _subCategories.Count == 0;
    public int? ParentCategoryId { get; set; }

    public void UpdateName(string name)
    {
        Name = new EntityName(name);
    }

    public void MoveTo(Category parentCategory)
    {
        if (parentCategory != null && parentCategory == this)
            throw new InvalidOperationException("Category cannot be its own parent");

        if (parentCategory != null && IsAncestorOf(parentCategory))
            throw new InvalidOperationException("Cannot create circular reference in category hierarchy");

        // Remove from old parent
        if (ParentCategory != null && ParentCategory != parentCategory)
        {
            ParentCategory._subCategories.Remove(this);
        }

        // Set new parent
        ParentCategory = parentCategory;

        // Add to new parent
        if (parentCategory != null && !parentCategory._subCategories.Contains(this))
        {
            parentCategory._subCategories.Add(this);
        }
    }

    public void AddSubCategory(Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(subCategory);

        if (subCategory == this)
            throw new InvalidOperationException("Category cannot be its own subcategory");

        if (_subCategories.Any(c => c == subCategory))
            throw new InvalidOperationException("Subcategory already exists");

        if (subCategory.IsAncestorOf(this))
            throw new InvalidOperationException("Cannot create circular reference in category hierarchy");

        subCategory.MoveTo(this);
    }

    public void RemoveSubCategory(Category subCategory)
    {
        ArgumentNullException.ThrowIfNull(subCategory);

        Category existing = _subCategories.FirstOrDefault(c => c == subCategory);
        if (existing == null)
            throw new InvalidOperationException("Subcategory does not exist");

        existing.ParentCategory = null;
        _subCategories.Remove(existing);
    }

    public bool HasSubCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return _subCategories.Any(c => c == category);
    }

    public bool IsAncestorOf(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        Category current = category.ParentCategory;
        while (current != null)
        {
            if (current == this)
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
        GetDescendantsRecursive(this, descendants);
        return descendants;
    }

    private void GetDescendantsRecursive(Category category, List<Category> descendants)
    {
        foreach (Category subCategory in category._subCategories)
        {
            descendants.Add(subCategory);
            GetDescendantsRecursive(subCategory, descendants);
        }
    }

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