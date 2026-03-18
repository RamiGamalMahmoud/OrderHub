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
    private IWebDriver _driver;
    private WebDriverWait _wait;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IApplicationDirectoriesService _directories;

    public WhatsappService(IApplicationDirectoriesService directories)
    {
        _directories = directories;
    }

    public async Task<bool> StartWhatsAppAsync(string url = "https://web.whatsapp.com")
    {
        return await Task.Run(() =>
        {
            try
            {
                ChromeOptions options = new ChromeOptions();
                options.AddArgument(
                    $"--user-data-dir={_directories.DefaultWhatAppProfilePath}\\MainProfile"
                );
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-backgrounding-occluded-windows");
                options.AddArgument("--disable-renderer-backgrounding");
                options.AddArgument("--disable-background-timer-throttling");
                options.AddArgument("--remote-debugging-port=9222");


                _driver = new ChromeDriver(options);
                _driver.Navigate().GoToUrl(url);

                _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
                _wait.Until(d =>
                {
                    var boxes = d.FindElements(By.CssSelector("div[contenteditable='true']"));
                    return boxes.Count > 0;
                });

                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> SendAsync(string contact, string message)
    {
        if (!IsDriverAlive())
            return false;

        await _lock.WaitAsync();

        try
        {
            string cleanNumber = new string(contact.Where(char.IsDigit).ToArray());
            string url = $"https://web.whatsapp.com/send?phone={cleanNumber}";
            _driver.Navigate().GoToUrl(url);

            var messageBox = _wait.Until(driver =>
            {
                try
                {
                    var elements = driver.FindElements(By.XPath("//div[@contenteditable='true'][@data-tab='10']"));
                    return elements.Count > 0 ? elements[0] : null;
                }
                catch { return null; }
            });

            string[] lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                messageBox.SendKeys(lines[i]);
                if (i < lines.Length - 1)
                    messageBox.SendKeys(Keys.Shift + Keys.Enter);
            }

            messageBox.SendKeys(Keys.Enter);

            await Task.Delay(1000); // rate limit

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsDriverAlive()
    {
        try
        {
            return _driver != null && _driver.WindowHandles.Any();
        }
        catch
        {
            return false;
        }
    }

    public void Close()
    {
        try { _driver?.Quit(); } catch { }
    }
}