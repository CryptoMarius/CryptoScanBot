using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Trader;

#if TRADEBOT
public class PaperTrading
{
    public static async Task CreatePaperTradeObject(CryptoDatabase Database, CryptoPosition position, CryptoPositionStep step, decimal price, DateTime LastCandle1mCloseTimeDate)
    {
        // Als een surrogaat van de exchange...

        CryptoTrade trade = new()
        {
            TradeAccount = position.TradeAccount,
            TradeAccountId = position.TradeAccountId,
            Exchange = position.Symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,

            TradeTime = LastCandle1mCloseTimeDate.AddSeconds(2), // Datum van sluiten candle en een beetje extra
            Price = price, // prijs bijwerken voor berekening break-even (is eigenlijk niet okay, 2e veld introduceren?)
            Quantity = step.Quantity,
            QuoteQuantity = step.Quantity * price, // (via de meegegeven prijs)

            Commission = step.Quantity * price * 0.1m / 100m, // full commission, met BNB korting=0.075 (zonder kickback, anders was het 0.065?)
            CommissionAsset = position.Symbol.Quote,

            TradeId = Database.CreateNewUniqueId(), // Een fake trade ID (als er maar een getal in zit)
            Side = step.Side,
        };
        // TODO: Dit gaat niet goed als van een OCO de stop wordt geraakt (Order2Id), price is wel okay overigens
        if (step.OrderId != "")
            trade.OrderId = step.OrderId;


        // bewaar de gemaakte trade
        using CryptoDatabase databaseThread = new();
        databaseThread.Open();
        databaseThread.Connection.Insert<CryptoTrade>(trade);
        GlobalData.AddTrade(trade);

        await TradeHandler.HandleTradeAsync(position.Symbol, step.OrderType, step.Side, CryptoOrderStatus.Filled, trade);
    }


    /// <summary>
    /// Controle van alle posities na het opnieuw opstarten
    /// </summary>
    public static async Task CheckPositionsAfterRestart(CryptoTradeAccount tradeAccount)
    {
        // Positions - Parts - Steps 1 voor 1 bij langs om te zien of de prijs ooit boven of beneden de prijs is geweest

        if (tradeAccount.PositionList.Any())
        {
            CryptoDatabase database = new();
            database.Open();

            foreach (var positionList in tradeAccount.PositionList.Values.ToList())
            {
                foreach (var position in positionList.Values.ToList())
                {
                    SortedList<DateTime, CryptoPositionStep> indexList = new();

                    // Verzamel de open steps
                    foreach (var part in position.Parts.Values.ToList())
                    {
                        if (!part.CloseTime.HasValue)
                        {
                            foreach (var step in part.Steps.Values.ToList())
                            {
                                if (step.Status == CryptoOrderStatus.New)
                                    indexList.TryAdd(step.CreateTime, step);
                            }
                        }
                    }


                    // controleer vanaf de openstaande step, en het kan vast veel optimaler
                    // als we de hogere intervallen inzetten (of een combinatie indien nodig)
                    // (maar zoveel posities staan niet open. dus voorlopig is dit prima)
                    foreach (var step in indexList.Values)
                    {
                        long from = CandleTools.GetUnixTime(step.CreateTime, 60) + 60;
                        long limit = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                        while (from < limit)
                        {
                            // Eventueel missende candles hebben op deze manier geen impact
                            if (position.Symbol.CandleList.TryGetValue(from, out CryptoCandle candle))
                            {
                                await PaperTradingCheckStep(database, position, step, candle);
                            }
                            from += 60;
                        }
                    }

                    PositionTools.CalculatePositionResultsViaTrades(database, position);
                }
            }
        }

    }


    public static async Task PaperTradingCheckStep(CryptoDatabase database, CryptoPosition position, CryptoPositionStep step, CryptoCandle lastCandle1m)
    {
        if (step.Status == CryptoOrderStatus.New)
        {
            if (step.Side == CryptoOrderSide.Buy)
            {
                if (step.OrderType == CryptoOrderType.Market) // is reeds afgehandeld
                    await CreatePaperTradeObject(database, position, step, lastCandle1m.Close, lastCandle1m.Date.AddMinutes(1));
                if (step.StopPrice.HasValue)
                {
                    if (lastCandle1m.High > step.StopPrice)
                        await CreatePaperTradeObject(database, position, step, (decimal)step.StopPrice, lastCandle1m.Date.AddMinutes(1));
                }
                else if (lastCandle1m.Low < step.Price)
                    await CreatePaperTradeObject(database, position, step, step.Price, lastCandle1m.Date.AddMinutes(1));
            }
            else if (step.Side == CryptoOrderSide.Sell)
            {
                if (step.OrderType == CryptoOrderType.Market)  // is reeds afgehandeld
                    await CreatePaperTradeObject(database, position, step, lastCandle1m.Close, lastCandle1m.Date.AddMinutes(1));
                else if (step.StopPrice.HasValue)
                {
                    if (lastCandle1m.Low < step.StopPrice)
                        await CreatePaperTradeObject(database, position, step, (decimal)step.StopPrice, lastCandle1m.Date.AddMinutes(1));
                }
                else if (lastCandle1m.High > step.Price)
                    await CreatePaperTradeObject(database, position, step, step.Price, lastCandle1m.Date.AddMinutes(1));

            }
        }
    }


    public static async Task PaperTradingCheckOrders(CryptoDatabase database, CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        // Is er iets gekocht of verkocht?
        // Zoja dan de HandleTrade aanroepen.

        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (CryptoPosition position in positionList.Values.ToList())
            {
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    // reeds afgesloten
                    if (part.CloseTime.HasValue)
                        continue;

                    foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                    {
                        await PaperTradingCheckStep(database, position, step, lastCandle1m);
                    }

                }
            }
        }
    }
}
#endif
