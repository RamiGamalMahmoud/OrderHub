using OrderHub.Domain.Enums;

namespace OrderHub.Infrastructure.Models;

internal class DocumentSequence
{
    private DocumentSequence()
    {
    }

    private DocumentSequence(DocumentType documentType, int year, int month)
    {
        DocumentType = documentType;
        Month = month;
        Year = year;
    }

    public DocumentType DocumentType { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int LastNumber { get; private set; }

    public static DocumentSequence Create(DocumentType documentType, int year, int month)
    {
        return new DocumentSequence(documentType, year, month );
    }

    public void Increment()
    {
        LastNumber++;
    }
}