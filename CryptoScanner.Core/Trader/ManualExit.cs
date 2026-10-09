using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// The user asked for a position to be closed from the grid, whatever its result (take the profit
/// or take the loss). Shared by the Avalonia command and the Blazor grid service so both UIs do
/// exactly the same thing.
/// <para>
/// An OPEN position takes the door the trader already has for a strategy exit and for a position
/// past its maximum duration: <see cref="CryptoPosition.ExitRequested"/>. From then on the take
/// profit is aimed one tick through the last price instead of at the target, and a waiting entry
/// is cancelled instead. The position is handled right away with the last 1m candle, so the exit
/// order is on the book before the next candle. When the exit fills the position becomes Ready and
/// ThreadCheckFinishedPosition sends the Altrady close by itself, for a position that was delegated
/// (PaperTradingAndAltrady). No second close is sent from here: Altrady refuses a close for a
/// position it no longer has, and that refusal lands in the error tab.
/// </para>
/// <para>
/// A CLOSED position in our own administration can still be open at Altrady: in pure Altrady mode
/// the position is closed here the moment the entry is delegated (status Altrady), and a delegated
/// paper position whose close signal was refused keeps its counterpart there too. For those only
/// the webhook close applies, addressed by the id Altrady gave the opening signal.
/// </para>
/// </summary>
public static class ManualExit
{
    /// <summary>
    /// Whether the grid may offer the close for this position: it is still open in our own
    /// administration, or it carries an Altrady id so the close can be sent to Altrady.
    /// </summary>
    public static bool CanExit(CryptoPosition position)
    {
        if (!position.CloseTime.HasValue)
            return true;
        return CanCloseAtAltrady(position);
    }

    /// <summary>
    /// A position that reached Altrady (the opening signal was accepted, so it has an id) in a mode
    /// that delegates to Altrady. The emulator never touches the network.
    /// </summary>
    public static bool CanCloseAtAltrady(CryptoPosition position)
    {
        if (GlobalData.IsEmulatorMode)
            return false;
        if (string.IsNullOrEmpty(position.AltradyPositionId))
            return false;
        return GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady
            || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTradingAndAltrady;
    }

    /// <summary>
    /// The text for the confirmation, telling the user which of the two routes the close takes.
    /// </summary>
    public static string ConfirmationText(CryptoPosition position)
    {
        if (!position.CloseTime.HasValue)
            return $"Close position {position.Id} {position.Symbol.Name} {position.SideText} at the current price (take the profit or the loss)?";
        return $"Position {position.Id} {position.Symbol.Name} {position.SideText} is already closed here, send a close to Altrady (id {position.AltradyPositionId})?";
    }

    /// <summary>
    /// Close the position. Returns false when there was nothing to close, with the reason in the log.
    /// Runs on a background thread in both UIs, so everything it touches is the shared position
    /// object and the database; the grids learn about the result through the usual messages.
    /// </summary>
    public static async Task<bool> RequestExitAsync(CryptoDatabase database, CryptoPosition position)
    {
        CryptoSymbol symbol = position.Symbol;

        // Closed in our own administration: the only thing left to do is the Altrady side
        if (position.CloseTime.HasValue)
        {
            if (!CanCloseAtAltrady(position))
            {
                GlobalData.AddTextToLogTab($"{symbol.Name} position {position.Id} is already closed, nothing to close");
                return false;
            }

            GlobalData.AddTextToLogTab($"{symbol.Name} {position.SideText} position {position.Id} close requested by the user, sending the close to Altrady");
            return await AltradyWebhook.DelegateControlToAltradyAsync(position, command: "close");
        }

        // Make sure the administration is current before deciding anything on it (the DCA
        // commands do the same): a fill that the ticker missed changes what the exit has to do.
        PositionTools.LoadPosition(database, position);
        await TradeTools.CalculatePositionResultsViaOrders(database, position, forceCalculation: true);

        // The recalculation itself can have closed the position (all coins were sold already)
        if (position.CloseTime.HasValue)
        {
            GlobalData.AddTextToLogTab($"{symbol.Name} position {position.Id} turned out to be closed already, nothing to close");
            return false;
        }

        var symbolPeriod = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (symbolPeriod.CandleList.Count == 0)
        {
            GlobalData.AddTextToLogTab($"{symbol.Name} position {position.Id} cannot be closed, no 1m candle available yet");
            return false;
        }
        CryptoCandle lastCandle1m = symbolPeriod.CandleList.Values.Last();

        // Same two flags CheckStrategyExit sets: the exit price in CalculateTpPrice, and the
        // candle gates (CandleCanMovePosition, ShouldRunHandlePosition) letting the position through
        // on a quiet candle. In memory only, like the strategy exit; the exit order placed below is
        // what survives a restart.
        position.ExitRequested = true;
        position.ForceCheckPosition = true;
        GlobalData.AddTextToLogTab($"{symbol.Name} {position.SideText} position {position.Id} exit requested by the user");

        // CheckThePosition rather than HandlePosition: a waiting entry is cancelled in
        // CancelOrdersIfClosedOrTimeoutOrReposition, a trading position gets its exit order from
        // HandlePosition, and CheckThePosition runs both.
        using PositionMonitor positionMonitor = new(symbol, lastCandle1m);
        await positionMonitor.CheckThePosition(position);
        return true;
    }
}
