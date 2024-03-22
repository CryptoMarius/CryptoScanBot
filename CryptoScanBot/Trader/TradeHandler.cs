using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Trader;

#if TRADEBOT
static public class TradeHandler
{
    /// <summary>
    /// Deze routine is een reactie op een gemaakte trade (vanuit de emulator of vanuit de exchange userdata stream)
    /// Momenteel wordt enkel de step, trade en/of positie bijgewerkt, er wordt niet geannuleerd of geplaatst
    /// Omdat deze in BULK (kan) worden aangeroepen worden hier verder geen besissingen gemaakt (denk aan 5 openstaande oco's)
    /// </summary>
    public static async Task HandleTradeAsync(CryptoSymbol symbol,
        CryptoOrderType orderType,
        CryptoOrderSide orderSide,
        CryptoOrderStatus orderStatus,
        CryptoOrder order // Een tijdelijke of permanente order voor de interne datatransfer
        )
    {
        {
            string s;
            string info = $"{symbol.Name} side={orderSide} type={orderType} status={orderStatus} order={order.OrderId} " +
                $"price={order.Price.ToString0()} quantity={order.Quantity.ToString0()} value={order.QuoteQuantity.ToString0()}";

            //// Extra logging ingeschakeld
            //s = string.Format("handletrade#1 {0}", info);
            //if (data.TradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest)
            //    s += string.Format(" ({0})", data.TradeAccount.Name);
            //else if (data.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
            //    s += string.Format(" ({0})", data.TradeAccount.Name);
            //// Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
            //if (orderStatus != CryptoOrderStatus.Canceled)
            //    GlobalData.AddTextToLogTab(s);


            // De oude TradeDash pling....
            if (orderStatus.IsFilled()) //|| orderStatus == CryptoOrderStatus.PartiallyFilled
            {
                if (GlobalData.Settings.General.SoundTradeNotification)
                    GlobalData.PlaySomeMusic("sound-trade-notification.wav");
            }



            // Zoek de openstaande positie op
            CryptoPosition position = null;
            if (order.TradeAccount.PositionList.TryGetValue(symbol.Name, out var posTemp))
            {
                if (posTemp.Orders.TryGetValue(order.OrderId, out _))
                {
                    position = posTemp;
                }
            }


            // De positie staat niet in het geheugen. Exchange en trader hebben
            // elkaar gekruist, daarom de positie hier alsnog laden vanuit de db.
            if (position == null)
            {
                using CryptoDatabase databaseThread = new();
                databaseThread.Open();

                // Controleer via de database of we de positie kunnen vinden (opnieuw inladen van de positie)
                string sql = string.Format("select * from positionstep where OrderId=@OrderId or Order2Id=@OrderId");
                CryptoPositionStep step = databaseThread.Connection.QueryFirstOrDefault<CryptoPositionStep>(sql, new { order.OrderId });
                if (step != null && step.Id > 0)
                {
                    // De positie en child objects alsnog uit de database laden
                    position = databaseThread.Connection.Get<CryptoPosition>(step.PositionId);

                    // Na het sluiten van de positie annuleren we tevens de (optionele) openstaande DCA order.
                    // Deze code veroorzaakt nu een reload van de positie en dat is niet echt nodig.
                    if (orderStatus == CryptoOrderStatus.Canceled && position.CloseTime.HasValue)
                    {
                        s = string.Format("handletrade#3.1 {0} step gevonden, name={1} id={2} positie.status={3} (cancel + positie gesloten)", info, step.Side, step.Id, position.Status);
                        GlobalData.AddTextToLogTab(s);
                        return; // Task.CompletedTask;
                    }


                    // De positie en child objects alsnog uit de database laden
                    PositionTools.AddPosition(order.TradeAccount, position);
                    PositionTools.LoadPosition(databaseThread, position);
                    if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                        GlobalData.PositionsHaveChanged("");

                    // De positie terugzetten naar trading (wordt verderop toch opnieuw doorgerekend)
                    if (orderStatus.IsFilled() || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        position.Status = CryptoPositionStatus.Trading;

                    s = string.Format("handletrade#3.2 {0} step hersteld, name={1} id={2} positie.status={3} (database)", info, step.Side, step.Id, position.Status);
                    GlobalData.AddTextToLogTab(s);

                    //if (orderStatus.IsFilled() || orderStatus == CryptoOrderStatus.PartiallyFilled)
                    GlobalData.AddTextToTelegram(info, position);
                }
                else
                {
                    // De step kan niet gevonden worden! We negeren deze order en gaan verder..
                    // We hebben handmatig een order geplaatst (buiten de bot om).

                    s = $"handletrade#4 {info} dit is geen order van de trader. Statistiek bijwerken & exit";
                    GlobalData.AddTextToLogTab(s);

                    // Gebruiker informeren (een trade blijft interessant tenslotte)
                    //if (orderStatus.IsFilled() || orderStatus == CryptoOrderStatus.PartiallyFilled)
                    GlobalData.AddTextToTelegram(info, position);
                    return; // Task.CompletedTask;
                }
            }

            // De positie laten afhandelen door een andere thread.
            // (dan staat alle code voor het afhandelen van een positie centraal)
            position.ForceCheckPosition = true;
            position.DelayUntil = DateTime.UtcNow.AddSeconds(10);
            await GlobalData.ThreadDoubleCheckPosition.AddToQueue(position, order.OrderId, order.Status);
        }

        return; // Task.CompletedTask;
    }
}
#endif
