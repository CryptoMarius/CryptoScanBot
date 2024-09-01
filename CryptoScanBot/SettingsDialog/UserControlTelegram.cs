using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Telegram;

namespace CryptoScanBot.SettingsDialog;

public partial class UserControlTelegram : UserControl
{
    public UserControlTelegram()
    {
        InitializeComponent();

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
    }

    public void SaveConfig()
    {
        GlobalData.Telegram.Token = EditTelegramToken.Text.Trim();
        GlobalData.Telegram.ChatId = EditTelegramChatId.Text.Trim();
        GlobalData.Telegram.EmojiInTrend = EditUseEmojiInTrend.Checked;
        GlobalData.Telegram.SendSignalsToTelegram = EditSendSignalsToTelegram.Checked;
    }

}
