using OrderHub.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Index
{
    public partial class ViewModel : IndexViewModelBase<CategoryTreeDto>
    {
        private List<CategoryListDto> _allCategories = [];

        public ViewModel(IMediator mediator) : base(mediator)
        {
            WeakReferenceMessenger.Default.Register<CategoriesMessages.CategoryCreatedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });

            WeakReferenceMessenger.Default.Register<CategoriesMessages.CategoryUpdatedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });

            WeakReferenceMessenger.Default.Register<CategoriesMessages.CategoryDeletedMessage>(this, async (m, r) =>
            {
                SelectedCategory = null;
                await LoadAsync();
            });
        }

        protected override async Task LoadAsync()
        {
            _allCategories = (await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(null))).ToList();
            _selectedCategoriesList.Clear();
            ApplyFilter();
        }

        protected override async Task ReloadAsync()
        {
            _allCategories = (await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(null))).ToList();
            ApplyFilter();
        }

        [RelayCommand]
        private async Task NavigateToCategory(CategoryListDto dto)
        {
            SelectedCategory = dto;
            if(SelectedCategory is null)
            {
                await LoadAsync();
                _selectedCategoriesList.Clear();
                return;
            }
            if(dto.ParentId is null)
            {
                _selectedCategoriesList.Clear();
            }
            _selectedCategoriesList.Add(dto);

            _allCategories = (await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(dto.Id))).ToList();
            ApplyFilter();
        }

        [RelayCommand]
        private async Task NavigateToHome() => await NavigateToCategory(null);

        [ObservableProperty]
        private IEnumerable<CategoryListDto> _categoriesList;

        [ObservableProperty]
        private string _searchTerm;

        partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

        private readonly ObservableCollection<CategoryListDto> _selectedCategoriesList = [];
        public IEnumerable<CategoryListDto> SelectedCategoriesList => _selectedCategoriesList;
        [ObservableProperty]
        private CategoryListDto _selectedCategory;

        private void ApplyFilter()
        {
            IEnumerable<CategoryListDto> filtered = _allCategories;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                string term = SearchTerm.Trim();
                filtered = filtered.Where(category => category.Name?.Contains(term) == true);
            }

            CategoriesList = filtered.ToList();
        }
    }
}
