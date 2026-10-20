using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.VwapMr.Signal;

/// <summary>
/// VWAP mean reversion. A long fires when the low of this candle or of the one before it touched
/// the lower band (each against its own band) and this candle is green; a short the other way round
/// with the upper band and a red candle. With ExitAtVwap the same strategy closes the position once
/// a candle closes back at the day VWAP.
/// <para>
/// The band lives in the plugin slot of CryptoData (<see cref="VwapMrCandleData"/>) and is only
/// there when the day VWAP is complete; on a candle without it there is no signal and no exit.
/// </para>
/// </summary>
public class VwapMrBase : SignalCreateBase
{
    private decimal? _slPercentage;

    public override decimal? OverrideSlPercentage => _slPercentage;

    public override bool HasExitSignal => VwapMrPlugin.Settings.ExitAtVwap;


    public override bool IsSignal()
    {
        ExtraText = "";
        _slPercentage = null;
        VwapMrSettings settings = VwapMrPlugin.Settings;
        var candle = CandleLast.Candle;
        bool isLong = SignalSide == CryptoTradeSide.Long;

        // Cheapest first: the colour of the candle needs no lookup at all.
        if (settings.RequireReversalCandle)
        {
            bool turned = isLong ? candle.Close > candle.Open : candle.Close < candle.Open;
            if (!turned)
            {
                ExtraText = isLong ? "no green candle" : "no red candle";
                return false;
            }
        }

        var band = CandleLast.CandleData?.GetPluginData<VwapMrCandleData>();
        if (band == null)
        {
            ExtraText = "no complete day vwap yet";
            return false;
        }

        // The touch, on this candle or on the one before it, each against its own band. The
        // candle before may belong to the previous day: its band is the one that held then.
        bool touchedNow = isLong ? (double)candle.Low <= band.Lower : (double)candle.High >= band.Upper;
        bool touchedBefore = false;
        if (!touchedNow && GetPrevCandle(CandleLast, out MyData? prev) && prev != null)
        {
            var prevBand = prev.CandleData?.GetPluginData<VwapMrCandleData>();
            if (prevBand != null)
            {
                touchedBefore = isLong
                    ? (double)prev.Candle.Low <= prevBand.Lower
                    : (double)prev.Candle.High >= prevBand.Upper;
            }
        }
        if (!touchedNow && !touchedBefore)
        {
            ExtraText = isLong ? "no touch of the lower band" : "no touch of the upper band";
            return false;
        }

        if (settings.StopLossPercentage > 0)
            _slPercentage = settings.StopLossPercentage;

        string which = isLong ? "lower" : "upper";
        string when = touchedNow ? "" : " on the previous candle";
        double distance = (double)candle.Close != 0 ? 100.0 * Math.Abs(band.Vwap - (double)candle.Close) / (double)candle.Close : 0;
        ExtraText = $"touched the {which} band{when}, vwap {band.Vwap:N8} is {distance:N2}% away";
        return true;
    }


    /// <summary>
    /// Out when the close is at or beyond the day VWAP. A state, not an event: a candle the monitor
    /// did not get to see is not a lost exit, the next close beyond the VWAP asks again. Right after
    /// midnight the VWAP starts again close to the price, so a position that is still open then
    /// usually leaves on the first candles of the new day.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        if (!VwapMrPlugin.Settings.ExitAtVwap)
            return false;

        var band = CandleLast.CandleData?.GetPluginData<VwapMrCandleData>();
        if (band == null)
        {
            ExtraText = "no complete day vwap yet";
            return false;
        }

        double close = (double)CandleLast.Candle.Close;
        bool back = SignalSide == CryptoTradeSide.Long ? close >= band.Vwap : close <= band.Vwap;
        if (!back)
        {
            ExtraText = "price not back at the vwap";
            return false;
        }

        ExtraText = $"close {close:N8} back at the vwap {band.Vwap:N8}";
        return true;
    }
}


public class VwapMrLong : VwapMrBase
{
}


public class VwapMrShort : VwapMrBase
{
}
