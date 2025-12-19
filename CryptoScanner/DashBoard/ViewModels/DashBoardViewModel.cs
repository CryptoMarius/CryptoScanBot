using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.DashBoard.Model;
using CryptoScanner.DashBoard.Services;

using SkiaSharp;

using System.Collections.ObjectModel;

namespace CryptoScanner.DashBoard.ViewModels;

public partial class DashBoardViewModel : ObservableObject
{
    private readonly DispatcherTimer? _barometerTimer;

    #region Traffic Light Status

    [ObservableProperty]
    private IBrush _scannerStatusBrush = Brushes.Gray;

    [ObservableProperty]
    private IBrush _traderStatusBrush = Brushes.Gray;

    [ObservableProperty]
    private IBrush _rulezStatusBrush = Brushes.Gray;

    #endregion

    #region Barometer

    [ObservableProperty]
    private ObservableCollection<string> _quotes = ["USDT", "BTC", "EUR"];

    [ObservableProperty]
    private string? _selectedQuote = "USDT";

    [ObservableProperty]
    private ObservableCollection<string> _intervals = ["15m", "30m", "1h", "4h", "1d"];

    [ObservableProperty]
    private string? _selectedInterval = "1h";

    [ObservableProperty]
    private WriteableBitmap? _chartImage;


    [ObservableProperty]
    private decimal _barometer1h = 0;

    [ObservableProperty]
    private decimal _barometer4h = 0;

    [ObservableProperty]
    private decimal _barometer1d = 0;

    [ObservableProperty]
    private string _barometerTime = "";

    [ObservableProperty]
    private string _applicationStatus = "";

    #endregion



    #region Market Indicators

    [ObservableProperty]
    private SymbolData _marketCapTotal = new();

    [ObservableProperty]
    private SymbolData _dollarIndex = new();

    [ObservableProperty]
    private SymbolData _spx500 = new();

    [ObservableProperty]
    private SymbolData _bitcoinDominance = new();

    [ObservableProperty]
    private SymbolData _fearAndGreedIndex = new();

    #endregion

    #region Crypto Symbols

    [ObservableProperty]
    private SymbolData _btcUsdt = new();

    [ObservableProperty]
    private SymbolData _ethUsdt = new();

    [ObservableProperty]
    private SymbolData _bnbUsdt = new();

    [ObservableProperty]
    private SymbolData _solUsdt = new();

    [ObservableProperty]
    private SymbolData _xrpUsdt = new();

    [ObservableProperty]
    private SymbolData _adaUsdt = new();

    #endregion


    [ObservableProperty]
    private int _klineTickerCount = 0;
    [ObservableProperty]
    private int _scannerExecuteCount = 0;
    [ObservableProperty]
    private int _scannerSignalCount = 0;


    private readonly ITradingViewService _tradingViewService;

    public DashBoardViewModel(ITradingViewService tradingViewService)
    {
        _tradingViewService = tradingViewService;

        // Subscribe to market indicator events
        _tradingViewService.MarketCapTotalChanged += (s, v) => MarketCapTotal.Update(v, null);
        _tradingViewService.DollarIndexChanged += (s, v) => DollarIndex.Update(v, null);
        _tradingViewService.Spx500Changed += (s, v) => Spx500.Update(v, null);
        _tradingViewService.BitcoinDominanceChanged += (s, v) => BitcoinDominance.Update(v, null);
        _tradingViewService.FearAndGreedIndexChanged += (s, v) => FearAndGreedIndex.Update(v, null);

        // Subscribe to crypto symbol events
        _tradingViewService.BtcUsdtChanged += (s, data) => BtcUsdt.Update(data.Price, data.Volume);
        _tradingViewService.EthUsdtChanged += (s, data) => EthUsdt.Update(data.Price, data.Volume);
        _tradingViewService.BnbUsdtChanged += (s, data) => BnbUsdt.Update(data.Price, data.Volume);
        _tradingViewService.SolUsdtChanged += (s, data) => SolUsdt.Update(data.Price, data.Volume);
        _tradingViewService.XrpUsdtChanged += (s, data) => XrpUsdt.Update(data.Price, data.Volume);

        System.Diagnostics.Debug.WriteLine("DashBoardViewModel constructor called");

        GlobalData.StatusesHaveChangedEvent += new AddTextEvent(StatusesHaveChangedEvent);
        InitializeBarometer();

        _barometerTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _barometerTimer.Tick += OnBarometerTimer;
        _barometerTimer.Start();

        StatusesHaveChangedEvent("");
    }

    private void StatusesHaveChangedEvent(string text)
    {
        if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            ApplicationStatus = "";
        else
            ApplicationStatus = GlobalData.ApplicationStatus.ToString();

        if (GlobalData.Settings.Trading.Active && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            TraderStatusBrush = App.GetBrushResource("PriceUpBrush");
        else
            TraderStatusBrush = App.GetBrushResource("PriceDownBrush");

        if (GlobalData.Settings.Signal.Active && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            ScannerStatusBrush = App.GetBrushResource("PriceUpBrush");
        else
            ScannerStatusBrush = App.GetBrushResource("PriceDownBrush");

        if (GlobalData.ActiveExchange != null)
        {
            var pause = GlobalData.ActiveExchange.Data.PauseTrading;
            if (!pause.Calculated.HasValue || (pause.Until.HasValue && pause.Until > DateTime.UtcNow))
                RulezStatusBrush = App.GetBrushResource("PriceDownBrush");
            else
                RulezStatusBrush = App.GetBrushResource("PriceUpBrush");
        }
        else RulezStatusBrush = App.GetBrushResource("PriceNeutralBrush");
    }

    public void InitializeBarometer()
    {
        // De actieve quotes erin zetten (default=usdt)
        List<string> quotes = [];
        foreach (CryptoQuoteData cryptoQuoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (cryptoQuoteData.FetchCandles)
                quotes.Add(cryptoQuoteData.Name);
        }
        if (quotes.Count == 0)
            quotes.Add("USDT");
        Quotes = new ObservableCollection<string>(quotes);
        SelectedQuote = quotes[0];

        // De intervallen in de combox zetten (default=1h)
        List<string> intervals = [];
        intervals.Add("1h");
        intervals.Add("4h");
        intervals.Add("1d");
        Intervals = new ObservableCollection<string>(intervals);
        SelectedInterval = intervals[0];

        BarometerTime = DateTime.Now.ToString("HH:mm");
    }

    partial void OnSelectedQuoteChanged(string? value)
    {
        System.Diagnostics.Debug.WriteLine($"Quote changed to: {value}");
        Task.Run(CalculateBarometer);
    }

    partial void OnSelectedIntervalChanged(string? value)
    {
        System.Diagnostics.Debug.WriteLine($"Interval changed to: {value}");
        Task.Run(CalculateBarometer);
    }

    private int BarometerLastMinute = -1;
    private void OnBarometerTimer(object? sender, EventArgs e)
    {
        try
        {
            // Update barometer chart
            if (((DateTime.Now.Second > 10) && (DateTime.Now.Minute != BarometerLastMinute)) || BarometerLastMinute == -1)
            {
                if (CalculateBarometer())
                {
                    BarometerLastMinute = DateTime.Now.Minute;
                    BarometerTime = DateTime.Now.ToString("HH:mm");
                }
            }
        }
        catch (Exception ex)
        {
            ScannerLog.Logger.Error(ex, "Error in OnBarometerTimer");
        }


        if (ExchangeBase.KLineTicker != null)
        {
            int count = ExchangeBase.KLineTicker.Count();
            if (KlineTickerCount != count)
                KlineTickerCount = count;
        }
        if (ScannerExecuteCount != SignalExecute.AnalyseCount)
            ScannerExecuteCount = SignalExecute.AnalyseCount;
        if (ScannerSignalCount != GlobalData.CreatedSignalCount)
            ScannerSignalCount = GlobalData.CreatedSignalCount;
    }



    private static SKColor GetThemeColor(string themeColor)
    {
        var app = Application.Current;
        if (app?.TryGetResource(themeColor, app.ActualThemeVariant, out object? resource) == true)
        {
            if (resource is SolidColorBrush brush)
            {
                var color = brush.Color;
                return new SKColor(color.R, color.G, color.B, color.A);
            }
        }
        return SKColors.White;
    }


    private void CreateBarometerBitmap(CryptoSymbolInterval symbolPeriod)
    {
        int blocks = Constants.BarometerGraphHours;

        // Dimensions 
        int intWidth = 400;
        int intHeight = 100;

        // Get theme colors
        SKColor bgColor = SKColor.Empty;
        Dispatcher.UIThread.Post(() => { bgColor = GetThemeColor("SystemControlBackgroundAltHighBrush"); });
        //var fgColor = GetForegroundColor();

        var bitmap = new WriteableBitmap(new PixelSize(intWidth, intHeight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var frameBuffer = bitmap.Lock(); // unsafe?

        CryptoCandleList candleList = symbolPeriod.CandleList;
        if (candleList.Count == 0)
            return;

        // determine range of data
        long loX = long.MaxValue;
        long hiX = long.MinValue;
        float loY = float.MaxValue;
        float hiY = float.MinValue;
        int candleCount = blocks * 60;
        long unix = candleList.Values.Last().OpenTime;
        while (candleCount-- > 0)
        {
            if (candleList.TryGetValue(unix, out CryptoCandle? candle))
            {
                if (loX > candle.OpenTime)
                    loX = candle.OpenTime;
                if (hiX < candle.OpenTime)
                    hiX = candle.OpenTime;

                // Ignore very high barometer values (malfunctions Bybit Futures)
                if (candle.Close > -50 && candle.Close < 50)
                {
                    if (loY > (float)candle.Close)
                        loY = (float)candle.Close;
                    if (hiY < (float)candle.Close)
                        hiY = (float)candle.Close;
                }
            }
            unix -= 60; // interval.Duration; The barometer has each 1 minute a barometer value
        }
        if (loX == long.MaxValue)
            return;


        // ranges x and y
        float screenX = hiX - loX; // unix time
        float screenY = hiY - loY; // barometer, something like -5 .. +5
        if (screenY < 5)
            screenY = 5f; // from -2 to +2
        if (hiY > 0.5 * screenY)
            screenY = +2 * hiY;
        if (loY < -0.5 * screenY)
            screenY = -2 * loY;



        // factor to keep points within picture
        float scaleX = intWidth / screenX;
        float scaleY = intHeight / screenY;

        // ofset to first point
        float offsetX = 0; // start in the left of the picture
        float offsetY = scaleY * 0.5f * screenY; // center of picture

        // flix y (specific for winform - what a crap)
        scaleY = -1 * scaleY;


        // Create SkiaSharp surface
        var info = new SKImageInfo(intWidth, intHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info, frameBuffer.Address, frameBuffer.RowBytes);
        var canvas = surface.Canvas;

        // Clear background
        canvas.Clear(bgColor);

        // Anti-aliasing
        using var paint = new SKPaint
        {
            IsAntialias = true,
            //FilterQuality = SKFilterQuality.High
        };

        // horizontal lines (1% per line)
        for (int y = -3; y <= 3; y++)
        {
            SKPoint p1 = new(0, offsetY + scaleY * y);
            SKPoint p2 = new(intWidth, offsetY + scaleY * y);
            if (y == 0)
            {
                paint.Color = SKColors.Red;
                paint.StrokeWidth = 1;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawLine(p1, p2, paint);
            }
            else
            {
                paint.Color = SKColors.Gray;
                paint.StrokeWidth = 1;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawLine(p1, p2, paint);
            }
        }

        // vertical lines (show hours)
        //Pen pen = new Pen(Color.Gray, 0.5F);
        long intervalTime = 60 * 60;
        long lastX = hiX - (hiX % intervalTime);
        while (lastX > loX)
        {
            //DateTime ehh = CandleTools.GetUnixDate(lastX);
            //GlobalData.AddTextToLogTab(ehh.ToLocalTime() + " " + lastX.ToString() + " intervaltime=" + intervalTime.ToString());
            SKPoint p1 = new(0, 0)
            {
                X = offsetX + scaleX * (float)(lastX - loX)
            };
            SKPoint p2 = new(0, intHeight)
            {
                X = offsetX + scaleX * (float)(lastX - loX)
            };
            paint.Color = SKColors.Gray;
            paint.StrokeWidth = 1;
            paint.Style = SKPaintStyle.Stroke;
            canvas.DrawLine(p1, p2, paint);
            lastX -= intervalTime;
        }


        bool init = false;
        SKPoint point1 = new(0, 0);
        SKPoint point2 = new(0, 0);
        candleCount = blocks * 60;
        unix = candleList.Values.Last().OpenTime;
        while (candleCount-- > 0)
        {
            if (candleList.TryGetValue(unix, out CryptoCandle? candle))
            {
                point2.X = offsetX + scaleX * (float)(candle.OpenTime - loX);
                point2.Y = offsetY + scaleY * ((float)candle.Close);
                //GlobalData.AddTextToLogTab(candle.OhlcText(symbol.DisplayFormat) + " " + point2.X.ToString("N8") + " " + point2.Y.ToString("N8"));

                if (init)
                {
                    if (candle.Close < 0)
                    {
                        //g.DrawLine(Pens.Red, point1, point2, paint);
                        paint.Color = SKColors.Red;
                        paint.StrokeWidth = 1;
                        paint.Style = SKPaintStyle.Stroke;
                    }
                    else
                    {
                        paint.Color = SKColors.Green;
                        paint.StrokeWidth = 1;
                        paint.Style = SKPaintStyle.Stroke;
                        //g.DrawLine(Pens.DarkGreen, point1, point2, paint);
                    }
                    canvas.DrawLine(point1, point2, paint);
                }

                point1 = point2;
                init = true;
            }
            unix -= 60; // interval.Duration; The barometer has each 1 minute a barometer value
        }
        //??????? dead code..
        //}
        //else
        //{
        //    // TODO: Een kruis door de bitmap zetten zodat we iets zien (het is nu een lege bitmap)
        //    var info = new SKImageInfo(intWidth, intHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        //    using var surface = SKSurface.Create(info, frameBuffer.Address, frameBuffer.RowBytes);
        //    var canvas = surface.Canvas;

        //    // Clear background
        //    canvas.Clear(bgColor);
        //}
        ChartImage = bitmap;
    }



    private bool CalculateBarometer()
    {
        try
        {
            if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running)
                return false;
            if (string.IsNullOrEmpty(SelectedQuote))
                return false;
            if (string.IsNullOrEmpty(SelectedInterval))
                return false;

            // Calculate the latest barometer if needed
            BarometerTools barometerTools = new();
            barometerTools.ExecuteAsync();


            // Update the barometer graph
            if (GlobalData.ActiveExchange == null)
                return false;

            string quoteName = SelectedQuote;
            if (!GlobalData.Settings.QuoteCoins.TryGetValue(quoteName, out CryptoQuoteData? quoteData))
                return false;

            string intervalName = SelectedInterval;
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                return false;

            if (!GlobalData.ActiveExchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice + quoteData.Name, out CryptoSymbol? symbol))
                return false;

            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
            CreateBarometerBitmap(symbolPeriod);


            // Update the barometer Values
            CryptoIntervalPeriod[] list = [CryptoIntervalPeriod.interval1h, CryptoIntervalPeriod.interval4h, CryptoIntervalPeriod.interval1d];
            foreach (CryptoIntervalPeriod intervalPeriod in list)
            {
                CryptoBarometerData? barometerData = GlobalData.ActiveExchange!.Data.GetBarometer(quoteData.Name, intervalPeriod);
                if (barometerData != null)
                {
                    if (barometerData.PriceBarometer != null)
                    {
                        if (intervalPeriod == CryptoIntervalPeriod.interval1h)
                            Barometer1h = barometerData.PriceBarometer.Value;
                        else if (intervalPeriod == CryptoIntervalPeriod.interval4h)
                            Barometer4h = barometerData.PriceBarometer.Value;
                        else if (intervalPeriod == CryptoIntervalPeriod.interval1d)
                            Barometer1d = barometerData.PriceBarometer.Value;
                    }
                }
            }


            // Update the barometer time
            if (symbolPeriod.CandleList.Values.Count > 0)
            {
                CryptoCandle candle = symbolPeriod.CandleList.Values.Last();
                BarometerTime = CandleTools.GetUnixDate((long)candle.OpenTime + 60).ToLocalTime().ToString("HH:mm");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return true;
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
            return false;
        }
    }
}