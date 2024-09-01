using System.Text;

namespace CryptoScanBot.Core.Telegram;
public class TelegramShowHelp
{
    public static void ShowHelp(StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("status        show status bots");

#if TRADEBOT
        stringBuilder.AppendLine("start         start trade bot");
        stringBuilder.AppendLine("stop          stop trade bot");
        stringBuilder.AppendLine("positions     show positions trade bot");
        stringBuilder.AppendLine("profits       show profits trade bot (today)");
#endif
#if BALANCING
        stringBuilder.AppendLine("balancestart  start balancing bot");
        stringBuilder.AppendLine("balancestop   stop balancing bot");
        stringBuilder.AppendLine("advicestart   start advice balancing bot");
        stringBuilder.AppendLine("advicestop    stop advice balancing bot");
        stringBuilder.AppendLine("balance       show balance overview");
#endif

        stringBuilder.AppendLine("signalstart   start signal bot");
        stringBuilder.AppendLine("signalstop    stop signal bot");

        stringBuilder.AppendLine("value         show value BTC,BNB and ETH"); // todo, de juiste basismunten tonen
        stringBuilder.AppendLine("barometer     show barometer BTC/ETH/USDT"); // todo, de juiste basismunten tonen
#if TRADEBOT
        stringBuilder.AppendLine("assets        show asset overview");
#endif
        stringBuilder.AppendLine("chatid        ChatId configuratie");
        stringBuilder.AppendLine("help          this help screen");
    }



}
