﻿using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange;

public class CandleBase(ExchangeBase api)
{
    private static readonly SemaphoreSlim GetCandlesSemaphore = new(1);

    private ExchangeBase Api { get; set; } = api;

    public async Task GetCandlesForAllIntervalsAsync(CryptoSymbol symbol, long fetchEndUnix)
    {
        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
            return;

        using IDisposable client = Api.GetClient();
        for (int i = 0; i < GlobalData.IntervalList.Count; i++)
        {
            CryptoInterval interval = GlobalData.IntervalList[i];
            await Api.Candle.GetCandlesForIntervalAsync(client, symbol, interval, fetchEndUnix);
        }


        // Remove the candles we needed because of the not supported intervals & bulk calculation
        await CandleTools.CleanCandleDataAsync(symbol, null);
    }


    public virtual async Task GetCandlesAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Fetching {exchange.Name} information");
            try
            {
                await GetCandlesSemaphore.WaitAsync();
                try
                {
                    GlobalData.SetCandleTimerEnable(false);
                    //GlobalData.AddTextToLogTab("");
                    //GlobalData.AddTextToLogTab("Ophalen " + exchange.Name);

                    // Bij het opstarten is deze (vanuit de LoadData) reeds uitgevoerd
                    if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Initializing)
                        await Api.Symbol.GetSymbolsAsync();

                    // TODO: Niet alle symbols zijn actief
                    GlobalData.AddTextToLogTab($"Aantal symbols={exchange.SymbolListName.Values.Count}");


                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        if (!symbol.IsSpotTradingAllowed || symbol.Status == 0 || symbol.IsBarometerSymbol() || !symbol.QuoteData.FetchCandles)
                            continue;

                        //if (symbol.Name.Equals("BTCUSDT") || symbol.Name.Equals("ETHUSDT") || symbol.Name.Equals("ADABTC") || symbol.Name.Equals("LEVERBTC"))
                        queue.Enqueue(symbol);
                    }


                    // Haal de candles op en zorg dat deze overlapt met de candles van de socket stream(s)
                    // De datum en tijd tot na het activeren van beide streams (overlap)
                    DateTime fetchEndUnixDate = DateTimeOffset.UtcNow.UtcDateTime;
                    long fetchEndUnix = CandleTools.GetUnixTime(fetchEndUnixDate, 60);


                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = [];
                    while (taskList.Count < 5)
                    {
                        Task task = Task.Run(async () =>
                        {
                            //await GetCandlesForAllSymbolsFetchCandlesAsync(fetchEndUnix, queue); }
                            try
                            {
                                while (true)
                                {
                                    CryptoSymbol symbol;

                                    Monitor.Enter(queue);
                                    try
                                    {
                                        if (queue.Count > 0)
                                            symbol = queue.Dequeue();
                                        else
                                            break;
                                    }
                                    finally
                                    {
                                        Monitor.Exit(queue);
                                    }

                                    // Er is niet geswitvhed van exchange (omdat het ophalen zo lang duurt)
                                    if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId)
                                    {
                                        CandleTools.DetermineFetchStartDate(symbol, fetchEndUnix);
                                        await GetCandlesForAllIntervalsAsync(symbol, fetchEndUnix);
                                    }
                                }
                            }
                            catch (Exception error)
                            {
                                ScannerLog.Logger.Error(error, "");
                                GlobalData.AddTextToLogTab("error getting candles " + error.ToString()); // symbol.Text + " " + 
                            }
                        });
                        taskList.Add(task);
                    }
                    await Task.WhenAll(taskList).ConfigureAwait(false);

                    GlobalData.AddTextToLogTab("Candles ophalen klaar");
                }
                finally
                {
                    // Enabled analysing
                    GlobalData.SetCandleTimerEnable(true);

                    GetCandlesSemaphore.Release();
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("error get prices " + error.ToString());
            }
        }
    }


}
