using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.ImpulseBox.Signal;

/// <summary>
/// Impulse box. Fires on the candle where the detector reports an event of the enabled kinds for
/// this side: a counter trade (the box breaks against the impulse), a fake break (a break with the
/// impulse closed back inside the box), or, when switched on, a continuation. The detector state
/// lives in the plugin slot of CryptoData (<see cref="ImpulseBoxCandleData"/>); the exit is left to
/// the trading settings.
/// </summary>
public class ImpulseBoxBase : SignalCreateBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        ImpulseBoxSettings settings = ImpulseBoxPlugin.Settings;
        bool isLong = SignalSide == CryptoTradeSide.Long;

        var data = CandleLast.CandleData?.GetPluginData<ImpulseBoxCandleData>();
        if (data == null)
        {
            ExtraText = "not enough candles yet";
            return false;
        }

        if (data.Events == null)
        {
            ExtraText = data.Status;
            return false;
        }

        // The first enabled kind for this side, in this order
        ImpulseBoxEvent? found = null;
        foreach (ImpulseBoxEventKind kind in new[] { ImpulseBoxEventKind.Counter, ImpulseBoxEventKind.FakeBreak, ImpulseBoxEventKind.Continuation })
        {
            bool enabled = kind switch
            {
                ImpulseBoxEventKind.Counter => settings.TradeCounter,
                ImpulseBoxEventKind.FakeBreak => settings.TradeFakeBreak,
                _ => settings.TradeContinuation,
            };
            if (!enabled)
                continue;
            found = data.Events.FirstOrDefault(e => e.Kind == kind && e.IsLong == isLong);
            if (found != null)
                break;
        }

        if (found == null)
        {
            ExtraText = "no enabled box event for this side";
            return false;
        }

        string impulse = found.Direction == 1 ? "impulse up" : "impulse down";
        string what = found.Kind switch
        {
            ImpulseBoxEventKind.Counter => "box broken against the",
            ImpulseBoxEventKind.FakeBreak => "fake break of the box after an",
            _ => "box broken with the",
        };
        ExtraText = $"{what} {impulse}, box {found.BoxLow:N8}..{found.BoxHigh:N8}, impulse start {found.ImpulseStart:N8}";
        return true;
    }
}


public class ImpulseBoxLong : ImpulseBoxBase
{
}


public class ImpulseBoxShort : ImpulseBoxBase
{
}
