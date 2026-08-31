using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.UI.Features.Categories.Index
{
    public partial class ViewModel : IndexViewModelBase<CategoryTreeDto>
    {
        private List<CategoryListDto> _allCategories = [];

        public ViewModel(IMediator mediator) : base(mediator)
        {
            WeakReferenceMessenger.Default.Register<Application.Messages.Categories.CategoryCreatedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });

            WeakReferenceMessenger.Default.Register<Application.Messages.Categories.CategoryUpdatedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });

            WeakReferenceMessenger.Default.Register<Application.Messages.Categories.CategoryDeletedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });
        }

        protected override async Task DeleteAsync(CategoryTreeDto dto)
        {
            if (!DialogService.Instance.Confirm($"هل تريد حذف قسم الـ ({dto.Name})؟"))
                return;
            Result result = await _mediator.Send(new Application.Commands.CategoryCommands.DeleteCategoryCommand(dto.Id));
            if(result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess("تم حذف القسم");
            }
            else
            {
                NotificationService.Instance.ShowError("خطأ أثناء الحذف !");
            }
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

        protected override async Task ShowCreateAsync()
        {
            await DialogService.Instance.ShowDialog<Create.View>("إضافة قسم جديد");
        }

        protected override async Task ShowEditAsync(CategoryTreeDto dto)
        {
            await DialogService.Instance.ShowDialog<Update.View>("تعديل بيانات قسم", dto.Id);
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

        [RelayCommand]
        private async Task Edit(CategoryListDto dto)
        {
            await DialogService.Instance.ShowDialog<Update.View>("تعديل بيانات قسم", dto.Id);
        }

        [RelayCommand]
        private async Task RemoveAsync(CategoryListDto dto)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if(dto.SubCategoriesCount > 0)
            {
                stringBuilder.AppendLine($"القسم يحتوي على {dto.SubCategoriesCount} قسم فرعي");
            }

            if(dto.ProductsCount > 0)
            {
                stringBuilder.AppendLine($"القسم يحتوي على {dto.ProductsCount} منتج");
            }
            stringBuilder.AppendLine("سوف يتم حذف القسم الذي تم اختياره؟");
            stringBuilder.AppendLine("و جميع الأقسام التابعة له و كذلك المنتجات المرتبطة به");
            stringBuilder.AppendLine("و كذلك عمليات الشراء");

            if (!DialogService.Instance.Confirm(stringBuilder.ToString()))
                return;
            Result result =await _mediator.Send(new Application.Commands.CategoryCommands.DeleteCategoryCommand(dto.Id));
            if(result.IsSuccess)
            {
                NotificationService.Instance.ShowError("تم حذف القسم");
                await LoadAsync();
            }
            else
            {
                NotificationService.Instance.ShowError(result.ErrorMessage);
            }
        }

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
