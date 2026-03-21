using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Momentum;

/// <summary>
/// Stochastic Directional Short strategy.
///
/// The idea: the higher-interval stochastic was recently overbought and is now falling toward
/// oversold (but not there yet). This tells us the higher-interval direction is down.
/// The current (lower) interval stochastic is also falling, confirming a good short entry moment.
///
/// The higher interval is determined automatically via a ~12x ratio mapping (same convention
/// as BBMA), e.g. 5m → 1h, 15m → 4h, 1h → 1d.
/// </summary>
public class SignalStochDirShort : SignalSbmBaseShort
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
    /// Shared direction checks for a single interval (short side).
    /// Covers: BB width (optional), stoch not yet oversold, stoch falling, %K below %D, RSI falling.
    /// Unique higher-interval checks (exited overbought, travel distance, recent history,
    /// previous-candle cross) are handled inline in IsSignal.
    /// </summary>
    private bool CheckIntervalShort(CryptoSymbolInterval symbolInterval, MyData data, 
        bool checkBb, int stochLookback, int stochAllowed)
    {
        if (checkBb && !data.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {data.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        if (data.StochOverbought())
        {
            ExtraText = $"{symbolInterval.Interval.Name} stoch still overbought";
            return false;
        }

        // Stoch must not yet be oversold — if already there the move is mostly done
        if (data.StochOversold())
        {
            ExtraText = $"{symbolInterval.Interval.Name} stoch already oversold ({data.CandleData.StochOscillator:N1})";
            return false;
        }

        // Stoch must be falling
        if (!this.StochDecreasingInTheLast(symbolInterval, data, stochLookback, stochAllowed))
        {
            ExtraText = $"{symbolInterval.Interval.Name} stoch not falling";
            return false;
        }

        // %K must be below %D (bearish alignment)
        if (data.CandleData!.StochOscillator >= data.CandleData.StochSignal)
        {
            ExtraText = $"{symbolInterval.Interval.Name} %K({data.CandleData.StochOscillator:N1}) above %D({data.CandleData.StochSignal:N1})";
            return false;
        }

        // RSI must be decreasing (allow 2 deviations for a forming candle)
        if (!this.RsiDecreasingInTheLast(symbolInterval, data, 3, 2))
        {
            ExtraText = $"{symbolInterval.Interval.Name} rsi not decreasing";
            return false;
        }

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // ── Step 1: lower-interval checks (cheap, no extra candle lookup) ────────────────

        if (!CheckIntervalShort(SymbolInterval, CandleLast, checkBb: true, stochLookback: 2, stochAllowed: 999))
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

        // Higher-interval stoch must have traveled at least 15 points down from the overbought boundary,
        // ensuring a real move has taken place and not just a tiny pullback
        const double MinStochTravel = 15.0;
        double stochTraveled = GlobalData.Settings.General.SettingsStoch.Overbought - stochHigher;
        if (stochTraveled < MinStochTravel)
        {
            ExtraText = $"{higherInterval.Name} stoch barely moved ({stochTraveled:N1} < {MinStochTravel})";
            return false;
        }

        // Higher interval must have been recently overbought (within the last 30 candles).
        // Note: we do NOT use StochOverboughtSurface here because that function starts
        // from the current candle and breaks at stoch < 60, making it always return 0
        // once the stoch has meaningfully fallen. Instead we simply walk back.
        bool wasOverbought = false;
        MyData? walkCandle = higherData;
        for (int i = 0; i < 30; i++)
        {
            if (!GetPrevCandle(result.higherInterval.Interval, walkCandle, out walkCandle))
                break;

            if (walkCandle!.StochOversold())
            {
                ExtraText = $"{higherInterval.Name} stoch oversold";
                return false;
            }

            if (walkCandle!.StochOverbought())
            {
                wasOverbought = true;
                break;
            }
        }
        if (!wasOverbought)
        {
            ExtraText = $"{higherInterval.Name} not recently overbought";
            return false;
        }

        // Shared direction checks for the higher interval
        if (!CheckIntervalShort(result.higherInterval, higherData, checkBb: false, stochLookback: 3, stochAllowed: 2))
            return false;

        // Also verify previous higher-interval candle had %K below %D — a recent bullish cross signals weakness
        if (GetPrevCandle(result.higherInterval.Interval, higherData, out MyData? prevHigher)
            && prevHigher?.CandleData?.StochOscillator != null
            && prevHigher.CandleData.StochOscillator >= prevHigher.CandleData.StochSignal)
        {
            ExtraText = $"{higherInterval.Name} stoch %K recently crossed above %D";
            return false;
        }

        double stochLow = CandleLast.CandleData!.StochOscillator!.Value;
        ExtraText = $"{Interval.Name}:%K{stochLow:N1}/%D{CandleLast.CandleData.StochSignal:N1} {higherInterval.Name}:%K{stochHigher:N1}/%D{higherData.CandleData.StochSignal:N1} trvl:{stochTraveled:N1} bb:{CandleLast.CandleData.BollingerBandsPercentage:N2}";

        return true;
    }
}
