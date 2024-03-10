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
    public static Task HandleTradeAsync(CryptoSymbol symbol,
        CryptoOrderType orderType,
        CryptoOrderSide orderSide,
        CryptoOrderStatus orderStatus,
        CryptoOrder data // Een tijdelijke of permanente order voor de interne datatransfer
        )
    {
        {
            string s;
            string msgInfo = $"{symbol.Name} side={orderSide} type={orderType} status={orderStatus} order={data.OrderId} " +
                $"price={data.Price.ToString0()} quantity={data.Quantity.ToString0()} value={data.QuoteQuantity.ToString0()}";
            
            //// Extra logging ingeschakeld
            //s = string.Format("handletrade#1 {0}", msgInfo);
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


            CryptoPosition position = null;
            CryptoPositionStep step;

            // Zoek de openstaande positie in het geheugen op
            if (data.TradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
            {
                for (int i = 0; i < positionList.Count; i++)
                {
                    CryptoPosition posTemp = positionList.Values[i];
                    if (posTemp.Orders.TryGetValue(data.OrderId, out step))
                    {
                        position = posTemp;
                        //s = $"handletrade#2 {msgInfo} positie gevonden, name={step.Side} id={step.Id} positie.status={position.Status} (memory)";
                        //// Teveel logging vermijden (zo'n trailing order veroorzaakt ook een cancel)
                        //if (orderStatus != CryptoOrderStatus.Canceled)
                        //   GlobalData.AddTextToLogTab(s);
                        break;
                    }
                }
            }


            // *********************************************************************
            // Dit is een apart stukje code, WANT: de positie staat niet in het
            // geheugen. De timeout en de buy hebben elkaar gekruist, daarom de
            // positie hier alsnog laden vanuit de database)
            // *********************************************************************
            if (position == null)
            {
                using CryptoDatabase databaseThread = new();
                databaseThread.Open();

                // Controleer via de database of we de positie kunnen vinden (opnieuw inladen van de positie)
                string sql = string.Format("select * from positionstep where OrderId=@OrderId or Order2Id=@OrderId");
                step = databaseThread.Connection.QueryFirstOrDefault<CryptoPositionStep>(sql, new { data.OrderId });
                if (step != null && step.Id > 0)
                {
                    // De positie en child objects alsnog uit de database laden
                    position = databaseThread.Connection.Get<CryptoPosition>(step.PositionId);

                    // Na het sluiten van de positie annuleren we tevens de (optionele) openstaande DCA order.
                    // Deze code veroorzaakt nu een reload van de positie en dat is niet echt nodig.
                    if (orderStatus == CryptoOrderStatus.Canceled && position.CloseTime.HasValue)
                    {
                        s = string.Format("handletrade#3.1 {0} step gevonden, name={1} id={2} positie.status={3} (cancel + positie gesloten)", msgInfo, step.Side, step.Id, position.Status);
                        GlobalData.AddTextToLogTab(s);
                        return Task.CompletedTask;
                    }


                    // De positie en child objects alsnog uit de database laden
                    PositionTools.AddPosition(data.TradeAccount, position);
                    PositionTools.LoadPosition(databaseThread, position);
                    if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                        GlobalData.PositionsHaveChanged("");

                    // De positie terugzetten naar trading (wordt verderop toch opnieuw doorgerekend)
                    if (orderStatus.IsFilled() || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        position.Status = CryptoPositionStatus.Trading;

                    s = string.Format("handletrade#3.2 {0} step hersteld, name={1} id={2} positie.status={3} (database)", msgInfo, step.Side, step.Id, position.Status);
                    GlobalData.AddTextToLogTab(s);
                }
                else
                {
                    // De step kan niet gevonden worden! We negeren deze order en gaan verder..
                    // We hebben handmatig een order geplaatst (buiten de bot om).

                    s = $"handletrade#4 {msgInfo} dit is geen order van de trader. Statistiek bijwerken & exit";
                    GlobalData.AddTextToLogTab(s);

                    // Gebruiker informeren (een trade blijft interessant tenslotte)
                    if (orderStatus.IsFilled() || orderStatus == CryptoOrderStatus.PartiallyFilled)
                        GlobalData.AddTextToTelegram(msgInfo, position);
                    return Task.CompletedTask;
                }
            }

            // De positie laten afhandelen door een andere thread.
            // (dan staat alle code voor het afhandelen van een positie centraal)
            position.DelayUntil = position.UpdateTime.Value.AddSeconds(5);
            GlobalData.ThreadDoubleCheckPosition.AddToQueue(position, true, $"Trigger userticker {data.OrderId}");


        }

        return Task.CompletedTask;
    }
}
#endif
