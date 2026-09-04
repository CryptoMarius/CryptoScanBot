using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Signal;


// Het draait allemaal om de status van het algoritme
// (het algoritme zet die status zelf alsmede delay enz.):
// -None, candle aanbieden voor signaal detectie
// -WarmingUp (voor de indicators)
// -Delaying: Een (optionele) delay
// -TryStepIn: Na een OK van het algoritme om in te stappen

public class MyData
{
    public required CryptoCandle Candle { get; set; }
    public required CryptoData CandleData { get; set; }
}

public class SignalCreateBase
{
    // RegisterAlgorithms.GetAlgorithm
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }

    // The requested strategy and side
    public required CryptoTradeSide SignalSide { get; set; }
    // The strategy NAME (e.g. "atrrb") — what the settings, the registry and the database all
    // address a strategy by. Used to be the CryptoSignalStrategy enum.
    public required string SignalStrategy { get; set; }

    // The requested candle and its indicator data (grouped)
    public required MyData CandleLast { get; set; }

    // Indicator data for other intervals now lives on the symbol's CryptoSymbolInterval.Data
    // (filled by IndicatorEngine.PrepareIndicators); read it via Symbol.GetSymbolInterval(...).TryGetCandle.

    public string ExtraText = "";

    /// <summary>
    /// Zijn de indicatoren aanwezig
    /// </summary>
    public virtual bool IndicatorsOkay(MyData data)
    {
        return data.Candle.OpenTime != 0 && data.CandleData != null;
    }

    /// <summary>
    /// Is het een signaal?
    /// </summary>
    public virtual bool IsSignal() => false;

    /// <summary>
    /// Cheap entry-condition pre-filters (indicator lookups, no candlePrev needed)
    /// applied BEFORE IsSignal. Only active when the strategy has its own entry
    /// conditions configured; skipped when using the global fallback.
    /// </summary>
    public virtual bool EntryConditionsBeforeSignal()
    {
        if (!HasStrategyEntryConditions(out var settings))
            return true;
        return CheckCheapEntryConditions(settings);
    }

    /// <summary>
    /// Expensive entry-condition post-filters (candlePrev-dependent checks and trend
    /// calculations) applied AFTER IsSignal. Only active when the strategy has its
    /// own entry conditions configured; skipped when using the global fallback.
    /// </summary>
    public virtual bool EntryConditionsAfterSignal()
    {
        if (!HasStrategyEntryConditions(out var settings))
            return true;
        return CheckExpensiveEntryConditions(settings);
    }


    /// <summary>
    /// Optional override for the price stored on the signal. Return null to use
    /// the default (last candle close). Use this when the signal references an
    /// earlier candle than CandleLast — for example BOS/CHoCH breaks, which
    /// happen at a swing pivot, not at the candle the check is running on.
    /// </summary>
    public virtual decimal? OverrideSignalPrice => null;

    /// <summary>
    /// Optional per-signal stop-loss distance, as a positive percentage from the entry. When non-null
    /// the trader uses this instead of the default percentage-based SL from Settings.Trading. Strategies
    /// that size their SL off volatility (e.g. factor * ATR%) populate this. A percentage is
    /// reference-independent, so it works for market orders and maps straight onto Altrady.
    /// </summary>
    public virtual decimal? OverrideSlPercentage => null;

    /// <summary>
    /// Optional per-signal take-profit distance, as a positive percentage from the entry. When non-null
    /// the trader uses a single TP at this distance (closing the whole position) instead of the global
    /// TP grid from Settings.Trading. Strategies that size their TP off the SL (e.g. RiskRewardRatio *
    /// SL%) populate this. A percentage is reference-independent, matching OverrideSlPercentage.
    /// </summary>
    public virtual decimal? OverrideProfitPercentage => null;

    public virtual int MacdRecoveryBarCount => 1;

    /// <summary>
    /// Whether this strategy also decides when to LEAVE. PositionMonitor.CheckStrategyExit asks
    /// <see cref="IsExitSignal"/> on every close of the position's own interval, but only for
    /// strategies that say yes here - so a strategy without an exit rule costs the monitor nothing.
    /// </summary>
    public virtual bool HasExitSignal => false;

    /// <summary>
    /// The strategy's own exit rule, evaluated on the last closed candle of the position's interval:
    /// SignalSide is the side of the POSITION and CandleLast that candle. Returning true puts the
    /// position on its way out at whatever the market offers - the same exit a position past
    /// Trading.MaxPositionDurationDays gets. Stop loss and take profit keep working next to it; this
    /// is an extra way out, not a replacement.
    /// <para>
    /// Best written as a STATE ("the lines are against us") rather than an EVENT ("they crossed on
    /// this candle"): the flag it sets lives in memory only, and a state is found again after a
    /// restart where an event is gone.
    /// </para>
    /// </summary>
    public virtual bool IsExitSignal() => false;


    public virtual bool AdditionalChecks(MyData candle, out string response)
    {
        response = "";
        return true;
    }


    /// <summary>
    /// Give up when the trader fails to pick up the signal within EntryRemoveTime bars
    /// after it fired (for example when no trading slot is free).
    /// </summary>
    public virtual bool GiveUp(CryptoSignal signal)
    {
        // BUGFIX: the previous condition was
        //     signal.CloseDate.Minutes + N * Duration < CandleLast.OpenTime.Minutes
        // which combined two off-by-ones: (a) signal.CloseDate already includes one
        // Duration past signal.OpenDate, and (b) the strict "<" requires another full
        // candle past the threshold. Result: a 15m signal with EntryRemoveTime=5 was
        // only removed 7 candles after signal close.
        //
        // Correct: signal expires once N full candles have elapsed since the signal
        // candle's OPEN time, i.e. CandleLast (the just-closed signal-interval candle)
        // sits at or beyond the N-th candle after signal.OpenDate.
        CandleTime expiryTime = CandleTime.FromDateTime(signal.OpenDate) + GlobalData.Settings.Trading.EntryRemoveTime * signal.Interval.Duration;
        if (CandleLast.Candle.OpenTime >= expiryTime)
        {
            ExtraText = $"Stop after {GlobalData.Settings.Trading.EntryRemoveTime} candles";

            // A wait that outlives the signal's lifetime means NO signal of this strategy can
            // ever be entered - the expiry above always wins from the watch window. Say so in
            // every reject instead of producing a silent zero-trade run.
            var entryConditions = ResolveEntryConditions();
            if (entryConditions.EntryWaitCandles > 0 && !WatchWindowHasPassed(signal, entryConditions))
                ExtraText += $" (EntryWaitCandles {entryConditions.EntryWaitCandles} never elapsed within the signal's lifetime, no entry is ever made)";
            return true;
        }

        // Track how far price has run against the signal, and drop it once the watch window is over
        // and it ran too far. This sits in GiveUp rather than in AllowStepIn because AllowStepIn
        // returning false means "not yet, keep waiting" - the signal stays in SignalList and is
        // re-examined on every candle for every symbol. A rejected signal has to leave that list.
        if (TrackAndRejectOnAdverseMove(signal))
            return true;

        // Avoid duplicate signals — but allow a newer signal to replace a Waiting (unfilled) position
        var position = PositionTools.HasPosition(GlobalData.ActiveExchange!, Symbol);
        if (position != null && position.Status >= CryptoPositionStatus.Trading)
        {
            ExtraText = $"Position open {position.Id} on interval {position.Interval.Name}";
            return true;
        }

        return false;
    }


    /// <summary>
    /// Updates signal.WorstAdversePercentage from the candle being evaluated, and reports whether
    /// the signal has to be abandoned: the watch window has passed and price ran further against it
    /// than EntryMaxAdversePercentage allows.
    /// <para>
    /// The tracking runs on every candle, including the one on which the window expires - the move
    /// that pushes a signal past the limit is often that same candle.
    /// </para>
    /// </summary>
    protected bool TrackAndRejectOnAdverseMove(CryptoSignal signal)
    {
        var settings = ResolveEntryConditions();
        if (settings.EntryWaitCandles <= 0)
            return false;

        // Without a limit the wait only delays the entry - there is nothing the tracking
        // could ever act on, so skip the per-candle bookkeeping altogether.
        if (settings.EntryMaxAdversePercentage <= 0)
            return false;

        // The first evaluation happens on the tick that closed the signal candle itself, and
        // SignalPrice is that candle's CLOSE. Its low/high describe what happened BEFORE the
        // signal fired, so counting them would reject a signal for its own pre-signal wick.
        // Only candles after the signal candle describe movement against the signal.
        decimal reference = signal.SignalPrice;
        if (reference > 0 && CandleLast.Candle.OpenTime.ToDateTime() > signal.OpenDate)
        {
            decimal adverse = signal.Side == CryptoTradeSide.Long
                ? 100m * (reference - CandleLast.Candle.Low) / reference
                : 100m * (CandleLast.Candle.High - reference) / reference;
            if (adverse > signal.WorstAdversePercentage)
                signal.WorstAdversePercentage = adverse;
        }
        if (!WatchWindowHasPassed(signal, settings))
            return false;
        if (signal.WorstAdversePercentage <= settings.EntryMaxAdversePercentage)
            return false;

        ExtraText = $"price ran {signal.WorstAdversePercentage:N2}% against the signal, "
            + $"more than the {settings.EntryMaxAdversePercentage}% allowed";
        return true;
    }


    /// <summary>
    /// Whether the EntryWaitCandles watch window has elapsed, measured from the signal candle's
    /// open time to the open of the candle being evaluated.
    /// <para>
    /// The window is expressed in candles of the SIGNAL'S OWN interval, not in wall-clock time. A
    /// signal is only re-examined when a candle of that interval closes, so a wall-clock window is
    /// rounded up to the next candle anyway - which made one setting mean four different delays
    /// across a run with 5m, 15m, 30m and 1h signals. Multiplying by the interval duration here
    /// makes the delay the same number of candles on every interval.
    /// </para>
    /// </summary>
    protected bool WatchWindowHasPassed(CryptoSignal signal, SettingsEntryConditions settings)
    {
        if (settings.EntryWaitCandles <= 0)
            return true;
        // signal.Interval, not the algorithm's Interval: the signal carries the interval it was
        // created on, and that is the one whose candles drive its re-examination.
        double minutes = (double)settings.EntryWaitCandles * signal.Interval.Duration;
        DateTime enterFrom = signal.OpenDate.AddMinutes(minutes);
        return CandleLast.Candle.OpenTime.ToDateTime() >= enterFrom;
    }


    /// <summary>
    /// Resolves the effective entry conditions for this signal's strategy.
    /// When the strategy has its own EntryConditions they take precedence;
    /// otherwise the global SettingsTrading values are used.
    /// </summary>
    protected SettingsEntryConditions ResolveEntryConditions()
    {
        if (GlobalData.StrategiesSettings.TryGetValue(SignalStrategy, out var entry)
            && entry.strategySettings.EntryConditions != null)
            return entry.strategySettings.EntryConditions;

        return GlobalData.Settings.Trading.EntryConditions;
    }

    /// <summary>
    /// Returns true when the strategy has its own entry conditions (not the global fallback).
    /// </summary>
    protected bool HasStrategyEntryConditions(out SettingsEntryConditions settings)
    {
        if (GlobalData.StrategiesSettings.TryGetValue(SignalStrategy, out var entry)
            && entry.strategySettings.EntryConditions != null)
        {
            settings = entry.strategySettings.EntryConditions;
            return true;
        }
        settings = null!;
        return false;
    }


    /// <summary>
    /// Cheap entry-condition checks: indicator lookups that need no candlePrev.
    /// MA200, RSI/Stoch zone recovery, Stoch extreme strength.
    /// </summary>
    protected bool CheckCheapEntryConditions(SettingsEntryConditions settings)
    {
        if (!CheckMa200Filter(settings.CheckPriceAboveMa200, settings.Ma200MinDistancePercentage, settings.Ma200ConfirmationCandles))
            return false;

        if (settings.WaitForRsiRecovery)
        {
            var rsi = CandleLast!.CandleData?.Rsi;
            if (rsi == null)
                return false;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (rsi < GlobalData.Settings.General.SettingsRsi.Oversold)
                    {
                        ExtraText = "waiting for rsi to exit os zone";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (rsi > GlobalData.Settings.General.SettingsRsi.Overbought)
                    {
                        ExtraText = "waiting for rsi to exit ob zone";
                        return false;
                    }
                    break;
            }
        }

        if (settings.WaitForStochRecovery)
        {
            var k = CandleLast!.CandleData?.StochOscillator;
            if (k == null)
                return false;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (k < GlobalData.Settings.General.SettingsStoch.Oversold)
                    {
                        ExtraText = "waiting for stoch %K to exit os zone";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (k > GlobalData.Settings.General.SettingsStoch.Overbought)
                    {
                        ExtraText = "waiting for stoch %K to exit ob zone";
                        return false;
                    }
                    break;
            }
        }

        if (settings.StochMinExtremeBars > 0 ||
            settings.StochMinExtremeArea > 0m ||
            settings.StochMinExtremeZScore > 0m)
        {
            int lookback = settings.StochExtremeLookback > 0 ? settings.StochExtremeLookback : 20;

            // (1) Persistence — count consecutive bars in OS/OB in the most-recent run.
            if (settings.StochMinExtremeBars > 0)
            {
                int bars = this.CountStochExtremeBarsBack(SymbolInterval, CandleLast, lookback, SignalSide);
                if (bars < settings.StochMinExtremeBars)
                {
                    ExtraText = $"Stoch {(SignalSide == CryptoTradeSide.Long ? "OS" : "OB")} run {bars} < min {settings.StochMinExtremeBars}";
                    return false;
                }
            }

            // (2) AUC — cumulative depth over the lookback window.
            if (settings.StochMinExtremeArea > 0m)
            {
                double os = GlobalData.Settings.General.SettingsStoch.Oversold;
                double ob = GlobalData.Settings.General.SettingsStoch.Overbought;
                double area = SignalSide == CryptoTradeSide.Long
                    ? this.StochOversoldSurface(SymbolInterval, CandleLast, lookback, os)
                    : this.StochOverboughtSurface(SymbolInterval, CandleLast, lookback, ob);
                if ((decimal)area < settings.StochMinExtremeArea)
                {
                    ExtraText = $"Stoch area {area:N1} < min {settings.StochMinExtremeArea:N1}";
                    return false;
                }
            }

            // (3) Z-score — statistical extremeness of the most extreme %K in the window.
            if (settings.StochMinExtremeZScore > 0m)
            {
                double? z = this.StochExtremeZScore(SymbolInterval, CandleLast, lookback, SignalSide);
                if (!z.HasValue)
                {
                    ExtraText = "Stoch z-score: insufficient/flat sample";
                    return false;
                }
                double need = (double)settings.StochMinExtremeZScore;
                bool ok = SignalSide == CryptoTradeSide.Long ? z.Value <= -need : z.Value >= need;
                if (!ok)
                {
                    ExtraText = $"Stoch z-score {z.Value:N2} not extreme enough (need {(SignalSide == CryptoTradeSide.Long ? "≤ -" : "≥ ")}{need:N2})";
                    return false;
                }
            }
        }

        return true;
    }


    /// <summary>
    /// Expensive entry-condition checks: candlePrev-dependent recovery checks
    /// followed by trend calculations (most expensive last).
    /// </summary>
    protected bool CheckExpensiveEntryConditions(SettingsEntryConditions settings)
    {
        if (settings.CheckFurtherPriceMove
            || settings.CheckIncreasingMacd
            || settings.CheckIncreasingRsi
            || settings.CheckIncreasingStoch)
        {
            if (!GetPrevCandle(CandleLast!, out MyData? candlePrev) || candlePrev == null)
                return false;

            if (settings.CheckFurtherPriceMove)
            {
                switch (SignalSide)
                {
                    case CryptoTradeSide.Long:
                        if (CandleLast.Candle.Close < candlePrev!.Candle.Close)
                        {
                            ExtraText = $"Price {candlePrev!.Candle.Close:N8} goes down even more {CandleLast.Candle.Close:N8}";
                            return false;
                        }
                        break;
                    case CryptoTradeSide.Short:
                        if (CandleLast.Candle.Close > candlePrev!.Candle.Close)
                        {
                            ExtraText = $"Price {candlePrev!.Candle.Close:N8} goes up even more {CandleLast.Candle.Close:N8}";
                            return false;
                        }
                        break;
                }
            }

            if (settings.CheckIncreasingMacd)
            {
                int barCount = MacdRecoveryBarCount;

                switch (SignalSide)
                {
                    case CryptoTradeSide.Long:
                        if (!this.IsMacdRecoveryOversold(barCount))
                            return false;
                        break;
                    case CryptoTradeSide.Short:
                        if (!this.IsMacdRecoveryOverbought(barCount))
                            return false;
                        break;
                }
            }

            if (settings.CheckIncreasingRsi)
            {
                switch (SignalSide)
                {
                    case CryptoTradeSide.Long:
                        if (CandleLast?.CandleData?.Rsi <= candlePrev?.CandleData?.Rsi)
                        {
                            ExtraText = $"Rsi {candlePrev.CandleData.Rsi:N8} not recovering <= {CandleLast.CandleData.Rsi:N8}";
                            return false;
                        }
                        break;
                    case CryptoTradeSide.Short:
                        if (CandleLast?.CandleData?.Rsi >= candlePrev?.CandleData?.Rsi)
                        {
                            ExtraText = $"Rsi {candlePrev.CandleData.Rsi:N8} not recovering >= {CandleLast.CandleData.Rsi:N8}";
                            return false;
                        }
                        break;
                }
            }

            // Red %D = signal, average from the last 3 %K values
            // Blue %K = Oscilator calculated from the last 14 candles
            if (settings.CheckIncreasingStoch)
            {
                switch (SignalSide)
                {
                    case CryptoTradeSide.Long:
                        // %K should recover
                        if (CandleLast?.CandleData?.StochOscillator <= candlePrev?.CandleData?.StochOscillator)
                        {
                            ExtraText = $"Stoch.K {candlePrev.CandleData.StochOscillator:N8} not recovering < {CandleLast.CandleData.StochOscillator:N8}";
                            return false;
                        }

                        // %D and %K should have crossed, %K(quick/blue) > %D(slow/red)
                        if (CandleLast?.CandleData?.StochOscillator <= CandleLast?.CandleData?.StochSignal)
                        {
                            ExtraText = $"Stoch.%D {candlePrev?.CandleData?.StochSignal:N8} not above %K {candlePrev?.CandleData?.StochOscillator:N8}";
                            return false;
                        }
                        break;
                    case CryptoTradeSide.Short:
                        // %K should recover (= fall) for a short — refuse while it is still rising.
                        if (CandleLast?.CandleData!.StochOscillator >= candlePrev?.CandleData?.StochOscillator)
                        {
                            ExtraText = $"Stoch.K {candlePrev.CandleData.StochOscillator:N8} not recovering > {CandleLast.CandleData?.StochOscillator:N8}";
                            return false;
                        }

                        // BUGFIX: the previous condition was StochSignal > StochOscillator (= %D > %K
                        // = %K < %D), which is the DESIRED short state — so the check refused exactly
                        // when it should have allowed and vice versa, letting bullish %K-above-%D
                        // setups through on shorts. Correct test: refuse while %K is still above %D
                        // (cross has not yet happened in the short direction).
                        if (CandleLast?.CandleData?.StochOscillator >= CandleLast?.CandleData?.StochSignal)
                        {
                            ExtraText = $"Stoch.%K {CandleLast?.CandleData?.StochOscillator:N8} not below %D {CandleLast?.CandleData?.StochSignal:N8}";
                            return false;
                        }
                        break;
                }
            }
        }

        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;

        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        return true;
    }


    /// <summary>
    /// Extra controles nadat we het accepteren
    /// </summary>
    public virtual bool AllowStepIn(CryptoSignal signal)
    {
        var settings = ResolveEntryConditions();

        // Watch first, act later. Returning false keeps the signal in SignalList so the next candle
        // examines it again; GiveUp above is what removes it, either because it ran too far against
        // us or because EntryRemoveTime expired.
        if (!WatchWindowHasPassed(signal, settings))
        {
            ExtraText = $"watching for {settings.EntryWaitCandles} candle(s) after the signal";
            return false;
        }

        if (!CheckCheapEntryConditions(settings))
            return false;

        return CheckExpensiveEntryConditions(settings);
    }


    // Get the candle and indicator data from the signal interval
    public bool GetPrevCandle(MyData? oldCandle, out MyData? newCandle)
    {
        if (oldCandle == null)
        {
            ExtraText = $"Candle = null";
            newCandle = null;
            return false;
        }

        CandleTime targetTime = oldCandle.Candle.OpenTime - Interval.Duration;
        if (!SymbolInterval.TryGetCandle(targetTime, out newCandle))
        {
            ExtraText = $"No prev candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newCandle = null;
            return false;
        }


        if (!IndicatorsOkay(newCandle!))
        {
            ExtraText = $"Prev problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }

    // Get the previous candle and indicator data from a DIFFERENT interval
    public bool GetPrevCandle(CryptoInterval interval, MyData? oldData, out MyData? newData)
    {
        if (oldData == null)
        {
            ExtraText = $"Candle = null";
            newData = null;
            return false;
        }

        CandleTime targetTime = oldData.Candle.OpenTime - interval.Duration;
        if (!Symbol.GetSymbolInterval(interval.IntervalPeriod).TryGetCandle(targetTime, out newData))
        {
            ExtraText = $"No prev candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newData = null;
            return false;
        }

        if (!IndicatorsOkay(newData!))
        {
            ExtraText = $"Prev problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }


    // Get the candle and indicator data from a DIFFERENT interval
    public bool GetNextCandle(CryptoInterval interval, MyData? oldData, out MyData? newData)
    {
        if (oldData == null)
        {
            newData = null;
            return false;
        }

        CandleTime targetTime = oldData.Candle.OpenTime + interval.Duration;
        if (!Symbol.GetSymbolInterval(interval.IntervalPeriod).TryGetCandle(targetTime, out newData))
        {
            ExtraText = $"No next candle or data! {targetTime.ToDateTime().ToLocalTime()}";
            newData = null;
            return false;
        }

        if (!IndicatorsOkay(newData!))
        {
            ExtraText = $"Next problem indicators! {targetTime.ToDateTime().ToLocalTime()}";
            return false;
        }

        return true;
    }


    protected MyData? HadStobbInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount, bool useHighLow)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--;
            bool isOverSold = candle is not null && candle.IsBelowBollingerBands(useHighLow) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.IsAboveBollingerBands(useHighLow) && candle.StochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }



    protected MyData? HadStorsiInThelastXCandles(CryptoTradeSide side, int skipCandleCount, int candleCount, int correction = 0)
    {
        // Is de prijs onlangs dicht bij de onderste bb geweest?
        MyData? candle = CandleLast;
        while (candleCount > 0)
        {
            skipCandleCount--; // GlobalData.Settings.Signal.StoRsi.AddRsiAmount
            bool isOverSold = candle is not null && candle.RsiOversold(correction) && candle.StochOversold();
            bool isOverBought = candle is not null && candle.RsiOverbought(correction) && candle.StochOverbought();

            if (side == CryptoTradeSide.Long)
            {
                if (isOverBought) // Een short melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverSold)
                    return candle;
            }
            else
            {
                if (isOverSold) // Een long melding! Nee ongeldig!
                    return null;
                if (skipCandleCount < 0 && isOverBought)
                    return candle;
            }

            if (!GetPrevCandle(candle, out candle))
                return null;
            candleCount--;
        }

        return null;
    }

    protected bool InLowerPartOfBollingerBands(int candleCount, decimal percentage, bool useLowHigh)
    {
        // Was the price near the lower bb?

        MyData? last = CandleLast;
        while (candleCount-- > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsLowerBand!;
            band += (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (useLowHigh)
                value = last.Candle.Low;
            else
                value = Math.Max(last.Candle.Open, last.Candle.Close);

            if (value <= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
        }

        return false;
    }


    protected bool InUpperPartOfBollingerBands(int candleCount, decimal percentage, bool useLowHigh)
    {
        // Was the price near the upper bb?

        MyData? last = CandleLast;
        while (candleCount > 0)
        {
            decimal band = (decimal)last!.CandleData?.BollingerBandsUpperBand!;
            band -= (decimal)last!.CandleData?.BollingerBandsDeviation! * percentage / 100m;

            decimal value;
            if (useLowHigh)
                value = last.Candle.High;
            else
                value = Math.Max(last.Candle.Open, last.Candle.Close);

            if (value >= band)
                return true;

            if (!GetPrevCandle(last, out last))
                return false;
            candleCount--;
        }

        return false;
    }


    protected bool CheckMa200Filter(bool enabled, decimal minDistancePercentage, int confirmationCandles)
    {
        if (!enabled)
            return true;

        var ma200 = CandleLast?.CandleData?.Sma200;
        if (ma200 == null)
        {
            ExtraText = "MA200 not available";
            return false;
        }

        decimal ma200Value = (decimal)ma200.Value;
        decimal buffer = ma200Value * minDistancePercentage / 100m;

        switch (SignalSide)
        {
            case CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close <= ma200Value + buffer)
                {
                    ExtraText = $"Price {CandleLast.Candle.Close:N8} not above MA200+buffer {ma200Value + buffer:N8} (MA200={ma200Value:N8}, buffer={minDistancePercentage}%)";
                    return false;
                }
                break;
            case CryptoTradeSide.Short:
                if (CandleLast?.Candle.Close >= ma200Value - buffer)
                {
                    ExtraText = $"Price {CandleLast.Candle.Close:N8} not below MA200-buffer {ma200Value - buffer:N8} (MA200={ma200Value:N8}, buffer={minDistancePercentage}%)";
                    return false;
                }
                break;
        }

        if (confirmationCandles > 0)
        {
            MyData? candle = CandleLast;
            for (int i = 0; i < confirmationCandles; i++)
            {
                if (!GetPrevCandle(candle!, out candle) || candle == null)
                {
                    ExtraText = $"Not enough candles for MA200 confirmation ({i}/{confirmationCandles})";
                    return false;
                }

                var prevMa200 = candle.CandleData?.Sma200;
                if (prevMa200 == null)
                {
                    ExtraText = $"MA200 not available for confirmation candle {i + 1}";
                    return false;
                }

                decimal prevMa200Value = (decimal)prevMa200.Value;

                switch (SignalSide)
                {
                    case CryptoTradeSide.Long:
                        if (candle.Candle.Close <= prevMa200Value)
                        {
                            ExtraText = $"MA200 confirmation failed: candle {i + 1} close {candle.Candle.Close:N8} not above MA200 {prevMa200Value:N8}";
                            return false;
                        }
                        break;
                    case CryptoTradeSide.Short:
                        if (candle.Candle.Close >= prevMa200Value)
                        {
                            ExtraText = $"MA200 confirmation failed: candle {i + 1} close {candle.Candle.Close:N8} not below MA200 {prevMa200Value:N8}";
                            return false;
                        }
                        break;
                }
            }
        }

        return true;
    }


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and50(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (SymbolInterval.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma50 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma50 <= lastCandle.CandleData.Sma200)
                            return true;
                    }
                }
            }

            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    /// <summary>
    /// Als de ma200 en ma20 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed200and20(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (SymbolInterval.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 200 naar boven
                        if (prevCandle.CandleData!.Sma20 < prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma20 >= lastCandle.CandleData.Sma200)
                            return true;
                        // de 50 kruist de 200 naar beneden
                        if (prevCandle.CandleData!.Sma20 > prevCandle.CandleData.Sma200 &&
                                lastCandle!.CandleData!.Sma20 <= lastCandle.CandleData.Sma200)
                            return true;
                    }
                }
            }
            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    /// <summary>
    /// Als de ma200 en ma50 elkaar gekruist of geraakt hebben dan is het een nogo
    /// Er geen crosover is geweest van de 200 en 50 in de laatste x candles.
    /// </summary>
    public bool HasCrossed50and20(int candleCount, out int candlesAgo)
    {
        // We gaan van rechts naar links (dus prev en last zijn ietwat raar)
        candlesAgo = 0;
        CandleTime time = CandleLast.Candle.OpenTime;
        MyData? prevCandle = null;
        while (candleCount >= 0)
        {
            if (SymbolInterval.TryGetCandle(time, out MyData? lastCandle))
            {
                //TimeDebug = CandleTools.GetUnixDate(lCandle.OpenTime);
                if (prevCandle != null)
                {
                    if (IndicatorsOkay(lastCandle!) && IndicatorsOkay(prevCandle))
                    {
                        // de 50 kruist de 20 naar boven
                        if (prevCandle.CandleData!.Sma50 < prevCandle.CandleData.Sma20 &&
                                lastCandle!.CandleData!.Sma50 >= lastCandle.CandleData.Sma20)
                            return true;

                        // de 50 kruist de 20 naar beneden
                        if (prevCandle.CandleData!.Sma50 > prevCandle.CandleData.Sma20 &&
                                lastCandle!.CandleData!.Sma50 <= lastCandle.CandleData.Sma20)
                            return true;
                    }
                }
            }

            candlesAgo++;
            candleCount--;
            prevCandle = lastCandle;
            time -= Interval.Duration;
        }
        return false;
    }


    public bool CheckMaCrossings(
        bool ma200AndMa20Crossing, int ma200AndMa20Lookback,
        bool ma200AndMa50Crossing, int ma200AndMa50Lookback,
        bool ma50AndMa20Crossing, int ma50AndMa20Lookback,
        out string response)
    {
        if (ma200AndMa20Crossing && HasCrossed200and20(ma200AndMa20Lookback, out int candlesAgo))
        {
            response = string.Format("ma200 and ma20 crossed ({0} candles)", candlesAgo);
            return false;
        }
        if (ma200AndMa50Crossing && HasCrossed200and50(ma200AndMa50Lookback, out candlesAgo))
        {
            response = string.Format("ma200 and ma50 crossed ({0} candles)", candlesAgo);
            return false;
        }
        if (ma50AndMa20Crossing && HasCrossed50and20(ma50AndMa20Lookback, out candlesAgo))
        {
            response = string.Format("ma50 and ma20 crossed ({0} candles)", candlesAgo);
            return false;
        }

        response = "";
        return true;
    }


    private bool CheckTrend(bool primaryTrend, string captionTrend, int intervalCount)
    {
        var trendType = primaryTrend ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
        _ = MarketTrend.CalculateMarketTrendAsync(Symbol, trendType).Result;

        // Guard against the noise on the lower timeframes
        var period = Interval.IntervalPeriod;
        //if (period < CryptoIntervalPeriod.interval5m)
        //    period = CryptoIntervalPeriod.interval5m;

        while (intervalCount-- > 0)
        {
            // Stop at the weekly interval. CalculateMarketTrendAsync deliberately skips interval1w,
            // so its trend slot stays Unknown and every signal reaching it would be refused for the
            // wrong reason. There is nothing above 1w either: GetSymbolInterval indexes straight
            // into SymbolIntervalList, so one step further would read past the end of that list.
            if (period >= CryptoIntervalPeriod.interval1w)
                break;

            var symbolPeriod = Symbol.GetSymbolInterval(period);
            var trendData = primaryTrend ? symbolPeriod.TrendPrimary : symbolPeriod.TrendSecondary;
            var trend = trendData.Trend;

            switch (SignalSide)
            {
                case CryptoTradeSide.Long:
                    if (trend != CryptoTrendIndicator.Bullish)
                    {
                        ExtraText = $"Trend{captionTrend} {trend}, need Bullish";
                        return false;
                    }
                    // Structure check: if current price has broken below the most recent swing-low,
                    // the bullish structure is invalidated even though Trend still reports Bullish.
                    // The most recent Low is either LastPivot (when type='L') or PrevPivot (when
                    // LastPivot is the High that followed the Low). When there are <2 pivots yet,
                    // both lookups return null and we skip the check.
                    decimal? lastLow = trendData.LastPivotType == 'L' ? trendData.LastPivotValue
                                     : trendData.PrevPivotType == 'L' ? trendData.PrevPivotValue
                                     : null;
                    if (lastLow.HasValue && CandleLast.Candle.Close < lastLow.Value)
                    {
                        ExtraText = $"Trend{captionTrend} {period} price {CandleLast.Candle.Close:N8} below last low {lastLow.Value:N8}";
                        return false;
                    }
                    break;
                case CryptoTradeSide.Short:
                    if (trend != CryptoTrendIndicator.Bearish)
                    {
                        ExtraText = $"Trend{captionTrend} {trend}, need Bearish";
                        return false;
                    }

                    // Mirror: if current price has broken above the most recent swing-high, the
                    // bearish structure is invalidated.
                    decimal? lastHigh = trendData.LastPivotType == 'H' ? trendData.LastPivotValue
                                      : trendData.PrevPivotType == 'H' ? trendData.PrevPivotValue
                                      : null;
                    if (lastHigh.HasValue && CandleLast.Candle.Close > lastHigh.Value)
                    {
                        ExtraText = $"Trend{captionTrend} {period} price {CandleLast.Candle.Close:N8} above last high {lastHigh.Value:N8}";
                        return false;
                    }
                    break;
            }
            period++;
        }

        return true;
    }

    public bool CheckTrendPrimary(int intervalCount = 2)
    {
        return CheckTrend(true, "primary", intervalCount);
    }


    public bool CheckTrendSecondary(int intervalCount = 2)
    {
        return CheckTrend(false, "primary", intervalCount);
    }

}