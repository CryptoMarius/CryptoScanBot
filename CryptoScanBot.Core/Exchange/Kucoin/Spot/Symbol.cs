using System.Text.Json;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Kucoin.Net.Clients;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                KucoinWeights.WaitForFairWeight(1);

                using CryptoDatabase database = new();
                database.Open();

                using var client = new KucoinRestClient();

                /*
                "Data": [
                {
                    "Symbol": "BTC-USDT",
                    "Name": "BTC-USDT",
                    "Market": "USDS",
                    "BaseAsset": "BTC",
                    "QuoteAsset": "USDT",
                    "BaseMinQuantity": 0.00001,
                    "QuoteMinQuantity": 0.01,
                    "BaseMaxQuantity": 10000000000,
                    "QuoteMaxQuantity": 99999999,
                    "BaseIncrement": 0.00000001,
                    "QuoteIncrement": 0.000001,
                    "PriceIncrement": 0.1,
                    "PriceLimitRate": 0.1,
                    "FeeAsset": "USDT",
                    "IsMarginEnabled": true,
                    "EnableTrading": true,
                    "MinFunds": 0.1
                },
                ...
                ]
                */

                var exchangeData = await client.SpotApi.ExchangeData.GetSymbolsAsync();
                if (!exchangeData.Success)
                    GlobalData.AddTextToLogTab($"error getting exchangeinfo {exchangeData.Error}");
                if (exchangeData == null)
                    throw new ExchangeException("No exchange data received");

                // Save for debug purposes
                {
                    string filename = $@"{GlobalData.GetBaseDir()}\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "symbols.json";

                    string text = JsonSerializer.Serialize(exchangeData, JsonTools.JsonSerializerIndented);
                    File.WriteAllText(filename, text);
                }


                /* ticker
                 
                  {
                    "Symbol": "BTC-USDT",
                    "SymbolName": "BTC-USDT",
                    "BestAskPrice": 29435.3,
                    "BestBidPrice": 29435.2,
                    "ChangePercentage": 0.0018,
                    "ChangePrice": 55.4,
                    "HighPrice": 29472.5,
                    "LowPrice": 29100.5,
                    "Volume": 1143.99893497,
                    "QuoteVolume": 33549185.280333833,
                    "LastPrice": 29435.2,
                    "AveragePrice": 29410.48473908,
                    "TakerFeeRate": 0.001,
                    "MakerFeeRate": 0.001,
                    "TakerCoefficient": 1,
                    "MakerCoefficient": 1
                  },
                */

                // tickers for volumes... (need volume because of filtered kline and price tickers)
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                var tickerData = await client.SpotApi.ExchangeData.GetTickersAsync();
                if (!tickerData.Success)
                    GlobalData.AddTextToLogTab("error getting symbol ticker {tickersInfos.Error}");
                if (tickerData == null)
                    throw new ExchangeException("No ticker data received");

                // Save for debug purposes
                {
                    string filename = $@"{GlobalData.GetBaseDir()}\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "tickers.json";

                    string text = JsonSerializer.Serialize(tickerData, JsonTools.JsonSerializerIndented);
                    File.WriteAllText(filename, text);
                }

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerData.Data != null && tickerData.Data.Data != null)
                {
                    foreach (var tickerInfo in tickerData.Data.Data)
                    {
                        if (tickerInfo.QuoteVolume.HasValue)
                        {
                            string symbolName = tickerInfo.Symbol.Replace("-", "");
                            volumeTicker.Add(symbolName, tickerInfo.QuoteVolume.Value);
                        }
                    }
                }


                if (exchangeData.Data != null)
                {

                    // Om achteraf de niet aangeboden munten te deactiveren
                    SortedList<string, CryptoSymbol> activeSymbols = [];


                    using (var transaction = database.BeginTransaction())
                    {
                        List<CryptoSymbol> cache = [];
                        try
                        {
                            foreach (var symbolData in exchangeData.Data)
                            {
                                // https://docs.kucoin.com/#symbols-amp-ticker
                                // https://api.kucoin.com/api/v1/symbols
                                //Eventueel symbol toevoegen
                                string symbolName = symbolData.Name.Replace("-", "");

                                if (symbolName != symbolData.BaseAsset + symbolData.QuoteAsset)
                                {
                                    GlobalData.AddTextToLogTab($"Ignoring symbol {symbolName} {symbolData.BaseAsset} {symbolData.QuoteAsset} weird name?");
                                    continue;
                                }

                                if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                                {
                                    var quoteData = GlobalData.AddQuoteData(symbolData.QuoteAsset);

                                    symbol = new()
                                    {
                                        Exchange = exchange,
                                        ExchangeId = exchange.Id,
                                        Name = symbolName,
                                        Base = symbolData.BaseAsset,
                                        Quote = symbolData.QuoteAsset,
                                        QuoteData = quoteData,
                                        Status = 1,
                                    };
                                }

                                //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                                //De te gebruiken precisie in prijzen
                                //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                                //if (symbol.BaseAssetPrecision <= 0)
                                //    symbol.BaseAssetPrecision = 8;
                                //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                                //if (symbol.QuoteAssetPrecision <= 0)
                                //    symbol.QuoteAssetPrecision = 8;
                                //symbol.MinNotional = binanceSymbol.MinNotional; // ????

                                //Minimale en maximale amount voor een order (in base amount)
                                symbol.QuantityMinimum = symbolData.BaseMinQuantity;
                                symbol.QuantityMaximum = symbolData.BaseMaxQuantity; //baseMinSize
                                                                                     // Dit klopt niet, deze heeft wederom effect op de Clamp routine!
                                symbol.QuantityTickSize = symbolData.BaseIncrement;

                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = niet aanwezig! binanceSymbol.PriceFilter.min;
                                //symbol.PriceMaximum = niet aanwezig! binanceSymbol.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = symbolData.PriceIncrement;

                                symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                if (symbolData.EnableTrading)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                    symbol.Volume = volume;

                                if (symbol.Id == 0)
                                {
                                    database.Connection.Insert(symbol, transaction);
                                    cache.Add(symbol);
                                }
                                else
                                    database.Connection.Update(symbol, transaction);
                                activeSymbols.Add(symbol.Name, symbol);
                            }

                            // Deactiveer de munten die niet meer voorkomen
                            int deactivated = 0;
                            foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                            {
                                if (symbol.Status == 1 && !symbol.IsBarometerSymbol() && !activeSymbols.ContainsKey(symbol.Name))
                                {
                                    deactivated++;
                                    symbol.Status = 0;
                                    database.Connection.Update(symbol, transaction);
                                }
                            }
                            if (deactivated > 0)
                                GlobalData.AddTextToLogTab($"{deactivated} symbols deactivated");


                            // De nieuwe symbols toevoegen aan de lijst
                            // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                            foreach (CryptoSymbol symbol in cache)
                            {
                                GlobalData.AddSymbol(symbol);
                            }


                            transaction.Commit();
                        }
                        catch (Exception error)
                        {
                            ScannerLog.Logger.Error(error, "");
                            GlobalData.AddTextToLogTab(error.ToString());
                            transaction.Rollback();
                            throw;
                        }
                    }

                    exchange.LastTimeFetched = DateTime.UtcNow;
                    database.Connection.Update(exchange);
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}