namespace OrderHub.Web.Infrastructure.DeviceAccess;

public sealed class DeviceAccessMiddleware(RequestDelegate next, IDeviceDetector detector)
{
    private const string UnsupportedDeviceHtml = """
        <!doctype html>
        <html lang="ar" dir="rtl">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>جهاز غير مدعوم — OrderHub</title>
          <style>
            body { margin: 0; padding: 24px; min-height: 100vh; box-sizing: border-box;
                   display: grid; place-items: center; background: #f5f6f8; color: #243047;
                   font-family: Tahoma, Arial, sans-serif; }
            main { max-width: 480px; padding: 32px; background: white; border: 1px solid #e2e5eb;
                   border-radius: 12px; text-align: center; line-height: 1.8; }
            h1 { margin: 0 0 16px; font-size: 24px; }
            p { margin: 0; }
          </style>
        </head>
        <body>
          <main>
            <h1>هذا الجهاز غير مدعوم</h1>
            <p>نظام OrderHub مخصص للاستخدام من أجهزة الكمبيوتر المكتبية والمحمولة فقط.</p>
          </main>
        </body>
        </html>
        """;

    public async Task InvokeAsync(HttpContext context)
    {
        var device = detector.Detect(context.Request);
        if (device is DeviceType.Mobile or DeviceType.Tablet)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync(UnsupportedDeviceHtml, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
