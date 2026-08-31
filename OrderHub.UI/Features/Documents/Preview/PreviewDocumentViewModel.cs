using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.UI.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Documents.Preview;

public partial class PreviewDocumentViewModel : ObservableObject, IParameterizedViewModel
{
    public Task Initialize(object parameter)
    {
        if (parameter is not Data data)
            throw new System.ArithmeticException();

        if(!File.Exists(data.FilePath))
            throw new FileNotFoundException();

        FileInfo documentInfo = new FileInfo(data.FilePath);

        DocumentName = documentInfo.Name;
        DocumentPath = data.FilePath;
        RecipientName = data.RecipientName;
        Destination = data.Destination;
        DocumentUri = new Uri(DocumentPath);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ShowInFolder()
    {
        if (string.IsNullOrWhiteSpace(DocumentPath))
            return;

        if (!File.Exists(DocumentPath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{DocumentPath}\"",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void OpenInBrowser()
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = DocumentPath,
            UseShellExecute = true
        });
    }

    [ObservableProperty]
    private string _documentName;

    [ObservableProperty]
    private string _documentPath;

    [ObservableProperty]
    private string _recipientName;

    [ObservableProperty]
    private string _destination;

    [ObservableProperty]
    private Uri _documentUri;

    [RelayCommand]
    private async Task SendDocument()
    {
        await DialogService.Instance.ShowDialog<Features.Messages.SendMessage.SendMessageView>("إرسال رسالة جديدة", new Features.Messages.SendMessage.MessageData(
            "",
            Destination,
            [DocumentPath]));
    }
}

public record Data(string FilePath, string RecipientName, string Destination);