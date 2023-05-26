using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.CommonObjects;
using CryptoSbmScanner.Model;
using System.Collections.Concurrent;

namespace CryptoSbmScanner.Intern;

public class ThreadMonitorCandle
{
    public int analyseCount = 0; //Tellertje die in de taakbalk c.q. applicatie titel komt (indicatie meldingen)
    private readonly AnalyseEvent SignalEvent = null;
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoCandle candle)> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();

    public ThreadMonitorCandle(AnalyseEvent AnalyseAlgoritmSignalEvent)
    {
        SignalEvent = AnalyseAlgoritmSignalEvent;
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop create signals"));
    }

    public void AddToQueue(CryptoSymbol symbol, CryptoCandle candle)
    {
        Queue.Add((symbol, candle));
    }

    private static void CleanSymbolData(CryptoSymbol symbol)
    {
        // Analyseer de pair in de opgegeven intervallen (zou in aparte threads kunnen om dit te versnellen, maar dit gaat best goed momenteel)
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
            // ieder geval de CandleData te clearen! Vanaf x candles terug tot de eerste de beste die null is.

            Monitor.Enter(symbol.CandleList);
            try
            {
                // Remove old indicator data
                SortedList<long, CryptoCandle> candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                for (int i = candles.Count - 62; i > 0; i--)
                {
                    CryptoCandle c = candles.Values[i];
                    if (c.CandleData != null)
                        c.CandleData = null;
                    else break;
                }


                // Remove old candles
                long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, DateTime.UtcNow);
                while (candles.Values.Any())
                {
                    CryptoCandle c = candles.Values.First();
                    if (c == null)
                        break;
                    if (c.OpenTime < startFetchUnix)
                        candles.Remove(c.OpenTime);
                    else break;
                }
            }
            finally
            {
                Monitor.Exit(symbol.CandleList);
            }
        }
    }

    
    private async void PaperTrading(CryptoSymbol symbol, CryptoCandle candle)
    {
        // Is er iets gekocht of verkocht? Zoja, een event aanroepen

        // TODO: de monitorAlgorithm.HandleTrade(symbol, data) aanroepen!
        // (met exact dezelfde informatie als Binance dat zou doen)
        // Nog even uitzoeken welke EXACT!

        if (symbol.Exchange.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            for (int p = positionList.Values.Count - 1; p >= 0; p--)
            {
                CryptoPosition position = positionList.Values[p];
                if (!position.PaperTrade)
                    continue;

                for (int s = 0; s < position.Steps.Values.Count; s++)
                {
                    CryptoPositionStep step = position.Steps.Values[s];

                    if (step.Status == OrderStatus.New)
                    {
                        if ((step.OrderSide == CryptoOrderSide.Buy) && (
                            ((step.OrderType == CryptoOrderType.Limit) && (candle.Low < step.Price)) ||
                            ((step.OrderType == CryptoOrderType.Oco) && (candle.Low >= step.StopPrice)) ||
                            ((step.OrderType == CryptoOrderType.StopLimit) && (candle.High >= step.StopPrice))
                            ))
                        {
                            //if ((step.OrderType == CryptoOrderType.StopLimit) && (candle.High >= step.StopPrice))
                            //    step.Price = (decimal)step.StopPrice; // voor de herberekening
                            //if ((step.OrderType == CryptoOrderType.Oco) && (candle.Low >= step.StopPrice))
                            //    step.Price = (decimal)step.StopPrice; // voor herberekening

                            //step.CloseTime = DateTime.UtcNow;
                            //step.Status = OrderStatus.Filled;


                            // TODO: de monitorAlgorithm.HandleTrade(symbol, data) aanroepen!
                            // (met exact dezelfde informatie als Binance dat zou doen)
                            BinanceStreamOrderUpdate data = new();
                            data.BuyerIsMaker = true;
                            data.Symbol = symbol.Name;
                            data.Side = OrderSide.Buy;
                            data.Status = OrderStatus.Filled;
                            //data.Type = step.OrderType;  ??
                            data.CreateTime = step.CreateTime;
                            data.EventTime = DateTime.UtcNow;
                            data.Id = step.OrderId;
                            data.OrderListId = (long)step.OrderListId;
                            data.Price = candle.Close; //hmmm, dat komt niet goed denk ik
                            data.Quantity = step.Quantity;
                            data.QuoteQuantity = step.Quantity; // wat is nu wat?
                            data.QuoteQuantityFilled = step.Quantity; // wat is nu wat?
                            
                            // TODO: Ik mis hier eentje?
                            if (step.OrderType == CryptoOrderType.Limit)
                                data.Type = SpotOrderType.Limit;
                            else
                            if (step.OrderType == CryptoOrderType.Oco)
                                data.Type = SpotOrderType.StopLoss;
                            else
                            if (step.OrderType == CryptoOrderType.StopLimit)
                                data.Type = SpotOrderType.StopLoss;
                            // Enzovoort?

                            PositionMonitor monitorAlgorithm = new PositionMonitor();
                            await monitorAlgorithm.HandleTrade(symbol, data, false);
                        }
                        else if ((step.OrderSide == CryptoOrderSide.Sell) && (
                            ((step.OrderType == CryptoOrderType.Limit) && (candle.High > step.Price)) ||
                            ((step.OrderType == CryptoOrderType.Oco) && (candle.High > step.Price)) ||
                            ((step.OrderType == CryptoOrderType.Oco) && (candle.Low <= step.StopPrice)) ||
                            ((step.OrderType == CryptoOrderType.StopLimit) && (candle.Low <= step.StopPrice)) //&& (order.Trailing != CryptoTrailing.TrailWaiting)
                            ))
                        {

                            //if ((step.OrderType == CryptoOrderType.StopLimit) && (candle.Low <= step.StopPrice))
                            //    step.Price = (decimal)step.StopPrice; // voor de herberekening
                            //if ((step.OrderType == CryptoOrderType.Oco) && (candle.Low <= step.StopPrice))
                            //    step.Price = (decimal)step.StopPrice; // voor de herberekening


                            // TODO: de monitorAlgorithm.HandleTrade(symbol, data) aanroepen!
                            // (met exact dezelfde informatie als Binance dat zou doen)

                            // TODO: de monitorAlgorithm.HandleTrade(symbol, data) aanroepen!
                            // (met exact dezelfde informatie als Binance dat zou doen)
                            BinanceStreamOrderUpdate data = new();
                            data.BuyerIsMaker = true;
                            data.Symbol = symbol.Name;
                            data.Side = OrderSide.Sell;
                            data.Status = OrderStatus.Filled;
                            //data.Type = step.OrderType;  ??
                            data.CreateTime = step.CreateTime;
                            data.EventTime = DateTime.UtcNow;
                            data.Id = step.OrderId;
                            data.OrderListId = (long)step.OrderListId;
                            data.Price = candle.Close; //hmmm, dat komt niet goed denk ik
                            data.Quantity = step.Quantity;
                            data.QuoteQuantity = step.Quantity; // wat is nu wat?
                            data.QuoteQuantityFilled = step.Quantity; // wat is nu wat?

                            // TODO: Ik mis hier eentje?
                            if (step.OrderType == CryptoOrderType.Limit)
                                data.Type = SpotOrderType.Limit;
                            else
                            if (step.OrderType == CryptoOrderType.Oco)
                                data.Type = SpotOrderType.StopLoss;
                            else
                            if (step.OrderType == CryptoOrderType.StopLimit)
                                data.Type = SpotOrderType.StopLoss;
                            // Enzovoort?

                            // Enzovoort?

                            PositionMonitor monitorAlgorithm = new PositionMonitor();
                            await monitorAlgorithm.HandleTrade(symbol, data, false);
                        }
                    }
                }
            }
        }
    }


    private void CreateSignals(CryptoSymbol symbol, CryptoCandle candle)
    {
        if (GlobalData.Settings.Signal.SignalsActive && symbol.QuoteData.CreateSignals)
        {
            // De 1m candle is final, neem de sluittijd van deze 1m candle
            long candleCloseTime = candle.OpenTime + 60;

            // Een extra ToList() zodat we een readonly setje hebben (en we de instellingen kunnen muteren)
            foreach (CryptoInterval interval in TradingConfig.AnalyzeInterval.Values.ToList())
            {
                // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
                if (candleCloseTime % interval.Duration == 0)
                {
                    // We geven als tijd het begin van de "laatste" candle (van dat interval)
                    SignalCreate createSignal = new(symbol, interval, false, SignalEvent);
                    createSignal.AnalyzeSymbol(candleCloseTime - interval.Duration);

                    // Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                    // TODO: MultiTread aware maken ..
                    analyseCount++;
                }
            }
        }
    }


    // 3 threads tegelijk
    private readonly SemaphoreSlim Semaphore = new(3);


    private void NewCandleArrived(CryptoSymbol symbol, CryptoCandle candle)
    {
        // Er is net een 1m candle gearriveerd en de candles in de andere intervallen zijn zojuist berekend
        Semaphore.Wait();
        try
        {
            try
            {
                if (symbol.IsSpotTradingAllowed)
                {
#if TRADEBOT
                    PaperTrading(symbol, candle);

                    // TODO: Reactivate of weghalen?
                    // Controleer de openstaande posities van deze symbol
                    if (symbol.PositionCount > 0)
                    {
                        // TODO:
                        //PositionMonitor monitorAlgorithm = new PositionMonitor();
                        //await monitorAlgorithm.CheckOpenPositions(symbol);
                    }
#endif


                    CreateSignals(symbol, candle);

#if TRADEBOT
                    // Alway's: if any available positions or signals
                    if (symbol.PositionCount > 0 || symbol.SignalCount > 0)
                        GlobalData.TaskMonitorSignal.AddToQueue(symbol);
#endif

#if BALANCING
                    if (GlobalData.Settings.BalanceBot.Active && (symbol.IsBalancing))
                        GlobalData.ThreadBalanceSymbols.AddToQueue(symbol);
#endif

                }
                CleanSymbolData(symbol);
            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("\r\n" + "\r\n" + symbol.Name + " error Analyse thread\r\n" + error.ToString());
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public void Execute()
    {
        foreach ((CryptoSymbol symbol, CryptoCandle candle) in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            new Thread(() => NewCandleArrived(symbol, candle)).Start();
        }
    }
}
