using Bybit.Net.Clients;

using CryptoScanBot.Core.Json;

using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trader;

using System.Text.Json;


namespace CryptoScanner.Experiment;

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

            string? exchangeName = ApplicationParams.Options!.ExchangeName;
            if (exchangeName != null)
            {
                // De default exchange is Binance (geen goede keuze in NL op dit moment)
                if (exchangeName == "")
                    exchangeName = "Bybit Spot";
                if (GlobalData.ExchangeListName.TryGetValue(exchangeName, out var exchange))
                {
                    GlobalData.ActiveExchange = exchange;
                    GlobalData.Settings.General.ExchangeName = exchange.Name;
                }
                else throw new Exception($"Exchange {exchangeName} bestaat niet");
            }
        }


        GlobalData.LoadSettings();
        ApplicationParams.InitApplicationOptions();
        GlobalData.InitializeExchange();
        TradingConfig.IndexStrategyInternally();
        TradingConfig.InitWhiteAndBlackListSettings();

        GlobalData.LoadSymbols();
        BarometerTools.InitBarometerSymbols();
        TradingConfig.InitWhiteAndBlackListSettings(); // after loading symbols

        GlobalData.ActiveExchange!.GetApiInstance().ExchangeDefaults();
        ThreadLoadData.IndexQuoteDataSymbols(GlobalData.ActiveExchange!);



        GlobalData.ApplicationStatus = CryptoApplicationStatus.Running;

        GlobalData.TradingApi.Key = "";
        GlobalData.TradingApi.Secret = "";
        GlobalData.TradingApi.PassPhrase = "";

        Button1_Click(null, null);
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

            //File.AppendAllText(@"D:\Shares\Projects\.Net\CryptoScanner\Testjes\bin\Debug\data\backtest.txt", text);
        }
    }

    public static void LoadExchangeSettings(string name)
    {
        string filename = $"{GlobalData.AppName}-exchange{name}.json";
        try
        {
            string fullName = GlobalData.GetBaseDir() + filename;
            if (File.Exists(fullName))
            {
                string text = File.ReadAllText(fullName);
                GlobalData.TradingApi = JsonSerializer.Deserialize<SettingsExchangeApi>(text, JsonTools.DeSerializerOptions)!;
            }
            else
                throw new Exception($"file not found {filename}");
            GlobalData.ActiveExchange!.GetApiInstance().ExchangeDefaults();
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
                GlobalData.AltradyApi = JsonSerializer.Deserialize<SettingsAltradyApi>(text, JsonTools.DeSerializerOptions)!;
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


    private async void DoSomethingWithLux()
    {

        DateTime dateMax = DateTime.UtcNow.AddMinutes(5);
        DateTime dateMin = dateMax.AddDays(-2);
        long timeMin = CandleTools.GetUnixTime(dateMin, 5 * 60);
        long timeMax = CandleTools.GetUnixTime(dateMax, 5 * 60);

        var exchange = GlobalData.ActiveExchange!;
        if (exchange.SymbolListName.TryGetValue("JUPUSDT", out CryptoSymbol? symbol))
        {
            CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m);

            BybitRestClient client = new();
            var api = exchange.GetApiInstance();
            await api.Candle.GetCandlesForInterval(client, symbol, interval, timeMin, timeMax);

            List<CryptoCandle> history = [.. symbolInterval.CandleList.Values];
            CandleIndicatorData.CalculateIndicators(symbol, interval, history, symbolInterval.CandleList.Count);

            foreach (var candle in symbolInterval.CandleList.Values)
            {
                LuxIndicator.CalculateNew(symbol, out int luxOverSold, out int luxOverBought, interval.IntervalPeriod, candle.OpenTime + interval.Duration);

                if (luxOverSold > 0 || luxOverBought > 0)
                    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {candle.OpenTime} {candle.DateLocal} {candle.CandleData!.Rsi:N2} {luxOverSold} {luxOverBought}");
            }
            GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} done...");
        }

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
            AltradyWebhook.DelegateControlToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position open");
    }

    private void ButtonAltradyIncreasePositionClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateControlToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position increase");
    }

    private void ButtonAltradyAddTpClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateControlToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position set tp");
    }

    private void ButtonAltradyCancelClick(object? sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        if (position != null)
            AltradyWebhook.DelegateControlToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position cancel");
    }



    private async void Button1_Click(object? sender, EventArgs? e)
    {
        // just a general purpose test place
        ScannerLog.Logger.Info("Testing....");
        ScannerLog.Logger.Trace("Testing....");
        ScannerLog.Logger.Error("Testing....");

        //https://api.vantage.sh/v2/workspaces



        //LoadExchangeSettings(" - Bybit UTA api");
        //LoadExchangeSettings(" - Bybit Spot - Main account");
        //LoadExchangeSettings(" - Bybit Spot - DcaBot account");

        //BinanceTestAsync();
        //ByBitUtaSpotTestAsync();
        //KucoinTest();
        //MexcTest();

        // EmulatorTest();

        // Be carefull, this one places active/live orders on the exchange
        //await ExchangeTest.Exchange.Bybit.Spot.Test.BybitTestAsync();
        //int loop = 10;
        //string prefix = "JUPUSDT 5m";
        //using MexcRestClient client = new();



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


        string token = "KHW58SAIQTA3XXQR";
        //Get candles? (open/high/low/close, maar geen volume)
        //string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=IBM&interval=5min&apikey={token}";
        //voorbeeld: https://www.alphavantage.co/query?function=FX_INTRADAY&from_symbol=EUR&to_symbol=USD&interval=5min&apikey=demo

        string url = $"https://www.alphavantage.co/query?function=FX_INTRADAY&from_symbol=EUR&to_symbol=USD&interval=5min&apikey={token}";
        using (var client = new HttpClient()) // { BaseAddress = new Uri(url) }
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            //client.DefaultRequestHeaders.Add("authorization", $"Bearer {token}");

            using (var response = await client.GetAsync(url))
            {
                string responseData = await response.Content.ReadAsStringAsync();
                GlobalData.AddTextToLogTab(url);
                GlobalData.AddTextToLogTab(responseData);
            }
        }
    }

}