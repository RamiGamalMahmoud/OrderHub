namespace OrderHub.UI.Common
{
    internal static class MessageBuilder
    {
        public static string Build(OperationType operationType, bool success, string model, string details ="")
        {
            return operationType switch
            {
                OperationType.Create when success => $"🎉 تم إنشاء {model} بنجاح! {details}",
                OperationType.Create => $"😞 عذراً، لم نتمكن من إنشاء {model}. {details}",

                OperationType.Update when success => $"✨ تم تحديث {model} بنجاح! {details}",
                OperationType.Update => $"⚠️ تعذر تحديث {model}. {details}",

                OperationType.Delete when success => $"🗑️ تمت إزالة {model} بنجاح. {details}",
                OperationType.Delete => $"🔒 تعذر حذف {model}. {details}",

                _ => success ? "تم إكمال العملية" : "فشلت العملية"
            };
        }

        public enum OperationType
        {
            Create, Update, Delete
        }
    }
}
