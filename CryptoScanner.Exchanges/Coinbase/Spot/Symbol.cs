using Coinbase.Net.Clients;
using Coinbase.Net.Enums;

using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.Coinbase.Spot;

public class Symbol() : SymbolBase(), ISymbol
{

    public async Task GetSymbolsAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new CoinbaseRestClient(options => { options.OutputOriginalData = true; });
                var api = client.AdvancedTradeApi;

                using CryptoDatabase database = new();
                database.Open();


                // No separate ticker call for the 24 hour volume. The obvious candidate,
                // GetBookTickersAsync, is a private endpoint (/api/v3/brokerage/best_bid_ask) with no
                // public variant, so it fails outright without api credentials - and it reports a best
                // ask price, not a volume. The symbol definitions below carry the volume themselves.
                GlobalData.AddTextToLogTab($"Reading symbol information from {ExchangeBase.ExchangeOptions.ExchangeName}");
                //LimitRate.WaitForFairWeight(1);
                var symbolInfo = await api.ExchangeData.GetSymbolsAsync(SymbolType.Spot) ??
                    throw new ExchangeException("No symbol data received");
                if (!symbolInfo.Success)
                    GlobalData.AddErrorToLogTab("error getting exchangeinfo " + symbolInfo.Error);
                if (symbolInfo.Data == null)
                    throw new ExchangeException("No exchange data received (2)");
                SaveExchangeInfo(symbolInfo, "symbols.json");


                // Track which symbols are still active, to deactivate the ones we no longer follow
                SortedList<string, CryptoSymbol> activeSymbols = [];


                using (var transaction = database.BeginTransaction())
                {
                    List<CryptoSymbol> cache = [];
                    try
                    {
                        //BybitSpotSymbol
                        //HttpResult<BybitResponse<BybitSpotSymbol>> x;
                        foreach (var symbolData in symbolInfo.Data)
                        {
                            SymbolInfo info = ParseSymbol(symbolData.Symbol, symbolData.BaseAsset, symbolData.QuoteAsset, ProductOfExchange(exchange));
                            if (IsSymbolAccepted(exchange, info, api, TradingMode.Spot, out CryptoSymbol? symbol))
                            {
                                //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                // https://api.bybit.com/v5/market/instruments-info?category=spot
                                /*
                                    {
                                    "ExchangeSymbol": "HFTUSDT",
                                    "Base": "HFT",
                                    "Quote": "USDT",
                                    "Status": 1,
                                    "MarginTrading": 2,
                                    "Innovation": false,
                                    "LotSizeFilter": {
                                        "BasePrecision": 0.01,
                                        "QuotePrecision": 0.000001,
                                        "MinOrderQuantity": 2.5,
                                        "MaxOrderQuantity": 738825.267824,
                                        "MinOrderValue": 1,
                                        "MaxOrderValue": 200000
                                    },
                                    "PriceFilter": {
                                        "TickSize": 0.0001
                                    },
                                    "PricePercentageFilter": {
                                        "LimitPricePercentageLimit": 0.03,
                                        "MarketPricePercentageLimit": 0.03
                                    }
                                    },
                               
                                enzovoort..
                                */



                                //Temporarily copy everything (because of the new fields)
                                //The precision to use for prices
                                //symbol.BaseAssetPrecision = binanceSymbol.LotSizeFilter.BasePrecision.ToString().Length - 2;
                                //if (symbol.BaseAssetPrecision <= 0)
                                //    symbol.BaseAssetPrecision = 8;
                                //symbol.QuoteAssetPrecision = binanceSymbol.LotSizeFilter.QuotePrecision.ToString().Length - 2;
                                //if (symbol.QuoteAssetPrecision <= 0)
                                //    symbol.QuoteAssetPrecision = 8;
                                //symbol.MinNotional = binanceSymbol.MinNotional; // ????

                                // min, max en tick (in base amount)
                                symbol!.QuantityTickSize = symbolData.QuantityStep;
                                symbol.QuantityMinimum = symbolData.MinOrderQuantity;
                                symbol.QuantityMaximum = symbolData.MaxOrderQuantity;

                                //symbol.QuoteValueMinimum = symbolInfo.LotSizeFilter?.MinOrderValue ?? 0;
                                //symbol.QuoteValueMaximum = symbolInfo.LotSizeFilter?.MaxOrderValue ?? 0;


                                // The minimum and maximum price for an order (in base price)
                                // The definitions do contain a minPrice and a maxPrice, but they are not filled
                                // (which has consequences for the Clamp, which does expect values)
                                //symbol.PriceMinimum = symbolInfo.LotSizeFilter.MinOrderValue;
                                //symbol.PriceMaximum = symbolInfo.LotSizeFilter.MaxOrderValue;

                                symbol.PriceTickSize = symbolData.PriceStep;

                                //symbol.IsSpotTradingAllowed = true; // binanceSymbol.IsSpotTradingAllowed;
                                //symbol.IsMarginTradingAllowed = false; // binanceSymbol.MarginTading; ???

                                // 24 hour volume, straight from the symbol definition. The scanner works
                                // in quote volume, which is exactly what approximate_quote_24h_volume is;
                                // Volume24h next to it is the volume in the base asset.
                                // The field is empty for the symbols that had no trades at all.
                                symbol.Volume = (double)(symbolData.ApproximateQuote24hVolume ?? 0);

                                if (symbolData.SymbolStatus == SymbolStatus.Online)
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; //Pass the status on (PreTrading, PostTrading or Halt)

                                if (symbol.Id == 0)
                                {
                                    database.Connection.Insert(symbol, transaction);
                                    cache.Add(symbol);
                                }
                                else
                                    database.Connection.Update(symbol, transaction);
                                activeSymbols[symbol.Name] = symbol;
                            }
                        }

                        // Deactivate the symbols who have disappeared
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
                            GlobalData.AddTextToLogTab($"{deactivated} munten gedeactiveerd");

                        transaction.Commit();


                        // Add the new symbols to the list
                        // (because the symbols only get an id during the BulkInsert)
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