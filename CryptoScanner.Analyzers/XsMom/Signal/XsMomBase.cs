using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.XsMom.Signal;

/// <summary>
/// Cross-sectional momentum. On a candle that closes at a rebalance moment (00:00 UTC on the rebalance
/// day) a coin gets a signal when the ranking puts it on this side; a position of this strategy is
/// closed at such a moment when its coin is no longer on its side (SignalCreateBase.IsExitSignal).
/// Between two rebalance moments nothing happens. A coin that stays selected keeps its position: the
/// new signal finds the position already open.
/// <para>
/// The ranking is made over all coins of the quote at once (<see cref="XsMomRanking"/>), so every
/// interval whose candle closes at the rebalance moment gives the same answer; 1d is the natural one.
/// </para>
/// </summary>
public class XsMomBase : SignalCreateBase
{
    public override bool HasExitSignal => true;


    /// <summary>The moment the last candle closed, null when that is not a rebalance moment.</summary>
    private CandleTime? RebalanceMoment(XsMomSettings settings)
    {
        CandleTime closeTime = CandleLast.Candle.OpenTime + Interval.Duration;
        return XsMomRanking.IsRebalanceMoment(closeTime, settings) ? closeTime : null;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        XsMomSettings settings = XsMomPlugin.Settings;

        // Cheapest first: is this a rebalance moment at all
        CandleTime? moment = RebalanceMoment(settings);
        if (moment == null)
        {
            ExtraText = "no rebalance moment";
            return false;
        }

        XsMomSelection? selection = XsMomRanking.GetSelection(Symbol, moment.Value, settings);
        if (selection == null)
        {
            ExtraText = "no daily history for the ranking";
            return false;
        }

        if (!selection.TryGet(Symbol.Name, out XsMomPick pick) || pick.Side != SignalSide)
        {
            ExtraText = SignalSide == CryptoTradeSide.Long ? "not among the strongest" : "not among the weakest";
            return false;
        }

        ExtraText = $"rank {pick.Rank + 1} of {pick.Count}, {pick.ReturnPercentage:N2}% over {settings.LookbackDays} days";
        return true;
    }


    /// <summary>
    /// Out at a rebalance moment when the coin is no longer on the side of the position. When the
    /// ranking cannot be made the position stays: no answer is not a reason to sell.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        XsMomSettings settings = XsMomPlugin.Settings;

        CandleTime? moment = RebalanceMoment(settings);
        if (moment == null)
        {
            ExtraText = "no rebalance moment";
            return false;
        }

        XsMomSelection? selection = XsMomRanking.GetSelection(Symbol, moment.Value, settings);
        if (selection == null)
        {
            ExtraText = "no daily history for the ranking";
            return false;
        }

        if (selection.TryGet(Symbol.Name, out XsMomPick pick) && pick.Side == SignalSide)
        {
            ExtraText = $"still selected, rank {pick.Rank + 1} of {pick.Count}";
            return false;
        }

        ExtraText = SignalSide == CryptoTradeSide.Long ? "no longer among the strongest" : "no longer among the weakest";
        return true;
    }
}


public class XsMomLong : XsMomBase
{
}


public class XsMomShort : XsMomBase
{
}
