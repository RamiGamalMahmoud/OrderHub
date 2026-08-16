using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Threading.Tasks;

namespace OrderHub.UI.Common
{
    public abstract partial class IndexViewModelBase<TModel> : ObservableObject where TModel : class
    {
        protected readonly IMediator _mediator;

        protected IndexViewModelBase(IMediator mediator)
        {
            _mediator = mediator;
        }

        [RelayCommand]
        protected abstract Task ShowCreateAsync();

        [RelayCommand]
        protected abstract Task LoadAsync();

        [RelayCommand]
        protected abstract Task ReloadAsync();

        [RelayCommand]
        protected abstract Task ShowEditAsync(TModel model);

        [RelayCommand]
        protected abstract Task DeleteAsync(TModel model);
    }
}
