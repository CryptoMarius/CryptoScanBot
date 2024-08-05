using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;

using CryptoScanBot.Core;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.Barometer;
using System.Text.Json;
using CryptoScanBot.Core.Settings;
using Bybit.Net.Clients;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Objects;
using Bybit.Net.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Experiment.Exchange.Altrady;


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

        ExchangeHelper.ExchangeDefaults();
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

            if (text != "")
            {
                text = DateTime.Now.ToLocalTime() + " " + text + "\r\n";
                if (InvokeRequired)
                    Invoke((MethodInvoker)(() => textBox1.AppendText(text)));
                else
                    textBox1.AppendText(text);
            }
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
                GlobalData.TradingApi = JsonSerializer.Deserialize<SettingsExchangeApi>(text);
            }
            else
                throw new Exception($"file not found {filename}");
            ExchangeHelper.ExchangeDefaults();
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
                GlobalData.AltradyApi = JsonSerializer.Deserialize<SettingsAltradyApi>(text);
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
        LoadExchangeSettings(" - Bybit Spot - DcaBot account");

        //BinanceTestAsync();
        //ByBitUtaSpotTestAsync();
        //KucoinTest();
        //MexcTest();

        // EmulatorTest();

        // Be carefull, this one places active/live orders on the exchange
        //await ExchangeTest.Exchange.Bybit.Spot.Test.BybitTestAsync();


        // Plaats een order op de exchange *ze lijken op elkaar, maar het is net elke keer anders)
        using BybitRestClient client = new();

        //client.ClientOptions.

        //StopOrderType x = StopOrderType.TpSlOrder;

        client.ClientOptions.OutputOriginalData = true;

        WebCallResult<BybitOrderId> result = await client.V5Api.Trading.PlaceOrderAsync(
            Category.Spot,
            "APRSUSDT",
            OrderSide.Buy,
            NewOrderType.Limit,
            quantity: 1.01m,
            price: 0.51m,
            isLeverage: false,
            triggerPrice: 0.50m,
            triggerDirection: TriggerDirection.Fall
            //timeInForce: TimeInForce.PostOnly
            );

        string text = JsonSerializer.Serialize(result, ExchangeHelper.JsonSerializerNotIndented).Trim();
        GlobalData.AddTextToLogTab(text);
        ScannerLog.Logger.Trace(text);

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
        CryptoPosition position = new();

        string symbolName = "NAKAUSDT";
        if (GlobalData.ExchangeListName.TryGetValue("Bybit Spot", out Core.Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                position.Symbol = symbol;
                position.Exchange = symbol.Exchange;
                position.Interval = GlobalData.IntervalList[3];
                return position;
            }
        }

        return null;
    }

    private void ButtonAltradyOpenClick(object sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position open");
    }

    private void ButtonAltradyIncreasePositionClick(object sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position increase");
    }

    private void ButtonAltradyAddTpClick(object sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position set tp");
    }

    private void ButtonAltradyCancelClick(object sender, EventArgs e)
    {
        CryptoPosition? position = SetupPosition();
        AltradyWebhook.DelegateAllToAltrady(position, "https://api.altrady.com/v2/signal_bot_positions", "Altrady - Position cancel");
    }
}