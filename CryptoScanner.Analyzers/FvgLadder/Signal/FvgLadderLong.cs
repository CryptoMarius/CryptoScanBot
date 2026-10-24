using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Analyzers.FvgLadder.Signal;

/// <summary>
/// FVG ladder, long only. Runs on every minute candle and keeps ONE buy limit on the book: on the
/// top of the gap the price will reach first, placed once the price comes within
/// PlaceOrderWithinPercentage of it. The signal is that order; the trader keeps it as a waiting
/// position (StrategyRegistration.StandingEntry) and asks <see cref="WithdrawEntry"/> on every minute
/// whether it may stay. The strategy may hold several positions on the coin at once
/// (StrategyRegistration.ParallelPositions, limited by Trading.MaxPositionsPerCoin); a new one only comes
/// when the rules allow it.
/// <para>
/// The rules themselves are in FvgLadderRules; this class collects the gaps, the trend and the open
/// positions, and hands the entry price, the DCA ladder and the take profit over with the signal.
/// </para>
/// </summary>
public class FvgLadderLong : SignalCreateBase
{
    private decimal? EntryPrice;
    public override decimal? OverrideSignalPrice => EntryPrice;

    private List<CryptoDcaEntry>? DcaList;
    public override List<CryptoDcaEntry>? OverrideDcaList => DcaList;

    private bool NoDca;
    public override bool WithoutDca => NoDca;

    private decimal? TakeProfit;
    public override decimal? OverrideProfitPercentage => TakeProfit;


    public override bool IsSignal()
    {
        ExtraText = "";
        EntryPrice = null;
        DcaList = null;
        NoDca = false;
        TakeProfit = null;

        FvgLadderSettings settings = FvgLadderPlugin.Settings;
        // Only the coins of the list (BTC by default); the rest is not this strategy's business
        if (!settings.TradesCoin(Symbol.Base))
            return false;

        decimal price = CandleLast.Candle.Close;
        List<CryptoPosition> positions = PositionTools.ParallelPositionsOf(GlobalData.ActiveExchange!, Symbol, SignalStrategy);

        // One buy limit at a time: while it waits, WithdrawEntry decides about it
        if (positions.Any(p => p.Status == CryptoPositionStatus.Waiting))
            return false;

        // Rule 1, only for starting a position
        if (!FvgLadderTrend.IsUp(Symbol, CandleLast.Candle.OpenTime + Interval.Duration, settings, out string trendText))
        {
            ExtraText = trendText;
            return false;
        }

        if (!TryFindTarget(settings, price, positions, out decimal target, out List<decimal> tops, out string reason))
        {
            ExtraText = reason;
            return false;
        }

        // The order only has to be there once the price comes near
        decimal distance = 100m * (price - target) / target;
        if (distance > settings.PlaceOrderWithinPercentage)
            return false;

        EntryPrice = target;
        List<FvgLadderLevel> levels = settings.UseDca
            ? FvgLadderRules.DcaLevels(target, tops, settings.MaxDca, settings.DcaStepPercentage, settings.DcaFvgMaxPercentage, settings.DcaFactor)
            : [];
        if (levels.Count == 0)
            NoDca = true;
        else
            DcaList = FvgLadderRules.ToDcaList(target, levels);
        // Rule 5.1: always above the break-even price of the whole position, fees included
        TakeProfit = settings.TakeProfitPercentage;

        string dcaText = NoDca ? "no dca" : "dca " + string.Join("/", levels.Select(l => $"{l.Price:0.########}"));
        ExtraText = $"buy limit {target} ({distance:N2}% under the price), {dcaText}, {positions.Count} open position(s)";
        return true;
    }


    /// <summary>
    /// The rules decide the entry themselves; the global entry conditions (watch candles, RSI
    /// recovery and the like) are not part of them.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal) => true;


    /// <summary>
    /// The waiting buy limit stays until it fills, unless the coin left the list, the trend went, or
    /// the target changed: the gap is no longer valid, another (higher) gap became the target, or no
    /// new position is allowed any more. The distance of the price does not withdraw it, so a wick
    /// into the gap is still caught.
    /// </summary>
    public override bool WithdrawEntry(CryptoPosition position)
    {
        ExtraText = "";
        FvgLadderSettings settings = FvgLadderPlugin.Settings;
        decimal price = CandleLast.Candle.Close;

        if (!settings.TradesCoin(Symbol.Base))
        {
            ExtraText = $"{Symbol.Base} is no longer in the coin list";
            return true;
        }

        if (!FvgLadderTrend.IsUp(Symbol, CandleLast.Candle.OpenTime + Interval.Duration, settings, out string trendText))
        {
            ExtraText = $"trend gone, {trendText}";
            return true;
        }

        // The other positions of the strategy on this coin, without this waiting one
        List<CryptoPosition> others = [.. PositionTools.ParallelPositionsOf(GlobalData.ActiveExchange!, Symbol, SignalStrategy)
            .Where(p => !ReferenceEquals(p, position))];
        if (!TryFindTarget(settings, price, others, out decimal target, out _, out string reason))
        {
            ExtraText = reason;
            return true;
        }

        if (target != position.SignalPrice)
        {
            ExtraText = $"the order belongs on gap top {target} now";
            return true;
        }

        return false;
    }


    /// <summary>
    /// The gap top the next entry goes on, given the open positions of the strategy on this coin.
    /// <paramref name="tops"/> are all usable gap tops, for the DCA ladder.
    /// </summary>
    private bool TryFindTarget(FvgLadderSettings settings, decimal price, List<CryptoPosition> positions,
        out decimal target, out List<decimal> tops, out string reason)
    {
        target = 0m;
        reason = "";
        tops = FvgLadderRules.UsableTops(CollectZones(), settings.MinimumGapPercentage);

        List<FvgLadderTrade> trades = [];
        foreach (CryptoPosition position in positions)
        {
            if (TryGetTrade(position, out FvgLadderTrade trade))
                trades.Add(trade);
        }

        // The maximum is the trader's since 24-10-2026 (Trading.MaxPositionsPerCoin)
        int maxPositions = PositionTools.MaxPositionsPerCoin;
        if (!FvgLadderRules.NewTradeAllowed(trades, maxPositions, settings.UseDca, settings.DcaStepPercentage, out decimal? limit))
        {
            reason = trades.Count >= maxPositions
                ? $"already {trades.Count} position(s) on the coin, the maximum"
                : "the lowest position has not bought all of its DCA's yet";
            return false;
        }

        decimal? found = FvgLadderRules.EntryTarget(tops, price, settings.GapsInView, limit);
        if (found == null)
        {
            reason = $"no usable gap under the price ({tops.Count} gap(s), {trades.Count} open position(s))";
            return false;
        }
        target = found.Value;
        return true;
    }


    /// <summary>The open bullish fvg zones of the scanner on 1h and 4h that exist at this candle.</summary>
    private List<FvgLadderZone> CollectZones()
    {
        List<FvgLadderZone> zones = [];
        CandleTime now = CandleLast.Candle.OpenTime;
        foreach (CryptoIntervalPeriod period in (CryptoIntervalPeriod[])[CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h])
        {
            // Capture the reference so a concurrent swap of the zone list cannot pull it away mid-loop
            var longOpen = Symbol.Data.Get(period).Fvg.Zones.LongOpen;
            for (int index = 0; index < longOpen.Count; index++)
            {
                CryptoZone zone = longOpen[index];
                if (zone.CloseTime == null && zone.OpenTime <= now)
                    zones.Add(new FvgLadderZone(zone.Top, zone.Bottom, period == CryptoIntervalPeriod.interval4h));
            }
        }
        return zones;
    }


    /// <summary>
    /// What the rules need of a filled position: its first and last buy and whether its DCA's are done.
    /// False for a position that has not bought anything yet.
    /// </summary>
    public static bool TryGetTrade(CryptoPosition position, out FvgLadderTrade trade)
    {
        trade = default;
        if (position.Status != CryptoPositionStatus.Trading)
            return false;

        CryptoOrderSide buySide = position.GetEntryOrderSide();
        decimal? first = null;
        decimal? last = null;
        DateTime lastTime = DateTime.MinValue;
        int dcaFilled = 0;
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (part.Purpose != CryptoPartPurpose.Entry && part.Purpose != CryptoPartPurpose.Dca)
                continue;
            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
            {
                if (step.Side != buySide || !step.Status.IsFilled())
                    continue;
                decimal fillPrice = step.AveragePrice > 0 ? step.AveragePrice : step.Price;
                if (part.Purpose == CryptoPartPurpose.Entry)
                    first = fillPrice;
                else
                    dcaFilled++;
                DateTime fillTime = step.CloseTime ?? step.CreateTime;
                if (last == null || fillTime >= lastTime)
                {
                    last = fillPrice;
                    lastTime = fillTime;
                }
            }
        }
        if (first == null || last == null)
            return false;

        trade = new FvgLadderTrade(first.Value, last.Value, dcaFilled >= TradeTools.EffectiveDcaList(position).Count);
        return true;
    }
}
