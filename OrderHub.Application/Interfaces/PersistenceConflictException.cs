using System;

namespace OrderHub.Application.Interfaces;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(Exception innerException)
        : base("تم تعديل الطلب بواسطة مستخدم آخر. حدّث البيانات ثم راجع العملية.", innerException) { }
}
