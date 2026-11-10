using MudBlazor;
using OrderHub.Application.Features.Trips.CancelTrip;
using OrderHub.Application.Features.Trips.CompleteTrip;
using OrderHub.Application.Features.Trips.FailTrip;
using OrderHub.Application.Features.Trips.RetryTrip;
using OrderHub.Application.Features.Trips.StartTrip;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Trips;

namespace OrderHub.Web.Services;

/// <summary>Opens the shared confirmation dialog and dispatches the chosen lifecycle command.</summary>
public sealed class TripActions(IRequestExecutor requestExecutor, IDialogService dialogService)
{
    public async Task<TripActionOutcome?> ShowAsync(
        int tripId,
        int version,
        TripDisplayState displayState,
        string action)
    {
        var parameters = new DialogParameters<TripActionDialog>
        {
            { dialog => dialog.Title, TripPresentation.ActionLabel(action) },
            { dialog => dialog.Explanation, TripPresentation.ActionExplanation(action, displayState) },
            { dialog => dialog.ConfirmLabel, TripPresentation.ActionLabel(action) },
            { dialog => dialog.Tone, TripPresentation.ActionTone(action) },
            { dialog => dialog.Icon, TripPresentation.ActionIcon(action) },
            { dialog => dialog.RequiresReason, TripPresentation.RequiresReason(action) },
            { dialog => dialog.ReasonLabel, TripPresentation.RequiresReason(action) ? "سبب العملية" : "ملاحظة" },
            {
                dialog => dialog.Submit,
                new Func<string?, Task<Result<int>>>(reason => ExecuteAsync(tripId, version, action, reason))
            }
        };

        var dialog = await dialogService.ShowAsync<TripActionDialog>(
            TripPresentation.ActionLabel(action),
            parameters,
            new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = false,
                BackdropClick = false
            });

        var result = await dialog.Result;

        return result is { Canceled: false, Data: TripActionOutcome outcome } ? outcome : null;
    }

    private Task<Result<int>> ExecuteAsync(int tripId, int version, string action, string? reason) =>
        action switch
        {
            TripPresentation.ActionStart => requestExecutor.ExecuteAsync(new StartTripCommand(tripId, version)),
            TripPresentation.ActionComplete => requestExecutor.ExecuteAsync(new CompleteTripCommand(tripId, version)),
            TripPresentation.ActionFail => requestExecutor.ExecuteAsync(new FailTripCommand(tripId, version, reason)),
            TripPresentation.ActionCancel => requestExecutor.ExecuteAsync(new CancelTripCommand(tripId, version, reason)),
            TripPresentation.ActionRetry => requestExecutor.ExecuteAsync(new RetryTripCommand(tripId, version)),
            _ => Task.FromResult(Result<int>.Failure("إجراء غير معروف."))
        };
}
