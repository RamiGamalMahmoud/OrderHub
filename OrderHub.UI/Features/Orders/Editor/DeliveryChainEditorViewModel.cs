using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Common.Extensions;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class DeliveryChainEditorViewModel : ObservableObject
{
    public DeliveryChainEditorViewModel()
    {
        DeliverySteps.CollectionChanged += DeliverySteps_CollectionChanged;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveDeliveryStepDownCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDeliveryStepUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveDeliveryStepCommand))]
    private DeliveryStepViewModel _selectedDeliveryStep;

    public ObservableCollection<DeliveryStepViewModel> DeliverySteps { get; } = [];

    public event PropertyChangedEventHandler StepSelectionChanged;

    public void RefreshDeliverymanHandlers(IEnumerable<Deliveryman> deliverymen)
    {
        RefreshDeliveryStepHandlers(
            DeliveryMethod.DeliveryMan,
            (deliverymen ?? Enumerable.Empty<Deliveryman>())
                .Select(x => new Handler(x.Id, x.Name)));
    }

    public void RefreshShippingCarrierHandlers(IEnumerable<ShippingCarrier> shippingCarriers)
    {
        RefreshDeliveryStepHandlers(
            DeliveryMethod.ShippingCompany,
            (shippingCarriers ?? Enumerable.Empty<ShippingCarrier>())
                .Select(x => new Handler(x.Id, x.Name)));
    }

    public void AddDeliverymanStep(IEnumerable<Handler> handlers)
    {
        DeliveryStepViewModel step = new()
        {
            Handlers = handlers?.ToArray() ?? [],
            Method = DeliveryMethod.DeliveryMan,
            Type = DeliveryMethod.DeliveryMan.GetDescription(),
            StepOrder = DeliverySteps.Count + 1
        };

        DeliverySteps.Add(step);
    }

    public void AddShippingCarrierStep(IEnumerable<Handler> handlers)
    {
        DeliveryStepViewModel step = new()
        {
            Handlers = handlers?.ToArray() ?? [],
            Method = DeliveryMethod.ShippingCompany,
            Type = DeliveryMethod.ShippingCompany.GetDescription(),
            StepOrder = DeliverySteps.Count + 1
        };

        DeliverySteps.Add(step);
    }

    public void ResetSelectedDeliveryStepsOrder()
    {
        for (int i = 0; i < DeliverySteps.Count; i++)
        {
            DeliverySteps[i].StepOrder = i + 1;
        }
    }

    public IEnumerable<OrderDeliveryStep> BuildDeliverySteps()
    {
        return DeliverySteps
            .Where(step => step.SelectedHandler is not null)
            .OrderBy(step => step.StepOrder)
            .Select(step => new OrderDeliveryStep(
                step.StepOrder,
                step.Method,
                step.SelectedHandler.Id));
    }

    [RelayCommand]
    private void AddDeliverymanStep()
    {
        DeliverySteps.Add(new DeliveryStepViewModel
        {
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
    }

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepDown))]
    private void MoveDeliveryStepDown(DeliveryStepViewModel step)
        => MoveStep(step, MoveDirection.Down);

    [RelayCommand(CanExecute = nameof(CanMoveDeliveryStepUp))]
    private void MoveDeliveryStepUp(DeliveryStepViewModel step)
        => MoveStep(step, MoveDirection.Up);

    private bool CanRemoveDeliveryStep(DeliveryStepViewModel step)
        => step is not null;

    private bool CanMoveDeliveryStepDown(DeliveryStepViewModel step)
    {
        if (step is null)
            return false;

        int index = DeliverySteps.IndexOf(step);

        return index >= 0 && index < DeliverySteps.Count - 1;
    }

    private bool CanMoveDeliveryStepUp(DeliveryStepViewModel step)
    {
        if (step is null)
            return false;

        int index = DeliverySteps.IndexOf(step);

        return index > 0;
    }

    private void MoveStep(DeliveryStepViewModel step, MoveDirection direction)
    {
        int index = DeliverySteps.IndexOf(step);

        if (index == -1)
            return;

        int newIndex = index + (int)direction;

        if (newIndex < 0 || newIndex >= DeliverySteps.Count)
            return;

        DeliverySteps.Move(index, newIndex);

        ResetSelectedDeliveryStepsOrder();

        MoveDeliveryStepUpCommand.NotifyCanExecuteChanged();
        MoveDeliveryStepDownCommand.NotifyCanExecuteChanged();
    }

    private void RefreshDeliveryStepHandlers(DeliveryMethod method, IEnumerable<Handler> handlers)
    {
        Handler[] handlerOptions = handlers.ToArray();

        foreach (DeliveryStepViewModel step in DeliverySteps.Where(x => x.Method == method))
        {
            Handler selectedHandler = step.SelectedHandler;

            step.Handlers = handlerOptions;

            if (selectedHandler is not null)
            {
                step.SelectedHandler =
                    step.Handlers.FirstOrDefault(x => x.Id == selectedHandler.Id);
            }
        }
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

        StepSelectionChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(nameof(DeliverySteps)));
    }

    private void DeliveryStep_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeliveryStepViewModel.SelectedHandler))
        {
            StepSelectionChanged?.Invoke(this, e);
        }
    }
}

public record OrderDeliveryStep(
    int StepOrder,
    DeliveryMethod DeliveryMethod,
    int HandlerId);