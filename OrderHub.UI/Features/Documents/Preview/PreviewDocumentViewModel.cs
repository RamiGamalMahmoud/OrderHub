using CommunityToolkit.Mvvm.ComponentModel;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Documents.Preview;

public partial class PreviewDocumentViewModel :
    ObservableObject,
    IParameterizedViewModel
{
    private readonly IFileStorageService _fileStorageService;

    public PreviewDocumentViewModel(
        IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public Task Initialize(object parameter)
    {
        if (parameter is not Data data)
            throw new ArgumentException(
                "Invalid document preview data.",
                nameof(parameter));

        DocumentName = data.FileName;
        RecipientName = data.RecipientName;
        Destination = data.Destination;
        DocumentType = data.DocumentType;

        DocumentUri = _fileStorageService.GetDocumentUri(
            data.DocumentType,
            data.FileName);

        return Task.CompletedTask;
    }

    [ObservableProperty]
    private string _documentName;

    [ObservableProperty]
    private string _recipientName;

    [ObservableProperty]
    private string _destination;

    [ObservableProperty]
    private Uri _documentUri;

    private DocumentType DocumentType { get; set; }
}

public record Data(
    string FileName,
    DocumentType DocumentType,
    string RecipientName,
    string Destination);