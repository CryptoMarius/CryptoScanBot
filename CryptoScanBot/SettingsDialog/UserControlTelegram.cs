using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Telegram;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTelegram : UserControl
{
    public UserControlTelegram()
    {
        InitializeComponent();

        EditTelegramToken.TextChanged += EditTokenChanged;
        EditTelegramChatId.TextChanged += EditChatIdChanged;
        
        ButtonTestTelegram.Click += ButtonTestTelegram_Click;
        buttonTelegramStart.Click += ButtonTelegramStart_Click;
    }

    private async void ButtonTelegramStart_Click(object? sender, EventArgs? e)
    {
        await ThreadTelegramBot.Start(EditTelegramToken.Text, EditTelegramChatId.Text);
    }

    private void ButtonTestTelegram_Click(object? sender, EventArgs? e)
    {
        ThreadTelegramBot.ChatId = EditTelegramChatId.Text;
        GlobalData.AddTextToTelegram("Dit is een test bericht");
    }

    public void LoadConfig()
    {
        EditTelegramToken.Text = GlobalData.Telegram.Token;
        EditTelegramChatId.Text = GlobalData.Telegram.ChatId;
        EditUseEmojiInTrend.Checked = GlobalData.Telegram.EmojiInTrend;
        EditSendSignalsToTelegram.Checked = GlobalData.Telegram.SendSignalsToTelegram;
    
        EditTokenChanged(null, EventArgs.Empty);
        EditChatIdChanged(null, EventArgs.Empty);
    }

    public void SaveConfig()
    {
        GlobalData.Telegram.Token = EditTelegramToken.Text.Trim();
        GlobalData.Telegram.ChatId = EditTelegramChatId.Text.Trim();
        GlobalData.Telegram.EmojiInTrend = EditUseEmojiInTrend.Checked;
        GlobalData.Telegram.SendSignalsToTelegram = EditSendSignalsToTelegram.Checked;
    }

    private static string GetDisplayApiKey(string text)
    {
        return text.Length < 4 ? "" : $"{text[..3]}.. {text[^3..]}";
    }

    private void EditTokenChanged(object? sender, EventArgs e)
    {
        LabelTelegramToken.Text = GetDisplayApiKey(EditTelegramToken.Text);
    }

    private void EditChatIdChanged(object? sender, EventArgs e)
    {
        LabelTelegramChatId.Text = GetDisplayApiKey(EditTelegramChatId.Text);
    }

}
