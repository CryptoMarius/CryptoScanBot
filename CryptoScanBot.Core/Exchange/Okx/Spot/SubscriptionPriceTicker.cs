using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Market;

namespace CryptoScanBot.Core.Exchange.Okx.Spot;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        // 1 symbol per ticker
        string symbolName = "";
        List<string> symbols = [];
        foreach (var symbol in SymbolList)
        {
            //Symbol = symbol;
            if (symbolName == "")
                symbolName = symbol.Base + "-" + symbol.Quote;
            else
                symbolName += "," + symbol.Base + "-" + symbol.Quote;
            symbols.Add(symbolName);
        }


        TickerGroup!.SocketClient ??= new OKXSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = 
            await ((OKXSocketClient)TickerGroup.SocketClient).UnifiedApi.ExchangeData.SubscribeToTickerUpdatesAsync(symbols, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                var tick = data.Data;
                {
                    string symbolName = tick.Symbol.Replace("-", "");
                    if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                    {
                        Interlocked.Increment(ref TickerCount);

                        if (!GlobalData.BackTest)
                        {

                            // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
                            //symbol.OpenPrice = tick.OpenPrice;
                            //symbol.HighPrice = tick.HighPrice;
                            //symbol.LowPrice = tick.LowPrice;
                            //if (tick.LastPrice.HasValue)
                            symbol.LastPrice = tick.LastPrice;
                            //if (tick.BestBidPrice.HasValue)
                            //symbol.BidPrice = tick.BestBidPrice;
                            //if (tick.BestAskPrice.HasValue)
                            //    symbol.AskPrice = tick.BestAskPrice;
                            //symbol.Volume = tick.BaseVolume; //?
                            //if (tick.Turnover24h.HasValue)
                            symbol.Volume = tick.QuoteVolume; //= Quoted = het volume * de prijs

                            //symbol.Volume = tick.Volume24h; //= Base = het volume * de prijs                                


                            // Hiermee kunnen we een "toekomstige" candle opbouwen.
                            // (maar de berekeningen verwachten dat niet en dan gaan er zaken fout)
                            // Kortom: Beslissingen op basis van niet voltooide candles moet je vermijden.
                            //try
                            //{
                            //Monitor.Enter(symbol.CandleList); await symbol.CandleLock.WaitAsync();
                            //try
                            //{
                            //    //symbol.HandleExchangeMiniTick(GlobalData.Settings, symbol, tick);
                            //}
                            //catch (Exception error)
                            //{
                            //    GlobalData.AddTextToLogTab(error.ToString());
                            //}
                            //}
                            //finally
                            //{
                            //    Monitor.Exit(symbol.CandleList);
                            //}

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
                        }
                    }
                }

                if (TickerCount > 999999999)
                    Interlocked.Exchange(ref TickerCount, 0);
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
