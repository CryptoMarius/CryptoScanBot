using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trader;

namespace CryptoScanBot.Core.Intern;

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
        Symbol = symbol;
        Exchange = symbol.Exchange;
        Interval = interval;
        QuoteData = symbol.QuoteData;
        SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        Candles = SymbolInterval.CandleList;
    }


    //public static bool CheckValidApikey(out string reaction)

    //{
    //    reaction = "";
    //    //TODO: Configuratie en security

    //    //1-Controleer of er wel een API key aanwezig is
    //    //2-Controleer of we met deze API key kunnen handelen
    //    //Maar hoe? (geeft uiteindelijk wel een foutmelding)

    //    //BinanceSocketClient.
    //    //BinanceClient.SetDefaultOptions(new BinanceClientOptions()
    //    //{
    //    //    ApiCredentials = new ApiCredentials(APIKEY, APISECRET),
    //    //    LogVerbosity = LogVerbosity.Debug,
    //    //    LogWriters = new List<TextWriter> { Console.Out }
    //    //});

    //    //BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
    //    //{
    //    //    ApiCredentials = new ApiCredentials(APIKEY, APISECRET),
    //    //    LogVerbosity = LogVerbosity.Debug,
    //    //    LogWriters = new List<TextWriter> { Console.Out }
    //    //});

    //    if (GlobalData.TradingApi.Key == "" || GlobalData.TradingApi.Secret == "")
    //    {
    //        reaction = "No API credentials available";
    //        return false;
    //    }

    //    return true;
    //}



    public static bool CheckValidMinimalVolume(CryptoSymbol symbol, long candleStart, int candleDuration, out string reaction)
    {
        //Indien volume regels : Voldoende volume -> nee = signaal negeren
        //In principe is dit bij de CC signals al gedaan, maar een tweede keer kan geen kwaad
        if (!symbol.CheckValidMinimalVolume(candleStart, candleDuration, out string text))
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


    public static bool CheckSymbolBlackListOversold(CryptoSymbol symbol, CryptoTradeSide mode, out string reaction)
    {
        //Als de muntpaar op de zwarte lijst staat dit signaal overslagen
        //Indien blacklist: Staat de muntpaar op de blacklist -> ja = signaal negeren
        if (TradingConfig.Signals[mode].InBlackList(symbol.Name) == MatchBlackAndWhiteList.Present)
        {
            reaction = "Symbol zit in de black list";
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckSymbolWhiteListOversold(CryptoSymbol symbol, CryptoTradeSide mode, out string reaction)
    {
        // Als de muntpaar niet op de toegelaten lijst staat dit signaal overslagen
        // Indien whitelist: Staat de muntpaar op de whitelist -> nee = signaal negeren
        if (TradingConfig.Signals[mode].InWhiteList(symbol.Name) == MatchBlackAndWhiteList.NotPresent)
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

        decimal barcodePercentage = 100 * symbol.PriceTickSize / symbol.LastPrice.Value;
        if (barcodePercentage > GlobalData.Settings.Signal.MinimumTickPercentage)
        {
            // Er zijn nogal wat van die flut munten, laat de tekst maar achterwege
            reaction = string.Format("{0} Tick percentage te hoog {1:N3} (removed)", symbol.Name, barcodePercentage);
            return false;
        }

        reaction = "";
        return true;
    }


    //public static bool CheckAvailableSlotsExchange(CryptoTradeAccount tradeAccount, int slotLimit, out string reaction)
    //{
    //    // Zijn er slots beschikbaar op de exchange?

    //    int slotsOccupied = 0;
    //    foreach (var positionList in tradeAccount.PositionList.Values)
    //    {
    //        slotsOccupied += positionList.Count;
    //    }

    //    if (slotsOccupied >= slotLimit)
    //    {
    //        reaction = string.Format("No more global-slots available {0}", slotLimit);
    //        return false;
    //    }

    //    reaction = "";
    //    return true;
    //}


    //public static bool CheckAvailableSlotsBase(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    //{
    //    // Zijn er slots beschikbaar op de base? 

    //    int slotsOccupied = 0;
    //    if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
    //    {
    //        foreach (CryptoPosition position in positionList.Values)
    //        {
    //            if (position.Symbol.Base.Equals(symbol.Base))
    //                slotsOccupied++;
    //        }
    //    }

    //    if (slotsOccupied >= slotLimit)
    //    {
    //        reaction = string.Format("No more base-slots available {0}", slotLimit);
    //        return false;
    //    }

    //    reaction = "";
    //    return true;
    //}


    //public static bool CheckAvailableSlotsQuote(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    //{
    //    // Zijn er slots beschikbaar?

    //    int slotsOccupied = 0;
    //    if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
    //    {
    //        foreach (CryptoPosition position in positionList.Values)
    //        {
    //            if (position.Symbol.Quote.Equals(symbol.Quote))
    //                slotsOccupied++;
    //        }
    //    }

    //    if (slotsOccupied >= slotLimit)
    //    {
    //        reaction = string.Format("No more quote-slots available {0}", slotLimit);
    //        return false;
    //    }

    //    reaction = "";
    //    return true;
    //}


    /// <summary>
    /// Is er nog een slot beschikbaar (het aantal openstaande posities in 1 munt)
    /// </summary>
    public static bool CheckAvailableSlotsSymbol(CryptoAccount tradeAccount, CryptoSymbol symbol, int slotLimit, out string reaction)
    {
        // Zijn er slots beschikbaar?

        int slotsOccupied = 0;
        if (tradeAccount.Data.PositionList.TryGetValue(symbol.Name, out _))
        {
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

    /// <summary>
    /// Is er nog een slot beschikbaar (het aantal openstaande posities ten opzichte van de configuratie)
    /// </summary>
    public static bool CheckAvailableSlotsLongShort(CryptoAccount tradeAccount, CryptoTradeSide side, int slotLimit, out string reaction)
    {
        int slotsOccupied = 0;
        foreach (var position in tradeAccount.Data.PositionList.Values)
        {
            if (position.Side == side)
                slotsOccupied++;
        }

        if (slotsOccupied >= slotLimit)
        {
            reaction = $"No more slots available {slotLimit} for {side}";
            return false;
        }

        reaction = "";
        return true;
    }


    public static bool CheckAvailableSlots(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTradeSide side, out string reaction)
    {
        //if (!CheckAvailableSlotsExchange(tradeAccount, GlobalData.Settings.Trading.SlotsMaximalExchange, out reaction))
        //    return false;

        //if (!CheckAvailableSlotsSymbol(tradeAccount, symbol, GlobalData.Settings.Trading.SlotsMaximalSymbol, out reaction))
        //    return false;
        if (!CheckAvailableSlotsSymbol(tradeAccount, symbol, 1, out reaction))
            return false;

        //if (!CheckAvailableSlotsBase(tradeAccount, symbol, GlobalData.Settings.Trading.SlotsMaximalBase, out reaction))
        //    return false;

        //if (!CheckAvailableSlotsQuote(tradeAccount, symbol, symbol.QuoteData.SlotsMaximal, out reaction))
        //    return false;

        if (side == CryptoTradeSide.Long && !CheckAvailableSlotsLongShort(tradeAccount, side, GlobalData.Settings.Trading.SlotsMaximalLong, out reaction))
            return false;

        if (side == CryptoTradeSide.Short && !CheckAvailableSlotsLongShort(tradeAccount, side, GlobalData.Settings.Trading.SlotsMaximalShort, out reaction))
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
            reaction = $"De munt is te nieuw, geen {GlobalData.Settings.Signal.SymbolMustExistsDays} dagen beschikbaar";
            return false;
        }

        reaction = "";
        return true;
    }


    public bool BarometerIndicators(CryptoInterval interval, long candleOpenTime, out string response)
    {
        // TODO: Probleem: De barometer is afhankelijk van alle symbols en wordt x seconden NA het minuut berekend
        // dat betekend dat de laatste candle (nog) niet aanwezig hoeft te zijn (en de candleOpenTime impliceert)

        var exchange = GlobalData.Settings.General.Exchange;
        if (exchange != null)
        {
            if (exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice, out CryptoSymbol? symbol))
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                if (symbolInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle? candle))
                {
                    if (candle.CandleData == null)
                    {
                        List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(Symbol, Interval, candleOpenTime, out response);

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
