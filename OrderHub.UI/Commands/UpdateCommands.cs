using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.UI.Services;
using OrderHub.UI.States;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class UpdateCommands : ObservableObject, ICommandGroup
{
    private readonly IApplicationContext _applicationContext;
    private readonly IUpdateService _updateService;
    private readonly UpdateState _state;

    private CancellationTokenSource _updatingCTS;

    public UpdateCommands(
        IApplicationContext applicationContext,
        IUpdateService updateService,
        UpdateState state)
    {
        _applicationContext = applicationContext;
        _updateService = updateService;
        _state = state;

        _state.PropertyChanged += OnStatePropertyChanged;
    }

    public bool CanUpdate =>
        _applicationContext.Update is not null &&
        _applicationContext.Update.NeedToUpdate;

    private bool CanCancelUpdate =>
        _state.IsUpdating &&
        _updatingCTS is not null;

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private async Task UpdateAsync()
    {
        bool update = await DialogService.Instance.ConfirmAsync(
            """
            يتوفر إصدار جديد من البرنامج. هل تريد تنزيل التحديث وتثبيته الآن؟
            """);

        if (!update)
            return;

        _updatingCTS = new CancellationTokenSource();

        _state.IsUpdating = true;
        _state.Progress = 0;

        var progress = new Progress<double>(value =>
        {
            _state.Progress = value;
        });

        try
        {
            string installPath =
                await _updateService.DownloadUpdateAsync(
                    _applicationContext.Update!,
                    progress,
                    _updatingCTS.Token);

            _updateService.RunInstaller(installPath);
        }
        catch (OperationCanceledException)
        {
            _state.Progress = 0;
        }
        catch (Exception)
        {
            // Log actual download error
        }
        finally
        {
            _state.IsUpdating = false;

            _updatingCTS.Dispose();
            _updatingCTS = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelUpdate))]
    private async Task CancelUpdateAsync()
    {
        bool cancelUpdate = await DialogService.Instance.ConfirmAsync(
            """
            هل أنت متأكد من إلغاء التحديث؟ سيتم إيقاف عملية التنزيل الحالية ولن يتم تثبيت الإصدار الجديد.
            """);

        if (!cancelUpdate)
            return;

        await _updatingCTS!.CancelAsync();
    }

    private void OnStatePropertyChanged(
        object sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UpdateState.IsUpdating))
        {
            CancelUpdateCommand.NotifyCanExecuteChanged();
        }
    }
}