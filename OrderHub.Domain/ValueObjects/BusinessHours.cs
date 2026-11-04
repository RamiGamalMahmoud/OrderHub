using System;

namespace OrderHub.Domain.ValueObjects;

public record BusinessHours
{
    // الخصائص قابلة لاتاحة قيمة فارغة (null) ليتوافق مع أعمدة قاعدة البيانات،
    // بينما يفرض المُنشئ توفير الوقيتين معًا.
    public TimeOnly? OpenAt { get; }

    public TimeOnly? CloseAt { get; }

    public BusinessHours(TimeOnly? openAt, TimeOnly? closeAt)
    {
        // أي توليفة مسموح: وقت الافتتاح فقط، وقت الإغلاق فقط، الاثنان معًا، أو لا شيء.
        if (openAt > closeAt)
            throw new ArgumentException("Closing time must be after opening time.");

        OpenAt = openAt;
        CloseAt = closeAt;
    }
}
