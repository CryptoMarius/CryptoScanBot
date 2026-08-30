using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Settings;

[Serializable]
public class SettingsTelegram
{
    [JsonConverter(typeof(SecureStringConverter))]
    public string Token { get; set; } = "";
    [JsonConverter(typeof(SecureStringConverter))]
    public string ChatId { get; set; } = "";

    /// <summary>
    /// The master switch. Off means the bot is not started at all: nothing is sent, and the commands
    /// typed into the chat (STATUS, ZONES, ..) are not answered either. The Start button of the
    /// settings screen still starts it by hand, so the connection can be tried out.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public bool EmojiInTrend { get; set; } = true;
    public bool SendSignalsToTelegram { get; set; } = false;

    /// <summary>
    /// Orders the scanner places, changes or cancels, including the ones that fail.
    /// </summary>
    public bool SendOrdersToTelegram { get; set; } = true;

    /// <summary>
    /// Orders the exchange reports as filled, and positions the user took over.
    /// </summary>
    public bool SendFilledOrdersToTelegram { get; set; } = true;

    /// <summary>
    /// Ready after loading, a restart of the streams and the pause rules.
    /// </summary>
    public bool SendSystemMessagesToTelegram { get; set; } = true;


    /// <summary>
    /// Whether a message of this category may go to Telegram. These three default to true because
    /// they were sent unconditionally before they had a checkbox - an existing settings file has no
    /// value for them and should keep behaving the way it did.
    /// </summary>
    public bool IsAllowed(CryptoTelegramCategory category)
    {
        // The test button has to work whatever is switched off, that is the point of it
        if (category == CryptoTelegramCategory.Test)
            return true;
        if (!Enabled)
            return false;

        return category switch
        {
            CryptoTelegramCategory.Signal => SendSignalsToTelegram,
            CryptoTelegramCategory.OrderPlaced => SendOrdersToTelegram,
            CryptoTelegramCategory.OrderFilled => SendFilledOrdersToTelegram,
            CryptoTelegramCategory.System => SendSystemMessagesToTelegram,
            _ => false,
        };
    }
}
