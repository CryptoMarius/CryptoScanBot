using System.Text.Json;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper.Contrib.Extensions;
using Kucoin.Net.Clients;

namespace ExchangeTest.Exchange.Kucoin.Futures;

public class Symbols
{
    const string ExchangeName = "Kucoin Futures";


    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out CryptoScanBot.Core.Model.CryptoExchange? exchange))
        {
            try
            {
                using var client = new KucoinRestClient();
                var exchangeInfo = await client.FuturesApi.ExchangeData.GetOpenContractsAsync() ?? throw new ExchangeException("Geen exchange data ontvangen (1)");
                if (!exchangeInfo.Success)
                    GlobalData.AddTextToLogTab($"error getting exchangeinfo {exchangeInfo.Error}", true);

                // Save for debugging
                {
                    string filename = GlobalData.GetBaseDir();
                    filename += $@"\{exchange.Name}\";
                    Directory.CreateDirectory(filename);
                    filename += "symbols.json";

                    string text = JsonSerializer.Serialize(exchangeInfo, GlobalData.JsonSerializerIndented);
                    File.WriteAllText(filename, text);
                }



                //if (exchangeInfo.Data == null)
                //    throw new ExchangeException($"Geen exchange data ontvangen (2) {exchangeInfo.Error}");
                if (exchangeInfo.Data != null)
                {
                    using CryptoDatabase database = new();
                    database.Open();

                    // Om achteraf de niet aangeboden munten te deactiveren
                    SortedList<string, CryptoSymbol> activeSymbols = [];


                    using (var transaction = database.BeginTransaction())
                    {
                        List<CryptoSymbol> cache = [];
                        try
                        {
                            foreach (var symbolData in exchangeInfo.Data)
                            {
                                // https://docs.kucoin.com/#symbols-amp-ticker
                                // https://api.kucoin.com/api/v1/symbols

                                GlobalData.AddTextToLogTab($"symbol {symbolData.Symbol} {symbolData.BaseAsset} {symbolData.QuoteAsset}");

                                // Kucoin has a different (weird?) symbolname "[Base][Quote]M"
                                string symbolName = symbolData.Symbol; //symbolData.BaseAsset + symbolData.QuoteAsset; 

                                if (!exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                                {
                                    symbol = new()
                                    {
                                        Exchange = exchange,
                                        ExchangeId = exchange.Id,
                                        Base = symbolData.BaseAsset,
                                        Quote = symbolData.QuoteAsset,
                                        Status = 1,
                                    };
                                }
                                symbol.Name = symbolName; //symbolData.Symbol, BaseQuoteM?

                                // Minimale en maximale amount voor een order (in base amount)
                                symbol.QuantityMinimum = symbolData.LotSize;
                                symbol.QuantityMaximum = symbolData.MaxOrderQuantity;
                                // Dit klopt niet, deze heeft wederom effect op de Clamp routine!
                                symbol.QuantityTickSize = 0; // symbolData.?;

                                // De minimale en maximale prijs voor een order (in base price)
                                // In de definities is wel een minPrice en maxprice aanwezig, maar die is niet gevuld
                                // (dat heeft consequenties voro de werking van de Clamp die wel waarden verwacht)
                                symbol.PriceMinimum = 0; // symbolData.?;
                                symbol.PriceMaximum = symbolData.MaxPrice;
                                symbol.PriceTickSize = symbolData.TickSize; //?

                                symbol.Volume = symbolData.Volume24H; // or turnoverOf24h?


                                symbol.IsSpotTradingAllowed = true;
                                symbol.IsMarginTradingAllowed = false;

                                if (symbolData.FundingFeeRate.HasValue)
                                    symbol.FundingRate = (decimal)symbolData.FundingFeeRate;

                                if (symbolData.Status.Equals("Open"))
                                    symbol.Status = 1;
                                else
                                    symbol.Status = 0; // Zet de status door (PreTrading, PostTrading of Halt)

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
                                GlobalData.AddTextToLogTab($"{deactivated} munten gedeactiveerd");


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