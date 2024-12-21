using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Settings;
using CryptoScanBot.Core.Trader;

using ExchangeTest.Exchange.Altrady;

using Mexc.Net.Clients;

using System.Text.Json;


namespace CryptoScanBot.Experiment;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();


        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);

        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();

        // Is er via de command line aangegeven dat we default een andere exchange willen?
        {
            ApplicationParams.InitApplicationOptions();

            string? exchangeName = ApplicationParams.Options.ExchangeName;
            if (exchangeName != null)
            {
                // De default exchange is Binance (geen goede keuze in NL op dit moment)
                if (exchangeName == "")
                    exchangeName = "Binance";
                if (GlobalData.ExchangeListName.TryGetValue(exchangeName, out var exchange))
                {
                    GlobalData.Settings.General.Exchange = exchange;
                    GlobalData.Settings.General.ExchangeId = exchange.Id;
                    GlobalData.Settings.General.ExchangeName = exchange.Name;
                }
                else throw new Exception(string.Format("Exchange {0} bestaat niet", exchangeName));
            }
        }


        GlobalData.LoadSettings();
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();
        GlobalData.SetTradingAccounts();
        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        GlobalData.LoadAccounts();
        GlobalData.LoadSymbols();
        BarometerTools.InitBarometerSymbols();
        TradingConfig.InitWhiteAndBlackListSettings(); // after loading symbols

        GlobalData.Settings.General.Exchange!.GetApiInstance().ExchangeDefaults();
        ThreadLoadData.IndexQuoteDataSymbols(GlobalData.Settings.General.Exchange);



        GlobalData.ApplicationStatus = CryptoApplicationStatus.Running;

        GlobalData.TradingApi.Key = "";
        GlobalData.TradingApi.Secret = "";

    }

    private void AddTextToLogTab(string text)
    {
        if (IsHandleCreated)
        {
            text = text.Trim();
            ScannerLog.Logger.Info(text);

            if (text == "")
                text = "\r\n";
            else
                text = DateTime.Now.ToLocalTime() + " " + text + "\r\n";
            if (InvokeRequired)
                Invoke((MethodInvoker)(() => textBox1.AppendText(text)));
            else
                textBox1.AppendText(text);

            //File.AppendAllText(@"D:\Shares\Projects\.Net\CryptoScanBot\Testjes\bin\Debug\data\backtest.txt", text);
        }
    }

    static public void LoadExchangeSettings(string name)
    {
        string filename = $"{GlobalData.AppName}-exchange{name}.json";
        try
        {
            string fullName = GlobalData.GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                GlobalData.TradingApi = JsonSerializer.Deserialize<SettingsExchangeApi>(text, JsonTools.DeSerializerOptions);
            }
            else
                throw new Exception($"file not found {filename}");
            GlobalData.Settings.General.Exchange!.GetApiInstance().ExchangeDefaults();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }


        filename = $"{GlobalData.AppName}-altrady{name}.json";
        try
        {
            string fullName = GlobalData.GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                GlobalData.AltradyApi = JsonSerializer.Deserialize<SettingsAltradyApi>(text, JsonTools.DeSerializerOptions);
            }
            else
                throw new Exception($"file not found {filename}");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"Error loading {filename} " + error.ToString());
        }
    }


    private async void Button1_Click(object? sender, EventArgs? e)
    {
        // just a general purpose test place
        ScannerLog.Logger.Info("Testing....");
        ScannerLog.Logger.Trace("Testing....");
        ScannerLog.Logger.Error("Testing....");


        //LoadExchangeSettings(" - Bybit UTA api");
        //LoadExchangeSettings(" - Bybit Spot - Main account");
        LoadExchangeSettings(" - Mexc Spot - DcaBot account");

        //BinanceTestAsync();
        //ByBitUtaSpotTestAsync();
        //KucoinTest();
        //MexcTest();

        // EmulatorTest();

        // Be carefull, this one places active/live orders on the exchange
        //await ExchangeTest.Exchange.Bybit.Spot.Test.BybitTestAsync();
        int loop = 10;
        string prefix = "BTCUSDT 1m";
        using MexcRestClient client = new();

        CryptoQuoteData quoteData = new()
        {
            Name = "USDT",
        };
        CryptoSymbol symbol = new()
        {
            Exchange = null,
            Name = "BTCUSDT",
            Quote = "USDT",
            QuoteData = quoteData,
            Base = "BTC",
        };
        CryptoInterval interval = GlobalData.IntervalList[0];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        // This exchange is alway's returning the last 1000 candles (not what we asked, but voila)
        //DateTime dateStart = DateTime.UtcNow.AddDays(-10); //CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
        DateTime dateStartX = new(2024, 08, 28, 00, 30, 00, DateTimeKind.Utc);
        symbolInterval.LastCandleSynchronized = CandleTools.GetUnixTime(dateStartX.AddDays(-1), 60);


        while (true)
        {
            GlobalData.AddTextToLogTab("");
            long? startFetchDate = symbolInterval.LastCandleSynchronized; // Remember for reporting
            DateTime dateStart = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
            DateTime dateEnd = dateStart.AddSeconds(1000 * 60); // To create a valid date period
            GlobalData.AddTextToLogTab($"Fetch candles {symbol.Name} {interval.Name} {dateStart.ToLocalTime()} {dateEnd.ToLocalTime()}");

            var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbol.Name, Mexc.Net.Enums.KlineInterval.OneMinute, startTime: dateStart, endTime: dateEnd); // limit: 1000
            if (!result.Success)
            {
                // This is based on the kucoin error number,, does Mexc have an error for overloading the exchange as wel?
                // We doen het gewoon over (dat is tenminste het advies)
                // 13-07-2023 14:08:00 AOA-BTC 30m error getting klines 429: Too Many Requests
                if (result.Error?.Code == 429)
                {
                    GlobalData.AddTextToLogTab($"{prefix} delay needed for weight: (because of rate limits)");
                    Thread.Sleep(10000);
                    continue;
                }
                // Might do something better than this
                GlobalData.AddTextToLogTab($"{prefix} error getting klines {result.Error}");
                break;
            }


            // Might have problems with no internet etc.
            if (result == null || result.Data == null || !result.Data.Any())
            {
                GlobalData.AddTextToLogTab($"{prefix} fetch from {CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized)} no candles received");
                break;
            }


            //await symbol.CandleLock.WaitAsync();
            //Monitor.Enter(symbol.CandleList);???
            //try
            {
                GlobalData.AddTextToLogTab("First candle " + symbol.Name + " " + interval.Name + " " + result.Data.First().OpenTime.ToLocalTime());
                GlobalData.AddTextToLogTab("Last candle " + symbol.Name + " " + interval.Name + " " + result.Data.Last().OpenTime.ToLocalTime());

                // Fetch candles, sorting nog garanteed (its even recersed on bybit)
                long last = long.MinValue;
                //long first = long.MaxValue;
                foreach (var kline in result.Data)
                {
                    CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 0, kline.QuoteVolume, false);

                    //GlobalData.AddTextToLogTab("Debug: Fetched candle " + symbol.Name + " " + interval.Name + " " + candle.DateLocal);

                    if (candle.OpenTime > last)
                        last = candle.OpenTime; // newest candle received (in this session)
                    //if (candle.OpenTime < first)
                    //    first = candle.OpenTime; // oldest candle received (in this session)
                }
                symbolInterval.LastCandleSynchronized = last + interval.Duration;
            }



            // Fill missing candles (the only place we know it can be done safely)
            if (symbolInterval.CandleList.Count != 0)
            {
                CryptoCandle stickOld = symbolInterval.CandleList.Values.First();
                //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
                long unixTime = stickOld.OpenTime;
                while (unixTime < symbolInterval.LastCandleSynchronized)
                {
                    if (!symbolInterval.CandleList.TryGetValue(unixTime, out CryptoCandle? candle))
                    {
                        candle = new()
                        {
                            OpenTime = unixTime,
                            Open = stickOld.Close,
                            High = stickOld.Close,
                            Low = stickOld.Close,
                            Close = stickOld.Close,
#if SUPPORTBASEVOLUME
                            BaseVolume = 0,
#endif
                            Volume = 0
                        };
                        symbolInterval.CandleList.Add(candle.OpenTime, candle);
                        GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Added missing candle " + CandleTools.GetUnixDate(candle.OpenTime).ToLocalTime());
                    }
                    stickOld = candle;
                    unixTime += interval.Duration;
                }
            }

            loop--;
            if (loop < 0)
                break;
        }

        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab("Overzicht");
        foreach (var x in symbol.IntervalPeriodList)
        {
            GlobalData.AddTextToLogTab($"{symbol.Name} {x.Interval.Name} {x.CandleList.Count}");
        }

        //// Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        //using BybitRestClient client = new();

        ////client.ClientOptions.

        ////StopOrderType x = StopOrderType.TpSlOrder;

        //client.ClientOptions.OutputOriginalData = true;

        //WebCallResult<BybitOrderId> result = await client.V5Api.Trading.PlaceOrderAsync(
        //    Category.Spot,
        //    "APRSUSDT",
        //    OrderSide.Buy,
        //    NewOrderType.Limit,
        //    quantity: 1.01m,
        //    price: 0.51m,
        //    isLeverage: false,
        //    triggerPrice: 0.50m,
        //    triggerDirection: TriggerDirection.Fall
        //    //timeInForce: TimeInForce.PostOnly
        //    );

        //string text = JsonSerializer.Serialize(result, ExchangeHelper.JsonSerializerNotIndented).Trim();
        //GlobalData.AddTextToLogTab(text);
        //ScannerLog.Logger.Trace(text);

        //stopLossOrderType: OrderType.LimitMaker
        //triggerDirection: TriggerDirection.Fall, orderFilter: OrderFilter.OcoOrder, triggerPrice: 50000m,

        //    //stopLossTrigger: TriggerType.LastPrice,
        //    //stopLossOrderType: OrderType.Market,
        //    //stopLossTakeProfitMode:, 
        //    //StopLossTakeProfitMode.Full,
        //    //stopLoss: stop,
        //    //stopPrice: stop,

        //    takeProfitOrderType: OrderType.Limit,
        //    takeProfit: price,

        //    stopLossTriggerBy: TriggerType.IndexPrice,
        //    stopLossOrderType: OrderType.Market,
        //    stopLoss: stop,

        //    stopLossLimitPrice: limit,

        //    timeInForce: TimeInForce.GoodTillCanceled
        //);



        ////client.V5Api.Trading.PlaceOrderAsync(Category.Spot, "BTCUSDT", OrderSide.Sell, NewOrderType.Limit, quantity: quantity, 
        ////    timeInForce: TimeInForce.GoodTillCanceled, 
        ////    stopLossOrderType: OrderType.Limit, 
        ////    stopLoss: price, stopLossLimitPrice: stopPrice, 
        ////    stopLossTakeProfitMode: StopLossTakeProfitMode.Full, 
        ////    stopLossTriggerBy: TriggerType.LastPrice, 
        ////    clientOrderId: newClientOrderId
        //// );



        //////Task<WebCallResult<BybitOrderId>> PlaceOrderAsync(Category category, string symbol, OrderSide side, NewOrderType type, decimal quantity, decimal? price = null, 
        //////    bool? isLeverage = null, TriggerDirection? triggerDirection = null, OrderFilter? orderFilter = null, decimal? triggerPrice = null, 
        //////    TriggerType? triggerBy = null, decimal? orderIv = null, TimeInForce? timeInForce = null, PositionIdx? positionIdx = null, string? clientOrderId = null, 
        //////    OrderType? takeProfitOrderType = null, decimal? takeProfit = null, decimal? takeProfitLimitPrice = null, OrderType? stopLossOrderType = null, 
        //////    decimal? stopLoss = null, decimal? stopLossLimitPrice = null, TriggerType? takeProfitTriggerBy = null, TriggerType? stopLossTriggerBy = null, 
        //////    bool? reduceOnly = null, bool? closeOnTrigger = null, bool? marketMakerProtection = null, StopLossTakeProfitMode? stopLossTakeProfitMode = null, 
        //////    SelfMatchPreventionType? selfMatchPreventionType = null, MarketUnit? marketUnit = null, CancellationToken ct = default(CancellationToken));

    }

    private static CryptoPosition? SetupPosition()
    {
        string symbolName = "NAKAUSDT";
        if (GlobalData.ExchangeListName.TryGetValue("Bybit Spot", out Core.Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                CryptoPosition position = new()
                {
                    Account = null,
                    Exchange = symbol.Exchange,
                    Symbol = symbol,
                    Interval = GlobalData.IntervalList[3]
                };
                return position;
            }
        }

        return null;
    }

    private void ButtonAltradyOpenClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position open");
    }

    private void ButtonAltradyIncreasePositionClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position increase");
    }

    private void ButtonAltradyAddTpClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position set tp");
    }

    private void ButtonAltradyCancelClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position cancel");
    }
}