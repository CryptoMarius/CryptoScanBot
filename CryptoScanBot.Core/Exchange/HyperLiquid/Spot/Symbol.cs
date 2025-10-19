using CryptoExchange.Net.SharedApis;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using HyperLiquid.Net.Clients;

namespace CryptoScanBot.Core.Exchange.HyperLiquid.Spot;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new HyperLiquidRestClient(options => { options.OutputOriginalData = true; });
                var api = client.SpotApi;
                using CryptoDatabase database = new();
                database.Open();


                // tickers for volumes... (need volume because of filtered kline and price tickers)
                GlobalData.AddTextToLogTab($"Reading symbol and ticker information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                //LimitRate.WaitForFairWeight(1);
                var tickerInfo = await api.ExchangeData.GetExchangeInfoAndTickersAsync() ?? throw new ExchangeException("No ticker and symbol data received");
                if (!tickerInfo.Success)
                    GlobalData.AddTextToLogTab($"error getting symbol ticker info {tickerInfo.Error}");
                if (tickerInfo == null)
                    throw new ExchangeException("No ticker data received");
                SaveExchangeInfo(tickerInfo, "tickers.json");

                // index volume
                SortedList<string, decimal> volumeTicker = [];
                if (tickerInfo.Data != null && tickerInfo.Data != null)
                {
                    foreach (var tickerData in tickerInfo.Data.Tickers)
                    {
                        if (tickerData.Symbol != null)
                        {
                            string symbolName = tickerData.Symbol.Replace("/", "");
                            volumeTicker.Add(symbolName, tickerData.QuoteVolume);
                        }
                    }
                }



                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                //LimitRate.WaitForFairWeight(1);
                var symbolInfo = tickerInfo;
                //var symbolInfo = await api.ExchangeData.GetExchangeInfoAndTickersAsync() ?? throw new ExchangeException("No exchange data retrieved (1)");
                if (!symbolInfo.Success)
                    GlobalData.AddTextToLogTab("error getting symbol information " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data retrieved (2)");
                SaveExchangeInfo(symbolInfo, "symbols.json");


                // Om achteraf de niet aangeboden munten te deactiveren
                SortedList<string, CryptoSymbol> activeSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        foreach (var symbolData in symbolInfo.Data.ExchangeInfo.Symbols)
                        {
                            SymbolInfo info = ParseSymbol(symbolData.Name, symbolData.BaseAsset.Name, symbolData.QuoteAsset.Name);
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

                                // min, max en tick (in base amount)
                                //if (symbolData.Base.PriceDecimals)
                                //    symbol.QuantityTickSize = symbolData.LotSize.Value;
                                symbol.QuantityTickSize = symbolData.BaseAsset.QuantityDecimals;
                                //symbol.QuantityMinimum = symbolInfo.LotSizeFilter?.MinOrderQuantity ?? 0;
                                //symbol.QuantityMaximum = symbolInfo.LotSizeFilter?.MaxOrderQuantity ?? 0;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = symbolData.BaseAsset.PriceDecimals;

                                symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // volume from the tickers
                                if (volumeTicker.TryGetValue(symbol.Name, out decimal volume))
                                    symbol.Volume = volume;
                                else
                                    symbol.Volume = 0;

                                //if (symbolData.Base.QuantityDecimalsState == InstrumentState.Live)
                                symbol.Status = 1;
                                //else
                                //  symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

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
                            GlobalData.AddTextToLogTab($"{deactivated} coins deactivated");

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