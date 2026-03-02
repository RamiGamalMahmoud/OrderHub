using OrderHub.Application.Interfaces.Services;
using orderu.UI.Helpers;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OrderHub.UI.Services;

internal class WhatsappService : IWhatsappService
{
    public async Task Send(string contact, string message)
    {
        SwitchEnglishInput();
        RunWhatapp();
        await Wait(2000);

        await SpotSearch();

        await Clear();

        await InsertText(contact);

        await Accept();

        await Clear();
        await InsertText(message);
        await Accept();

        //await Task.Delay(3000);
        //KeyboardSimulator.SendFind();

        //KeyboardSimulator.SelectAll();
        //await Wait(100);

        //KeyboardSimulator.PressKey(WindowsInput.Native.VirtualKeyCode.BACK);
        //await Wait(100);

        //KeyboardSimulator.PressKey(WindowsInput.Native.VirtualKeyCode.ESCAPE);
    }

    private void RunWhatapp()
    {
        Process process = new Process();
        process.StartInfo.FileName = "whatsapp://";
        process.StartInfo.UseShellExecute = true;
        process.Start();
    }
    private async Task Wait(int milliseconds) => await Task.Delay(milliseconds);

    private async Task SpotSearch()
    {
        KeyboardSimulator.SendFind();
        await Wait(200);
    }

    private async Task Clear()
    {
        KeyboardSimulator.SelectAll();
        await Wait(100);
        KeyboardSimulator.PressKey(WindowsInput.Native.VirtualKeyCode.BACK);
        await Wait(100);
    }

    private async Task Accept()
    {
        KeyboardSimulator.PressEnter();
        await Wait(100);
    }

    private void Copy(string text) => Clipboard.SetText(text);

    private async Task InsertText(string text)
    {
        KeyboardSimulator.TypeText(text);
        await Wait(100);
    }

    private void CloseChat() => KeyboardSimulator.PressKey(WindowsInput.Native.VirtualKeyCode.ESCAPE);

    private void SwitchEnglishInput()
    {
        CultureInfo culture = InputLanguageManager.Current.CurrentInputLanguage;
        InputLanguageManager.Current.CurrentInputLanguage = new System.Globalization.CultureInfo("en-US");
    }
}
