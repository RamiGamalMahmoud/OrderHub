namespace OrderHub.Domain.Models;

public class OrderItemProperty : ModelBase
{
    public int OrderItemId { get; private set; }

    private int _propertyId;
    private string _value;
    private Property _property;
    public int PropertyId { get => _propertyId; set { EnsureEditable(); _propertyId = value; } }

    public string Value { get => _value; set { EnsureEditable(); _value = value; } }

    public OrderItem OrderItem { get; private set; }

    public Property Property { get => _property; set { EnsureEditable(); _property = value; } }

    internal void AttachTo(OrderItem item) { OrderItem = item; OrderItemId = item.Id; }
    private void EnsureEditable()
    {
        if (OrderItem?.Order is not null && !OrderItem.Order.CanEdit)
            throw new System.InvalidOperationException("بيانات الطلب مقفولة ولا تقبل التعديل.");
    }
}
