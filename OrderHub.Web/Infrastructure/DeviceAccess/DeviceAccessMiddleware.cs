namespace OrderHub.Web.Infrastructure.DeviceAccess;

public sealed class DeviceAccessMiddleware(RequestDelegate next, IDeviceDetector detector)
{
    private const string UNSUPPORTED_DEVICE_HTML = """
    <!doctype html>
    <html lang="ar" dir="rtl">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">

        <title>جهاز غير مدعوم — OrderHub</title>

        <style>
            * {
                box-sizing: border-box;
            }

            body {
                margin: 0;
                min-height: 100vh;
                display: flex;
                align-items: center;
                justify-content: center;
                padding: 24px;

                background: #F8FAFC;
                color: #0F172A;

                font-family: "Tajawal", "Cairo", Tahoma, Arial, sans-serif;
            }

            .container {
                width: 100%;
                max-width: 460px;
                text-align: center;
            }

            .brand {
                margin-bottom: 32px;

                font-size: 24px;
                font-weight: 700;
                color: #2563EB;
                letter-spacing: -0.5px;
            }

            .card {
                padding: 40px 32px;

                background: #FFFFFF;
                border: 1px solid #E2E8F0;
                border-radius: 16px;
            }

            .icon {
                width: 64px;
                height: 64px;

                display: flex;
                align-items: center;
                justify-content: center;

                margin: 0 auto 24px;

                border-radius: 50%;
                background: #EFF6FF;
                color: #2563EB;
            }

            .icon svg {
                width: 32px;
                height: 32px;
            }

            h1 {
                margin: 0 0 12px;

                font-size: 24px;
                font-weight: 700;
                line-height: 1.4;
            }

            p {
                margin: 0;

                color: #64748B;
                font-size: 16px;
                line-height: 1.9;
            }

            .hint {
                margin-top: 24px;
                padding-top: 24px;

                border-top: 1px solid #E2E8F0;

                color: #94A3B8;
                font-size: 14px;
                line-height: 1.7;
            }
        </style>
    </head>

    <body>
        <main class="container">

            <div class="brand">
                OrderHub
            </div>

            <section class="card">

                <div class="icon" aria-hidden="true">
                    <svg
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="1.8"
                        stroke-linecap="round"
                        stroke-linejoin="round">

                        <rect x="3" y="4" width="18" height="12" rx="2"></rect>
                        <path d="M8 20h8"></path>
                        <path d="M12 16v4"></path>
                    </svg>
                </div>

                <h1>هذا الجهاز غير مدعوم</h1>

                <p>
                    نظام OrderHub مخصص للاستخدام من أجهزة
                    الكمبيوتر المكتبية والمحمولة فقط.
                </p>

                <div class="hint">
                    يرجى فتح النظام من جهاز كمبيوتر للمتابعة.
                </div>

            </section>

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
            await context.Response.WriteAsync(UNSUPPORTED_DEVICE_HTML, context.RequestAborted);
            return;
        }

        await next(context);
    }
}
