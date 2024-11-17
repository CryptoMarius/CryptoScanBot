﻿using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Kucoin.Net.Clients;

namespace ExchangeTest.Exchange.Kucoin.Futures;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{

    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {

        TickerGroup!.SocketClient ??= new KucoinSocketClient();


        // What the use if it only reports de ask and bid prices?

        //((KucoinSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToAllTickerUpdatesAsync

        CallResult<UpdateSubscription> subscriptionResult = await ((KucoinSocketClient)TickerGroup.SocketClient).FuturesApi.SubscribeToTickerUpdatesAsync("", data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out CryptoScanBot.Core.Model.CryptoExchange? exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                var tick = data.Data;
                {
                    string symbolName = tick.Symbol.Replace("-", "");
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        TickerCount++;
                        //symbol.OpenPrice = tick.OpenPrice;
                        //symbol.HighPrice = tick.HighPrice;
                        //symbol.LowPrice = tick.LowPrice;
                        //if (tick.LastPrice.HasValue)
                        //    symbol.LastPrice = tick.LastPrice;
                        //if (tick.BestBidPrice.HasValue)
                        symbol.BidPrice = tick.BestBidPrice;
                        //if (tick.BestAskPrice.HasValue)
                        symbol.AskPrice = tick.BestAskPrice;
                        //symbol.Volume = tick.BaseVolume; //?
                        //if (tick.LastQuantity.HasValue)
                        //    symbol.Volume = (decimal)tick.LastQuantity; //= Quoted = het volume * de prijs                                
                                                                        //symbol.Volume = tick.Volume24h; //= Base = het volume * de prijs                                

                        //bool first = true;
                        //Bewaren voor debug werkzaamheden
                        //if (first && tick.Symbol == "BTCUSDT")
                        //{
                        //    first = false;
                        //    string filename = GlobalData.GetBaseDir();
                        //    filename += @"\Bybit\";
                        //    Directory.CreateDirectory(filename);
                        //    filename += "PriceTicker.json";

                        //    string text = JsonSerializer.Serialize(data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                        //    File.WriteAllText(filename, text);
                        //}

                        //#if KUCOINDEBUG
                        //                        //Debug
                        //                        tickerIndex++;
                        //                        long unix = CandleTools.GetUnixTime(tick.Timestamp, 60);
                        //                        string filename = GlobalData.GetBaseDir() + $@"\Kucoin\Price-{data.Topic}-1m-{unix}-#{tickerIndex}.json";
                        //                        string text = System.Text.Json.JsonSerializer.Serialize(tick, new System.Text.Json.JsonSerializerOptions {
                        //                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true});
                        //                        File.WriteAllText(filename, text);
                        //#endif
                    }
                }

                if (TickerCount > 999999999)
                    Interlocked.Exchange(ref TickerCount, 0);
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;

    }

}