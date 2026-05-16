using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Trend;

#if DEBUG
namespace CryptoScanner.Core.Signal.StochMacd;

/// <summary>
/// Short variant — mirror of <see cref="SignalStochMacdLong"/>.
///
/// Trigger (in evaluation order — cheapest first):
///   1. Optional trend filter : TrendPrimary on the active interval must be Bearish.
///   2. Stochastic overbought.
///   3. MACD histogram cross DOWN through zero.
/// </summary>
public class SignalStochMacdShort : SignalStochMacdBase
{
    private decimal? _proposedSl;
    private decimal? _proposedTp;

    public override decimal? OverrideSlPrice => _proposedSl;
    public override decimal? OverrideTpPrice => _proposedTp;

    public override bool IsSignal()
    {
        _proposedSl = null;
        _proposedTp = null;
        ExtraText = "";
        var settings = GlobalData.Settings.Signal.StochMacd;

        // 1. Trend filter — TrendPrimary (Dow-theory ZigZag) on the active interval must be Bearish.
        //    Replaces the older "close < SMA200" check, aligning this strategy with the rest of the
        //    trend infrastructure (same source as SignalTrend / SignalTrendHtf).
        if (settings.RequireTrendFilter)
        {
            _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;
            var primary = SymbolInterval.TrendPrimary.Trend;
            if (primary != CryptoTrendIndicator.Bearish)
            {
                ExtraText = $"TrendPrimary {primary}, need Bearish";
                return false;
            }
        }
        decimal close = CandleLast.Candle.Close;

        // 2. Stoch overbought
        if (!CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }

        // 3. MACD histogram cross down — previous bar at-or-above zero, current bar below zero
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
        {
            ExtraText = "no prev candle for macd cross check";
            return false;
        }
        double prevH = prev.CandleData!.MacdHistogram!.Value;
        double currH = CandleLast.CandleData!.MacdHistogram!.Value;
        if (!(prevH >= 0 && currH < 0))
        {
            ExtraText = $"no macd cross down (prev={prevH:N4}, curr={currH:N4})";
            return false;
        }

        // Compute proposed SL (swing high above entry) and TP, exposed via the override hooks.
        if (TryFindSwingHigh(settings.SwingLookback, settings.SwingPivotBars, out decimal swingHigh) && swingHigh > close)
        {
            decimal risk = swingHigh - close;
            decimal rawTp = close - settings.RiskRewardRatio * risk;
            decimal tp = AdjustTpForFees(close, rawTp);
            _proposedSl = swingHigh;
            _proposedTp = tp;
            string feeNote = settings.IncludeFeesInTp && Symbol.Exchange.FeeRate > 0
                ? $", fee {Symbol.Exchange.FeeRate:N3}% → rawTp={rawTp:N6}"
                : "";
            ExtraText = $"macd cross down @ {close} | sl={swingHigh:N6} (risk {risk:N6}) | tp={tp:N6} (rrr={settings.RiskRewardRatio}{feeNote})";
        }
        else
        {
            ExtraText = $"macd cross down @ {close} | no valid swing-high in last {settings.SwingLookback} bars";
        }
        return true;
    }
}
#endif
