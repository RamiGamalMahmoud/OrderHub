using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class WhatsappService : IWhatsappService, IMessageSender
{
    private const string _defaultUrl = "https://web.whatsapp.com";

    private IWebDriver _driver;
    private WebDriverWait _wait;

    private readonly SemaphoreSlim _driverLock = new(1, 1);
    private readonly IApplicationDirectoriesService _directories;

    public WhatsappService(IApplicationDirectoriesService directories)
    {
        _directories = directories;
    }

    public async Task<bool> StartWhatsAppAsync(string url = _defaultUrl)
    {
        await _driverLock.WaitAsync();

        try
        {
            if (IsDriverAlive())
            {
                _driver.Navigate().GoToUrl(url);
                return WaitForWhatsAppReady();
            }

            CloseDriver();

            ChromeOptions options = new ChromeOptions();
            options.AddArgument($"--user-data-dir={_directories.DefaultWhatsAppProfileDirectory}\\MainProfile");
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
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
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
            _driverLock.Release();
        }
    }

    public Task<bool> SendAsync(string destination, MessageToSend message)
    {
        return WhatsappServiceHelpers.LooksLikeGroupLink(destination)
            ? SendToGroupAsync(destination, message)
            : SendToPhoneAsync(destination, message);
    }

    public async Task<bool> SendToPhoneAsync(string destination, MessageToSend message)
    {
        await _driverLock.WaitAsync();

        try
        {
            if (!IsDriverAlive())
            {
                CloseDriver();
                return false;
            }

            string cleanNumber = new string(destination.Where(char.IsDigit).ToArray());

            if (string.IsNullOrWhiteSpace(cleanNumber))
            {
                return false;
            }

            return NavigateAndSend(
                $"{_defaultUrl}/send?phone={cleanNumber}",
                message,
                isGroupChat: false);
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
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
            _driverLock.Release();
        }
    }

    public async Task<bool> SendToGroupAsync(string destination, MessageToSend message)
    {
        await _driverLock.WaitAsync();

        try
        {
            if (!IsDriverAlive())
            {
                CloseDriver();
                return false;
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                return false;
            }

            string normalizedLink = WhatsappServiceHelpers.NormalizeGroupLink(destination);

            if (string.IsNullOrWhiteSpace(normalizedLink))
            {
                return false;
            }

            return NavigateAndSend(
                normalizedLink,
                message,
                isGroupChat: true);
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
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
            _driverLock.Release();
        }
    }

    public void Close()
    {
        CloseDriver();
    }

    private bool NavigateAndSend(string url, MessageToSend message, bool isGroupChat)
    {
        _driver.Navigate().GoToUrl(url);

        if (isGroupChat && !EnsureGroupChatOpened())
        {
            return false;
        }

        //if(! SendTextMessage(message.Message))
        //{
        //    return false;
        //}

        if (message.HasAttachments && !SendAttachments(message.Attachments))
        {
            return false;
        }

        return true;
    }

    private bool SendAttachments(IReadOnlyCollection<string> attachments)
    {
        if (attachments is null || attachments.Count == 0)
        {
            Debug.WriteLine("SendAttachments: No attachments.");
            return true;
        }

        try
        {
            // ---------------------------------------------------------
            // 1. Find Attach button
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Finding Attach button...");

            var attachButton = new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(10))
                .Until(driver =>
                    driver.FindElements(
                            By.XPath(
                                "//span[@data-icon='plus-rounded']"))
                        .FirstOrDefault());

            if (attachButton is null)
            {
                Debug.WriteLine(
                    "SendAttachments: Attach button not found.");

                return false;
            }

            Debug.WriteLine(
                "SendAttachments: Attach button found.");

            // ---------------------------------------------------------
            // 2. Open attachment menu
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Opening attachment menu...");

            attachButton.Click();

            // ---------------------------------------------------------
            // 3. Find Document option
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Finding Document option...");

            var documentButton = new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(10))
                .Until(driver =>
                    driver.FindElements(
                            By.XPath(
                                "//button[@role='menuitem' and @aria-label='Document']"))
                        .FirstOrDefault());

            if (documentButton is null)
            {
                Debug.WriteLine(
                    "SendAttachments: Document button not found.");

                return false;
            }

            Debug.WriteLine(
                "SendAttachments: Document button found.");

            // ---------------------------------------------------------
            // 4. Install temporary file input click hook
            //
            // WhatsApp creates the real <input type="file"> when
            // Document is selected.
            //
            // WhatsApp then normally calls input.click(), which opens
            // the native Windows File Dialog.
            //
            // We temporarily intercept that click.
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Installing file input click hook...");

            var javascript = (IJavaScriptExecutor)_driver;

            javascript.ExecuteScript(@"
                (() => {
                    if (window.__whatsappFileInputHookInstalled) {
                        return;
                    }

                    window.__whatsappOriginalInputClick =
                        HTMLInputElement.prototype.click;

                    window.__whatsappFileInput =
                        null;

                    HTMLInputElement.prototype.click = function () {

                        if (this instanceof HTMLInputElement &&
                            this.type === 'file') {

                            window.__whatsappFileInput = this;

                            console.log(
                                '[SendAttachments] File input click intercepted.'
                            );

                            return;
                        }

                        return window.__whatsappOriginalInputClick
                            .call(this);
                    };

                    window.__whatsappFileInputHookInstalled = true;
                })();
            ");

            Debug.WriteLine(
                "SendAttachments: File input click hook installed.");

            // ---------------------------------------------------------
            // 5. Click Document
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Clicking Document...");

            try
            {
                documentButton.Click();
            }
            catch (StaleElementReferenceException)
            {
                Debug.WriteLine(
                    "SendAttachments: Document button became stale. Re-finding...");

                documentButton = new WebDriverWait(
                        _driver,
                        TimeSpan.FromSeconds(10))
                    .Until(driver =>
                        driver.FindElements(
                                By.XPath(
                                    "//button[@role='menuitem' and @aria-label='Document']"))
                            .FirstOrDefault());

                if (documentButton is null)
                {
                    Debug.WriteLine(
                        "SendAttachments: Document button could not be re-found.");

                    return false;
                }

                documentButton.Click();
            }

            Debug.WriteLine(
                "SendAttachments: Document clicked.");

            // ---------------------------------------------------------
            // 6. Wait for WhatsApp to create the REAL Document
            //    file input
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Waiting for Document file input...");

            var documentFileInput = new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(10))
                .Until(driver =>
                {
                    var input = driver.FindElements(
                            By.CssSelector(
                                "input[type='file'][accept='*']"))
                        .FirstOrDefault();

                    return input;
                });

            if (documentFileInput is null)
            {
                Debug.WriteLine(
                    "SendAttachments: Document file input not found.");

                return false;
            }

            Debug.WriteLine(
                "SendAttachments: Document file input found.");

            // ---------------------------------------------------------
            // 7. Inspect the REAL input
            // ---------------------------------------------------------

            try
            {
                Debug.WriteLine(
                    "SendAttachments: Inspecting Document file input...");

                Debug.WriteLine(
                    $"Tag: {documentFileInput.TagName}");

                Debug.WriteLine(
                    $"Type: {documentFileInput.GetAttribute("type")}");

                Debug.WriteLine(
                    $"Accept: {documentFileInput.GetAttribute("accept")}");

                Debug.WriteLine(
                    $"Multiple: {documentFileInput.GetAttribute("multiple")}");

                Debug.WriteLine(
                    $"Displayed: {documentFileInput.Displayed}");

                Debug.WriteLine(
                    $"Enabled: {documentFileInput.Enabled}");

                Debug.WriteLine(
                    $"OuterHtml: {documentFileInput.GetAttribute("outerHTML")}");
            }
            catch (StaleElementReferenceException)
            {
                Debug.WriteLine(
                    "SendAttachments: Document file input became stale during inspection.");

                documentFileInput = new WebDriverWait(
                        _driver,
                        TimeSpan.FromSeconds(5))
                    .Until(driver =>
                        driver.FindElements(
                                By.CssSelector(
                                    "input[type='file'][accept='*']"))
                            .FirstOrDefault());

                if (documentFileInput is null)
                {
                    Debug.WriteLine(
                        "SendAttachments: Could not re-find Document file input.");

                    return false;
                }
            }

            // ---------------------------------------------------------
            // 8. Restore original input.click()
            //
            // The native dialog has already been prevented.
            //
            // Now restore browser behavior before SendKeys.
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Restoring original file input click behavior...");

            javascript.ExecuteScript(@"
                (() => {
                    if (window.__whatsappFileInputHookInstalled) {

                        HTMLInputElement.prototype.click =
                            window.__whatsappOriginalInputClick;

                        window.__whatsappFileInputHookInstalled = false;
                    }
                })();
            ");

            Debug.WriteLine(
                "SendAttachments: Original file input click behavior restored.");

            // ---------------------------------------------------------
            // 9. Set file path directly on WhatsApp's REAL input
            // ---------------------------------------------------------

            var filePaths = string.Join(Environment.NewLine, attachments);

            documentFileInput.SendKeys(filePaths);

            Debug.WriteLine(
                "SendAttachments: File path sent to file input.");

            // IMPORTANT:
            //
            // Do NOT use documentFileInput from this point forward.
            // WhatsApp may rebuild the DOM after SendKeys().
            // ---------------------------------------------------------

            // ---------------------------------------------------------
            // 10. Wait for WhatsApp to process selected file
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Waiting for WhatsApp to process the selected file...");

            // ---------------------------------------------------------
            // 11. Find Send button
            //
            // Actual WhatsApp DOM:
            //
            // <div
            //     role="button"
            //     aria-label="Send 1 selected">
            //
            //     <span
            //         data-testid="wds-ic-send-filled"
            //         data-icon="wds-ic-send-filled">
            //     </span>
            //
            // </div>
            // ---------------------------------------------------------

            var sendButton = new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(15))
                .Until(driver =>
                    driver.FindElements(
                            By.XPath(
                                "//div[@role='button' and starts-with(@aria-label,'Send ') and .//span[@data-icon='wds-ic-send-filled']]"))
                        .FirstOrDefault());

            if (sendButton is null)
            {
                Debug.WriteLine(
                    "SendAttachments: Send button not found.");

                return false;
            }

            Debug.WriteLine(
                "SendAttachments: Send button found.");

            // ---------------------------------------------------------
            // 12. Inspect Send button
            // ---------------------------------------------------------

            try
            {
                Debug.WriteLine(
                    $"SendAttachments: Send button aria-label: " +
                    $"{sendButton.GetAttribute("aria-label")}");

                Debug.WriteLine(
                    $"SendAttachments: Send button role: " +
                    $"{sendButton.GetAttribute("role")}");

                Debug.WriteLine(
                    $"SendAttachments: Send button outerHTML: " +
                    $"{sendButton.GetAttribute("outerHTML")}");
            }
            catch (StaleElementReferenceException)
            {
                Debug.WriteLine(
                    "SendAttachments: Send button became stale. Re-finding...");

                sendButton = new WebDriverWait(
                        _driver,
                        TimeSpan.FromSeconds(5))
                    .Until(driver =>
                        driver.FindElements(
                                By.XPath(
                                    "//div[@role='button' and starts-with(@aria-label,'Send ') and .//span[@data-icon='wds-ic-send-filled']]"))
                            .FirstOrDefault());

                if (sendButton is null)
                {
                    Debug.WriteLine(
                        "SendAttachments: Send button could not be re-found.");

                    return false;
                }
            }

            // ---------------------------------------------------------
            // 13. Click Send
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Clicking Send button...");

            sendButton.Click();

            Debug.WriteLine(
                "SendAttachments: Send button clicked.");

            // ---------------------------------------------------------
            // 14. Wait for Send button to disappear
            //
            // This indicates that WhatsApp has left the attachment
            // preview/send state.
            // ---------------------------------------------------------

            Debug.WriteLine(
                "SendAttachments: Waiting for attachment to be sent...");

            var attachmentSent = new WebDriverWait(
                    _driver,
                    TimeSpan.FromSeconds(15))
                .Until(driver =>
                {
                    try
                    {
                        return !driver.FindElements(
                                By.XPath(
                                    "//div[@role='button' and starts-with(@aria-label,'Send ') and .//span[@data-icon='wds-ic-send-filled']]"))
                            .Any();
                    }
                    catch (StaleElementReferenceException)
                    {
                        return true;
                    }
                });

            if (!attachmentSent)
            {
                Debug.WriteLine(
                    "SendAttachments: Attachment sending could not be confirmed.");

                return false;
            }

            Debug.WriteLine(
                "SendAttachments: Attachment sending completed.");

            Debug.WriteLine(
                "SendAttachments: Attachment processed successfully.");


            Debug.WriteLine(
                "SendAttachments: All attachments processed successfully.");

            return true;
        }
        catch (WebDriverTimeoutException ex)
        {
            Debug.WriteLine(
                $"SendAttachments: Timeout: {ex.Message}");

            return false;
        }
        catch (StaleElementReferenceException ex)
        {
            Debug.WriteLine(
                $"SendAttachments: Stale element: {ex.Message}");

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"SendAttachments: Unexpected error: {ex}");

            return false;
        }
    }

    private bool EnsureGroupChatOpened()
    {
        if (_wait is null || _driver is null)
        {
            return false;
        }

        try
        {
            if (WhatsappServiceHelpers.FindMessageBox(_driver) is not null)
            {
                return true;
            }

            string[] existingHandles = _driver.WindowHandles.ToArray();

            return _wait.Until(driver =>
            {
                if (WhatsappServiceHelpers.HasLoginPrompt(driver)
                    || WhatsappServiceHelpers.HasInvalidChatState(driver))
                {
                    return false;
                }

                SwitchToNewestWindow(existingHandles);

                if (WhatsappServiceHelpers.FindMessageBox(driver) is not null)
                {
                    return true;
                }

                IWebElement actionButton =
                    WhatsappServiceHelpers.FindContinueToChatButton(driver);

                if (actionButton is not null)
                {
                    ClickElement(actionButton);
                    SwitchToNewestWindow(existingHandles);
                }

                return WhatsappServiceHelpers.FindMessageBox(driver) is not null;
            });
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
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
                if (WhatsappServiceHelpers.HasLoginPrompt(driver))
                {
                    return false;
                }

                try
                {
                    return driver.FindElements(By.Id("side")).Any()
                        || WhatsappServiceHelpers.FindMessageBox(driver) is not null;
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
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
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
                if (WhatsappServiceHelpers.HasLoginPrompt(driver)
                    || WhatsappServiceHelpers.HasInvalidChatState(driver))
                {
                    return null;
                }

                IWebElement messageBox = WhatsappServiceHelpers.FindMessageBox(driver);

                if (messageBox is null)
                {
                    return null;
                }

                return WhatsappServiceHelpers.IsChatComposer(messageBox)
                    ? messageBox
                    : null;
            });
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return null;
        }
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
        {
            CloseDriver();
            return null;
        }
        catch
        {
            return null;
        }
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
                if (WhatsappServiceHelpers.HasLoginPrompt(driver)
                    || WhatsappServiceHelpers.HasInvalidChatState(driver))
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
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool IsDriverAlive()
    {
        try
        {
            return _driver is not null && _driver.WindowHandles.Count > 0;
        }
        catch (NoSuchWindowException)
        {
            CloseDriver();
            return false;
        }
        catch (WebDriverException ex) when (WhatsappServiceHelpers.IsClosedWindowError(ex))
        {
            CloseDriver();
            return false;
        }
        catch
        {
            return false;
        }
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
}