using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

/// <summary>
/// Stochastic Directional Long strategy.
///
/// The idea: the higher-interval stochastic was recently oversold and is now rising toward
/// overbought (but not there yet). This tells us the higher-interval direction is up.
/// The current (lower) interval stochastic is also rising, confirming a good long entry moment.
///
/// The higher interval is determined automatically via a ~12x ratio mapping (same convention
/// as BBMA), e.g. 5m → 1h, 15m → 4h, 1h → 1d.
/// </summary>
public class SignalStochDirLong : SignalSbmBaseLong
{

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
            || data.CandleData == null
            || data.CandleData.Rsi == null
            || data.CandleData.StochSignal == null
            || data.CandleData.StochOscillator == null)
            return false;

        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
        response = "";
        return true;
    }


    /// <summary>
    /// Shared direction checks for a single interval (long side).
    /// Covers: BB width (optional), stoch not yet overbought, stoch rising, %K above %D, RSI rising.
    /// Unique higher-interval checks (exited oversold, travel distance, recent history,
    /// previous-candle cross) are handled inline in IsSignal.
    /// </summary>
    private bool CheckIntervalLong(CryptoSymbolInterval symbolInterval, MyData data, 
        bool checkBb, int stochLookback, int stochAllowed)
    {
        if (checkBb && !data.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {data.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (data.StochOversold())
        {
            ExtraText = $"{symbolInterval.Interval.Name} stoch still oversold";
            return false;
        }

        // Stoch must not yet be overbought — if already there the move is mostly done
        if (data.StochOverbought())
        {
            ExtraText = $"{symbolInterval.Interval.Name} stoch already overbought ({data.CandleData.StochOscillator:N1})";
            return false;
        }

        // Stoch must be rising
        if (!this.StochIncreasingInTheLast(symbolInterval, data, stochLookback, stochAllowed))
        {
            ExtraText = $"{symbolInterval.Interval.Name} stoch not rising";
            return false;
        }

        // %K must be above %D (bullish alignment)
        if (data.CandleData!.StochOscillator <= data.CandleData.StochSignal)
        {
            ExtraText = $"{symbolInterval.Interval.Name} %K({data.CandleData.StochOscillator:N1}) below %D({data.CandleData.StochSignal:N1})";
            return false;
        }

        // RSI must be increasing (allow 2 deviations for a forming candle)
        if (!this.RsiIncreasingInTheLast(symbolInterval, data, 3, 2))
        {
            ExtraText = $"{symbolInterval.Interval.Name} rsi not increasing";
            return false;
        }

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // ── Step 1: lower-interval checks (cheap, no extra candle lookup) ────────────────
        if (!CheckIntervalLong(SymbolInterval, CandleLast, checkBb: true, stochLookback: 2, stochAllowed: 999))
            return false;

        // ── Step 2: higher-interval checks (only reached when lower interval matched) ────

        // Determine the higher directional interval (~12x ratio, same convention as BBMA)
        if (!StochHelper.GetStochDirHigherInterval(Interval.IntervalPeriod, out CryptoIntervalPeriod higherIntervalPeriod))
        {
            ExtraText = $"no valid higher interval for {Interval.Name}";
            return false;
        }

        var result = IndicatorDataList.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, higherIntervalPeriod);
        if (!result.success)
            return false;

        MyData higherData = result.candle!;
        var higherInterval = result.higherInterval.Interval;
        double stochHigher = higherData.CandleData!.StochOscillator!.Value;

        // Higher-interval stoch must have traveled at least 15 points up from the oversold boundary,
        // ensuring a real move has taken place and not just a tiny bounce
        const double MinStochTravel = 15.0;
        double stochTraveled = stochHigher - GlobalData.Settings.General.SettingsStoch.Oversold;
        if (stochTraveled < MinStochTravel)
        {
            ExtraText = $"{higherInterval.Name} stoch barely moved ({stochTraveled:N1} < {MinStochTravel})";
            return false;
        }

        // Higher interval must have been recently oversold (within the last 30 candles).
        // Note: we do NOT use StochOversoldSurface here because that function starts
        // from the current candle and breaks at stoch > 40, making it always return 0
        // once the stoch has meaningfully recovered. Instead we simply walk back.
        bool wasOversold = false;
        MyData? walkCandle = higherData;
        for (int i = 0; i < 30; i++)
        {
            if (!GetPrevCandle(result.higherInterval.Interval, walkCandle, out walkCandle))
                break;

            if (walkCandle!.StochOverbought())
            {
                ExtraText = $"{higherInterval.Name} stoch overbought";
                return false;
            }

            if (walkCandle!.StochOversold())
            {
                wasOversold = true;
                break;
            }
        }
        if (!wasOversold)
        {
            ExtraText = $"{higherInterval.Name} not recently oversold";
            return false;
        }

        // Shared direction checks for the higher interval
        if (!CheckIntervalLong(result.higherInterval, higherData, checkBb: false, stochLookback: 3, stochAllowed: 2))
            return false;

        // Also verify previous higher-interval candle had %K above %D — a recent bearish cross signals weakness
        if (GetPrevCandle(result.higherInterval.Interval, higherData, out MyData? prevHigher)
            && prevHigher?.CandleData?.StochOscillator != null
            && prevHigher.CandleData.StochOscillator <= prevHigher.CandleData.StochSignal)
        {
            ExtraText = $"{higherInterval.Name} stoch %K recently crossed below %D";
            return false;
        }

        double stochLow = CandleLast.CandleData!.StochOscillator!.Value;
        ExtraText = $"{Interval.Name}:%K{stochLow:N1}/%D{CandleLast.CandleData.StochSignal:N1} {higherInterval.Name}:%K{stochHigher:N1}/%D{higherData.CandleData.StochSignal:N1} trvl:{stochTraveled:N1} bb:{CandleLast.CandleData.BollingerBandsPercentage:N2}";

        return true;
    }
}
