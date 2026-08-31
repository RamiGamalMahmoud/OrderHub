using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Application.Interfaces.Services;

public interface IInvoiceService
{
    void GenerateInvoiceReport(string savePath, Invoice invoice);
}
