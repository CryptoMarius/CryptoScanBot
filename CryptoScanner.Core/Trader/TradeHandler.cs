using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

public static class TradeHandler
{
    /// <summary>
    /// Vanuit de user ticker komt een reactie op een trade. De positie wordt doorgegeven naar 
    /// een andere thread waar de positie doorberekend wordt (met een kleine vertraging) 
    /// NB: De meegegeven order is meestal een tijdelijke order (voor interne datatransfer)
    /// </summary>
    public static async Task HandleTradeAsync(CryptoSymbol symbol, CryptoOrderStatus orderStatus, CryptoOrder order)
    {
        // Find the open position (the one the order belongs to: a coin can hold several parallel ones)
        CryptoPosition? position = PositionTools.PositionOfOrder(GlobalData.ActiveExchange!, symbol, order.OrderId);
        if (position != null)
        {
            // could also be done in ThreadDoubleCheckPosition
            if (!GlobalData.IsEmulatorMode && orderStatus.IsFilled() && GlobalData.Settings.General.SoundTradeNotification)
                GlobalData.PlaySomeMusic("sound-trade-notification.wav");

            // De actie doorgeven naar een andere thread
            position.ForceCheckPosition = true;
            position.DelayUntil = GlobalData.Clock.UtcNow.AddSeconds(10);
            if (GlobalData.ThreadCheckPosition != null)
                await GlobalData.ThreadCheckPosition.AddToQueue(position, order.OrderId, order.Status);

            // Moved to ThreadCheckPosition (we need the trades for the exact fees)
            //PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, order.Side, CryptoOrderStatus.Filled, order.Quantity, order.QuoteQuantity);
        }
    }
}
