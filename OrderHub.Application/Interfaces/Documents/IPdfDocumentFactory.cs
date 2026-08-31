namespace OrderHub.Application.Interfaces.Documents;

public interface IPdfDocumentFactory
{
    IPdfDocument Create<TData>(TData data) where TData : class;
}