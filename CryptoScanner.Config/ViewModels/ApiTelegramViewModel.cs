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
    private bool _isTelegramRunning = false; // For Start/Stop button state

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
    }

    public void SaveConfig(SettingsTelegram settings)
    {
        settings.Token = Token.Trim();
        settings.ChatId = ChatId.Trim();
        settings.EmojiInTrend = EmojiInTrend;
        settings.SendSignalsToTelegram = SendSignalsToTelegram;
    }

    [RelayCommand]
    private async Task StartStopTelegram()
    {
        await ThreadTelegramBot.Start(Token, ChatId);
        IsTelegramRunning = !IsTelegramRunning;
    }

    [RelayCommand]
    private void TestTelegram()
    {
        ThreadTelegramBot.ChatId = ChatId;
        GlobalData.AddTextToTelegram("This is a test message");
    }
}
