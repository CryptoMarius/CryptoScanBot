using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Helpers;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using System.Text.Json.Nodes;

namespace CryptoScanner.Core.Exchange.Altrady;

/// <summary>
/// Hands a signal to the Altrady desktop application as a prefilled trade setup: the trade form opens
/// with the entry, the DCA's, the take profits and the stop filled in, and the user confirms it there
/// ("Import", then "Place order"). Nothing is placed without that confirmation.
/// <para>
/// The setup travels as the deep link parameter <c>trade_setup_data</c> on the trading route
/// (help.altrady.com, Technical integration: "Deep links" and "Trade setup format v2 for partners"),
/// URL-encoded v2 JSON, over the same local WebSocket as <see cref="AltradyDeepLink"/>. Tested on
/// 13-10-2026 with Altrady 5.5.0, long and short.
/// </para>
/// <para>
/// The content follows the open signal of <see cref="AltradyWebhook"/> rule for rule, so a setup
/// confirmed by hand is the same plan the webhook would have sent: the DCA levels and their factors,
/// DCA's beyond a signal stop left out, the stop behind the deepest DCA, the take profits as
/// percentages (Altrady anchors them itself, the same as for the webhook), the trailing last take
/// profit, the profit lock and the entry expiration. Differences forced by the format:
/// </para>
/// <list type="bullet">
/// <item>There is no amount. Altrady sizes the trade itself (the form, or a preset); the factors of
/// the entry and the DCA's become the split of the position over the ladder.</item>
/// <item>The DCA's are a price ladder, not percentages, so their prices are calculated here.</item>
/// <item>A market entry with DCA's is a ladder too, its first order a limit at the current price.</item>
/// </list>
/// </summary>
public static class AltradyTradeSetup
{
    /// <summary>
    /// The time Altrady gets to switch its tab to the market before the setup follows. Two seconds
    /// worked on 13-10-2026 (Okx tab open, HyperLiquid setup).
    /// </summary>
    private static readonly TimeSpan MarketSwitchDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Build the setup for the signal and hand it to Altrady. Runs on a worker thread; the outcome
    /// (and the reason when nothing was sent) goes to the log tab.
    /// </summary>
    public static void Send(CryptoSignal signal)
    {
        _ = Task.Run(async () =>
        {
            string caption = $"{signal.Symbol.Name} {signal.Interval.Name} {signal.SideText} {signal.StrategyText}";
            try
            {
                string? route = BuildRoute(signal, DateTime.UtcNow, out string reason);
                if (route == null)
                {
                    GlobalData.AddErrorToLogTab($"{caption} Altrady trade setup not sent: {reason}");
                    return;
                }

                GlobalData.AddTextToLogTab($"{caption} Altrady trade setup {route}");

                // Two steps. With a setup attached Altrady ignores the market of the route and imports
                // into the tab that happens to be open, so an Okx tab got a Binance or HyperLiquid setup
                // (tested 13-10-2026). Opening the market first switches the tab, and the setup that
                // follows then lands on the account of that market.
                string marketRoute = route[..route.IndexOf("trade_setup_data=", StringComparison.Ordinal)].TrimEnd('&', '?');
                if (await AltradyDeepLink.TrySendAsync(marketRoute))
                {
                    await Task.Delay(MarketSwitchDelay);
                    if (await AltradyDeepLink.TrySendAsync(route))
                    {
                        GlobalData.AddTextToLogTab($"{caption} Altrady trade setup handed to Altrady, confirm it there");
                        return;
                    }
                }

                // No desktop application listening: the web version reads the same route
                string url = "https://app.altrady.com/dashboard#/o/" + route[2..];
                GlobalData.AddTextToLogTab($"{caption} Altrady did not take the trade setup over the local WebSocket, opening it in the system browser instead");
                ExternalLinkHelper.OpenSystemBrowser(url);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddErrorToLogTab($"{caption} Altrady trade setup error: {error.Message}");
            }
        });
    }


    /// <summary>
    /// The trading route of the signal's market with the setup attached, for example
    /// <c>#/trade/BINA_USDT_BTC?resolution=60&amp;trade_setup_data=...</c>. The market comes from the
    /// same Altrady link as "Open trading app", so it follows the exchange the links are set to.
    /// Null, with the reason, when there is no Altrady link or no setup.
    /// </summary>
    public static string? BuildRoute(CryptoSignal signal, DateTime utcNow, out string reason)
    {
        (string url, _) = GlobalData.ExternalUrls.GetExternalRef(CryptoTradingApp.Altrady, false, signal.Symbol, signal.Interval);
        string? route = AltradyDeepLink.RouteOf(url);
        if (route == null || !route.StartsWith("#/trade/", StringComparison.Ordinal))
        {
            reason = $"no Altrady market link for {signal.Symbol.Name} (link: '{url}')";
            return null;
        }

        JsonObject? setup = BuildSetup(signal, utcNow, out reason);
        if (setup == null)
            return null;

        string data = Uri.EscapeDataString(setup.ToJsonString());
        return route + (route.Contains('?') ? "&" : "?") + "trade_setup_data=" + data;
    }


    /// <summary>
    /// The v2 setup for a signal, built from the trading settings the same way the webhook builds its
    /// open signal. Null, with the reason, when the symbol has no price yet.
    /// </summary>
    public static JsonObject? BuildSetup(CryptoSignal signal, DateTime utcNow, out string reason)
    {
        reason = "";
        CryptoSymbol symbol = signal.Symbol;
        SettingsTrading trading = GlobalData.Settings.Trading;
        CryptoTradeSide side = signal.Side;

        if (!symbol.LastPrice.HasValue || symbol.LastPrice.Value <= 0)
        {
            reason = "the symbol has no price yet";
            return null;
        }
        decimal lastPrice = symbol.LastPrice.Value;

        // The entry price the trader would take: a strategy that supplied its own entry level enters
        // there, every other signal at the market price (PositionMonitor, opening a position)
        decimal anchor = signal.EntryPriceOverridden && signal.SignalPrice > 0 ? signal.SignalPrice : lastPrice;
        bool limitEntry = trading.EntryOrderType == CryptoOrderType.Limit;
        decimal entryPrice = limitEntry
            ? LimitEntryPrice(side, anchor, trading.EntryLimitOffsetPercentage, lastPrice, symbol.PriceTickSize)
            : lastPrice;
        entryPrice = ClampPrice(symbol, side, entryPrice);

        // DCA levels, without the ones beyond a stop the strategy set itself (the webhook skips those
        // as well: the stop triggers before they could fill)
        List<CryptoDcaEntry> dcaList = trading.DcaList
            .Where(dca => !(signal.SlPercentage.HasValue && dca.Percentage >= signal.SlPercentage.Value))
            .ToList();

        JsonObject setup = new()
        {
            ["version"] = 2,
            ["direction"] = side == CryptoTradeSide.Long ? "long" : "short",
            // Without a creation time Altrady warns "Creation time unknown" in its import dialog
            ["time"] = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            ["entry"] = BuildEntry(symbol, side, entryPrice, limitEntry, dcaList),
        };

        // Take profits: a per-signal take profit collapses to one level, the same as EffectiveTpList
        List<CryptoTpEntry> tpList = signal.TpPercentage is decimal tpPercentage && tpPercentage > 0
            ? [new CryptoTpEntry { Factor = 100m, Percentage = tpPercentage }]
            : trading.TpList;
        if (tpList.Count > 0)
            setup["takeProfit"] = BuildTakeProfit(tpList, trading.TakeProfitTrailPercentage);

        // Stop: the strategy's own distance, otherwise the deepest DCA plus the stop percentage
        decimal? stopPercentage = null;
        if (signal.SlPercentage is decimal slPercentage)
            stopPercentage = slPercentage;
        else if (trading.StopLossPercentage > 0)
            stopPercentage = (dcaList.Count > 0 ? dcaList.Max(dca => dca.Percentage) : 0m) + trading.StopLossPercentage;
        if (stopPercentage.HasValue)
        {
            setup["stopLoss"] = BuildStopLoss(stopPercentage.Value, trading.MoveSlToBreakEven,
                trading.MoveSlToBreakEvenMethod, trading.MoveSlToBreakEvenPercentage,
                trading.MoveSlToBreakEvenTrailPercentage, tpList.Count);
        }

        // Entry expiration: after the entry remove time, or when the price reaches the first take
        // profit without the entry having filled (the webhook sends the same two conditions)
        JsonObject expiration = [];
        if (trading.EntryRemoveTime > 0)
        {
            DateTime expiresAt = utcNow.AddMinutes(trading.EntryRemoveTime * (int)signal.Interval.Duration);
            expiration["expiresAt"] = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds();
        }
        if (tpList.Count > 0)
            //expiration["price"] = ClampPrice(symbol, side, PricePlacement.Favorable(side, entryPrice, tpList[0].Percentage));
            expiration["price"] = ClampPrice(symbol, side, PricePlacement.TakeProfit(side, entryPrice, tpList[0].Percentage));
        if (expiration.Count > 0)
            setup["entryExpiration"] = expiration;

        return setup;
    }


    /// <summary>
    /// A limit entry the way the trader places it: the offset away from the anchor, and never on or
    /// through the market, so it does not fill at once (PositionMonitor.CorrectBuyOrDcaPrice).
    /// </summary>
    internal static decimal LimitEntryPrice(CryptoTradeSide side, decimal anchor, decimal offsetPercentage, decimal lastPrice, decimal tickSize)
    {
        decimal price = offsetPercentage > 0 && offsetPercentage < 100
            ? PricePlacement.Adverse(side, anchor, offsetPercentage)
            : anchor;
        if (side == CryptoTradeSide.Long)
        {
            if (lastPrice <= price)
                price = lastPrice - tickSize;
        }
        else
        {
            if (lastPrice >= price)
                price = lastPrice + tickSize;
        }
        return price;
    }


    /// <summary>
    /// The entry: a single market or limit order, or, with DCA's, a ladder whose first order is the
    /// entry and whose split follows the factors (the entry counts as 100, a DCA with factor 200 as
    /// twice that).
    /// </summary>
    internal static JsonObject BuildEntry(CryptoSymbol symbol, CryptoTradeSide side, decimal entryPrice, bool limitEntry, List<CryptoDcaEntry> dcaList)
    {
        if (dcaList.Count == 0)
        {
            if (!limitEntry)
                return new JsonObject { ["type"] = "market" };
            return new JsonObject { ["type"] = "limit", ["price"] = entryPrice };
        }

        List<(decimal price, decimal weight)> levels = [(entryPrice, 100m)];
        foreach (CryptoDcaEntry dca in dcaList)
            levels.Add((ClampPrice(symbol, side, PricePlacement.Adverse(side, entryPrice, dca.Percentage)), dca.Factor));

        List<decimal> split = SplitPercentages(levels.Select(level => level.weight).ToList());
        JsonArray orders = [];
        for (int i = 0; i < levels.Count; i++)
            orders.Add(new JsonObject { ["price"] = levels[i].price, ["positionPercentage"] = split[i] });
        return new JsonObject { ["type"] = "ladder", ["orders"] = orders };
    }


    /// <summary>
    /// Weights as percentages of their total, on two decimals, adding up to exactly 100: the last one
    /// takes what the rounding left.
    /// </summary>
    internal static List<decimal> SplitPercentages(List<decimal> weights)
    {
        decimal total = weights.Sum();
        List<decimal> split = [];
        if (total <= 0)
            return weights.Select(_ => 0m).ToList();

        decimal used = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            decimal share = i == weights.Count - 1
                ? 100m - used
                : Math.Round(100m * weights[i] / total, 2, MidpointRounding.AwayFromZero);
            used += share;
            split.Add(share);
        }
        return split;
    }


    /// <summary>
    /// The take profits as percentages, which Altrady places from the entry itself. The last level
    /// takes the rest of the position, the way the trader reads the list; a trail percentage makes
    /// that last level trail instead of sell (allowed on the last target only).
    /// </summary>
    internal static JsonObject BuildTakeProfit(List<CryptoTpEntry> tpList, decimal trailPercentage)
    {
        JsonArray orders = [];
        decimal used = 0;
        for (int i = 0; i < tpList.Count; i++)
        {
            decimal share = i == tpList.Count - 1 ? 100m - used : tpList[i].Factor;
            used += share;
            orders.Add(new JsonObject { ["pricePercentage"] = tpList[i].Percentage, ["positionPercentage"] = share });
        }

        JsonObject takeProfit = new()
        {
            ["exitPriceType"] = "percentage",
            ["orders"] = orders,
        };
        if (trailPercentage > 0 && trailPercentage < 99)
            takeProfit["trailingDistance"] = trailPercentage;
        return takeProfit;
    }


    /// <summary>
    /// The stop as a percentage, with the same profit lock the webhook hands over
    /// (<see cref="AltradyWebhook.BuildStopLossBlock"/>): break even or follow take profit after a
    /// take profit fills (two or more targets), or a trailing stop that starts at the trigger.
    /// </summary>
    internal static JsonObject BuildStopLoss(decimal stopPercentage, bool moveSlToBreakEven,
        CryptoProfitLockMethod method, decimal triggerPercentage, decimal trailPercentage, int takeProfitCount)
    {
        JsonObject stopLoss = new()
        {
            ["orderType"] = "market",
            ["stopPercentage"] = stopPercentage,
        };

        if (!moveSlToBreakEven)
            return stopLoss;

        if (ProfitLockCalculator.IsArmedByTakeProfit(method))
        {
            if (takeProfitCount >= 2)
                stopLoss["protection"] = method == CryptoProfitLockMethod.FollowTakeProfit ? "followTakeProfit" : "breakEven";
            return stopLoss;
        }

        if (method != CryptoProfitLockMethod.TrailingPercentage)
            return stopLoss;
        if (triggerPercentage <= 0 || trailPercentage <= 0 || trailPercentage >= 99)
            return stopLoss;

        stopLoss["protection"] = "price";
        stopLoss["trailingType"] = "position";
        stopLoss["trailingPercentage"] = triggerPercentage;
        stopLoss["trailingDistance"] = trailPercentage;
        return stopLoss;
    }


    private static decimal ClampPrice(CryptoSymbol symbol, CryptoTradeSide side, decimal price)
    {
        return price.ClampPrice(side, symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);
    }
}
