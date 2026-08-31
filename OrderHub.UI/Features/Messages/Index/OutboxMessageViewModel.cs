using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OrderHub.UI.Features.Messages.Index;

public partial class OutboxMessageViewModel : ObservableObject
{
    public OutboxMessageViewModel()
    {
        Attachments.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(AttachmentsCount));
            OnPropertyChanged(nameof(HasAttchaments));
        };

        Notes.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(NotesCount));
            OnPropertyChanged(nameof(HasNotes));
        };
    }

    public int Id { get; set; }

    [ObservableProperty]
    private EnumItem<OutboxMessageStatus> _status;

    public string OrderNumber { get; init; }
    public string RecipientName { get; init; }
    public EnumItem<RecipientType> RecipientType { get; init; }
    public string Text { get; init; }
    public string PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastAttemptAt { get; init; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddNoteCommand))]
    private string _newNoteText;

    public string Title => Text?.Split("\n").FirstOrDefault() ?? string.Empty;

    public bool CanResend => Status?.Value == OutboxMessageStatus.Failed;

    public bool HasNotes => NotesCount > 0;
    public bool HasAttchaments => AttachmentsCount > 0;

    public ObservableCollection<MessageAttachmentItem> Attachments { get; } = [];
    public ObservableCollection<string> Notes { get; } = [];

    public int AttachmentsCount => Attachments.Count;
    public int NotesCount => Notes.Count;

    public void AddAttachments(Dictionary<string, string> attachments)
    {
        if (attachments is null || attachments.Count == 0)
            return;

        foreach (var attachment in attachments)
        {
            if (!string.IsNullOrEmpty(attachment.Key))
                Attachments.Add(new MessageAttachmentItem(attachment.Key, attachment.Value));
        }
    }

    public void AddAttachments(List<string> attachments)
    {
        if (attachments is null || attachments.Count == 0)
            return;

        foreach (string attachment in attachments)
        {
            if (!string.IsNullOrEmpty(attachment))
                Attachments.Add(new MessageAttachmentItem(attachment));
        }
    }

    [RelayCommand]
    private void AddAttachments()
    {
        OpenFileDialog fileDialog = new()
        {
            Multiselect = true,
            Title = "اختر مرفق أو أكثر",
            Filter = "كل الملفات المدعومة (*.jpg;*.jpeg;*.png;*.pdf;*.docx)|*.jpg;*.jpeg;*.png;*.pdf;*.docx|" +
                     "الصور (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|" +
                     "المستندات (*.pdf;*.docx)|*.pdf;*.docx"
        };

        if (fileDialog.ShowDialog() is true)
            AddAttachments(fileDialog.FileNames.ToList());
    }

    [RelayCommand]
    private void RemoveAttachment(MessageAttachmentItem attachment)
    {
        if (attachment is null)
            return;

        Attachments.Remove(attachment);
    }

    public void AddNotes(List<string> notes)
    {
        if (notes is null || notes.Count == 0)
            return;

        foreach (string note in notes)
            Notes.Add(note);
    }

    [RelayCommand(CanExecute = nameof(CanAddNote))]
    private void AddNote()
    {
        Notes.Add(NewNoteText);
        NewNoteText = string.Empty;
    }

    private bool CanAddNote => !string.IsNullOrEmpty(NewNoteText);

    [RelayCommand]
    private void RemoveNote(string note)
    {
        if (Notes.Contains(note))
            Notes.Remove(note);
    }

    partial void OnStatusChanged(EnumItem<OutboxMessageStatus> oldValue, EnumItem<OutboxMessageStatus> newValue)
        => OnPropertyChanged(nameof(CanResend));
}