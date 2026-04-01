using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class WhatsappService : IWhatsappService, IMessageSender
{
    private const string _defaultUrl = "https://web.whatsapp.com";

    private IWebDriver _driver;
    private WebDriverWait _wait;

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly IApplicationDirectoriesService _directories;

    public WhatsappService(IApplicationDirectoriesService directories)
    {
        _directories = directories;
    }

    public async Task<bool> StartWhatsAppAsync(string url = _defaultUrl)
    {
        await _lifecycleLock.WaitAsync();

        try
        {
            if (IsDriverAlive())
            {
                _driver.Navigate().GoToUrl(url);
                return WaitForWhatsAppReady();
            }

            CloseDriver();

            ChromeOptions options = new ChromeOptions();
            options.AddArgument($"--user-data-dir={_directories.DefaultWhatAppProfilePath}\\MainProfile");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--disable-background-timer-throttling");
            options.AddArgument("--remote-debugging-port=9222");

            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
            _driver.Navigate().GoToUrl(url);

            return WaitForWhatsAppReady();
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            CloseDriver();
            return false;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public async Task<bool> SendToPhoneAsync(string phoneNumber, string message)
    {
        await _lifecycleLock.WaitAsync();

        try
        {
            if (!IsDriverAlive())
            {
                CloseDriver();
                return false;
            }

            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(cleanNumber))
            {
                return false;
            }

            return await NavigateAndSendAsync($"{_defaultUrl}/send?phone={cleanNumber}", message, isGroupChat: false);
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public Task<bool> SendAsync(string contact, string message)
    {
        return LooksLikeGroupLink(contact)
            ? SendToGroupAsync(contact, message)
            : SendToPhoneAsync(contact, message);
    }

    public async Task<bool> SendToGroupAsync(string groupLink, string message)
    {
        await _lifecycleLock.WaitAsync();

        try
        {
            if (!IsDriverAlive())
            {
                CloseDriver();
                return false;
            }

            if (string.IsNullOrWhiteSpace(groupLink))
            {
                return false;
            }

            string normalizedLink = NormalizeGroupLink(groupLink);
            if (string.IsNullOrWhiteSpace(normalizedLink))
            {
                return false;
            }

            return await NavigateAndSendAsync(normalizedLink, message, isGroupChat: true);
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    private Task<bool> NavigateAndSendAsync(string url, string message, bool isGroupChat)
    {
        _driver.Navigate().GoToUrl(url);

        if (isGroupChat && !EnsureGroupChatOpened())
        {
            return Task.FromResult(false);
        }

        IWebElement messageBox = WaitForMessageBox();
        if (messageBox is null)
        {
            return Task.FromResult(false);
        }

        string[] lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            messageBox.SendKeys(lines[i]);
            if (i < lines.Length - 1)
            {
                messageBox.SendKeys(Keys.Shift + Keys.Enter);
            }
        }

        messageBox.SendKeys(Keys.Enter);

        return Task.FromResult(WaitForMessageAccepted(messageBox, message));
    }

    private static string NormalizeGroupLink(string groupLink)
    {
        string trimmed = groupLink?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertInviteLinkToWebUrl(trimmed);
        }

        return ConvertInviteLinkToWebUrl($"https://{trimmed.TrimStart('/')}");
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

        return $"{_defaultUrl}/accept?code={inviteCode}";
    }

    private static bool LooksLikeGroupLink(string destination)
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

    private bool EnsureGroupChatOpened()
    {
        if (_wait is null || _driver is null)
        {
            return false;
        }

        try
        {
            if (FindMessageBox(_driver) is not null)
            {
                return true;
            }

            string[] existingHandles = _driver.WindowHandles.ToArray();

            return _wait.Until(driver =>
            {
                if (HasLoginPrompt(driver) || HasInvalidChatState(driver))
                {
                    return false;
                }

                SwitchToNewestWindow(existingHandles);

                if (FindMessageBox(driver) is not null)
                {
                    return true;
                }

                IWebElement actionButton = FindContinueToChatButton(driver);
                if (actionButton is not null)
                {
                    ClickElement(actionButton);
                    SwitchToNewestWindow(existingHandles);
                }

                return FindMessageBox(driver) is not null;
            });
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static IWebElement FindContinueToChatButton(IWebDriver driver)
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

    private void SwitchToNewestWindow(string[] previousHandles)
    {
        try
        {
            string newHandle = _driver.WindowHandles
                .FirstOrDefault(handle => !previousHandles.Contains(handle));

            if (!string.IsNullOrWhiteSpace(newHandle))
            {
                _driver.SwitchTo().Window(newHandle);
            }
        }
        catch
        {
        }
    }

    private void ClickElement(IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch
        {
            if (_driver is IJavaScriptExecutor executor)
            {
                executor.ExecuteScript("arguments[0].click();", element);
            }
        }
    }

    private bool WaitForWhatsAppReady()
    {
        if (_driver is null || _wait is null)
        {
            return false;
        }

        try
        {
            return _wait.Until(driver =>
            {
                if (HasLoginPrompt(driver))
                {
                    return false;
                }

                try
                {
                    return driver.FindElements(By.Id("side")).Any()
                        || FindMessageBox(driver) is not null;
                }
                catch
                {
                    return false;
                }
            });
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private IWebElement WaitForMessageBox()
    {
        if (_wait is null)
        {
            return null;
        }

        try
        {
            return _wait.Until(driver =>
            {
                if (HasLoginPrompt(driver) || HasInvalidChatState(driver))
                {
                    return null;
                }

                IWebElement messageBox = FindMessageBox(driver);
                if (messageBox is null)
                {
                    return null;
                }

                return IsChatComposer(messageBox) ? messageBox : null;
            });
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return null;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static IWebElement FindMessageBox(IWebDriver driver)
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

    private static bool IsChatComposer(IWebElement element)
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

    private bool WaitForMessageAccepted(IWebElement messageBox, string message)
    {
        if (_wait is null)
        {
            return false;
        }

        try
        {
            return _wait.Until(driver =>
            {
                if (HasLoginPrompt(driver) || HasInvalidChatState(driver))
                {
                    return false;
                }

                string currentText = messageBox.Text?.Trim() ?? string.Empty;
                return string.IsNullOrEmpty(currentText)
                    || !string.Equals(currentText, message.Trim(), StringComparison.Ordinal);
            });
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasLoginPrompt(IWebDriver driver)
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

    private static bool HasInvalidChatState(IWebDriver driver)
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

            if (driver.FindElements(By.XPath($"//*[contains(normalize-space(.),\"{marker}\")]")).Any())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDriverAlive()
    {
        try
        {
            return _driver is not null && _driver.WindowHandles.Any();
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
    }

    public void Close()
    {
        CloseDriver();
    }

    private void CloseDriver()
    {
        try
        {
            _driver?.Quit();
        }
        catch
        {
        }
        finally
        {
            _driver = null;
            _wait = null;
        }
    }

    private static bool IsClosedWindowError(WebDriverException exception)
    {
        string message = exception.Message ?? string.Empty;
        return message.Contains("no such window", StringComparison.OrdinalIgnoreCase)
            || message.Contains("target window already closed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("web view not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("invalid session id", StringComparison.OrdinalIgnoreCase)
            || message.Contains("session deleted as the browser has closed the connection", StringComparison.OrdinalIgnoreCase);
    }
}
