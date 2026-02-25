using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
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
        private readonly IDialogService _dialogService;
        private readonly ISelectionStore<ICategoryMarker, int> _selectionStore;

        public ViewModel(IMediator mediator, IDialogService dialogService, ISelectionStore<ICategoryMarker, int> selectionStore, IMessenger messenger) : base(mediator, messenger)
        {
            _dialogService = dialogService;
            _selectionStore = selectionStore;

            _messenger.Register<Application.Messages.Categories.CategoryCreatedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });

            _messenger.Register<Application.Messages.Categories.CategoryUpdatedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });

            _messenger.Register<Application.Messages.Categories.CategoryDeletedMessage>(this, async (m, r) =>
            {
                await NavigateToCategory(SelectedCategory);
            });
        }

        protected override async Task DeleteAsync(CategoryTreeDto dto)
        {
            if (!_dialogService.Confirm($"هل تريد حذف قسم الـ ({dto.Name})؟"))
                return;
            Result result = await _mediator.Send(new Application.Commands.CategoryCommands.DeleteCategoryCommand(dto.Id));
            if(result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف القسم"));
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification("خطأ أثناء حذف القسم"));
            }
        }

        protected override async Task LoadAsync()
        {
            CategoriesList = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(null));
            _selectedCategoriesList.Clear();
        }

        protected override async Task ReloadAsync()
        {
            CategoriesList = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(null));
        }

        protected override Task ShowCreateAsync()
        {
            _dialogService.ShowDialog<Create.View>();
            return Task.CompletedTask;
        }

        protected override Task ShowEditAsync(CategoryTreeDto dto)
        {
            _selectionStore.Id = dto.Id;
            _dialogService.ShowDialog<Update.View>();
            return Task.CompletedTask;
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

            CategoriesList = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(dto.Id));
        }

        [RelayCommand]
        private async Task NavigateToHome() => await NavigateToCategory(null);

        [RelayCommand]
        private void Edit(CategoryListDto dto)
        {
            _selectionStore.Id = dto.Id;
            _dialogService.ShowDialog<Update.View>();
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

            if (!_dialogService.Confirm(stringBuilder.ToString()))
                return;
            Result result =await _mediator.Send(new Application.Commands.CategoryCommands.DeleteCategoryCommand(dto.Id));
            if(result.IsSuccess)
            {
                await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف القسم"));
                await LoadAsync();
            }
            else
            {
                await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
            }
        }

        [ObservableProperty]
        private IEnumerable<CategoryListDto> _categoriesList;

        [ObservableProperty]
        private string _searchTirm;

        private readonly ObservableCollection<CategoryListDto> _selectedCategoriesList = [];
        public IEnumerable<CategoryListDto> SelectedCategoriesList => _selectedCategoriesList;
        [ObservableProperty]
        private CategoryListDto _selectedCategory;
    }
}
