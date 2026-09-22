using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal sealed class RemoteFileStorageService
    : IFileStorageService
{
    private readonly HttpClient _httpClient;

    public RemoteFileStorageService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("OrderHubApi");
    }

    public Uri GetDocumentUri(
        DocumentType documentType,
        string fileName)
    {
        string path = documentType switch
        {
            DocumentType.Quotation =>
                "files/quotations",

            DocumentType.ProformaInvoice =>
                "files/proforma-invoices",

            DocumentType.Invoice =>
                "files/invoices",

            _ => throw new ArgumentOutOfRangeException(
                nameof(documentType))
        };

        return new Uri(
            _httpClient.BaseAddress!,
            $"{path}/{Uri.EscapeDataString(fileName)}");
    }

    #region Attachments

    public async Task<StoredFile> SaveAttachmentAsync(
        string sourceFilePath,
        string destinationName,
        CancellationToken cancellationToken = default)
    {
        await using FileStream stream = new(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        using StreamContent fileContent = new(stream);
        using MultipartFormDataContent content = new();

        content.Add(
            fileContent,
            "file",
            destinationName);

        content.Add(
            new StringContent(Path.GetFileName(sourceFilePath)),
            "originalFileName");

        using HttpResponseMessage response =
            await _httpClient.PostAsync(
                $"files/attachments/{EncodeFileName(destinationName)}",
                content,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await ReadStoredFileAsync(
            response,
            cancellationToken);
    }

    public async Task<byte[]> GetAttachmentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await GetFileAsync(
            $"files/attachments/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    public async Task DeleteAttachmentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await DeleteFileAsync(
            $"files/attachments/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    #endregion


    #region Quotations

    public async Task SaveQuotationDocumentAsync(
        byte[] document,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await UploadDocumentAsync(
            $"files/quotations/{EncodeFileName(fileName)}",
            document,
            fileName,
            cancellationToken);
    }

    public async Task<byte[]> GetQuotationDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await GetFileAsync(
            $"files/quotations/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    public async Task DeleteQuotationDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await DeleteFileAsync(
            $"files/quotations/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    #endregion


    #region Proforma Invoices

    public async Task SaveProformaInvoiceDocumentAsync(
        byte[] document,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await UploadDocumentAsync(
            $"files/proforma-invoices/{EncodeFileName(fileName)}",
            document,
            fileName,
            cancellationToken);
    }

    public async Task<byte[]> GetProformaInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await GetFileAsync(
            $"files/proforma-invoices/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    public async Task DeleteProformaInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await DeleteFileAsync(
            $"files/proforma-invoices/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    #endregion


    #region Invoices

    public async Task SaveInvoiceDocumentAsync(
        byte[] document,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await UploadDocumentAsync(
            $"files/invoices/{EncodeFileName(fileName)}",
            document,
            fileName,
            cancellationToken);
    }

    public async Task<byte[]> GetInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await GetFileAsync(
            $"files/invoices/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    public async Task DeleteInvoiceDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await DeleteFileAsync(
            $"files/invoices/{EncodeFileName(fileName)}",
            cancellationToken);
    }

    #endregion


    #region HTTP Helpers

    private async Task UploadDocumentAsync(
        string endpoint,
        byte[] document,
        string fileName,
        CancellationToken cancellationToken)
    {
        using ByteArrayContent fileContent =
            new(document);

        using MultipartFormDataContent content = new();

        content.Add(
            fileContent,
            "file",
            fileName);

        using HttpResponseMessage response =
            await _httpClient.PostAsync(
                endpoint,
                content,
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<byte[]> GetFileAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                endpoint,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(
            cancellationToken);
    }

    private async Task DeleteFileAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await _httpClient.DeleteAsync(
                endpoint,
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<StoredFile> ReadStoredFileAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return await response.Content
            .ReadFromJsonAsync<StoredFile>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The server returned an invalid file response.");
    }

    private static string EncodeFileName(
        string fileName)
    {
        return Uri.EscapeDataString(fileName);
    }

    public async Task SaveFileAsync(
        string url,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using Stream source =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        await using FileStream destination = new(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await source.CopyToAsync(
            destination,
            cancellationToken);
    }

    #endregion
}