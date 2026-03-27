using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.DeliverymanDtos;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.ShippingCarriersDtos;

namespace OrderHub.UI.Features.Orders.Editor;

public partial class DeliveryMethodsViewModel : ObservableValidator
{
    private readonly IMediator _mediator;
    private CancellationTokenSource _deliverymanSearchCts;
    private CancellationTokenSource _shippingCarrierSearchCts;

    public DeliveryMethodsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        DeliverySteps.CollectionChanged += DeliverySteps_CollectionChanged;
        ValidateAllProperties();
    }

    public static IEnumerable<EnumItem<DeliveryMethod>> DeliveryMethods =>
        Enum.GetValues<DeliveryMethod>()
            .Cast<DeliveryMethod>()
            .Select(e => new EnumItem<DeliveryMethod>(e, e.GetDescription()))
            .OrderBy(e => e.DisplayName);

    [ObservableProperty]
    [Required(ErrorMessage = "Delivery method is required")]
    [CustomValidation(typeof(DeliveryMethodsViewModel), nameof(ValidateDeliveryMethod))]
    [NotifyDataErrorInfo]
    private EnumItem<DeliveryMethod> _selecteddDeliveryMethod;

    partial void OnSelecteddDeliveryMethodChanged(EnumItem<DeliveryMethod> oldValue, EnumItem<DeliveryMethod> newValue)
    {
        SelectedDeliveryman = null;
        SelectedShippingCarrier = null;
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [ObservableProperty]
    private ShippingCarrierListDto _selectedShippingCarrier;

    partial void OnSelectedShippingCarrierChanged(ShippingCarrierListDto oldValue, ShippingCarrierListDto newValue)
    {
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [ObservableProperty]
    private DeliverymanListDto _selectedDeliveryman;

    partial void OnSelectedDeliverymanChanged(DeliverymanListDto oldValue, DeliverymanListDto newValue)
    {
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [ObservableProperty]
    private IEnumerable<DeliverymanListDto> _deliverymen = [];

    [ObservableProperty]
    private IEnumerable<ShippingCarrierListDto> _shippingCarriers = [];

    [ObservableProperty]
    private string _deliverymanSearchTerm;

    [ObservableProperty]
    private string _shippingCarrierSearchTerm;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveDeliveryStepDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDeliveryStepUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveDeliveryStepCommand))]
    private DeliveryStepViewModel _selectedDeliveryStep;

    public ObservableCollection<DeliveryStepViewModel> DeliverySteps { get; set; } = [];

    public void SetDeliverymen(IEnumerable<DeliverymanListDto> deliverymen)
    {
        Deliverymen = MergeSelectedItem(deliverymen, SelectedDeliveryman);
    }

    public void SetShippingCarriers(IEnumerable<ShippingCarrierListDto> shippingCarriers)
    {
        ShippingCarriers = MergeSelectedItem(shippingCarriers, SelectedShippingCarrier);
    }

    async partial void OnDeliverymanSearchTermChanged(string oldValue, string newValue)
    {
        _deliverymanSearchCts?.Cancel();
        _deliverymanSearchCts = new CancellationTokenSource();
        CancellationToken token = _deliverymanSearchCts.Token;

        try
        {
            await Task.Delay(300, token);
            IEnumerable<DeliverymanListDto> deliverymen = await _mediator.Send(
                new Application.Queries.DeliverymanQueries.GetDeliverymenByNameQuery(newValue),
                token);
            SetDeliverymen(deliverymen);
        }
        catch (OperationCanceledException)
        {
        }
    }

    async partial void OnShippingCarrierSearchTermChanged(string oldValue, string newValue)
    {
        _shippingCarrierSearchCts?.Cancel();
        _shippingCarrierSearchCts = new CancellationTokenSource();
        CancellationToken token = _shippingCarrierSearchCts.Token;

        try
        {
            await Task.Delay(300, token);
            IEnumerable<ShippingCarrierListDto> shippingCarriers = await _mediator.Send(
                new Application.Queries.ShippingCarriersQueries.GetShippingCarriersByNameQuery(newValue),
                token);
            SetShippingCarriers(shippingCarriers);
        }
        catch (OperationCanceledException)
        {
        }
    }

    partial void OnDeliverymenChanged(IEnumerable<DeliverymanListDto> oldValue, IEnumerable<DeliverymanListDto> newValue)
    {
        RefreshDeliveryStepHandlers(
            DeliveryMethod.DeliveryMan,
            (newValue ?? Enumerable.Empty<DeliverymanListDto>())
                .Select(deliveryman => new Handler(deliveryman.Id, deliveryman.Name)));
    }

    partial void OnShippingCarriersChanged(IEnumerable<ShippingCarrierListDto> oldValue, IEnumerable<ShippingCarrierListDto> newValue)
    {
        RefreshDeliveryStepHandlers(
            DeliveryMethod.ShippingCompany,
            (newValue ?? Enumerable.Empty<ShippingCarrierListDto>())
                .Select(carrier => new Handler(carrier.Id, carrier.Name)));
    }

    public static ValidationResult ValidateDeliveryMethod(EnumItem<DeliveryMethod> deliveryMethod, ValidationContext context)
    {
        if (context.ObjectInstance is not DeliveryMethodsViewModel viewModel || deliveryMethod is null)
            return ValidationResult.Success;

        return deliveryMethod.Value switch
        {
            DeliveryMethod.DeliveryMan when viewModel.SelectedDeliveryman is null
                => new ValidationResult("Deliveryman is required"),

            DeliveryMethod.ShippingCompany when viewModel.SelectedShippingCarrier is null
                => new ValidationResult("Shipping carrier is required"),

            DeliveryMethod.DeliveryChain when !viewModel.DeliverySteps.Any()
                => new ValidationResult("At least one delivery step is required"),

            DeliveryMethod.DeliveryChain when viewModel.DeliverySteps.Any(step => step.SelectedHandler is null)
                => new ValidationResult("Each delivery step must have a selected handler"),

            DeliveryMethod.Pickup => ValidationResult.Success,

            _ => ValidationResult.Success,
        };
    }

    [RelayCommand]
    private void AddDeliverymanStep()
    {
        DeliverySteps.Add(new DeliveryStepViewModel
        {
            Handlers = (Deliverymen ?? Enumerable.Empty<DeliverymanListDto>()).Select(d => new Handler(d.Id, d.Name)),
            Method = DeliveryMethod.DeliveryMan,
            Type = DeliveryMethod.DeliveryMan.GetDescription(),
            StepOrder = DeliverySteps.Count + 1
        });
    }

    [RelayCommand]
    private void AddShippingCarrierStep()
    {
        DeliverySteps.Add(new DeliveryStepViewModel
        {
            Handlers = (ShippingCarriers ?? Enumerable.Empty<ShippingCarrierListDto>()).Select(d => new Handler(d.Id, d.Name)),
            Method = DeliveryMethod.ShippingCompany,
            Type = DeliveryMethod.ShippingCompany.GetDescription(),
            StepOrder = DeliverySteps.Count + 1
        });
    }

    [RelayCommand(CanExecute = nameof(CanRemoveDeliveryStep))]
    private void RemoveDeliveryStep(DeliveryStepViewModel step)
    {
        DeliverySteps.Remove(step);
        ResetSelectedDeliveryStepsOrder();
        if (ReferenceEquals(SelectedDeliveryStep, step))
        {
            SelectedDeliveryStep = null;
        }

        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepDown))]
    private void MoveDeliveryStepDown(DeliveryStepViewModel step) => MoveStep(step, MoveDirection.Down);

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepUp))]
    private void MoveDeliveryStepUp(DeliveryStepViewModel step) => MoveStep(step, MoveDirection.Up);

    private bool CanRemoveDeliveryStep(DeliveryStepViewModel step) => step is not null;

    private bool CanMoveDeliveryStepDown(DeliveryStepViewModel step)
    {
        if (step is null) return false;

        int index = DeliverySteps.IndexOf(step);
        return index >= 0 && index < DeliverySteps.Count - 1;
    }

    private bool CanMoveDeliveryStepUp(DeliveryStepViewModel step)
    {
        if (step is null) return false;

        int index = DeliverySteps.IndexOf(step);
        return index > 0;
    }

    private void MoveStep(DeliveryStepViewModel step, MoveDirection direction)
    {
        int index = DeliverySteps.IndexOf(step);
        if (index == -1) return;

        int newIndex = index + (int)direction;

        if (newIndex < 0 || newIndex >= DeliverySteps.Count)
            return;

        DeliverySteps.Move(index, newIndex);
        ResetSelectedDeliveryStepsOrder();

        MoveDeliveryStepUpCommand.NotifyCanExecuteChanged();
        MoveDeliveryStepDownCommand.NotifyCanExecuteChanged();
    }

    public void ResetSelectedDeliveryStepsOrder()
    {
        for (int i = 0; i < DeliverySteps.Count; i++)
        {
            DeliverySteps[i].StepOrder = i + 1;
        }
    }

    public IEnumerable<OrderDeliveryStepCreateDto> BuildDeliverySteps()
    {
        return DeliverySteps
            .Where(step => step.SelectedHandler is not null)
            .OrderBy(step => step.StepOrder)
            .Select(step => new OrderDeliveryStepCreateDto(
                step.StepOrder,
                step.Method,
                step.SelectedHandler.Id));
    }

    private void RefreshDeliveryStepHandlers(DeliveryMethod method, IEnumerable<Handler> handlers)
    {
        Handler[] handlerOptions = handlers.ToArray();

        foreach (DeliveryStepViewModel step in DeliverySteps.Where(step => step.Method == method))
        {
            Handler selectedHandler = step.SelectedHandler;
            step.Handlers = handlerOptions;
            if (selectedHandler is not null)
            {
                step.SelectedHandler = step.Handlers.FirstOrDefault(handler => handler.Id == selectedHandler.Id);
            }
        }
    }

    private static IEnumerable<TItem> MergeSelectedItem<TItem>(IEnumerable<TItem> items, TItem selectedItem)
        where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null || results.Contains(selectedItem))
            return results;

        return new[] { selectedItem }.Concat(results);
    }

    private void DeliverySteps_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (DeliveryStepViewModel step in e.NewItems)
            {
                step.PropertyChanged += DeliveryStep_PropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (DeliveryStepViewModel step in e.OldItems)
            {
                step.PropertyChanged -= DeliveryStep_PropertyChanged;
            }
        }

        MoveDeliveryStepUpCommand.NotifyCanExecuteChanged();
        MoveDeliveryStepDownCommand.NotifyCanExecuteChanged();
        RemoveDeliveryStepCommand.NotifyCanExecuteChanged();
        ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
    }

    private void DeliveryStep_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeliveryStepViewModel.SelectedHandler))
        {
            ValidateProperty(SelecteddDeliveryMethod, nameof(SelecteddDeliveryMethod));
        }
    }
}

public record Handler(int Id, string Name);

public partial class DeliveryStepViewModel : ObservableObject
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DeliveryMethod Method { get; set; }

    [ObservableProperty]
    private int _stepOrder;

    [ObservableProperty]
    private IEnumerable<Handler> _handlers;

    [ObservableProperty]
    private Handler _selectedHandler;

    [ObservableProperty]
    private string _type;
}

enum MoveDirection
{
    Up = -1,
    Down = 1
}
