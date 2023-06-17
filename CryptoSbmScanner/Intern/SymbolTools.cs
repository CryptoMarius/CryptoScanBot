using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

public class SymbolTools
{
    protected Model.CryptoExchange Exchange;
    protected CryptoSymbol Symbol;
    protected CryptoSymbolInterval SymbolInterval;
    protected CryptoInterval Interval;
    protected CryptoQuoteData QuoteData;
    protected SortedList<long, CryptoCandle> Candles;


    public SymbolTools(CryptoSymbol symbol, CryptoInterval interval)
    {
        this.Symbol = symbol;
        this.Exchange = symbol.Exchange;
        this.Interval = interval;
        this.QuoteData = symbol.QuoteData;
        this.SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        this.Candles = SymbolInterval.CandleList;
    }


    public static bool CheckValidApikey(out string reaction)
    {
        //TODO: Configuratie en security

        //1-Controleer of er wel een API key aanwezig is
        //2-Controleer of we met deze API key kunnen handelen
        //Maar hoe? (geeft uiteindelijk wel een foutmelding)

        //BinanceSocketClient.
        //BinanceClient.SetDefaultOptions(new BinanceClientOptions()
        //{
        //    ApiCredentials = new ApiCredentials(APIKEY, APISECRET),
        //    LogVerbosity = LogVerbosity.Debug,
        //    LogWriters = new List<TextWriter> { Console.Out }
        //});

        //BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
        //{
        //    ApiCredentials = new ApiCredentials(APIKEY, APISECRET),
        //    LogVerbosity = LogVerbosity.Debug,
        //    LogWriters = new List<TextWriter> { Console.Out }
        //});

        //if (false)
        //{
        //    Signal.Reaction = "No API key available";
        //    return false;
        //}

        reaction = "";
        return true;
    }


    public static bool CheckValidAmount(CryptoSymbol symbol, decimal assetAmount, out decimal amount, out string reaction)
    {
        // Koopbedrag, heeft de gebruiker een percentage of een aantal ingegeven?
        decimal percentage = symbol.QuoteData.BuyPercentage;
        bool isPercentage = percentage > 0m;
        if (isPercentage)
            amount = percentage * assetAmount / 100;
        else
            amount = symbol.QuoteData.BuyAmount;

        if (amount <= 0)
        {
            reaction = "No amount/percentage given";
            return false;
        }


        //Nog uit te zoeken!
        //04-01-2021 07:32:08 monitor FILBTC 2m 04-01-2021 07:28:00 cp=0.00068  try buy failed, result= Not enough cash available 0.00000070 < 0.00010020000
        if (assetAmount < amount)
        {
            reaction = string.Format("Not enough cash available {0} < {1}", assetAmount, amount);
            return false;
        }

        reaction = "";
        return true;
    }


    public static (bool result, decimal value) CheckPortFolio(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        decimal assetQuantity = 0;
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            if (tradeAccount.AssetList.TryGetValue(symbol.Quote, out CryptoAsset asset))
                assetQuantity = asset.Free;
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
        

        if (assetQuantity == 0)
        {
            //reaction = string.Format("No {0} cash available", Signal.Symbol.Quote);
            return (false, assetQuantity);
        }

        return (true, assetQuantity);
    }


    //public static async Task<bool> CheckDelistedCoin(CryptoSymbol symbol)
    //{

    //    // Vage comments:
    //    // Probleem delisted coins: Ververs de informatie van de exchange (1x per x minuten ofzo)
    //    // Extra: object locking zodat niet door meerdere threads tegelijk wordt gedaan
    //    // NB: Dit geld voor zowel de BUY als SELL, want we kunnen niet handelen in delisted assets.
    //    // Bij nader inzien denk ik dat de site delistedcoin (eh what?)

    //    // TODO:!!!!!
    //    // Makkelijker om gewoon de exchange info met 1 munt aan te roepen!
    //    // Dan kun je de status daarvan beoordelen (wat in principe hier de bedoeling is)
    //    // https://binance-docs.github.io/apidocs/spot/en/#exchange-information
    //    // https://api.binance.com/api/v3/exchangeInfo?symbol=BNBBTC

    //    TimeSpan span = DateTime.UtcNow.Subtract(symbol.Exchange.ExchangeInfoLastTime);
    //    if (span.TotalMinutes >= 60)
    //    {
    //        await BinanceFetchSymbols.ExecuteAsync();
    //    }

    //    // Indien delisted: Staat de muntpaar op de lijst van delisted munten->ja = signaal negeren
    //    if (symbol.Status == 0)
    //    {
    //        //reaction = string.Format("Delisted coin {0}", Signal.Symbol.Name);
    //        return false;
    //    }

    //    //reaction = "";
    //    return true;
    //}


    public static bool CheckValidMinimalVolume(CryptoSymbol symbol, out string reaction)
    {
        //Indien volume regels : Voldoende volume -> nee = signaal negeren
        //In principe is dit bij de CC signals al gedaan, maar een tweede keer kan geen kwaad
        if (!symbol.CheckValidMinimalVolume(out string text))
        {
            reaction = text;
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckValidMinimalPrice(CryptoSymbol symbol, out string reaction)
    {
        //Indien prijs regels : Prijs hoog genoeg (geen barcode chart)
        //In principe is dit bij de CC signals al gedaan, maar een tweede keer kan geen kwaad
        if (!symbol.CheckValidMinimalPrice(out string text))
        {
            reaction = text;
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckSymbolBlackListOversold(CryptoSymbol symbol, CryptoTradeDirection mode, out string reaction)
    {
        //Als de muntpaar op de zwarte lijst staat dit signaal overslagen
        //Indien blacklist: Staat de muntpaar op de blacklist -> ja = signaal negeren
        if (TradingConfig.Config[mode].InBlackList(symbol.Name))
        {
            reaction = "Symbol zit in de black list";
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckSymbolWhiteListOversold(CryptoSymbol symbol, CryptoTradeDirection mode, out string reaction)
    {
        // Als de muntpaar niet op de toegelaten lijst staat dit signaal overslagen
        // Indien whitelist: Staat de muntpaar op de whitelist -> nee = signaal negeren
        if (!TradingConfig.Config[mode].InWhiteList(symbol.Name))
        {
            reaction = "Symbol zit niet in de white list";
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckMinimumTickPercentage(CryptoSymbol symbol, out string reaction)
    {
        // Munten waarvan de ticksize percentage groot is (barcode charts)

        decimal barcodePercentage = 100 * (symbol.PriceTickSize) / (decimal)symbol.LastPrice.Value;
        if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
        {
            // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
            reaction = string.Format("{0} Tick percentage te hoog {1:N3} (removed)", symbol.Name, barcodePercentage);
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckAvailableSlotsExchange(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    {
        // Zijn er slots beschikbaar op de exchange?

        int slotsOccupied = 0;
        foreach (var positionList in tradeAccount.PositionList.Values)
        {
            foreach (var position in positionList.Values)
                slotsOccupied++;
        }

        if (slotsOccupied >= slotLimit)
        {
            reaction = string.Format("No more global-slots available {0}", slotLimit);
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckAvailableSlotsBase(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    {
        // Zijn er slots beschikbaar op de base? 

        int slotsOccupied = 0;
        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (CryptoPosition position in positionList.Values)
            {
                if (position.Symbol.Base.Equals(symbol.Base))
                    slotsOccupied++;
            }
        }

        if (slotsOccupied >= slotLimit)
        {
            reaction = string.Format("No more base-slots available {0}", slotLimit);
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckAvailableSlotsQuote(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    {
        //Zijn er slots beschikbaar op de quote? 

        int slotsOccupied = 0;
        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (CryptoPosition position in positionList.Values)
            {
                if (position.Symbol.Quote.Equals(symbol.Quote))
                    slotsOccupied++;
            }
        }

        if (slotsOccupied >= slotLimit)
        {
            reaction = string.Format("No more quote-slots available {0}", slotLimit);
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckAvailableSlotsSymbol(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    {
        // Zijn er slots beschikbaar op de munt?

        int slotsOccupied = 0;
        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (CryptoPosition position in positionList.Values)
                slotsOccupied++;
        }

        if (slotsOccupied >= slotLimit)
        {
            reaction = string.Format("No more pair-slots available {0}", slotLimit);
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckAvailableSlots(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, out string reaction)
    {
        if (!CheckAvailableSlotsExchange(tradeAccount, symbol, GlobalData.Settings.Trading.SlotsMaximalExchange, out reaction))
            return false;

        if (!CheckAvailableSlotsSymbol(tradeAccount, symbol, GlobalData.Settings.Trading.SlotsMaximalSymbol, out reaction))
            return false;

        if (!CheckAvailableSlotsBase(tradeAccount, symbol, GlobalData.Settings.Trading.SlotsMaximalBase, out reaction))
            return false;

        if (!CheckAvailableSlotsQuote(tradeAccount, symbol, symbol.QuoteData.SlotsMaximal, out reaction))
            return false;

        reaction = "";
        return true;
    }


    /// <summary>
    /// Te nieuwe coin (daar is over het algemeen weinig vertrouwen in)
    /// </summary>
    public static bool CheckNewCoin(CryptoSymbol symbol, out string reaction)
    {
        //// Zijn er candles aanwezig in het gekozen interval?
        //if (intervalCandles.Count == 0)
        //{
        //    Reaction = string.Format("No {0} candles available", Interval.Name);
        //    return false;
        //}

        //// 250 candles van het gekozen interval lijkt me een mooie hoeveelheid voor berekeningen
        //if (intervalCandles.Count < 250)
        //{
        //    Reaction = string.Format("No 250 {0} candles available", Interval.Name);
        //    return false;
        //}

        // Zijn er 30 dagen aan candles aanwezig in het dag interval?
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d);
        if (symbolPeriod.CandleList.Count < GlobalData.Settings.Signal.SymbolMustExistsDays)
        {
            reaction = string.Format("De munt is te nieuw, geen {0} dagen beschikbaar", GlobalData.Settings.Signal.SymbolMustExistsDays);
            return false;
        }

        reaction = "";
        return true;
    }



    public static bool CheckValidBarometer(CryptoQuoteData quoteData, CryptoIntervalPeriod intervalPeriod, decimal minValue, out string reaction)
    {
        if (minValue > -99m)
        {
            if (!GlobalData.IntervalListPeriod.TryGetValue(intervalPeriod, out CryptoInterval interval))
            {
                reaction = string.Format("Interval {0} bestaat niet", intervalPeriod.ToString());
                return false;
            }

            // We gaan ervan uit dat alles in 1x wordt berekend
            BarometerData barometerData = quoteData.BarometerList[(long)intervalPeriod];
            if (!barometerData.PriceBarometer.HasValue)
            {
                reaction = string.Format("Barometer {0} not calculated", interval.Name);
                return false;
            }

            barometerData = quoteData.BarometerList[(long)intervalPeriod];
            if (barometerData.PriceBarometer <= minValue)
            {
                reaction = string.Format("Barometer {0} is te laag {1} < {2}", interval.Name, barometerData.PriceBarometer?.ToString0("N2"), minValue.ToString0("N2"));
                return false;
            }
        }

        reaction = "";
        return true;
    }


    public bool BarometerIndicators(CryptoInterval interval, long candleOpenTime, out string response)
    {
        // TODO: Probleem: De barometer is afhankelijk van alle symbols en wordt x seconden NA het minuut berekend
        // dat betekend dat de laatste candle (nog) niet aanwezig hoeft te zijn (en de candleOpenTime impliceert)

        if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice, out CryptoSymbol symbol))
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                if (symbolInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle candle))
                {
                    if (candle.CandleData == null)
                    {
                        List<CryptoCandle> history = CandleIndicatorData.CalculateCandles(Symbol, Interval, candleOpenTime, out response);

                        if (history == null)
                        {
#if DEBUG
                            //if (GlobalData.Settings.Signal.LogNotEnoughCandles)
                            GlobalData.AddTextToLogTab("Analyse " + response);
#endif
                            return false;
                        }

                        // Eenmalig de indicators klaarzetten
                        CandleIndicatorData.CalculateIndicators(history);

                    }
                }
            }
        }

        response = "";
        return true;
    }
}
