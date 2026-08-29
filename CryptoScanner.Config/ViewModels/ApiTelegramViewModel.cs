using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Telegram;

namespace CryptoScanner.Config.ViewModels;

public partial class ApiTelegramViewModel : ObservableObject
{
    [ObservableProperty]
    private string _token = ""; // string (EXACT match)

    [ObservableProperty]
    private string _chatId = ""; // string (EXACT match)

    [ObservableProperty]
    private bool _emojiInTrend = true; // bool (EXACT match)

    [ObservableProperty]
    private bool _sendSignalsToTelegram = false; // bool (EXACT match)

    [ObservableProperty]
    private string _tokenDisplay = ""; // Display version

    [ObservableProperty]
    private string _chatIdDisplay = ""; // Display version

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartStopCaption))]
    private bool _isTelegramRunning = false; // For Start/Stop button state

    /// <summary>
    /// Caption of the Start/Stop button, so the screen shows whether the bot is running.
    /// </summary>
    public string StartStopCaption => IsTelegramRunning ? "Stop" : "Start";

    partial void OnTokenChanged(string value)
    {
        TokenDisplay = GetDisplayApiKey(value);
    }

    partial void OnChatIdChanged(string value)
    {
        ChatIdDisplay = GetDisplayApiKey(value);
    }

    private static string GetDisplayApiKey(string text)
    {
        return text.Length < 4 ? "" : $"{text[..3]}.. {text[^3..]}";
    }

    public void LoadConfig(SettingsTelegram settings)
    {
        Token = settings.Token;
        ChatId = settings.ChatId;
        EmojiInTrend = settings.EmojiInTrend;
        SendSignalsToTelegram = settings.SendSignalsToTelegram;
        IsTelegramRunning = ThreadTelegramBot.IsRunning;
    }

    public void SaveConfig(SettingsTelegram settings)
    {
        settings.Token = Token.Trim();
        settings.ChatId = ChatId.Trim();
        settings.EmojiInTrend = EmojiInTrend;
        settings.SendSignalsToTelegram = SendSignalsToTelegram;
    }

    /// <summary>
    /// Stop the bot, or start it with the token that is typed in the screen at this moment. That is
    /// the point of the button: trying out a changed token without restarting the scanner. The
    /// settings themselves are only written by SaveConfig.
    /// </summary>
    [RelayCommand]
    private async Task StartStopTelegram()
    {
        if (ThreadTelegramBot.IsRunning)
        {
            await ThreadTelegramBot.StopAsync();
            GlobalData.AddTextToLogTab("Telegram bot stopped");
        }
        else
        {
            if (await ThreadTelegramBot.Start(Token.Trim(), ChatId.Trim()))
                GlobalData.AddTextToLogTab("Telegram bot started");
            // A refused token is reported by Start itself, on the same log tab
        }
        IsTelegramRunning = ThreadTelegramBot.IsRunning;
    }

    [RelayCommand]
    private void TestTelegram()
    {
        ThreadTelegramBot.ChatId = ChatId.Trim();
        if (!ThreadTelegramBot.IsRunning)
        {
            // Without this the button did nothing at all and said nothing either
            GlobalData.AddTextToLogTab("Telegram bot is not running, press Start first");
            return;
        }
        GlobalData.AddTextToTelegram("This is a test message");
    }
}
