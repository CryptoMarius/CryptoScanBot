using System.Text;

namespace CryptoScanBot.Core.Telegram;

public class TelegramShowHelp
{
    public static void ShowHelp(StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("status        show status bots");

        stringBuilder.AppendLine("start         start trade bot");
        stringBuilder.AppendLine("stop          stop trade bot");
        stringBuilder.AppendLine("positions     show positions trade bot");
        stringBuilder.AppendLine("profits       show profits trade bot (today)");

        stringBuilder.AppendLine("signalstart   start signal bot");
        stringBuilder.AppendLine("signalstop    stop signal bot");
        stringBuilder.AppendLine("reset         reset the scanner (restart)");
        stringBuilder.AppendLine("calculatezones calculate the zones");

        stringBuilder.AppendLine("value         show value BTC,BNB and ETH"); // todo, de juiste basismunten tonen
        stringBuilder.AppendLine("barometer     show barometer BTC/ETH/USDT"); // todo, de juiste basismunten tonen
        stringBuilder.AppendLine("assets        show asset overview");
        stringBuilder.AppendLine("chatid        ChatId configuratie");
        stringBuilder.AppendLine("help          this help screen");
    }

}
