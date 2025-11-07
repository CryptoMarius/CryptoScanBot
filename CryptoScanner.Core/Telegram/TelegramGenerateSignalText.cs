using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Text;

namespace CryptoScanner.Core.Telegram;

internal class TelegramGenerateSignalText
{

    private static string GetEmoiFromMarketTrend(float trend)
    {
        // https://beta.emojipedia.org/police-car-light

        if (GlobalData.Telegram.EmojiInTrend)
        {
            // Circles
            if (trend >= 0)
                return "\U00002B06"; // Arrow up
            else
            if (trend < 0)
                return "\U00002B07"; // Arrown down
            else
                return "\U00002753"; // questionmark
        }
        else
        {
            if (trend >= 0)
                return "bullish";
            else if (trend < 0)
                return "bearish";
        }
        return "sideways";
    }

    private static string GetEmoiFromTrend(CryptoTrendIndicator? trend)
    {
        // https://beta.emojipedia.org/police-car-light

        if (GlobalData.Telegram.EmojiInTrend)
        {
            // Circles
            return trend switch
            {
                CryptoTrendIndicator.Bullish => "\U0001f7e2",
                CryptoTrendIndicator.Bearish => "\U0001F534",
                _ => "\U000026AB",
            };
        }
        else
        {
            return trend switch
            {
                CryptoTrendIndicator.Bullish => "up@",
                CryptoTrendIndicator.Bearish => "down@",
                _ => "?",
            };
        }


        // Arrows
        //return trend switch
        //{
        //    CryptoTrendIndicator.Bullish => "\U00002B06",
        //    CryptoTrendIndicator.Bearish => "\U00002B07",
        //    _ => "\U00002753", // questionmark
        //};
    }



    public static string Execute(CryptoSignal signal)
    {
        if (ThreadTelegramBot.ChatId == "")
            return string.Empty;

        try
        {
            StringBuilder builder = new();
            builder.Append(signal.Symbol.Name + " " + signal.Interval.Name + " ");
            builder.Append(signal.OpenDate.ToLocalTime().ToString("dd MMM HH:mm"));
            builder.Append(" " + signal.StrategyText + "");
            //builder.Append(" " + signal.SideText + " ");

            // https://apps.timwhitlock.info/emoji/tables/unicode
            if (GlobalData.Telegram.EmojiInTrend)
            {
                if (signal.Side == CryptoTradeSide.Long)
                    builder.Append($"\U0001f7e2 {signal.SideText}");
                else
                    builder.Append($"\U0001F534 {signal.SideText}");
            }
            else
            {
                if (signal.Side == CryptoTradeSide.Long)
                    builder.Append($" {signal.SideText}");
                else
                    builder.Append($" {signal.SideText}");
            }

            string text = Settings.CryptoExternalUrlList.GetTradingAppName(GlobalData.Settings.General.TradingApp, "").Trim();
            (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, true, signal.Symbol, signal.Interval);
            if (Url != "")
                builder.Append($" <a href='{Url}'>{text}</a>");
            builder.AppendLine();

            builder.Append("Candle: open " + signal.Candle?.Open.ToString0());
            builder.Append(" high " + signal.Candle?.High.ToString0());
            builder.Append(" low " + signal.Candle?.Low.ToString0());
            builder.Append(" close " + signal.Candle?.Close.ToString0());
            builder.AppendLine();

            builder.Append("Volume 24h: " + signal.Symbol.Volume.ToString("N0"));
            if (signal.CandlesWithZeroVolume > 0)
                builder.Append(", candles with volume " + signal.CandlesWithZeroVolume.ToString());
            builder.AppendLine();


            // De trend informatie
            // Even in de juiste volgorde toevoegen (je verwacht een vaste volgorde)
            SortedList<CryptoIntervalPeriod, (string, CryptoTrendIndicator?)> a = [];
            a.TryAdd(signal.Interval.IntervalPeriod, (signal.Interval.Name, signal.TrendInterval));
            a.TryAdd(CryptoIntervalPeriod.interval15m, ("15m", signal.Trend15m));
            a.TryAdd(CryptoIntervalPeriod.interval30m, ("30m", signal.Trend30m));
            a.TryAdd(CryptoIntervalPeriod.interval1h, ("1h", signal.Trend1h));
            a.TryAdd(CryptoIntervalPeriod.interval4h, ("4h", signal.Trend4h));
            a.TryAdd(CryptoIntervalPeriod.interval12h, ("1d", signal.Trend1d));

            builder.Append("Trend: ");
            builder.Append(GetEmoiFromMarketTrend(signal.TrendPercentagePrimary));
            builder.Append(' ');
            builder.Append(signal.TrendPercentagePrimary.ToString("N2") + "%");

            foreach (KeyValuePair<CryptoIntervalPeriod, (string name, CryptoTrendIndicator? trendIndicator)> entry in a)
            {
                builder.Append(' ');
                builder.Append(GetEmoiFromTrend(entry.Value.trendIndicator));
                builder.Append(entry.Value.name);
            }
            builder.AppendLine();


            // De barometer informatie
            SortedList<CryptoIntervalPeriod, string> b = [];
            b.TryAdd(CryptoIntervalPeriod.interval1h, "1h");
            b.TryAdd(CryptoIntervalPeriod.interval4h, "4h");
            b.TryAdd(CryptoIntervalPeriod.interval1d, "1d");

            builder.Append("Barometer: ");
            foreach (KeyValuePair<CryptoIntervalPeriod, string> entry in b)
            {
                CryptoBarometerData? barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(signal.Symbol.QuoteData.Name, entry.Key);
                builder.Append($" {entry.Value} {barometerData.PriceBarometer?.ToString("N2")}");
            }
            builder.AppendLine();


            builder.Append("Stoch: " + signal.StochOscillator?.ToString("N2"));
            builder.Append(" Signal " + signal.StochSignal?.ToString("N2"));
            builder.Append(" RSI " + signal.Rsi?.ToString("N2"));
            builder.AppendLine();

            builder.Append("BB: " + signal.BollingerBandsPercentage?.ToString("N2") + "%");
            builder.Append(" low " + signal.BollingerBandsLowerBand?.ToString("N6"));
            builder.Append(" high " + signal.BollingerBandsUpperBand?.ToString("N6"));
            builder.AppendLine();


            //builder.Append("<b>Google</b>");
            //builder.Append("<a color:red;>Google</a>");
            //builder.Append("<span color:red;>Google</span>");

            //bot.send_message(chat_id = update.message.chat_id, text = "<a href='https://www.google.com/'>Google</a>", parse_mode = ParseMode.HTML)
            //bot.send_message(chat_id = update.message.chat_id, text = "<b>Bold font</b>", parse_mode = ParseMode.HTML)

            //var DisableLink = new LinkPreviewOptions { IsDisabled = true };
            //await bot.SendMessage(ThreadTelegramBot.ChatId, builder.ToString(), parseMode: ParseMode.Html, linkPreviewOptions: DisableLink);
            //await bot.SendTextMessageAsync(ThreadTelegramBot.ChatId, builder.ToString(), parseMode: ParseMode.Html, disableWebPagePreview: true);
            //return Task.FromResult((StringBuilder?)builder);
            return builder.ToString();
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("\r\n" + "\r\n" + " error telegram thread(1)\r\n" + error.ToString());
        }

        return string.Empty;
    }

}
