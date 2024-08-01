using System.Text;

using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;
using Skender.Stock.Indicators;

namespace CryptoScanBot.BackTest;

public class BackTest
{
    //public static int UniqueId = 0;



    //private readonly Model.CryptoExchange Exchange;
    private readonly CryptoSymbol Symbol;
    //private readonly CryptoBackConfig Config;
    
    private readonly CryptoInterval Interval;
    private readonly CryptoSymbolInterval SymbolInterval;
    private readonly SortedList<long, CryptoCandle> Candles;

    private readonly CryptoInterval Interval1m;
    private readonly CryptoSymbolInterval SymbolInterval1m;
    private readonly SortedList<long, CryptoCandle> Candles1m;

    public StringBuilder Log = new();
    public string HeaderText;
    public string Outcome = "";
    public CryptoBackTestResults Results;

    public BackTest(CryptoSymbol symbol, CryptoInterval interval1m, CryptoInterval interval, CryptoBackConfig config)
    {
        Symbol = symbol;
        //Exchange = symbol.Exchange;
        Interval = interval;
        Interval1m = interval1m;
        //Config = config;

        SymbolInterval1m = Symbol.GetSymbolInterval(Interval1m.IntervalPeriod);
        Candles1m = SymbolInterval1m.CandleList;

        SymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        Candles = SymbolInterval.CandleList;

        Results = new(config.QuoteMarket, symbol, interval, config);
    }


     
    /// <summary>
    /// Geheugen teruggeven (anders blijft het ook maar hangen)
    /// </summary>
    public void ClearCandles()
    {
        Symbol.LastTradeDate = null;
        foreach (CryptoInterval intervalX in GlobalData.IntervalListPeriod.Values)
            Symbol.GetSymbolInterval(intervalX.IntervalPeriod).CandleList.Clear();
    }





    /// <summary>
    /// In bulk de indicator waarden invullen
    /// </summary>
    public static void CalculateAllIndicatorsViaSkender(List<CryptoCandle> history)
    {
        // Overslaan indien het gevuld is (meerdere aanroepen)
        CryptoCandle candle = history.Last();
        if (candle.CandleData != null)
            return;

        //List<TemaResult> temaList = (List<TemaResult>)Indicator.GetTema(history, 5);

        // Dit werkt niet voor de EMA (en psar)!
        //List<EmaResult> emaList8 = (List<EmaResult>)history.GetEma(8);
        List<EmaResult> emaList20 = (List<EmaResult>)history.GetEma(20);
        List<EmaResult> emaList50 = (List<EmaResult>)history.GetEma(50);
        //List<EmaResult> emaList100 = (List<EmaResult>)history.GetEma(100);
        List<EmaResult> emaList200 = (List<EmaResult>)history.GetEma(200);
        List<SlopeResult> slopeEma20List = (List<SlopeResult>)emaList20.GetSlope(3);
        List<SlopeResult> slopeEma50List = (List<SlopeResult>)emaList50.GetSlope(3);

        //List<SmaResult> smaList8 = (List<SmaResult>)Indicator.GetSma(history, 8);
        List<SmaResult> smaList20 = (List<SmaResult>)Indicator.GetSma(history, 20);
        List<SmaResult> smaList50 = (List<SmaResult>)history.GetSma(50);
        //List<SmaResult> smaList100 = (List<SmaResult>)Indicator.GetSma(history, 100);
        List<SmaResult> smaList200 = (List<SmaResult>)history.GetSma(200);
        List<SlopeResult> slopeSma20List = (List<SlopeResult>)smaList20.GetSlope(3);
        List<SlopeResult> slopeSma50List = (List<SlopeResult>)smaList50.GetSlope(3);

        // Berekend vanuit de EMA 20 en de upper en lowerband ontstaat uit 2x de ATR
        //List<KeltnerResult> keltnerList = (List<KeltnerResult>)Indicator.GetKeltner(history, 20, 1);

        //List<AtrResult> atrList = (List<AtrResult>)Indicator.GetAtr(history);
        List<RsiResult> rsiList = (List<RsiResult>)history.GetRsi();
        List<MacdResult> macdList = (List<MacdResult>)history.GetMacd();
        List<MacdResult> macdLtList = (List<MacdResult>)history.GetMacd(34, 144);

        List<SlopeResult> slopeRsiList = (List<SlopeResult>)rsiList.GetSma(25).GetSlope(3);

        // (volgens de telegram groepen op 14,3,1 ipv de standaard 14,3,3)
        List<StochResult> stochList = (List<StochResult>)history.GetStoch(14, 3, 1); // 18-11-22: omgedraaid naar 1, 3...
        List<ParabolicSarResult> psarList = (List<ParabolicSarResult>)Indicator.GetParabolicSar(history);
        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)history.GetBollingerBands();

        // Fill the last x candles with the indicator data
        for (int index = history.Count - 1; index >= 0; index--)
        {
            candle = history[index];

            CandleIndicatorData candleData = new();
            candle.CandleData = candleData;
            try
            {
                //// EMA's
                ////candleData.Ema8 = emaList8[index].Ema;
                //candleData.Ema20 = emaList20[index].Ema;
                //candleData.Ema50 = emaList50[index].Ema;
                ////candleData.Ema100 = emaList100[index].Ema;
                //candleData.Ema200 = emaList200[index].Ema;
                //candleData.SlopeEma20 = slopeEma20List[index].Slope;
                //candleData.SlopeEma50 = slopeEma50List[index].Slope;

                //candleData.Tema = temaList[index].Tema;

                // SMA's
                //candleData.Sma8 = smaList8[index].Sma;
                candleData.Sma20 = bollingerBandsList[index].Sma;
                candleData.Sma50 = smaList50[index].Sma;
                //candleData.Sma100 = smaList100[index].Sma;
                candleData.Sma200 = smaList200[index].Sma;
                //candleData.SlopeSma20 = slopeSma20List[index].Slope;
                //candleData.SlopeSma50 = slopeSma50List[index].Slope;

                candleData.Rsi = rsiList[index].Rsi;
                //candleData.SlopeRsi = slopeRsiList[index].Slope;

                candleData.MacdValue = macdList[index].Macd;
                candleData.MacdSignal = macdList[index].Signal;
                candleData.MacdHistogram = macdList[index].Histogram;

                //candleData.MacdLtValue = macdLtList[index].Macd;
                //candleData.MacdLtSignal = macdLtList[index].Signal;
                //candleData.MacdLtHistogram = macdLtList[index].Histogram;

                candleData.StochSignal = stochList[index].Signal;
                candleData.StochOscillator = stochList[index].Oscillator;

                double? BollingerBandsLowerBand = bollingerBandsList[index].LowerBand;
                double? BollingerBandsUpperBand = bollingerBandsList[index].UpperBand;
                candleData.BollingerBandsDeviation = 0.5 * (BollingerBandsUpperBand - BollingerBandsLowerBand);
                candleData.BollingerBandsPercentage = 100 * ((BollingerBandsUpperBand / BollingerBandsLowerBand) - 1);

                if (psarList[index].Sar != null)
                    candleData.PSar = psarList[index].Sar;
                //candleData.KeltnerUpperBand = keltnerList[index].UpperBand;
                //candleData.KeltnerLowerBand = keltnerList[index].LowerBand;
            }
            catch (Exception error)
            {
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab("error indicators");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab(history.ToString());
                throw;
            }
        }
    }



    public bool CalculateIndicators()
    {
        // TODO - Laatste tijdstip meegeven zodat de trend over 0..x candles gaat en niet allemaal
        if (Candles.Count < 300)
        {
            Log.AppendLine("no candles!");
            return false;
        }

        // in 1x alle indicators berekenen (eenmalig)
        CalculateAllIndicatorsViaSkender([.. Candles.Values]);
        //CalculateAllIndicatorsViaSkender(Candles1m.Values.ToList()); // wordt verder niet gebruikt
        //CalculateAllIndicatorsViaTaLib(Candles.Values.ToList());

        return true;
    }

    //public CryptoSignal CreateSignal(CryptoCandle candle)
    //{
    //    CryptoSignal signal = new();
    //    signal.BackTest = true;
    //    signal.Symbol = Symbol;
    //    signal.SymbolId = Symbol.Id;
    //    signal.Exchange = Exchange;
    //    signal.ExchangeId = Exchange.Id;
    //    signal.Mode = TradeDirection.Long; //Buy signal (ahum, dat klopt dus niet voor de BB+)
    //    signal.Strategy = SignalStrategy.Jump;
    //    signal.Interval = Interval;
    //    signal.IntervalId = Interval.Id;

    //    // Dit is is de start van een candle (ondanks de naamgeving)
    //    signal.EventTime = candle.OpenTime; // Candle.OpenTime (unix)
    //    signal.OpenDate = CandleTools.GetUnixDate(candle.OpenTime); // Candle.OpenTime
    //    signal.CloseDate = signal.OpenDate.AddSeconds(Interval.Duration); // Candle.CloseTime
    //    signal.ExpirationDate = signal.CloseDate.AddSeconds(15 * Interval.Duration); // 15 candles verder (om het aantal meldingen te reduceren)
    //    signal.Candle = candle; //Dit is de laatste candle voor de opgegeven eindtijd
    //    signal.Price = candle.Close;
    //    signal.Volume = Symbol.Volume;


    //    // Waarden van de indicators invulen
    //    signal.BollingerBandsPercentage = candle.CandleData.BollingerBandsPercentage;
    //    signal.BollingerBandsDeviation = candle.CandleData.BollingerBandsDeviation;

    //    signal.StochSignal = candle.CandleData.StochSignal;
    //    signal.StochOscillator = candle.CandleData.StochOscillator;

    //    signal.Rsi = candle.CandleData.Rsi;
    //    signal.SlopeRsi = candle.CandleData.SlopeRsi;
    //    signal.PSar = candle.CandleData.PSar;

    //    signal.Ema20 = candle.CandleData.Ema20;
    //    signal.Ema50 = candle.CandleData.Ema50;
    //    //signal.Ema100 = candle.CandleData.Ema100;
    //    signal.Ema200 = candle.CandleData.Ema200;
    //    signal.SlopeEma20 = candle.CandleData.SlopeEma20;
    //    signal.SlopeEma50 = candle.CandleData.SlopeEma50;

    //    signal.Sma20 = candle.CandleData.Sma20;
    //    signal.Sma50 = candle.CandleData.Sma50;
    //    //signal.Sma100 = candle.CandleData.Sma100;
    //    signal.Sma200 = candle.CandleData.Sma200;
    //    signal.SlopeSma20 = candle.CandleData.SlopeSma20;
    //    signal.SlopeSma50 = candle.CandleData.SlopeSma50;

    //    signal.KeltnerLowerBand = candle.CandleData.KeltnerLowerBand;
    //    signal.KeltnerUpperBand = candle.CandleData.KeltnerUpperBand;

    //    //if (LastCandle.CandleData.Ema200.Ema != null)
    //    //Signal.Ema200 = (decimal)LastCandle.CandleData.Ema200.Ema.Value;
    //    //if (LastCandle.CandleData.Ema100.Ema != null)
    //    //Signal.Ema100 = (decimal)LastCandle.CandleData.Ema100.Ema.Value;
    //    //if (LastCandle.CandleData.Ema50.Ema != null)
    //    //Signal.Ema50 = (decimal)LastCandle.CandleData.Ema50.Ema.Value;
    //    //if (LastCandle.CandleData.Ema20.Ema != null)
    //    //Signal.Ema20 = (decimal)LastCandle.CandleData.Ema20.Ema.Value;

    //    //CalculateTrendStuff(); tijd intersief, alleen bij een signaal doen
    //    //CalculateAdditionalAlarmProperties(Signal, history, 60);
    //    return signal;
    //}


    ///// <summary>
    ///// Candles uit de database halen voor de gevraagde interval X indien deze niet aanwezig zijn
    ///// </summary>
    //private SortedList<long, CryptoCandle> LoadSymbolCandles(CryptoInterval interval, DateTime dateCandleStart, DateTime dateCandleEinde)
    //{
    //    SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
    //    if (candles.Count == 0)
    //    {
    //        using (SqlConnection database = new SqlConnection(GlobalData.ConnectionString))
    //        {
    //            database.Open();

    //            long startTime = CandleTools.GetUnixTime(dateCandleStart, 60);
    //            long eindeTime = CandleTools.GetUnixTime(dateCandleEinde, 60);
    //            GlobalData.AddTextToLogTab(string.Format("{0} {1} Candles lezen", Symbol.Name, interval.Name));

    //            StringBuilder builder = new StringBuilder();
    //            builder.AppendLine("select *");
    //            builder.AppendLine("from candles WITH (NOLOCK)");
    //            builder.AppendLine(string.Format("where IntervalId={0}", interval.Id));
    //            builder.AppendLine(string.Format("and SymbolId='{0}'", Symbol.Id));
    //            builder.AppendLine(string.Format("and OpenTime >= {0}", startTime));
    //            builder.AppendLine(string.Format("and OpenTime <= {0}", eindeTime));

    //            foreach (CryptoCandle candle in database.Query<CryptoCandle>(builder.ToString()))
    //            {
    //                candle.Symbol = Symbol;
    //                candle.Interval = interval;
    //                candle.Exchange = Symbol.Exchange;
    //                if (!candles.ContainsKey(candle.OpenTime))
    //                    candles.Add(candle.OpenTime, candle);
    //            }
    //        }
    //    }
    //    return candles;
    //}



    ///// <summary>
    ///// De routine die alles doet op basis van de aangeboden candle
    ///// (buiten het basis algoritme ander wordt reallive verstoord)
    ///// </summary>
    //private async Task HandleCandle(BackTestEmulator emulator, SignalCreateBase cryptoBackTest, CryptoCandle candle)
    //{
    //    cryptoBackTest.CandleLast = candle;
    //    //if (!cryptoBackTest.IndicatorsOkay(candle))
    //    //    return;


    //    //if (candle.Date >= DateTime.SpecifyKind(new DateTime(2023, 02, 03, 17, 58, 00), DateTimeKind.Utc))
    //    //    candle = candle; // debug; 2022-07-19 13:12-13:15

    //    //string s = candle.OhlcText(Symbol, Interval, Symbol.PriceDisplayFormat, false, false, Config.LogIncludeVolume) + " " + cryptoBackTest.DisplayText();
    //    //cryptoBackTest.ExtraText = "";


    //    // Nieuw idee, gebruik de routines die we al hebben
    //    // (en er niet iedere keer omheen werken!)
    //    // Dat kost wat aanpassingen denk ik.

    //    PositionMonitor x = new(Symbol);
    //    await x.NewCandleArrived(candle);

    //    //// Slechts 3 routines, dit is de redenatie:
    //    //// Signal = is er een signaal? (bijvoorbeeld oversold en onder de bb)
    //    //// GiveUp = reden om een eerde signaal toch te negeren
    //    //// AllowStepIn = is het acceptabel om NU in te stappen?
    //    //return;

    //    ////*****************************************
    //    //// JOB: Verwijderen van de PersistentData.
    //    //// Dit moet door algoritme geregeld worden.
    //    ////*****************************************


    //    //// Controleren op een nieuw signaal
    //    //if (!Exchange.PositionList.ContainsKey(Symbol.Name))
    //    //{
    //    //    // Als er een vervolgd signaal is nemen we die
    //    //    bool newSignal = false;
    //    //    cryptoBackTest.ExtraText = "";
    //    //    if (cryptoBackTest.IsSignal() && cryptoBackTest.AdditionalChecks(candle, out string response))
    //    //    {
    //    //        bool hasSignals = SymbolInterval.Signal != null;
    //    //        if (SymbolInterval.Signal == null || (cryptoBackTest.ReplaceSignal && hasSignals))
    //    //        {
    //    //            if (SymbolInterval.Signal != null)
    //    //                Log.AppendLine("");

    //    //            newSignal = true;
    //    //            emulator.Data.Reset();
    //    //            CryptoSignal signal = CreateSignal(candle);
    //    //            signal.Mode = cryptoBackTest.SignalMode;
    //    //            signal.Strategy = cryptoBackTest.SignalStrategy;
    //    //            SymbolInterval.Signal = signal;
    //    //            Log.AppendLine(s + " signal " + signal.StrategyText + " " + cryptoBackTest.ExtraText);
    //    //            s = "";

    //    //            // En doorgaan we kunnen direct instappen
    //    //        }
    //    //    }



    //    //    // Het signaal kan net nieuw zijn en dan wil je de delay en de wait nog niet toepassen..
    //    //    // Een optionele delay (cc#1) (een periode van geheel niets doen)
    //    //    //if (Exchange.PositionList.TryGetValue(Symbol.Name, out var positionListx) && (positionListx != null && positionListx.Any() && !newSignal))
    //    //    if (SymbolInterval.Signal != null && !newSignal)
    //    //    {
    //    //        // Een optionele delay (cc#1) (een periode van geheel niets doen)
    //    //        if (Config.DelayCandles > 0)
    //    //        {
    //    //            CryptoSignal Signal = SymbolInterval.Signal;
    //    //            if (Signal.EventTime + Config.DelayCandles * Interval.Duration <= candle.OpenTime)
    //    //            {
    //    //                long elapsed = (candle.OpenTime - Signal.EventTime) / Interval.Duration;
    //    //                Log.AppendLine(s + " (delay) " + elapsed.ToString());
    //    //                return;
    //    //            }
    //    //        }


    //    //        // Hoelang willen we wachten totdat het signaal geaccepteerd wordt (zoniet dan reset)
    //    //        if (Config.WaitCandles > 0)
    //    //        {
    //    //            CryptoSignal Signal = SymbolInterval.Signal;
    //    //            if (Signal.EventTime + (Config.DelayCandles + Config.WaitCandles) * Interval.Duration <= candle.OpenTime)
    //    //            {
    //    //                // Als er na x candles geen geldig signaal is dan deze resetten
    //    //                emulator.Data.Reset();
    //    //                SymbolInterval.Signal = null;
    //    //                //cryptoBackTest.Reset();
    //    //                Log.AppendLine(s + " timeout " + Config.WaitCandles.ToString());
    //    //                Log.AppendLine("");
    //    //                Log.AppendLine("");
    //    //                Log.AppendLine("");
    //    //                return;
    //    //            }
    //    //            else
    //    //            {
    //    //                long elapsed = (candle.OpenTime - Signal.EventTime) / Interval.Duration;
    //    //            }
    //    //        }
    //    //    }

    //    //    // Geeft het algoritme de kans om te annuleren (die kan zijn net gegenereerde signaal annuleren)
    //    //    if (SymbolInterval.Signal != null)
    //    //    {
    //    //        CryptoSignal Signal = SymbolInterval.Signal;
    //    //        if (cryptoBackTest.GiveUp(Signal))
    //    //        {
    //    //            // Na timeout de status terug zetten en opnieuw beginnen
    //    //            emulator.Data.Reset();
    //    //            SymbolInterval.Signal = null;
    //    //            //cryptoBackTest.Reset();

    //    //            Log.AppendLine(s + " " + string.Format("signal give-up {0}", cryptoBackTest.ExtraText));
    //    //            Log.AppendLine("");
    //    //            Log.AppendLine("");
    //    //            Log.AppendLine("");
    //    //            return;
    //    //        }
    //    //    }


    //    //    // Een extra delay na het 1e signaal
    //    //    if (SymbolInterval.Signal != null)
    //    //    {
    //    //        CryptoSignal signal = SymbolInterval.Signal;
    //    //        if (cryptoBackTest.AllowStepIn(signal))
    //    //        {
    //    //            // We mogen instappen, simuleer dat we de orders plaatsen enzovoort..
    //    //            // Maak een positie (en vandaar uit hebben we de persistent data)
    //    //            emulator.Data.Reset();
    //    //            SymbolInterval.Signal = null;

    //    //            emulator.Data.Position = PositionTools.CreatePosition(Symbol, SymbolInterval, candle);
    //    //            emulator.Data.Position.Id = ++UniqueId;
    //    //            PositionTools.AddPosition(emulator.Data.Position);

    //    //            // Bereken de instap op basis van de aangeboden candle
    //    //            emulator.CandleLast = candle;
    //    //            emulator.CalculateBuyPrice(emulator.Data.Position);
    //    //            emulator.Results = new CryptoBackTestResults(Config.QuoteMarket, Symbol, Interval, Config);
    //    //        }
    //    //    }
    //    //    //else
    //    //    //  Log.AppendLine(s + " (waiting to step in) " + cryptoBackTest.ExtraText + " " + emulator.Data.WaitCandles.ToString());
    //    //}

    //    //// (Uit de switch gehaald, als we mogen instappen deze direct aanbieden
    //    //if (Exchange.PositionList.ContainsKey(Symbol.Name))
    //    //{
    //    //    emulator.CandleLast = candle;
    //    //    EmulatorResult result = emulator.HandleCandle();
    //    //    if (result != EmulatorResult.Waiting)
    //    //    {
    //    //        // Na afloop de status terug zetten en opnieuw beginnen
    //    //        Results.Add(emulator.Results);
    //    //        emulator.Data.Reset();
    //    //        SymbolInterval.Signal = null;
    //    //        Exchange.PositionList.Clear();

    //    //        Log.AppendLine("");
    //    //        Log.AppendLine("");
    //    //        Log.AppendLine("");
    //    //        return;
    //    //    }
    //    //}

    //    //if (s != "")
    //    //    Log.AppendLine(s + " " + cryptoBackTest.ExtraText); // ter debug (wat een hoop tekst)

    //}


    public async Task Execute(SignalCreateBase cryptoBackTest, string baseFolder)
    {
        // Het idee is simpel, itereer door de candles en bied deze 1 voor 1 aan het gekozen algoritme.

        // Weetjes:
        // -Candles hoeven qua timing niet aan te sluiten! (onderhoud, storingen en andere uitval)
        // -De aansturing moet echt zo dom mogelijk zijn, de inteligentie moet in de object zitten

        // Nadelen
        // -Het is een simulatie, een gedeeltelijke fill, timing issues enzovoort gebeurd niet
        // -Het debuggen is tijd intensief (alles traceren via tekstbestanden in een tool)

        // Voordelen
        // -Enig realiteit besef hebben over de gekozen waarden
        // -Berekening van alles (inclusief inschatting fees)

        //cryptoBackTest.Log = Log;

        //BackTestEmulator emulator = new(Symbol, Interval, Config)
        //{
        //    CryptoBackTest = cryptoBackTest,
        //    Log = Log
        //};

        //try
        //{
        //    Results.ShowHeader(Log);

        //    try
        //    {
        //        // Voro de zekerheid..
        //        //Exchange.PositionList.Clear();
        //        SymbolInterval.Signal = null;
        //        //GlobalData.Settings.Trading.PauseTradingRules. TradingUntil = null; ??

        //        // De indicators berekenen (eenmalig) - beetje vaag?
        //        //CryptoSymbolInterval cryptoSymbolInterval = Symbol.GetSymbolInterval(Interval.IntervalPeriod);
        //        //if ((!cryptoSymbolInterval.TrendInfoDate.HasValue))
        //        if ((!SymbolInterval.TrendInfoDate.HasValue))
        //        {
        //            // TODO - Laatste tijdstip meegeven zodat de trend over 0..x candles gaat en niet allemaal
        //            if (!CalculateIndicators())
        //                return;
        //            SymbolInterval.TrendInfoDate = DateTime.UtcNow;
        //        }

        //        // Iedere symbol heeft op elk interval een PersistentData nodig
        //        // Iedere positie heeft een eigen PersistenData voor de lopende positie

        //        int warmUptime = 210; // De warmup time is enkel voor de indicators en emulator
        //        for (int i = 0; i < Candles1m.Count; i++)
        //        {
        //            CryptoCandle candle = Candles1m.Values[i];
        //            if (--warmUptime <= 0)
        //            {
        //                // barometer initialiseren
        //                if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Core.Model.CryptoExchange exchangeX))
        //                {
        //                    if (exchangeX.SymbolListName.TryGetValue("$BMP" + Symbol.Quote, out CryptoSymbol? symbolX))
        //                    {
        //                        CryptoSymbolInterval cryptoSymbolIntervalX = symbolX.GetSymbolInterval(CryptoIntervalPeriod.interval1h);
        //                        BarometerData? barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(symbolX.Quote, CryptoIntervalPeriod.interval1h);
        //                        if (cryptoSymbolIntervalX.CandleList.TryGetValue(candle.OpenTime, out CryptoCandle candleX))
        //                        {
        //                            barometerData.PriceBarometer = candleX.Close;
        //                            barometerData.PriceDateTime = candleX.OpenTime;
        //                        }
        //                        else barometerData.PriceBarometer = 0;

        //                        // interval4h
        //                        cryptoSymbolIntervalX = symbolX.GetSymbolInterval(CryptoIntervalPeriod.interval4h);
        //                        barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(symbolX.Quote, CryptoIntervalPeriod.interval4h);
        //                        if (cryptoSymbolIntervalX.CandleList.TryGetValue(candle.OpenTime, out candleX))
        //                        {
        //                            barometerData.PriceBarometer = candleX.Close;
        //                            barometerData.PriceDateTime = candleX.OpenTime;
        //                        }
        //                        else barometerData.PriceBarometer = 0;

        //                        // interval1d
        //                        cryptoSymbolIntervalX = symbolX.GetSymbolInterval(CryptoIntervalPeriod.interval1d);
        //                        barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(symbolX.Quote, CryptoIntervalPeriod.interval1d);
        //                        if (cryptoSymbolIntervalX.CandleList.TryGetValue(candle.OpenTime, out candleX))
        //                        {
        //                            barometerData.PriceBarometer = candleX.Close;
        //                            barometerData.PriceDateTime = candleX.OpenTime;
        //                        }
        //                        else barometerData.PriceBarometer = 0;
        //                    }



        //                }

        //                Symbol.LastPrice = candle.Close;
        //                //await HandleCandle(emulator, cryptoBackTest, candle);

        //                using PositionMonitor positionMonitor = new(Symbol, candle);
        //                await positionMonitor.NewCandleArrivedAsync();
        //            }
        //        }

        //        Log.AppendLine("");
        //        Log.AppendLine("einde");
        //        Log.AppendLine("");
        //    }
        //    catch (Exception error)
        //    {
        //        GlobalData.AddTextToLogTab(error.ToString());
        //        Log.AppendLine(error.ToString());
        //        // De log echter wel schrijven dus geen raize
        //    }


        //    Log.AppendLine("Totaal:");
        //    Outcome = Results.ShowFooter(Log);
        //    Log.AppendLine(Outcome);

        //    string s = Log.ToString();
        //    //filename = filename + @"\data\" + Exchange.Name + @"\BackTest\" + Symbol.Name + @"\"; // + interval.Name + @"\";
        //    baseFolder += Symbol.Name + @"\";
        //    Directory.CreateDirectory(baseFolder);
        //    File.WriteAllText(baseFolder + Symbol.Name + "-" + Interval.Name + ".txt", s);


        //    // Bewaar de candles (voor debug en test)
        //    {
        //        SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(Interval.IntervalPeriod).CandleList;
        //        string filename = baseFolder + Symbol.Name + ".json";
        //        string text = JsonConvert.SerializeObject(candles.Values, Formatting.Indented);
        //        System.IO.File.WriteAllText(filename, text);
        //    }

        //}
        //catch (Exception error)
        //{
        //    ScannerLog.Logger.Error(error, "");
        //    GlobalData.AddTextToLogTab("");
        //    GlobalData.AddTextToLogTab(error.ToString(), true);
        //}
    }

}