using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.SettingsDialog;

public partial class UserControlTelegram : UserControl
{
    public UserControlTelegram()
    {
        InitializeComponent();

        ButtonTestTelegram.Click += ButtonTestTelegram_Click;
        buttonTelegramStart.Click += ButtonTelegramStart_Click;
    }

    private async void ButtonTelegramStart_Click(object sender, EventArgs e)
    {
        await ThreadTelegramBot.Start(EditTelegramToken.Text, EditTelegramChatId.Text);
    }

    private void ButtonTestTelegram_Click(object sender, EventArgs e)
    {
        ThreadTelegramBot.ChatId = EditTelegramChatId.Text;
        GlobalData.AddTextToTelegram("Dit is een test bericht");
    }

    public void LoadConfig(SettingsBasic settings)
    {
        EditTelegramToken.Text = settings.Telegram.Token;
        EditTelegramChatId.Text = settings.Telegram.ChatId;
        EditUseEmojiInTrend.Checked = settings.Telegram.EmojiInTrend;
        EditSendSignalsToTelegram.Checked = settings.Telegram.SendSignalsToTelegram;
    }

    public void SaveConfig(SettingsBasic settings)
    {
        settings.Telegram.Token = EditTelegramToken.Text.Trim();
        settings.Telegram.ChatId = EditTelegramChatId.Text.Trim();
        settings.Telegram.EmojiInTrend = EditUseEmojiInTrend.Checked;
        settings.Telegram.SendSignalsToTelegram = EditSendSignalsToTelegram.Checked;
    }

}
