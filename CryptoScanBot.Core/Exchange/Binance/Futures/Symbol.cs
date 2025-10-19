using Binance.Net.Clients;
using Binance.Net.Enums;

using CryptoExchange.Net.SharedApis;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

public class Symbol() : SymbolBase(), ISymbol
{
    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new BinanceRestClient(options => { options.OutputOriginalData = true; });
                var api = client.UsdFuturesApi;
                using CryptoDatabase database = new();
                database.Open();
               

                // Tickers for the 24h volume
                GlobalData.AddTextToLogTab($"Reading symbol ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetTickersAsync() ?? throw new ExchangeException("No ticker data received");
                if (!tickerInfo.Success)
                    GlobalData.AddTextToLogTab("error getting symbol ticker {tickersInfos.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo.OriginalData, "tickers.json");

                // Create dictionary for the volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data)
                        volumeTicker.Add(tickerData.Symbol, tickerData.QuoteVolume);
                }


                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetExchangeInfoAsync() ?? throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddTextToLogTab("error getting exchangeinfo " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("Geen exchange data ontvangen (2)");
                SaveExchangeInfo(symbolInfo.OriginalData, "symbols.json");



                // Om achteraf de niet gedeactiveerde munten te melden en te deactiveren
                List<string> reportSymbols = [];
                SortedList<string, CryptoSymbol> activeSymbols = [];
                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var symbolData in symbolInfo.Data.Symbols)
                        {
                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.BaseAsset, symbolData.QuoteAsset);
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.PerpetualLinear, out CryptoSymbol? symbol))
                            {
                                //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                                //De te gebruiken precisie in prijzen
                                //symbol.BaseAssetPrecision = binanceSymbol.BaseAssetPrecision;
                                //symbol.QuoteAssetPrecision = binanceSymbol.QuoteAssetPrecision;
                                // Tijdelijke fix voor Binance.net (kan waarschijnlijk weer weg)
                                //if (binanceSymbol.MinNotionalFilter != null)
                                //    symbol.MinNotional = binanceSymbol.MinNotionalFilter.MinNotional;
                                //else
                                //    symbol.MinNotional = 0;

                                //Minimale en maximale amount voor een order (in base amount)
                                symbol!.QuantityMinimum = symbolData.LotSizeFilter?.MinQuantity ?? 0;
                                symbol.QuantityMaximum = symbolData.LotSizeFilter?.MaxQuantity ?? 0;
                                symbol.QuantityTickSize = symbolData.LotSizeFilter?.StepSize ?? 0;

                                //Minimale en maximale prijs voor een order (in base price)
                                symbol.PriceMinimum = symbolData.PriceFilter?.MinPrice ?? 0;
                                symbol.PriceMaximum = symbolData.PriceFilter?.MaxPrice ?? 0;
                                symbol.PriceTickSize = symbolData.PriceFilter?.TickSize ?? 0;

                                symbol.IsSpotTradingAllowed = true; // symbolData.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = true; // symbolData.IsMarginTradingAllowed;

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                    symbol.Volume = volume;
                                else
                                    symbol.Volume = 0;

                                if (symbolData.Status == SymbolStatus.Trading | symbolData.Status == SymbolStatus.EndOfDay)
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
                        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                        {
                            if (symbol.Status == 1 && !symbol.IsBarometerSymbol() && !activeSymbols.ContainsKey(symbol.Name))
                            {
                                if (symbol.Status != 0)
                                {
                                    symbol.Status = 0;
                                    database.Connection.Update(symbol, transaction);

                                    reportSymbols.Add(symbol.Name);
                                }
                            }
                        }
                        if (reportSymbols.Count != 0)
                        {
                            var symbols = string.Join(',', [.. reportSymbols]);
                            GlobalData.AddTextToLogTab($"{reportSymbols.Count} symbols deactivated {symbols}");
                        }


                        transaction.Commit();


                        // De nieuwe symbols toevoegen aan de lijst
                        // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                        foreach (CryptoSymbol symbol in cache)
                        {
                            GlobalData.AddSymbol(symbol);
                        }

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
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
            }

        }
    }
}