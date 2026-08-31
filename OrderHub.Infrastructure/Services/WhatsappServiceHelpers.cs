using OpenQA.Selenium;
using System;
using System.Linq;

namespace OrderHub.Infrastructure.Services;

internal static class WhatsappServiceHelpers
{
    private const string _baseUrl = "https://web.whatsapp.com";
    public static bool LooksLikeGroupLink(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return false;
        }

        string value = destination.Trim();
        return value.Contains("chat.whatsapp.com", StringComparison.OrdinalIgnoreCase)
            || value.Contains("whatsapp.com", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeGroupLink(string destination)
    {
        string link = destination?.Trim();
        if (string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertInviteLinkToWebUrl(link);
        }

        return ConvertInviteLinkToWebUrl($"https://{link.TrimStart('/')}");
    }

    private static string ConvertInviteLinkToWebUrl(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
        {
            return link;
        }

        if (!uri.Host.Contains("chat.whatsapp.com", StringComparison.OrdinalIgnoreCase))
        {
            return link;
        }

        string inviteCode = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            return link;
        }

        return $"{_baseUrl}/accept?code={inviteCode}";
    }

    public static bool HasInvalidChatState(IWebDriver driver)
    {
        string[] markers =
        [
            "The number isn't on WhatsApp",
            "The number is not on WhatsApp",
            "This number isn't on WhatsApp",
            "This number is not on WhatsApp",
            "isn't on WhatsApp",
            "not on WhatsApp",
            "Phone number shared via url is invalid",
            "The phone number shared via url is invalid",
            "غير صالح",
            "ليس على واتساب"
        ];

        string pageText = string.Empty;

        try
        {
            pageText = driver.FindElement(By.TagName("body")).Text ?? string.Empty;
        }
        catch
        {
        }

        foreach (string marker in markers)
        {
            if (!string.IsNullOrWhiteSpace(pageText)
                && pageText.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (driver.FindElements(By.XPath($"//*[contains(normalize-space(.),\"{marker}\")]")).Count > 0)
            {
                return true;
            }
        }

        return false;
    }


    public static bool IsClosedWindowError(WebDriverException exception)
    {
        string message = exception.Message ?? string.Empty;
        return message.Contains("no such window", StringComparison.OrdinalIgnoreCase)
            || message.Contains("target window already closed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("web view not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid session id", StringComparison.OrdinalIgnoreCase)
            || message.Contains("session deleted as the browser has closed the connection", StringComparison.OrdinalIgnoreCase);
    }

    public static IWebElement FindContinueToChatButton(IWebDriver driver)
    {
        string[] selectors =
        [
            "//a[contains(@href,'web.whatsapp.com/send') or contains(@href,'web.whatsapp.com/accept')]",
            "//a[contains(@href,'web.whatsapp.com') and (contains(.,'Continue to chat') or contains(.,'Continue') or contains(.,'Join chat'))]",
            "//button[contains(.,'Continue to chat') or contains(.,'Join chat') or contains(.,'Use WhatsApp Web')]",
            "//a[contains(.,'Join chat') or contains(.,'Continue to chat')]",
            "//button[contains(.,'واتساب ويب') or contains(.,'الدردشة')]",
            "//a[contains(.,'واتساب ويب') or contains(.,'الدردشة')]"
        ];

        foreach (string selector in selectors)
        {
            IWebElement button = driver
                .FindElements(By.XPath(selector))
                .FirstOrDefault(element => element.Displayed && element.Enabled);

            if (button is not null)
            {
                return button;
            }
        }

        return null;
    }

    public static IWebElement FindMessageBox(IWebDriver driver)
    {
        string[] selectors =
        [
            "//footer//div[@contenteditable='true' and @role='textbox']",
            "//footer//*[@contenteditable='true']"
        ];

        foreach (string selector in selectors)
        {
            IWebElement messageBox = driver
                .FindElements(By.XPath(selector))
                .FirstOrDefault(element => element.Displayed && element.Enabled);

            if (messageBox is not null)
            {
                return messageBox;
            }
        }

        return null;
    }

    public static bool HasLoginPrompt(IWebDriver driver)
    {
        try
        {
            return driver.FindElements(By.CssSelector("canvas[aria-label*='QR'], canvas[aria-label*='qr']")).Any()
                || driver.FindElements(By.XPath("//*[contains(text(),'Scan') and contains(text(),'QR')]")).Any();
        }
        catch
        {
            return false;
        }
    }

    public static bool IsChatComposer(IWebElement element)
    {
        try
        {
            IWebElement current = element;
            while (current is not null)
            {
                if (string.Equals(current.TagName, "footer", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.FindElement(By.XPath(".."));
            }
        }
        catch
        {
        }

        return false;
    }
}
