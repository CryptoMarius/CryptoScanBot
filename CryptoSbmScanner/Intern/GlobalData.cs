using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Routing;
using Newtonsoft.Json;
using TradingView;

namespace CryptoSbmScanner
{

    public enum ApplicationStatus
    {
        AppStatusPrepare,
        AppStatusRunning,
        AppStatusExiting
    }

    /// <summary>
    /// Om vanuit de threads tekst in het main scherm te zetten
    /// TODO Betere logger te gebruiken, maar dit werkt (voorlopig)
    /// </summary>
    public delegate void AddTextEvent(string text, bool extraLineFeed = false);

    public delegate void PlayMediaEvent(string text, bool test = false);

    /// <summary>
    /// Om vanuit de threads de timer voor ophalen Candles te disablen
    /// </summary>
    public delegate void SetCandleTimerEnable(bool value);



    /// De laatst berekende barometer standen
    public class BarometerData
    {
        public long? PriceDateTime { get; set; }
        public decimal? PriceBarometer { get; set; }

        public long? VolumeDateTime { get; set; }
        public decimal? VolumeBarometer { get; set; }
    }


    /// <summary>
    /// Some global variabeles
    /// </summary>
    static public class GlobalData
    {
        static public SettingsBasic Settings { get; set; } = new SettingsBasic();
        static public ApplicationStatus ApplicationStatus { get; set; } = ApplicationStatus.AppStatusPrepare;

        // The nlogger stuff
        static public NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static public List<CryptoInterval> IntervalList = new List<CryptoInterval>();
        //static public SortedList<long, CryptoInterval> IntervalListId { get; } = new SortedList<long, CryptoInterval>();
        static public SortedList<CryptoIntervalPeriod, CryptoInterval> IntervalListPeriod { get; } = new SortedList<CryptoIntervalPeriod, CryptoInterval>();

        static public SortedList<string, bool> SymbolBlackListOversold { get; } = new SortedList<string, bool>();
        static public SortedList<string, bool> SymbolWhiteListOversold { get; } = new SortedList<string, bool>();

        static public SortedList<string, bool> SymbolBlackListOverbought { get; } = new SortedList<string, bool>();
        static public SortedList<string, bool> SymbolWhiteListOverbought { get; } = new SortedList<string, bool>();

        /// Exchanges indexed on name
        static public SortedList<string, CryptoSbmScanner.CryptoExchange> ExchangeListName { get; } = new SortedList<string, CryptoSbmScanner.CryptoExchange>();

        // To avoid duplicate signals
        static public Dictionary<string, long> AnalyseNotification = new Dictionary<string, long>();

        static public event PlayMediaEvent PlaySound;
        static public event PlayMediaEvent PlaySpeech;
        static public event AddTextEvent LogToTelegram;
        static public event AddTextEvent LogToLogTabEvent;

        // Events for refresing data
        //static public event AddTextEvent AssetsHaveChangedEvent;
        static public event AddTextEvent SymbolsHaveChangedEvent;
        static public event AddTextEvent ConnectionWasLostEvent;
        static public event AddTextEvent ConnectionWasRestoredEvent;

        static public event SetCandleTimerEnable SetCandleTimerEnableEvent;

        // Some running tasks/threads
        //static public ThreadSaveCandles TaskSaveCandles { get; set; }
        static public ThreadCreateSignal ThreadCreateSignal { get; set; }
        //static public ThreadOrderHandler ThreadOrderHandler { get; set; }        
        //static public ThreadMonitorSignal TaskMonitorSignal { get; set; }
        //static public ThreadBalanceSymbols ThreadBalanceSymbols { get; set; }

        // Binance stuff
        //static public BinanceStreamUserData TaskBinanceStreamUserData { get; set; }
        static public BinanceStreamPriceTicker TaskBinanceStreamPriceTicker { get; set; }


        // On special request of a hardcore trader..
        static public SymbolValue FearAndGreedIndex { get; set; } = new SymbolValue();
        static public SymbolValue TradingViewDollarIndex { get; set; } = new SymbolValue();
        static public SymbolValue TradingViewSpx500 { get; set; } = new SymbolValue();
        static public SymbolValue TradingViewBitcoinDominance { get; set; } = new SymbolValue();
        static public SymbolValue TradingViewMarketCapTotal { get; set; } = new SymbolValue();

        static public void InitializeGlobalData()
        {
            FearAndGreedIndex.Name = "Fear and Greed index";
            FearAndGreedIndex.Url = "https://alternative.me/crypto/fear-and-greed-index/";
        }

        static public void InitializeIntervalList()
        {
            IntervalList.Clear();

            // All intervals and how they are constructed
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1m, "1m", 1 * 60, null)); //0
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2m, "2m", 2 * 60, IntervalList[0])); //1
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3m, "3m", 3 * 60, IntervalList[0])); //2
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval5m, "5m", 5 * 60, IntervalList[0])); //3
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval10m, "10m", 10 * 60, IntervalList[3])); //4
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval15m, "15m", 15 * 60, IntervalList[3]));  //5
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval30m, "30m", 30 * 60, IntervalList[5])); //6
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1h, "1h", 01 * 60 * 60, IntervalList[6])); //7
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval2h, "2h", 02 * 60 * 60, IntervalList[7])); //8
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval3h, "3h", 03 * 60 * 60, IntervalList[7])); //9
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval4h, "4h", 04 * 60 * 60, IntervalList[8])); //10
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval6h, "6h", 06 * 60 * 60, IntervalList[9])); //11
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval8h, "8h", 08 * 60 * 60, IntervalList[10])); //12
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval12h, "12h", 12 * 60 * 60, IntervalList[10])); //13
            IntervalList.Add(CryptoInterval.CreateInterval(CryptoIntervalPeriod.interval1d, "1d", 1 * 24 * 60 * 60, IntervalList[11])); //14

            //IntervalList.Add(new CryptoInterval(CryptoIntervalPeriod.interval3d, "3d", 3 * 24 * 60 * 60, IntervalList[12])); //13
            // iets teveel, niet relevant voor deze tool
            //IntervalList.Add(new Interval(IntervalPeriod.interval1w, "1w", 7 * 24 * 60 * 60, IntervalList[12], (decimal)4)); //14
            //IntervalList.Add(new Interval(IntervalPeriod.interval2w, "2w", 14 * 24 * 60 * 60, IntervalList[14], (decimal)4)); //15

            //foreach (Interval interval in IntervalList)
            //{
            //    if (interval.ConstructFrom != null)
            //        interval.ConstructFromId = interval.ConstructFrom.Id;
            //    databaseMain.Insert<Interval>(interval);
            //}

            IntervalListPeriod.Clear();
            foreach (CryptoInterval interval in IntervalList)
            {
                IntervalListPeriod.Add(interval.IntervalPeriod, interval);
            }

        }


        static public void AddExchange(CryptoSbmScanner.CryptoExchange exchange)
        {
            if (!ExchangeListName.ContainsKey(exchange.Name))
            {
                ExchangeListName.Add(exchange.Name, exchange);
            }
        }


        static public CryptoQuoteData AddQuoteData(string quoteName)
        {
            if (!Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData quoteData))
            {
                quoteData = new CryptoQuoteData();
                quoteData.Name = quoteName;
                Settings.QuoteCoins.Add(quoteName, quoteData);
            }
            return quoteData;
        }

        static public void AddSymbol(CryptoSymbol symbol)
        {
            //if (!(symbol.Name.Equals("BTCBUSD") ))  //|| symbol.Name.Equals("ETHBUSD")
            //    return;

            if (!symbol.Exchange.SymbolListName.ContainsKey(symbol.Name))
                symbol.Exchange.SymbolListName.Add(symbol.Name, symbol);

            // Reference to QuoteData
            symbol.QuoteData = AddQuoteData(symbol.Quote);


            string s = symbol.PriceTickSize.ToString0();
            int numberOfDecimalPlaces = s.Length - 2;
            symbol.DisplayFormat = "N" + numberOfDecimalPlaces.ToString();
        }



        static public void InitBarometerSymbols()
        {
            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
                {
                    if (quoteData.FetchCandles)
                    {
                        if (!exchange.SymbolListName.ContainsKey(Constants.SymbolNameBarometerPrice + quoteData.Name))
                        {
                            CryptoSymbol symbol = new CryptoSymbol();
                            symbol.Base = Constants.SymbolNameBarometerPrice; //De "munt"
                            symbol.Quote = quoteData.Name; //USDT, BTC etc.
                            symbol.Name = symbol.Base + symbol.Quote;
                            symbol.Volume = 0;
                            symbol.Status = 1;
                            symbol.Exchange = exchange;
                            GlobalData.AddSymbol(symbol);
                        }
                    }
                }
            }
        }


        static public void InitExchanges()
        {
            // Add the Exchange binance (just 1 for now)
            CryptoExchange exchange = new CryptoExchange();
            exchange.Name = "Binance";

            GlobalData.AddExchange(exchange);
        }

        static public void InitWhiteAndBlackListSettings()
        {
            GlobalData.SymbolBlackListOversold.Clear();
            foreach (string text in GlobalData.Settings.BlackListOversold)
            {
                if (!GlobalData.SymbolBlackListOversold.ContainsKey(text))
                    GlobalData.SymbolBlackListOversold.Add(text, true);
            }
            GlobalData.SymbolWhiteListOversold.Clear();
            foreach (string text in GlobalData.Settings.WhiteListOversold)
            {
                if (!GlobalData.SymbolWhiteListOversold.ContainsKey(text))
                    GlobalData.SymbolWhiteListOversold.Add(text, true);
            }



            GlobalData.SymbolBlackListOverbought.Clear();
            foreach (string text in GlobalData.Settings.BlackListOverbought)
            {
                if (!GlobalData.SymbolBlackListOverbought.ContainsKey(text))
                    GlobalData.SymbolBlackListOverbought.Add(text, true);
            }
            GlobalData.SymbolWhiteListOverbought.Clear();
            foreach (string text in GlobalData.Settings.WhiteListOverbought)
            {
                if (!GlobalData.SymbolWhiteListOverbought.ContainsKey(text))
                    GlobalData.SymbolWhiteListOverbought.Add(text, true);
            }
        }

        static public void LoadSettings()
        {
            string filename = GlobalData.GetBaseDir() + "GlobalData.Settings2.json";
            if (System.IO.File.Exists(filename))
            {
                //using (FileStream readStream = new FileStream(filename, FileMode.Open))
                //{
                //    BinaryFormatter formatter = new BinaryFormatter();
                //    GlobalData.Settings = (Settings)formatter.Deserialize(readStream);
                //    readStream.Close();
                //}

                string text = File.ReadAllText(filename);
                Settings = JsonConvert.DeserializeObject<SettingsBasic>(text);
            }
            else
                DefaultSettings();


            // Migratie probleem van de 1.5 naar 1.6. Het array groter maken vanwege de extra intervallen
            if (Settings.Signal.AnalyseInterval.Length < Enum.GetNames(typeof(CryptoIntervalPeriod)).Length)
            {
                bool[] intervals = new bool[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];

                for (int i = 0; i < Settings.Signal.AnalyseInterval.Length; i++)
                    intervals[i] = Settings.Signal.AnalyseInterval[i];

                Settings.Signal.AnalyseInterval = intervals;
            }


            InitWhiteAndBlackListSettings();
        }

        static public void DefaultSettings()
        {
            // Apply some defaults
            if (Settings.QuoteCoins.Count == 0)
            {
                CryptoQuoteData quote = new CryptoQuoteData();
                quote.Name = "BUSD";
                quote.FetchCandles = true;
                quote.CreateSignals = true;
                quote.MinimalVolume = 6500000;
                quote.MinimalPrice = 0.00000001m;
                Settings.QuoteCoins.Add(quote.Name, quote);

                quote = new CryptoQuoteData();
                quote.Name = "USDT";
                quote.FetchCandles = false;
                quote.CreateSignals = false;
                quote.MinimalVolume = 6500000;
                quote.MinimalPrice = 0.00000001m;
                Settings.QuoteCoins.Add(quote.Name, quote);

                quote = new CryptoQuoteData();
                quote.Name = "BTC";
                quote.FetchCandles = false;
                quote.CreateSignals = false;
                quote.MinimalVolume = 250;
                quote.MinimalPrice = 0.00000001m;
                Settings.QuoteCoins.Add(quote.Name, quote);
            }
        }

        static public void SaveSettings()
        {
            //Laad de gecachte (langere historie, minder overhad)
            string filename = GlobalData.GetBaseDir() + "GlobalData.Settings2.json";

            //using (FileStream writeStream = new FileStream(filename, FileMode.Create))
            //{
            //    BinaryFormatter formatter = new BinaryFormatter();
            //    formatter.Serialize(writeStream, GlobalData.Settings);
            //    writeStream.Close();
            //}

            string text = JsonConvert.SerializeObject(GlobalData.Settings, Formatting.Indented);
            //var accountFile = new FileInfo(filename);
            File.WriteAllText(filename, text);

            //filename = GlobalData.GetBaseDir() + "Settings.json";
            //File.WriteAllText(filename, text);
        }


        static public void PlaySomeMusic(string text, bool test = false)
        {
            try
            {
                PlaySound(text, test);
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                GlobalData.AddTextToLogTab(" error play music (1) " + error.ToString(), false);
            }
        }

        static public void PlaySomeSpeech(string text, bool test = false)
        {
            try
            {
                PlaySpeech(text, test);
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(" error play speech (1) " + error.ToString(), false);
            }
        }

        static public void AddTextToTelegram(string text)
        {
            try
            {
                LogToTelegram(text);
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
                GlobalData.AddTextToLogTab(" error telegram thread(1)" + error.ToString(), false);
            }
        }


        static public void AddTextToLogTab(string text, bool extraLineFeed = false)
        {
            LogToLogTabEvent(text, extraLineFeed);
        }


        static public void SymbolsHaveChanged(string text)
        {
            SymbolsHaveChangedEvent(text);
        }

        static public void ConnectionWasLost(string text)
        {
            if (ConnectionWasLostEvent != null)
                ConnectionWasLostEvent(text);
        }

        static public void ConnectionWasRestored(string text)
        {
            if (ConnectionWasRestoredEvent != null)
                ConnectionWasRestoredEvent(text);
        }

        static public void SetCandleTimerEnable(bool value)
        {
            SetCandleTimerEnableEvent(value);
        }

        static bool IsInitialized = false;

        static public string GetBaseDir()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string specificFolder = Path.Combine(folder, "CryptoScanner");

            if (!IsInitialized)
            {
                IsInitialized = true;
                Directory.CreateDirectory(specificFolder);
            }

            return specificFolder + @"\";
        }
    }
}