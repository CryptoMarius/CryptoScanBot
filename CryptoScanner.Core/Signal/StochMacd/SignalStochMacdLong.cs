using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Trend;

#if DEBUG
namespace CryptoScanner.Core.Signal.StochMacd;

/// <summary>
/// Long variant of the Stoch + MACD crossover strategy.
///
/// Trigger (in evaluation order — cheapest first):
///   1. Optional trend filter : TrendPrimary on the active interval must be Bullish.
///   2. Stochastic oversold.
///   3. MACD histogram cross UP through zero (equivalent to MacdValue crossing above MacdSignal —
///      histogram = MacdValue - MacdSignal, so a zero-cross of one ⇔ a line-cross of the other).
///
/// On a hit the proposed SL (most recent swing low) and TP (entry + RRR × risk) are written to
/// <see cref="SignalCreateBase.ExtraText"/> for visibility. The active trader does not yet
/// apply per-signal SL/TP automatically.
/// </summary>
public class SignalStochMacdLong : SignalStochMacdBase
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

        // 1. Trend filter — TrendPrimary (Dow-theory ZigZag) on the active interval must be Bullish.
        //    Replaces the older "close > SMA200" check, aligning this strategy with the rest of the
        //    trend infrastructure (same source as SignalTrend / SignalTrendHtf).
        if (settings.RequireTrendFilter)
        {
            _ = MarketTrend.CalculateMarketTrendAsync(Symbol, GlobalData.Settings.Trend.Primary).Result;
            var primary = SymbolInterval.TrendPrimary.Trend;
            if (primary != CryptoTrendIndicator.Bullish)
            {
                ExtraText = $"TrendPrimary {primary}, need Bullish";
                return false;
            }
        }
        decimal close = CandleLast.Candle.Close;

        // 2. Stoch oversold
        if (!CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        // 3. MACD histogram cross up — previous bar at-or-below zero, current bar above zero
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
        {
            ExtraText = "no prev candle for macd cross check";
            return false;
        }
        double prevH = prev.CandleData!.MacdHistogram!.Value;
        double currH = CandleLast.CandleData!.MacdHistogram!.Value;
        if (!(prevH <= 0 && currH > 0))
        {
            ExtraText = $"no macd cross up (prev={prevH:N4}, curr={currH:N4})";
            return false;
        }

        // Compute proposed SL/TP. When a valid swing-low is found these are exposed via
        // OverrideSlPrice / OverrideTpPrice so the trader can pick them up.
        if (TryFindSwingLow(settings.SwingLookback, settings.SwingPivotBars, out decimal swingLow)
            && swingLow < close)
        {
            decimal risk = close - swingLow;
            decimal rawTp = close + settings.RiskRewardRatio * risk;
            decimal tp = AdjustTpForFees(close, rawTp);
            _proposedSl = swingLow;
            _proposedTp = tp;
            string feeNote = settings.IncludeFeesInTp && Symbol.Exchange.FeeRate > 0
                ? $", fee {Symbol.Exchange.FeeRate:N3}% → rawTp={rawTp:N6}"
                : "";
            ExtraText = $"macd cross up @ {close} | sl={swingLow:N6} (risk {risk:N6}) | tp={tp:N6} (rrr={settings.RiskRewardRatio}{feeNote})";
        }
        else
        {
            ExtraText = $"macd cross up @ {close} | no valid swing-low in last {settings.SwingLookback} bars";
        }
        return true;
    }
}
#endif
