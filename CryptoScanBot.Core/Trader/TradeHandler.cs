using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trader;

static public class TradeHandler
{
    /// <summary>
    /// Vanuit de user ticker komt een reactie op een trade. De positie wordt doorgegeven naar 
    /// een andere thread waar de positie doorberekend wordt (met een kleine vertraging) 
    /// NB: De meegegeven order is meestal een tijdelijke order (voor interne datatransfer)
    /// </summary>
    public static async Task HandleTradeAsync(CryptoSymbol symbol, CryptoOrderStatus orderStatus, CryptoOrder order)
    {
        // Find the open position
        if (order.TradeAccount.Data.PositionList.TryGetValue(symbol.Name, out CryptoPosition? position))
        {
            // could also be done in ThreadDoubleCheckPosition
            if (!GlobalData.BackTest && orderStatus.IsFilled() && GlobalData.Settings.General.SoundTradeNotification)
                GlobalData.PlaySomeMusic("sound-trade-notification.wav");

            // De actie doorgeven naar een andere thread
            position.ForceCheckPosition = true;
            position.DelayUntil = GlobalData.GetCurrentDateTime(position.Account).AddSeconds(10);
            if (GlobalData.ThreadCheckPosition != null)
                await GlobalData.ThreadCheckPosition.AddToQueue(position, order.OrderId, order.Status);

            PaperAssets.Change(position.Account, position.Symbol, position.Side, order.Side, CryptoOrderStatus.Filled, order.Quantity, order.QuoteQuantity);
        }
    }
}
