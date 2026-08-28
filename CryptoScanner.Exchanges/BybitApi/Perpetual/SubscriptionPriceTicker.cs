using Bybit.Net.Clients;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitApi.Perpetual;

public class SubscriptionPriceTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BybitSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BybitSocketClient)SubscriptionBundle.SocketClient).V5LinearApi.SubscribeToTickerUpdatesAsync(Symbols, data =>
        {
            if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
            {
                //GET /api/v3/ticker/24hr
                // client.Spot.SubscribeToSymbolTickerUpdates("ETHBTC", (test) => result = test);

                var tick = data.Data;
                {
                    // The exchange names the instrument, not the scanner. SymbolListExchangeName is keyed on
                    // exactly what arrives here, and it stays right when one market carries two instruments
                    // that share a scanner name.
                    if (exchange.SymbolListExchangeName.TryGetValue(tick.Symbol, out CryptoSymbol? symbol))
                    {
                        IncrementTickerCount();

                        if (!GlobalData.IsEmulatorMode)
                        {

                            //symbol.OpenPrice = tick.OpenPrice;
                            //symbol.HighPrice = tick.HighPrice;
                            //symbol.LowPrice = tick.LowPrice;
                            if (tick.LastPrice.HasValue)
                                symbol.LastPrice = (decimal)tick.LastPrice;
                            //if (tick.BestBidPrice.HasValue)
                            //    symbol.BidPrice = tick.BestBidPrice;
                            //if (tick.BestAskPrice.HasValue)
                            //    symbol.AskPrice = tick.BestAskPrice;
                            //symbol.Volume = tick.BaseVolume; //?
                            if (tick.Turnover24h.HasValue)
                                symbol.Volume = (decimal)tick.Turnover24h; //= Quoted = het volume * de prijs

                            //symbol.Volume = tick.Volume24h; //= Base = het volume * de prijs                                

                            if (tick.FundingRate.HasValue)
                                symbol.FundingRate = (decimal)tick.FundingRate;

                            //"nextFundingTime": "1673280000000",

                            // Hiermee kunnen we een "toekomstige" candle opbouwen.
                            // (maar de berekeningen verwachten dat niet en dan gaan er zaken fout)
                            // Kortom: Beslissingen op basis van niet voltooide candles moet je vermijden.
                            //try
                            //{
                            //Monitor.Enter(symbol.CandleList);await symbol.CandleLock.WaitAsync();
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
                            //    Monitor.Exit(symbol.CandleList); symbol.CandleLock.Release();
                            //}

                            //Bewaren voor debug werkzaamheden
                            //if (first && tick.Symbol == "BTCUSDT")
                            //{
                            //    first = false;
                            //    string filename = GlobalData.GetBaseDir();
                            //    filename += @"\Bybit\";
                            //    Directory.CreateDirectory(filename);
                            //    filename += "PriceTicker.json";

                            //    string text = JsonSerializer.Serialize(data, ExchangeHelper.JsonSerializerNotIndented);
                            //    File.WriteAllText(filename, text);
                            //}
                        }
                    }
                }

            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}