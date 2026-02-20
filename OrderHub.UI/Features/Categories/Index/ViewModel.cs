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
                await LoadAsync();
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
            Categories = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryTreeQuery());
            CategoriesList = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(null));
        }

        protected override async Task ReloadAsync()
        {
            Categories = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryTreeQuery());
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
            if(dto.ParentId is null)
            {
                _selectedCategoriesList.Clear();
            }
            _selectedCategoriesList.Add(dto);

            CategoriesList = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(dto.Id));
            OnPropertyChanged(nameof(SelectedPath));
        }

        [RelayCommand]
        private async Task NavigateToHome()
        {
            await LoadAsync();
            _selectedCategoriesList.Clear();
        }

        [RelayCommand]
        private void Edit(CategoryListDto dto)
        {
            _selectionStore.Id = dto.Id;
            _dialogService.ShowDialog<Update.View>();
        }

        [RelayCommand]
        private async Task RemoveAsync(CategoryListDto dto)
        {
            await _mediator.Send(new Application.Commands.CategoryCommands.DeleteCategoryCommand(dto.Id));
        }

        [ObservableProperty]
        private IEnumerable<CategoryTreeDto> _categories;

        [ObservableProperty]
        private IEnumerable<CategoryListDto> _categoriesList;

        [ObservableProperty]
        private string _selectedPath = string.Empty;

        [ObservableProperty]
        private string _searchTirm;

        private ObservableCollection<CategoryListDto> _selectedCategoriesList = new ObservableCollection<CategoryListDto>();
        public IEnumerable<CategoryListDto> SelectedCategoriesList => _selectedCategoriesList;
    }
}
