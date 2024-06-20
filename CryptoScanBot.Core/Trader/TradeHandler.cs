using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trader;

#if TRADEBOT
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
        }
        // This leads to confusion if we signal stuff when the position is already closed, just keep it simple..
        //else
        //{
        //    //...if (!symbol.OrderList.TryGetValue(order.OrderId, out CryptoOrder tempOrder)) // closed?
        //    string s = $"Handletrade {symbol.Name} side={orderSide} type={orderType} status={orderStatus} order={order.OrderId} " +
        //        $"price={order.Price.ToString0()} quantity={order.Quantity.ToString0()} value={order.QuoteQuantity.ToString0()}";
        //    GlobalData.AddTextToLogTab(s);
        //    GlobalData.AddTextToTelegram(s, symbol);
        //}
    }
}
#endif
