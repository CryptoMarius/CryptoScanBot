using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

using Kucoin.Net.Clients;

namespace CryptoScanner.Core.Exchange.Kucoin.Spot;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new KucoinRestClient(options => { options.OutputOriginalData = true; });
                var api = client.SpotApi;
                using CryptoDatabase database = new();
                database.Open();



                // Tickers for the 24h volume
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                KucoinWeights.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddTextToLogTab($"error getting symbol ticker {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data.Data)
                    {
                        if (tickerData.QuoteVolume.HasValue)
                        {
                            string symbolName = tickerData.Symbol.Replace("-", "");
                            volumeTicker.Add(symbolName, tickerData.QuoteVolume.Value);
                        }
                    }
                }




                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                KucoinWeights.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddTextToLogTab($"error getting exchangeinfo {symbolInfo.Error}");
                if (symbolInfo == null)
                    throw new ExchangeException("No exchange data received");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");


                if (symbolInfo.Data != null)
                {

                    // Om achteraf de niet aangeboden munten te deactiveren
                    SortedList<string, CryptoSymbol> activeSymbols = [];


                    using (var transaction = database.BeginTransaction())
                    {
                        List<CryptoSymbol> cache = [];
                        try
                        {
                            foreach (var symbolData in symbolInfo.Data)
                            {
                                SymbolInfo info = ParseSymbol(symbolData.Symbol, symbolData.BaseAsset, symbolData.QuoteAsset);
                                if (IsSymbolAccepted(exchange, info, api, TradingMode.Spot, out CryptoSymbol? symbol))
                                {
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

                                    // volume from the tickers
                                    if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                        symbol.Volume = volume;
                                    else
                                        symbol.Volume = 0;

                                    if (symbolData.EnableTrading)
                                        symbol.Status = 1;
                                    else
                                        symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                                    if (symbol.Id == 0)
                                    {
                                        database.Connection.Insert(symbol, transaction);
                                        cache.Add(symbol);
                                    }
                                    else
                                        database.Connection.Update(symbol, transaction);
                                    activeSymbols.Add(symbol.Name, symbol);
                                }
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