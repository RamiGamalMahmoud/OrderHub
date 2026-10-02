using System.IO;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface ILocalFileAccessService
{
    bool FileExists(
        string fileName);

    // Attachments
    Task SaveAttachmentAsync(
        string fileName,
        Stream content);

    Task<string> GetAttachmentAsync(
        string fileName);

    Task DeleteAttachmentAsync(
        string fileName);


    // Quotations
    Task SaveQuotationAsync(
        string fileName,
        byte[] content);

    Task<string> GetQuotationAsync(
        string fileName);

    Task DeleteQuotationAsync(
        string fileName);


    // Invoices
    Task SaveInvoiceAsync(
        string fileName,
        byte[] content);

    Task<string> GetInvoiceAsync(
        string fileName);

    Task DeleteInvoiceAsync(
        string fileName);


    // Proforma Invoices
    Task SaveProformaInvoiceAsync(
        string fileName,
        byte[] content);

    Task<string> GetProformaInvoiceAsync(
        string fileName);

    Task DeleteProformaInvoiceAsync(
        string fileName);
}
