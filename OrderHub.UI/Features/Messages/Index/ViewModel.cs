using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Queries;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Messages.Index;

public partial class ViewModel : ObservableObject
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly IServerConfiguration _serverConfiguration;
    private bool _useFilter;

    private CancellationTokenSource _filterCts;

    public ViewModel(
        IRequestExecutor requestExecutor,
        IServerConfiguration serverConfiguration)
    {
        _requestExecutor = requestExecutor;
        _serverConfiguration = serverConfiguration;

        Filter.Initialize();

        Filter.FilterChanged += OnFilterChanged;
        Pagination.PageChanged += OnPageChanged;

        RegisterMessages();
    }

    // =========================
    // Child ViewModels
    // =========================

    public MessagesFilter Filter { get; } = new();

    public PaginationViewModel Pagination { get; } = new();

    // =========================
    // Messages
    // =========================

    [ObservableProperty]
    private ObservableCollection<OutboxMessageViewModel> _outboxMessages = [];

    // =========================
    // Selection
    // =========================

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedMessage))]
    [NotifyCanExecuteChangedFor(nameof(ResendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(MarkMessageSentCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateMessageQrCodeCommand))]
    private OutboxMessageViewModel _selectedMessage;

    public bool HasSelectedMessage =>
        SelectedMessage is not null;

    // =========================
    // Loading
    // =========================

    [RelayCommand]
    public async Task LoadAsync()
    {
        CancelFilterRequest();

        _useFilter = false;

        Pagination.Reset();

        await LoadMessagesAsync();
    }

    private async Task LoadMessagesAsync(
    CancellationToken cancellationToken = default)
    {
        try
        {
            var result =
                await _requestExecutor.ExecuteAsync(
                    new OutboxMessageQueries.GetOutboxMessagesQuery(
                        Pagination.CurrentPage,
                        Pagination.PageSize,
                        _useFilter ? Filter.SearchTerm : null,
                        _useFilter ? Filter.Status : null,
                        _useFilter ? Filter.FromDate : null,
                        _useFilter ? Filter.ToDate : null),
                    cancellationToken);

            Pagination.Update(
                result.PageNumber,
                result.TotalPages,
                result.TotalCount);

            OutboxMessages =
                new ObservableCollection<OutboxMessageViewModel>(
                    result.Items.Select(MapMessage));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            NotificationService.Instance.ShowError(
                ex.Message);
        }
    }

    private Task ReloadAsync()
    {
        return LoadMessagesAsync();
    }

    // =========================
    // Filter
    // =========================

    private async void OnFilterChanged(
        object sender,
        EventArgs e)
    {
        CancelFilterRequest();

        var cts = new CancellationTokenSource();
        _filterCts = cts;

        try
        {
            await Task.Delay(300, cts.Token);

            _useFilter = true;

            Pagination.Reset();

            await LoadMessagesAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    // =========================
    // Pagination
    // =========================

    private async void OnPageChanged(
        object sender,
        EventArgs e)
    {
        CancelFilterRequest();

        await LoadMessagesAsync();
    }

    // =========================
    // Mapping
    // =========================

    private OutboxMessageViewModel MapMessage(
        OutboxMessageQueries.OutboxMessageListItem message)
    {
        var viewModel =
            new OutboxMessageViewModel
            {
                Id = message.Id,

                Status =
                    new EnumItem<OutboxMessageStatus>(
                        message.Status,
                        message.Status.GetDescription()),

                RecipientId = message.RecipientId,

                RecipientName =
                    message.RecipientName,

                RecipientType =
                    new EnumItem<RecipientType>(
                        message.RecipientType,
                        message.RecipientType.GetDescription()),

                OrderNumber =
                    message.OrderNumber,

                Text =
                    message.Text,

                PhoneNumber =
                    message.PhoneNumber,

                CreatedAt =
                    message.CreatedAt,

                LastAttemptAt =
                    message.LastAttemptAt,
            };

        viewModel.AddAttachments(
            message.Attachments.Select(
                attachment =>
                    new MessageAttachmentItem(
                        $"http://{_serverConfiguration.ApiBaseUrl}/files/attachments/{attachment.StoredName}",
                        attachment.OriginalName,
                        attachment.Extension,
                        attachment.Size)));

        viewModel.AddNotes(
            message.Notes?.ToList());

        return viewModel;
    }
    // =========================
    // Helpers
    // =========================

    private void CancelFilterRequest()
    {
        _filterCts?.Cancel();
        _filterCts?.Dispose();

        _filterCts = null;
    }
}