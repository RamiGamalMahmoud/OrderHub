using Microsoft.Playwright;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Infrastructure.Services.WhatsApp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal sealed class PlaywrightWhatsAppService : IWhatsappService, IMessageSender
{
    private const string WHATSAPP_URL = "https://web.whatsapp.com/";
    //private const string WPP_SCRIPT_URL = "https://cdn.jsdelivr.net/npm/@wppconnect/wa-js@latest/dist/wppconnect-wa.js";

    private readonly IApplicationDirectoriesService _appDirectoriesService;
    private readonly IWppConnectScriptService _wppConnectScriptService;

    private IPlaywright _playwright;
    private IBrowserContext _context;
    private IPage _page;
    internal static readonly string[] _options =
        [
            "--disable-blink-features=AutomationControlled",
        ];

    public PlaywrightWhatsAppService(IApplicationDirectoriesService appDirectoriesService, IWppConnectScriptService wppConnectScriptService)
    {
        _appDirectoriesService = appDirectoriesService;
        _wppConnectScriptService = wppConnectScriptService;
    }

    public async Task<bool> StartWhatsAppAsync(string url = WHATSAPP_URL)
    {
        var chromePath = FindChrome();
        if (chromePath is null)
            return false;

        try
        {
            var sessionPath = Path.Combine(_appDirectoriesService.DefaultWhatsAppProfileDirectory, "WhatsApp");
            Directory.CreateDirectory(sessionPath);

            _playwright = await Playwright.CreateAsync();
            _context ??= await _playwright.Chromium.LaunchPersistentContextAsync(
                sessionPath,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = false,
                    ExecutablePath = chromePath,
                    Args = _options
                });

            _context.Close += (_, _) =>
            {
                _context = null;
                _page = null;
            };

            _page = _context.Pages.FirstOrDefault() ?? await _context.NewPageAsync();

            await InjectWppAsync();
            await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitForWhatsapReadyAsync(_page);
            await WaitForWppReadyAsync(_page);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WhatsApp Start Error: {ex.Message}");
            return false;
        }
    }



    private async Task<bool> SendMessageContentAsync(string chatId, MessageToSend message)
    {
        if (_page is null || message is null)
            return false;


        if (!string.IsNullOrWhiteSpace(message.Message))
        {
            if (!await SendTextMessageAsync(chatId, message.Message))
                return false;
        }

        if (message.Attachments is null || !message.Attachments.Any())
            return true;

        // 🚨 تعديل جوهري: انتظر قليلاً بعد إرسال النص قبل البدء في رفع المرفقات لمحاكاة السلوك البشري
        await Task.Delay(Random.Shared.Next(3000, 5000));

        foreach (var attachment in message.Attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment))
                continue;

            FileInfo file = new FileInfo(attachment);

            if (!await SendFileToChatAsync(chatId, file.FullName, file.Name))
                return false;

            // 🚨 تعديل جوهري: انتظر بين كل ملف وملف آخر لمنع قفل الجلسة (خاصة مع الملفات الكبيرة)
            await Task.Delay(Random.Shared.Next(4000, 7000));
        }

        return true;
    }


    public async Task<bool> SendAsync(string destination, MessageToSend message)
    {
        if (_page is null)
            return false;

        if (message is null)
            return false;

        var phoneNumber = CleanPhoneNumber(destination);

        var chatId = $"{phoneNumber}@c.us";

        return await SendMessageContentAsync(chatId, message);
    }

    public async Task<bool> SendToPhoneAsync(string destination, MessageToSend message) => await SendAsync(destination, message);

    public async Task<bool> SendToGroupAsync(string destination, MessageToSend message)
    {
        if (_page is null || message is null)
            return false;

        var groupId = await GetGroupIdFromInviteLinkAsync(destination, _page);

        if (string.IsNullOrWhiteSpace(groupId))
            return false;

        await WaitForWppReadyAsync(_page);

        return await SendMessageContentAsync(groupId, message);
    }

    private async Task<bool> SendTextMessageAsync(string chatId, string message)
    {
        if (_page is null)
            return false;

        return await _page.EvaluateAsync<bool>(
            WppScripts.SendTextMessage,
            new
            {
                chatId,
                message
            });
    }

    private static async Task<string> GetGroupIdFromInviteLinkAsync(string inviteLink, IPage page)
    {
        if (page is null)
            return null;

        try
        {
            var inviteCode = ExtractInviteCode(inviteLink);

            if (string.IsNullOrWhiteSpace(inviteCode))
                return null;

            await WaitForWppReadyAsync(page);

            var groupId = await page.EvaluateAsync<string>(
                    WppScripts.GetGroutIdFromIntitationCode,
                    inviteCode);

            if (string.IsNullOrWhiteSpace(groupId))
                return null;

            return groupId;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task InjectWppAsync()
    {
        if (_context is null)
        {
            throw new InvalidOperationException("Browser context is not initialized.");
        }

        string wppScript = await _wppConnectScriptService.PrepareAsync();

        //string scriptDirectoryPath = Path.Combine(Path.GetTempPath(), "OrderHub_WppConnect");
        //Directory.CreateDirectory(scriptDirectoryPath);
        //string scriptFilePath = Path.Combine(scriptDirectoryPath, "wppconnect-wa.js");

        //string wppScript = string.Empty;

        //if (File.Exists(scriptFilePath) && new FileInfo(scriptFilePath).Length > 0)
        //{
        //    wppScript = await File.ReadAllTextAsync(scriptFilePath);
        //}
        //else
        //{
        //    using var httpClient = new HttpClient();

        //    httpClient.Timeout = TimeSpan.FromSeconds(30);

        //    wppScript = await httpClient.GetStringAsync(WPP_SCRIPT_URL);

        //    if (!string.IsNullOrWhiteSpace(wppScript))
        //    {
        //        await File.WriteAllTextAsync(scriptFilePath, wppScript);
        //    }
        //    else
        //    {
        //        throw new Exception("The downloaded WPP script is empty.");
        //    }
        //}

        await _context.AddInitScriptAsync(wppScript);
    }

    private static async Task WaitForWhatsapReadyAsync(IPage page)
    {
        if (page is null)
            throw new InvalidOperationException("WhatsApp page is not initialized.");

        await page.WaitForFunctionAsync(
            WppScripts.WaitForWhatsapReadyScript,
            new PageWaitForFunctionOptions
            {
                Timeout = 120000
            });
    }

    private static async Task WaitForWppReadyAsync(IPage page)
    {
        if (page is null)
            throw new InvalidOperationException("WhatsApp page is not initialized.");

        await page.WaitForFunctionAsync(
            """
            () => {
                return window.WPP && window.WPP.isFullReady === true;
            }
            """,
            new PageWaitForFunctionOptions
            {
                Timeout = 60000
            });
    }

    private static string ExtractInviteCode(string inviteLink)
    {
        if (string.IsNullOrWhiteSpace(inviteLink))
            return string.Empty;

        return inviteLink
            .Trim()
            .TrimEnd('/')
            .Split('/')
            .Last();
    }

    private static string CleanPhoneNumber(string phoneNumber)
    {
        return phoneNumber
            .Replace("+", "")
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "");
    }

    private static string GetMimeType(string filePath)
    {
        return Path.GetExtension(filePath)
            .ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",

            ".pdf" => "application/pdf",

            ".doc" => "application/msword",

            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            ".xls" => "application/vnd.ms-excel",

            ".xlsx" =>
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

            ".txt" => "text/plain",

            ".zip" => "application/zip",

            _ => "application/octet-stream"
        };
    }

    public void Stop()
    {
        try
        {
            _context?
                .CloseAsync()
                .GetAwaiter()
                .GetResult();

            _playwright?.Dispose();

            _context = null;
            _page = null;
            _playwright = null;

        }
        catch (Exception)
        {
        }
    }

    private static string FindChrome()
    {
        var paths = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),

            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe"),

            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome",
                "Application",
                "chrome.exe")
        };

        return paths.FirstOrDefault(File.Exists);
    }

    private async Task<bool> SendFileToChatAsync(string chatId, string filePath, string caption = null)
    {
        if (_page is null)
            return false;

        if (!File.Exists(filePath))
            return false;

        var bytes = await File.ReadAllBytesAsync(filePath);
        var mimeType = GetMimeType(filePath);
        var mediaType = GetWppMediaType(filePath);
        var base64 = Convert.ToBase64String(bytes);
        var dataUri = $"data:{mimeType};base64,{base64}";
        var fileName = Path.GetFileName(filePath);

        await WaitForWppReadyAsync(_page);

        return await _page.EvaluateAsync<bool>(
            WppScripts.SendFileMessage,
            new
            {
                chatId,
                dataUri,
                caption,
                mediaType,
                fileName
            });
    }

    private static string GetWppMediaType(string filePath)
    {
        return Path.GetExtension(filePath)
            .ToLowerInvariant() switch
        {
            ".png" or
            ".jpg" or
            ".jpeg" or
            ".webp" or
            ".gif" => "image",

            ".mp4" or
            ".avi" or
            ".mov" or
            ".mkv" => "video",

            ".mp3" or
            ".wav" or
            ".ogg" or
            ".m4a" => "audio",

            _ => "document"
        };
    }

}