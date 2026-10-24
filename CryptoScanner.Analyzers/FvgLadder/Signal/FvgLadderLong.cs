using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Analyzers.FvgLadder.Signal;

/// <summary>
/// FVG &amp; DCA, long only. Runs on every minute candle and keeps ONE buy limit on the book: on the
/// top of the gap the price will reach first, placed once the price comes within
/// PlaceOrderDistancePercentage of it. The signal is that order; the trader keeps it as a waiting
/// position (StrategyRegistration.StandingEntry) and asks <see cref="WithdrawEntry"/> on every minute
/// whether it may stay. The strategy may hold several positions on the coin at once
/// (StrategyRegistration.ParallelPositions); a new one only comes when the rules allow it.
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

        // Rule 1.1, only for starting a position
        if (!FvgLadderTrend.IsUp(Symbol, CandleLast.Candle.OpenTime + Interval.Duration, out decimal dayClose, out decimal ema))
        {
            ExtraText = $"day close {dayClose} not above EMA{FvgLadderTrend.EmaLength} {ema:N2}";
            return false;
        }

        if (!TryFindTarget(settings, price, positions, out decimal target, out List<decimal> tops, out string reason))
        {
            ExtraText = reason;
            return false;
        }

        // The order only has to be there once the price comes near
        decimal distance = 100m * (price - target) / target;
        if (distance > settings.PlaceOrderDistancePercentage)
            return false;

        EntryPrice = target;
        List<FvgLadderLevel> levels = [];
        if (settings.Preset == FvgLadderPreset.Grid)
        {
            NoDca = true;
            TakeProfit = FvgLadderRules.TakeProfitFromBreakEven(settings.TakeProfitPercentage, Symbol.Exchange.FeeRate);
        }
        else
        {
            levels = FvgLadderRules.DcaLevels(target, tops, settings.MaxDca,
                settings.StepPercentage, settings.DcaFvgMaxPercentage, settings.DcaFactor);
            if (levels.Count == 0)
                NoDca = true;
            else
                DcaList = FvgLadderRules.ToDcaList(target, levels);
            TakeProfit = settings.TakeProfitPercentage;
        }

        string dcaText = NoDca ? "no dca" : "dca " + string.Join("/", levels.Select(l => $"{l.Price:0.########}"));
        ExtraText = $"{settings.Preset} buy limit {target} ({distance:N2}% under the price), {dcaText}, " +
            $"{positions.Count} open position(s)";
        return true;
    }


    /// <summary>
    /// The rules decide the entry themselves; the global entry conditions (watch candles, RSI
    /// recovery and the like) are not part of them.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal) => true;


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

        if (!FvgLadderTrend.IsUp(Symbol, CandleLast.Candle.OpenTime + Interval.Duration, out decimal dayClose, out decimal ema))
        {
            ExtraText = $"trend gone, day close {dayClose} not above EMA{FvgLadderTrend.EmaLength} {ema:N2}";
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

        decimal distance = 100m * (price - target) / target;
        if (distance > settings.WithdrawOrderDistancePercentage)
        {
            ExtraText = $"price {distance:N2}% above the gap top";
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
        bool ladder = settings.Preset == FvgLadderPreset.Ladder;
        tops = FvgLadderRules.UsableTops(CollectZones(), settings.MinimumGapPercentage, use4h: ladder);

        List<FvgLadderTrade> trades = [];
        foreach (CryptoPosition position in positions)
        {
            if (TryGetTrade(position, out FvgLadderTrade trade))
                trades.Add(trade);
        }

        decimal? found;
        if (ladder)
        {
            if (!FvgLadderRules.NewTradeAllowed(trades, settings.StepPercentage, out decimal? limit))
            {
                reason = "the lowest position has not bought all of its DCA's yet";
                return false;
            }
            found = FvgLadderRules.EntryTarget(tops, price, settings.OrderZones, limit);
        }
        else
            found = FvgLadderRules.GridEntryTarget(tops, price, settings.OrderZones, trades, settings.StepPercentage);

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
