using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CommonDtos;

namespace OrderHub.UI.Common
{
    public partial class CategorySelection : ObservableObject
    {
        private readonly IMediator _mediator;

        public CategorySelection(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task LoadRootCategoriesAsync()
        {
            RootCategories = await _mediator.Send(new Application.Queries.CommonQueries.GetRootCategoriesQuery());
        }

        public event EventHandler<CategoryInfoDto> SelectedCategoryChanged;
        partial void OnSelectedCategoryChanged(CategoryInfoDto oldValue, CategoryInfoDto newValue)
        {
            SelectedCategoryChanged?.Invoke(this, newValue);
        }

        [ObservableProperty]
        private IEnumerable<CategoryInfoDto> _rootCategories;

        [ObservableProperty]
        private ObservableCollection<KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>> _subCategories = [];

        [ObservableProperty]
        private CategoryInfoDto _selectedCategory;

        async partial void OnSelectedCategoryChanging(CategoryInfoDto oldValue, CategoryInfoDto newValue)
        {
            if (newValue is null)
            {
                SubCategories.Clear();
                return;
            }

            if (newValue.ParentId is null)
            {
                SubCategories.Clear();
            }
            else
            {
                RemoveSubCategoriesAfterParent(newValue.ParentId.Value);
            }

            IEnumerable<CategoryInfoDto> subCategories = await _mediator.Send(
                new Application.Queries.CommonQueries.GetSubCategoriesQuery(newValue.Id));

            bool exists = SubCategories.Any(s => s.Key.Id == newValue.Id);

            if (subCategories.Any() && !exists)
            {
                SubCategories.Add(new KeyValuePair<CategoryInfoDto, IEnumerable<CategoryInfoDto>>(newValue, subCategories));
            }
        }

        private void RemoveSubCategoriesAfterParent(int parentId)
        {
            List<int> ids = SubCategories.Select(c => c.Key.Id).ToList();
            int parentIndex = ids.IndexOf(parentId);

            if (parentIndex + 1 >= ids.Count)
                return;

            for (int i = ids.Count - 1; i > parentIndex; i--)
            {
                SubCategories.RemoveAt(i);
            }
        }
    }
}
