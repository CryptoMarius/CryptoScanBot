using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

using System.Net;

namespace CryptoScanner.Core.Telegram.Commands;

/// <summary>zones [count]: the symbols closest to a zone, with a link to the trading app.</summary>
public class TelegramCommandZones : TelegramCommand
{
    public override string Name => "zones";
    public override TelegramCommandGroup Group => TelegramCommandGroup.Market;
    public override string Arguments => "[count]";
    public override string Description => "show the symbols closest to a zone (default 10, at most 50)";

    public override Task ExecuteAsync(TelegramCommandContext context)
    {
        var builder = context.Output;
        context.Html = true;

        int zoneCount = 10;
        if (context.Argument(0) != "" && !int.TryParse(context.Argument(0), out zoneCount))
            zoneCount = 10;
        zoneCount = Math.Clamp(zoneCount, 1, 50);

        builder.AppendLine($"Zones top {zoneCount}");

        var exchange = GlobalData.ActiveExchange ?? throw new InvalidOperationException("no active exchange");

        // A plain list sorted afterwards: the SortedList keyed on the distance threw as soon as two
        // symbols had exactly the same distance, and the command then answered nothing.
        List<(decimal distance, CryptoTradeSide side, CryptoSymbol symbol)> list = [];
        foreach (var symbol in exchange.SymbolListName.Values)
        {
            decimal? distance = ZoneTools.ZoneDistance(symbol);
            CryptoTradeSide? side = ZoneTools.ZoneTradeSide(symbol);
            if (distance != null && distance != 100 && side != null)
                list.Add((distance.Value, side.Value, symbol));
        }

        if (list.Count == 0)
        {
            builder.AppendLine("not calculated?");
            return Task.CompletedTask;
        }

        var interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        foreach (var zone in list.OrderBy(z => z.distance).ThenBy(z => z.symbol.Name).Take(zoneCount))
        {
            var symbol = zone.symbol;
            //string text = Settings.CryptoExternalUrlList.GetTradingAppName(GlobalData.Settings.General.TradingApp, symbol.Exchange.Name);
            (string Url, CryptoExternalUrlType Execute) = GlobalData.ExternalUrls.GetExternalRef(GlobalData.Settings.General.TradingApp, true, symbol, interval);
            // HTML parse mode refuses a bare & or < in the text, the url included
            if (Url == "")
                builder.Append(WebUtility.HtmlEncode(symbol.Name));
            else
                builder.Append($"<a href='{WebUtility.HtmlEncode(Url)}'>{WebUtility.HtmlEncode(symbol.Name)}</a>");

            builder.Append(' ');
            builder.Append($"{zone.distance:N2}");
            builder.Append(' ');

            if (zone.side == CryptoTradeSide.Long)
                builder.Append("\U0001f7e2");
            else
                builder.Append("\U0001F534");

            builder.AppendLine();
        }
        return Task.CompletedTask;
    }
}
