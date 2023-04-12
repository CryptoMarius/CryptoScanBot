﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;

using Newtonsoft.Json;

namespace CryptoSbmScanner
{
    public class BinanceFetchSymbols
    {
        //public void SaveInformation(BinanceExchangeInfo binanceExchangeInfo)
        //{
        //    //Laad de gecachte (langere historie, minder overhad)
        //    string filename = GlobalData.GetBaseDir() + "Binance.Exchangeinfo.json";

        //    //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
        //    //{
        //    //    BinaryFormatter formatter = new BinaryFormatter();
        //    //    formatter.Serialize(writeStream, GlobalData.Settings);
        //    //    writeStream.Close();
        //    //}

        //    string text = JsonConvert.SerializeObject(binanceExchangeInfo, Formatting.Indented);
        //    //var accountFile = new FileInfo(filename);
        //    File.WriteAllText(filename, text);
        //}

        public async Task ExecuteAsync()
        {
            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                try
                {
                    GlobalData.AddTextToLogTab("Reading symbol information from Binance");
                    BinanceWeights.WaitForFairBinanceWeight(1, "exchangeinfo");

                    WebCallResult<BinanceExchangeInfo> exchangeInfo = null;
                    using (var client = new BinanceClient())
                    {
                        exchangeInfo = await client.SpotApi.ExchangeData.GetExchangeInfoAsync();

                        if (!exchangeInfo.Success)
                        {
                            GlobalData.AddTextToLogTab("error getting exchangeinfo " + exchangeInfo.Error + "\r\n");
                        }

                        //Zo af en toe komt er geen data of is de Data niet gezet.
                        //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                        if ((exchangeInfo == null) | (exchangeInfo.Data == null))
                            throw new ExchangeException("Geen exchange data ontvangen");
                    }
                    exchange.ExchangeInfoLastTime = DateTime.UtcNow;



                    //using (SqlConnection databaseThread = new SqlConnection(GlobalData.ConnectionString))
                    {
                        //databaseThread.Close();
                        //databaseThread.Open();
                        //using (var transaction = databaseThread.BeginTransaction())
                        {
                            //List<CryptoSymbol> cache = new List<CryptoSymbol>();
                            try
                            {
                                foreach (var binanceSymbol in exchangeInfo.Data.Symbols)
                                {
                                    //string coin = MatchingSymbol(item.Name);
                                    //if (coin != "")
                                    {
                                        ////
                                        //// Summary:
                                        ////     Status of a symbol
                                        //    public enum SymbolStatus
                                        //    {
                                        //        //
                                        //        // Summary:
                                        //        //     Not trading yet
                                        //        PreTrading = 0,
                                        //        //
                                        //        // Summary:
                                        //        //     Trading
                                        //        Trading = 1,
                                        //        //
                                        //        // Summary:
                                        //        //     No longer trading
                                        //        PostTrading = 2,
                                        //        //
                                        //        // Summary:
                                        //        //     Not trading
                                        //        EndOfDay = 3,
                                        //        //
                                        //        // Summary:
                                        //        //     Halted
                                        //        Halt = 4,
                                        //        AuctionMatch = 5,
                                        //        Break = 6
                                        //    }

                                        //Het is erg belangrijk om de delisted munten zo snel mogelijk te detecteren.
                                        //(ik heb wat slechte ervaringen met de Altrady bot die op paniek pieken handelt)

                                        CryptoSymbol symbol;
                                        if (!exchange.SymbolListName.TryGetValue(binanceSymbol.Name, out symbol))
                                        {
                                            symbol = new CryptoSymbol();
                                            symbol.Exchange = exchange;
                                            symbol.Name = binanceSymbol.Name;
                                            symbol.Base = binanceSymbol.BaseAsset;
                                            symbol.Quote = binanceSymbol.QuoteAsset;
                                            symbol.Status = 1;
                                        }

                                        //Tijdelijk alles overnemen (vanwege into nieuwe velden)
                                        //De te gebruiken precisie in prijzen
                                        symbol.BaseAssetPrecision = binanceSymbol.BaseAssetPrecision;
                                        symbol.QuoteAssetPrecision = binanceSymbol.QuoteAssetPrecision;
                                        if (binanceSymbol.MinNotionalFilter != null)
                                            symbol.MinNotional = binanceSymbol.MinNotionalFilter.MinNotional;
                                        else
                                            symbol.MinNotional = 0;

                                        //Minimale en maximale amount voor een order (in base amount)
                                        symbol.QuantityMinimum = binanceSymbol.LotSizeFilter.MinQuantity;
                                        symbol.QuantityMaximum = binanceSymbol.LotSizeFilter.MaxQuantity;
                                        symbol.QuantityTickSize = binanceSymbol.LotSizeFilter.StepSize;

                                        //Minimale en maximale prijs voor een order (in base price)
                                        symbol.PriceMinimum = binanceSymbol.PriceFilter.MinPrice;
                                        symbol.PriceMaximum = binanceSymbol.PriceFilter.MaxPrice;
                                        symbol.PriceTickSize = binanceSymbol.PriceFilter.TickSize;

                                        symbol.IsSpotTradingAllowed = binanceSymbol.IsSpotTradingAllowed;
                                        symbol.IsMarginTradingAllowed = binanceSymbol.IsMarginTradingAllowed;

                                        if ((binanceSymbol.Status == SymbolStatus.Trading) | (binanceSymbol.Status == SymbolStatus.EndOfDay))
                                            symbol.Status = 1;
                                        else
                                            symbol.Status = 0; //Zet de status door (PreTrading, PostTrading of Halt)

                                        GlobalData.AddSymbol(symbol);
                                    }
                                }

                                //SaveInformation(exchangeInfo.Data);




                                // De assets toevoegen aan de lijst (als ze niet bestaan)
                                // (omdat de symbols pas tijdens de BulkInsert een id krijgen)
                                //foreach (CryptoSymbol symbol in cache)
                                //{
                                //        GlobalData.AddSymbol(symbol);
                                //}

                                // De (nieuwe)muntparen toevoegen aan de userinterface
                                GlobalData.SymbolsHaveChanged("");


                                // De index lijsten opbouwen (een gedeelte van de ~1700 munten)
                                foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
                                {
                                    // Lock (zie onder andere de BarometerTools)
                                    Monitor.Enter(quoteData.SymbolList);
                                    try
                                    {
                                        quoteData.SymbolList.Clear();
                                        foreach (var symbol in exchange.SymbolListName.Values)
                                        {
                                            if ((symbol.Quote.Equals(quoteData.Name)) && (symbol.Status == 1) && (!symbol.IsBarometerSymbol()))
                                            {
                                                quoteData.SymbolList.Add(symbol);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        Monitor.Exit(quoteData.SymbolList);
                                    }
                                }

                            }
                            catch (Exception error)
                            {
                                GlobalData.Logger.Error(error);
                                GlobalData.AddTextToLogTab(error.ToString());
                                //transaction.Rollback();
                                throw;
                            }
                        }
                        //databaseThread.Close();
                    }
                }
                catch (Exception error)
                {
                    GlobalData.Logger.Error(error);
                    GlobalData.AddTextToLogTab(error.ToString());
                }

            }
        }
    }
}