using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OrderHub.Application.Interfaces.Services;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class WhatsappService : IWhatsappService
{
    private IWebDriver _driver;
    private WebDriverWait _wait;
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;

    public WhatsappService(IApplicationDirectoriesService applicationDirectoriesService)
    {
        _applicationDirectoriesService = applicationDirectoriesService;
    }

    public async Task<bool> StartWhatsApp(string url = "https://web.whatsapp.com")
    {
        return await Task.Run(() =>
        {
            // Main application driver
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--disable-background-timer-throttling");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--remote-debugging-port=9222");
            options.AddArgument($"--user-data-dir={_applicationDirectoriesService.DefaultWhatAppProfilePath}\\MainProfile");

            _driver = new ChromeDriver(options);

            _driver.Navigate().GoToUrl(url);

            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));


            try
            {
                _wait.Until(driver =>
                {
                    try
                    {
                        ReadOnlyCollection<IWebElement> elements = driver.FindElements(By.XPath("//div[@contenteditable='true']"));
                        return elements.Count > 0 ? elements[0] : null;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

    public async Task<bool> Send(string contact, string message)
    {
        return IsDriverAlive() &&
        await Task.Run<bool>(() =>
        {
            try
            {
                string cleanNumber = new string(contact.Where(char.IsDigit).ToArray());

                string url = $"https://web.whatsapp.com/send?phone={cleanNumber}";
                _driver.Navigate().GoToUrl(url);

                HandleChromeAlert();
                var messageBox = _wait.Until(driver =>
                {
                    try
                    {
                        var elements = driver.FindElements(By.XPath("//div[@contenteditable='true'][@data-tab='10']"));
                        return elements.Count > 0 ? elements[0] : null;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                });


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

                System.Threading.Thread.Sleep(1000);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });

    }

    private void HandleChromeAlert()
    {
        try
        {
            WebDriverWait alertWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
            alertWait.Until(ExpectedConditions.AlertIsPresent());
            _driver.SwitchTo().Alert().Dismiss();
        }
        catch { }
    }

    private bool IsDriverAlive()
    {
        try
        {
            var handles = _driver?.WindowHandles;
            return handles != null && handles.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public void Close()
    {
        _driver?.Quit();
    }
}
