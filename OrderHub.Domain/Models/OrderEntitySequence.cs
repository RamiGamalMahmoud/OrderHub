using OrderHub.Domain.Enums;

namespace OrderHub.Domain.Models;

public class OrderEntitySequence : ModelBase
{
    private OrderEntitySequence() { }

    public OrderEntitySequence(
        RecipientType recipientType,
        int entityId,
        int sequenceYear,
        int sequenceMonth,
        int sequenceNumber,
        string displayTitle)
    {
        RecipientType = recipientType;
        EntityId = entityId;
        SequenceYear = sequenceYear;
        SequenceMonth = sequenceMonth;
        SequenceNumber = sequenceNumber;
        DisplayTitle = displayTitle;
    }

    public int OrderId { get; private set; }
    public Order Order { get; private set; }

    public RecipientType RecipientType { get; private set; }
    public int EntityId { get; private set; }
    public int SequenceYear { get; private set; }
    public int SequenceMonth { get; private set; }
    public int SequenceNumber { get; private set; }
    public string DisplayTitle { get; private set; }
}
